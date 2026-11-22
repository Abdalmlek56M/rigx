import "server-only";
import { cookies } from "next/headers";
import { unwrap, type Schemas } from "@rigx/api-client";
import { required } from "@/lib/api/required";
import { createServerApi } from "@/lib/api/server";

export const COMMUNITY_PAGE_SIZE = 12;

/** One page of the approved public builds, with the visitor's own hearts and likes marked; `null` when the API does not answer (the page then says so). */
export async function loadCommunityBuilds(page: number): Promise<Schemas["PagedResultOfCommunityBuildCard"] | null> {
  try {
    const api = createServerApi({ cookie: (await cookies()).toString() });
    return await required(unwrap(api.GET("/api/v1/builder/community", { params: { query: { page, pageSize: COMMUNITY_PAGE_SIZE } } })));
  } catch (error) {
    console.warn("Customer builds unavailable:", error instanceof Error ? error.message : error);
    return null;
  }
}
