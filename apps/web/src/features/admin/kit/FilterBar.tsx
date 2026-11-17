"use client";

import { useEffect, useRef, useState, type ReactNode } from "react";

const SEARCH_DELAY_MS = 350;

interface SearchBoxProps {
  label: string;
  value: string;
  onChange: (value: string) => void;
}

/** A search input that reports after the person stops typing (one request, not one per key). */
export function SearchBox({ label, value, onChange }: SearchBoxProps) {
  const [text, setText] = useState(value);
  const timer = useRef<number | undefined>(undefined);

  useEffect(() => () => window.clearTimeout(timer.current), []);

  function type(next: string) {
    setText(next);
    window.clearTimeout(timer.current);
    timer.current = window.setTimeout(() => onChange(next.trim()), SEARCH_DELAY_MS);
  }

  return (
    <input
      type="search" aria-label={label} placeholder={label} value={text}
      onChange={(event) => type(event.target.value)}
      className="h-11 w-full min-w-0 rounded-control border border-input-line bg-surface px-3 text-sm md:w-72"
    />
  );
}

interface FilterSelectProps<V extends string> {
  label: string;
  value: V;
  onChange: (value: V) => void;
  options: readonly { value: V; label: string }[];
}

/** A compact dropdown for the filter bar (the label is read by screen readers, the first option says "all"). */
export function FilterSelect<V extends string>({ label, value, onChange, options }: FilterSelectProps<V>) {
  return (
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
