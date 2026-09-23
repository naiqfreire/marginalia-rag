# Marginalia

A browser assistant that lets you understand any web page without leaving it. Built on RAG (Retrieval-Augmented Generation) with focus on privacy, low latency, and zero cost.

## What It Does

Marginalia adds a side panel to Chrome/Edge that lets you:

- **Ask questions** about the page you're reading — get answers grounded in that page's content
- **Summarize** long articles, documentation, or tutorials in seconds
- **See citations** — every answer references the exact text spans used

No copying text to another tab. No losing context. No sending your data to generic AI services.

## Architecture

```
┌─────────────────────────────────┐     HTTP/JSON     ┌─────────────────────────────────┐
│  Browser Extension (TypeScript) │ ◄───────────────► │  Backend (.NET / ASP.NET Core)  │
│                                 │                   │                                 │
│ • Content script extracts text  │                   │ • Vertical Slice Architecture   │
│ • Service worker orchestrates   │                   │ • Semantic Kernel + RAG         │
│ • React side panel UI           │                   │ • pgvector + PostgreSQL         │
└─────────────────────────────────┘                   │ • Ollama / Groq / Gemini        │
                                                      └─────────────────────────────────┘
```

## Tech Stack

| Layer         | Technology                                              |
| ------------- | ------------------------------------------------------- |
| Extension     | TypeScript, Chrome Manifest V3, React                   |
| Backend       | ASP.NET Core (.NET 10 LTS), Vertical Slice Architecture |
| AI            | Semantic Kernel, Ollama (local) or Groq/Gemini (API)    |
| Vector DB     | pgvector                                                |
| Relational DB | PostgreSQL                                              |
| Container     | Docker + Docker Compose                                 |
| Cloud         | Railway (free tier)                                     |
| Observability | Serilog, OpenTelemetry                                  |

## Features (In Progress)

1. Content Extraction — captures main page content
2. Extension Interface — side panel with chat and summarization
3. Page Ingestion — chunks, embeds, stores in vector DB
4. Contextual Q&A — RAG over active page
5. Source Citations — references to exact text spans
6. Page Summarization — structured summaries
7. Session Lifecycle — auto-cleanup after TTL
8. Health & Observability — logs, metrics, health checks

## Status

Early development. Implementation order follows the feature list above.
