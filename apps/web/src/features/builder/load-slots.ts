import "server-only";
import { unwrap } from "@rigx/api-client";
import { required } from "@/lib/api/required";
import { createServerApi } from "@/lib/api/server";
import type { SlotInfo } from "./SlotList";

/** The builder's slots in display order; `null` when the API is unreachable (the page then says so instead of failing). */
export async function loadSlots(): Promise<SlotInfo[] | null> {
  try {
    const slots = await required(unwrap(createServerApi().GET("/api/v1/builder/slots")));
    return slots.map((entry) => ({ slot: entry.slot, name: entry.name }));
  } catch (error) {
    console.warn("Builder slots unavailable:", error instanceof Error ? error.message : error);
    return null;
  }
}
