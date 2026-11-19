import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { contentSecurityPolicy, securityHeaders, sentryOrigin } from "./csp.ts";

const directive = (policy: string, name: string) => policy.split("; ").find((part) => part.startsWith(`${name} `)) ?? "";

describe("content security policy", () => {
  it("allows only the site and Google sign-in when nothing else is configured", () => {
    const policy = contentSecurityPolicy({});
    assert.equal(directive(policy, "script-src"), "script-src 'self' 'unsafe-inline' https://accounts.google.com/gsi/client");
    assert.equal(directive(policy, "connect-src"), "connect-src 'self' https://accounts.google.com/gsi/");
    assert.ok(policy.includes("frame-ancestors 'none'"));
    assert.ok(policy.includes("object-src 'none'"));
    assert.ok(!policy.includes("googletagmanager") && !policy.includes("facebook"));
  });

  it("adds the Google tag, Meta pixel and Sentry hosts only when configured", () => {
    const policy = contentSecurityPolicy({ sentryDsn: "https://key@o12.ingest.de.sentry.io/34", gaId: "G-ABC123", metaPixelId: "1234567890" });
    assert.ok(directive(policy, "script-src").includes("https://www.googletagmanager.com"));
    assert.ok(directive(policy, "script-src").includes("https://connect.facebook.net"));
    assert.ok(directive(policy, "connect-src").includes("https://o12.ingest.de.sentry.io"));
    assert.ok(directive(policy, "img-src").includes("https://www.facebook.com"));
  });

  it("reads the Sentry ingest origin from a DSN and ignores anything else", () => {
    assert.equal(sentryOrigin("https://abc@o1.ingest.sentry.io/2"), "https://o1.ingest.sentry.io");
    assert.equal(sentryOrigin("http://abc@o1.ingest.sentry.io/2"), null);
    assert.equal(sentryOrigin("not a url"), null);
    assert.equal(sentryOrigin(undefined), null);
  });

  it("sends the CSP with the other security headers", () => {
    const keys = securityHeaders({}).map((header) => header.key);
    assert.deepEqual(keys, ["Content-Security-Policy", "X-Content-Type-Options", "Referrer-Policy", "Permissions-Policy", "Cross-Origin-Opener-Policy"]);
  });
});
