"use client";

import { useLocale, useTranslations } from "next-intl";
import { formatDate, formatEgp } from "@/lib/format";
import { statementDetail, statementKindKey } from "./partner-rules";
import type { Statement } from "./partners-api";

/** The partner statement: capital opening and closing, what was earned, paid and recovered, and every line in the range. Reads the same on screen and on paper. */
export function StatementSheet({ statement }: { statement: Statement }) {
  const t = useTranslations("admin.partners.statement");
  const locale = useLocale();
  const money = (value: number) => <span dir="ltr" className="whitespace-nowrap">{formatEgp(value, locale)}</span>;
  const figures = [
    { key: "opening", label: t("opening"), value: statement.capitalOpening },
    { key: "earned", label: t("earned"), value: statement.earned },
    { key: "paid", label: t("paid"), value: statement.paid },
    { key: "recovered", label: t("recovered"), value: statement.recovered },
    { key: "closing", label: t("closing"), value: statement.capitalClosing }
  ];

  return (
    <article className="flex min-w-0 flex-col gap-3 text-sm">
      <header className="flex flex-col gap-0.5">
        <h3 className="m-0 text-[17px] font-bold"><span dir="auto" className="[unicode-bidi:isolate]">{t("heading", { name: statement.partner.name })}</span></h3>
        <p className="m-0 text-ink-muted">{t("range", { from: formatDate(statement.from, locale), to: formatDate(statement.to, locale) })}</p>
      </header>
      <dl className="m-0 grid grid-cols-2 gap-2 sm:grid-cols-5">
        {figures.map((figure) => (
          <div key={figure.key} className="flex flex-col gap-0.5 rounded-control border border-line p-2.5">
            <dt className="text-xs text-ink-muted">{figure.label}</dt>
            <dd className="m-0 font-bold">{money(figure.value)}</dd>
          </div>
        ))}
      </dl>
      <div className="overflow-x-auto">
        <table aria-label={t("linesLabel")} className="w-full border-collapse">
          <thead>
            <tr className="border-b border-line text-start text-xs text-ink-muted">
              <th scope="col" className="px-2 py-2 text-start font-bold">{t("columns.date")}</th>
              <th scope="col" className="px-2 py-2 text-start font-bold">{t("columns.kind")}</th>
              <th scope="col" className="px-2 py-2 text-start font-bold">{t("columns.detail")}</th>
              <th scope="col" className="px-2 py-2 text-end font-bold">{t("columns.amount")}</th>
            </tr>
          </thead>
          <tbody>
            {statement.lines.length === 0 ? (
              <tr><td colSpan={4} className="px-2 py-6 text-center font-bold text-ink-muted">{t("empty")}</td></tr>
            ) : (
              statement.lines.map((line, index) => {
                const detail = statementDetail(line);
                return (
                  <tr key={`${line.at}-${index}`} className="border-t border-line">
                    <td className="whitespace-nowrap px-2 py-2 text-xs">{formatDate(line.at, locale)}</td>
                    <td className="whitespace-nowrap px-2 py-2">{t(`kinds.${statementKindKey(line.kind)}`)}</td>
                    <td className="px-2 py-2">{detail ? <span dir="auto" className="[unicode-bidi:isolate]">{detail}</span> : <span className="text-ink-muted">{t("none")}</span>}</td>
                    <td className={`px-2 py-2 text-end font-bold ${line.amount < 0 ? "text-error" : ""}`}>{money(line.amount)}</td>
                  </tr>
                );
              })
            )}
          </tbody>
        </table>
      </div>
    </article>
  );
}
