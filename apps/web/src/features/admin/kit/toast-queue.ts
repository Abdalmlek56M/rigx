/** The toast list: newest last, at most `MAX_TOASTS` on screen. Pure, so it is tested. */
export type ToastTone = "success" | "error";

export interface ToastItem {
  id: number;
  message: string;
  tone: ToastTone;
}

export const MAX_TOASTS = 3;
export const TOAST_MS = 4000;

export function pushToast(list: readonly ToastItem[], item: ToastItem): ToastItem[] {
  return [...list, item].slice(-MAX_TOASTS);
}

export function dropToast(list: readonly ToastItem[], id: number): ToastItem[] {
  return list.filter((item) => item.id !== id);
}
