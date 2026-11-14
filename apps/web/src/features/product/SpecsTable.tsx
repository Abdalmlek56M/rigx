import { useTranslations } from "next-intl";
import type { SpecRow } from "./spec-rows";

export function SpecsTable({ rows }: { rows: SpecRow[] }) {
  const t = useTranslations("product");
  if (rows.length === 0) return null;

  return (
    <section className="flex flex-col gap-3">
      <h2 className="m-0 text-2xl font-bold">{t("specs")}</h2>
      <dl className="m-0 grid grid-cols-1 overflow-hidden rounded-card border border-line bg-surface text-[15px] md:grid-cols-2">
        {rows.map((row) => (
          <div key={row.key} className="flex justify-between gap-4 border-b border-line px-[18px] py-3.5 md:odd:border-e">
            <dt className="text-ink-muted">{row.name}</dt>
            <dd dir="auto" className="m-0 font-bold [unicode-bidi:isolate]">{row.value}</dd>
          </div>
        ))}
      </dl>
    </section>
  );
}
