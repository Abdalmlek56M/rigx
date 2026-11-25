import { getLocale, getTranslations } from "next-intl/server";
import type { Schemas } from "@rigx/api-client";
import { parseCondition } from "@/lib/condition";
import { formatAmount, formatDateNumeric, joinValues } from "@/lib/format";
import { shapeLines, type InvoiceRow, type UnitFacts } from "./invoice-lines";

const CELL = "border border-line px-2 py-2";
const CENTER = `${CELL} text-center tabular-nums`;
const MONEY = `${CELL} whitespace-nowrap text-end tabular-nums`;
/** Column widths in pixels; the item column takes what is left. */
const WIDTHS = [36, null, 62, 112, 46, 92, 100];

/** The name, then one quiet line: the part type, its code(s) and serial number(s). */
async function ItemCell({ facts }: { facts: UnitFacts }) {
  const t = await getTranslations("invoice");
  return (
    <td className={CELL}>
      <div className="flex flex-col gap-0.5">
        <span dir="auto" className="text-start font-bold [unicode-bidi:isolate]">{facts.name}</span>
        <span className="flex flex-wrap gap-x-2.5 text-[11px] text-ink-muted">
          {facts.category ? <span>{facts.category}</span> : null}
          {facts.codes.length ? <span>{t("code")} <bdi dir="ltr" className="font-mono">{facts.codes.join(", ")}</bdi></span> : null}
          {facts.serials.length ? <bdi dir="ltr" className="font-mono [overflow-wrap:anywhere]">{t("serial", { serial: facts.serials.join(", ") })}</bdi> : null}
        </span>
      </div>
    </td>
  );
}

async function ConditionCell({ conditions }: { conditions: string[] }) {
  const t = await getTranslations("common.condition");
  const locale = await getLocale();
  const labels = conditions.map((value) => {
    const condition = parseCondition(value);
    return condition ? t(condition) : null;
  });
  return <td className={`${CELL} text-center text-xs`}>{joinValues(labels, locale)}</td>;
}

async function WarrantyCell({ warranties }: { warranties: UnitFacts["warranties"] }) {
  const t = await getTranslations("invoice");
  return (
    <td className={`${CELL} text-xs`}>
      {warranties.length ? warranties.map((warranty, index) => (
        <span key={index} className="flex flex-col">
          {warranty.label ? <span>{warranty.label}</span> : null}
          {warranty.endsAtUtc ? <span className="text-[11px] text-ink-muted">{t("until", { date: formatDateNumeric(warranty.endsAtUtc) })}</span> : null}
        </span>
      )) : <span className="text-ink-muted">—</span>}
    </td>
  );
}

function ItemRows({ row }: { row: Extract<InvoiceRow, { kind: "item" }> }) {
  return (
    <tbody className="break-inside-avoid">
      <tr className="align-top">
        <td className={`${CENTER} text-ink-muted`}>{row.no}</td>
        <ItemCell facts={row} />
        <ConditionCell conditions={row.conditions} />
        <WarrantyCell warranties={row.warranties} />
        <td className={CENTER}>{row.quantity}</td>
        <td className={MONEY}>{formatAmount(row.unitPrice)}</td>
        <td className={`${MONEY} font-bold`}>{formatAmount(row.lineTotal)}</td>
      </tr>
    </tbody>
  );
}

/** A build: its name across the table, each part with its condition and warranty (priced as one), and the build's subtotal. */
async function BuildRows({ row }: { row: Extract<InvoiceRow, { kind: "build" }> }) {
  const t = await getTranslations("invoice");
  const band = "bg-paper font-bold [print-color-adjust:exact]";
  return (
    <tbody className="break-inside-avoid border-y-2 border-ink">
      <tr className={`${band} break-after-avoid`}>
        <td className={CENTER}>{row.no}</td>
        <th scope="rowgroup" colSpan={6} className={`${CELL} text-start`}>
          {row.name ? <>{t("build")}: <bdi>{row.name}</bdi></> : t("build")}
        </th>
      </tr>
      {row.parts.map((part, index) => (
        <tr key={index} className="break-inside-avoid align-top">
          <td className={`${CENTER} text-[11px] text-ink-muted`}><bdi dir="ltr">{row.no}.{index + 1}</bdi></td>
          <ItemCell facts={part} />
          <ConditionCell conditions={part.conditions} />
          <WarrantyCell warranties={part.warranties} />
          <td className={CENTER}>{part.quantity}</td>
          {index === 0 ? (
            <td rowSpan={row.parts.length} colSpan={2} className={`${CELL} text-center align-middle text-xs text-ink-muted`}>{t("inBuild")}</td>
          ) : null}
        </tr>
      ))}
      <tr className={`${band} break-before-avoid`}>
        <td className={CELL} />
        <th scope="row" colSpan={3} className={`${CELL} text-start`}>{t("buildTotal")}</th>
        <td className={CENTER}>{row.quantity}</td>
        <td className={MONEY}>{formatAmount(row.unitPrice)}</td>
        <td className={MONEY}>{formatAmount(row.lineTotal)}</td>
      </tr>
    </tbody>
  );
}

/**
 * The invoice's table: number, item, condition, warranty, quantity, unit price and total. Its header repeats on every printed page and a
 * row (or a whole build, when it fits a page) is never cut between two pages.
 */
export async function InvoiceLines({ lines }: { lines: Schemas["InvoiceLineView"][] }) {
  const t = await getTranslations("invoice");
  const heads = [t("no"), t("item"), t("condition"), t("warranty"), t("qty"), t("price"), t("lineTotal")];
  const align = (index: number) => (index === 1 || index === 3 ? "text-start" : index >= 5 ? "text-end" : "text-center");

  return (
    <table className="w-full table-fixed border-collapse text-[13px]">
      <colgroup>
        {WIDTHS.map((width, index) => <col key={index} style={width ? { width } : undefined} />)}
      </colgroup>
      <thead>
        <tr className="bg-carbon text-on-carbon [print-color-adjust:exact]">
          {heads.map((head, index) => (
            <th key={head} scope="col" className={`border border-carbon px-2 py-2 text-xs font-bold ${align(index)}`}>{head}</th>
          ))}
        </tr>
      </thead>
      {shapeLines(lines).map((row) => (row.kind === "build" ? <BuildRows key={row.no} row={row} /> : <ItemRows key={row.no} row={row} />))}
    </table>
  );
}
