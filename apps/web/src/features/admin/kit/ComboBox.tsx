"use client";

import { useId, useState, type KeyboardEvent } from "react";
import { useTranslations } from "next-intl";
import { filterComboOptions, type ComboOption } from "./combo-rules.ts";

interface ComboBoxProps {
  label: string;
  name: string;
  value: string;
  options: readonly ComboOption[];
  onChange: (value: string) => void;
  placeholder?: string;
  error?: string | null;
  /** Shows a final "+ add «text»" row for the typed text. */
  onCreate?: (text: string) => void | Promise<void>;
}

/**
 * A text field with a list under it: typing filters the labels (anywhere, case-insensitive), click opens the full list,
 * and arrow keys / Enter / Escape work as one expects. The parent keeps the selected value; the field shows its label.
 */
export function ComboBox({ label, name, value, options, onChange, placeholder, error, onCreate }: ComboBoxProps) {
  const t = useTranslations("admin.kit.combo");
  const inputId = useId();
  const listId = useId();
  const errorId = `${name}-error`;
  const selected = options.find((option) => option.value === value);
  const [open, setOpen] = useState(false);
  const [query, setQuery] = useState("");
  const [browsing, setBrowsing] = useState(false);
  const [active, setActive] = useState(0);
  const visible = filterComboOptions(options, browsing ? "" : query);
  const typed = query.trim();
  const showCreate = Boolean(onCreate && typed && !browsing);
  const rowCount = visible.length + (showCreate ? 1 : 0);
  const activeRow = rowCount === 0 ? -1 : Math.min(active, rowCount - 1);
  const optionId = (index: number) => `${listId}-option-${index}`;
  const createId = `${listId}-create`;

  function openList() {
    if (open) return;
    setOpen(true);
    setBrowsing(true);
    setQuery(selected?.label ?? "");
    setActive(0);
  }

  function choose(option: ComboOption) {
    onChange(option.value);
    setOpen(false);
    setBrowsing(false);
    setQuery("");
  }

  function create() {
    if (!onCreate || !typed) return;
    setOpen(false);
    setBrowsing(false);
    setQuery("");
    void onCreate(typed);
  }

  function onKeyDown(event: KeyboardEvent<HTMLInputElement>) {
    if (event.key === "ArrowDown" || event.key === "ArrowUp") {
      event.preventDefault();
      if (!open) return openList();
      if (rowCount === 0) return;
      const delta = event.key === "ArrowDown" ? 1 : -1;
      setActive((current) => (Math.min(current, rowCount - 1) + delta + rowCount) % rowCount);
      return;
    }
    if (!open) return;
    if (event.key === "Enter") {
      event.preventDefault();
      if (showCreate && activeRow === visible.length) create();
      else if (visible[activeRow]) choose(visible[activeRow]);
    } else if (event.key === "Escape") {
      event.preventDefault();
      setOpen(false);
      setQuery("");
    } else if (event.key === "Home" || event.key === "End") {
      event.preventDefault();
      if (rowCount > 0) setActive(event.key === "Home" ? 0 : rowCount - 1);
    }
  }

  return (
    <div
      className="flex min-w-0 flex-col gap-1.5 text-[13px] font-bold"
      onBlur={(event) => {
        if (!event.currentTarget.contains(event.relatedTarget as Node | null)) {
          setOpen(false);
          setQuery("");
        }
      }}
    >
      <label htmlFor={inputId}>{label}</label>
      <div className="relative min-w-0">
        <input
          id={inputId} name={name} role="combobox" aria-expanded={open} aria-controls={listId} aria-autocomplete="list"
          aria-activedescendant={open && activeRow >= 0 ? (showCreate && activeRow === visible.length ? createId : optionId(activeRow)) : undefined}
          aria-invalid={error ? true : undefined} aria-describedby={error ? errorId : undefined}
          autoComplete="off" dir="auto" value={open ? query : selected?.label ?? ""}
          placeholder={placeholder ?? t("search")}
          onChange={(event) => {
            setQuery(event.target.value);
            setBrowsing(false);
            setOpen(true);
            setActive(0);
          }}
          onFocus={(event) => {
            openList();
            event.currentTarget.select();
          }}
          onClick={openList}
          onKeyDown={onKeyDown}
          className={`h-11 w-full rounded-control border bg-surface px-3 text-sm font-normal text-ink ${error ? "border-error" : "border-input-line"}`}
        />
        {open ? (
          <ul
            id={listId} role="listbox" aria-label={label}
            onMouseDown={(event) => event.preventDefault()}
            className="absolute inset-x-0 top-full z-20 m-0 mt-1 flex max-h-56 list-none flex-col overflow-y-auto rounded-control border border-input-line bg-surface p-1 shadow-lift"
          >
            {visible.map((option, index) => (
              <li
                key={option.value} id={optionId(index)} role="option" aria-selected={option.value === value} dir="auto"
                onClick={() => choose(option)}
                className={`flex min-h-11 cursor-pointer items-center rounded-control px-2.5 text-start text-sm ${index === activeRow ? "bg-paper font-bold" : "hover:bg-paper"}`}
              >
                {option.label}
              </li>
            ))}
            {showCreate ? (
              <li
                id={createId} role="option" aria-selected={false} onClick={create} dir="auto"
                className={`flex min-h-11 cursor-pointer items-center rounded-control px-2.5 text-start text-sm text-orange-text ${activeRow === visible.length ? "bg-paper font-bold" : "hover:bg-paper"}`}
              >
                {t("create", { text: typed })}
              </li>
            ) : null}
            {visible.length === 0 && !showCreate ? <li role="presentation" className="px-2.5 py-2 text-sm font-normal text-ink-muted">{t("empty")}</li> : null}
          </ul>
        ) : null}
      </div>
      {error ? <span id={errorId} role="alert" className="text-xs font-normal text-error">{error}</span> : null}
    </div>
  );
}
