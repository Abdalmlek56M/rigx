import "server-only";
import { cache } from "react";
import { ApiError, unwrap, type Schemas } from "@rigx/api-client";
import { required } from "@/lib/api/required";
import { createServerApi } from "@/lib/api/server";
import { toApiQuery, type BuildsState } from "./build-query";

const NOT_FOUND = 404;

/** Published builds for the list; `null` when the API is unreachable (the page then says so instead of failing). */
export async function loadBuilds(state: BuildsState): Promise<Schemas["AssemblyPublicView"][] | null> {
  try {
    return await required(unwrap(createServerApi().GET("/api/v1/store/assemblies", { params: { query: toApiQuery(state) } })));
  } catch (error) {
    console.warn("Builds unavailable:", error instanceof Error ? error.message : error);
    return null;
  }
}

/** One published build; `null` when it does not exist or is not published. Other failures throw. */
export const loadBuild = cache(async (code: string): Promise<Schemas["AssemblyPublicView"] | null> => {
  try {
    return await required(unwrap(createServerApi().GET("/api/v1/store/assemblies/{code}", { params: { path: { code } } })));
  } catch (error) {
    if (error instanceof ApiError && error.status === NOT_FOUND) return null;
    throw error;
  }
});
