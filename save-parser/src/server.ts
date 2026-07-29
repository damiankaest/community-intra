import {
  createServer,
  type IncomingMessage,
  type ServerResponse,
} from "node:http";
import { Worker } from "node:worker_threads";

const port = integerEnvironment("PORT", 3000);
const maximumBytes = integerEnvironment("MAX_SAVE_BYTES", 200 * 1024 * 1024);
let analysisRunning = false;

const server = createServer(async (request, response) => {
  setSecurityHeaders(response);
  if (request.method === "GET" && request.url === "/health") {
    return json(response, 200, {
      status: "Healthy",
      busy: analysisRunning,
    });
  }

  if (request.method !== "POST" || request.url !== "/analyze") {
    return json(response, 404, { error: "Not found" });
  }

  if (analysisRunning) {
    return json(response, 429, {
      error: "Eine Save-Analyse läuft bereits.",
    });
  }

  const fileName = safeFileName(
    request.headers["x-save-file-name"]?.toString() ?? "uploaded.sav",
  );
  if (!fileName.toLowerCase().endsWith(".sav")) {
    return json(response, 415, {
      error: "Nur .sav-Dateien werden akzeptiert.",
    });
  }

  try {
    const bytes = await readBody(request, maximumBytes);
    if (bytes.byteLength === 0) {
      return json(response, 400, { error: "Die Save-Datei ist leer." });
    }

    analysisRunning = true;
    const result = await analyzeInWorker(bytes, fileName);
    return result.ok
      ? json(response, 200, result.analysis)
      : json(response, 422, {
          error: "Die Save-Datei konnte nicht gelesen werden.",
          detail: result.error,
        });
  } catch (error) {
    const message =
      error instanceof Error
        ? error.message
        : "Die Anfrage ist fehlgeschlagen.";
    return json(
      response,
      message === "SAVE_TOO_LARGE" ? 413 : 500,
      message === "SAVE_TOO_LARGE"
        ? { error: "Die Save-Datei ist größer als erlaubt." }
        : { error: "Die Save-Analyse ist fehlgeschlagen." },
    );
  } finally {
    analysisRunning = false;
  }
});

server.requestTimeout = 5 * 60 * 1000;
server.headersTimeout = 30 * 1000;
server.listen(port, "0.0.0.0", () => {
  process.stdout.write(`Save parser listening on ${port}\n`);
});

function analyzeInWorker(
  bytes: Buffer,
  fileName: string,
): Promise<{ ok: true; analysis: unknown } | { ok: false; error: string }> {
  return new Promise((resolve, reject) => {
    const worker = new Worker(new URL("./worker.js", import.meta.url));
    const transferable = new Uint8Array(bytes).buffer;
    worker.once("message", resolve);
    worker.once("error", reject);
    worker.postMessage({ bytes: transferable, fileName }, [transferable]);
  });
}

async function readBody(request: IncomingMessage, limit: number) {
  const chunks: Buffer[] = [];
  let length = 0;
  for await (const chunk of request) {
    const buffer = Buffer.isBuffer(chunk) ? chunk : Buffer.from(chunk);
    length += buffer.length;
    if (length > limit) {
      request.destroy();
      throw new Error("SAVE_TOO_LARGE");
    }
    chunks.push(buffer);
  }
  return Buffer.concat(chunks);
}

function json(response: ServerResponse, status: number, body: unknown) {
  response.writeHead(status, {
    "Content-Type": "application/json; charset=utf-8",
  });
  response.end(JSON.stringify(body));
}

function setSecurityHeaders(response: ServerResponse) {
  response.setHeader("Cache-Control", "no-store");
  response.setHeader("X-Content-Type-Options", "nosniff");
}

function safeFileName(value: string) {
  return (
    value
      .replaceAll("\\", "/")
      .split("/")
      .at(-1)
      ?.replace(/[^\p{L}\p{N}_. -]/gu, "_")
      .slice(0, 160) ?? "uploaded.sav"
  );
}

function integerEnvironment(name: string, fallback: number) {
  const parsed = Number.parseInt(process.env[name] ?? "", 10);
  return Number.isFinite(parsed) && parsed > 0 ? parsed : fallback;
}
