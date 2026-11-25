import type { ReactNode } from "react";
import { fieldClass } from "./field-class";

interface SelectFieldProps<T extends string> {
  label: string;
  /** A small pill or note shown at the end of the label (for example "اختياري"). */
  labelEnd?: ReactNode;
  name: string;
  value: T;
  onChange: (value: T) => void;
  options: readonly { value: T; label: string; disabled?: boolean }[];
  error?: string | null;
  disabled?: boolean;
  /** Extra classes on the label text (`md:sr-only` when a header row above names the column). */
  labelClassName?: string;
}

/** A labelled dropdown that matches `TextField`. */
export function SelectField<T extends string>({ label, labelEnd, name, value, onChange, options, error, disabled, labelClassName }: SelectFieldProps<T>) {
  const errorId = `${name}-error`;
  return (
    <label className="flex min-w-0 flex-col gap-1.5 text-xs font-bold">
      <span className={`flex min-w-0 flex-wrap items-center gap-1.5${labelClassName ? ` ${labelClassName}` : ""}`}>{label}{labelEnd}</span>
      <select
        name={name}
        value={value}
        disabled={disabled}
        aria-invalid={error ? true : undefined}
        aria-describedby={error ? errorId : undefined}
        onChange={(event) => {
          const chosen = options.find((option) => option.value === event.target.value);
          if (chosen) onChange(chosen.value);
        }}
        className={fieldClass}
      >
        {options.map((option) => (
          <option key={option.value} value={option.value} disabled={option.disabled}>{option.label}</option>
        ))}
      </select>
      {error ? (
        <span id={errorId} role="alert" className="text-xs font-normal text-error-text">
          {error}
        </span>
      ) : null}
    </label>
  );
}
