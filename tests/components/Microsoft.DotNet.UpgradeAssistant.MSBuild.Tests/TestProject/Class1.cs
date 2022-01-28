// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Newtonsoft.Json;

namespace TestProject
{
    public static class Class1
    {
        public static void Test()
        {
            string json = @"{
            'Email': 'test@example.com',
            'Active': true,
            'CreatedDate': '2013-01-20T00:00:00Z'
            }";

            var account = JsonConvert.DeserializeObject<Account>(json);

            Console.WriteLine(account.Email);
        }
    }
}
