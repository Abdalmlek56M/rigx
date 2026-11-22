import { getLocale, getTranslations } from "next-intl/server";
import type { Schemas } from "@rigx/api-client";
import { ConditionBadge } from "@/components/ui/ConditionBadge";
import { parseCondition } from "@/lib/condition";
import { formatDate, formatEgp } from "@/lib/format";
import { shapeLines } from "./invoice-lines";

async function PartDetails({ part, withIdentity }: { part: Schemas["InvoicePartView"]; withIdentity: boolean }) {
  const t = await getTranslations("invoice");
  const locale = await getLocale();
  const condition = parseCondition(part.condition);
  const warranty = [part.warrantyLabel, part.warrantyEndsAtUtc ? t("until", { date: formatDate(part.warrantyEndsAtUtc, locale) }) : null].filter(Boolean).join("، ");

  return (
    <span className="flex flex-wrap items-center gap-x-2 gap-y-1 text-xs text-ink-muted">
      {withIdentity ? <span dir="ltr" className="font-mono">{part.code}</span> : null}
      {withIdentity ? <span dir="auto" className="[unicode-bidi:isolate]">{part.name}</span> : null}
      {withIdentity && condition ? <ConditionBadge condition={condition} /> : null}
      {part.serialNumber ? <span dir="ltr" className="font-mono">{t("serial", { serial: part.serialNumber })}</span> : null}
      {warranty ? <span>{t("warranty")}: {warranty}</span> : null}
    </span>
  );
}

/**
 * The lines table: number, code, item (with serial and warranty; a build lists its parts), the condition, quantity, price and line total.
 * A line for one part shows its condition in its own column; a build's parts each carry theirs.
 */
export async function InvoiceLines({ lines }: { lines: Schemas["InvoiceLineView"][] }) {
  const t = await getTranslations("invoice");
  const locale = await getLocale();
  const head = "border-b-2 border-ink bg-paper px-2 py-2.5 text-start text-xs font-bold [print-color-adjust:exact]";
  const numberHead = `${head} text-end whitespace-nowrap`;

  return (
    <table className="w-full border-collapse text-sm">
      <thead>
        <tr>
          <th scope="col" className={head}>{t("no")}</th>
          <th scope="col" className={head}>{t("code")}</th>
          <th scope="col" className={head}>{t("item")}</th>
          <th scope="col" className={head}>{t("condition")}</th>
          <th scope="col" className={numberHead}>{t("qty")}</th>
          <th scope="col" className={numberHead}>{t("price")}</th>
          <th scope="col" className={numberHead}>{t("lineTotal")}</th>
        </tr>
      </thead>
      <tbody>
        {shapeLines(lines).map((row, index) => {
          const condition = row.listParts ? null : parseCondition(row.parts[0]?.condition);
          return (
            <tr key={index} className="break-inside-avoid border-b border-line align-top">
              <td className="px-2 py-3 text-start tabular-nums text-ink-muted">{index + 1}</td>
              <td dir="ltr" className="px-2 py-3 text-start font-mono text-xs">{row.code}</td>
              <td className="px-2 py-3">
                <div className="flex flex-col gap-1.5">
                  <span dir="auto" className="font-bold [unicode-bidi:isolate]">{row.name}</span>
                  {row.parts.map((part) => <PartDetails key={part.code} part={part} withIdentity={row.listParts} />)}
                </div>
              </td>
              <td className="px-2 py-3">{condition ? <ConditionBadge condition={condition} /> : null}</td>
              <td className="whitespace-nowrap px-2 py-3 text-end tabular-nums">{row.quantity}</td>
              <td className="whitespace-nowrap px-2 py-3 text-end tabular-nums">{formatEgp(row.unitPrice, locale)}</td>
              <td className="whitespace-nowrap px-2 py-3 text-end font-bold tabular-nums">{formatEgp(row.lineTotal, locale)}</td>
            </tr>
          );
        })}
      </tbody>
    </table>
  );
}
