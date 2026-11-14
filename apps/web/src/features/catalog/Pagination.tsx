import { getTranslations } from "next-intl/server";
import { Link } from "@/i18n/navigation";
import { pageWindow } from "./catalog-query";

const box = "flex h-11 min-w-11 items-center justify-center rounded-control px-1";

interface PaginationProps {
  current: number;
  pages: number;
  /** The URL of a page (each list keeps its own filters in it). */
  hrefFor: (page: number) => string;
}

export async function Pagination({ current, pages, hrefFor }: PaginationProps) {
  const t = await getTranslations("catalog.pagination");
  if (pages <= 1) return null;

  return (
    <nav aria-label={t("label")} className="flex justify-center pt-3">
      <ul className="m-0 flex list-none flex-wrap justify-center gap-1.5 p-0">
        {pageWindow(current, pages).map((entry, index) =>
          entry === "gap" ? (
            <li key={`gap-${index}`} aria-hidden="true" className={`${box} text-ink-muted`}>…</li>
          ) : (
            <li key={entry}>
              <Link
                href={hrefFor(entry)}
                aria-current={entry === current ? "page" : undefined}
                aria-label={t("page", { page: entry })}
                className={`${box} font-mono ${entry === current ? "bg-carbon text-on-carbon" : "border border-input-line"}`}
              >
                {entry}
              </Link>
            </li>
          )
        )}
      </ul>
    </nav>
  );
}
