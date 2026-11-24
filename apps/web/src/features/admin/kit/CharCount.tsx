import { charCountState } from "./char-count-rules";

/**
 * "42 / 60" for a field that has a recommended length (an SEO title, a description): grey inside the limit, red once past it.
 * It never stops the typing or the save; it only says how long the text is. Put it in the field's `labelEnd`.
 */
export function CharCount({ length, limit }: { length: number; limit: number }) {
  const over = charCountState(length, limit) === "over";
  return <span dir="ltr" className={`tabular-nums text-xs ${over ? "font-bold text-error-text" : "font-normal text-ink-muted"}`}>{length} / {limit}</span>;
}
