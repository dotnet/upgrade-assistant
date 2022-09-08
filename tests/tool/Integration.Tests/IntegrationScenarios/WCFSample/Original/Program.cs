﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.Diagnostics;

namespace ConsoleApp
{
    internal class Program
    {
        static void Main(string[] args)
        {
            try
            {
                ServiceHost host = new ServiceHost(typeof(WcfServiceLibrary1.Service1));
                host.Open();
                Console.WriteLine("Service Hosted Sucessfully. Hit any key to exit");
                Console.ReadKey();
                host.Close();
            }
            catch(Exception ex)
            {
                Debug.Write(ex.ToString());
            }
        }
    }
}
