using Microsoft.AspNetCore.Mvc;
using dotnet_sample.Services;

namespace dotnet_sample.Controllers;

[ApiController]
[Route("[controller]")]
public class PedidosController : ControllerBase
{
    private readonly IPedidoService _pedidoService;

    public PedidosController(IPedidoService pedidoService)
    {
        _pedidoService = pedidoService;
    }

    [HttpGet]
    public IEnumerable<Pedido> Get()
    {
        return _pedidoService.ObterTodos();
    }

    [HttpPost]
    public IActionResult Post(Pedido pedido)
    {
        var criado = _pedidoService.Criar(pedido);
        return Ok(criado);
    }
}