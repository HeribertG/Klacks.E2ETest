// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.E2ETest.Constants;
using Npgsql;

namespace Klacks.E2ETest.Helpers;

/// <summary>
/// Remembers the client_availability rows that exist before a test and, afterwards, puts the touched
/// client/date pairs back into exactly that state: rows created by the test are deleted, changed rows
/// get their old values back.
/// </summary>
/// <param name="connectionString">Connection string of the database the API writes to</param>
public sealed class AvailabilityRowRestorer
{
    private readonly string _connectionString;
    private readonly Dictionary<Guid, (bool IsAvailable, bool IsDeleted)> _snapshot = new();

    public AvailabilityRowRestorer(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task SnapshotAsync()
    {
        _snapshot.Clear();
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(MacContextClickIds.AvailabilitySnapshotSql, connection);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            _snapshot[reader.GetGuid(0)] = (reader.GetBoolean(1), reader.GetBoolean(2));
        }
    }

    public async Task RestoreAsync(IEnumerable<(Guid ClientId, DateOnly Date)> touched)
    {
        var pairs = touched.Distinct().ToList();
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        foreach (var (clientId, date) in pairs)
        {
            var current = new List<Guid>();
            await using (var select = new NpgsqlCommand(MacContextClickIds.AvailabilityRowsOfDaySql, connection))
            {
                select.Parameters.AddWithValue(MacContextClickIds.ClientParameter, clientId);
                select.Parameters.AddWithValue(MacContextClickIds.DateParameter, date);
                await using var reader = await select.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    current.Add(reader.GetGuid(0));
                }
            }

            foreach (var id in current)
            {
                if (_snapshot.TryGetValue(id, out var old))
                {
                    await using var update = new NpgsqlCommand(MacContextClickIds.AvailabilityResetRowSql, connection);
                    update.Parameters.AddWithValue(MacContextClickIds.IdParameter, id);
                    update.Parameters.AddWithValue(MacContextClickIds.IsAvailableParameter, old.IsAvailable);
                    update.Parameters.AddWithValue(MacContextClickIds.IsDeletedParameter, old.IsDeleted);
                    await update.ExecuteNonQueryAsync();
                }
                else
                {
                    await using var delete = new NpgsqlCommand(MacContextClickIds.AvailabilityDeleteRowSql, connection);
                    delete.Parameters.AddWithValue(MacContextClickIds.IdParameter, id);
                    await delete.ExecuteNonQueryAsync();
                }
            }
        }
    }
}
