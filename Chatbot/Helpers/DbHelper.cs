// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Diagnostics;

namespace Klacks.E2ETest.Chatbot.Helpers;

public static class DbHelper
{
    private const string PsqlPath = @"C:\Program Files\PostgreSQL\17\bin\psql.exe";
    private const string Host = "localhost";
    private const string Port = "5434";
    private const string User = "postgres";
    private const string Password = "admin";
    private const string Database = "klacks";

    public static async Task<string> ExecuteSqlAsync(string sql)
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"klacks_e2e_{Guid.NewGuid():N}.sql");
        await File.WriteAllTextAsync(tempFile, sql);
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = PsqlPath,
                Arguments = $"-h {Host} -p {Port} -U {User} -d {Database} -t -A -f \"{tempFile}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            psi.Environment["PGPASSWORD"] = Password;

            using var process = Process.Start(psi)!;
            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            return string.IsNullOrEmpty(error) ? output.Trim() : $"ERROR: {error.Trim()}";
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    private static string Escape(string value) => value.Replace("'", "''");

    public static async Task<Dictionary<string, string>> GetUiControlSelectorsAsync(string pageKey)
    {
        var sql = $"SELECT control_key, selector FROM ui_controls WHERE page_key = '{Escape(pageKey)}' AND is_deleted = false ORDER BY sort_order";
        var result = await ExecuteSqlAsync(sql);

        var selectors = new Dictionary<string, string>();
        if (string.IsNullOrEmpty(result) || result.StartsWith("ERROR:"))
            return selectors;

        foreach (var line in result.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = line.Split('|');
            if (parts.Length == 2)
                selectors[parts[0].Trim()] = parts[1].Trim();
        }

        return selectors;
    }

    public static async Task<string> GetSelectorAsync(string pageKey, string controlKey)
    {
        var sql = $"SELECT selector FROM ui_controls WHERE page_key = '{Escape(pageKey)}' AND control_key = '{Escape(controlKey)}' AND is_deleted = false";
        var result = await ExecuteSqlAsync(sql);

        if (string.IsNullOrEmpty(result) || result.StartsWith("ERROR:"))
            throw new InvalidOperationException($"UI control not found: {pageKey}/{controlKey}. Result: {result}");

        return result.Trim();
    }

    public static async Task<Dictionary<string, string>> GetUiControlRoutesAsync(string pageKey)
    {
        var sql = $"SELECT control_key, route FROM ui_controls WHERE page_key = '{Escape(pageKey)}' AND route IS NOT NULL AND is_deleted = false ORDER BY sort_order";
        var result = await ExecuteSqlAsync(sql);

        var routes = new Dictionary<string, string>();
        if (string.IsNullOrEmpty(result) || result.StartsWith("ERROR:"))
            return routes;

        foreach (var line in result.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = line.Split('|');
            if (parts.Length == 2)
                routes[parts[0].Trim()] = parts[1].Trim();
        }

        return routes;
    }

    public static async Task<bool> IsSkillEnabledAsync(string skillName)
    {
        var sql = $"SELECT is_enabled FROM agent_skills WHERE name = '{Escape(skillName)}' AND is_deleted = false LIMIT 1";
        var result = await ExecuteSqlAsync(sql);
        return result.Trim() == "t";
    }

    public static async Task<SkillInfo?> GetSkillInfoAsync(string skillName)
    {
        var sql = $"SELECT name, description, execution_type, is_enabled, category, parameters_json FROM agent_skills WHERE name = '{Escape(skillName)}' AND is_deleted = false LIMIT 1";
        var result = await ExecuteSqlAsync(sql);

        if (string.IsNullOrEmpty(result) || result.StartsWith("ERROR:"))
            return null;

        var parts = result.Split('|');
        if (parts.Length < 6)
            return null;

        return new SkillInfo(
            Name: parts[0].Trim(),
            Description: parts[1].Trim(),
            ExecutionType: parts[2].Trim(),
            IsEnabled: parts[3].Trim() == "t",
            Category: parts[4].Trim(),
            ParametersJson: parts[5].Trim()
        );
    }
}

public record SkillInfo(
    string Name,
    string Description,
    string ExecutionType,
    bool IsEnabled,
    string Category,
    string ParametersJson
);
