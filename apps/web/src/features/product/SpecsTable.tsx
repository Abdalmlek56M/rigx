import { useTranslations } from "next-intl";
import { Card } from "@/components/ui/Card";
import { SpecIcon } from "@/components/ui/SpecIcon";
import { CpuIcon } from "@/components/ui/icons";
import type { SpecRow } from "./spec-rows";

/** The item's specifications as tiles in one card: an orange icon for the kind of spec, the name, and the value in bold. */
export function SpecsTable({ rows }: { rows: SpecRow[] }) {
  const t = useTranslations("product");
  if (rows.length === 0) return null;

  return (
    <Card title={t("specs")} icon={CpuIcon} padding="md">
      <dl className="m-0 grid grid-cols-2 gap-2.5 lg:grid-cols-3 xl:grid-cols-4">
        {rows.map((row) => (
          <div key={row.key} className="flex min-w-0 flex-col items-start gap-2 rounded-control border border-line bg-paper p-3 sm:flex-row sm:items-center sm:gap-3 sm:px-3.5">
            <span className="flex size-10 shrink-0 items-center justify-center rounded-full bg-orange-soft">
              <SpecIcon specKey={row.key} size={19} />
            </span>
            <div className="flex min-w-0 flex-col gap-0.5">
              <dt className="type-label">{row.name}</dt>
              <dd dir="auto" className="m-0 text-[15px] font-bold leading-6 text-ink [overflow-wrap:anywhere] [unicode-bidi:isolate]">{row.value}</dd>
            </div>
          </div>
        ))}
      </dl>
    </Card>
  );
}
