// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Klacks.E2ETest.Constants;
using Npgsql;

namespace Klacks.E2ETest.Helpers;

/// <summary>
/// Creates a container shift with one container template holding two timed shift rectangles through the
/// REST API, and removes every created row again (including soft-deleted and lock rows) on disposal.
/// All rows carry the INTEGRATION_TEST_ name prefix.
/// </summary>
/// <param name="apiBaseUrl">Base URL of the backend API, for example https://localhost:5001/api/backend/</param>
/// <param name="userName">Login name of an account that may create shifts and templates</param>
/// <param name="password">Password of that account</param>
/// <param name="connectionString">Connection string of the database the API writes to, used for the hard cleanup</param>
public sealed class ContainerTemplateTestData : IAsyncDisposable
{
    private readonly HttpClient _http;
    private readonly string _userName;
    private readonly string _password;
    private readonly string _connectionString;

    public ContainerTemplateTestData(string apiBaseUrl, string userName, string password, string connectionString)
    {
        var handler = new HttpClientHandler { ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator };
        _http = new HttpClient(handler) { BaseAddress = new Uri(apiBaseUrl) };
        _userName = userName;
        _password = password;
        _connectionString = connectionString;
    }

    public Guid ContainerId { get; private set; }

    public Guid TaskId { get; private set; }

    public async Task CreateAsync()
    {
        try
        {
            await LoginAsync();
            ContainerId = await CreateShiftAsync(ContainerTemplateTestDataIds.ContainerName, ContainerTemplateTestDataIds.ContainerAbbreviation, ContainerTemplateTestDataIds.ContainerShiftType);
            TaskId = await CreateShiftAsync(ContainerTemplateTestDataIds.TaskName, ContainerTemplateTestDataIds.TaskAbbreviation, ContainerTemplateTestDataIds.TaskShiftType);
            await SaveTemplateAsync();
        }
        catch
        {
            await DisposeAsync();
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (ContainerId != Guid.Empty || TaskId != Guid.Empty)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(ContainerTemplateTestDataIds.CleanupSql, connection);
            command.Parameters.AddWithValue(ContainerTemplateTestDataIds.ContainerParameter, ContainerId);
            command.Parameters.AddWithValue(ContainerTemplateTestDataIds.TaskParameter, TaskId);
            command.Parameters.AddWithValue(ContainerTemplateTestDataIds.NamePrefixParameter, ContainerTemplateTestDataIds.NamePrefix + "%");
            await command.ExecuteNonQueryAsync();
            ContainerId = Guid.Empty;
            TaskId = Guid.Empty;
        }

        _http.Dispose();
    }

    private async Task LoginAsync()
    {
        var response = await _http.PostAsJsonAsync(ContainerTemplateTestDataIds.LoginPath, new { email = _userName, password = _password });
        await EnsureSuccessAsync(response);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var token = document.RootElement.GetProperty(ContainerTemplateTestDataIds.TokenJsonKey).GetString();
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(ContainerTemplateTestDataIds.BearerScheme, token);
    }

    private async Task<Guid> CreateShiftAsync(string name, string abbreviation, int shiftType)
    {
        var body = new Dictionary<string, object?>
        {
            ["name"] = name,
            ["abbreviation"] = abbreviation,
            ["description"] = string.Empty,
            ["status"] = ContainerTemplateTestDataIds.OriginalShiftStatus,
            ["shiftType"] = shiftType,
            ["fromDate"] = ContainerTemplateTestDataIds.ShiftFromDate,
            ["startShift"] = ContainerTemplateTestDataIds.ContainerStart,
            ["endShift"] = ContainerTemplateTestDataIds.ContainerEnd,
            ["isMonday"] = true,
            ["quantity"] = 1,
            ["sumEmployees"] = 1,
            ["workTime"] = ContainerTemplateTestDataIds.ShiftWorkTime,
        };

        var response = await _http.PostAsJsonAsync(ContainerTemplateTestDataIds.ShiftsPath, body);
        await EnsureSuccessAsync(response);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty(ContainerTemplateTestDataIds.IdJsonKey).GetGuid();
    }

    private async Task SaveTemplateAsync()
    {
        var instanceId = ContainerTemplateTestDataIds.InstanceIdPrefix + Guid.NewGuid();
        var lockResponse = await _http.PostAsJsonAsync(
            ContainerTemplateTestDataIds.LockAcquirePath,
            new { resourceType = ContainerTemplateTestDataIds.LockResourceType, resourceId = ContainerId, instanceId });
        await EnsureSuccessAsync(lockResponse);
        using var lockDocument = JsonDocument.Parse(await lockResponse.Content.ReadAsStringAsync());
        var lockId = lockDocument.RootElement.GetProperty(ContainerTemplateTestDataIds.IdJsonKey).GetGuid();

        try
        {
            var template = new[]
            {
                new
                {
                    containerId = ContainerId,
                    fromTime = ContainerTemplateTestDataIds.ContainerStart,
                    untilTime = ContainerTemplateTestDataIds.ContainerEnd,
                    weekday = ContainerTemplateTestDataIds.MondayWeekday,
                    isWeekdayAndHoliday = false,
                    isHoliday = false,
                    transportMode = 0,
                    containerTemplateItems = new[]
                    {
                        BuildItem(ContainerTemplateTestDataIds.FirstItemStart, ContainerTemplateTestDataIds.FirstItemEnd),
                        BuildItem(ContainerTemplateTestDataIds.SecondItemStart, ContainerTemplateTestDataIds.SecondItemEnd),
                    },
                },
            };

            var save = new HttpRequestMessage(HttpMethod.Post, $"{ContainerTemplateTestDataIds.ContainersPath}{ContainerId}/templates")
            {
                Content = JsonContent.Create(template),
            };
            save.Headers.Add(ContainerTemplateTestDataIds.InstanceIdHeader, instanceId);
            await EnsureSuccessAsync(await _http.SendAsync(save));
        }
        finally
        {
            await _http.DeleteAsync($"{ContainerTemplateTestDataIds.LockReleasePath}{lockId}");
        }
    }

    private object BuildItem(string start, string end)
    {
        return new
        {
            shiftId = TaskId,
            weekday = ContainerTemplateTestDataIds.MondayWeekday,
            startItem = start,
            endItem = end,
            briefingTime = ContainerTemplateTestDataIds.ZeroTime,
            debriefingTime = ContainerTemplateTestDataIds.ZeroTime,
            travelTimeAfter = ContainerTemplateTestDataIds.ZeroTime,
            travelTimeBefore = ContainerTemplateTestDataIds.ZeroTime,
            transportMode = 0,
        };
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"{(int)response.StatusCode} {response.RequestMessage?.RequestUri}: {await response.Content.ReadAsStringAsync()}");
        }
    }
}
