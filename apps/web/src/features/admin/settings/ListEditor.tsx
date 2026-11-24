"use client";

import { useState, type ReactNode } from "react";
import { useTranslations } from "next-intl";
import { ChevronDownIcon, PlusIcon, TrashIcon } from "@/components/ui/icons";
import { DragItem, DragSort } from "../kit/DragSort";
import { Button, IconButton } from "../kit/Button";
import { addItem, moveItem, moveItemTo, removeItem, replaceItem } from "./list-editor-rules.ts";

/** The width of a grip (24) and of one trailing icon button (36), plus the 8 px gaps of the row: the header row uses them to line up. */
const GRIP = 24;
const ICON = 36;
const GAP = 8;

/** The rows' own keys, so a row keeps its open state and the focus on its grip while it moves; `next` numbers the keys given so far. */
interface Keys {
  list: string[];
  next: number;
}

/** `from` cut or grown to `count` keys (a list that was replaced from outside keeps the keys it has and gives the new rows theirs). */
function keysFor(count: number, from: Keys): Keys {
  const list = from.list.slice(0, count);
  let next = from.next;
  while (list.length < count) list.push(`row-${next++}`);
  return { list, next };
}

interface ListEditorProps<T> {
  /** The list's name for screen readers. */
  label: string;
  items: readonly T[];
  onChange: (items: T[]) => void;
  /** The fields of one row; they sit on one line from `md` (use the same grid classes as `columns`). */
  renderItem: (item: T, update: (item: T) => void) => ReactNode;
  /** What opens under a row from its chevron (the rarely changed fields); omit for a list with nothing more to show. */
  renderDetails?: (item: T, update: (item: T) => void) => ReactNode;
  /** The names of the row's columns, shown once above the list from `md`; `className` is the grid the row's fields use. */
  columns?: { labels: readonly string[]; className: string };
  /** The empty item the add button appends; omit (with `fixed`) for a list that cannot grow or shrink. */
  create?: () => T;
  addLabel?: string;
  /** Order only: no add or remove (the homepage sections). */
  fixed?: boolean;
  max?: number;
}

/**
 * One list used by contact links, lead sources, warranty options, shipping groups and the homepage sections: a compact row per item, a
 * grip to drag it (the up and down arrows on the focused grip are the keyboard way), its fields on one line, an optional chevron for the
 * details and a bin. The section form draws only the fields, so the stored data does not change.
 */
export function ListEditor<T>({ label, items, onChange, renderItem, renderDetails, columns, create, addLabel, fixed = false, max }: ListEditorProps<T>) {
  const t = useTranslations("admin.settings.list");
  const [keys, setKeys] = useState<Keys>(() => keysFor(items.length, { list: [], next: 0 }));
  const [open, setOpen] = useState<ReadonlySet<string>>(new Set());
  const synced = keys.list.length === items.length ? keys : keysFor(items.length, keys);
  if (synced !== keys) setKeys(synced);
  const rowKeys = synced.list;

  const apply = (nextItems: T[], nextList: string[], next = synced.next) => {
    setKeys({ list: nextList, next });
    onChange(nextItems);
  };
  const step = (index: number, direction: -1 | 1) => apply(moveItem(items, index, direction), moveItem(rowKeys, index, direction));
  const drop = (active: string, over: string) => {
    const from = rowKeys.indexOf(active);
    const to = rowKeys.indexOf(over);
    if (from < 0 || to < 0) return;
    apply(moveItemTo(items, from, to), moveItemTo(rowKeys, from, to));
  };
  const toggle = (key: string) => setOpen((current) => {
    const nextOpen = new Set(current);
    if (!nextOpen.delete(key)) nextOpen.add(key);
    return nextOpen;
  });
  const trailing = (renderDetails ? ICON + GAP : 0) + (fixed ? 0 : ICON + GAP);

  return (
    <div className="flex min-w-0 flex-col gap-2">
      {columns ? (
        <div aria-hidden="true" className="hidden items-center gap-2 px-2 text-xs font-bold text-ink-muted md:flex">
          <span style={{ width: GRIP }} className="shrink-0" />
          <span className={`grid min-w-0 flex-1 gap-2 ${columns.className}`}>
            {columns.labels.map((name) => <span key={name} className="truncate">{name}</span>)}
          </span>
          <span style={{ width: trailing }} className="shrink-0" />
        </div>
      ) : null}
      <DragSort onMove={drop}>
        <ul aria-label={label} className="m-0 flex list-none flex-col gap-2 p-0">
          {items.map((item, index) => {
            const key = rowKeys[index] ?? String(index);
            const update = (changed: T) => onChange(replaceItem(items, index, changed));
            const expanded = open.has(key);
            return (
              <DragItem
                key={key} id={key} handle label={t("drag")} onStep={(direction) => step(index, direction)}
                className="flex min-w-0 flex-wrap items-center gap-2 rounded-control border border-line bg-paper p-2"
              >
                <div className="min-w-0 flex-1">{renderItem(item, update)}</div>
                {renderDetails ? (
                  <IconButton
                    icon={ChevronDownIcon} label={t("details")} variant="ghost" size="sm" aria-expanded={expanded}
                    className={expanded ? "[&_svg]:rotate-180" : undefined} onClick={() => toggle(key)}
                  />
                ) : null}
                {fixed ? null : (
                  <IconButton
                    icon={TrashIcon} label={t("remove")} variant="ghost" size="sm"
                    onClick={() => apply(removeItem(items, index), removeItem(rowKeys, index))}
                  />
                )}
                {renderDetails && expanded ? (
                  <div className="min-w-0 basis-full border-t border-line pt-2">{renderDetails(item, update)}</div>
                ) : null}
              </DragItem>
            );
          })}
        </ul>
      </DragSort>
      {fixed || !create ? null : (
        <Button
          icon={PlusIcon} className="self-start" disabled={max !== undefined && items.length >= max}
          onClick={() => apply(addItem(items, create()), [...rowKeys, `row-${synced.next}`], synced.next + 1)}
        >
          {addLabel}
        </Button>
      )}
    </div>
  );
}
