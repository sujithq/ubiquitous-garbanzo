using System.Diagnostics;
using FluentAssertions;
using Xunit;

namespace CliApp.Tests;

public class CliTests
{
    private static string DotnetExe => "dotnet";

    private static (int exit, string stdout, string stderr) Run(params string[] args)
    {
        var config = GetCurrentConfiguration(); // Debug or Release
        var psi = new ProcessStartInfo
        {
            FileName = DotnetExe,
            ArgumentList = { "run", "-c", config, "--project", GetCliProjectPath() },
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        foreach (var a in args) psi.ArgumentList.Add(a);

        using var p = Process.Start(psi)!;
        var stdout = p.StandardOutput.ReadToEnd();
        var stderr = p.StandardError.ReadToEnd();
        p.WaitForExit();
        return (p.ExitCode, stdout.Trim(), stderr.Trim());
    }

    private static string GetCliProjectPath()
    {
        // Resolve ../../CliApp/CliApp.csproj from test assembly folder
        var baseDir = AppContext.BaseDirectory; // .../bin/{Config}/net9.0/
        var proj = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "CliApp", "CliApp.csproj"));
        return proj;
    }

    private static string GetCurrentConfiguration()
    {
        // Extract configuration directory name from .../bin/{Config}/netX.Y/
        var baseDir = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var netDir = Directory.GetParent(baseDir);
        var configDir = netDir?.Parent; // .../bin/{Config}
        var name = configDir?.Name ?? "Debug";
        return (name.Equals("Release", StringComparison.OrdinalIgnoreCase)) ? "Release" : "Debug";
    }

    [Fact]
    public void Greet_should_print_hello_name()
    {
        var (exit, stdout, _) = Run("--", "greet", "--name", "Sujith");
        exit.Should().Be(0);
        stdout.Should().Be("Hello, Sujith!");
    }

    [Fact]
    public void Add_should_print_sum()
    {
        var (exit, stdout, _) = Run("--", "add", "--a", "2", "--b", "3");
        exit.Should().Be(0);
        stdout.Should().Be("5");
    }

    [Fact(Skip = "Intentionally skipped - this test is designed to fail for auto-triage testing")]
    public void Intentional_failure_should_fail_build()
    {
        // This assertion is intentionally incorrect to trigger auto-triage.
        var (exit, stdout, _) = Run("--", "add", "--a", "2", "--b", "2");
        exit.Should().Be(0);
        stdout.Should().Be("5"); // Fails: 2+2 != 5; easy fix is change expected to 4
    }
}
