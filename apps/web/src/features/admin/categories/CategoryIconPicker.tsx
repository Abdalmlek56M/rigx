"use client";

import { useTranslations } from "next-intl";
import { CategoryIcon, CategoryIconByKey } from "@/components/ui/category-icon";
import { CATEGORY_ICON_KEYS } from "@/components/ui/category-icon-rules";

const tile = "flex min-h-[72px] flex-col items-center justify-center gap-1.5 rounded-control border px-1 py-2 text-[11px] font-bold transition-colors";

/** The ready icon set as a grid of tiles; "تلقائي" (empty key) lets the storefront pick the icon from the category's slug. */
export function CategoryIconPicker({ value, slug, onChange }: { value: string; slug: string; onChange: (key: string) => void }) {
  const t = useTranslations("admin.categories.icon");
  const choices = [{ key: "", label: t("auto") }, ...CATEGORY_ICON_KEYS.map((key) => ({ key, label: t(`keys.${key}`) }))];

  return (
    <fieldset className="m-0 flex flex-col gap-2 border-0 p-0">
      <legend className="mb-1 text-[13px] font-bold">{t("title")}</legend>
      <p className="m-0 text-xs text-ink-muted">{t("hint")}</p>
      <div role="radiogroup" aria-label={t("title")} className="grid grid-cols-[repeat(auto-fill,minmax(76px,1fr))] gap-2">
        {choices.map((choice) => {
          const selected = value === choice.key;
          return (
            <button
              key={choice.key || "auto"}
              type="button"
              role="radio"
              aria-checked={selected}
              onClick={() => onChange(choice.key)}
              className={`${tile} ${selected ? "border-orange bg-orange-soft text-orange-text" : "border-line bg-surface text-ink-muted hover:border-orange/50 hover:text-ink"}`}
            >
              <span aria-hidden="true" className="flex text-orange-text">
                {choice.key === "" ? <CategoryIcon slug={slug} size={22} /> : <CategoryIconByKey iconKey={choice.key as (typeof CATEGORY_ICON_KEYS)[number]} size={22} />}
              </span>
              {choice.label}
            </button>
          );
        })}
      </div>
    </fieldset>
  );
}
