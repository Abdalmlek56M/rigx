"use client";

import { useEffect, useState } from "react";
import { unwrap, type Schemas } from "@rigx/api-client";
import { browserApi } from "@/lib/api/browser";
import { required } from "@/lib/api/required";
import type { Condition } from "@/lib/condition";
import { parsePartsParam } from "./builder-state";

type Option = Schemas["BuilderOptionView"];

interface Loaded {
  key: string;
  slot: string;
  page: number;
  options: Option[];
  total: number;
}

/** The slot's options for the chosen parts (`partsKey`), a page at a time; `more()` appends the next page. */
export function useOptions(slot: string, partsKey: string, condition: Condition | null, search: string) {
  const key = [slot, partsKey, condition ?? "", search].join("|");
  const [page, setPage] = useState({ key, number: 1 });
  const [loaded, setLoaded] = useState<Loaded | null>(null);
  const [failedKey, setFailedKey] = useState<string | null>(null);
  const number = page.key === key ? page.number : 1;

  useEffect(() => {
    let cancelled = false;
    required(unwrap(browserApi().POST("/api/v1/builder/options", { body: { slot, parts: parsePartsParam(partsKey), condition, q: search || null, page: number } })))
      .then((result) => {
        if (cancelled) return;
        setFailedKey(null);
        setLoaded((previous) => ({
          key,
          slot,
          page: number,
          options: number > 1 && previous?.key === key ? [...previous.options, ...result.items] : result.items,
          total: result.total
        }));
      })
      .catch(() => {
        if (!cancelled) setFailedKey(key);
      });
    return () => {
      cancelled = true;
    };
  }, [key, slot, partsKey, condition, search, number]);

  const current = loaded?.key === key ? loaded : null;
  return {
    /** Kept while the next request loads (same slot), so the list does not blink empty after every pick. */
    options: loaded?.slot === slot ? loaded.options : [],
    total: current?.total ?? 0,
    loading: failedKey !== key && (current === null || current.page !== number),
    failed: failedKey === key,
    hasMore: current !== null && current.options.length < current.total,
    more: () => setPage({ key, number: number + 1 })
  };
}
