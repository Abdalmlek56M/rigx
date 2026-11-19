"use client";

import { useEffect, useRef } from "react";
import { createPortal } from "react-dom";
import { useLocale, useMessages, useTranslations } from "next-intl";
import { formatDateTimeFull } from "@/lib/format";
import { columnKey, displayCell, isNumeric, type ExportSheet } from "./export-sheet";
import type { ExportDataset } from "./system-rules.ts";

interface ExportPrintSheetProps {
  dataset: ExportDataset;
  sheet: ExportSheet;
  title: string;
  /** "From … to …", or the last-30-days note when no range was picked. */
  range: string;
  generatedAtUtc: string;
  /** Called once the print dialog closes (saved, printed or cancelled). */
  onDone: () => void;
}

/**
 * The PDF export: the dataset as an A4 landscape table, right-to-left in Arabic, with Arabic column names. It is added to the
 * document as the only printed content (`data-print-export`) and the print dialog opens on it; "Save as PDF" there makes the file.
 */
export function ExportPrintSheet({ dataset, sheet, title, range, generatedAtUtc, onDone }: ExportPrintSheetProps) {
  const t = useTranslations("admin.system.export.pdf");
  const locale = useLocale();
  const words = { yes: t("yes"), no: t("no") };
  // A column the server adds later still prints, under its own name, until it gets an Arabic label here.
  const messages = useMessages();
  const names: Record<string, string | undefined> = messages.admin.system.export.pdf.columns;
  const label = (column: string) => names[columnKey(column)] ?? column;
  // Order channel, status, payment and delivery values read the way the orders screen shows them.
  const orders = messages.admin.orders;
  const values: Record<string, string | undefined> =
    dataset === "orders" ? { ...orders.channels, ...orders.statuses, ...orders.paymentStatuses, ...orders.methods, ...orders.fulfillmentKinds } : {};
  const cell = (value: string) => values[value] ?? displayCell(value, locale, words);
  const printed = useRef(false);

  useEffect(() => {
    const finish = () => onDone();
    window.addEventListener("afterprint", finish, { once: true });
    // Once per sheet, even when React runs the effect twice in development.
    if (!printed.current) {
      printed.current = true;
      window.print();
    }
    return () => window.removeEventListener("afterprint", finish);
  }, [onDone]);

  return createPortal(
    <main data-print-export className="hidden bg-surface p-0 text-ink print:block">
      <header className="mb-3 flex items-end justify-between gap-4 border-b-2 border-ink pb-2">
        <div className="flex flex-col gap-0.5">
          <h1 className="m-0 text-lg font-bold">{title}</h1>
          <p className="m-0 text-xs text-ink-muted">{range}</p>
        </div>
        <p className="m-0 text-end text-xs text-ink-muted">
          RIGX · {t("rows", { count: sheet.rows.length })}<br />
          {t("generated", { date: formatDateTimeFull(generatedAtUtc, locale) })}
        </p>
      </header>
      {sheet.rows.length === 0 ? (
        <p className="m-0 py-6 text-center font-bold">{t("empty")}</p>
      ) : (
        <table className="w-full border-collapse text-[10px] leading-snug">
          <thead className="[display:table-header-group]">
            <tr>
              {sheet.columns.map((column, index) => (
                <th key={index} scope="col" className="border border-line bg-paper px-1.5 py-1 text-start font-bold">{label(column)}</th>
              ))}
            </tr>
          </thead>
          <tbody>
            {sheet.rows.map((row, rowIndex) => (
              <tr key={rowIndex} className="break-inside-avoid">
                {row.map((value, index) => (
                  <td key={index} className={`border border-line px-1.5 py-1 align-top [overflow-wrap:anywhere] ${isNumeric(value) ? "text-end tabular-nums" : "text-start"}`}>
                    <span dir="auto" className="[unicode-bidi:isolate]">{cell(value)}</span>
                  </td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </main>,
    document.body
  );
}
