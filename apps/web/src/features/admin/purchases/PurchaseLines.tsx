"use client";

import { useTranslations } from "next-intl";
import type { Category } from "../categories/categories-api";
import type { Partner } from "../expenses/expenses-api";
import { PurchaseLineCard } from "./PurchaseLineCard";
import { emptyLine, type LineForm } from "./purchase-rules";

let lineCounter = 0;
/** A client-only id for a line, so removing one keeps the others' inputs. */
export const newLineKey = () => `line-${++lineCounter}`;

/** The purchased lines: add and remove, each line edited in its own card. */
export function PurchaseLines({ lines, minLines, tree, partners, warrantyLabels, errors, onChange }: {
  lines: readonly LineForm[];
  /** A whole PC needs two parts; a parts purchase needs one. */
  minLines: number;
  tree: readonly Category[];
  partners: readonly Partner[] | null;
  warrantyLabels: readonly string[] | null;
  errors: Record<string, string>;
  onChange: (lines: LineForm[]) => void;
}) {
  const t = useTranslations("admin.purchases.lines");
  return (
    <section className="flex flex-col gap-3 rounded-[10px] border border-line bg-surface p-5">
      <h2 className="m-0 text-[17px] font-bold">{t("title")}</h2>
      <ul className="m-0 flex list-none flex-col gap-3 p-0">
        {lines.map((line, index) => (
          <PurchaseLineCard
            key={line.key} line={line} index={index} tree={tree} partners={partners} warrantyLabels={warrantyLabels} errors={errors}
            onChange={(next) => onChange(lines.map((entry) => (entry.key === line.key ? next : entry)))}
            onRemove={lines.length > 1 ? () => onChange(lines.filter((entry) => entry.key !== line.key)) : null}
          />
        ))}
      </ul>
      {lines.length < minLines ? <p role="status" className="m-0 text-sm font-bold text-error">{t("needMore", { count: minLines })}</p> : null}
      <button type="button" onClick={() => onChange([...lines, emptyLine(newLineKey())])} className="h-11 self-start rounded-control border border-input-line bg-surface px-5 font-bold">{t("add")}</button>
      <p className="m-0 text-xs text-ink-muted">{t("modelHint")}</p>
    </section>
  );
}
