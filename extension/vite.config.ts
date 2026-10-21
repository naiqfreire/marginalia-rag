import { defineConfig } from "vite";
import { resolve } from "node:path";

export default defineConfig({
  build: {
    outDir: "dist",
    emptyOutDir: true,
    rollupOptions: {
      input: {
        content: resolve(__dirname, "src/features/ContentExtraction/content.ts"),
        background: resolve(__dirname, "src/features/ContentExtraction/background.ts"),
      },
      output: {
        entryFileNames: "[name].js",
      },
    },
  },
  define: {
    "process.env.VITE_API_BASE_URL": JSON.stringify(
      process.env.VITE_API_BASE_URL ?? "http://localhost:5000",
    ),
    "process.env.VITE_MAX_PAGE_WORDS": JSON.stringify(
      process.env.VITE_MAX_PAGE_WORDS ?? "10000",
    ),
  },
});
