using Fanex.Athena.IdentityServer.Core.Helpers;
using Fanex.Athena.IdentityServer.Core.Services;
using Fanex.Athena.IdentityServer.Core.Settings;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Fanex.Athena.IdentityServer.Core.BackgroundServices
{
    public class TokenCleanupBackgroundService : BackgroundService
    {
        private readonly ITokenCleanupService _tokenCleanupService;
        private readonly OperationalStoreSetting _options;
        private readonly ILogger<TokenCleanupBackgroundService> _logger;

        private TimeSpan CleanupInterval => TimeSpan.FromSeconds(_options.TokenCleanupInterval);

        private bool EnableTokenCleanup => _options.EnableTokenCleanup &&
            (string.IsNullOrEmpty(_options.CleanupTokenOnInstance) ||
               string.Equals(Dns.GetHostName(), _options.CleanupTokenOnInstance, StringComparison.OrdinalIgnoreCase));

        private CancellationTokenSource _source;

        public TokenCleanupBackgroundService(ITokenCleanupService tokenCleanupService, OperationalStoreSetting options, ILogger<TokenCleanupBackgroundService> logger)
        {
            _tokenCleanupService = tokenCleanupService;
            _options = options;
            _logger = logger;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (EnableTokenCleanup)
            {
                if (_source != null)
                {
                    throw new InvalidOperationException("Already started. Call Stop first.");
                }

                _logger.LogWarning("Starting token removal background service");

                _source = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);

                Task.Factory.StartNew(() => DoWork(_source.Token));
            }
            else
            {
                _logger.LogWarning("Skipping token removal background service");
            }

            return Task.CompletedTask;
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            if (EnableTokenCleanup)
            {
                if (_source == null)
                {
                    throw new InvalidOperationException("Not started. Call Start first.");
                }

                _logger.LogWarning("Stopping token removal background service");

                _source.Cancel();
                _source = null;
            }

            await base.StopAsync(cancellationToken);
        }

        private async Task DoWork(CancellationToken stoppingToken)
        {
            while (true)
            {
                if (stoppingToken.IsCancellationRequested)
                {
                    _logger.LogDebug("CancellationRequested. Exiting.");
                    return;
                }

                try
                {
                    await Task.Delay(CleanupInterval, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    _logger.LogDebug("TaskCanceledException. Exiting.");
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError("Task.Delay exception: {0}. Exiting.", ex.Message);
                    return;
                }

                if (stoppingToken.IsCancellationRequested)
                {
                    _logger.LogDebug("CancellationRequested. Exiting.");
                    return;
                }

                await RemoveExpiredGrantsAsync();
            }
        }

        private async Task RemoveExpiredGrantsAsync()
        {
            try
            {
                await _tokenCleanupService.CleanupExpiredTokenAsync();
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, ErrorMessageHelper.GenerateMessage(ex));
            }
        }
    }

}
