"use client";

import { useTranslations } from "next-intl";
import { useRouter } from "@/i18n/navigation";

export interface SortOption {
  value: string;
  label: string;
  href: string;
}

/** The sort dropdown: choosing an option navigates to that option's catalog URL. */
export function SortSelect({ options, current }: { options: SortOption[]; current: string }) {
  const t = useTranslations("catalog.sort");
  const router = useRouter();

  return (
    <label className="flex items-center gap-2 text-sm">
      {t("label")}
      <select
        value={current}
        onChange={(event) => {
          const option = options.find((o) => o.value === event.target.value);
          if (option) router.push(option.href);
        }}
        className="h-11 rounded-control border border-input-line bg-surface px-2.5 text-ink"
      >
        {options.map((option) => (
          <option key={option.value} value={option.value}>
            {option.label}
          </option>
        ))}
      </select>
    </label>
  );
}
