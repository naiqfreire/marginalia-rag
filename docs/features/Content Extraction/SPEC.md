# SPEC.md — Content Extraction

## Feature

**Content Extraction** (ID 1) — Browser extension captures the main content of the active page and sends it to the backend via `POST /api/ingest`.

## Objective

Extract the readable main content of the current tab (article, documentation, news) in Chrome/Edge (Manifest V3), package it as the backend-owned payload, and deliver it to `POST /api/ingest` so downstream features (Session Lifecycle, Page Ingestion, Q&A, Summarization, Citations) have page content to work with. Extraction works with no product predecessors; the receiving stub unblocks Page Ingestion (ID 3).

## Business Requirements

- Extract main page text only (exclude navigation, footers, ads, scripts, styles, hidden UI).
- Include at least: `sessionId`, `url`, `title`, `text`.
- Enforce `MAX_PAGE_WORDS` (default `10000`) before send; reject/trim at the contract limit owned by the backend.
- Send from the extension service worker (background), not from the page context.
- Target article-like pages (articles, documentation, news) — not complex SPA shells.
- English for code, messages, and docs; free services only (no paid extraction APIs).
- Never log page content, user questions, or payload bodies — metadata only (ids, sizes, durations, status).

## Constraints

- Chrome/Edge Manifest V3 only; no PDF, local files, auth, or mobile.
- Content script must not mutate page DOM/JS or break the host page.
- HTTP payload shape is **owned by the backend** (`Page Ingestion` hardens validation); extension only fills allowed fields.
- Config via environment / `.env` only (`API_BASE_URL`, `CORS_ALLOWED_ORIGINS`, `MAX_PAGE_WORDS`); never commit `.env` or secrets.
- CORS must use exact origins (`chrome-extension://<extension-id>` + `http://localhost:5173`) — not `chrome-extension://*`.
- Errors to callers: `{ "error": "...", "code": "..." }` only — no stack traces or internal paths.
- Predecessors: none. Successors: Session Lifecycle (7), Page Ingestion (3).

## Acceptance Criteria

- [ ] On an article-like page, content script yields non-empty main `text` plus `url` and `title`.
- [ ] Service worker builds the payload and `POST /api/ingest` returns 2xx against the feature-1 stub.
- [ ] Missing required fields or malformed body → HTTP 400 with `{ "error", "code" }`.
- [ ] Payload exceeding `MAX_PAGE_WORDS` → HTTP 422 with `{ "error", "code" }`.
- [ ] Request from an origin not in the allowlist is rejected by CORS.
- [ ] Request logs show metadata only (path, status, duration, size) — no page text.
- [ ] Definition of Done (rule A): complete production code that works correctly with stub + config dependencies; automated tests tracked separately (still required before merge per AGENTS.md).
