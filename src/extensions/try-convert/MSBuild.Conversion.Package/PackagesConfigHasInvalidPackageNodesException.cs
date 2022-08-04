// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace MSBuild.Conversion.Package
{
    public class PackagesConfigHasInvalidPackageNodesException : Exception
    {
        public PackagesConfigHasInvalidPackageNodesException()
        {
        }

        public PackagesConfigHasInvalidPackageNodesException(string message)
            : base(message)
        {
        }
    }
}
