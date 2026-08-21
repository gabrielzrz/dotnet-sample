// Arquivo apenas ilustrativo, para explicar os conceitos de DI/DIP em testes.
// Não faz parte do projeto (não é compilado pelo dotnet-sample.csproj) e não referencia
// xUnit/Moq de verdade — é só pra mostrar a diferença de código entre os 3 cenários.

using dotnet_sample;
using dotnet_sample.Services;

namespace Exemplos;

// =====================================================================
// CENÁRIO 1 — SEM DI
// PedidoService cria o PedidoRepository internamente, com "new".
// =====================================================================

public class PedidoServiceSemDI
{
    private readonly PedidoRepository _repository;

    public PedidoServiceSemDI()
    {
        _repository = new PedidoRepository(); // criado internamente, sem escolha externa
    }

    public Pedido Criar(Pedido pedido)
    {
        if (pedido.Valor <= 0)
            throw new ArgumentException("Valor do pedido deve ser positivo.");

        _repository.Adicionar(pedido);
        return pedido;
    }
}

public class PedidoServiceSemDITests
{
    [Fact]
    public void Criar_DeveLancarExcecao_QuandoValorForZeroOuNegativo()
    {
        var service = new PedidoServiceSemDI(); // não dá pra trocar o repository
        var pedido = new Pedido { Valor = 0 };

        // problema: esse teste sempre executa o PedidoRepository real por dentro,
        // sem nenhum ponto de controle
        Assert.Throws<ArgumentException>(() => service.Criar(pedido));
    }
}

// =====================================================================
// CENÁRIO 2 — COM DI, IMPLEMENTAÇÃO CONCRETA (sem interface)
// PedidoService recebe PedidoRepository pronto via construtor.
// =====================================================================

public class PedidoServiceComDI
{
    private readonly PedidoRepository _repository;

    public PedidoServiceComDI(PedidoRepository repository)
    {
        _repository = repository; // recebido de fora, não criado aqui
    }

    public Pedido Criar(Pedido pedido)
    {
        if (pedido.Valor <= 0)
            throw new ArgumentException("Valor do pedido deve ser positivo.");

        _repository.Adicionar(pedido);
        return pedido;
    }
}

public class PedidoServiceComDITests
{
    [Fact]
    public void Criar_DeveLancarExcecao_QuandoValorForZeroOuNegativo_SemMock()
    {
        var repository = new PedidoRepository(); // ainda é obrigado a usar a real
        var service = new PedidoServiceComDI(repository);
        var pedido = new Pedido { Valor = 0 };

        Assert.Throws<ArgumentException>(() => service.Criar(pedido));
    }

    [Fact]
    public void Criar_DeveLancarExcecao_QuandoValorForZeroOuNegativo_ComMock()
    {
        // só funciona se PedidoRepository não for sealed
        // e Adicionar()/ObterTodos() forem marcados como virtual
        var mockRepository = new Mock<PedidoRepository>();
        var service = new PedidoServiceComDI(mockRepository.Object);
        var pedido = new Pedido { Valor = 0 };

        Assert.Throws<ArgumentException>(() => service.Criar(pedido));
    }
}

// =====================================================================
// CENÁRIO 3 — COM DI + INVERSÃO DE DEPENDÊNCIA (interface)
// PedidoService depende só de IPedidoRepository.
// =====================================================================

public class PedidoServiceComDIETests
{
    private class PedidoRepositoryFake : IPedidoRepository
    {
        private readonly List<Pedido> _pedidos = [];

        public IEnumerable<Pedido> ObterTodos() => _pedidos;
        public Pedido? ObterPorId(int id) => _pedidos.FirstOrDefault(p => p.Id == id);
        public void Adicionar(Pedido pedido) => _pedidos.Add(pedido);
    }

    [Fact]
    public void Criar_DeveLancarExcecao_QuandoValorForZeroOuNegativo()
    {
        var repositorioFake = new PedidoRepositoryFake(); // implementação de mentira
        var service = new PedidoService(repositorioFake);
        var pedido = new Pedido { Valor = 0 };

        Assert.Throws<ArgumentException>(() => service.Criar(pedido));

        // dá pra verificar comportamento também, não só a exceção:
        Assert.Empty(repositorioFake.ObterTodos());
    }
}
