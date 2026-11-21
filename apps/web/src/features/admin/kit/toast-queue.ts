/** The toast list: newest last, at most `MAX_TOASTS` on screen. Pure, so it is tested. */
export type ToastTone = "success" | "error" | "info";

export interface ToastItem {
  id: number;
  /** The title line. */
  message: string;
  tone: ToastTone;
  /** An optional second line with the details. */
  detail?: string;
}

export const MAX_TOASTS = 3;
export const TOAST_MS = 5000;

export function pushToast(list: readonly ToastItem[], item: ToastItem): ToastItem[] {
  return [...list, item].slice(-MAX_TOASTS);
}

export function dropToast(list: readonly ToastItem[], id: number): ToastItem[] {
  return list.filter((item) => item.id !== id);
}
