import "server-only";
import { cookies } from "next/headers";
import { cache } from "react";
import { ApiError, unwrap, type Schemas } from "@rigx/api-client";
import { required } from "@/lib/api/required";
import { createServerApi } from "@/lib/api/server";

const NOT_FOUND = 404;

/**
 * A saved build for its page, priced live; `null` when the link is unknown (or the build is private and the visitor is not its owner).
 * The visitor's cookie goes along, so an owner sees their private build and their own reactions. Other failures throw.
 */
export const loadSharedBuild = cache(async (token: string): Promise<Schemas["SavedBuildView"] | null> => {
  try {
    return await required(unwrap(createServerApi({ cookie: (await cookies()).toString() }).GET("/api/v1/builder/saved/{token}", { params: { path: { token } } })));
  } catch (error) {
    if (error instanceof ApiError && error.status === NOT_FOUND) return null;
    throw error;
  }
});
