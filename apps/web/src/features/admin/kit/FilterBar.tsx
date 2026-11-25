"use client";

import { useEffect, useRef, useState, type ReactNode } from "react";
import { SearchIcon } from "@/components/ui/icons";

const SEARCH_DELAY_MS = 350;

interface SearchBoxProps {
  label: string;
  value: string;
  onChange: (value: string) => void;
  /** A name a screen can find the input by (`data-focus`), for a keyboard shortcut that jumps to it. */
  focusKey?: string;
  /**
   * Enter reports the text at once (without waiting for the pause). Returning true means the text was used (an item was added from a
   * scanned code): the box then empties and keeps the focus.
   */
  onEnter?: (text: string) => Promise<boolean> | boolean;
}

/** A search input that reports after the person stops typing (one request, not one per key). */
export function SearchBox({ label, value, onChange, focusKey, onEnter }: SearchBoxProps) {
  const [text, setText] = useState(value);
  const timer = useRef<number | undefined>(undefined);

  useEffect(() => () => window.clearTimeout(timer.current), []);

  function type(next: string) {
    setText(next);
    window.clearTimeout(timer.current);
    timer.current = window.setTimeout(() => onChange(next.trim()), SEARCH_DELAY_MS);
  }

  async function enter() {
    window.clearTimeout(timer.current);
    const typed = text.trim();
    onChange(typed);
    if (onEnter && (await onEnter(typed))) {
      setText("");
      onChange("");
    }
  }

  return (
    <div className="relative w-full min-w-0 md:w-72">
      <span aria-hidden="true" className="pointer-events-none absolute inset-y-0 start-3 flex items-center text-orange-text"><SearchIcon size={16} /></span>
      <input
        type="search" aria-label={label} placeholder={label} value={text} data-focus={focusKey}
        onChange={(event) => type(event.target.value)}
        onKeyDown={onEnter ? (event) => {
          // Ctrl+Enter is a screen shortcut (confirm the sale), not a search.
          if (event.key !== "Enter" || event.ctrlKey || event.metaKey) return;
          event.preventDefault();
          void enter();
        } : undefined}
        className="h-11 w-full min-w-0 rounded-control border border-input-line bg-surface ps-9 pe-3 text-sm"
      />
    </div>
  );
}

interface FilterSelectProps<V extends string> {
  label: string;
  value: V;
  onChange: (value: V) => void;
  options: readonly { value: V; label: string }[];
  /** Shows the label as a small title above the dropdown (a bar of several dropdowns that would otherwise read as unnamed boxes). */
  titled?: boolean;
}

/** A compact dropdown for the filter bar (the label is read by screen readers, the first option says "all"). */
export function FilterSelect<V extends string>({ label, value, onChange, options, titled = false }: FilterSelectProps<V>) {
  const select = (
    <select
      aria-label={label} value={value}
      onChange={(event) => {
        const chosen = options.find((option) => option.value === event.target.value);
        if (chosen) onChange(chosen.value);
      }}
      className="h-11 rounded-control border border-input-line bg-surface px-2.5 text-sm"
    >
      {options.map((option) => <option key={option.value} value={option.value}>{option.label}</option>)}
    </select>
  );
  return titled ? (
    <label className="flex flex-col gap-1">
      <span className="type-label font-bold">{label}</span>
      {select}
    </label>
  ) : select;
}

/** One row above a table: search and filters at the start, extra actions (export, add) at the end; wraps on phones. */
export function FilterBar({ children, actions }: { children: ReactNode; actions?: ReactNode }) {
  return (
    <div className="flex flex-wrap items-center justify-between gap-2.5">
      <div role="search" className="flex min-w-0 flex-1 flex-wrap items-center gap-2">{children}</div>
      {actions ? <div className="flex flex-wrap gap-2">{actions}</div> : null}
    </div>
  );
}
