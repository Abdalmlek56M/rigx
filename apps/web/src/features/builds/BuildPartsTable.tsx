import { useLocale, useTranslations } from "next-intl";
import type { Schemas } from "@rigx/api-client";
import { ConditionBadge } from "@/components/ui/ConditionBadge";
import { parseCondition } from "@/lib/condition";
import { formatEgp } from "@/lib/format";
import { showPartPrices } from "./build-view";

/** What the build is made of: model, جديد / مستعمل, warranty, and each part's price only for builds that can be split. */
export function BuildPartsTable({ build }: { build: Schemas["AssemblyPublicView"] }) {
  const t = useTranslations("build.parts");
  const locale = useLocale();
  const prices = showPartPrices(build);

  return (
    <section className="flex flex-col gap-3">
      <h2 className="m-0 text-2xl font-bold">{t("title")}</h2>
      <div className="overflow-x-auto rounded-card border border-line bg-surface">
        <table className="w-full border-collapse text-[15px]">
          <thead>
            <tr className="border-b border-line text-start text-[13px] text-ink-muted">
              <th scope="col" className="px-[18px] py-3 text-start font-normal">{t("name")}</th>
              <th scope="col" className="px-3 py-3 text-start font-normal">{t("condition")}</th>
              <th scope="col" className="px-3 py-3 text-start font-normal">{t("warranty")}</th>
              {prices ? <th scope="col" className="px-[18px] py-3 text-end font-normal">{t("price")}</th> : null}
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
                    <td className="px-[18px] py-3.5 text-end font-bold">{unit.partPrice != null ? formatEgp(unit.partPrice, locale) : "—"}</td>
                  ) : null}
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
      {prices ? <p className="m-0 text-[13px] text-ink-muted">{t("splitNote")}</p> : null}
    </section>
  );
}
