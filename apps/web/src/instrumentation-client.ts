import * as Sentry from "@sentry/nextjs";

/** Browser error tracking (Sentry), on only when the DSN was set at build time. No tracing, no replay (v11 sends no personal data by default). */
const dsn = process.env.NEXT_PUBLIC_SENTRY_DSN;
if (dsn) Sentry.init({ dsn, environment: "production", tracesSampleRate: 0 });
