// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Microsoft.ReverseProxy.Service.RuntimeModel.Transforms
{
    /// <summary>
    /// Base64 encodes the client certificate (if any) and sets it as the header value.
    /// </summary>
    internal class RequestHeaderClientCertTransform : RequestHeaderTransform
    {
        public override StringValues Apply(HttpContext context, StringValues values)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var clientCert = context.Connection.ClientCertificate;
            if (clientCert == null)
            {
                return StringValues.Empty;
            }

            return Convert.ToBase64String(clientCert.RawData);
        }
    }
}
