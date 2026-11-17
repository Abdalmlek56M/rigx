const DEFAULT_NEXT = "/account";

/** The sign-in page URL that returns to `next` afterwards. */
export function loginHref(next: string): string {
  return `/login?next=${encodeURIComponent(next)}`;
}

/** Only same-site relative paths survive; anything else (other hosts, `//x`, `/\x`) falls back to the account page. */
export function safeNext(value: string | null | undefined): string {
  if (!value || !value.startsWith("/") || value.startsWith("//") || value.startsWith("/\\")) return DEFAULT_NEXT;
  if (/[\u0000-\u001f]/.test(value)) return DEFAULT_NEXT;
  return value;
}

/** `mohamed@gmail.com` → `m••••@gmail.com`: the first character and the domain only. */
export function maskEmail(email: string): string {
  const at = email.lastIndexOf("@");
  if (at < 1) return email;
  return `${email[0]}••••${email.slice(at)}`;
}

/** Seconds as `m:ss` for the resend countdown. */
export function formatCountdown(seconds: number): string {
  const safe = Math.max(0, Math.ceil(seconds));
  return `${Math.floor(safe / 60)}:${String(safe % 60).padStart(2, "0")}`;
}

/** Keeps only digits, at most six (pasted codes often carry spaces or dashes). */
export function normalizeCode(value: string): string {
  return value.replace(/\D/g, "").slice(0, 6);
}

export type SignInFailure =
  | { kind: "wrongCode"; attemptsLeft: number }
  | { kind: "tooManyAttempts" | "expired" | "rateLimited" | "generic" | "google" };

/** Maps a failed code verification to what the person should see. */
export function classifyCodeFailure(status: number, attemptsLeft: unknown): SignInFailure {
  if (status === 429) return { kind: "rateLimited" };
  if (status >= 500) return { kind: "generic" };
  if (typeof attemptsLeft === "number") {
    return attemptsLeft > 0 ? { kind: "wrongCode", attemptsLeft } : { kind: "tooManyAttempts" };
  }
  return { kind: "expired" };
}
