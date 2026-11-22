/**
 * The point-of-sale keyboard shortcuts, read from a key event. The cashier keeps their hands on the keyboard: F2 or / for the item
 * search, F4 for the customer search, Ctrl+Enter to confirm the sale, Ctrl+Shift+Enter to save it as pending, ? for the list.
 * Pure, so it is tested; the screen decides what each one does.
 */
export type PosShortcut = "focusItems" | "focusCustomer" | "confirm" | "savePending" | "help";

export interface PosKeyEvent {
  key: string;
  /** The physical key, so "/" works on an Arabic layout too. */
  code: string;
  ctrlKey: boolean;
  metaKey: boolean;
  altKey: boolean;
  shiftKey: boolean;
}

/** The legend shown to the cashier: the keys of each shortcut, in this order. */
export const POS_SHORTCUT_KEYS: readonly { shortcut: PosShortcut; keys: readonly string[] }[] = [
  { shortcut: "focusItems", keys: ["F2", "/"] },
  { shortcut: "focusCustomer", keys: ["F4"] },
  { shortcut: "confirm", keys: ["Ctrl", "Enter"] },
  { shortcut: "savePending", keys: ["Ctrl", "Shift", "Enter"] },
  { shortcut: "help", keys: ["?"] }
];

/**
 * `typing` is true while the focus is in a field: then plain characters (/ and ?) are text, and only F-keys and Ctrl+Enter act.
 */
export function posShortcut(event: PosKeyEvent, typing: boolean): PosShortcut | null {
  if (event.altKey) return null;
  if (event.ctrlKey || event.metaKey) {
    if (event.key !== "Enter") return null;
    return event.shiftKey ? "savePending" : "confirm";
  }
  if (event.key === "F2") return "focusItems";
  if (event.key === "F4") return "focusCustomer";
  if (typing) return null;
  if (event.code === "Slash") return event.shiftKey ? "help" : "focusItems";
  return null;
}

/** True when the key event came from a field the person can type in. */
export function isTypingTarget(target: EventTarget | null): boolean {
  if (!(target instanceof HTMLElement)) return false;
  return target.isContentEditable || target instanceof HTMLInputElement || target instanceof HTMLTextAreaElement || target instanceof HTMLSelectElement;
}
