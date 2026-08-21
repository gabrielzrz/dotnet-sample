using dotnet_sample.Services;

namespace dotnet_sample.Controllers;

public static class PedidoEndpoints
{
    public static void MapPedidoEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/pedidos-minimal", (IPedidoService pedidoService) =>
        {
            return pedidoService.ObterTodos();
        });
    }
}