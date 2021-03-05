using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace ConsoleApp1
{
    /// <summary>
    /// A console app that demonstrates how to serialize object data so that it
    /// can be persisted to disk and loaded at a later point in time
    /// </summary>
    class Program
    {
        private const string STATE_FILE = "DataFile.dat";

        static void Main(string[] args)
        {
            // arrange
            var data = GetData();
            // act
            Serialize(data);
            var dataFromFile = Deserialize();
            // assert
            Assert(dataFromFile);

            Console.WriteLine("See! No exceptions... that means it worked");
        }

        private static void Assert(Dictionary<string, string> testData)
        {
            var addresses = GetData();

            if (testData.Count != addresses.Count)
            {
                throw new InvalidOperationException("The two collections are expected to be the same size");
            }

            // To prove that the table deserialized correctly,
            // display the key/value pairs.
            for (int i = 0; i < addresses.Count; i++)
            {
                if (!testData.ElementAt(i).Key.Equals(addresses.ElementAt(i).Key, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException($"Expected the name '{addresses.ElementAt(i).Key}' but found '{testData.ElementAt(i).Key}'");
                }
                else if (!testData.ElementAt(i).Value.Equals(addresses.ElementAt(i).Value, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException($"Expected the address '{addresses.ElementAt(i).Value}' but found '{testData.ElementAt(i).Value}'");
                }
            }
        }

        private static Dictionary<string,string> GetData()
        {
            // Create a hashtable of values that will eventually be serialized.
            var addresses = new Dictionary<string, string>()
            {
                {"Jeff", "123 Main Street, Redmond, WA 98052" },
                {"Fred", "987 Pine Road, Phila., PA 19116" },
                {"Mary", "PO Box 112233, Palo Alto, CA 94301" }
            };

            return addresses;
        }

        static void Serialize(Dictionary<string, string> dataToSerialize)
        {
            // To serialize the hashtable and its key/value pairs,
            // you must first open a stream for writing.
            // In this case, use a file stream.
            var fs = new FileStream(STATE_FILE, FileMode.Create);

            // Construct a BinaryFormatter and use it to serialize the data to the stream.
            var formatter = new BinaryFormatter();
            try
            {
                formatter.Serialize(fs, dataToSerialize);
            }
            catch (SerializationException e)
            {
                Console.WriteLine("Failed to serialize. Reason: " + e.Message);
                throw;
            }
            finally
            {
                fs.Close();
            }
        }

        static Dictionary<string, string> Deserialize()
        {
            // Declare the hashtable reference.
            Dictionary<string, string> addresses = null;

            // Open the file containing the data that you want to deserialize.
            var fs = new FileStream(STATE_FILE, FileMode.Open);
            try
            {
                var formatter = new BinaryFormatter();

                // Deserialize the hashtable from the file and
                // assign the reference to the local variable.
                addresses = (Dictionary<string, string>)formatter.UnsafeDeserialize(fs, null);
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
