"use client";

import { useTranslations } from "next-intl";
import { useRouter } from "@/i18n/navigation";
import { FIELD_BASE } from "@/components/ui/field-class";

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
        className={`${FIELD_BASE} h-11`}
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
