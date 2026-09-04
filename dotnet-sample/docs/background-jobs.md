# Background Jobs no .NET — Resumo de Estudo

## 1. O que são

Unidades de trabalho que rodam fora do ciclo request-response, dentro do mesmo
processo da aplicação, gerenciadas pelo `Host` (o mesmo `IHost`/`WebApplication`
que serve os controllers). A peça central é `IHostedService`, geralmente
implementado via `BackgroundService` (classe base que já cuida do start/stop).

Categorias:

- **Long-running/contínuo** — ex: consumidor de fila.
- **Agendado/periódico** — `PeriodicTimer`, cron.
- **Fire-and-forget** — processar algo depois de uma requisição HTTP responder.

Comparando com Spring: equivale a `@Scheduled` rodando dentro do mesmo
`ApplicationContext` da aplicação — beans gerenciados pelo mesmo container que
serve a API.

## 2. Mecânica do `BackgroundService`

- Sempre registrado como **Singleton** via `AddHostedService<T>()` — sem opção
  de mudar isso.
- `ExecuteAsync(CancellationToken stoppingToken)` roda uma vez no boot e fica
  ativo até o shutdown.
- `stoppingToken` é cancelado no shutdown gracioso — todo `await` deve
  respeitá-lo, senão o processo não encerra limpo.
- `PeriodicTimer` é preferível a `while(true) + Task.Delay`: não gera drift no
  intervalo e já entende cancelamento.
- Nunca deixe exceção escapar de `ExecuteAsync` sem `try/catch` — senão o
  worker para de rodar (silenciosamente) até a app reiniciar.

## 3. Injeção de dependência dentro do worker

Regra de ouro: **Singleton não pode consumir Scoped/Transient direto no
construtor** — isso é uma *captive dependency*: a instância Scoped ficaria
presa pra sempre, nunca recriada, nunca descartada.

### Detalhe importante: não é sobre ser um job

O problema **não tem nada a ver com ser um `BackgroundService`**. É uma regra
geral do container de DI: qualquer serviço registrado como Singleton que
dependa (direta ou indiretamente) de um Scoped tem o mesmo problema — seja ele
um worker, um cache, um serviço de configuração, qualquer classe comum
registrada com `AddSingleton<T>()`.

Isso foi comprovado na prática de duas formas:

1. Injetando `IPedidoService` (Scoped) direto no construtor de
   `PedidoAltoValorWorker` (um `BackgroundService`) — a aplicação falhou no
   `builder.Build()`:

   ```
   InvalidOperationException: Cannot consume scoped service
   'IPedidoService' from singleton 'IHostedService'.
   ```

2. Criando um serviço comum, sem nenhuma relação com background jobs,
   registrado como `AddSingleton<MeuServicoSingletonTeste>()`, com
   `IPedidoService` injetado no construtor — o mesmo erro aconteceu,
   trocando só o nome da classe:

   ```
   InvalidOperationException: Cannot consume scoped service
   'IPedidoService' from singleton 'MeuServicoSingletonTeste'.
   ```

`BackgroundService` só é o lugar onde esse erro aparece com mais frequência,
por dois motivos: `AddHostedService<T>()` sempre registra como Singleton
(obrigatório, sem alternativa), e é comum um worker precisar de
repositórios/`DbContext` (tipicamente Scoped) pra fazer seu trabalho — daí a
colisão de lifetimes surgir naturalmente.

Essa validação acontece no `builder.Build()`, porque em Development
`ValidateOnBuild` + `ValidateScopes` vêm ligados por padrão — o .NET varre
toda a árvore de dependências no boot e recusa subir a aplicação se detectar
um Singleton dependendo de um Scoped, em vez de deixar isso estourar
silenciosamente em produção mais tarde. Em Production essa validação vem
desligada por padrão (por performance no boot).

### Soluções corretas

| Padrão | Quando usar | Como |
|---|---|---|
| `IServiceScopeFactory` | Genérico, qualquer serviço Scoped | `_scopeFactory.CreateScope()` a cada ciclo, resolve o resto via `scope.ServiceProvider` |
| `IDbContextFactory<TContext>` | Específico do EF Core | Injeta direto no construtor (é Singleton), `await using var db = await _dbFactory.CreateDbContextAsync(...)` por ciclo |

