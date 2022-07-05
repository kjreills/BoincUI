using System.Collections.Concurrent;

namespace Boinc.Blazor.Data
{
    public class BaseHostedService<T> : BackgroundService, IDisposable where T : IScopedProcessingService
    {
        private readonly IServiceProvider _services;
        protected readonly ILogger<BaseHostedService<T>> _logger;
        private readonly TimeSpan _frequency;

        public BaseHostedService(IServiceProvider services, ILogger<BaseHostedService<T>> logger, TimeSpan frequency)
        {
            _services = services;
            _logger = logger;
            _frequency = frequency;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("{BackgroundService} is starting", nameof(T));

            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _services.CreateScope())
                {
                    var service = scope.ServiceProvider.GetRequiredService<T>();

                    await service.ExecuteAsync(stoppingToken);
                }

                await Task.Delay(_frequency, stoppingToken);
            }
        }

        public override Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Timed Hosted Service is stopping.");

            return base.StopAsync(stoppingToken);
        }
    }

    public interface IScopedProcessingService
    {
        Task ExecuteAsync(CancellationToken stoppingToken);
    }

    public class BoincHostConnector : IScopedProcessingService
    {
        private readonly ILogger<BoincHostConnector> _logger;
        private readonly HostService _hostService;

        private static readonly ConcurrentDictionary<int, BoincHostViewModel> _boincHosts = new();

        public static IReadOnlyList<BoincHostViewModel> BoincHosts => _boincHosts.Values.ToList();
        public static event EventHandler? HostsUpdated;

        public BoincHostConnector(ILogger<BoincHostConnector> logger, HostService hostService)
        {
            _logger = logger;
            _hostService = hostService;
        }

        public async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Begin BOINC Host Connector refresh loop");

            if (!BoincHosts.Any())
            {
                var newHosts = await _hostService.GetAll();
                AddHostRange(newHosts);
            }
            else
            {
                foreach (var host in _boincHosts)
                {
                    await host.Value.LoadDataAsync();
                }
            }

            Updated();

            _logger.LogInformation("Finish BOINC Host Connector refresh loop");
        }

        public static void Updated() => HostsUpdated?.Invoke(BoincHosts, new EventArgs());

        public static async void AddHost(BoincHost host)
        {
            var newHost = await new BoincHostViewModel(host).LoadDataAsync();

            _boincHosts.AddOrUpdate(newHost.Id, (k) => newHost, (k, v) => newHost);
            Updated();
        }

        public static async void AddHostRange(IEnumerable<BoincHost> hosts)
        {
            var newHosts = await Task.WhenAll(hosts.Select(x => new BoincHostViewModel(x).LoadDataAsync()));

            foreach (var host in newHosts)
            {
                _boincHosts.AddOrUpdate(host.Id, (k) => host, (k, v) => host);
            }
            Updated();
        }

        public static void RemoveHost(BoincHostViewModel host)
        {
            _boincHosts.TryRemove(host.Id, out var oldHost);
            Updated();
        }
    }
}
