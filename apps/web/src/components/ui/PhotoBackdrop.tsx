import { mediaUrl } from "@/lib/media";

/**
 * The same product photo, scaled up and blurred, behind the real one. A wide or tall picture then fills its square frame
 * without a bare strip and without being cropped, so every card and gallery keeps one size whatever the photo's ratio.
 * Decorative (no alt text, no extra request: the browser reuses the photo it already loads). Put it first inside a
 * `relative overflow-hidden` frame, with the real photo on top as `object-contain`.
 */
export function PhotoBackdrop({ storageKey, size }: { storageKey: string; size: number }) {
  return (
    // eslint-disable-next-line @next/next/no-img-element -- media is served by the API through the same-origin rewrite
    <img src={mediaUrl(storageKey)} alt="" aria-hidden="true" width={size} height={size} className="photo-stage-fill pointer-events-none absolute inset-0 h-full w-full scale-125 object-cover blur-2xl" />
  );
}
