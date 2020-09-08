// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.ReverseProxy.Abstractions.Telemetry;
using Microsoft.ReverseProxy.Service.Proxy;
using Microsoft.ReverseProxy.Utilities;

namespace Microsoft.ReverseProxy.Middleware
{
    /// <summary>
    /// Invokes the proxy at the end of the request processing pipeline.
    /// </summary>
    internal class ProxyInvokerMiddleware
    {
        private readonly IRandomFactory _randomFactory;
        private readonly RequestDelegate _next; // Unused, this middleware is always terminal
        private readonly ILogger _logger;
        private readonly IOperationLogger<ProxyInvokerMiddleware> _operationLogger;
        private readonly IHttpProxy _httpProxy;

        public ProxyInvokerMiddleware(
            RequestDelegate next,
            ILogger<ProxyInvokerMiddleware> logger,
            IOperationLogger<ProxyInvokerMiddleware> operationLogger,
            IHttpProxy httpProxy,
            IRandomFactory randomFactory)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _operationLogger = operationLogger ?? throw new ArgumentNullException(nameof(operationLogger));
            _httpProxy = httpProxy ?? throw new ArgumentNullException(nameof(httpProxy));
            _randomFactory = randomFactory ?? throw new ArgumentNullException(nameof(randomFactory));
        }

        /// <inheritdoc/>
        public async Task Invoke(HttpContext context)
        {
            _ = context ?? throw new ArgumentNullException(nameof(context));

            var reverseProxyFeature = context.GetRequiredProxyFeature();
            var destinations = reverseProxyFeature.AvailableDestinations
                ?? throw new InvalidOperationException($"The {nameof(IReverseProxyFeature)} Destinations collection was not set.");

            var routeConfig = context.GetRequiredRouteConfig();
            var cluster = routeConfig.Cluster;

            if (destinations.Count == 0)
            {
                Log.NoAvailableDestinations(_logger, cluster.ClusterId);
                context.Response.StatusCode = 503;
                return;
            }

            var destination = destinations[0];
            if (destinations.Count > 1)
            {
                var random = _randomFactory.CreateRandomInstance();
                Log.MultipleDestinationsAvailable(_logger, cluster.ClusterId);
                destination = destinations[random.Next(destinations.Count)];
            }

            var destinationConfig = destination.Config;
            if (destinationConfig == null)
            {
                throw new InvalidOperationException($"Chosen destination has no configs set: '{destination.DestinationId}'");
            }

            using (var shortCts = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted))
            {
                // TODO: Configurable timeout, measure from request start, make it unit-testable
                shortCts.CancelAfter(TimeSpan.FromSeconds(30));

                // TODO: Retry against other destinations
                try
                {
                    // TODO: Apply caps
                    cluster.ConcurrencyCounter.Increment();
                    destination.ConcurrencyCounter.Increment();

                    // TODO: Duplex channels should not have a timeout (?), but must react to Proxy force-shutdown signals.
                    var longCancellation = context.RequestAborted;

                    var proxyTelemetryContext = new ProxyTelemetryContext(
                        clusterId: cluster.ClusterId,
                        routeId: routeConfig.Route.RouteId,
                        destinationId: destination.DestinationId);

                    await _operationLogger.ExecuteAsync(
                        "ReverseProxy.Proxy",
                        () => _httpProxy.ProxyAsync(context, destinationConfig.Address, routeConfig.Transforms, reverseProxyFeature.ClusterConfig.HttpClient, proxyTelemetryContext, shortCancellation: shortCts.Token, longCancellation: longCancellation));
                }
                finally
                {
                    destination.ConcurrencyCounter.Decrement();
                    cluster.ConcurrencyCounter.Decrement();
                }
            }
        }

        private static class Log
        {
            private static readonly Action<ILogger, string, Exception> _noAvailableDestinations = LoggerMessage.Define<string>(
                LogLevel.Warning,
                EventIds.NoAvailableDestinations,
                "No available destinations after load balancing for cluster '{clusterId}'.");

            private static readonly Action<ILogger, string, Exception> _multipleDestinationsAvailable = LoggerMessage.Define<string>(
                LogLevel.Warning,
                EventIds.MultipleDestinationsAvailable,
                "More than one destination available for cluster '{clusterId}', load balancing may not be configured correctly. Choosing randomly.");

            public static void NoAvailableDestinations(ILogger logger, string clusterId)
            {
                _noAvailableDestinations(logger, clusterId, null);
            }

            public static void MultipleDestinationsAvailable(ILogger logger, string clusterId)
            {
                _multipleDestinationsAvailable(logger, clusterId, null);
            }
        }
    }
}
