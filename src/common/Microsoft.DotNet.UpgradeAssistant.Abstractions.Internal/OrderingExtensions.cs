// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public static class OrderingExtensions
    {
        public static IOrderedEnumerable<T> OrderyByPrecedence<T>(this IEnumerable<T> items)
            => items.OrderBy(static t => t.GetOrder());

        private static int GetOrder<T>(this T item)
        {
            if (item is null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            var attribute = item.GetType().GetCustomAttributes<OrderAttribute>().FirstOrDefault();

            if (attribute is null)
            {
                return 0;
            }

            return attribute.Order;
        }
    }
}
