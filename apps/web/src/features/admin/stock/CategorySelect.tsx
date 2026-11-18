"use client";

import { useTranslations } from "next-intl";
import { SelectField } from "@/components/ui/SelectField";
import type { Category } from "../categories/categories-api";
import { parentOptions } from "../categories/category-rules";

/** A category dropdown over the category tree: children are indented under their parent. */
export function CategorySelect({ tree, value, onChange, name = "categoryId", error }: {
  tree: readonly Category[];
  value: string;
  onChange: (id: string) => void;
  name?: string;
  error?: string | null;
}) {
  const t = useTranslations("admin.inventory.fields");
  return (
    <SelectField
      label={t("category")} name={name} value={value} onChange={onChange} error={error}
      options={[{ value: "", label: t("pickCategory") }, ...parentOptions(tree, null).map((option) => ({ value: option.id, label: `${"— ".repeat(option.depth)}${option.name}` }))]}
    />
  );
}
