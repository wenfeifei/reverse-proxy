// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.ReverseProxy.Service.SessionAffinity;
using Microsoft.ReverseProxy.Signals;
using Moq;
using Xunit;

namespace Microsoft.ReverseProxy.Middleware
{
    public class AffinitizedDestinationLookupMiddlewareTests : AffinityMiddlewareTestBase
    {
        [Theory]
        [InlineData(AffinityStatus.AffinityKeyNotSet, null)]
        [InlineData(AffinityStatus.OK, AffinitizedDestinationName)]
        public async Task Invoke_SuccessfulFlow_CallNext(AffinityStatus status, string foundDestinationId)
        {
            var cluster = GetCluster();
            var endpoint = GetEndpoint(cluster);
            var foundDestinations = foundDestinationId != null ? Destinations.Where(d => d.DestinationId == foundDestinationId).ToArray() : null;
            var invokedMode = string.Empty;
            const string expectedMode = "Mode-B";
            var providers = RegisterAffinityProviders(
                true,
                Destinations,
                cluster.ClusterId,
                ("Mode-A", AffinityStatus.DestinationNotFound, (RuntimeModel.DestinationInfo[])null, (Action<ISessionAffinityProvider>)(p => throw new InvalidOperationException($"Provider {p.Mode} call is not expected."))),
                (expectedMode, status, foundDestinations, p => invokedMode = p.Mode));
            var nextInvoked = false;
            var middleware = new AffinitizedDestinationLookupMiddleware(c => {
                    nextInvoked = true;
                    return Task.CompletedTask;
                },
                providers.Select(p => p.Object), new IAffinityFailurePolicy[0],
                new Mock<ILogger<AffinitizedDestinationLookupMiddleware>>().Object);
            var context = new DefaultHttpContext();
            context.SetEndpoint(endpoint);
            var destinationFeature = GetDestinationsFeature(Destinations, cluster.Config.Value);
            context.Features.Set(destinationFeature);

            await middleware.Invoke(context);

            Assert.Equal(expectedMode, invokedMode);
            Assert.True(nextInvoked);
            providers[0].VerifyGet(p => p.Mode, Times.Once);
            providers[0].VerifyNoOtherCalls();
            providers[1].VerifyAll();

            if (foundDestinationId != null)
            {
                Assert.Equal(1, destinationFeature.AvailableDestinations.Count);
                Assert.Equal(foundDestinationId, destinationFeature.AvailableDestinations[0].DestinationId);
            }
            else
            {
                Assert.Same(Destinations, destinationFeature.AvailableDestinations);
            }
        }

        [Theory]
        [InlineData(AffinityStatus.DestinationNotFound, true)]
        [InlineData(AffinityStatus.DestinationNotFound, false)]
        [InlineData(AffinityStatus.AffinityKeyExtractionFailed, true)]
        [InlineData(AffinityStatus.AffinityKeyExtractionFailed, false)]
        public async Task Invoke_ErrorFlow_CallFailurePolicy(AffinityStatus affinityStatus, bool keepProcessing)
        {
            var cluster = GetCluster();
            var endpoint = GetEndpoint(cluster);
            var providers = RegisterAffinityProviders(true, Destinations, cluster.ClusterId, ("Mode-B", affinityStatus, null, _ => { }));
            var invokedPolicy = string.Empty;
            const string expectedPolicy = "Policy-1";
            var failurePolicies = RegisterFailurePolicies(
                affinityStatus,
                ("Policy-0", false, p => throw new InvalidOperationException($"Policy {p.Name} call is not expected.")),
                (expectedPolicy, keepProcessing, p => invokedPolicy = p.Name));
            var nextInvoked = false;
            var logger = AffinityTestHelper.GetLogger<AffinitizedDestinationLookupMiddleware>();
            var middleware = new AffinitizedDestinationLookupMiddleware(c => {
                    nextInvoked = true;
                    return Task.CompletedTask;
                },
                providers.Select(p => p.Object), failurePolicies.Select(p => p.Object),
                logger.Object);
            var context = new DefaultHttpContext();
            var destinationFeature = GetDestinationsFeature(Destinations, cluster.Config.Value);

            context.SetEndpoint(endpoint);
            context.Features.Set(destinationFeature);

            await middleware.Invoke(context);

            Assert.Equal(expectedPolicy, invokedPolicy);
            Assert.Equal(keepProcessing, nextInvoked);
            failurePolicies[0].VerifyGet(p => p.Name, Times.Once);
            failurePolicies[0].VerifyNoOtherCalls();
            failurePolicies[1].VerifyAll();
            if (!keepProcessing)
            {
                logger.Verify(
                    l => l.Log(LogLevel.Warning, EventIds.AffinityResolutionFailedForCluster, It.IsAny<It.IsAnyType>(), null, (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
                    Times.Once);
            }
        }
    }
}
