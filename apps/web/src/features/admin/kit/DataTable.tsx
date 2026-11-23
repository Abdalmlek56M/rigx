"use client";

import type { ReactNode } from "react";
import { useTranslations } from "next-intl";
import type { Load } from "@/lib/hooks/use-load";
import { ariaSort, nextSort, type SortState } from "./table-sort";
import { ArrowDownIcon, ArrowUpIcon, InboxIcon, RefreshIcon } from "@/components/ui/icons";
import { Button } from "./Button";

export interface Column<T> {
  key: string;
  header: string;
  cell: (row: T) => ReactNode;
  /** The API sort field; the header becomes a sort button. */
  sortKey?: string;
  /** Numbers and money line up at the end. */
  align?: "start" | "end";
  /** A fixed share of the table width (e.g. "32%"), so header and body stay in the same column. */
  width?: string;
  /** On a phone this value takes the whole card width (long names). */
  phoneFull?: boolean;
}

interface DataTableProps<T> {
  /** Screen-reader name of the table. */
  label: string;
  columns: readonly Column<T>[];
  state: Load<readonly T[]>;
  onRetry: () => void;
  rowKey: (row: T) => string;
  empty: string;
  sort?: SortState | null;
  onSort?: (sort: SortState | null) => void;
}

const SKELETON_ROWS = 5;

/** The sort arrow of a header: bold for the column being sorted, a faint hint on the others (it brightens on hover or focus). */
function SortMark({ direction }: { direction: SortState["direction"] | null }) {
  const Arrow = direction === "asc" ? ArrowUpIcon : ArrowDownIcon;
  return (
    <span aria-hidden="true" className={`flex ${direction ? "text-orange-text" : "opacity-30 group-hover:opacity-100 group-focus-visible:opacity-100"}`}>
      <Arrow size={14} />
    </span>
  );
}

// On a phone each row becomes a card: the column names sit above the values (`data-label`), the header row is hidden and empty cells vanish.
const PHONE_ROW = "max-md:mb-2 max-md:grid max-md:grid-cols-2 max-md:gap-x-3 max-md:gap-y-2.5 max-md:rounded-card max-md:border max-md:border-line max-md:bg-surface max-md:p-3";
const PHONE_CELL = "max-md:flex max-md:min-w-0 max-md:flex-col max-md:items-start max-md:gap-0.5 max-md:p-0 max-md:text-start max-md:empty:hidden max-md:before:text-xs max-md:before:text-ink-muted max-md:before:content-[attr(data-label)]";

/** The admin list table: sortable headers, loading rows, a retry on failure and an empty message; scrolls sideways on phones. Row height follows the density the person chose (`--row-py`, set by the shell). */
export function DataTable<T>({ label, columns, state, onRetry, rowKey, empty, sort = null, onSort }: DataTableProps<T>) {
  const t = useTranslations("admin.kit");
  const cellAlign = (column: Column<T>) => (column.align === "end" ? "text-end" : "text-start");
  const fixed = columns.some((column) => column.width);

  let body: ReactNode;
  if (state.status === "loading") {
    body = Array.from({ length: SKELETON_ROWS }, (_, index) => (
      <tr key={index} aria-hidden="true">
        <td colSpan={columns.length} className="px-3 py-3"><span className="block h-5 animate-pulse rounded-control bg-line" /></td>
      </tr>
    ));
  } else if (state.status === "error") {
    body = (
      <tr>
        <td colSpan={columns.length} className="px-3 py-8">
          <div role="alert" className="flex flex-col items-center gap-3">
            <p className="m-0 font-bold">{t("loadFailed")}</p>
            <Button variant="primary" icon={RefreshIcon} onClick={onRetry}>{t("retry")}</Button>
          </div>
        </td>
      </tr>
    );
  } else if (state.data.length === 0) {
    body = (
      <tr>
        <td colSpan={columns.length} className="px-3 py-10 text-center">
          <div className="flex flex-col items-center gap-2 text-ink-muted">
            <span aria-hidden="true" className="flex size-11 items-center justify-center rounded-full bg-paper"><InboxIcon size={22} /></span>
            <p className="m-0 text-sm font-bold">{empty}</p>
          </div>
        </td>
      </tr>
    );
  } else {
    body = state.data.map((row) => (
      <tr key={rowKey(row)} className={`border-t border-line max-md:border-t ${PHONE_ROW}`}>
        {columns.map((column) => <td key={column.key} data-label={column.header} className={`px-3 py-[var(--row-py,0.625rem)] align-middle ${cellAlign(column)} ${PHONE_CELL}${column.phoneFull ? " max-md:col-span-2" : ""}`}>{column.cell(row)}</td>)}
      </tr>
    ));
  }

  return (
    <div className="overflow-x-auto rounded-card border border-line bg-surface max-md:overflow-visible max-md:border-0 max-md:bg-transparent">
      <table aria-label={label} aria-busy={state.status === "loading"} className={`w-full border-collapse text-sm max-md:block ${fixed ? "table-fixed" : ""}`}>
        {fixed ? <colgroup>{columns.map((column) => <col key={column.key} style={column.width ? { width: column.width } : undefined} />)}</colgroup> : null}
        <thead className="border-b border-line bg-paper text-[13px] text-ink max-md:hidden">
          <tr>
            {columns.map((column) => (
              <th key={column.key} scope="col" aria-sort={column.sortKey ? ariaSort(sort, column.sortKey) : undefined} className={`whitespace-nowrap px-3 py-[var(--row-py,0.625rem)] font-bold ${cellAlign(column)}`}>
                {column.sortKey && onSort ? (
                  <button type="button" onClick={() => onSort(nextSort(sort, column.sortKey as string))} className="group inline-flex min-h-8 items-center gap-1 font-bold hover:text-ink">
                    {column.header}
                    <SortMark direction={sort?.key === column.sortKey ? sort.direction : null} />
                  </button>
                ) : column.header}
              </th>
            ))}
          </tr>
        </thead>
        <tbody className="max-md:block">{body}</tbody>
      </table>
    </div>
  );
}
