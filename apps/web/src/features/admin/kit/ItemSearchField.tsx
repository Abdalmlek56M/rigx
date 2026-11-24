"use client";

import { useEffect, useId, useState, type KeyboardEvent } from "react";
import { useTranslations } from "next-intl";
import { CloseIcon } from "@/components/ui/icons";
import { useDebouncedValue } from "@/lib/hooks/use-debounced-value";
import { mediaUrl } from "@/lib/media";
import { usePermissions } from "../use-permissions";
import { IconButton } from "./Button";
import { searchItemHits } from "./item-search-api";
import { hitForCode, ITEM_SEARCH_MIN, searchable, type ItemHit } from "./item-search-rules";

const DEBOUNCE_MS = 250;

function Thumb({ hit }: { hit: ItemHit }) {
  return (
    <span aria-hidden="true" className="flex size-10 shrink-0 items-center justify-center overflow-hidden rounded-control bg-paper">
      {hit.thumbnailStorageKey ? (
        // eslint-disable-next-line @next/next/no-img-element -- media is served by the API through the same-origin rewrite
        <img src={mediaUrl(hit.thumbnailStorageKey)} alt="" className="size-full object-contain" />
      ) : null}
    </span>
  );
}

function Described({ hit }: { hit: ItemHit }) {
  return (
    <span className="flex min-w-0 flex-1 flex-col text-start">
      <span dir="auto" className="truncate text-sm font-bold [unicode-bidi:isolate]">{hit.name || hit.code}</span>
      <span dir="ltr" className="self-start font-mono text-xs font-normal text-ink-muted">{hit.code}</span>
    </span>
  );
}

/**
 * Picks one stock item by searching its name or code as one types (a short pause after the last key, then the inventory search), showing
 * each match with its picture. The value that is kept is the item's code, as before; a chosen item is shown as a card with a remove button.
 * Arrow keys, Enter and Escape work on the list. With `value=""` after a pick (a list that collects items) the field simply starts over.
 */
