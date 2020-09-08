// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;

namespace Microsoft.ReverseProxy.Service.Proxy.Infrastructure
{
    /// <summary>
    /// Default implementation of <see cref="IProxyHttpClientFactory"/>.
    /// </summary>
    internal class ProxyHttpClientFactory : IProxyHttpClientFactory
    {
        private readonly ILogger<ProxyHttpClientFactory> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProxyHttpClientFactory"/> class.
        /// </summary>
        public ProxyHttpClientFactory(ILogger<ProxyHttpClientFactory> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public HttpMessageInvoker CreateClient(ProxyHttpClientContext context)
        {
            if (CanReuseOldClient(context))
            {
                Log.ProxyClientReused(_logger, context.ClusterId);
                return context.OldClient;
            }

            var newClientOptions = context.NewOptions;
            var handler = new SocketsHttpHandler
            {
                UseProxy = false,
                AllowAutoRedirect = false,
                AutomaticDecompression = DecompressionMethods.None,
                UseCookies = false

                // NOTE: MaxResponseHeadersLength = 64, which means up to 64 KB of headers are allowed by default as of .NET Core 3.1.
            };

            if (newClientOptions.SslProtocols.HasValue)
            {
                handler.SslOptions.EnabledSslProtocols = newClientOptions.SslProtocols.Value;
            }
            if (newClientOptions.ClientCertificate != null)
            {
                handler.SslOptions.ClientCertificates = new X509CertificateCollection
                {
                    newClientOptions.ClientCertificate
                };
            }
            if (newClientOptions.MaxConnectionsPerServer != null)
            {
                handler.MaxConnectionsPerServer = newClientOptions.MaxConnectionsPerServer.Value;
            }
            if (newClientOptions.DangerousAcceptAnyServerCertificate)
            {
                handler.SslOptions.RemoteCertificateValidationCallback = delegate { return true; };
            }

            Log.ProxyClientCreated(_logger, context.ClusterId);
            return new HttpMessageInvoker(handler, disposeHandler: true);
        }

        private bool CanReuseOldClient(ProxyHttpClientContext context)
        {
            return context.OldClient != null && context.NewOptions == context.OldOptions;
        }

        private static class Log
        {
            private static readonly Action<ILogger, string, Exception> _proxyClientCreated = LoggerMessage.Define<string>(
                  LogLevel.Debug,
                  EventIds.ProxyClientCreated,
                  "New proxy client created for cluster '{clusterId}'.");

            private static readonly Action<ILogger, string, Exception> _proxyClientReused = LoggerMessage.Define<string>(
                LogLevel.Debug,
                EventIds.ProxyClientReused,
                "Existing proxy client reused for cluster '{clusterId}'.");

            public static void ProxyClientCreated(ILogger logger, string clusterId)
            {
                _proxyClientCreated(logger, clusterId, null);
            }

            public static void ProxyClientReused(ILogger logger, string clusterId)
            {
                _proxyClientReused(logger, clusterId, null);
            }
        }
    }
}
