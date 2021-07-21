// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Microsoft.DotNet.UpgradeAssistant.Telemetry
{
    internal class MacAddressGetter : IMacAddressProvider
    {
        private const string InvalidMacAddress = "00-00-00-00-00-00";
        private const string MacRegex = @"(?:[a-z0-9]{2}[:\-]){5}[a-z0-9]{2}";
        private const string ZeroRegex = @"(?:00[:\-]){5}00";
        private const int ErrorFileNotFound = 0x2;

        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "We don't want any errors in telemetry to cause failures in the product.")]
        public string? GetMacAddress()
        {
            try
            {
                var macAddress = GetMacAddressCore();
                if (string.IsNullOrWhiteSpace(macAddress) || macAddress!.Equals(InvalidMacAddress, StringComparison.OrdinalIgnoreCase))
                {
                    return GetMacAddressByNetworkInterface();
                }
                else
                {
                    return macAddress;
                }
            }
            catch
            {
                return null;
            }
        }

        private static string? GetMacAddressCore()
        {
            try
            {
                var shelloutput = GetShellOutMacAddressOutput();
                if (string.IsNullOrWhiteSpace(shelloutput))
                {
                    return null;
                }

                return ParseMACAddress(shelloutput!);
            }
            catch (Win32Exception e)
            {
                if (e.NativeErrorCode == ErrorFileNotFound)
                {
                    return GetMacAddressByNetworkInterface();
                }
                else
                {
                    throw;
                }
            }
        }

        private static string? ParseMACAddress(string shelloutput)
        {
            foreach (Match match in Regex.Matches(shelloutput, MacRegex, RegexOptions.IgnoreCase))
            {
                if (!Regex.IsMatch(match.Value, ZeroRegex))
                {
                    return match.Value;
                }
            }

            return null;
        }

        private static string? GetIpCommandOutput()
        {
            var ipResult = new ProcessStartInfo
            {
                FileName = "ip",
                Arguments = "link",
                UseShellExecute = false
            }.ExecuteAndCaptureOutput(out var ipStdOut, out _);

            if (ipResult == 0)
            {
                return ipStdOut;
            }
            else
            {
                return null;
            }
        }

        private static string? GetShellOutMacAddressOutput()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var result = new ProcessStartInfo
                {
                    FileName = "getmac.exe",
                    UseShellExecute = false
                }.ExecuteAndCaptureOutput(out var stdOut, out _);

                if (result == 0)
                {
                    return stdOut;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                try
                {
                    var ifconfigResult = new ProcessStartInfo
                    {
                        FileName = "ifconfig",
                        Arguments = "-a",
                        UseShellExecute = false
                    }.ExecuteAndCaptureOutput(out var ifconfigStdOut, out var ifconfigStdErr);

                    if (ifconfigResult == 0)
                    {
                        return ifconfigStdOut;
                    }
                    else
                    {
                        return GetIpCommandOutput();
                    }
                }
                catch (Win32Exception e)
                {
                    if (e.NativeErrorCode == ErrorFileNotFound)
                    {
                        return GetIpCommandOutput();
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }

        private static string GetMacAddressByNetworkInterface()
        {
            return GetMacAddressesByNetworkInterface().Where(x => !x.Equals(InvalidMacAddress, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
        }

        private static List<string> GetMacAddressesByNetworkInterface()
        {
            var nics = NetworkInterface.GetAllNetworkInterfaces();
            var macs = new List<string>();

            if (nics == null || nics.Length < 1)
            {
                macs.Add(string.Empty);
                return macs;
            }

            foreach (NetworkInterface adapter in nics)
            {
                IPInterfaceProperties properties = adapter.GetIPProperties();

                PhysicalAddress address = adapter.GetPhysicalAddress();
                byte[] bytes = address.GetAddressBytes();
#pragma warning disable CA1305 // Specify IFormatProvider
                macs.Add(string.Join("-", bytes.Select(x => x.ToString("X2"))));
#pragma warning restore CA1305 // Specify IFormatProvider
                if (macs.Count >= 10)
                {
                    break;
                }
            }

            return macs;
        }
    }
}
