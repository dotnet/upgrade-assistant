// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac.Extras.Moq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Moq;
using Xunit;

namespace Microsoft.DotNet.UpgradeAssistant.Abstractions.Internal.Tests
{
    public class OrderingExtensionsTests
    {
        [Fact]
        public void NoItems()
        {
            Assert.Empty(Enumerable.Empty<ITest>().OrderyByPrecedence());
        }

        [Fact]
        public void SingleItem()
        {
            // Arrange
            var items = new ITest[] { new TestNoOrder1() };

            // Act
            var result = items.OrderyByPrecedence();

            // Assert
            Assert.Collection(result, r => Assert.IsType<TestNoOrder1>(r));
        }

        [Fact]
        public void ThreeItems()
        {
            // Arrange
            var items = new ITest[]
            {
                new TestNoOrder1(),
                new TestNoOrder2(),
                new TestNoOrder3(),
            };

            // Act
            var result = items.OrderyByPrecedence();

            // Assert
            Assert.Collection(result,
                r => Assert.IsType<TestNoOrder1>(r),
                r => Assert.IsType<TestNoOrder2>(r),
                r => Assert.IsType<TestNoOrder3>(r));
        }

        [Fact]
        public void ThreeItemsWithOneOrderedValueHigh()
        {
            // Arrange
            var items = new ITest[]
            {
                new TestOrderMaxValue(),
                new TestNoOrder1(),
                new TestNoOrder2(),
                new TestNoOrder3(),
            };

            // Act
            var result = items.OrderyByPrecedence();

            // Assert
            Assert.Collection(result,
                r => Assert.IsType<TestNoOrder1>(r),
                r => Assert.IsType<TestNoOrder2>(r),
                r => Assert.IsType<TestNoOrder3>(r),
                r => Assert.IsType<TestOrderMaxValue>(r));
        }

        [Fact]
        public void ThreeItemsWithOneOrderedValueHighStartMiddle()
        {
            // Arrange
            var items = new ITest[]
            {
                new TestNoOrder1(),
                new TestNoOrder2(),
                new TestOrderMaxValue(),
                new TestNoOrder3(),
            };

            // Act
            var result = items.OrderyByPrecedence();

            // Assert
            Assert.Collection(result,
                r => Assert.IsType<TestNoOrder1>(r),
                r => Assert.IsType<TestNoOrder2>(r),
                r => Assert.IsType<TestNoOrder3>(r),
                r => Assert.IsType<TestOrderMaxValue>(r));
        }

        [Fact]
        public void ThreeItemsWithOneOrderedValueLow()
        {
            // Arrange
            var items = new ITest[]
            {
                new TestNoOrder1(),
                new TestNoOrder2(),
                new TestNoOrder3(),
                new TestOrderMinValue(),
            };

            // Act
            var result = items.OrderyByPrecedence();

            // Assert
            Assert.Collection(result,
                r => Assert.IsType<TestOrderMinValue>(r),
                r => Assert.IsType<TestNoOrder1>(r),
                r => Assert.IsType<TestNoOrder2>(r),
                r => Assert.IsType<TestNoOrder3>(r));
        }

        [Fact]
        public void AllItemsWithOrder()
        {
            // Arrange
            var items = new ITest[]
            {
                new TestOrder1(),
                new TestOrder3(),
                new TestOrderNegative3(),
                new TestOrder2(),
                new TestOrderNegative1(),
                new TestOrderNegative2(),
            };

            // Act
            var result = items.OrderyByPrecedence();

            // Assert
            Assert.Collection(result,
                r => Assert.IsType<TestOrderNegative3>(r),
                r => Assert.IsType<TestOrderNegative2>(r),
                r => Assert.IsType<TestOrderNegative1>(r),
                r => Assert.IsType<TestOrder1>(r),
                r => Assert.IsType<TestOrder2>(r),
                r => Assert.IsType<TestOrder3>(r));
        }

        private interface ITest
        {
        }

        private class TestNoOrder1 : ITest
        {
        }

        private class TestNoOrder2 : ITest
        {
        }

        private class TestNoOrder3 : ITest
        {
        }

        [Order(int.MaxValue)]
        private class TestOrderMaxValue : ITest
        {
        }

        [Order(int.MinValue)]
        private class TestOrderMinValue : ITest
        {
        }

        [Order(1)]
        private class TestOrder1 : ITest
        {
        }

        [Order(2)]
        private class TestOrder2 : ITest
        {
        }

        [Order(3)]
        private class TestOrder3 : ITest
        {
        }

        [Order(-1)]
        private class TestOrderNegative1 : ITest
        {
        }

        [Order(-2)]
        private class TestOrderNegative2 : ITest
        {
        }

        [Order(-3)]
        private class TestOrderNegative3 : ITest
        {
        }
    }
}
