import createClient, { type Client, type Middleware } from "openapi-fetch";
import type { components, paths } from "./generated/schema.d.ts";

export type { components, paths };
export type Schemas = components["schemas"];
export type ProblemDetails = Schemas["ProblemDetails"];

/** Header that carries the emailed step-up code for destructive admin actions. */
export const STEP_UP_HEADER = "X-Step-Up-Code";
const CSRF_HEADER = "X-CSRF-TOKEN";
const CSRF_PATH = "/api/v1/identity/antiforgery";
const SAFE_METHODS = new Set(["GET", "HEAD", "OPTIONS"]);

/** Every non-2xx response, carrying the server's ProblemDetails (`title`, `status`, field `errors`, extensions). */
export class ApiError extends Error {
  readonly status: number;
  readonly problem: ProblemDetails;

  constructor(status: number, problem: ProblemDetails) {
    super(problem.title ?? `Request failed (${status}).`);
    this.name = "ApiError";
    this.status = status;
    this.problem = problem;
  }

  /** Field errors keyed by request field (validation problems only). */
  get fieldErrors(): Record<string, string[]> {
    return this.problem.errors ?? {};
  }
}

export interface RigxClientOptions {
  /** API origin, e.g. `https://rigx.example` or `http://localhost:5080`. Empty = same origin. */
  baseUrl?: string;
  /** Custom fetch (server components, tests). Defaults to the global fetch. */
  fetch?: typeof globalThis.fetch;
  /** Extra headers for every request (e.g. forwarded cookies on the server). */
  headers?: Record<string, string>;
}

export type RigxClient = Client<paths> & {
  /** Forget the cached CSRF token (the next unsafe request fetches a new one). */
  resetCsrf(): void;
};

/**
 * The one client the frontend uses: cookie session (`credentials: include`), the CSRF token added to every unsafe
 * request (fetched once and refreshed after sign-in/out or a rejected token), and typed paths from the contract.
 */
export function createRigxClient(options: RigxClientOptions = {}): RigxClient {
  const baseUrl = options.baseUrl ?? "";
  const fetchImpl = options.fetch ?? globalThis.fetch.bind(globalThis);
  let csrfToken: Promise<string> | undefined;

  const loadCsrfToken = async (): Promise<string> => {
    const response = await fetchImpl(new Request(`${baseUrl}${CSRF_PATH}`, {
      credentials: "include",
      ...(options.headers ? { headers: options.headers } : {})
    }));
    if (!response.ok) throw new ApiError(response.status, await readProblem(response));
    const body = (await response.json()) as { requestToken?: string };
    if (!body.requestToken) throw new ApiError(response.status, { title: "Missing anti-forgery token.", status: response.status });
    return body.requestToken;
  };

  const csrf: Middleware = {
    async onRequest({ request }) {
      if (SAFE_METHODS.has(request.method)) return request;
      csrfToken ??= loadCsrfToken().catch((error: unknown) => {
        csrfToken = undefined;
        throw error;
      });
      request.headers.set(CSRF_HEADER, await csrfToken);
      return request;
    },
    onResponse({ request, response }) {
      if (SAFE_METHODS.has(request.method)) return response;
      // The token is bound to the signed-in user: refresh it after identity changes or a 400 (possibly a stale token).
      if (new URL(request.url).pathname.startsWith("/api/v1/identity/") || response.status === 400)
        csrfToken = undefined;
      return response;
    }
  };

  const client = createClient<paths>({
    baseUrl,
    credentials: "include",
    fetch: fetchImpl,
    ...(options.headers ? { headers: options.headers } : {})
  });
  client.use(csrf);
  return Object.assign(client, { resetCsrf: () => { csrfToken = undefined; } });
}

type Outcome<T> =
  | { data: T; error?: never; response: Response }
  | { data?: never; error: unknown; response: Response };

/** Returns the success body or throws {@link ApiError}: `const items = await unwrap(api.GET("/api/v1/store/items"))`. */
export async function unwrap<T>(call: Promise<Outcome<T>>): Promise<T> {
  const { data, error, response } = await call;
  if (response.ok) return data as T;
  throw new ApiError(response.status, toProblem(error, response));
}

function toProblem(error: unknown, response: Response): ProblemDetails {
  return typeof error === "object" && error !== null && "title" in error
    ? (error as ProblemDetails)
    : { title: response.statusText || "Request failed.", status: response.status };
}

async function readProblem(response: Response): Promise<ProblemDetails> {
  try {
    return toProblem(await response.json(), response);
  } catch {
    return toProblem(undefined, response);
  }
}
