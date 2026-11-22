"use client";

import { useEffect, useId, useMemo, useRef, useState } from "react";
import { useTranslations } from "next-intl";
import { BoxIcon, InboxIcon, ListIcon, MoonIcon, RegisterIcon, SearchIcon, StorageIcon, TruckIcon, UserIcon } from "@/components/ui/icons";
import { useRouter } from "@/i18n/navigation";
import { AREA_ICONS } from "../admin-nav-icons";
import { searchRecords, type RecordResults } from "./palette-api";
import {
  canSearchRecords, matchEntries, moveActive, recordSources, staticEntries,
  type PaletteCommand, type PaletteEntry, type PaletteKind, type QuickActionKey
} from "./palette-rules";

const SEARCH_DELAY_MS = 250;

/** Sections in the order they appear; a record kind with no match is left out. */
const SECTION_ORDER: readonly PaletteKind[] = ["action", "page", "order", "customer", "item", "purchase"];

const ACTION_ICONS: Record<QuickActionKey, typeof BoxIcon> = {
  newSale: RegisterIcon, newPurchase: TruckIcon, newOrders: BoxIcon, newRequests: InboxIcon, toggleTheme: MoonIcon, toggleDensity: ListIcon
};

const KIND_ICONS: Record<Exclude<PaletteKind, "action" | "page">, typeof BoxIcon> = {
  order: BoxIcon, customer: UserIcon, item: StorageIcon, purchase: TruckIcon
};

function EntryIcon({ entry }: { entry: PaletteEntry }) {
  const Icon = entry.kind === "action" && entry.actionKey ? ACTION_ICONS[entry.actionKey]
    : entry.kind === "page" && entry.areaKey ? AREA_ICONS[entry.areaKey]
      : entry.kind === "action" || entry.kind === "page" ? SearchIcon : KIND_ICONS[entry.kind];
  return <Icon size={18} />;
}

interface BodyProps {
  permissions: readonly string[];
  onClose: () => void;
  onCommand: (command: PaletteCommand) => void;
}

