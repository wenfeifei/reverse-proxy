// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.ReverseProxy.Abstractions.Telemetry
{
    /// <summary>
    /// Provides contextual information for an ongoing operation.
    /// Operation contexts support nesting, and the current context
    /// can be obtained from <see cref="IOperationLogger{TCategoryName}.Context"/>.
    /// </summary>
    public interface IOperationContext
    {
        /// <summary>
        /// Sets a property on the current operation context.
        /// </summary>
        /// <param name="key">Property key.</param>
        /// <param name="value">Property value.</param>
        void SetProperty(string key, string value);
    }
}
