// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.ReverseProxy.Abstractions
{
    /// <summary>
    /// Describes a route that matches incoming requests based on a the <see cref="Match"/> criteria
    /// and proxies matching requests to the cluster identified by its <see cref="ClusterId"/>.
    /// </summary>
    public sealed class ProxyRoute : IDeepCloneable<ProxyRoute>
    {
        /// <summary>
        /// Globally unique identifier of the route.
        /// </summary>
        public string RouteId { get; set; }

        public ProxyMatch Match { get; private set; } = new ProxyMatch();

        /// <summary>
        /// Optionally, an order value for this route. Routes with lower numbers take precedence over higher numbers.
        /// </summary>
        public int? Order { get; set; }

        /// <summary>
        /// Gets or sets the cluster that requests matching this route
        /// should be proxied to.
        /// </summary>
        public string ClusterId { get; set; }

        /// <summary>
        /// The name of the AuthorizationPolicy to apply to this route.
        /// If not set then only the FallbackPolicy will apply.
        /// Set to "Default" to enable authorization with the applications default policy.
        /// </summary>
        public string AuthorizationPolicy { get; set; }

        /// <summary>
        /// The name of the CorsPolicy to apply to this route.
        /// If not set then the route won't be automatically matched for cors preflight requests.
        /// Set to "Default" to enable cors with the default policy.
        /// Set to "Disable" to refuses cors requests for this route.
        /// </summary>
        public string CorsPolicy { get; set; }

        /// <summary>
        /// Arbitrary key-value pairs that further describe this route.
        /// </summary>
        public IDictionary<string, string> Metadata { get; set; }

        /// <summary>
        /// Parameters used to transform the request and response. See <see cref="Service.ITransformBuilder"/>.
        /// </summary>
        public IList<IDictionary<string, string>> Transforms { get; set; }

        /// <inheritdoc/>
        ProxyRoute IDeepCloneable<ProxyRoute>.DeepClone()
        {
            return new ProxyRoute
            {
                RouteId = RouteId,
                Match = Match.DeepClone(),
                Order = Order,
                ClusterId = ClusterId,
                AuthorizationPolicy = AuthorizationPolicy,
                CorsPolicy = CorsPolicy,
                Metadata = Metadata?.DeepClone(StringComparer.OrdinalIgnoreCase),
                Transforms = Transforms?.Select(d => new Dictionary<string, string>(d, StringComparer.OrdinalIgnoreCase)).ToList<IDictionary<string, string>>(),
            };
        }

        // Used to diff for config changes
        internal int GetConfigHash()
        {
            var hash = 0;

            if (!string.IsNullOrEmpty(RouteId))
            {
                hash ^= RouteId.GetHashCode();
            }

            if (Match.Methods != null && Match.Methods.Count > 0)
            {
                // Assumes un-ordered
                hash ^= Match.Methods.Select(item => item.GetHashCode())
                    .Aggregate((total, nextCode) => total ^ nextCode);
            }

            if (Match.Hosts != null && Match.Hosts.Count > 0)
            {
                // Assumes un-ordered
                hash ^= Match.Hosts.Select(item => item.GetHashCode())
                    .Aggregate((total, nextCode) => total ^ nextCode);
            }

            if (!string.IsNullOrEmpty(Match.Path))
            {
                hash ^= Match.Path.GetHashCode();
            }

            if (Order.HasValue)
            {
                hash ^= Order.GetHashCode();
            }

            if (!string.IsNullOrEmpty(ClusterId))
            {
                hash ^= ClusterId.GetHashCode();
            }

            if (!string.IsNullOrEmpty(AuthorizationPolicy))
            {
                hash ^= AuthorizationPolicy.GetHashCode();
            }

            if (!string.IsNullOrEmpty(CorsPolicy))
            {
                hash ^= CorsPolicy.GetHashCode();
            }

            if (Metadata != null)
            {
                hash ^= Metadata.Select(item => HashCode.Combine(item.Key.GetHashCode(), item.Value.GetHashCode()))
                    .Aggregate((total, nextCode) => total ^ nextCode);
            }

            if (Transforms != null)
            {
                hash ^= Transforms.Select(transform =>
                    transform.Select(item => HashCode.Combine(item.Key.GetHashCode(), item.Value.GetHashCode()))
                        .Aggregate((total, nextCode) => total ^ nextCode)) // Unordered Dictionary
                    .Aggregate(seed: 397, (total, nextCode) => total * 31 ^ nextCode); // Ordered List
            }

            return hash;
        }
    }
}
