using dotnet_sample.Data;
using dotnet_sample.Services;
using Microsoft.EntityFrameworkCore;

namespace dotnet_sample.Workers;

public class PedidoAltoValorWorker : BackgroundService
{
    private const decimal ValorAlerta = 1000m;
    private static readonly TimeSpan Intervalo = TimeSpan.FromSeconds(10);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PedidoAltoValorWorker> _logger;

    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public PedidoAltoValorWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<PedidoAltoValorWorker> logger,
        IDbContextFactory<AppDbContext> dbFactory)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _dbFactory = dbFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Intervalo);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await VerificarPedidosAsync(stoppingToken);
                await VerificarPedidosAsync2(stoppingToken);
            }
            catch (Exception ex)
            {
                // precisa do try catch para caso der erro, nao derrubar a aplicação inteira. 
                // Desde .NET 6, a microsoft decidiu que um worker que der problema derruba a aplicação para nao ter um job importante parado silenciosamente
                _logger.LogError(ex, "Erro ao verificar pedidos de alto valor.");
            }
        }
    }

    private async Task VerificarPedidosAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();

        var pedidoService = scope.ServiceProvider.GetRequiredService<IPedidoService>();
        var notificador = scope.ServiceProvider.GetRequiredService<INotificadorService>();

        var pedidosAltoValor = pedidoService.ObterTodos()
            .Where(p => p.Valor >= ValorAlerta);

        foreach (var pedido in pedidosAltoValor)
        {
            stoppingToken.ThrowIfCancellationRequested();
            notificador.Notificar($"[Worker] Pedido {pedido.Id} tem valor alto: {pedido.Valor:C}");
        }

        _logger.LogInformation("Verificação de pedidos concluída às {Hora}", DateTimeOffset.Now);
    }

    private async Task VerificarPedidosAsync2(CancellationToken stoppingToken)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(stoppingToken);

        var pedidosAltoValor = await db.Pedidos
            .Where(p => p.Valor >= ValorAlerta)
            .AsNoTracking()
            .ToListAsync(stoppingToken);

        foreach (var pedido in pedidosAltoValor)
        {
            _logger.LogInformation(
                "[VerificarPedidosAsync2] Pedido {Id} (banco) tem valor alto: {Valor:C}",
                pedido.Id, pedido.Valor);
        }
    }
}
