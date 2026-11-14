import "server-only";
import { cache } from "react";
import { ApiError, unwrap, type Schemas } from "@rigx/api-client";
import { required } from "@/lib/api/required";
import { createServerApi } from "@/lib/api/server";

const NOT_FOUND = 404;

/** One item for the product page; `null` when it does not exist (or is hidden). Other failures throw. */
export const loadItem = cache(async (code: string): Promise<Schemas["StoreItemDetail"] | null> => {
  try {
    return await required(unwrap(createServerApi().GET("/api/v1/store/items/{code}", { params: { path: { code } } })));
  } catch (error) {
    if (error instanceof ApiError && error.status === NOT_FOUND) return null;
    throw error;
  }
});
