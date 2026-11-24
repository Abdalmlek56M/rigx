import type { HTMLInputTypeAttribute, ReactNode } from "react";
import { latinNumber } from "@/lib/digits";
import { fieldClass } from "./field-class";

export interface TextFieldProps {
  label: string;
  /** A small pill or note shown at the end of the label (for example "اختياري"). */
  labelEnd?: ReactNode;
  name: string;
  value: string;
  onChange: (value: string) => void;
  type?: HTMLInputTypeAttribute;
  /** Numbers, emails and codes read left-to-right. */
  ltr?: boolean;
  autoComplete?: string;
  placeholder?: string;
  required?: boolean;
  error?: string | null;
  /** The text's own direction when it is in a fixed language (an Arabic or English copy), whatever the page language. */
  dir?: "ltr" | "rtl";
  /** The keypad a phone/email field asks for on touch screens. */
  inputMode?: "none" | "text" | "tel" | "url" | "email" | "numeric" | "decimal" | "search";
  /** Bounds for date/number inputs (e.g. no future date). */
  max?: string;
  min?: string;
  /** Number steps: 1 for a whole number, 0.01 for money/percent with two decimals. */
  step?: string;
  /** The server's length limit, so the field cannot go past it. */
  maxLength?: number;
  /** Enter in the field does this instead of its default (a barcode scanner ends a scan with Enter). */
  onEnter?: () => void;
  /** The language of the value (a date field shows its parts in it where the browser allows). */
  lang?: string;
  /** Takes the focus when the field appears (the next line of a scanned list). */
  autoFocus?: boolean;
  /** Extra classes on the label text (`md:sr-only` when a header row above names the column). */
  labelClassName?: string;
}

/**
 * A labelled text input; `error` is announced and linked to the input. A number field is a text input with a number keypad, so digits typed
 * on an Arabic keyboard are accepted (as Western digits) instead of being silently ignored.
 */
export function TextField({ label, labelEnd, name, value, onChange, type = "text", ltr = false, autoComplete, placeholder, required, error, dir, inputMode, max, min, step, maxLength, onEnter, autoFocus, lang, labelClassName }: TextFieldProps) {
  const errorId = `${name}-error`;
  const numeric = type === "number";
  return (
    <label className="flex min-w-0 flex-col gap-1.5 text-xs font-bold">
      <span className={`flex min-w-0 flex-wrap items-center gap-1.5${labelClassName ? ` ${labelClassName}` : ""}`}>{label}{labelEnd}</span>
      <input
        name={name}
        type={numeric ? "text" : type}
        inputMode={numeric ? (step === "1" ? "numeric" : "decimal") : inputMode}
        value={value}
        required={required}
        max={numeric ? undefined : max}
        min={numeric ? undefined : min}
        step={numeric ? undefined : step}
        maxLength={maxLength}
        autoComplete={autoComplete}
        autoFocus={autoFocus}
        lang={lang}
        placeholder={placeholder}
        onKeyDown={onEnter ? (event) => {
          if (event.key !== "Enter") return;
          event.preventDefault();
          onEnter();
        } : undefined}
        dir={ltr ? "ltr" : dir}
        aria-invalid={error ? true : undefined}
        aria-describedby={error ? errorId : undefined}
        onChange={(event) => onChange(numeric ? latinNumber(event.target.value) : event.target.value)}
        className={`${fieldClass}${ltr ? " text-start" : ""}`}
      />
      {error ? (
        <span id={errorId} role="alert" className="text-xs font-normal text-error-text">
          {error}
        </span>
      ) : null}
    </label>
  );
}
