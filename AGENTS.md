# AGENTS.md — Marginalia

## Project Purpose

Browser assistant that answers questions about the active page using RAG. Extension (Chrome/Edge) + .NET backend + Semantic Kernel.

## Stack

- TypeScript (Vite) + Manifest V3
- ASP.NET Core (.NET 10 LTS)
- Vertical Slice Architecture (VSA)
- Semantic Kernel + Ollama (local) or Groq/Gemini (API)
- MediatR (dispatch: Endpoint → Handler)
- pgvector
- PostgreSQL
- Redis (optional, embedding cache)
- Docker + Docker Compose
- Railway
- Observability: Serilog + OpenTelemetry

## Repository Structure

Existing paths:

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

## General Rules

- **VSA:** each feature is independent (see Architecture Rules).
- **Language:** code, commits, and docs in **English**.
- **No cost:** use only free services (local Ollama, Groq, Gemini Flash).
- **Privacy:** page data never persists beyond session TTL.
- **Performance:** chat < 2s, ingestion < 5s.
- **Tests:** coverage > 80% on primary slices.
- **Logs:** every endpoint logs metadata only (ids, sizes, durations, status, error) via Serilog — never content.
- **Report progress:** after every task or meaningful step, always say what was done (files changed, commands run, results) before moving on.

## Architecture Rules

### Vertical Slice Architecture

- Each feature is independent and self-contained in `app/features/<Name>/`.
- Only extract to `common/` if code is used by **2 or more** features.
- Each slice has its own Endpoint, Handler, Request, and Response.
- MediatR dispatches Endpoint → Handler; Endpoint maps HTTP → command/query only.
- Handlers never know `HttpContext` — they receive only DTOs.
- Endpoints never contain business logic — only map HTTP → Handler.

### DTOs and Contracts

- Always use DTOs for input (Request) and output (Response). Never expose domain entities.
- DTOs are immutable `record` types, with validation via Data Annotations or FluentValidation.
- Requests and Responses live inside the feature folder that uses them.
- Never return `object`, `dynamic`, or anonymous types from an endpoint.

### Security and Privacy

- **Never** log access tokens, API keys, prompt content, page content, user questions, or LLM responses. Log metadata only (ids, sizes, durations, status).
- **Never** persist page data beyond session TTL.
- **Never** expose stack traces, internal error messages, or file paths in HTTP responses.
- **Never** commit API keys, connection strings, or secrets — use environment variables.
- **Always** validate and sanitize user input before processing.
- **Always** use HTTPS in production; CORS restricted to extension origin.
- Error responses follow `{ "error": "...", "code": "..." }` without internal details.

### Error Handling

- Handlers return `Result<T>`; do not throw domain exceptions for expected failures.
- Validation errors → HTTP 400. Business errors → HTTP 422. Internal errors → HTTP 500.
- Never swallow failures silently; log metadata with context (no content).

### Persistence

- Data access **only** via `DbContext` (EF Core) or `VectorStoreConfig`.
- Handlers never open connections directly — use injected services.
- EF Core migrations versioned in repository.

### AI and LLM

- All LLM and embedding access goes through `SemanticKernelConfig`.
- Prompts live in versioned files, not hardcoded in handlers.
- Always define timeout and retry with exponential backoff for external calls.
- Always limit input and output tokens to control cost and latency.

### Dependency Injection

- Services registered in `Program.cs` with explicit lifetime (`Scoped`, `Singleton`, `Transient`).
- `SemanticKernelConfig` and `VectorStoreConfig` are `Singleton`.
- `DbContext` and services using it are `Scoped`.

### Code and Style

- Modern C#: `record`, `file-scoped namespace`, nullable reference types enabled.
- No `async void`; always `async Task`.
- No generic `catch (Exception)` without rethrow or log.
- Descriptive names in English; no obscure abbreviations.
- One file per public class/record.

### Tests

- Coverage > 80% on primary slices.
- Unit tests for Handlers (with service mocks).
- Integration tests for Endpoints (with `WebApplicationFactory`).
- Contract tests for extension (Playwright optional).

## Commands (Use Docker containers when needed)

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

## Local Development

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

## Configuration

- Full template: `.env.example` (copy to `.env`); never commit `.env`.
- Key variables:
  - `SESSION_TTL_MINUTES` — page data lifetime (privacy hard limit)
  - `LLM_PROVIDER` — `Ollama` | `Groq` | `Gemini`
  - `OLLAMA_CHAT_MODEL` / `OLLAMA_EMBEDDING_MODEL` — e.g. `llama3.2`, `nomic-embed-text`
  - `OLLAMA_ENDPOINT`, `VECTOR_STORE_CONNECTION_STRING`, `DATABASE_URL` — service endpoints
  - `CORS_ALLOWED_ORIGINS` — extension + Vite dev origins
- Read config via environment / `IConfiguration` only; never hardcode secrets in code.

## Git Workflow

- **Branch naming:** `feat/<slice-name>`, `fix/<issue>`, `refactor/<area>`
- **Branches:** `develop` is the default integration branch (create it from `main` if missing); `main` is the release branch
- **Commits:** Conventional Commits (`feat:`, `fix:`, `refactor:`, `test:`, `docs:`)
- **Never commit without confirmation:** show what will be committed and ask first; wait for an explicit yes
- **PRs:** `feat/*` → `develop`, `develop` → `main` (release), require 1 approval, CI must pass
- **Squash merge** with standardized message

## Code Review

- **Required:** PRs cannot be merged without review
- **Focus:** architecture (VSA), security, tests, performance
- **Checklist:** input validation, logs without sensitive data, timeouts configured, immutable DTOs

## Versioning

- **.NET:** Pin SDK version in `global.json` (e.g., `10.0.401`, `rollForward: latestMinor`, `allowPrerelease: false`)
- **NuGet:** Fixed versions in `Directory.Packages.props` (Central Package Management)
- **Node:** `package-lock.json` committed, `engines.node` specified

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

1. Content Extraction
2. Extension Interface
3. Page Ingestion
4. Session Lifecycle
5. Contextual Q&A
6. Source Citations
7. Page Summarization
8. Health & Observability

## Do Not Do

- PDFs, authentication, multi-user, mobile, monetization.
- Model fine-tuning.
- Duplicate logic between slices (extract to `common/`).
- Persist sensitive data.
- Use paid services.
- Expose domain entities in HTTP responses.
- Execute tasks before explicit confirmation like "execute"
- Commit, push, or merge without explicit confirmation first
