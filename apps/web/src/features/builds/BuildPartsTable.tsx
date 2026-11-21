import { useLocale, useTranslations } from "next-intl";
import type { Schemas } from "@rigx/api-client";
import { Card } from "@/components/ui/Card";
import { ConditionBadge } from "@/components/ui/ConditionBadge";
import { CaseIcon } from "@/components/ui/icons";
import { parseCondition } from "@/lib/condition";
import { formatEgp } from "@/lib/format";
import { showPartPrices } from "./build-view";

/** What the build is made of: model, جديد / مستعمل, warranty, and each part's price only for builds that can be split. */
export function BuildPartsTable({ build }: { build: Schemas["AssemblyPublicView"] }) {
  const t = useTranslations("build.parts");
  const locale = useLocale();
  const prices = showPartPrices(build);

  return (
    <Card title={t("title")} icon={CaseIcon} padding="none" className="overflow-hidden">
      <div className="overflow-x-auto">
        <table className="w-full border-collapse text-[15px]">
          <thead>
            <tr className="border-b border-line text-start type-label">
              <th scope="col" className="px-[18px] py-3 text-start font-medium">{t("name")}</th>
              <th scope="col" className="px-3 py-3 text-start font-medium">{t("condition")}</th>
              <th scope="col" className="px-3 py-3 text-start font-medium">{t("warranty")}</th>
              {prices ? <th scope="col" className="px-[18px] py-3 text-end font-medium">{t("price")}</th> : null}
            </tr>
          </thead>
          <tbody>
            {build.units.map((unit) => {
              const condition = parseCondition(unit.condition);
              return (
                <tr key={unit.stockItemCode} className="border-b border-line last:border-b-0">
                  <td dir="ltr" className="px-[18px] py-3.5 text-start font-bold [unicode-bidi:isolate]">{unit.name}</td>
                  <td className="px-3 py-3.5">{condition ? <ConditionBadge condition={condition} /> : null}</td>
                  <td className="px-3 py-3.5 text-ink-muted">{unit.warrantyLabel ?? "—"}</td>
                  {prices ? (
                    <td className="px-[18px] py-3.5 text-end font-bold tabular-nums">{unit.partPrice != null ? formatEgp(unit.partPrice, locale) : "—"}</td>
                  ) : null}
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
      {prices ? <p className="m-0 border-t border-line px-[18px] py-3 text-[13px] text-ink-muted">{t("splitNote")}</p> : null}
    </Card>
  );
}
