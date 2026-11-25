"use client";

import { useCallback, useEffect, useId, useRef, useState, type KeyboardEvent } from "react";
import { useLocale, useTranslations } from "next-intl";
import { unwrap, type Schemas } from "@rigx/api-client";
import { ArrowEndIcon, ImagePlaceholderIcon, SearchIcon } from "@/components/ui/icons";
import { Link, getPathname, useRouter } from "@/i18n/navigation";
import type { Locale } from "@/i18n/routing";
import { browserApi } from "@/lib/api/browser";
import { parseCondition } from "@/lib/condition";
import { formatEgp } from "@/lib/format";
import { useDebouncedValue } from "@/lib/hooks/use-debounced-value";
import { useDismiss } from "@/lib/hooks/use-dismiss";
import { mediaUrl } from "@/lib/media";
import { productPath } from "@/lib/seo/urls";
import { cardPrice } from "@/features/product/price-options";
import { SUGGEST_LIMIT, moveHighlight, suggestQuery } from "./search-rules";

const SUGGEST_DELAY_MS = 300;

type Suggestions = { query: string; items: Schemas["StoreItemCard"][] | null };

/**
 * Header search: a rounded field with the search icon at its start. While typing it suggests the first matching items (photo, name,
 * condition and price) after a short pause, with a link to every result; arrows move through them and Enter opens one. Without
 * scripts it is a plain form that opens the catalog.
 */
export function SearchForm() {
  const t = useTranslations("header");
  const tc = useTranslations("common.condition");
  const locale = useLocale() as Locale;
  const router = useRouter();
  const listId = useId();
  const box = useRef<HTMLFormElement>(null);
  const [text, setText] = useState("");
  const [open, setOpen] = useState(false);
  const [highlight, setHighlight] = useState(-1);
  const [result, setResult] = useState<Suggestions | null>(null);
  const query = useDebouncedValue(suggestQuery(text), SUGGEST_DELAY_MS);
  const close = useCallback(() => setOpen(false), []);
  useDismiss(box, open, close);

  useEffect(() => {
    if (!query) return;
    let live = true;
    unwrap(browserApi().GET("/api/v1/store/items", { params: { query: { search: query, pageSize: SUGGEST_LIMIT } } }))
      .then((page) => page?.items ?? [], () => null)
      .then((items) => {
        if (live) setResult({ query, items });
      });
    return () => {
      live = false;
    };
  }, [query]);

  const current = suggestQuery(text);
  const ready = result !== null && result.query === current ? result.items : undefined;
  const items = ready ?? [];
  const allHref = `/products?search=${encodeURIComponent(current ?? "")}`;
  const showPanel = open && current !== null;

  function onKeyDown(event: KeyboardEvent<HTMLInputElement>) {
    if (event.key === "ArrowDown" || event.key === "ArrowUp") {
      event.preventDefault();
      setOpen(true);
      setHighlight((index) => moveHighlight(index, event.key === "ArrowDown" ? 1 : -1, items.length));
    } else if (event.key === "Enter" && showPanel && highlight >= 0 && items[highlight]) {
      event.preventDefault();
      setOpen(false);
      router.push(productPath(items[highlight].slug));
    }
  }

  return (
    <form ref={box} action={getPathname({ href: "/products", locale })} method="get" role="search" className="relative w-full min-w-0" onSubmit={close}>
      <label htmlFor="header-search" className="sr-only">
        {t("searchLabel")}
      </label>
      <span aria-hidden="true" className="pointer-events-none absolute inset-y-0 start-4 flex items-center text-orange-text">
        <SearchIcon size={18} />
      </span>
      <input
        id="header-search"
        type="search"
        name="search"
        enterKeyHint="search"
        autoComplete="off"
        role="combobox"
        aria-expanded={showPanel}
        aria-controls={listId}
        aria-autocomplete="list"
        aria-activedescendant={showPanel && highlight >= 0 ? `${listId}-${highlight}` : undefined}
        value={text}
        onChange={(event) => {
          setText(event.target.value);
          setHighlight(-1);
          setOpen(true);
        }}
        onFocus={() => setOpen(true)}
        onKeyDown={onKeyDown}
        placeholder={t("searchPlaceholder")}
        className="h-11 w-full rounded-full border border-line bg-paper pe-4 ps-11 text-[15px] text-ink transition-[border-color,background-color,box-shadow] duration-200 placeholder:text-muted hover:border-input-line focus:border-orange focus:bg-surface focus:shadow-[0_0_0_4px_var(--orange-soft)] focus:outline-none"
      />
      {showPanel ? (
        <div className="mega-in absolute start-0 top-full z-50 mt-2 w-full overflow-hidden rounded-panel border border-line bg-surface text-ink shadow-lift md:w-[max(100%,440px)]">
          <ul id={listId} role="listbox" aria-label={t("searchResults")} aria-busy={ready === undefined} className="m-0 flex list-none flex-col p-1.5">
            {ready === undefined
              ? [0, 1, 2].map((index) => (
                  <li key={index} aria-hidden="true" className="flex items-center gap-3 p-2">
                    <span className="size-12 shrink-0 animate-pulse rounded-control bg-paper" />
                    <span className="h-4 flex-1 animate-pulse rounded-full bg-paper" />
                  </li>
                ))
              : items.map((item, index) => {
                  const condition = parseCondition(item.condition);
                  return (
                    <li key={item.code} id={`${listId}-${index}`} role="option" aria-selected={index === highlight}>
                      <Link
                        href={productPath(item.slug)}
                        onClick={close}
                        onPointerEnter={() => setHighlight(index)}
                        className={`flex items-center gap-3 rounded-control p-2 transition-colors ${index === highlight ? "bg-orange-soft" : ""}`}
                      >
                        <span className="flex size-12 shrink-0 items-center justify-center overflow-hidden rounded-control border border-line bg-paper text-muted">
                          {item.thumbnailStorageKey ? (
                            // eslint-disable-next-line @next/next/no-img-element -- media is served by the API through the same-origin rewrite
                            <img src={mediaUrl(item.thumbnailStorageKey)} alt="" width={48} height={48} className="product-photo h-full w-full object-cover" />
                          ) : (
                            <ImagePlaceholderIcon size={20} />
                          )}
                        </span>
                        <span className="flex min-w-0 flex-1 flex-col gap-0.5">
                          <span dir="ltr" className="line-clamp-2 text-start text-sm font-bold leading-5 [unicode-bidi:isolate]">{item.modelName}</span>
                          {condition ? <span className="text-xs text-ink-muted">{tc(condition)}</span> : null}
                        </span>
                        <span className="shrink-0 text-sm font-bold text-orange-text tabular-nums">{formatEgp(cardPrice(item), locale)}</span>
                      </Link>
                    </li>
                  );
                })}
          </ul>
          {ready !== undefined && items.length === 0 ? (
            <p role="status" className="m-0 px-4 pb-3 pt-1 text-sm text-ink-muted">{ready === null ? t("searchFailed") : t("searchEmpty", { query: current })}</p>
          ) : null}
          <Link
            href={allHref}
            onClick={close}
            className="flex min-h-11 items-center justify-between gap-2 border-t border-line px-4 text-sm font-bold text-orange-text transition-colors hover:bg-paper"
          >
            {t("searchAll", { query: current })}
            <span aria-hidden="true" className="flex"><ArrowEndIcon size={16} /></span>
          </Link>
        </div>
      ) : null}
    </form>
  );
}
