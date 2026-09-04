using Cronos;

namespace dotnet_sample.Workers;

public class HorariosFixosWorker : BackgroundService
{
    private static readonly CronExpression Cron = CronExpression.Parse("0 5,14,16,20 * * *");

    private readonly ILogger<HorariosFixosWorker> _logger;

    public HorariosFixosWorker(ILogger<HorariosFixosWorker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var agora = DateTimeOffset.Now;
            var proximaExecucao = Cron.GetNextOccurrence(agora, TimeZoneInfo.Local);

            if (proximaExecucao is null)
            {
                _logger.LogWarning("Expressão cron não tem próxima ocorrência. Encerrando.");
                return;
            }

            var delay = proximaExecucao.Value - agora;
            _logger.LogInformation(
                "Próxima execução agendada para {Horario} (faltam {Delay})",
                proximaExecucao.Value, delay);

            await Task.Delay(delay, stoppingToken);

            await RodarJobAsync(stoppingToken);
        }
    }

    private Task RodarJobAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[HorariosFixosWorker] Job executado às {Hora}", DateTimeOffset.Now);
        return Task.CompletedTask;
    }
}
