// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.ReverseProxy.Signals
{
    /// <summary>
    /// Represents a signal whose value can be read at any time, and
    /// which can interoperate with any other signals belonging to the same
    /// <see cref="SignalContext"/>.
    /// </summary>
    /// <typeparam name="T">Type of the stored value.</typeparam>
    internal interface IReadableSignal<out T> : ISignal
    {
        /// <summary>
        /// Gets the current value of this signal.
        /// </summary>
        T Value { get; }

        /// <summary>
        /// Gets a snapshot that represents the current state of the signal
        /// and provides facilities to subscribe and unsubscribe to changes.
        /// </summary>
        ISignalSnapshot<T> GetSnapshot();
    }
}
