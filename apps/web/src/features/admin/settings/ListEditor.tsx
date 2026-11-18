"use client";

import type { ReactNode } from "react";
import { useTranslations } from "next-intl";
import { ArrowUpIcon, CloseIcon } from "@/components/ui/icons";
import { addItem, moveItem, removeItem, replaceItem } from "./list-editor-rules.ts";

const control = "flex h-11 w-11 items-center justify-center rounded-control border border-input-line bg-surface text-ink disabled:opacity-40";
const add = "h-11 self-start rounded-control border border-input-line bg-surface px-4 text-sm font-bold disabled:opacity-50";

interface ListEditorProps<T> {
  /** The list's name for screen readers. */
  label: string;
  items: readonly T[];
  onChange: (items: T[]) => void;
  renderItem: (item: T, update: (item: T) => void) => ReactNode;
  /** The empty item the add button appends; omit (with `fixed`) for a list that cannot grow or shrink. */
  create?: () => T;
  addLabel?: string;
  /** Order only: no add or remove (the homepage sections). */
  fixed?: boolean;
  max?: number;
}

/**
 * One list used by contact links, shipping groups, lead sources, warranty options and homepage sections:
 * each item is a card with up/down (and remove), and the section form draws only the item's fields.
 */
export function ListEditor<T>({ label, items, onChange, renderItem, create, addLabel, fixed = false, max }: ListEditorProps<T>) {
  const t = useTranslations("admin.settings.list");
  return (
    <div className="flex min-w-0 flex-col gap-2">
      <ul aria-label={label} className="m-0 flex list-none flex-col gap-2 p-0">
        {items.map((item, index) => (
          <li key={index} className="flex min-w-0 flex-col gap-2 rounded-control border border-line bg-paper p-3">
            <div className="flex items-center justify-between gap-2">
              <span className="text-xs font-bold text-ink-muted">{t("item", { number: index + 1 })}</span>
              <span className="flex items-center gap-1">
                <button
                  type="button" aria-label={t("up")} disabled={index === 0}
                  onClick={() => onChange(moveItem(items, index, -1))} className={control}
                >
                  <ArrowUpIcon size={18} />
                </button>
                <button
                  type="button" aria-label={t("down")} disabled={index === items.length - 1}
                  onClick={() => onChange(moveItem(items, index, 1))} className={control}
                >
                  <span className="inline-flex rotate-180"><ArrowUpIcon size={18} /></span>
                </button>
                {fixed ? null : (
                  <button type="button" aria-label={t("remove")} onClick={() => onChange(removeItem(items, index))} className={control}>
                    <CloseIcon size={18} />
                  </button>
                )}
              </span>
            </div>
            {renderItem(item, (next) => onChange(replaceItem(items, index, next)))}
          </li>
        ))}
      </ul>
      {fixed || !create ? null : (
        <button
          type="button" disabled={max !== undefined && items.length >= max}
          onClick={() => onChange(addItem(items, create()))} className={add}
        >
          + {addLabel}
        </button>
      )}
    </div>
  );
}
