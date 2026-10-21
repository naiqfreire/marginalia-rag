# AGENTS.md — Marginalia

## Project Stack

Browser assistant that answers questions about the active page using RAG. Extension (Chrome/Edge) + .NET backend + Semantic Kernel.

- TypeScript (Vite) + Manifest V3
- ASP.NET Core (.NET 10 LTS)
- Vertical Slice Architecture (VSA)
- Semantic Kernel + Ollama (local) or Groq/Gemini (API)
- MediatR (dispatch: Endpoint → Handler)
- pgvector, PostgreSQL
- Redis (optional, embedding cache)
- Docker + Docker Compose, Railway
- Observability: Serilog + OpenTelemetry
- Runtime/tooling: .NET SDK pinned in `global.json` (e.g., `10.0.401`, `rollForward: latestMinor`, `allowPrerelease: false`); NuGet fixed versions in `Directory.Packages.props` (Central Package Management); Node with committed `package-lock.json` and `engines.node` specified
- Language: code, commits, and docs in **English**
- No cost: use only free services (local Ollama, Groq, Gemini Flash)
- Performance: chat < 2s, ingestion < 5s

## Build and Test Commands

Use Docker containers when needed.

```bash
# Backend (run from app/)
dotnet run --project src/Marginalia.Api
dotnet test

# Extension
cd extension && npm run dev
npm run build
npm run typecheck
npm run lint

# Local infra
docker compose up -d

# Ollama local (before first run)
docker exec -it ollama ollama pull llama3.2
docker exec -it ollama ollama pull nomic-embed-text
```

```bash
# Run tests for a specific feature (from app/)
dotnet test --filter "FullyQualifiedName~IngestPage"
dotnet test --filter "FullyQualifiedName~AskQuestion"

# Run only unit tests
dotnet test --filter "Category=Unit"

# Run only integration tests
dotnet test --filter "Category=Integration"

# Check formatting
dotnet format --verify-no-changes

# Lint + typecheck
dotnet build --no-restore
```

- **Report progress:** after every task or meaningful step, always say what was done (files changed, commands run, results) before moving on.

## Code Style Conventions

- Modern C#: `record`, `file-scoped namespace`, nullable reference types enabled.

```csharp
namespace Marginalia.Api.Features.AskQuestion;

public sealed record AskQuestionRequest(string Question, string SessionId);
```

- No `async void`; always `async Task`.

```csharp
public async Task<AskQuestionResponse> Handle(AskQuestionRequest request, CancellationToken cancellationToken) { /* ... */ }
```

- No generic `catch (Exception)` without rethrow or log.

```csharp
catch (Exception ex)
{
    _logger.LogError(ex, "AskQuestion failed for sessionId {SessionId}", request.SessionId);
    throw;
}
```

- Descriptive names; no obscure abbreviations.
- One file per public class/record.
- DTOs are immutable `record` types, with validation via Data Annotations or FluentValidation.
- Requests and Responses live inside the feature folder that uses them.
- Never return `object`, `dynamic`, or anonymous types from an endpoint.
- Endpoints never contain business logic — only map HTTP → Handler.
- Handlers never know `HttpContext` — they receive only DTOs.
- Handlers return `Result<T>`; do not throw domain exceptions for expected failures.

```csharp
return Result.Ok(new AskQuestionResponse(answer, citations));
// expected failure:
return Result.Fail<AskQuestionResponse>(error, code: "SESSION_EXPIRED");
```

- Prompts live in versioned files, not hardcoded in handlers.

## Architecture Constraints

Each feature is independent and self-contained in `app/features/<Name>/`. Only extract to `common/` if code is used by **2 or more** features. Each slice has its own Endpoint, Handler, Request, and Response. MediatR dispatches Endpoint → Handler.

Repository structure — existing paths:

- `app/`: backend (solution, Dockerfile)
- `docker/`: local infra scripts
- `docs/`: general and feature documentation (SPEC, PLAN, CONSTITUTION, TASKS per feature)
- Root docs: `SPEC.md`, `README.md`, `AGENTS.md`, `GeneralFix.md`

Planned paths (not yet created — do not assume they exist):

- `extension/`: chrome extension

In `app/` (intended layout):

```
src/Marginalia.Api/
  features/
    IngestPage/      → Endpoint + Handler + Request + Response
    AskQuestion/     → Endpoint + Handler + Request + Response
    SummarizePage/   → Endpoint + Handler + Request + Response
  common/
    → DbContext, SemanticKernelConfig, VectorStoreConfig,
      EmbeddingService, ChunkingService
tests/
  Marginalia.Api.Tests/
  Marginalia.Core.Tests/
```

In `extension/` (planned):

```
features/
  ContentExtraction/  → Content script + background messaging
  SidePanel/          → React UI + state management
  SessionManager/     → Session lifecycle + storage
common/
  → messaging, storage, types
```

Data flow / boundaries between layers:

