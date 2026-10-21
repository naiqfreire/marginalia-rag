# PLAN.md — Content Extraction

## Architecture

```
Content Script ──chrome.runtime──► Service Worker ──HTTP POST──► POST /api/ingest
 (extract main text)               (build payload,                 (VSA slice IngestPage
                                    timeout/retry)                  stub: validate + Result)
```

1. Content script reads the active tab’s DOM and extracts main readable text + `title` + `url`.
2. Content script sends a message to the service worker (MV3 background).
3. Service worker builds `{ sessionId, url, title, text }` (backend-owned field set), enforces client-side `MAX_PAGE_WORDS` as a fast path, and `fetch`es `{API_BASE_URL}/api/ingest` with timeout + bounded retry.
4. Endpoint maps HTTP → MediatR command → Handler: FluentValidation, metadata-only logs, `Result` — returns 2xx / 400 / 422 / 500 with `{ error, code }` on failure.
5. No chunking, embeddings, or vector storage in this feature (Page Ingestion, ID 3).

## Security Strategies

- **Contract ownership:** backend `IngestPage` Request DTO is authoritative; extension conforms.
- **Validation:** FluentValidation for required fields; size/word limit → 422; malformed JSON → 400; unexpected → 500 (no details leaked).
- **CORS:** allowlist from `CORS_ALLOWED_ORIGINS` (comma-separated exact origins): `chrome-extension://<extension-id>` + `http://localhost:5173`. Reflect only allowlisted origins; never `chrome-extension://*`. Document obtaining ID from `chrome://extensions` → `.env` (uncommitted).
- **Transport:** HTTPS in production; CORS restricted to extension + Vite dev origins (AGENTS).
- **Logging:** Serilog metadata only (path, status, duration, content length) — never body/text.
- **Secrets:** config via `IConfiguration` / env only; no keys in repo.
- **HTTP client limits:** timeout (`LLM_TIMEOUT_SECONDS` pattern or dedicated fetch timeout); limited retries with backoff; cap tokens/words via `MAX_PAGE_WORDS`.
- **Privacy:** extension does not persist extracted text; backend session TTL is feature 7’s concern (not implemented here).

## Technical Structure

**Extension (create `extension/` — planned path):**

```
extension/
  package.json
  vite.config.ts
  tsconfig.json
  manifest.json                 # MV3: content_scripts, background.service_worker
  src/
    features/ContentExtraction/
      content.ts                # extract main text/title/url → runtime message
      background.ts             # receive message, build payload, POST /api/ingest
      types.ts                  # payload types mirroring backend contract (thin)
    common/
      messaging.ts
      config.ts                 # API_BASE_URL from VITE_API_BASE_URL
```

**Backend (modify existing host):**

```
app/src/Marginalia.Api/
  features/IngestPage/
    IngestPageEndpoint.cs       # MapPost("/api/ingest") — HTTP → MediatR only
    IngestPageHandler.cs        # validate, log metadata, Result
    IngestPageRequest.cs        # immutable record (backend-owned shape)
    IngestPageResponse.cs
  Program.cs                    # add CORS allowlist policy from configuration
```

- MediatR already registered in `Program.cs`; add handler registration via existing assembly scan.
- FluentValidation package version already in `Directory.Packages.props`; reference in Api csproj when implementing.
- One public type per file; file-scoped namespaces; `record` DTOs (AGENTS style).

## Dependencies

- **Extension:** Chrome/Edge MV3, TypeScript, Vite; `chrome.tabs` / scripting / runtime messaging.
- **Backend:** ASP.NET Core, MediatR (wired), Serilog (wired), FluentValidation (CPM ready), OpenAPI optional for dev.
- **Infra:** none new — `API_BASE_URL` (e.g. `http://localhost:5000`), compose `api` service already defined.
- **Predecessors:** none. **Successors:** Session Lifecycle (7), Page Ingestion (3) — replaces stub validation with persistence/chunking.

## Configuration

| Variable | Purpose | Source |
|----------|---------|--------|
| `API_BASE_URL` | Base URL for `POST /api/ingest` | `.env` / extension env |
| `VITE_API_BASE_URL` | Vite dev absolute API URL if not injected | `.env` |
| `CORS_ALLOWED_ORIGINS` | Exact origins: `chrome-extension://<id>,http://localhost:5173` | `.env` / compose |
| `MAX_PAGE_WORDS` | Word cap (default 10000) — client fast-path + server 422 | `.env` |
| `ASPNETCORE_ENVIRONMENT` | Development → OpenAPI map | compose |

Copy `.env.example` → `.env`; never commit `.env`. After loading unpacked extension, paste real ID into `CORS_ALLOWED_ORIGINS`.

## Out of Scope

- EF Core, pgvector, Semantic Kernel, Redis — Page Ingestion / Q&A.
- Side panel UI — Extension Interface (ID 2).
- Session TTL cleanup — Session Lifecycle (ID 7).
- Automated test suites in DoD (rule A); tests required before merge per AGENTS.md separately.
