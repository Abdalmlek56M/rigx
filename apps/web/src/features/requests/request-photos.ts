/** The backend's rules for request photos (`RequestPhotoRules`, `CatalogMediaOptions`): at most 5, 12 MB each, ordinary image types. */
export const MAX_PHOTOS = 5;
export const MAX_PHOTO_BYTES = 12 * 1024 * 1024;
export const PHOTO_TYPES: readonly string[] = ["image/jpeg", "image/png", "image/webp"];

export interface PhotoLike {
  size: number;
  type: string;
}

export type PhotoProblem = "count" | "size" | "type";

/**
 * Adds `incoming` photos to the chosen ones. Photos that break a rule are left out, and the first problem found is
 * reported so the form can say why (the others are still added).
 */
export function addPhotos<T extends PhotoLike>(current: readonly T[], incoming: readonly T[]): { photos: T[]; problem: PhotoProblem | null } {
  const photos = [...current];
  let problem: PhotoProblem | null = null;
  for (const photo of incoming) {
    const rejected: PhotoProblem | null = !PHOTO_TYPES.includes(photo.type) ? "type" : photo.size > MAX_PHOTO_BYTES ? "size" : photos.length >= MAX_PHOTOS ? "count" : null;
    if (rejected) problem ??= rejected;
    else photos.push(photo);
  }
  return { photos, problem };
}
