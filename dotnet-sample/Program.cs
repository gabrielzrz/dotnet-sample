using dotnet_sample.Controllers;
using dotnet_sample.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddScoped<IPedidoRepository, PedidoRepository>();
builder.Services.AddTransient<IPedidoService, PedidoService>();

// builder.Services.AddSingleton<IPedidoService, PedidoService>();
// builder.Services.AddTransient<IPedidoService, PedidoService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
app.MapPedidoEndpoints();

await app.RunAsync();