# SPEC.md — Marginalia

## 1. Overview

Marginalia is a browser assistant that lets users understand any web page without leaving it. It extracts the main content of the active page, processes it in a .NET backend, and delivers contextual answers, summaries, and citations in a side panel. Built on RAG (Retrieval-Augmented Generation) principles, with focus on privacy, low latency, and zero cost.

---

## 2. Problem

Understanding a long page — technical article, documentation, news, contract, or tutorial — is slow and fragmented:

- Users must scroll, go back, and re-read sections.
- For quick answers, people copy text into a generic ChatGPT.
- This breaks context, loses formatting, consumes tokens, and exposes sensitive data.
- Generic assistants don't know which page is active, so they give vague answers.
- Users waste time and often give up on understanding the content.

**Pain point summary:** No tool understands the page the user is reading right now and answers questions directly about it, without leaving the browser.

---

## 3. Solution

Marginalia has two components:

1. **Browser Extension (Chrome/Edge):** captures active page content, sends to backend, displays answers in a side panel.
2. **Backend .NET (ASP.NET Core + Semantic Kernel):** receives content, chunks it, generates embeddings, stores in vector database, and uses RAG to answer questions, generate summaries, and produce citations.

**In one sentence:** A "ChatGPT for the current page," built as a real software engineering system.

---

## 4. Scope

### 4.1. In Scope

- Browser extension for Chrome and Edge (Manifest V3).
- Extraction of main content from HTML pages.
- Backend in ASP.NET Core with REST API.
- Pipeline for chunking, embeddings, and vector storage.
- Contextual chat with RAG over active page content.
- Structured page summarization.
- Citations for text spans used in answers.
- Data isolation per session with automatic cleanup (TTL).
- Health checks, structured logs, and basic metrics.
- Deployment to cloud free tier.
- Complete documentation on GitHub (README, diagrams, demo video).

### 4.2. Out of Scope

- PDFs, local files, or emails support.
- User authentication and multi-user.
- Language model fine-tuning.
- Integration with external databases or third-party APIs.
- Mobile version (iOS/Android).
- Monetization, paid plans, or billing.
- Multi-language UI support.
- Persistent conversation history across sessions.

---

## 5. Features

Marginalia consists of 8 features, organized in three groups. Each feature has its own `SPEC.md`, `PLAN.md`, `CONSTITUTION.md`, and `TASKS.md`.

### Group 1 — Browser Extension

| #   | Feature                 | Description                                                          |
| --- | ----------------------- | -------------------------------------------------------------------- |
| 1   | **Content Extraction**  | Extension captures main content of active page and sends to backend. |
| 2   | **Extension Interface** | Side panel provides chat, summarize button, and loading feedback.    |

### Group 2 — Backend (RAG Core)

| #   | Feature                | Description                                                                           |
| --- | ---------------------- | ------------------------------------------------------------------------------------- |
| 3   | **Page Ingestion**     | Backend receives content, chunks it, generates embeddings, stores in vector database. |
| 4   | **Contextual Q&A**     | User asks questions, receives answers grounded only in page content.                  |
| 5   | **Page Summarization** | User requests structured summary of entire page.                                      |
| 6   | **Source Citations**   | Each answer includes references to specific page spans used to generate it.           |

### Group 3 — Infrastructure & Operations

| #   | Feature                    | Description                                                              |
| --- | -------------------------- | ------------------------------------------------------------------------ |
| 7   | **Session Lifecycle**      | Page data isolated per session, auto-removed after TTL or tab close.     |
| 8   | **Health & Observability** | System exposes health checks, structured logs, latency and cost metrics. |

### 5.1. Dependency Map

```

Content Extraction (1) ──► Page Ingestion (3) ──► Contextual Q&A (4) ──► Source Citations (6)
│ │
│ └──► Page Summarization (5)
│
└──► Session Lifecycle (7)

Extension Interface (2) ──► consumes features 4, 5, and 6

Health & Observability (8) ──► cross-cutting across all features

```

### 5.2. Implementation Order

1. Content Extraction (1)
2. Extension Interface (2)
3. Page Ingestion (3)
4. Session Lifecycle (7)
5. Contextual Q&A (4)
6. Source Citations (6)
7. Page Summarization (5)
8. Health & Observability (8)

---

## 6. High-Level Architecture

