import "server-only";
import { ApiError, unwrap, type Schemas } from "@rigx/api-client";
import { required } from "@/lib/api/required";
import { createServerApi } from "@/lib/api/server";

const NOT_FOUND = 404;

/** The order behind a tracking link, read fresh on every request; `null` when the link is unknown. Other failures throw. */
export async function loadTracking(token: string): Promise<Schemas["TrackingView"] | null> {
  try {
    return await required(unwrap(createServerApi({ fresh: true }).GET("/api/v1/track/{token}", { params: { path: { token } } })));
  } catch (error) {
    if (error instanceof ApiError && error.status === NOT_FOUND) return null;
    throw error;
  }
}
