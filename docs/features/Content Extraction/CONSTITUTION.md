# CONSTITUTION.md — Content Extraction

Principles and constraints that bind every implementation and review of this feature.

## Principles

1. **Backend owns the HTTP contract.** Extension fills fields only; `Page Ingestion` defines and validates the DTO shape and limits.
2. **Never trust the client.** Sanitize and validate server-side (size, required fields, session); client errors must not take down the API.
3. **Metadata-only logs.** Log ids, sizes, durations, status, error codes — never page content, payload bodies, or user text (Serilog).
4. **No page persistence in the extension.** Do not store extracted text in `chrome.storage` or elsewhere beyond the in-flight request; page data lifetime is backend session TTL only.
5. **Non-invasive content script.** Read the DOM; do not mutate page scripts, styles, or event handlers.
6. **Manifest V3 only.** Service worker + content script + messages; no legacy background pages, no remote code.
7. **Free stack.** Local Ollama / free APIs only; no paid extraction or enrichment services.
8. **Exact CORS origins.** Allowlist `chrome-extension://<id>` and `http://localhost:5173` from configuration; never `chrome-extension://*`.
9. **Config over secrets.** Read `API_BASE_URL`, `CORS_ALLOWED_ORIGINS`, `MAX_PAGE_WORDS` from environment / `IConfiguration`; never hardcode keys or commit `.env`.
10. **Error shape.** Responses are `{ "error": "...", "code": "..." }` — no stack traces, file paths, or internal messages.

## Constraints

- Out of scope: PDFs, auth, multi-user, mobile, monetization, fine-tuning.
- Input cap: `MAX_PAGE_WORDS` (default 10000) aligned with SPEC NFRs (ingestion < 5s for ≤ 10k words).
- Definition of Done (rule A): production code works with dependencies (stub endpoint, CORS config); automated tests separate and still required before merge per AGENTS.md.
- Predecessors: none. Successors: Session Lifecycle (7), Page Ingestion (3).

## Non-goals

- Owning or freezing the full backend DTO (deferred to Page Ingestion).
- Rendering answers, summaries, or citations (Extension Interface / Q&A / Summarization / Citations).
- Cross-session history or persistence beyond backend TTL.
