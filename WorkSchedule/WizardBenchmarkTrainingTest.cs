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

    private const string PeriodFrom = "2026-04-20";
    private const string PeriodUntil = "2026-05-03";
    private const int AgentCount = 50;
    private const int ShiftPerBucket = 5;

    [Test]
    public async Task Benchmark_ParameterGrid_WritesJsonReport()
    {
        var agents = await FetchIdsAsync(
            $"SELECT id FROM client WHERE is_deleted = false ORDER BY name LIMIT {AgentCount}");

        var shifts = new List<Guid>();
        shifts.AddRange(await FetchShiftBucketAsync("07:00", "15:00")); // Frueh
        shifts.AddRange(await FetchShiftBucketAsync("15:00", "23:00")); // Spaet
        shifts.AddRange(await FetchShiftBucketAsync("23:00", "07:00")); // Nacht

        Assert.That(agents, Has.Count.EqualTo(AgentCount), "need clients in Dev DB");
        Assert.That(shifts, Has.Count.EqualTo(ShiftPerBucket * 3),
            "need FD/SD/ND shift pool (07-15, 15-23, 23-07) in Dev DB");

        var grid = BuildParameterGrid();

        var results = new List<BenchmarkResponse>();
        foreach (var overrides in grid)
        {
            var response = await RunBenchmarkAsync(agents, shifts, overrides);
            results.Add(response);
            TestContext.Out.WriteLine(FormatRow(overrides, response));
        }

        var reportPath = Path.Combine(TestContext.CurrentContext.TestDirectory,
            $"wizard-benchmark-{DateTime.Now:yyyyMMdd-HHmmss}.json");
        var reportJson = JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(reportPath, reportJson, Encoding.UTF8);
        TestContext.Out.WriteLine($"Report written to: {reportPath}");

        Assert.That(results, Has.All.Matches<BenchmarkResponse>(r => r.DurationMs >= 0),
            "all runs should complete successfully");
    }

    private static IReadOnlyList<TrainingOverrides> BuildParameterGrid()
    {
        const int seed = 42;
        return new[]
        {
            // Baseline (current TokenEvolutionConfig defaults)
            new TrainingOverrides(RandomSeed: seed),

            // Population scaling
            new TrainingOverrides(PopulationSize: 20, MaxGenerations: 100, RandomSeed: seed),
            new TrainingOverrides(PopulationSize: 80, MaxGenerations: 100, RandomSeed: seed),
            new TrainingOverrides(PopulationSize: 120, MaxGenerations: 100, RandomSeed: seed),

            // Generation depth
            new TrainingOverrides(PopulationSize: 40, MaxGenerations: 50, RandomSeed: seed),
            new TrainingOverrides(PopulationSize: 40, MaxGenerations: 300, RandomSeed: seed),

            // Mutation rate sweep
            new TrainingOverrides(MutationRate: 0.10, RandomSeed: seed),
            new TrainingOverrides(MutationRate: 0.40, RandomSeed: seed),
            new TrainingOverrides(MutationRate: 0.60, RandomSeed: seed),

            // Tournament selection pressure
            new TrainingOverrides(TournamentK: 2, RandomSeed: seed),
            new TrainingOverrides(TournamentK: 5, RandomSeed: seed),
            new TrainingOverrides(TournamentK: 7, RandomSeed: seed),

            // Elitism
            new TrainingOverrides(ElitismCount: 0, RandomSeed: seed),
            new TrainingOverrides(ElitismCount: 5, RandomSeed: seed),

            // Early-stop horizon
            new TrainingOverrides(EarlyStopNoImprovementGenerations: 10, RandomSeed: seed),
            new TrainingOverrides(EarlyStopNoImprovementGenerations: 60, RandomSeed: seed),

            // Promising combo (exploration-heavy big run)
            new TrainingOverrides(
                PopulationSize: 80, MaxGenerations: 200, MutationRate: 0.35,
                TournamentK: 4, ElitismCount: 3, RandomSeed: seed),
        };
    }

    private static string FormatRow(TrainingOverrides ov, BenchmarkResponse r)
    {
        return $"pop={Fmt(ov.PopulationSize)} gen={Fmt(ov.MaxGenerations)} " +
               $"mut={FmtD(ov.MutationRate)} k={Fmt(ov.TournamentK)} elit={Fmt(ov.ElitismCount)} " +
               $"early={Fmt(ov.EarlyStopNoImprovementGenerations)} | " +
               $"time={r.DurationMs}ms stage0={r.FinalHardViolations} " +
               $"stage1={r.FinalStage1Completion:F2} stage2={r.FinalStage2Score:F3} " +
               $"tokens={r.TokenCount}/{r.AvailableShiftSlots} " +
               $"slots={r.CoveredShiftSlots}/{r.AvailableShiftSlots} " +
               $"slotCov={r.ShiftCoverageRatio:P1}(max={r.TheoreticalMaxCoverage:P1}) " +
               $"agents={r.DistinctAgents}/{r.AgentsInContext}(shiftCap={r.AgentsShiftCapable}) " +
               $"over={r.OverstaffingCount} under={r.UndersupplyCount} dup={r.ClientDayDuplicates}";
    }

    private static string Fmt(int? v) => v?.ToString() ?? "dflt";
    private static string FmtD(double? v) => v?.ToString("F2") ?? "dflt";

    private async Task<IReadOnlyList<Guid>> FetchShiftBucketAsync(string startTime, string endTime)
    {
        var sql =
            "SELECT id FROM shift " +
            "WHERE is_deleted = false " +
            $"  AND start_shift = TIME '{startTime}' " +
            $"  AND end_shift = TIME '{endTime}' " +
            "  AND work_time = 8 " +
            $"  AND (until_date IS NULL OR until_date >= '{PeriodFrom}') " +
            $"  AND from_date <= '{PeriodUntil}' " +
            "ORDER BY name " +
            $"LIMIT {ShiftPerBucket}";
        return await FetchIdsAsync(sql);
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
        int? TournamentK = null,
        double? MutationRate = null,
        double? CrossoverRate = null,
        int? ElitismCount = null,
        int? EarlyStopNoImprovementGenerations = null,
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
        int CoveredShiftSlots,
        int OverstaffingCount,
        int UndersupplyCount,
        double ShiftCoverageRatio,
        int AgentsInContext,
        int AgentsShiftCapable,
        int SlotsFillableByData,
        double TheoreticalMaxCoverage,
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
