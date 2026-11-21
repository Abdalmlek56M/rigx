import type { ComponentType, ReactNode } from "react";
import { CopyButton } from "./CopyButton";

export type InfoColumns = 1 | 2 | 3;

const COLUMNS: Record<InfoColumns, string> = {
  1: "grid-cols-1",
  2: "grid-cols-1 sm:grid-cols-2",
  3: "grid-cols-2 md:grid-cols-3"
};

interface InfoGridProps {
  /** 1, 2 or 3 columns on wide screens; phones always read one field per row (except the invoice's compact 3-column block). */
  columns?: InfoColumns;
  className?: string;
  children: ReactNode;
}

/** The label-above-value grid used everywhere a set of facts is shown (account, tracking, invoice, product facts). */
export function InfoGrid({ columns = 2, className = "", children }: InfoGridProps) {
  return <dl className={`m-0 grid gap-x-6 gap-y-4 ${COLUMNS[columns]}${className ? ` ${className}` : ""}`}>{children}</dl>;
}

interface InfoFieldProps {
  /** The small muted label above the value, for example "البريد الإلكتروني". */
  label: string;
  value: ReactNode;
  /** An icon beside the label, for the kind of fact (phone, address, date). */
  icon?: ComponentType<{ size?: number }>;
  /** Phone numbers, e-mails and codes read left-to-right inside the Arabic page. */
  ltr?: boolean;
  /** Spans the full row (a long address). */
  wide?: boolean;
  /** Shows a copy button after the value. */
  copy?: { label: string; copiedLabel: string; value: string };
  className?: string;
}

/** One fact: label 12 px muted above value 14 px bold. An empty value renders nothing, so grids never show "—". */
export function InfoField({ label, value, icon: Icon, ltr = false, wide = false, copy, className = "" }: InfoFieldProps) {
  const empty = value === null || value === undefined || value === "";
  if (empty) return null;
  return (
    <div className={`flex min-w-0 flex-col gap-1 ${wide ? "col-span-full" : ""}${className ? ` ${className}` : ""}`}>
      <dt className="flex items-center gap-1.5 type-label">
        {Icon ? <span aria-hidden="true" className="flex"><Icon size={14} /></span> : null}
        {label}
      </dt>
      <dd className="m-0 flex min-w-0 items-center gap-1.5">
        <span
          dir={ltr ? "ltr" : "auto"}
          className={`min-w-0 type-value ${ltr ? "text-start font-mono" : "[unicode-bidi:isolate]"}`}
        >
          {value}
        </span>
        {copy ? <CopyButton value={copy.value} label={copy.label} copiedLabel={copy.copiedLabel} /> : null}
      </dd>
    </div>
  );
}
