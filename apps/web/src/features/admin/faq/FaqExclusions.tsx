"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { SelectField } from "@/components/ui/SelectField";
import { TextField } from "@/components/ui/TextField";
import { flattenTree } from "../categories/category-rules";
import type { Category } from "../categories/categories-api";
import { addExcludedCategory, addExcludedCode, removeValue } from "./faq-rules";
import { PlusIcon } from "@/components/ui/icons";
import { Button } from "../kit/Button";

/** "Excluded in": categories and item codes this general/category FAQ never shows on. */
export function FaqExclusions({ categories, currentCategoryId, categoryIds, itemCodes, onChange }: {
  categories: readonly Category[];
  currentCategoryId: string;
  categoryIds: readonly string[];
  itemCodes: readonly string[];
  onChange: (change: { excludedCategoryIds?: string[]; excludedItemCodes?: string[] }) => void;
}) {
  const t = useTranslations("admin.faq.exclusions");
  const [category, setCategory] = useState("");
  const [code, setCode] = useState("");
  const categoriesFlat = flattenTree(categories, new Set()).map(({ node, depth }) => ({ value: node.id, label: `${"— ".repeat(depth)}${node.name}` }));
  const options = categoriesFlat.filter((option) => option.value !== currentCategoryId && !categoryIds.includes(option.value));
  const names = new Map(flattenTree(categories, new Set()).map(({ node }) => [node.id, node.name]));

  return (
    <div className="flex flex-col gap-3 rounded-control border border-line bg-paper p-3">
      <h3 className="m-0 text-[15px] font-bold">{t("title")}</h3>
      <p className="m-0 text-xs text-ink-muted">{t("hint")}</p>
      <div className="flex flex-wrap items-end gap-2">
        <div className="min-w-56 flex-1">
          <SelectField
            label={t("category")} name="excludedCategory" value={category} onChange={setCategory}
            options={[{ value: "", label: t("pickCategory") }, ...options]}
          />
        </div>
        <Button icon={PlusIcon} disabled={category === ""} onClick={() => { onChange({ excludedCategoryIds: addExcludedCategory(categoryIds, category) }); setCategory(""); }}>
          {t("add")}
        </Button>
      </div>
      {categoryIds.length > 0 ? (
        <ul className="m-0 flex list-none flex-wrap gap-1.5 p-0">
          {categoryIds.map((id) => (
            <li key={id} className="flex items-center gap-1 rounded-[4px] border border-input-line bg-surface px-2 py-1 text-xs">
              {names.get(id) ?? id}
              <button type="button" onClick={() => onChange({ excludedCategoryIds: removeValue(categoryIds, id) })} aria-label={t("remove", { name: names.get(id) ?? id })} className="text-error">×</button>
            </li>
          ))}
        </ul>
      ) : null}
      <div className="flex flex-wrap items-end gap-2">
        <div className="min-w-56 flex-1">
          <TextField label={t("itemCode")} name="excludedCode" value={code} onChange={(value) => setCode(value.toUpperCase())} ltr placeholder="RX-0154" />
        </div>
        <Button icon={PlusIcon} disabled={code.trim() === ""} onClick={() => { onChange({ excludedItemCodes: addExcludedCode(itemCodes, code) }); setCode(""); }}>
          {t("add")}
        </Button>
      </div>
      {itemCodes.length > 0 ? (
        <ul className="m-0 flex list-none flex-wrap gap-1.5 p-0">
          {itemCodes.map((value) => (
            <li key={value} className="flex items-center gap-1 rounded-[4px] border border-input-line bg-surface px-2 py-1 font-mono text-xs">
              {value}
              <button type="button" onClick={() => onChange({ excludedItemCodes: removeValue(itemCodes, value) })} aria-label={t("remove", { name: value })} className="text-error">×</button>
            </li>
          ))}
        </ul>
      ) : null}
    </div>
  );
}
