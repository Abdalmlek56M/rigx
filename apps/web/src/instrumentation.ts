import * as Sentry from "@sentry/nextjs";

/** Server-side error tracking (Sentry), on only when `SENTRY_DSN` is set (production compose). */
export function register() {
  const dsn = process.env.SENTRY_DSN;
  if (!dsn || process.env.NEXT_RUNTIME !== "nodejs") return;
  Sentry.init({ dsn, environment: "production", tracesSampleRate: 0 });
}

/** Errors thrown while rendering a page, a route handler or the proxy. */
export const onRequestError = Sentry.captureRequestError;
