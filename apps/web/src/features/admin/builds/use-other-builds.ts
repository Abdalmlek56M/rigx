"use client";

import { useCallback } from "react";
import { useLoad } from "@/lib/hooks/use-load";
import { fetchBuilds } from "./builds-api";
import { otherBuilds } from "./build-rules";

/** The other open builds, read fresh when the dialog opens (the move or swap sends their current row version). */
export function useOtherBuilds(currentId: string) {
  return useLoad(useCallback(async () => otherBuilds(await fetchBuilds(), currentId), [currentId]));
}
