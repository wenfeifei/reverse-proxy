// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.ReverseProxy.Abstractions.Telemetry;
using Microsoft.ReverseProxy.RuntimeModel;
using Microsoft.ReverseProxy.Service.Management;
using Microsoft.ReverseProxy.Service.Proxy;
using Microsoft.ReverseProxy.Service.Proxy.Infrastructure;
using Microsoft.ReverseProxy.Service.RuntimeModel.Transforms;
using Microsoft.ReverseProxy.Telemetry;
using Moq;
using Tests.Common;
using Xunit;

namespace Microsoft.ReverseProxy.Middleware.Tests
{
    public class ProxyInvokerMiddlewareTests : TestAutoMockBase
    {
        public ProxyInvokerMiddlewareTests()
        {
            Provide<IOperationLogger<ProxyInvokerMiddleware>, TextOperationLogger<ProxyInvokerMiddleware>>();
        }

        [Fact]
        public void Constructor_Works()
        {
            Create<ProxyInvokerMiddleware>();
        }

        [Fact]
        public async Task Invoke_Works()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Method = "GET";
            httpContext.Request.Scheme = "https";
            httpContext.Request.Host = new HostString("example.com");
            httpContext.Request.Path = "/api/test";
            httpContext.Request.QueryString = new QueryString("?a=b&c=d");

            var httpClient = new HttpMessageInvoker(new Mock<HttpMessageHandler>().Object);
            var cluster1 = new ClusterInfo(
                clusterId: "cluster1",
                destinationManager: new DestinationManager());
            var clusterConfig = new ClusterConfig(default, default, default, httpClient, default, new Dictionary<string, string>());
            var destination1 = cluster1.DestinationManager.GetOrCreateItem(
                "destination1",
                destination =>
                {
                    destination.ConfigSignal.Value = new DestinationConfig("https://localhost:123/a/b/");
                    destination.DynamicStateSignal.Value = new DestinationDynamicState(DestinationHealth.Healthy);
                });
            httpContext.Features.Set<IReverseProxyFeature>(
                new ReverseProxyFeature() { AvailableDestinations = new List<DestinationInfo>() { destination1 }.AsReadOnly(), ClusterConfig = clusterConfig });
            httpContext.Features.Set(cluster1);

            var aspNetCoreEndpoints = new List<Endpoint>();
            var routeConfig = new RouteConfig(
                route: new RouteInfo("route1"),
                configHash: 0,
                order: null,
                cluster: cluster1,
                aspNetCoreEndpoints: aspNetCoreEndpoints.AsReadOnly(),
                transforms: null);
            var aspNetCoreEndpoint = CreateAspNetCoreEndpoint(routeConfig);
            aspNetCoreEndpoints.Add(aspNetCoreEndpoint);
            httpContext.SetEndpoint(aspNetCoreEndpoint);

            var tcs1 = new TaskCompletionSource<bool>();
            var tcs2 = new TaskCompletionSource<bool>();
            Mock<IHttpProxy>()
                .Setup(h => h.ProxyAsync(
                    httpContext,
                    It.Is<string>(uri => uri == "https://localhost:123/a/b/"),
                    It.IsAny<Transforms>(),
                    httpClient,
                    It.Is<ProxyTelemetryContext>(ctx => ctx.ClusterId == "cluster1" && ctx.RouteId == "route1" && ctx.DestinationId == "destination1"),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<CancellationToken>()))
                .Returns(
                    async () =>
                    {
                        tcs1.TrySetResult(true);
                        await tcs2.Task;
                    })
                .Verifiable();

            var sut = Create<ProxyInvokerMiddleware>();

            // Act
            Assert.Equal(0, cluster1.ConcurrencyCounter.Value);
            Assert.Equal(0, destination1.ConcurrencyCounter.Value);

            var task = sut.Invoke(httpContext);
            if (task.IsFaulted)
            {
                // Something went wrong, don't hang the test.
                await task;
            }
            await tcs1.Task; // Wait until we get to the proxying step.
            Assert.Equal(1, cluster1.ConcurrencyCounter.Value);
            Assert.Equal(1, destination1.ConcurrencyCounter.Value);

            tcs2.TrySetResult(true);
            await task;
            Assert.Equal(0, cluster1.ConcurrencyCounter.Value);
            Assert.Equal(0, destination1.ConcurrencyCounter.Value);

            // Assert
            Mock<IHttpProxy>().Verify();
        }

        private static Endpoint CreateAspNetCoreEndpoint(RouteConfig routeConfig)
        {
            var endpointBuilder = new RouteEndpointBuilder(
                requestDelegate: httpContext => Task.CompletedTask,
                routePattern: RoutePatternFactory.Parse("/"),
                order: 0);
            endpointBuilder.Metadata.Add(routeConfig);
            return endpointBuilder.Build();
        }
    }
}
