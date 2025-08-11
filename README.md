# ubiquitous-garbanzo

AI triage for failed GitHub Actions runs

## AI triage for failed GitHub Actions runs

This document explains what the workflow `auto-triage.yml` does, how it’s wired, the required inputs, and how it evolved across iterations so you know why each decision was made.

---

## What it does (high level)

- Listens for completed workflow runs and only executes when the run concluded as failure.
- Collects logs and the `test-results` artifact (if present).
- Parses TRX test results to extract failed tests with messages and stack traces.
- Builds a prompt that includes:
  - Repo, branch and commit
  - AI goal statement
  - Parsed test failures (TRX)
  - Discovered artifacts
  - Head and tail of the first few log files
- Calls GitHub Models to generate a short, actionable failure summary.
- Creates a labeled GitHub issue with the AI summary and run metadata.
- Optionally assigns the issue to the Copilot coding agent when a PAT is provided and the agent is enabled for the repo.

---

## Trigger and permissions

- Trigger: `on.workflow_run` with `types: [completed]` and `workflows: ["*"]` so it can react to any workflow in the repository. The job additionally gates on `if: github.event.workflow_run.conclusion == 'failure'`.
- Permissions (job-level):
  - `actions: read` to fetch run logs and artifacts
  - `contents: read` to interact with repo resources
  - `issues: write` to create and update issues
  - `models: read` to call GitHub Models API

---

## Job environment and prerequisites

- Runner: `ubuntu-24.04`.
- Job env:
  - `PAT_WITH_ISSUES_WRITE`: mapped from `secrets.PAT_WITH_ISSUES_WRITE` for conditional execution of the assignment step.
- Secrets required:
  - `GITHUB_TOKEN`: provided by Actions; used for logs/artifact access, model calls, and issue creation.
  - `PAT_WITH_ISSUES_WRITE` (optional): a user PAT with permission to assign issues. Required only if you want the optional Copilot assignment to run.

---

## Step-by-step

### 1) Set vars

Captures run id, run URL, repo, branch and SHA into step outputs for reuse.

### 2) Download logs (zip)

Uses `GITHUB_TOKEN` to GET the run logs archive, unzips to `logs/`. If no logs are found, it logs a message and continues.

### 3) Download test-results artifact

Attempts to download an artifact named `test-results` into `artifacts/test-results`. This is optional; if missing, the step continues.

### 4) Parse TRX → failures.md

- Runs a PowerShell script to find `*.trx` under the test-results path.
- Extracts failed test cases, including duration, message, stack, and source file.
- Writes a concise, readable `failures.md`. If no failed tests are found, the file notes that explicitly.

### 5) Build AI prompt (includes TRX + logs)

Creates `prompt.txt` that includes:
- Run metadata (repo/branch/SHA and run URL)
- A short instruction (goal)
- The rendered TRX failures block
- An artifact inventory
- Head and tail (60 lines each) of up to three `*.txt` logs

### 6) Call GitHub Models (chat completions)

- Targets `https://models.github.ai/inference/chat/completions` (GitHub Models API)
- Uses a primary model (`openai/gpt-4o`) with a fallback to `openai/gpt-4o-mini` when the response contains an error object.
- Extracts the assistant content to `summary.md`, defaulting to a placeholder if empty.

### 7) Ensure labels exist

Creates two labels (idempotent):
- `ci-failure` (red) — used to mark CI pipeline failures
- `needs-triage` (light purple) — used to signal triage is needed

### 8) Create issue

- Constructs a concise title including the workflow name and branch.
- POSTs to `repos/{owner}/{repo}/issues` with labels and the AI summary.
- Captures the created issue number in a step output for downstream use.

### 9) Assign to Copilot coding agent (optional)

Runs only when `PAT_WITH_ISSUES_WRITE` is set (checked via `if: ${{ env.PAT_WITH_ISSUES_WRITE != '' }}`).
- Uses GraphQL `repository.suggestedActors(capabilities:[CAN_BE_ASSIGNED])` to discover candidate actors.
- Looks for common Copilot agent logins (e.g., `copilot-swe-agent`, `copilot-agent`, `copilot`) and extracts the actor id.
- Resolves the issue node id via GraphQL and assigns using the `replaceActorsForAssignable` mutation.
- Skips with a clear message if the agent isn’t suggested/available.

Notes:
- Copilot assignment requires the Copilot coding agent to be enabled and assignable for this repository or organization.
- A user PAT is used here instead of `GITHUB_TOKEN` because assigning actors may require broader scopes than the default token grants.

---

## Configuration and customization

- Scope the trigger: Replace `workflows: ["*"]` with a curated list if you only want to triage certain pipelines.
- Artifact names: If your CI exports results under a different artifact name, update the `gh run download` step.
- Log sampling: Adjust the number of files or the `head/tail` line counts in the prompt builder.
- Models: Change the primary/fallback models if your org has access to different models in GitHub Models.
- Labels: Modify or add labels in the ensure-labels step to fit your triage process.
- Assignment: You can switch from the GraphQL assignment to a simple REST assignee update (for human users) if the agent is not used.

---

## Security considerations

- Least privilege: The workflow uses minimal permissions by default (actions/contents read, issues write, models read).
- PAT usage: Only required for the optional assignment step; keep the PAT scoped narrowly (repo:write for issues/assign).
- Data exposure: The prompt includes snippets of logs and test failures. Avoid embedding secrets into logs or test messages.

---

## Troubleshooting

- Invalid workflow expression referencing `secrets` in `if` conditions:
  - Fixed by mapping the secret to a job-level env var and checking `env.PAT_WITH_ISSUES_WRITE` instead.
- Model access errors (e.g., unsupported/unauthorized model):
  - The flow retries with a fallback model and logs the reason.
- No artifacts/logs:
  - Issues still get created with the best available context and an explicit note that artifacts/logs were missing.
- Copilot assignment fails:
  - Ensure the Copilot coding agent is enabled and assignable and the PAT has permissions to assign issues.

---

## Key changes from the initial version

- Trigger scope simplified to `workflows: ["*"]` with a failure-only guard at the job level.
- Switched to GitHub Models endpoint `models.github.ai` with primary/fallback model logic (`gpt-4o` → `gpt-4o-mini`).
- Added robust TRX parsing to present failing tests in a friendly format.
- Prompt builder expanded to include artifacts and sampled logs.
- Label creation made idempotent and standardized (`ci-failure`, `needs-triage`).
- Issue creation switched to REST and captures the issue number deterministically for subsequent steps.
- Optional assignment step:
  - Uses GraphQL `suggestedActors` to discover assignable actors.
  - Assigns via GraphQL `replaceActorsForAssignable` with a user PAT.
  - Step is conditionally enabled using env-based check to avoid invalid `secrets` references in `if`.
- General hardening: clearer messages, resilient fallbacks, and minimal required permissions.

---

## Quick reference

- Workflow file: `.github/workflows/auto-triage.yml`
- Inputs:
  - `secrets.GITHUB_TOKEN` (built-in)
  - `secrets.PAT_WITH_ISSUES_WRITE` (optional; enables Copilot assignment)
- Outputs:
  - Creates a GitHub issue with labels `ci-failure` and `needs-triage`
  - Optional: assigns it to Copilot coding agent when available
