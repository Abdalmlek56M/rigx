import type { ReactNode } from "react";

/** One stacked section of the system page: a title, a one-line hint and its content. */
export function SectionCard({ title, hint, children, danger = false }: { title: string; hint?: string; children: ReactNode; danger?: boolean }) {
  return (
    <section className={`flex min-w-0 flex-col gap-3 rounded-card border bg-surface p-5 ${danger ? "border-error/40" : "border-line"}`}>
      <div className="flex flex-col gap-1">
        <h2 className={`m-0 text-lg font-bold ${danger ? "text-error" : ""}`}>{title}</h2>
        {hint ? <p className="m-0 text-sm text-ink-muted">{hint}</p> : null}
      </div>
      {children}
    </section>
  );
}
