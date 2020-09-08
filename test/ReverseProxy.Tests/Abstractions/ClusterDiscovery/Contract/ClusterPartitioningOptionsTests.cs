// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Xunit;

namespace Microsoft.ReverseProxy.Abstractions.Tests
{
    public class ClusterPartitioningOptionsTests
    {
        [Fact]
        public void Constructor_Works()
        {
            new ClusterPartitioningOptions();
        }

        [Fact]
        public void DeepClone_Works()
        {
            // Arrange
            var sut = new ClusterPartitioningOptions
            {
                PartitionCount = 10,
                PartitionKeyExtractor = "Header('x-ms-org-id')",
                PartitioningAlgorithm = "alg1",
            };

            // Act
            var clone = sut.DeepClone();

            // Assert
            Assert.NotSame(sut, clone);
            Assert.Equal(sut.PartitionCount, clone.PartitionCount);
            Assert.Equal(sut.PartitionKeyExtractor, clone.PartitionKeyExtractor);
            Assert.Equal(sut.PartitioningAlgorithm, clone.PartitioningAlgorithm);
        }
    }
}
