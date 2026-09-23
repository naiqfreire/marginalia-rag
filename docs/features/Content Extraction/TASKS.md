# TASKS.md — Content Extraction

Atomic tasks. Each ends in independently runnable production code (DoD rule A: works with dependencies; no automated test tasks in scope — tests remain required before merge per AGENTS.md, tracked separately). Confirm before each commit.

**Working directory:** repo root unless noted. Backend commands from `app/`.

---

## Task 1 — Scaffold `extension/` (Vite + TS + MV3)

**Files:** `extension/package.json`, `extension/vite.config.ts`, `extension/tsconfig.json`, `extension/manifest.json`, `extension/index.html` (if needed for tooling), `.gitignore` entries for `extension/dist`, `extension/node_modules`

**Done when:** `npm install` + `npm run build` produce `extension/dist`; manifest loads as unpacked MV3 extension in Chrome/Edge (empty content script OK).

**Steps:**
- [ ] Create Vite + TypeScript project under `extension/` with `npm run build`, `npm run dev` scripts.
- [ ] Add `manifest.json`: `manifest_version: 3`, `permissions: ["activeTab", "scripting"]`, `background.service_worker`, `content_scripts` on `<all_urls>` (read-only match for extraction).
- [ ] Add content script + background entry points (stubs logging metadata only).
- [ ] Verify load unpacked → no manifest errors.

---

## Task 2 — Content script: extract main text / title / url

**Files:** `extension/src/features/ContentExtraction/content.ts`, `extension/src/features/ContentExtraction/types.ts`

**Done when:** On an article-like page, extracted payload has non-empty `text`, `url`, `title`.

**Steps:**
- [ ] Implement extraction: prefer `<article>` / `main` / readability-style largest text block; strip `script`, `style`, `nav`, `footer`, `aside`, hidden nodes.
- [ ] Capture `document.title` and `location.href`.
- [ ] Enforce `MAX_PAGE_WORDS` client-side fast path (trim or flag for server 422).
- [ ] Send `chrome.runtime.sendMessage({ type: "content-extracted", payload })`.
- [ ] Manual check: load a docs/news article; confirm message in service worker console (metadata only — do not log full text).

---

## Task 3 — Service worker: build payload + POST `/api/ingest`

**Files:** `extension/src/features/ContentExtraction/background.ts`, `extension/src/common/config.ts`, `extension/src/common/messaging.ts`

**Done when:** Worker POSTs to `{API_BASE_URL}/api/ingest` and surfaces 2xx / failure without storing page text.

**Steps:**
- [ ] Receive content message; build `{ sessionId, url, title, text }` (sessionId: ephemeral UUID per tab/session — full lifecycle is feature 7).
- [ ] Read `API_BASE_URL` / `VITE_API_BASE_URL` from build-time env.
- [ ] `fetch` POST JSON with timeout + limited retry/backoff; no body logging.
- [ ] Report success/failure to console as metadata (status, duration, byte length) only.

---

## Task 4 — Backend slice `IngestPage` (`POST /api/ingest`)

**Files:**
- Create: `app/src/Marginalia.Api/features/IngestPage/IngestPageRequest.cs`
- Create: `app/src/Marginalia.Api/features/IngestPage/IngestPageResponse.cs`
- Create: `app/src/Marginalia.Api/features/IngestPage/IngestPageHandler.cs`
- Create: `app/src/Marginalia.Api/features/IngestPage/IngestPageEndpoint.cs`
- Modify: `app/src/Marginalia.Api/Marginalia.Api.csproj` (FluentValidation if used directly)
- Modify: `app/src/Marginalia.Api/Program.cs` (map endpoint)

**Done when:** Valid body → 200; missing fields → 400 `{error,code}`; over `MAX_PAGE_WORDS` → 422 `{error,code}`; logs metadata only.

**Steps:**
- [ ] `record IngestPageRequest(string SessionId, string Url, string Title, string Text)` (+ validation attributes or FluentValidation rules).
- [ ] `record IngestPageResponse(string SessionId, bool Accepted)`.
- [ ] Handler: validate → word count vs `MAX_PAGE_WORDS` → `Result.Ok` / `Result.Fail` (no domain exceptions for expected failures).
- [ ] Endpoint: `app.MapPost("/api/ingest", ...)` — HTTP → MediatR only; map status codes (400/422/500) to `{ error, code }`.
- [ ] Serilog: log sessionId (or hash), text length, duration — never content.
- [ ] Run from `app/`: `dotnet build --no-restore` and `dotnet run --project src/Marginalia.Api`; curl happy path + 400 + 422.

---

## Task 5 — CORS exact-origin allowlist

**Files:** Modify `app/src/Marginalia.Api/Program.cs`, document in `extension/README.md` or PLAN (already covered); ensure `CORS_ALLOWED_ORIGINS` in `.env` / compose.

**Done when:** Preflight from allowed origin OK; disallowed origin blocked; no `chrome-extension://*` in policy.

**Steps:**
- [ ] Read `CORS_ALLOWED_ORIGINS` (comma-separated) via `IConfiguration`.
- [ ] Add CORS policy: `WithOrigins(...)` exact list; allow required headers/methods for JSON POST.
- [ ] Apply before endpoint mapping; Development logging of rejected origin metadata only.
- [ ] Document: copy extension ID from `chrome://extensions` into `.env` → restart API.

---

## Task 6 — End-to-end manual verification (no automated tests)

**Done when:** All checks pass; report results (files, commands, outcomes) before any commit.

**Checklist:**
- [ ] Article page → extension extract → POST → 200.
- [ ] Missing `title`/`text` → 400 `{error,code}`.
- [ ] Text over `MAX_PAGE_WORDS` → 422 `{error,code}`.
- [ ] Request with origin not on allowlist → CORS failure.
- [ ] API logs: path, status, duration, size only — grep confirms no article body.
- [ ] `dotnet build --no-restore` clean; extension `npm run build` clean.

---

## Out of Scope (this feature)

- Chunking, embeddings, vector DB, SK — Page Ingestion (3).
- Session TTL / tab-close cleanup — Session Lifecycle (7).
- Side panel chat/summarize UI — Extension Interface (2).
- Automated unit/integration/contract tests — separate track; required before merge per AGENTS.md.
