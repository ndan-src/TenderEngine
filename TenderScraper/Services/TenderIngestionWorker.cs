using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace TenderScraper.Services;

public class TenderIngestionWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;

    public TenderIngestionWorker(IServiceProvider serviceProvider) 
        => _serviceProvider = serviceProvider;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var orchestrator = scope.ServiceProvider.GetRequiredService<IngestionOrchestrator>();
                await orchestrator.RunDailyIngestion(DateTime.Now.AddDays(-1)); // Process yesterday's tenders
            }

            // Wait 24 hours until the next run
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }
}