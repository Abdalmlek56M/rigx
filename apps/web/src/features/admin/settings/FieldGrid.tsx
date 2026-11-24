import type { ReactNode } from "react";

/** Short fields (numbers, names, a choice) in an even grid: one column on a phone, two from `sm`, three from `lg`, never stretched across the page. */
export function FieldGrid({ children }: { children: ReactNode }) {
  return <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">{children}</div>;
}

/** A titled group inside a settings section: a hairline above, the title, then the fields. */
export function SubSection({ title, children }: { title: string; children: ReactNode }) {
  return (
    <div className="flex flex-col gap-3 border-t border-line pt-3">
      <h3 className="m-0 text-[15px] font-bold">{title}</h3>
      {children}
    </div>
  );
}

/** The small grey line under a group of fields. */
export function FieldNote({ children }: { children: ReactNode }) {
  return <p className="m-0 text-xs text-ink-muted">{children}</p>;
}
