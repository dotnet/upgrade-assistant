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
        public const string ADDRESSES_FILE = "DataFile.dat";
        public const string CONTACTS_FILE = "DataFile2.dat";
        public const string PROFILES_FILE = "button1_click.dat";
        public const string PHONE_NUMBERS_FILE = "button2_click.dat";
        public const string SUPER_FILE = "button3_click.dat";

        public UA0012()
        {
        }

        public Dictionary<string, string> GetSuperPowers()
        {
            // Open the file containing the data that you want to deserialize.
            var fs = new FileStream(SUPER_FILE, FileMode.Open);
            try
            {
                var formatter1 = new SuperHeroicSerializer.BinaryFormatter();

                // Deserialize the hashtable from the file and
                // assign the reference to the local variable.
                var superPowers = (Dictionary<string, string>)formatter1.UnsafeDeserialize(fs, null);
                return superPowers;
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

        public Dictionary<string, string> GetAddresses()
        {
            // Open the file containing the data that you want to deserialize.
            var fs = new FileStream(ADDRESSES_FILE, FileMode.Open);
            try
            {
                var formatter2 = new BinaryFormatter();

                // Deserialize the hashtable from the file and
                // assign the reference to the local variable.
                var addresses = (Dictionary<string, string>)formatter2.Deserialize(fs);
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

        public Dictionary<string, string> GetAlternateIdentities()
        {
            // Open the file containing the data that you want to deserialize.
            var fs = new FileStream(SUPER_FILE, FileMode.Open);
            try
            {
                var formatter3 = new BinaryFormatter();

                // Deserialize the hashtable from the file and
                // assign the reference to the local variable.
                string someValue = null;
                var identities = (Dictionary<string, string>)formatter3.UnsafeDeserialize(fs, headers => {
                    someValue = "something from the headers";
                    return null;
                });
                return identities;
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

        public Dictionary<string, string> GetContacts()
        {
            // Open the file containing the data that you want to deserialize.
            var fs = new FileStream(CONTACTS_FILE, FileMode.Open);
            try
            {
                // Deserialize the hashtable from the file and
                // assign the reference to the local variable.
                var contacts = (Dictionary<string, string>)new BinaryFormatter().Deserialize(fs);
                return contacts;
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

        public Dictionary<string, string> GetProfiles()
        {
            // Open the file containing the data that you want to deserialize.
            var fs = new FileStream(PROFILES_FILE, FileMode.Open);
            try
            {
                // Deserialize the hashtable from the file and
                // assign the reference to the local variable.
                string someValue = null;
                var profiles = (Dictionary<string, string>)new BinaryFormatter().UnsafeDeserialize(fs, headers => {
                    someValue = "something from the headers";
                    return null;
                });
                return profiles;
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

namespace SuperHeroicSerializer
{
    public class BinaryFormatter
    {
        public object UnsafeDeserialize(Stream serializationStream, System.Runtime.Remoting.Messaging.HeaderHandler handler)
        {
            return throw new NotImplementedException("under construction");
        }
    }
}
