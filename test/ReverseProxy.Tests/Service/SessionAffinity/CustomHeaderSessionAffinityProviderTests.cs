// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.ReverseProxy.Abstractions.ClusterDiscovery.Contract;
using Microsoft.ReverseProxy.RuntimeModel;
using Xunit;

namespace Microsoft.ReverseProxy.Service.SessionAffinity
{
    public class CustomHeaderSessionAffinityProviderTests
    {
        private const string AffinityHeaderName = "X-MyAffinity";
        private readonly ClusterConfig.ClusterSessionAffinityOptions _defaultOptions =
            new ClusterConfig.ClusterSessionAffinityOptions(true, "Cookie", "Return503", new Dictionary<string, string> { { "CustomHeaderName", AffinityHeaderName } });
        private readonly IReadOnlyList<DestinationInfo> _destinations = new[] { new DestinationInfo("dest-A"), new DestinationInfo("dest-B"), new DestinationInfo("dest-C") };

        [Fact]
        public void FindAffinitizedDestination_AffinityKeyIsNotSetOnRequest_ReturnKeyNotSet()
        {
            var provider = new CustomHeaderSessionAffinityProvider(AffinityTestHelper.GetDataProtector().Object, AffinityTestHelper.GetLogger<CustomHeaderSessionAffinityProvider>().Object);

            Assert.Equal(SessionAffinityConstants.Modes.CustomHeader, provider.Mode);

            var context = new DefaultHttpContext();
            context.Request.Headers["SomeHeader"] = new[] { "SomeValue" };

            var affinityResult = provider.FindAffinitizedDestinations(context, _destinations, "cluster-1", _defaultOptions);

            Assert.Equal(AffinityStatus.AffinityKeyNotSet, affinityResult.Status);
            Assert.Null(affinityResult.Destinations);
        }

        [Fact]
        public void FindAffinitizedDestination_AffinityKeyIsSetOnRequest_Success()
        {
            var provider = new CustomHeaderSessionAffinityProvider(AffinityTestHelper.GetDataProtector().Object, AffinityTestHelper.GetLogger<CustomHeaderSessionAffinityProvider>().Object);
            var context = new DefaultHttpContext();
            context.Request.Headers["SomeHeader"] = new[] { "SomeValue" };
            var affinitizedDestination = _destinations[1];
            context.Request.Headers[AffinityHeaderName] = new[] { affinitizedDestination.DestinationId.ToUTF8BytesInBase64() };

            var affinityResult = provider.FindAffinitizedDestinations(context, _destinations, "cluster-1", _defaultOptions);

            Assert.Equal(AffinityStatus.OK, affinityResult.Status);
            Assert.Equal(1, affinityResult.Destinations.Count);
            Assert.Same(affinitizedDestination, affinityResult.Destinations[0]);
        }

        [Theory]
        [MemberData(nameof(FindAffinitizedDestination_CustomHeaderNameIsNotSpecified_Cases))]
        public void FindAffinitizedDestination_CustomHeaderNameIsNotSpecified_UseDefaultName(Dictionary<string, string> settings)
        {
            var options = new ClusterConfig.ClusterSessionAffinityOptions(true, "CustomHeader", "Return503", settings);
            var provider = new CustomHeaderSessionAffinityProvider(AffinityTestHelper.GetDataProtector().Object, AffinityTestHelper.GetLogger<CustomHeaderSessionAffinityProvider>().Object);
            var context = new DefaultHttpContext();
            var affinitizedDestination = _destinations[1];
            context.Request.Headers[CustomHeaderSessionAffinityProvider.DefaultCustomHeaderName] = new[] { affinitizedDestination.DestinationId.ToUTF8BytesInBase64() };

            var affinityResult = provider.FindAffinitizedDestinations(context, _destinations, "cluster-1", options);

            Assert.Equal(AffinityStatus.OK, affinityResult.Status);
            Assert.Equal(1, affinityResult.Destinations.Count);
            Assert.Same(affinitizedDestination, affinityResult.Destinations[0]);
        }

        [Fact]
        public void AffinitizedRequest_AffinityKeyIsNotExtracted_SetKeyOnResponse()
        {
            var provider = new CustomHeaderSessionAffinityProvider(AffinityTestHelper.GetDataProtector().Object, AffinityTestHelper.GetLogger<CustomHeaderSessionAffinityProvider>().Object);
            var context = new DefaultHttpContext();
            var chosenDestination = _destinations[1];
            var expectedAffinityHeaderValue = chosenDestination.DestinationId.ToUTF8BytesInBase64();

            provider.AffinitizeRequest(context, _defaultOptions, chosenDestination);

            Assert.True(context.Response.Headers.ContainsKey(AffinityHeaderName));
            Assert.Equal(expectedAffinityHeaderValue, context.Response.Headers[AffinityHeaderName]);
        }

        [Fact]
        public void AffinitizedRequest_AffinityKeyIsExtracted_DoNothing()
        {
            var provider = new CustomHeaderSessionAffinityProvider(AffinityTestHelper.GetDataProtector().Object, AffinityTestHelper.GetLogger<CustomHeaderSessionAffinityProvider>().Object);
            var context = new DefaultHttpContext();
            context.Request.Headers["SomeHeader"] = new[] { "SomeValue" };
            var affinitizedDestination = _destinations[1];
            context.Request.Headers[AffinityHeaderName] = new[] { affinitizedDestination.DestinationId.ToUTF8BytesInBase64() };

            var affinityResult = provider.FindAffinitizedDestinations(context, _destinations, "cluster-1", _defaultOptions);

            Assert.Equal(AffinityStatus.OK, affinityResult.Status);

            provider.AffinitizeRequest(context, _defaultOptions, affinitizedDestination);

            Assert.False(context.Response.Headers.ContainsKey(AffinityHeaderName));
        }

        public static IEnumerable<object[]> FindAffinitizedDestination_CustomHeaderNameIsNotSpecified_Cases()
        {
            yield return new object[] { null };
            yield return new object[] { new Dictionary<string, string> { { "SomeSetting", "SomeValue" } } };
        }
    }
}
