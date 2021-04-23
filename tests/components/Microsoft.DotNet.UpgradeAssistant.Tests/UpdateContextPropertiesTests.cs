// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extras.Moq;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Microsoft.DotNet.UpgradeAssistant.Tests
{
    public class UpdateContextPropertiesTests
    {
        [Fact]
        public async Task GetAndSetWork()
        {
            var properties = new UpgradeContextProperties();
            var propertyKey = "Property:Key";
            var propertyValue = "Property:Value";
            properties.SetPropertyValue(propertyKey, propertyValue, false);
            Assert.Equal(propertyValue, properties.GetPropertyValue(propertyKey));
        }

        [Fact]
        public async Task GetAndSetPersistentWorks()
        {
            var properties = new UpgradeContextProperties();
            Assert.Empty(properties.GetPersistentPropertyValues());

            var persistentPropertykey = "foo";
            properties.SetPropertyValue(persistentPropertykey, "foobar", true);
            properties.SetPropertyValue("bar", "foobar", false);
            Assert.Collection(properties.GetPersistentPropertyValues(), e => Assert.Equal(e.Key, persistentPropertykey));
        }

        [Fact]
        public async Task GetAllWorks()
        {
            var properties = new UpgradeContextProperties();
            Assert.Empty(properties.GetAllPropertyValues());

            properties.SetPropertyValue("foo", "foobar", true);
            properties.SetPropertyValue("bar", "foobar", false);
            Assert.Collection(properties.GetAllPropertyValues(), _ => { }, _ => { });
        }

        [Fact]
        public async Task RemoveWorks()
        {
            var properties = new UpgradeContextProperties();
            var propertyKey = "foo";
            properties.SetPropertyValue(propertyKey, "foobar", true);
            var success = properties.RemovePropertyValue(propertyKey);
            Assert.True(success);
            Assert.Empty(properties.GetAllPropertyValues());
        }
    }
}
