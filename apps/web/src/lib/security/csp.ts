/**
 * Security headers for every web response (production builds only; `next dev` needs eval and websockets).
 *
 * The policy is static, not nonce-based: a nonce makes every page dynamic and would drop the cached/ISR pages
 * the storefront relies on (architecture decision 45). Inline scripts stay allowed for Next's hydration data and
 * the theme script; every external origin is listed, so an injected `<script src>` to another host is blocked.
 */
export interface CspSources {
  /** Sentry DSN of the web project; its ingest host is allowed for error reports. */
  sentryDsn?: string;
  /** Google Analytics 4 measurement id; adds the Google tag hosts. */
  gaId?: string;
  /** Meta pixel id; adds the Facebook hosts. */
  metaPixelId?: string;
}

const GOOGLE_SIGN_IN = "https://accounts.google.com/gsi/";

/** The ingest origin of a Sentry DSN (`https://key@o1.ingest.de.sentry.io/2` → `https://o1.ingest.de.sentry.io`), or null. */
export function sentryOrigin(dsn: string | undefined): string | null {
  if (!dsn) return null;
  try {
    const url = new URL(dsn);
    return url.protocol === "https:" ? url.origin : null;
  } catch {
    return null;
  }
}

export function contentSecurityPolicy({ sentryDsn, gaId, metaPixelId }: CspSources): string {
  const script = ["'self'", "'unsafe-inline'", `${GOOGLE_SIGN_IN}client`];
  const connect = ["'self'", GOOGLE_SIGN_IN];
  const img = ["'self'", "data:", "blob:", "https://*.googleusercontent.com"];
  const frame = [GOOGLE_SIGN_IN, "https://www.youtube-nocookie.com", "https://www.youtube.com"];

  const sentry = sentryOrigin(sentryDsn);
  if (sentry) connect.push(sentry);
  if (gaId) {
    script.push("https://www.googletagmanager.com");
    connect.push("https://*.google-analytics.com", "https://*.analytics.google.com", "https://www.googletagmanager.com");
    img.push("https://*.google-analytics.com", "https://www.googletagmanager.com");
  }
  if (metaPixelId) {
    script.push("https://connect.facebook.net");
    connect.push("https://www.facebook.com", "https://connect.facebook.net");
    img.push("https://www.facebook.com");
  }

  return [
    "default-src 'self'",
    `script-src ${script.join(" ")}`,
    `style-src 'self' 'unsafe-inline' ${GOOGLE_SIGN_IN}style`,
    `img-src ${img.join(" ")}`,
    "font-src 'self'",
    `connect-src ${connect.join(" ")}`,
    `frame-src ${frame.join(" ")}`,
    "worker-src 'self' blob:",
    "object-src 'none'",
    "base-uri 'self'",
    "form-action 'self'",
    "frame-ancestors 'none'",
    "upgrade-insecure-requests"
  ].join("; ");
}

export function securityHeaders(sources: CspSources): { key: string; value: string }[] {
  return [
    { key: "Content-Security-Policy", value: contentSecurityPolicy(sources) },
    { key: "X-Content-Type-Options", value: "nosniff" },
    { key: "Referrer-Policy", value: "strict-origin-when-cross-origin" },
    { key: "Permissions-Policy", value: "camera=(), microphone=(), geolocation=(), payment=()" },
    // Google sign-in opens a popup that must be able to message this page back.
    { key: "Cross-Origin-Opener-Policy", value: "same-origin-allow-popups" }
  ];
}
