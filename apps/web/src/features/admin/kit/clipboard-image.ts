/** Taking a picture out of the clipboard for an image upload: only the three types the API accepts. Pure parts are tested. */

export const PASTE_IMAGE_TYPES = ["image/png", "image/jpeg", "image/webp"] as const;

const EXTENSION: Readonly<Record<string, string>> = { "image/png": "png", "image/jpeg": "jpg", "image/webp": "webp" };

/** A pasted picture has no real name ("image.png"); the upload gets a clear one that carries the right extension. */
export function pastedFileName(type: string, now: Date): string {
  const stamp = now.toISOString().slice(0, 19).replace(/[-:T]/g, "");
  return `pasted-${stamp}.${EXTENSION[type] ?? "png"}`;
}

/** The first accepted picture among the clipboard items of a paste event, or null when it holds text or another kind of file. */
export function imageFromItems(items: ArrayLike<{ kind: string; type: string; getAsFile: () => File | null }>): File | null {
  for (const item of Array.from(items)) {
    if (item.kind !== "file" || !(PASTE_IMAGE_TYPES as readonly string[]).includes(item.type)) continue;
    const file = item.getAsFile();
    if (file) return new File([file], pastedFileName(item.type, new Date()), { type: item.type });
  }
  return null;
}

/** The first accepted picture the browser lets the page read from the clipboard on a button press (needs the person's permission), or null. */
export async function readClipboardImage(): Promise<File | null> {
  const entries = await navigator.clipboard.read();
  for (const entry of entries) {
    const type = entry.types.find((candidate) => (PASTE_IMAGE_TYPES as readonly string[]).includes(candidate));
    if (type) return new File([await entry.getType(type)], pastedFileName(type, new Date()), { type });
  }
  return null;
}
