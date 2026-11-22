import "server-only";
import { cache } from "react";
import { ApiError, unwrap, type Schemas } from "@rigx/api-client";
import { required } from "@/lib/api/required";
import { createServerApi } from "@/lib/api/server";

const NOT_FOUND = 404;

/** A saved build for its public page, priced live; `null` when the link is unknown. Other failures throw. */
export const loadSharedBuild = cache(async (token: string): Promise<Schemas["SavedBuildView"] | null> => {
  try {
    return await required(unwrap(createServerApi({ fresh: true }).GET("/api/v1/builder/saved/{token}", { params: { path: { token } } })));
  } catch (error) {
    if (error instanceof ApiError && error.status === NOT_FOUND) return null;
    throw error;
  }
});
