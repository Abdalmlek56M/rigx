"use client";

import { useTranslations } from "next-intl";
import { ChevronStartIcon } from "@/components/ui/icons";
import { Button } from "./Button";
import { pagerView, type PageInfo } from "./paging";

const button = "press flex h-11 min-w-11 items-center justify-center rounded-control px-3 text-sm font-bold transition-colors focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-orange";

/** Reading direction arrows: "previous" points back (right in Arabic), "next" points forward. */
const PreviousIcon = ({ size }: { size?: number }) => <span className="inline-flex rtl:-scale-x-100"><ChevronStartIcon size={size} /></span>;
const NextIcon = ({ size }: { size?: number }) => <span className="inline-flex ltr:-scale-x-100"><ChevronStartIcon size={size} /></span>;

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
          <Button icon={PreviousIcon} disabled={!view.hasPrevious} onClick={() => onPage(current - 1)}>{t("previous")}</Button>
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
          <Button icon={NextIcon} disabled={!view.hasNext} onClick={() => onPage(current + 1)} className="flex-row-reverse">{t("next")}</Button>
        </div>
      ) : null}
    </nav>
  );
}
