import { parentPort } from "node:worker_threads";
import { Parser } from "@etothepii/satisfactory-file-parser";
import { analyzeParsedSave } from "./analyzer.js";

if (!parentPort) {
  throw new Error("Save parser worker requires a parent port.");
}

parentPort.once(
  "message",
  ({ bytes, fileName }: { bytes: ArrayBuffer; fileName: string }) => {
    try {
      const parsed = Parser.ParseSave(fileName, bytes, {
        throwErrors: false,
      });
      parentPort?.postMessage({
        ok: true,
        analysis: analyzeParsedSave(parsed, fileName),
      });
    } catch (error) {
      parentPort?.postMessage({
        ok: false,
        error:
          error instanceof Error
            ? error.message.slice(0, 500)
            : "Unknown parser error",
      });
    }
  },
);
