const MEDIA_PATH = "/api/v1/catalog/media";

/** Public URL of a stored image (the API serves it through the same-origin `/api` rewrite). */
export function mediaUrl(storageKey: string): string {
  return `${MEDIA_PATH}/${storageKey}`;
}
