import type { ReactNode } from "react";
import { cardClass } from "@/components/ui/Card";

/** A white dashboard card with a title, an optional action at the end and an optional footnote. */
export function Panel({ title, action, note, children }: { title: ReactNode; action?: ReactNode; note?: string; children: ReactNode }) {
  return (
    <section className={cardClass({ className: "gap-3.5" })}>
      <div className="flex flex-wrap items-center justify-between gap-2.5">
        <h2 className="m-0 type-section-title">{title}</h2>
        {action}
      </div>
      {children}
      {note ? <p className="m-0 mt-auto text-xs text-ink-muted">{note}</p> : null}
    </section>
  );
}

/** A small table inside a panel; scrolls sideways on phones. */
export function PanelTable({ headers, children }: { headers: readonly string[]; children: ReactNode }) {
  return (
    <div className="overflow-x-auto">
      <table className="data-table w-full border-collapse text-sm">
        <thead className="text-[13px] text-ink-muted">
          <tr>{headers.map((header, index) => <th key={index} scope="col" className="whitespace-nowrap px-2 py-2 text-start font-bold">{header}</th>)}</tr>
        </thead>
        <tbody>{children}</tbody>
      </table>
    </div>
  );
}

export function Empty({ message }: { message: string }) {
  return <p className="m-0 py-4 text-center text-sm text-ink-muted">{message}</p>;
}
