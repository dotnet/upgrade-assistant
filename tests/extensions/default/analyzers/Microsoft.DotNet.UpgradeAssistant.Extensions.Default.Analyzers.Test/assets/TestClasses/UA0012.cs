// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers.Test.assets.TestClasses
{
    public class UA0012
    {
        public const string STATE_FILE = "DataFile.dat";

        public UA0012()
        {
        }

        public Dictionary<string,string> GetAddresses()
        {
            // Open the file containing the data that you want to deserialize.
            var fs = new FileStream(STATE_FILE, FileMode.Open);
            try
            {
                var formatter = new BinaryFormatter();

                // Deserialize the hashtable from the file and
                // assign the reference to the local variable.
                var addresses = (Dictionary<string, string>)formatter.UnsafeDeserialize(fs, null);
                return addresses;
            }
            catch (SerializationException e)
            {
                Console.WriteLine("Failed to deserialize. Reason: " + e.Message);
                throw;
            }
            finally
            {
                fs.Close();
            }
        }
    }
}