export function ItemSearchField({ label, name, value, onChange, placeholder, error }: {
  label: string;
  name: string;
  /** The code of the chosen item ("" = none). */
  value: string;
  onChange: (code: string) => void;
  placeholder?: string;
  error?: string | null;
}) {
  const t = useTranslations("admin.kit.itemSearch");
  const { has } = usePermissions();
  const full = has("Inventory.View");
  const inputId = useId();
  const listId = useId();
  const errorId = `${name}-error`;
  const [query, setQuery] = useState("");
  const [open, setOpen] = useState(false);
  const [active, setActive] = useState(0);
  const [found, setFound] = useState<{ text: string; hits: ItemHit[]; failed: boolean } | null>(null);
  const [known, setKnown] = useState<ItemHit | null>(null);
  const text = useDebouncedValue(query.trim(), DEBOUNCE_MS);

  useEffect(() => {
    if (!searchable(text)) return;
    let cancelled = false;
    searchItemHits(text, full)
      .then((hits) => { if (!cancelled) setFound({ text, hits, failed: false }); })
      .catch(() => { if (!cancelled) setFound({ text, hits: [], failed: true }); });
    return () => { cancelled = true; };
  }, [text, full]);

  // A code that was saved earlier (editing): look the item up once so its name and picture can be shown.
  useEffect(() => {
    if (!value) return;
    let cancelled = false;
    searchItemHits(value, full)
      .then((hits) => { if (!cancelled) setKnown(hitForCode(hits, value)); })
      .catch(() => { if (!cancelled) setKnown(null); });
    return () => { cancelled = true; };
  }, [value, full]);

  const results = found && searchable(text) && found.text === text ? found : null;
  const hits = results?.hits ?? [];
  const waiting = searchable(query) && results === null;
  const activeRow = hits.length === 0 ? -1 : Math.min(active, hits.length - 1);
  const optionId = (index: number) => `${listId}-option-${index}`;

  function choose(hit: ItemHit) {
    setKnown(hit);
    onChange(hit.code);
    setQuery("");
    setOpen(false);
    setActive(0);
  }

  function onKeyDown(event: KeyboardEvent<HTMLInputElement>) {
    if (event.key === "ArrowDown" || event.key === "ArrowUp") {
      event.preventDefault();
      if (!open) return setOpen(true);
      if (hits.length === 0) return;
      const delta = event.key === "ArrowDown" ? 1 : -1;
      setActive((current) => (Math.min(current, hits.length - 1) + delta + hits.length) % hits.length);
    } else if (event.key === "Enter" && open && hits[activeRow]) {
      event.preventDefault();
      choose(hits[activeRow]);
    } else if (event.key === "Escape" && open) {
      event.preventDefault();
      setOpen(false);
    }
  }

  if (value) {
    const shown = known && known.code.toUpperCase() === value.toUpperCase() ? known : { code: value, name: "", thumbnailStorageKey: null };
    return (
      <div className="flex min-w-0 flex-col gap-1.5 text-[13px] font-bold">
        <span>{label}</span>
        <div className="flex min-w-0 items-center gap-3 rounded-control border border-input-line bg-surface p-2">
          <Thumb hit={shown} />
          <Described hit={shown} />
          <IconButton icon={CloseIcon} label={t("clear")} variant="ghost" size="sm" onClick={() => onChange("")} />
        </div>
        {error ? <span id={errorId} role="alert" className="text-xs font-normal text-error-text">{error}</span> : null}
      </div>
    );
  }

  const status = !searchable(query) ? t("hint", { min: ITEM_SEARCH_MIN }) : waiting ? t("searching") : results?.failed ? t("failed") : hits.length === 0 ? t("empty") : "";

  return (
    <div
      className="flex min-w-0 flex-col gap-1.5 text-[13px] font-bold"
      onBlur={(event) => {
        if (!event.currentTarget.contains(event.relatedTarget as Node | null)) setOpen(false);
      }}
    >
      <label htmlFor={inputId}>{label}</label>
      <div className="relative min-w-0">
        <input
          id={inputId} name={name} role="combobox" aria-expanded={open} aria-controls={listId} aria-autocomplete="list" autoComplete="off" dir="auto"
          aria-activedescendant={open && activeRow >= 0 ? optionId(activeRow) : undefined}
          aria-invalid={error ? true : undefined} aria-describedby={error ? errorId : undefined}
          value={query} placeholder={placeholder ?? t("placeholder")}
          onChange={(event) => { setQuery(event.target.value); setOpen(true); setActive(0); }}
          onFocus={() => setOpen(true)}
          onKeyDown={onKeyDown}
          className={`h-12 w-full rounded-control border bg-surface px-3 text-sm font-normal text-ink placeholder:text-muted ${error ? "border-error" : "border-input-line"}`}
        />
        {open ? (
          <ul
            id={listId} role="listbox" aria-label={label} onMouseDown={(event) => event.preventDefault()}
            className="absolute inset-x-0 top-full z-20 m-0 mt-1 flex max-h-72 list-none flex-col overflow-y-auto rounded-control border border-input-line bg-surface p-1 shadow-lift"
          >
            {hits.map((hit, index) => (
              <li
                key={hit.code} id={optionId(index)} role="option" aria-selected={index === activeRow} onClick={() => choose(hit)}
                className={`flex min-h-12 cursor-pointer items-center gap-3 rounded-control px-2 ${index === activeRow ? "bg-paper" : "hover:bg-paper"}`}
              >
                <Thumb hit={hit} />
                <Described hit={hit} />
              </li>
            ))}
            {status ? <li role="presentation" className="px-2.5 py-2 text-sm font-normal text-ink-muted">{status}</li> : null}
          </ul>
        ) : null}
      </div>
      {error ? <span id={errorId} role="alert" className="text-xs font-normal text-error-text">{error}</span> : null}
      <span aria-live="polite" className="sr-only">{hits.length > 0 ? t("count", { count: hits.length }) : status}</span>
    </div>
  );
}
