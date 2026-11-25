/** The toast list: newest last, at most `MAX_TOASTS` on screen. Pure, so it is tested. */
import type { ToastTone } from "../../../components/ui/toast-class.ts";

export type { ToastTone };

export interface ToastItem {
  id: number;
  /** The title line. */
  message: string;
  tone: ToastTone;
  /** An optional second line with the details. */
  detail?: string;
  /** A button on the toast (for example "تراجع"); the toast then stays longer so there is time to use it. */
  action?: { label: string; run: () => void };
}

export const MAX_TOASTS = 3;
export const TOAST_MS = 5000;
/** A toast that offers an undo stays this long. */
export const UNDO_MS = 8000;

export const toastDuration = (item: ToastItem): number => (item.action ? UNDO_MS : TOAST_MS);

export function pushToast(list: readonly ToastItem[], item: ToastItem): ToastItem[] {
  return [...list, item].slice(-MAX_TOASTS);
}

export function dropToast(list: readonly ToastItem[], id: number): ToastItem[] {
  return list.filter((item) => item.id !== id);
}