```

┌──────────────────────────────────────────────────────────────┐
│ BROWSER (Chrome/Edge)                                        │
│                                                              │
│ ┌────────────────┐ ┌────────────────────────────┐           │
│ │ Content Script │───────►│ Service Worker       │           │
│ │ (extracts text)│ │ (orchestrates comms)       │           │
│ └────────────────┘ └─────────────┬──────────────┘           │
│                                  │                            │
│ ┌─────────────▼──────────────┐   │                            │
│ │ Side Panel (chat UI)       │   │                            │
│ └─────────────┬──────────────┘   │                            │
└──────────────────────────────────┼────────────────────────────┘
                                   │ HTTP/JSON
                                   ▼
┌──────────────────────────────────────────────────────────────┐
│ BACKEND (.NET / ASP.NET Core)                                │
│                                                              │
│ ┌────────────────────────────────────────────────────────┐   │
│ │ Vertical Slices (Features)                             │   │
│ │ IngestPage │ AskQuestion │ SummarizePage               │   │
│ └────────────────────────────────────────────────────────┘   │
│                                                              │
│ ┌────────────────────────────────────────────────────────┐   │
│ │ Common Services                                        │   │
│ │ ChunkingService │ EmbeddingService │ VectorStore       │   │
│ │ DbContext │ SemanticKernelConfig                       │   │
│ └────────────────────────────────────────────────────────┘   │
│                                                              │
└──────┬──────────────────────┬──────────────────────┬─────────┘
       │                      │                      │
       ▼                      ▼                      ▼
┌─────────────┐      ┌──────────────┐      ┌──────────────┐
│ Vector DB   │      │ Relational   │      │ LLM          │
│ (pgvector)  │      │ (PostgreSQL) │      │ (Ollama /    │
│             │      │              │      │  API)        │
└─────────────┘      └──────────────┘      └──────────────┘

```

---

## 7. Tech Stack

| Layer               | Technology                           | Justification                                                |
| ------------------- | ------------------------------------ | ------------------------------------------------------------ |
| Extension           | TypeScript + Chrome Manifest V3      | Modern standard, strong typing, native Chrome integration.   |
| Backend             | ASP.NET Core (.NET 10 LTS)           | Primary stack, strong typing, performance, mature ecosystem. |
| Architecture        | Vertical Slice Architecture (VSA)    | Feature-based organization, delivery velocity.               |
| AI Orchestration    | Semantic Kernel                      | Official Microsoft SDK for RAG in .NET.                      |
| Vector Database     | pgvector                             | Efficient semantic search; simplifies infrastructure.        |
| Relational Database | PostgreSQL                           | Metadata, sessions, cache; consistent with vector DB.        |
| Cache               | Redis (optional)                     | Reduce latency and embedding cost.                           |
| LLM                 | Ollama (local) or Groq/Gemini (API)  | Zero-cost response generation.                               |
| Embeddings          | sentence-transformers (local) or API | Zero-cost text-to-vector conversion.                         |
| Container           | Docker + Docker Compose              | Portability and environment reproducibility.                 |
| Cloud               | Railway                              | Free-tier deployment.                                        |
| Observability       | Serilog + OpenTelemetry              | Structured logs and latency/cost metrics.                    |

---

## 8. Non-Functional Requirements

| Category            | Requirement                   | Target                                                  |
| ------------------- | ----------------------------- | ------------------------------------------------------- |
| **Performance**     | Average chat response latency | < 2 seconds                                             |
| **Performance**     | Page ingestion latency        | < 5 seconds for pages up to 10,000 words                |
| **Cost**            | Monthly operational cost      | $0.00 (free services only)                              |
| **Privacy**         | Page data                     | Never persisted beyond session TTL                      |
| **Privacy**         | Sensitive data                | Not sent to third-party APIs when local model available |
| **Scalability**     | Concurrent users (MVP)        | Support at least 10 users without degradation           |
| **Reliability**     | Backend availability          | 95% in production                                       |
| **Quality**         | Test question accuracy rate   | > 80% (manual evaluation)                               |
| **Maintainability** | Test coverage                 | > 80% on primary slices                                 |
| **Observability**   | Structured logs               | 100% of endpoints log input, output, and error          |

---

## 9. Constraints

- **Timeline:** 2 months (until December 2026).
- **Team:** 1 solo developer, no prior professional experience.
- **Budget:** $0.00 for paid services.
- **Code language:** English (industry standard).
- **Documentation language:** English (for portfolio).
- **Target platform:** Chrome and Edge (Manifest V3).
- **Development OS:** Windows, Linux, or macOS.

---

## 10. Assumptions

- User has Chrome or Edge installed.
- User has internet connection (unless local model).
- Backend accessible via HTTPS in production.
- Ollama (or equivalent API) available in runtime environment.
- Developer has basic knowledge of C#, TypeScript, and Docker.
- Target web pages are articles, documentation, news — not complex SPAs.

---

## 11. Version Info

**Version:** 1.0
**Date:** 2026-09-23
**Author:** [Naiaque]
**Status:** Approved for implementation
