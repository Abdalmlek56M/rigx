import { unwrap } from "@rigx/api-client";

/** Saves a file the browser already holds under `fileName` (one temporary link, clicked and removed). */
function saveBlob(blob: Blob, fileName: string): void {
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = fileName;
  document.body.append(link);
  link.click();
  link.remove();
  URL.revokeObjectURL(url);
}

/**
 * The one file download: `request` is a typed client call made with `parseAs: "blob"` (and the step-up headers when needed).
 * A failure throws `ApiError` like any other call, so `useSubmit` still asks for the step-up code.
 */
export async function download(request: Parameters<typeof unwrap<Blob>>[0], fileName: string): Promise<void> {
  saveBlob(await unwrap(request), fileName);
}
