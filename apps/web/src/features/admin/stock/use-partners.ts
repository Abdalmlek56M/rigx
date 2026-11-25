"use client";

import { useCallback } from "react";
import { useLoad, type Load } from "@/lib/hooks/use-load";
import { fetchPartners, type Partner } from "../expenses/expenses-api";

export interface PartnerSource {
  state: Load<readonly Partner[]>;
  reload: () => void;
}

const NONE: readonly Partner[] = [];

/** The partners a screen shows or picks from, with their own loading, failure and reload; `null` when the person may not read them. */
export function usePartners(canSee: boolean): PartnerSource | null {
  const { state, reload } = useLoad(useCallback(() => (canSee ? fetchPartners() : Promise.resolve(NONE)), [canSee]));
  return canSee ? { state, reload } : null;
}

/** The loaded partners; empty while loading, after a failure or without permission. */
export const loadedPartners = (source: PartnerSource | null): readonly Partner[] => (source?.state.status === "ready" ? source.state.data : NONE);
