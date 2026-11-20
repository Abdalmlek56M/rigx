import type { HTMLInputTypeAttribute } from "react";
import { latinNumber } from "@/lib/digits";

interface TextFieldProps {
  label: string;
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
  /** Bounds for date/number inputs (e.g. no future date). */
  max?: string;
  min?: string;
  /** Number steps: 1 for a whole number, 0.01 for money/percent with two decimals. */
  step?: string;
  /** The server's length limit, so the field cannot go past it. */
  maxLength?: number;
}

/**
 * A labelled text input; `error` is announced and linked to the input. A number field is a text input with a number keypad, so digits typed
 * on an Arabic keyboard are accepted (as Western digits) instead of being silently ignored.
 */
export function TextField({ label, name, value, onChange, type = "text", ltr = false, autoComplete, placeholder, required, error, dir, max, min, step, maxLength }: TextFieldProps) {
  const errorId = `${name}-error`;
  const numeric = type === "number";
  return (
    <label className="flex min-w-0 flex-col gap-1.5 text-[13px] font-bold">
      {label}
      <input
        name={name}
        type={numeric ? "text" : type}
        inputMode={numeric ? (step === "1" ? "numeric" : "decimal") : undefined}
        value={value}
        required={required}
        max={numeric ? undefined : max}
        min={numeric ? undefined : min}
        step={numeric ? undefined : step}
        maxLength={maxLength}
        autoComplete={autoComplete}
        placeholder={placeholder}
        dir={ltr ? "ltr" : dir}
        aria-invalid={error ? true : undefined}
        aria-describedby={error ? errorId : undefined}
        onChange={(event) => onChange(numeric ? latinNumber(event.target.value) : event.target.value)}
        className={`h-12 w-full rounded-control border border-input-line bg-surface px-3 text-sm font-normal text-ink ${ltr ? "text-start" : ""}`}
      />
      {error ? (
        <span id={errorId} role="alert" className="text-xs font-normal text-error">
          {error}
        </span>
      ) : null}
    </label>
  );
}
