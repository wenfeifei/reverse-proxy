// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace Microsoft.ReverseProxy.Configuration.Contract
{
    /// <summary>
    /// Session affinitity options.
    /// </summary>
    public sealed class SessionAffinityData
    {
        /// <summary>
        /// Indicates whether session affinity is enabled.
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Session affinity mode which is implemented by one of providers.
        /// </summary>
        public string Mode { get; set; }

        /// <summary>
        /// Strategy handling missing destination for an affinitized request.
        /// </summary>
        public string FailurePolicy { get; set; }

        /// <summary>
        /// Key-value pair collection holding extra settings specific to different affinity modes.
        /// </summary>
        public IDictionary<string, string> Settings { get; set; }
    }
}
