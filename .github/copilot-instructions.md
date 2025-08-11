# ubiquitous-garbanzo
.NET 9.0 CLI application with AI-powered triage for failed GitHub Actions runs.

Always reference these instructions first and fallback to search or bash commands only when you encounter unexpected information that does not match the info here.

## Working Effectively

### Bootstrap Environment and Build
- Install .NET 9.0 SDK: `curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 9.0 --install-dir ~/.dotnet`
- Add to PATH: `export PATH="$HOME/.dotnet:$PATH"`
- Verify version: `dotnet --version` (should be 9.0.x)
- Navigate to repository root and run these commands in sequence:
  - `dotnet restore src/cli-sample.sln` -- takes 1-2 seconds. NEVER CANCEL.
  - `dotnet build src/cli-sample.sln -c Release --no-restore` -- takes 2-3 seconds. NEVER CANCEL.
  - Combined: `dotnet restore src/cli-sample.sln && dotnet build src/cli-sample.sln -c Release --no-restore` -- takes 3-4 seconds total

### Test the Application
- Run all tests: `dotnet test src/cli-sample.sln -c Release --no-build --logger trx --results-directory ./TestResults` -- takes 10-12 seconds. NEVER CANCEL. Set timeout to 30+ seconds.
- **CRITICAL**: Tests include an intentional failure (`Intentional_failure_should_fail_build`) to trigger the auto-triage system. This is EXPECTED and should NOT be fixed.
- Expected result: 3 tests total, 2 passed, 1 failed (the intentional failure)

### Run the CLI Application
- Show usage: `dotnet run --project src/CliApp -c Release`
- Greet command: `dotnet run --project src/CliApp -c Release -- greet --name "Your Name"`
- Add command: `dotnet run --project src/CliApp -c Release -- add --a 10 --b 20`
- Each run command takes 1-2 seconds

### Code Quality and CI Requirements
- **ALWAYS** run `dotnet format src/cli-sample.sln` before committing changes
- Verify formatting: `dotnet format src/cli-sample.sln --verify-no-changes`
- The CI will fail if code is not properly formatted

## Validation Scenarios

### ALWAYS test these scenarios after making changes:
1. **CLI Functionality**: Test both greet and add commands with sample data
   - `dotnet run --project src/CliApp -c Release -- greet --name "Test User"`
   - `dotnet run --project src/CliApp -c Release -- add --a 15 --b 25`
2. **Build Validation**: Ensure clean build from scratch
   - `dotnet clean src/cli-sample.sln && dotnet restore src/cli-sample.sln && dotnet build src/cli-sample.sln -c Release --no-restore`
3. **Test Validation**: Run tests and verify the expected failure pattern
   - Tests should show: 3 total, 2 passed, 1 failed (intentional failure)
4. **Format Validation**: Check code formatting compliance
   - `dotnet format src/cli-sample.sln --verify-no-changes`

## Common Tasks

### File Structure (from repository root)
```
.
├── .github/
│   └── workflows/
│       ├── auto-triage.yml    # AI triage for failed CI runs
│       └── build.yml          # Main CI/CD pipeline
├── src/
│   ├── cli-sample.sln         # Solution file
│   ├── CliApp/
│   │   ├── CliApp.csproj      # Main CLI project
│   │   └── Program.cs         # CLI implementation (greet, add commands)
│   └── CliApp.Tests/
│       ├── CliApp.Tests.csproj # Test project with xUnit and FluentAssertions
│       └── CliTests.cs        # Tests including intentional failure
├── global.json               # Specifies .NET 9.0 SDK requirement
└── README.md                 # Documentation for AI triage system
```

### Key Project Details
- **Technology**: .NET 9.0, xUnit testing framework, FluentAssertions
- **CLI Commands**: 
  - `greet --name <NAME>` - Prints greeting
  - `add --a <INT> --b <INT>` - Adds two integers
- **Test Framework**: xUnit with FluentAssertions for readable test assertions
- **CI/CD**: GitHub Actions with AI-powered failure triage system
- **Special Feature**: Intentional test failure triggers auto-triage workflow that creates GitHub issues

### AI Triage System (Important Context)
This repository includes an advanced AI triage system that:
- Monitors all workflow failures automatically
- Downloads logs and test results (TRX files)
- Uses GitHub Models API to generate failure summaries
- Creates labeled GitHub issues with actionable failure analysis
- Optionally assigns issues to Copilot coding agents

**DO NOT** attempt to "fix" the intentional test failure - it's designed to demonstrate the triage system.

## CI/CD Pipeline Information
- Triggers on push/PR to main branch
- Runs on ubuntu-latest with .NET 9.0
- Uploads test results as TRX artifacts for triage analysis
- Formatting validation is enforced - use `dotnet format` before committing

## Troubleshooting
- **SDK not found**: Ensure .NET 9.0 SDK is installed and PATH is updated
- **Build failures**: Run `dotnet clean` followed by full restore and build
- **Format errors**: Run `dotnet format src/cli-sample.sln` to auto-fix
- **Test timeouts**: Tests can take up to 12 seconds, use adequate timeouts
- **CLI not working**: Ensure build completed successfully before running

## Time Expectations
- **NEVER CANCEL**: All build and test operations complete quickly but require patience
- Restore: 1-2 seconds
- Build: 2-3 seconds  
- Tests: 10-12 seconds (includes intentional failure)
- CLI runs: 1-2 seconds each
- Format check: <1 second