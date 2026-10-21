import { CONTENT_EXTRACTED, type ExtractedPage } from "./types";

const STRIP_SELECTORS = ["script", "style", "nav", "footer", "aside", "noscript", "svg"];
const MAX_PAGE_WORDS = Number(process.env.VITE_MAX_PAGE_WORDS ?? "10000");

function countWords(text: string): number {
  return text.split(/\s+/).filter(Boolean).length;
}

function pickRoot(): Element {
  const article = document.querySelector("article");
  if (article) return article;
  const main = document.querySelector("main");
  if (main) return main;
  const candidates = Array.from(document.querySelectorAll("div, section"));
  let best: Element | null = null;
  let bestLen = 0;
  for (const el of candidates) {
    const len = (el.textContent ?? "").length;
    if (len > bestLen) {
      bestLen = len;
      best = el;
    }
  }
  return best ?? document.body;
}

function extractText(root: Element): string {
  const clone = root.cloneNode(true) as Element;
  for (const sel of STRIP_SELECTORS) {
    clone.querySelectorAll(sel).forEach((n) => n.remove());
  }
  let text = (clone as HTMLElement).innerText ?? clone.textContent ?? "";
  text = text.replace(/\n{3,}/g, "\n\n").trim();
  const words = countWords(text);
  if (words > MAX_PAGE_WORDS) {
    text = text.split(/\s+/).slice(0, MAX_PAGE_WORDS).join(" ");
  }
  return text;
}

function main(): void {
  const payload: ExtractedPage = {
    url: location.href,
    title: document.title,
    text: extractText(pickRoot()),
  };
  console.info("[marginalia] extracted", {
    url: payload.url,
    titleLength: payload.title.length,
    textLength: payload.text.length,
    words: countWords(payload.text),
  });
  chrome.runtime.sendMessage({ type: CONTENT_EXTRACTED, payload });
}

main();
