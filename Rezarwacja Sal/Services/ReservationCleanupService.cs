using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Rezarwacja_Sal.Data;
using Rezarwacja_Sal.Models;

namespace Rezarwacja_Sal.Services
{
    // Background worker that periodically cancels stale Pending reservations
    public class ReservationCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ReservationCleanupService> _logger;

        // How often to run the cleanup (default: every hour)
        private static readonly TimeSpan Interval = TimeSpan.FromHours(1);
        // Pending older than this threshold will be auto-cancelled (default: 3 days)
        private static readonly TimeSpan PendingMaxAge = TimeSpan.FromDays(3);

        public ReservationCleanupService(IServiceProvider serviceProvider, ILogger<ReservationCleanupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // initial small delay to let app start
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await RunCleanupAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Reservation cleanup failed");
                }

                try
                {
                    await Task.Delay(Interval, stoppingToken);
                }
                catch (TaskCanceledException) { /* ignore */ }
            }
        }

        private async Task RunCleanupAsync(CancellationToken ct)
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var utcNow = DateTime.UtcNow;
            var threshold = utcNow - PendingMaxAge;

            var stale = await db.Reservations
                .Where(r => r.Status == ReservationStatus.Pending && r.CreatedAt < threshold)
                .ToListAsync(ct);

            if (stale.Count == 0)
            {
                _logger.LogDebug("Cleanup: no stale pending reservations found.");
                return;
            }

            foreach (var res in stale)
            {
                res.Status = ReservationStatus.Cancelled;
                res.UpdatedAt = utcNow;
            }

            await db.SaveChangesAsync(ct);
            _logger.LogInformation("Cleanup: cancelled {Count} stale pending reservations (older than {Days} days)", stale.Count, PendingMaxAge.TotalDays);
        }
    }
}
