export const API_BASE_URL: string = process.env.VITE_API_BASE_URL ?? "http://localhost:5000";
export const INGEST_PATH = "/api/ingest";
export const REQUEST_TIMEOUT_MS = 15_000;
export const MAX_RETRIES = 2;