- Always use DTOs for input (Request) and output (Response). Never expose domain entities.
- Validation errors → HTTP 400. Business errors → HTTP 422. Internal errors → HTTP 500.
- Data access **only** via `DbContext` (EF Core) or `VectorStoreConfig`. Handlers never open connections directly — use injected services. EF Core migrations versioned in repository.
- All LLM and embedding access goes through `SemanticKernelConfig`.
- Always define timeout and retry with exponential backoff for external calls.
- Always limit input and output tokens to control cost and latency.
- Services registered in `Program.cs` with explicit lifetime (`Scoped`, `Singleton`, `Transient`).
- `SemanticKernelConfig` and `VectorStoreConfig` are `Singleton`.
- `DbContext` and services using it are `Scoped`.

Tests:

- Coverage > 80% on primary slices.
- Unit tests for Handlers (with service mocks).
- Integration tests for Endpoints (with `WebApplicationFactory`).
- Contract tests for extension (Playwright optional).

## Boundaries

Never:

- **Never** log access tokens, API keys, prompt content, page content, user questions, or LLM responses. Log metadata only (ids, sizes, durations, status, error) via Serilog; never swallow failures silently.
- **Never** persist page data beyond session TTL; sensitive data is never persisted.
- **Never** expose stack traces, internal error messages, or file paths in HTTP responses.
- **Never** commit API keys, connection strings, or secrets — use environment variables.
- Do not: PDFs, authentication, multi-user, mobile, monetization.
- Do not: model fine-tuning.
- Execute tasks before explicit confirmation like "execute".
- Commit, push, or merge — these are the user's responsibility; never run them.

Always:

- **Always** validate and sanitize user input before processing.
- **Always** use HTTPS in production; CORS restricted to configured origins.
- Error responses follow `{ "error": "...", "code": "..." }`.
- Code review checklist: input validation, logs without sensitive data, timeouts configured, immutable DTOs.

## Git Workflow

- **Branch naming:** `feat/<slice-name>`, `fix/<issue>`, `refactor/<area>`
- **Branches:** `develop` is the default integration branch (create it from `main` if missing); `main` is the release branch
- **Commits:** Conventional Commits (`feat:`, `fix:`, `refactor:`, `test:`, `docs:`)
- **Commits are the user's responsibility:** never run `git commit`, `git push`, or merge. Show the diff and propose a Conventional Commit message; the user commits.
- **PRs:** `feat/*` → `develop`, `develop` → `main` (release), require 1 approval, CI must pass
- **Squash merge** with standardized message
- **Required:** PRs cannot be merged without review. Focus: architecture (VSA), security, tests, performance.

## Configuration

- Full template: `.env.example` (copy to `.env`); never commit `.env`.
- Key variables:
  - `SESSION_TTL_MINUTES` — page data lifetime (privacy hard limit)
  - `LLM_PROVIDER` — `Ollama` | `Groq` | `Gemini`
  - `OLLAMA_CHAT_MODEL` / `OLLAMA_EMBEDDING_MODEL` — e.g. `llama3.2`, `nomic-embed-text`
  - `OLLAMA_ENDPOINT`, `VECTOR_STORE_CONNECTION_STRING`, `DATABASE_URL` — service endpoints
  - `CORS_ALLOWED_ORIGINS` — extension + Vite dev origins
- Read config via environment / `IConfiguration` only; never hardcode secrets in code.

## Documents

- `SPEC.md` — project overview (root)
- `docs/features/<Name>/SPEC.md` — feature specification
- `docs/features/<Name>/PLAN.md` — implementation plan
- `docs/features/<Name>/CONSTITUTION.md` — principles and constraints
- `docs/features/<Name>/TASKS.md` — atomic tasks

Each SPEC.md must contain:

- Feature
- Objective
- Business Requirements
- Constraints
- Acceptance Criteria

Each PLAN.md must contain:

- Architecture
- Security Strategies
- Technical Structure
- Dependencies
- Configuration

(Only applicable documents per feature.)

## Implementation Order

Follows `SPEC.md` §5.1 dependency map — do not reorder without updating SPEC.md, README.md, and feature docs together.

1. Content Extraction
2. Session Lifecycle
3. Page Ingestion
4. Contextual Q&A
5. Source Citations
6. Page Summarization
7. Extension Interface
8. Health & Observability

## File Modification Permission Policy

- Never create, edit, delete, move, rename, or overwrite any file without explicit permission from the user.
- Before any file operation, tell the user which file(s) will change, what will change and why, the expected outcome and scope, and whether multiple files are involved (and why they must change together).
- Then ask explicitly: "May I proceed with these file changes?" and wait for explicit approval (e.g., "yes", "proceed", "approved"). Silence, implied consent, or unrelated replies are not approval.
- Multiple files may be changed only when they belong to the same context/feature and the multi-file change is necessary; list and justify every file.
- If the scope changes or additional files become necessary after approval, stop and ask for permission again.
- Never make extra file changes beyond what was explicitly approved.
- Proposing code or file contents in chat is allowed, but writing them to disk requires permission.
