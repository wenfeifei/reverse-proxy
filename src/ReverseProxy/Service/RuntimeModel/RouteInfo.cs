// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.ReverseProxy.Utilities;
using Microsoft.ReverseProxy.Signals;
using System;

namespace Microsoft.ReverseProxy.RuntimeModel
{
    /// <summary>
    /// Representation of a route for use at runtime.
    /// </summary>
    /// <remarks>
    /// Note that while this class is immutable, specific members such as
    /// <see cref="Config"/> use <see cref="Signal{T}"/> to hold mutable references
    /// that can be updated atomically and which will always have latest information.
    /// All members are thread safe.
    /// </remarks>
    internal sealed class RouteInfo
    {
        public RouteInfo(string routeId)
        {
            if (string.IsNullOrEmpty(routeId))
            {
                throw new ArgumentNullException(nameof(routeId));
            }
            RouteId = routeId;
        }

        public string RouteId { get; }

        /// <summary>
        /// Encapsulates parts of a route that can change atomically
        /// in reaction to config changes.
        /// </summary>
        public Signal<RouteConfig> Config { get; } = SignalFactory.Default.CreateSignal<RouteConfig>();
    }
}
