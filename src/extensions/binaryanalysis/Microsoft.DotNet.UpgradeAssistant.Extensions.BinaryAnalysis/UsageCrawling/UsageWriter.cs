// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Data.Sqlite;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.BinaryAnalysis.UsageCrawling;

public sealed class UsageWriter : IDisposable
{
    private readonly SqliteCommand _command;
    private readonly SqliteTransaction _transaction;
    private readonly SqliteParameter _packageIdParameter;
    private readonly SqliteParameter _apiIdParameter;

    public UsageWriter(SqliteConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);

        _transaction = connection.BeginTransaction();
        _command = new SqliteCommand(@"
            INSERT INTO Usages
                (PackageId, ApiId)
            VALUES
                (@PackageId, @ApiId)
        ", connection, _transaction);

        _packageIdParameter = _command.Parameters.Add("PackageId", SqliteType.Integer);
        _apiIdParameter = _command.Parameters.Add("ApiId", SqliteType.Integer);
    }

    public Task WriteAsync(int packageId, int apiId)
    {
        _packageIdParameter.Value = packageId;
        _apiIdParameter.Value = apiId;
        return _command.ExecuteNonQueryAsync();
    }

    public Task SaveAsync()
    {
        return _transaction.CommitAsync();
    }

    public void Dispose()
    {
        _command.Dispose();
        _transaction.Dispose();
    }
}
