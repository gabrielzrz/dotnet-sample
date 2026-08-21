namespace dotnet_sample.Services;

public interface IPedidoService
{
    IEnumerable<Pedido> ObterTodos();

    Pedido Criar(Pedido pedido);
}