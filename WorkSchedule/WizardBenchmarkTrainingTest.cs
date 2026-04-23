// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// E2E training harness for the schedule autofill wizard.
/// Exercises POST /api/backend/Wizard/Benchmark across a grid of TokenEvolutionConfig
/// parameter combinations, measures duration/fitness/coverage, and writes a JSON report
/// to the test output directory. Designed to be run locally against a Dev DB to benchmark
/// algorithm changes or to feed the parameter-optimization loop.
/// </summary>

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Npgsql;

namespace Klacks.E2ETest.WorkSchedule;

[TestFixture]
[Category("Training")]
[Explicit("Training benchmark — run locally against Dev API on https://localhost:5001; skipped in CI")]
public class WizardBenchmarkTrainingTest
{
    private const string ApiBaseUrl = "https://localhost:5001/api/backend/";
    private const string LoginEmail = "admin@test.com";
    private const string LoginPassword = "P@ssw0rt1";
    private const string DevConnectionString = "Host=localhost;Port=5434;Username=postgres;Password=admin;Database=klacks";

    private HttpClient _http = null!;
    private string _token = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetup()
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true,
        };
        _http = new HttpClient(handler) { BaseAddress = new Uri(ApiBaseUrl) };

        var login = await _http.PostAsJsonAsync("Accounts/LoginUser",
            new { email = LoginEmail, password = LoginPassword });
        login.EnsureSuccessStatusCode();
        var loginBody = await login.Content.ReadFromJsonAsync<LoginResponse>()
            ?? throw new InvalidOperationException("Login returned no body");
        _token = loginBody.Token;
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _http.Dispose();
    }

    [Test]
    public async Task Benchmark_ParameterGrid_WritesJsonReport()
    {
        var agents = await FetchIdsAsync("SELECT id FROM client WHERE is_deleted = false LIMIT 10");
        var shifts = await FetchIdsAsync(
            "SELECT id FROM shift WHERE is_deleted = false " +
            "AND (until_date IS NULL OR until_date >= '2026-04-20') " +
            "AND from_date <= '2026-04-26' LIMIT 5");

        Assert.That(agents, Is.Not.Empty, "need clients in Dev DB");
        Assert.That(shifts, Is.Not.Empty, "need active shifts in Dev DB");

        var grid = new[]
        {
            new TrainingOverrides(PopulationSize: 20, MaxGenerations: 30, RandomSeed: 42),
            new TrainingOverrides(PopulationSize: 40, MaxGenerations: 100, RandomSeed: 42),
            new TrainingOverrides(PopulationSize: 80, MaxGenerations: 200, RandomSeed: 42),
            new TrainingOverrides(PopulationSize: 40, MaxGenerations: 100, MutationRate: 0.4, RandomSeed: 42),
            new TrainingOverrides(PopulationSize: 40, MaxGenerations: 100, MutationRate: 0.1, RandomSeed: 42),
        };

        var results = new List<BenchmarkResponse>();
        foreach (var overrides in grid)
        {
            var response = await RunBenchmarkAsync(agents, shifts, overrides);
            results.Add(response);
            TestContext.Out.WriteLine(
                $"pop={overrides.PopulationSize} gen={overrides.MaxGenerations} mut={overrides.MutationRate?.ToString("F2") ?? "dflt"} | " +
                $"time={response.DurationMs}ms stage0={response.FinalHardViolations} stage1={response.FinalStage1Completion:F2} " +
                $"stage2={response.FinalStage2Score:F3} tokens={response.TokenCount}/{response.AvailableShiftSlots} " +
                $"cov={response.CoverageRatio:F2} dup={response.ClientDayDuplicates}");
        }

        var reportPath = Path.Combine(TestContext.CurrentContext.TestDirectory,
            $"wizard-benchmark-{DateTime.Now:yyyyMMdd-HHmmss}.json");
        var reportJson = JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(reportPath, reportJson, Encoding.UTF8);
        TestContext.Out.WriteLine($"Report written to: {reportPath}");

        Assert.That(results, Has.All.Matches<BenchmarkResponse>(r => r.DurationMs >= 0),
            "all runs should complete successfully");
        Assert.That(results, Has.All.Matches<BenchmarkResponse>(r => r.FinalHardViolations == 0),
            "with these inputs, hard violations should stay 0 for all configs");
    }

    private async Task<BenchmarkResponse> RunBenchmarkAsync(
        IReadOnlyList<Guid> agents, IReadOnlyList<Guid> shifts, TrainingOverrides overrides)
    {
        var body = new
        {
            periodFrom = "2026-04-20",
            periodUntil = "2026-04-26",
            agentIds = agents,
            shiftIds = shifts,
            analyseToken = Guid.NewGuid(),
            trainingOverrides = overrides,
        };

        var response = await _http.PostAsJsonAsync("Wizard/Benchmark", body);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<BenchmarkResponse>(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        }) ?? throw new InvalidOperationException("empty benchmark response");
    }

    private static async Task<IReadOnlyList<Guid>> FetchIdsAsync(string sql)
    {
        await using var conn = new NpgsqlConnection(DevConnectionString);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync();
        var list = new List<Guid>();
        while (await reader.ReadAsync())
        {
            list.Add(reader.GetGuid(0));
        }
        return list;
    }

    private sealed record LoginResponse(string Token);

    private sealed record TrainingOverrides(
        int? PopulationSize = null,
        int? MaxGenerations = null,
        double? MutationRate = null,
        double? CrossoverRate = null,
        int? RandomSeed = null);

    private sealed record BenchmarkResponse(
        long DurationMs,
        int FinalHardViolations,
        double FinalStage1Completion,
        double FinalStage2Score,
        int TokenCount,
        int AvailableShiftSlots,
        double CoverageRatio,
        int ClientDayDuplicates,
        int DistinctAgents,
        int DistinctShifts,
        int DistinctDates,
        EffectiveConfig EffectiveConfig);

    private sealed record EffectiveConfig(
        int PopulationSize,
        int MaxGenerations,
        int TournamentK,
        double MutationRate,
        double CrossoverRate,
        int ElitismCount,
        double MutationWeightSwap,
        double MutationWeightSplit,
        double MutationWeightMerge,
        double MutationWeightReassign,
        double MutationWeightRepair,
        int EarlyStopNoImprovementGenerations,
        int RandomSeed);
}
