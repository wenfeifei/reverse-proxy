// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Net.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.ReverseProxy.RuntimeModel;

namespace Microsoft.ReverseProxy.Service.SessionAffinity
{
    /// <summary>
    /// Provides session affinity for load-balanced clusters.
    /// </summary>
    public interface ISessionAffinityProvider
    {
        /// <summary>
        ///  A unique identifier for this session affinity implementation. This will be referenced from config.
        /// </summary>
        public string Mode { get; }

        /// <summary>
        /// Finds <see cref="DestinationInfo"/> to which the current request is affinitized by the affinity key.
        /// </summary>
        /// <param name="context">Current request's context.</param>
        /// <param name="destinations"><see cref="DestinationInfo"/>s available for the request.</param>
        /// <param name="clusterId">Target cluster ID.</param>
        /// <param name="options">Affinity options.</param>
        /// <returns><see cref="AffinityResult"/> carrying the found affinitized destinations if any and the <see cref="AffinityStatus"/>.</returns>
        public AffinityResult FindAffinitizedDestinations(HttpContext context, IReadOnlyList<DestinationInfo> destinations, string clusterId, in ClusterConfig.ClusterSessionAffinityOptions options);

        /// <summary>
        /// Affinitize the current request to the given <see cref="DestinationInfo"/> by setting the affinity key extracted from <see cref="DestinationInfo"/>.
        /// </summary>
        /// <param name="context">Current request's context.</param>
        /// <param name="options">Affinity options.</param>
        /// <param name="destination"><see cref="DestinationInfo"/> to which request is to be affinitized.</param>
        public void AffinitizeRequest(HttpContext context, in ClusterConfig.ClusterSessionAffinityOptions options, DestinationInfo destination);
    }
}
