"use client";

import { useTranslations } from "next-intl";
import { pagerView, type PageInfo } from "./paging";

const button = "flex h-11 min-w-11 items-center justify-center rounded-control px-3 text-sm font-bold disabled:opacity-40";

/** "21–40 of 135" and the page buttons under a table; hidden when everything fits on one page. */
export function Pager({ info, onPage }: { info: PageInfo; onPage: (page: number) => void }) {
  const t = useTranslations("admin.kit.pager");
  const view = pagerView(info);
  const current = Math.min(Math.max(1, info.page), view.pages);

  return (
    <nav aria-label={t("label")} className="flex flex-wrap items-center justify-between gap-3 text-sm">
      <span className="text-ink-muted">{t("range", { from: view.from, to: view.to, total: info.total })}</span>
      {view.pages > 1 ? (
        <div className="flex flex-wrap items-center gap-1">
          <button type="button" disabled={!view.hasPrevious} onClick={() => onPage(current - 1)} className={`${button} border border-input-line bg-surface`}>
            {t("previous")}
          </button>
          {view.numbers.map((number, index) =>
            number === null ? (
              <span key={`gap-${index}`} aria-hidden="true" className="px-1 text-ink-muted">…</span>
            ) : (
              <button
                key={number} type="button" aria-current={number === current ? "page" : undefined} onClick={() => onPage(number)}
                className={`${button} ${number === current ? "bg-carbon text-on-carbon" : "hover:bg-line"}`}
              >
                {number}
              </button>
            )
          )}
          <button type="button" disabled={!view.hasNext} onClick={() => onPage(current + 1)} className={`${button} border border-input-line bg-surface`}>
            {t("next")}
          </button>
        </div>
      ) : null}
    </nav>
  );
}
