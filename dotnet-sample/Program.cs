using dotnet_sample;
using dotnet_sample.Controllers;
using dotnet_sample.Data;
using dotnet_sample.Services;
using dotnet_sample.Workers;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddScoped<IPedidoRepository, PedidoRepository>();
builder.Services.AddScoped<IPedidoService, PedidoService>();
builder.Services.AddScoped<INotificadorService, NotificadorService>();

builder.Services.AddDbContextFactory<AppDbContext>();

// AddHostedService registra o worker como Singleton e o inicia junto com a app.
builder.Services.AddHostedService<PedidoAltoValorWorker>();
builder.Services.AddHostedService<HorariosFixosWorker>();

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