namespace dotnet_sample.Services;

public interface IPedidoRepository
{
    IEnumerable<Pedido> ObterTodos();

    Pedido? ObterPorId(int id);

    void Adicionar(Pedido pedido);
}