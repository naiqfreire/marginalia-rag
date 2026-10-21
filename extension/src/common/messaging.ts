export type ContentExtractedMessage = {
  type: "content-extracted";
  payload: { url: string; title: string; text: string };
};

export function isContentExtracted(msg: unknown): msg is ContentExtractedMessage {
  return (
    typeof msg === "object" &&
    msg !== null &&
    (msg as { type?: unknown }).type === "content-extracted"
  );
}
