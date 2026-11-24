import type { ReactNode, Ref } from "react";
import { textAreaClass } from "./field-class";

interface TextAreaFieldProps {
  label: string;
  name: string;
  value: string;
  onChange: (value: string) => void;
  rows?: number;
  placeholder?: string;
  required?: boolean;
  error?: string | null;
  hint?: string;
  /** The server's length limit, so the field cannot go past it. */
  maxLength?: number;
  /** The text's own direction when it is in a fixed language (an Arabic or English copy), whatever the page language. */
  dir?: "ltr" | "rtl";
  /** A small note at the end of the label (a character count, "optional"). */
  labelEnd?: ReactNode;
  /** The textarea element, for a toolbar that works on the selection. */
  textareaRef?: Ref<HTMLTextAreaElement>;
}

/** A labelled multi-line input that matches `TextField`; `error` is announced and linked to the textarea. */
export function TextAreaField({ label, name, value, onChange, rows = 3, placeholder, required, error, hint, maxLength, dir, labelEnd, textareaRef }: TextAreaFieldProps) {
  const errorId = `${name}-error`;
  const hintId = `${name}-hint`;
  const describedBy = error ? errorId : hint ? hintId : undefined;
  return (
    <label className="flex min-w-0 flex-col gap-1.5 text-xs font-bold">
      <span className="flex min-w-0 flex-wrap items-center gap-1.5">{label}{labelEnd}</span>
      <textarea
        ref={textareaRef}
        name={name}
        value={value}
        rows={rows}
        required={required}
        maxLength={maxLength}
        dir={dir}
        placeholder={placeholder}
        aria-invalid={error ? true : undefined}
        aria-describedby={describedBy}
        onChange={(event) => onChange(event.target.value)}
        className={textAreaClass}
      />
      {hint && !error ? <span id={hintId} className="text-xs font-normal text-ink-muted">{hint}</span> : null}
      {error ? <span id={errorId} role="alert" className="text-xs font-normal text-error-text">{error}</span> : null}
    </label>
  );
}
