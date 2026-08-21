namespace dotnet_sample.Services;

public class PedidoRepository : IPedidoRepository
{
    private readonly List<Pedido> _pedidos = [];

    public IEnumerable<Pedido> ObterTodos() => _pedidos;

    public Pedido? ObterPorId(int id) => _pedidos.FirstOrDefault(p => p.Id == id);

    public void Adicionar(Pedido pedido)
    {
        pedido.Id = _pedidos.Count + 1;
        _pedidos.Add(pedido);
    }
}