function PaletteBody({ permissions, onClose, onCommand }: BodyProps) {
  const t = useTranslations("admin.palette");
  const nav = useTranslations("admin.nav");
  const router = useRouter();
  const listId = useId();
  const [query, setQuery] = useState("");
  const [active, setActive] = useState(0);
  const [found, setFound] = useState<{ query: string; results: RecordResults } | null>(null);

  const sources = useMemo(() => recordSources(permissions), [permissions]);
  const statics = useMemo(
    () => staticEntries(permissions, {
      area: (key) => nav(`areas.${key}`),
      group: (key) => nav(`groups.${key}`),
      action: (key) => t(`actions.${key}`)
    }),
    [permissions, nav, t]
  );

  const text = query.trim();
  const searching = sources.length > 0 && canSearchRecords(text);
  useEffect(() => {
    if (!searching) return;
    let current = true;
    const timer = window.setTimeout(() => {
      void searchRecords(text, sources).then((results) => current && setFound({ query: text, results }));
    }, SEARCH_DELAY_MS);
    return () => {
      current = false;
      window.clearTimeout(timer);
    };
  }, [searching, text, sources]);

  const records = searching && found?.query === text ? found.results : null;
  const entries = [...matchEntries(statics, query), ...(records?.entries ?? [])];
  const sections = SECTION_ORDER
    .map((kind) => ({ kind, entries: entries.filter((entry) => entry.kind === kind) }))
    .filter((section) => section.entries.length > 0);
  const rows = sections.flatMap((section) => section.entries);
  const current = rows.length === 0 ? -1 : Math.min(active, rows.length - 1);

  useEffect(() => {
    if (current >= 0) document.getElementById(`${listId}-${current}`)?.scrollIntoView({ block: "nearest" });
  }, [current, listId]);

  function choose(entry: PaletteEntry) {
    onClose();
    if (entry.command) onCommand(entry.command);
    else if (entry.href) router.push(entry.href);
  }

  const index = (entry: PaletteEntry) => rows.indexOf(entry);

  return (
    <div className="flex max-h-[min(560px,80dvh)] flex-col">
      <div className="flex items-center gap-2.5 border-b border-line px-4">
        <span aria-hidden="true" className="text-ink-muted"><SearchIcon size={18} /></span>
        <input
          type="search" role="combobox" aria-expanded="true" aria-controls={listId} aria-autocomplete="list" aria-label={t("title")}
          aria-activedescendant={current >= 0 ? `${listId}-${current}` : undefined}
          placeholder={t("placeholder")} value={query} autoComplete="off" autoFocus
          onChange={(event) => {
            setQuery(event.target.value);
            setActive(0);
          }}
          onKeyDown={(event) => {
            if (event.key === "ArrowDown" || event.key === "ArrowUp") {
              event.preventDefault();
              setActive(moveActive(current, rows.length, event.key === "ArrowDown" ? 1 : -1));
            } else if (event.key === "Enter" && current >= 0) {
              event.preventDefault();
              const entry = rows[current];
              if (entry) choose(entry);
            }
          }}
          className="h-14 min-w-0 flex-1 bg-transparent text-base text-ink [appearance:none] outline-none placeholder:text-ink-muted [&::-webkit-search-cancel-button]:hidden"
        />
        <kbd dir="ltr" className="hidden rounded-sm border border-line px-1.5 font-mono text-[11px] text-ink-muted md:inline">Esc</kbd>
      </div>

      <ul id={listId} role="listbox" aria-label={t("title")} className="m-0 min-h-0 flex-1 list-none overflow-y-auto p-2">
        {sections.map((section) => (
          <li key={section.kind} role="presentation" className="flex flex-col">
            <span className="px-2.5 pb-1 pt-2 text-[11px] font-bold text-ink-muted">{t(`groups.${section.kind}`)}</span>
            <ul role="presentation" className="m-0 flex list-none flex-col gap-0.5 p-0">
              {section.entries.map((entry) => {
                const position = index(entry);
                return (
                  <li
                    key={entry.id} id={`${listId}-${position}`} role="option" aria-selected={position === current}
                    onMouseMove={() => setActive(position)} onClick={() => choose(entry)}
                    className={`flex min-h-11 cursor-pointer items-center gap-3 rounded-control px-2.5 py-1.5 ${position === current ? "bg-paper outline-1 -outline-offset-1 outline-orange" : ""}`}
                  >
                    <span aria-hidden="true" className="text-ink-muted"><EntryIcon entry={entry} /></span>
                    <span className="flex min-w-0 flex-1 flex-col">
                      <span dir="auto" className={`truncate text-sm font-bold [unicode-bidi:isolate] ${entry.kind === "order" || entry.kind === "item" || entry.kind === "purchase" ? "font-mono" : ""}`}>{entry.label}</span>
                      {entry.hint ? <span dir="auto" className="truncate text-xs text-ink-muted [unicode-bidi:isolate]">{entry.hint}</span> : null}
                    </span>
                  </li>
                );
              })}
            </ul>
          </li>
        ))}
        {rows.length === 0 && !(searching && records === null) ? <li role="presentation" className="px-3 py-8 text-center text-sm font-bold text-ink-muted">{t("empty")}</li> : null}
        {searching && records === null ? <li role="presentation" className="px-3 py-3 text-center text-sm text-ink-muted">{t("searching")}</li> : null}
        {records?.failed ? <li role="presentation" className="px-3 py-2 text-center text-xs font-bold text-error-text">{t("failed")}</li> : null}
      </ul>

      <p className="m-0 hidden gap-4 border-t border-line px-4 py-2 text-xs text-ink-muted md:flex">
        <span>{t("hintMove")}</span>
        <span>{t("hintOpen")}</span>
      </p>
    </div>
  );
}

interface CommandPaletteProps extends BodyProps {
  open: boolean;
}

/**
 * Ctrl+K: one box to jump to an admin page, start a quick action, or find an order, customer, stock item or purchase by its code,
 * name or phone. A native modal dialog (focus trap, Escape, focus return); it shows only what the person's permissions open.
 */
export function CommandPalette({ open, ...body }: CommandPaletteProps) {
  const t = useTranslations("admin.palette");
  const ref = useRef<HTMLDialogElement>(null);

  useEffect(() => {
    const dialog = ref.current;
    if (!dialog) return;
    if (open && !dialog.open) dialog.showModal();
    if (!open && dialog.open) dialog.close();
  }, [open]);

  return (
    <dialog
      ref={ref} aria-label={t("title")}
      onClose={() => {
        if (open) body.onClose();
      }}
      onClick={(event) => {
        if (event.target === event.currentTarget) body.onClose();
      }}
      className="admin-dialog fixed inset-x-0 top-[6dvh] m-0 mx-auto mb-auto w-[min(640px,calc(100vw-16px))] max-w-none overflow-hidden rounded-panel border-0 bg-surface p-0 text-ink shadow-[0_0_40px_rgba(21,23,27,.25)] backdrop:bg-carbon/50 md:top-[12dvh]"
    >
      {open ? <PaletteBody {...body} /> : null}
    </dialog>
  );
}
