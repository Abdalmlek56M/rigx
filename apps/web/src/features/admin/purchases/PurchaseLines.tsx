"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import type { Brand } from "../brands/brands-api";
import type { Category } from "../categories/categories-api";
import type { Partner } from "../expenses/expenses-api";
import { PurchaseLineCard } from "./PurchaseLineCard";
import { dropPhotoCopy, emptyLine, type LineForm, type MissingField } from "./purchase-rules";
import { PlusIcon } from "@/components/ui/icons";
import { Button } from "../kit/Button";

let lineCounter = 0;
/** A client-only id for a line, so removing one keeps the others' inputs. */
export const newLineKey = () => `line-${++lineCounter}`;

/** The purchased lines: add, remove, repeat and collapse, each line edited in its own card. */
export function PurchaseLines({ lines, minLines, tree, brands, partners, warrantyLabels, errors, missing, submitted, revealKey, onChange, onBrandCreated }: {
  lines: readonly LineForm[];
  /** A whole PC needs two parts; a parts purchase needs one. */
  minLines: number;
  tree: readonly Category[];
  brands: readonly Brand[];
  partners: readonly Partner[] | null;
  warrantyLabels: readonly string[] | null;
  errors: Record<string, string>;
  missing: readonly MissingField[];
  submitted: boolean;
  /** A card the form asked to open after blocking a save (the parent remounts the list with this key). */
  revealKey: string | null;
  onChange: (lines: LineForm[]) => void;
  onBrandCreated?: (brand: Brand) => void;
}) {
  const t = useTranslations("admin.purchases.lines");
  const [openKeys, setOpenKeys] = useState<ReadonlySet<string>>(
    () => new Set(revealKey ? [revealKey] : lines.map((line) => line.key))
  );

  function add() {
    const line = emptyLine(newLineKey());
    const next = [...lines, line];
    onChange(next);
    setOpenKeys((current) => (next.length > 3 ? new Set([line.key]) : new Set([...current, line.key])));
  }

  function duplicate(index: number) {
    const source = lines[index];
    if (!source) return;
    const copy = dropPhotoCopy({ ...source, key: newLineKey(), serialNumber: "" });
    const next = [...lines];
    next.splice(index + 1, 0, copy);
    onChange(next);
    setOpenKeys((current) => new Set([...current, copy.key]));
  }

  function remove(key: string) {
    onChange(lines.filter((line) => line.key !== key));
    setOpenKeys((current) => {
      const next = new Set(current);
      next.delete(key);
      return next;
    });
  }

  return (
    <section data-field="lines" tabIndex={-1} className="flex flex-col gap-3 rounded-card border border-line bg-surface p-5 focus:outline-2 focus:outline-offset-2 focus:outline-orange">
      <h2 className="m-0 text-[17px] font-bold">{t("title")}</h2>
      <ul className="m-0 flex list-none flex-col gap-3 p-0">
        {lines.map((line, index) => (
          <PurchaseLineCard
            key={line.key} line={line} index={index} tree={tree} brands={brands} partners={partners} warrantyLabels={warrantyLabels} errors={errors}
            missingFields={missing.filter((entry) => entry.line === index).map((entry) => entry.field)}
            submitted={submitted} open={openKeys.has(line.key)}
            onToggle={() => setOpenKeys((current) => {
              const next = new Set(current);
              if (next.has(line.key)) next.delete(line.key);
              else next.add(line.key);
              return next;
            })}
            onChange={(next) => onChange(lines.map((entry) => (entry.key === line.key ? next : entry)))}
            onRemove={lines.length > 1 ? () => remove(line.key) : null}
            onDuplicate={() => duplicate(index)}
            onBrandCreated={onBrandCreated}
          />
        ))}
      </ul>
      {lines.length < minLines ? <p role="status" className="m-0 text-sm font-bold text-error-text">{t("needMore", { count: minLines })}</p> : null}
      <Button icon={PlusIcon} className="self-start" onClick={add}>{t("add")}</Button>
      <p className="m-0 text-xs text-ink-muted">{t("modelHint")}</p>
    </section>
  );
}
