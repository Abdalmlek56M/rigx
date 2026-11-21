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
  const warranty = [part.warrantyLabel, part.warrantyEndsAtUtc ? t("until", { date: formatDate(part.warrantyEndsAtUtc, locale) }) : null].filter(Boolean).join(" · ");

  return (
    <span className="flex flex-wrap items-center gap-x-2 gap-y-0.5 text-xs text-ink-muted">
      {withIdentity ? <span dir="ltr" className="font-mono">{part.code}</span> : null}
      {withIdentity ? <span dir="auto" className="[unicode-bidi:isolate]">{part.name}</span> : null}
      {condition ? <ConditionBadge condition={condition} /> : null}
      {part.serialNumber ? <span dir="ltr" className="font-mono">{t("serial", { serial: part.serialNumber })}</span> : null}
      {warranty ? <span>{t("warranty")}: {warranty}</span> : null}
    </span>
  );
}

/** The lines table: code, item (with serial, warranty and a build's parts), quantity, price and line total. */
export async function InvoiceLines({ lines }: { lines: Schemas["InvoiceLineView"][] }) {
  const t = await getTranslations("invoice");
  const locale = await getLocale();
  const head = "border-b border-ink py-2 text-start text-xs font-bold text-ink-muted";
  const numberHead = `${head} text-end whitespace-nowrap`;

  return (
    <table className="w-full border-collapse text-sm">
      <thead>
        <tr>
          <th scope="col" className={head}>{t("code")}</th>
          <th scope="col" className={head}>{t("item")}</th>
          <th scope="col" className={numberHead}>{t("qty")}</th>
          <th scope="col" className={numberHead}>{t("price")}</th>
          <th scope="col" className={numberHead}>{t("lineTotal")}</th>
        </tr>
      </thead>
      <tbody>
        {shapeLines(lines).map((row, index) => (
          <tr key={index} className="break-inside-avoid border-b border-line align-top">
            <td dir="ltr" className="py-2.5 pe-2 text-start font-mono text-xs">{row.code}</td>
            <td className="py-2.5 pe-2">
              <div className="flex flex-col gap-1">
                <span dir="auto" className="font-bold [unicode-bidi:isolate]">{row.name}</span>
                {row.parts.map((part) => <PartDetails key={part.code} part={part} withIdentity={row.listParts} />)}
              </div>
            </td>
            <td className="whitespace-nowrap py-2.5 pe-2 text-end tabular-nums">{row.quantity}</td>
            <td className="whitespace-nowrap py-2.5 pe-2 text-end tabular-nums">{formatEgp(row.unitPrice, locale)}</td>
            <td className="whitespace-nowrap py-2.5 text-end font-bold tabular-nums">{formatEgp(row.lineTotal, locale)}</td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}
