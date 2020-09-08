// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace Microsoft.ReverseProxy.Configuration.Contract
{
    /// <summary>
    /// A cluster is a group of equivalent endpoints and associated policies.
    /// A route maps requests to a cluster, and Reverse Proxy handles that request
    /// by proxying to any endpoint within the matching cluster,
    /// honoring load balancing and partitioning policies when applicable.
    /// </summary>
    public sealed class ClusterData
    {
        /// <summary>
        /// Circuit breaker options.
        /// </summary>
        public CircuitBreakerData CircuitBreaker { get; set; }

        /// <summary>
        /// Quota options.
        /// </summary>
        public QuotaData Quota { get; set; }

        /// <summary>
        /// Partitioning options.
        /// </summary>
        public ClusterPartitioningData Partitioning { get; set; }

        /// <summary>
        /// Load balancing options.
        /// </summary>
        public LoadBalancingData LoadBalancing { get; set; }

        /// <summary>
        /// Session affinity options.
        /// </summary>
        public SessionAffinityData SessionAffinity { get; set; }

        /// <summary>
        /// Active health checking options.
        /// </summary>
        public HealthCheckData HealthCheck { get; set; }

        /// <summary>
        /// Options of an HTTP client that is used to call this cluster.
        /// </summary>
        public ProxyHttpClientData HttpClient { get; set; }

        /// <summary>
        /// The set of destinations associated with this cluster.
        /// </summary>
        public IDictionary<string, DestinationData> Destinations { get; private set; } = new Dictionary<string, DestinationData>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Arbitrary key-value pairs that further describe this cluster.
        /// </summary>
        public IDictionary<string, string> Metadata { get; set; }
    }
}
