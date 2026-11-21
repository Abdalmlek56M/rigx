import { useTranslations } from "next-intl";
import { Card } from "@/components/ui/Card";
import { InfoField, InfoGrid } from "@/components/ui/InfoGrid";
import { CpuIcon } from "@/components/ui/icons";
import type { SpecRow } from "./spec-rows";

/** The item's specifications as a label-above-value grid in one card. */
export function SpecsTable({ rows }: { rows: SpecRow[] }) {
  const t = useTranslations("product");
  if (rows.length === 0) return null;

  return (
    <Card title={t("specs")} icon={CpuIcon} padding="md">
      <InfoGrid columns={2} className="gap-y-4">
        {rows.map((row) => (
          <InfoField key={row.key} label={row.name} value={row.value} />
        ))}
      </InfoGrid>
    </Card>
  );
}
