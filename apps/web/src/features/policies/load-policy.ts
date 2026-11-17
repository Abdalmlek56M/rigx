import "server-only";
import { ApiError, unwrap, type Schemas } from "@rigx/api-client";
import { required } from "@/lib/api/required";
import { createServerApi } from "@/lib/api/server";
import type { PolicyApiKind } from "./policy-body";

const NOT_FOUND = 404;

/** The published policy of a kind; `null` when it is not published. Other failures throw. */
export async function loadPolicy(kind: PolicyApiKind): Promise<Schemas["PolicyView"] | null> {
  try {
    return await required(unwrap(createServerApi().GET("/api/v1/content/policies/{kind}", { params: { path: { kind } } })));
  } catch (error) {
    if (error instanceof ApiError && error.status === NOT_FOUND) return null;
    throw error;
  }
}
