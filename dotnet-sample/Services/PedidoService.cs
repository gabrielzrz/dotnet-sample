namespace dotnet_sample.Services;

public class PedidoService : IPedidoService
{
    private readonly IPedidoRepository _repository;

    public PedidoService(IPedidoRepository repository)
    {
        _repository = repository;
    }

    public IEnumerable<Pedido> ObterTodos() => _repository.ObterTodos();

    public Pedido Criar(Pedido pedido)
    {
        if (pedido.Valor <= 0)
            throw new ArgumentException("Valor do pedido deve ser positivo.");

        _repository.Adicionar(pedido);

        INotificadorService notificador = new NotificadorService();
        notificador.Notificar($"Pedido {pedido.Id} criado com valor {pedido.Valor}.");

        return pedido;
    }
}