// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace MSBuild.Conversion.Package
{
    public class PackagesConfigHasNoPackagesException : Exception
    {
        public PackagesConfigHasNoPackagesException()
        {
        }

        public PackagesConfigHasNoPackagesException(string message)
            : base(message)
        {
        }
    }
}
