// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.ReverseProxy.RuntimeModel;

namespace Microsoft.ReverseProxy.Service.SessionAffinity
{
    /// <summary>
    /// Affinity resolution result.
    /// </summary>
    public readonly struct AffinityResult
    {
        public IReadOnlyList<DestinationInfo> Destinations { get; }

        public AffinityStatus Status { get; }

        public AffinityResult(IReadOnlyList<DestinationInfo> destinations, AffinityStatus status)
        {
            Destinations = destinations;
            Status = status;
        }
    }
}
