"use client";

import { useTranslations } from "next-intl";
import { flattenTree } from "../categories/category-rules";
import type { Category } from "../categories/categories-api";
import { toggleId } from "./brand-rules";

/** The categories a brand is suggested for: one pill each, in the tree's order. A suggestion only lists the brand first in the pickers; it never limits them. */
export function BrandCategoriesField({ tree, value, onChange }: { tree: readonly Category[]; value: readonly string[]; onChange: (ids: string[]) => void }) {
  const t = useTranslations("admin.brands.categories");
  const rows = flattenTree(tree, new Set());
  return (
    <fieldset className="m-0 flex min-w-0 flex-col gap-2 border-0 p-0">
      <legend className="p-0 text-[13px] font-bold">{t("title")}</legend>
      {rows.length === 0 ? <p className="m-0 text-xs text-ink-muted">{t("none")}</p> : (
        <ul className="m-0 flex list-none flex-wrap gap-2 p-0">
          {rows.map(({ node }) => {
            const on = value.includes(node.id);
            return (
              <li key={node.id}>
                <button
                  type="button" aria-pressed={on} onClick={() => onChange(toggleId(value, node.id))}
                  className={`press min-h-9 rounded-full border px-3 text-[13px] font-bold transition-colors focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-orange ${on ? "border-carbon bg-carbon text-on-carbon" : "border-line bg-surface hover:border-ink-muted"}`}
                >
                  <span dir="auto" className="[unicode-bidi:isolate]">{node.name}</span>
                </button>
              </li>
            );
          })}
        </ul>
      )}
      <p className="m-0 text-xs text-ink-muted">{t("hint")}</p>
    </fieldset>
  );
}
