"use client";

import { useCallback, useMemo } from "react";
import { useSearchParams } from "next/navigation";
import { usePathname, useRouter } from "@/i18n/navigation";
import { readFilters, writeFilters, type FilterDefaults, type FilterValues } from "./url-filters";

/**
 * A list's filters, sort and page as URL query values. `defaults` must be a module-level constant.
 * `set` replaces the URL (no new history entry per keystroke) and goes back to page 1 unless the page is what changed.
 */
export function useUrlFilters<T extends FilterDefaults>(defaults: T): { values: FilterValues<T>; set: (changes: Partial<FilterValues<T>>) => void } {
  const params = useSearchParams();
  const router = useRouter();
  const pathname = usePathname();
  const query = params.toString();

  const values = useMemo(() => readFilters(new URLSearchParams(query), defaults), [query, defaults]);
  const set = useCallback(
    (changes: Partial<FilterValues<T>>) => {
      const next = writeFilters(new URLSearchParams(query), defaults, changes);
      router.replace(next ? `${pathname}?${next}` : pathname, { scroll: false });
    },
    [query, defaults, router, pathname]
  );

  return { values, set };
}
