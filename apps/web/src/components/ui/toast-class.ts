/** Where every toast stack sits: the top centre of the screen, clear of the phone notch, above headers and drawers. */
export const TOAST_REGION_CLASS =
  "pointer-events-none fixed inset-x-0 top-[calc(env(safe-area-inset-top)+0.75rem)] bottom-auto z-[60] mx-auto flex w-[min(420px,calc(100vw-2rem))] flex-col gap-2";

/** One toast: a calm white (or dark) card with a thin tone line on its start edge, a soft shadow and room to read. */
export const TOAST_CARD_CLASS =
  "pointer-events-auto flex w-full items-start gap-3 rounded-card border border-line border-s-[3px] bg-surface px-4 py-3.5 text-ink shadow-[0_12px_32px_-14px_rgba(21,23,27,.35)]";

export type ToastTone = "success" | "error" | "info";

/** The start-edge line and the icon circle of a tone. */
export const TOAST_TONE_CLASS: Record<ToastTone, { edge: string; icon: string }> = {
  success: { edge: "border-s-success", icon: "bg-success/12 text-success" },
  error: { edge: "border-s-error", icon: "bg-error/10 text-error-text" },
  info: { edge: "border-s-orange", icon: "bg-orange-soft text-orange-text" }
};
