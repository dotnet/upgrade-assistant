// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.Sarif;

namespace Microsoft.DotNet.UpgradeAssistant.Analysis
{
    public class WriteOutput
    {
        private IJsonSerializer _serializer;

        public WriteOutput(IJsonSerializer jsonSerializer)
        {
            _serializer = jsonSerializer;
        }

        public virtual void Write(string filePath, SarifLog sarifLog)
        {
            _serializer.Write(filePath, sarifLog, ensureDirectory: true);
        }

        public static SarifLog CreateSarifLog(IList<Run> run)
        {
            var sarifLog = new SarifLog()
            {
                Version = SarifVersion.Current,
                SchemaUri = new Uri("http://json.schemastore.org/sarif-1.0.0"),
                Runs = run
            };

            return sarifLog;
        }
    }
}