`IDbContextFactory` em si é **thread-safe** (pode chamar `CreateDbContextAsync`
concorrentemente); cada `DbContext` criado **não é** — uso exclusivo,
descartável.

Comparando com Spring: equivalente ao problema de injetar um bean
`request`-scoped num singleton sem `@Scope(proxyMode =
ScopedProxyMode.TARGET_CLASS)` — só que o .NET não tem proxy mágico, você pede
o escopo novo manualmente.

## 4. `async`/`await` — a mecânica de threads

- `await` **não bloqueia** a thread. Ele só libera a thread **se a operação
  ainda não tiver terminado** naquele instante — se já terminou, o código só
  continua direto, sem custo nenhum de troca.
- O método é picotado em pedaços pelos pontos de `await` — cada pedaço pode
  rodar numa thread diferente do pool. Não existe "uma thread só do início ao
  fim do método".
- Isso funciona porque I/O real (rede, banco, timer) usa notificação do
  sistema operacional (`epoll`/IOCP) — ninguém precisa ficar plantado
  esperando, o SO avisa quando terminar.
- Ganho real: **concorrência de I/O com pouco uso de thread/memória**, não
  velocidade de CPU. Trabalho pesado de CPU não ganha nada com `async`/
  `await` — aí a ferramenta é `Task.Run`/paralelismo real.
- `@Async` do Spring é conceitualmente mais parecido com `Task.Run` (dedica
  uma thread inteira ao método) do que com `async`/`await`. O equivalente
  Java ao que `await` faz é WebFlux/Reactor ou Virtual Threads (Loom).
- Vale igual pra endpoints de Minimal API/Controllers: Kestrel não reserva
  thread por conexão/endpoint — mesmo modelo de pool compartilhado.
- É **opt-in por chamada**: `ToList()` bloqueia, `ToListAsync()` não — usar
  `async` no método não muda nada se você usa APIs síncronas por dentro (um
  método `async` sem nenhum `await` real dentro não libera thread nenhuma).

## 5. `using` / `await using`

Garante que um recurso que implementa `IDisposable`/`IAsyncDisposable` (timer,
conexão de banco, escopo do DI) seja liberado automaticamente ao sair do
escopo — mesmo se ocorrer uma exceção. Sem isso, objetos Scoped resolvidos
manualmente (ex: dentro de um `CreateScope()`) ou conexões de banco ficam
"vivos" sem necessidade até o garbage collector notar.

Equivalente Java: try-with-resources (`try (var x = ...) { }`), usando
`AutoCloseable`/`Closeable` em vez de `IDisposable`.

## 6. Uso de recursos

Workers rodam no **mesmo ThreadPool** da API — trabalho síncrono pesado
dentro de um job compete diretamente com as requisições HTTP por threads
(thread pool starvation). Vários `AddHostedService<T>` diferentes rodam
concorrentemente entre si, não em fila.

## 7. Casos práticos

Processamento de fila (RabbitMQ/Kafka), jobs agendados (limpeza, relatórios),
**outbox pattern**, cache warming, fire-and-forget pós-request (e-mail de
confirmação).

## 8. Boas práticas

- `try/catch` no loop.
- Respeitar `CancellationToken`.
- `PeriodicTimer` em vez de `while(true) + Task.Delay`.
- Idempotência (reprocessamento de mensagens).
- Health check separado pra saber se o worker está vivo.

## 9. Quando NÃO usar

- Trabalho que o cliente precisa confirmar de verdade (isso é síncrono).
- Jobs que precisam sobreviver a restart do processo (usar Hangfire/Quartz.NET
  com storage persistente, ou serviço separado).
- Carga pesada de CPU (separar em Worker Service próprio).
- Precisa escalar independente da API.

## 10. Impacto em memória

Worker é Singleton — estado acumulado nos campos dele vive a vida toda da
aplicação (fonte clássica de memory leak de longo prazo). `using`/`await
using` em cada scope/`DbContext` é essencial pra liberar memória entre
execuções. Filas internas (`Channel<T>`) precisam de capacidade limitada
(`BoundedChannelOptions`), senão crescem sem limite se o consumidor for mais
lento que o produtor.
