"use client";

import { useTranslations } from "next-intl";
import type { PosField, PosForm } from "./pos-rules";

export type SetForm = <K extends keyof PosForm>(key: K, value: PosForm[K]) => void;

/** The message for a flagged field, or `null` when the field is fine. */
export function usePosError(errors: readonly PosField[]): (field: PosField) => string | null {
  const t = useTranslations("admin.pos.errors");
  return (field) => (errors.includes(field) ? t(field) : null);
}

interface MoneyInputProps {
  label: string;
  value: string;
  onChange: (value: string) => void;
  invalid?: boolean;
  className?: string;
}

/** A bare money input for table cells and rows (the label is read by screen readers). */
export function MoneyInput({ label, value, onChange, invalid = false, className = "w-28" }: MoneyInputProps) {
  return (
    <input
      type="text" inputMode="decimal" dir="ltr" aria-label={label} value={value} aria-invalid={invalid || undefined}
      onChange={(event) => onChange(event.target.value)}
      className={`h-11 rounded-control border bg-surface px-3 text-start text-sm ${invalid ? "border-error" : "border-input-line"} ${className}`}
    />
  );
}

