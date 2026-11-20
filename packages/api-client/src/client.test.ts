import assert from "node:assert/strict";
import { test } from "node:test";
import { ApiError, createRigxClient, multipart, unwrap } from "./index.ts";

type Call = { method: string; path: string; csrf: string | null; credentials: RequestCredentials };

function fakeServer(handler: (request: Request) => Response | Promise<Response>) {
  const calls: Call[] = [];
  let issued = 0;
  const fetch = async (input: RequestInfo | URL, init?: RequestInit): Promise<Response> => {
    const request = input instanceof Request ? input : new Request(input, init);
    const path = new URL(request.url).pathname;
    calls.push({ method: request.method, path, csrf: request.headers.get("X-CSRF-TOKEN"), credentials: request.credentials });
    if (path === "/api/v1/identity/antiforgery") return Response.json({ requestToken: `token-${++issued}` });
    return handler(request);
  };
  return { calls, fetch };
}

const problem = (status: number, body: object) =>
  new Response(JSON.stringify({ status, ...body }), { status, headers: { "content-type": "application/problem+json" } });

test("safe requests carry no CSRF token and send cookies", async () => {
  const server = fakeServer(() => Response.json({ requestToken: "x" }));
  const api = createRigxClient({ baseUrl: "http://api.test", fetch: server.fetch });

  await unwrap(api.GET("/api/v1/identity/antiforgery"));

  assert.equal(server.calls.length, 1);
  assert.equal(server.calls[0]?.csrf, null);
  assert.equal(server.calls[0]?.credentials, "include");
});

test("unsafe requests fetch the CSRF token once and reuse it", async () => {
  const server = fakeServer(() => new Response(null, { status: 204 }));
  const api = createRigxClient({ baseUrl: "http://api.test", fetch: server.fetch });

  const restore = { params: { path: { kind: "articles", id: "00000000-0000-0000-0000-000000000001" } } };
  await unwrap(api.DELETE("/api/v1/identity/sessions/others"));
  await unwrap(api.POST("/api/v1/admin/system/trash/{kind}/{id}/restore", restore));
  await unwrap(api.POST("/api/v1/admin/system/trash/{kind}/{id}/restore", restore));

  const tokens = server.calls.filter(call => call.method !== "GET").map(call => call.csrf);
  // Identity requests change who is signed in, so the next unsafe request gets a new token.
  assert.deepEqual(tokens, ["token-1", "token-2", "token-2"]);
});

test("errors become ApiError with the ProblemDetails body", async () => {
  const server = fakeServer(() => problem(401, { title: "The code is wrong or expired.", attemptsLeft: 2 }));
  const api = createRigxClient({ baseUrl: "http://api.test", fetch: server.fetch });

  const error = await unwrap(api.POST("/api/v1/identity/email-code/verify", {
    body: { email: "a@b.c", code: "123456", rememberDevice: false }
  })).catch((caught: unknown) => caught);

  assert.ok(error instanceof ApiError);
  assert.equal(error.status, 401);
  assert.equal(error.message, "The code is wrong or expired.");
  assert.equal(error.problem["attemptsLeft"], 2);
});

test("validation problems expose field errors", async () => {
  const server = fakeServer(() => problem(400, { title: "Invalid request.", errors: { email: ["Enter a valid email."] } }));
  const api = createRigxClient({ baseUrl: "http://api.test", fetch: server.fetch });

  const error = await unwrap(api.POST("/api/v1/identity/email-code/request", { body: { email: "x" } }))
    .catch((caught: unknown) => caught);

  assert.ok(error instanceof ApiError);
  assert.deepEqual(error.fieldErrors, { email: ["Enter a valid email."] });
});

test("multipart uploads send the form with its boundary", async () => {
  let received: { type: string | null; image: FormDataEntryValue | null } | undefined;
  const server = fakeServer(async (request) => {
    received = { type: request.headers.get("content-type"), image: (await request.formData()).get("image") };
    return Response.json({ storageKey: "avatars/a.webp" });
  });
  const api = createRigxClient({ baseUrl: "http://api.test", fetch: server.fetch });
  const form = new FormData();
  form.set("image", new Blob(["png"], { type: "image/png" }), "a.png");

  await unwrap(api.POST("/api/v1/identity/profile/avatar", multipart(form)));

  assert.match(received?.type ?? "", /^multipart\/form-data; boundary=/);
  assert.ok(received?.image instanceof Blob);
});
