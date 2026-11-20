"use client";

import { useTranslations } from "next-intl";
import { ComboBox } from "../kit/ComboBox";
import { categoryPath } from "../inventory/inventory-rules";
import type { Category } from "../categories/categories-api";

function findCategory(nodes: readonly Category[], id: string): Category | null {
  for (const node of nodes) {
    if (node.id === id) return node;
    const child = findCategory(node.children, id);
    if (child) return child;
  }
  return null;
}

function flatten(nodes: readonly Category[], out: Category[] = []): Category[] {
  for (const node of nodes) {
    out.push(node);
    flatten(node.children, out);
  }
  return out;
}

/**
 * The searchable category picker over the tree (options show their parent path). A category with children gets a
 * second optional picker for one of its children, without changing the single value the forms keep.
 */
export function CategorySelect({ tree, value, onChange, name = "categoryId", error }: {
  tree: readonly Category[];
  value: string;
  onChange: (id: string) => void;
  name?: string;
  error?: string | null;
}) {
  const t = useTranslations("admin.inventory.fields");
  const tSub = useTranslations("admin.purchases.categories");
  const selected = value ? findCategory(tree, value) : null;
  const first = selected?.parentId ? findCategory(tree, selected.parentId) : selected;
  const children = first?.children ?? [];
  const firstId = first?.id ?? "";
  const subValue = selected && first && selected.parentId === first.id ? selected.id : "";
  const options = [
    { value: "", label: t("pickCategory") },
    ...flatten(tree).map((category) => ({ value: category.id, label: categoryPath(tree, category.id) || category.name }))
  ];
  const subOptions = [
    { value: "", label: tSub("noSubcategory") },
    ...children.map((category) => ({ value: category.id, label: category.name }))
  ];

  return (
    <div className="flex min-w-0 flex-col gap-3">
      <ComboBox
        label={t("category")} name={name} value={firstId} options={options} onChange={onChange} error={error}
      />
      {first && children.length > 0 ? (
        <ComboBox
          label={tSub("subcategory")} name={`${name}-sub`} value={subValue} options={subOptions}
          onChange={(id) => onChange(id || first.id)}
        />
      ) : null}
    </div>
  );
}
