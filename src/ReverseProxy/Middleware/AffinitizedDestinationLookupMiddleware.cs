// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.ReverseProxy.Abstractions.ClusterDiscovery.Contract;
using Microsoft.ReverseProxy.RuntimeModel;
using Microsoft.ReverseProxy.Service.SessionAffinity;

namespace Microsoft.ReverseProxy.Middleware
{
    /// <summary>
    /// Looks up an affinitized <see cref="DestinationInfo"/> matching the request's affinity key if any is set
    /// </summary>
    internal class AffinitizedDestinationLookupMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IDictionary<string, ISessionAffinityProvider> _sessionAffinityProviders;
        private readonly IDictionary<string, IAffinityFailurePolicy> _affinityFailurePolicies;
        private readonly ILogger _logger;

        public AffinitizedDestinationLookupMiddleware(
            RequestDelegate next,
            IEnumerable<ISessionAffinityProvider> sessionAffinityProviders,
            IEnumerable<IAffinityFailurePolicy> affinityFailurePolicies,
            ILogger<AffinitizedDestinationLookupMiddleware> logger)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _sessionAffinityProviders = sessionAffinityProviders.ToProviderDictionary();
            _affinityFailurePolicies = affinityFailurePolicies?.ToPolicyDictionary() ?? throw new ArgumentNullException(nameof(affinityFailurePolicies));
        }

        public Task Invoke(HttpContext context)
        {
            var proxyFeature = context.GetRequiredProxyFeature();

            var options = proxyFeature.ClusterConfig.SessionAffinityOptions;

            if (!options.Enabled)
            {
                return _next(context);
            }

            var cluster = context.GetRequiredCluster();
            return InvokeInternal(context, proxyFeature, options, cluster.ClusterId);
        }

        private async Task InvokeInternal(HttpContext context, IReverseProxyFeature proxyFeature, ClusterConfig.ClusterSessionAffinityOptions options, string clusterId)
        {
            var destinations = proxyFeature.AvailableDestinations;

            var currentProvider = _sessionAffinityProviders.GetRequiredServiceById(options.Mode, SessionAffinityConstants.Modes.Cookie);
            var affinityResult = currentProvider.FindAffinitizedDestinations(context, destinations, clusterId, options);

            switch (affinityResult.Status)
            {
                case AffinityStatus.OK:
                    proxyFeature.AvailableDestinations = affinityResult.Destinations;
                    break;
                case AffinityStatus.AffinityKeyNotSet:
                    // Nothing to do so just continue processing
                    break;
                case AffinityStatus.AffinityKeyExtractionFailed:
                case AffinityStatus.DestinationNotFound:

                    var failurePolicy = _affinityFailurePolicies.GetRequiredServiceById(options.FailurePolicy, SessionAffinityConstants.AffinityFailurePolicies.Redistribute);
                    var keepProcessing = await failurePolicy.Handle(context, options, affinityResult.Status);

                    if (!keepProcessing)
                    {
                        // Policy reported the failure is unrecoverable and took the full responsibility for its handling,
                        // so we simply stop processing.
                        Log.AffinityResolutionFailedForCluster(_logger, clusterId);
                        return;
                    }

                    Log.AffinityResolutionFailureWasHandledProcessingWillBeContinued(_logger, clusterId, options.FailurePolicy);

                    break;
                default:
                    throw new NotSupportedException($"Affinity status '{affinityResult.Status}' is not supported.");
            }

            await _next(context);
        }

        private static class Log
        {
            private static readonly Action<ILogger, string, Exception> _affinityResolutionFailedForCluster = LoggerMessage.Define<string>(
                LogLevel.Warning,
                EventIds.AffinityResolutionFailedForCluster,
                "Affinity resolution failed for cluster '{clusterId}'.");

            private static readonly Action<ILogger, string, string, Exception> _affinityResolutionFailureWasHandledProcessingWillBeContinued = LoggerMessage.Define<string, string>(
                LogLevel.Debug,
                EventIds.AffinityResolutionFailureWasHandledProcessingWillBeContinued,
                "Affinity resolution failure for cluster '{clusterId}' was handled successfully by the policy '{policyName}'. Request processing will be continued.");

            public static void AffinityResolutionFailedForCluster(ILogger logger, string clusterId)
            {
                _affinityResolutionFailedForCluster(logger, clusterId, null);
            }

            public static void AffinityResolutionFailureWasHandledProcessingWillBeContinued(ILogger logger, string clusterId, string policyName)
            {
                _affinityResolutionFailureWasHandledProcessingWillBeContinued(logger, clusterId, policyName, null);
            }
        }
    }
}
