"use client";

import type { ReactNode } from "react";
import { useTranslations } from "next-intl";
import type { CheckoutFormState, FieldKey } from "./checkout-request";

export type SetField = <K extends keyof CheckoutFormState>(key: K, value: CheckoutFormState[K]) => void;

export interface SectionProps {
  form: CheckoutFormState;
  set: SetField;
  errors: readonly FieldKey[];
}

/** The message for a flagged field, or `null` when the field is fine. */
export function useFieldError(errors: readonly FieldKey[]): (key: FieldKey) => string | null {
  const t = useTranslations("checkout.errors");
  return (key) => (errors.includes(key) ? t(key) : null);
}

/** "اختياري" as a quiet pill after a label, not inline text. */
export function OptionalPill() {
  const t = useTranslations("checkout");
  return <span className="rounded-full bg-paper px-2 py-0.5 text-[11px] font-normal text-ink-muted">{t("optional")}</span>;
}

export function FormSection({ title, badge, hint, aside, children }: {
  title: string;
  /** A pill after the section title (for example "اختياري"). */
  badge?: ReactNode;
  hint?: string;
  aside?: ReactNode;
  children: ReactNode;
}) {
  return (
    <section className="flex flex-col gap-3.5 rounded-card border border-line bg-surface p-5 md:p-6">
      <div className="flex flex-wrap items-center justify-between gap-x-4 gap-y-1">
        <div className="flex flex-wrap items-center gap-2">
          <h2 className="m-0 text-lg font-bold">{title}</h2>
          {badge}
        </div>
        {aside}
      </div>
      {hint ? <p className="m-0 text-sm text-ink-muted">{hint}</p> : null}
      {children}
    </section>
  );
}

export function FieldAlert({ children }: { children: ReactNode }) {
  return children ? <p role="alert" className="m-0 text-xs text-error">{children}</p> : null;
}

interface ChipRadioProps {
  name: string;
  label: string;
  selected: boolean;
  onSelect: () => void;
  /** Optional groups let a second click clear the choice. */
  onClear?: () => void;
}

/** A pill that is a real radio input underneath (keyboard and screen readers work as for any radio group). */
export function ChipRadio({ name, label, selected, onSelect, onClear }: ChipRadioProps) {
  return (
    <label className="relative flex min-h-11 cursor-pointer items-center">
      <input
        type="radio"
        name={name}
        checked={selected}
        readOnly
        onClick={() => (selected && onClear ? onClear() : onSelect())}
        className="peer sr-only"
      />
      <span className="flex min-h-11 items-center rounded-full border border-input-line bg-surface px-4 text-sm transition-colors duration-150 peer-checked:border-carbon peer-checked:bg-carbon peer-checked:font-bold peer-checked:text-on-carbon peer-focus-visible:outline-2 peer-focus-visible:outline-offset-2 peer-focus-visible:outline-orange motion-reduce:transition-none">
        {label}
      </span>
    </label>
  );
}
