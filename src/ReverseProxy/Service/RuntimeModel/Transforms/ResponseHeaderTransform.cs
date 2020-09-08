// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Microsoft.ReverseProxy.Service.RuntimeModel.Transforms
{
    /// <summary>
    /// Transforms for response headers and trailers.
    /// </summary>
    public abstract class ResponseHeaderTransform
    {
        /// <summary>
        /// Transforms the given response header value and returns the result.
        /// </summary>
        /// <param name="context">The current request context.</param>
        /// <param name="response">The proxied response.</param>
        /// <param name="values">The header value to transform.</param>
        /// <returns>The transformed value.</returns>
        public abstract StringValues Apply(HttpContext context, HttpResponseMessage response, StringValues values);
    }
}
