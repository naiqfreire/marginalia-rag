import { API_BASE_URL, INGEST_PATH, MAX_RETRIES, REQUEST_TIMEOUT_MS } from "../../common/config";
import { isContentExtracted, type ContentExtractedMessage } from "../../common/messaging";

function newSessionId(): string {
  return crypto.randomUUID();
}

async function postIngest(body: unknown): Promise<{ status: number; durationMs: number; bytes: number }> {
  const json = JSON.stringify(body);
  const bytes = new TextEncoder().encode(json).byteLength;
  let lastStatus = 0;
  for (let attempt = 0; attempt <= MAX_RETRIES; attempt++) {
    const started = performance.now();
    try {
      const res = await fetch(`${API_BASE_URL}${INGEST_PATH}`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: json,
        signal: AbortSignal.timeout(REQUEST_TIMEOUT_MS),
      });
      const durationMs = Math.round(performance.now() - started);
      lastStatus = res.status;
      if (res.ok) return { status: res.status, durationMs, bytes };
      if (res.status >= 400 && res.status < 500) {
        return { status: res.status, durationMs, bytes };
      }
    } catch {
      lastStatus = 0;
    }
    await new Promise((r) => setTimeout(r, 200 * 2 ** attempt));
  }
  return { status: lastStatus, durationMs: 0, bytes };
}

chrome.runtime.onMessage.addListener((message: unknown) => {
  if (!isContentExtracted(message)) return;
  const msg = message as ContentExtractedMessage;
  const body = {
    sessionId: newSessionId(),
    url: msg.payload.url,
    title: msg.payload.title,
    text: msg.payload.text,
  };
  void postIngest(body).then((result) => {
    console.info("[marginalia] ingest result", result);
  });
});
