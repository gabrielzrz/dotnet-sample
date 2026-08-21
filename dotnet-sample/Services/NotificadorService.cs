namespace dotnet_sample.Services;

public class NotificadorService : INotificadorService
{
    public void Notificar(string mensagem)
    {
        Console.WriteLine($"[Notificacao] {mensagem}");
    }
}