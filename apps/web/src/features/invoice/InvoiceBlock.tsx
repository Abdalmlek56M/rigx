import type { ReactNode } from "react";

export interface Fact {
  label: string;
  value: string | null | undefined;
  /** Numbers, e-mails and codes read left-to-right inside the Arabic page. */
  ltr?: boolean;
}

/** One framed block of the invoice's head (the store, the customer or the invoice itself): a titled strip over "label: value" lines. */
export function InvoiceBlock({ title, name, facts }: { title: string; name?: ReactNode; facts: Fact[] }) {
  return (
    <section className="flex break-inside-avoid flex-col overflow-hidden rounded-control border border-line">
      <h2 className="m-0 border-b border-line bg-paper px-3 py-1.5 text-xs font-bold [print-color-adjust:exact]">{title}</h2>
      <div className="flex flex-col gap-1.5 px-3 py-2.5">
        {name ? <b dir="auto" className="text-sm [unicode-bidi:isolate]">{name}</b> : null}
        <dl className="m-0 grid grid-cols-[auto_1fr] gap-x-2.5 gap-y-1 text-xs">
          {facts.filter((fact) => fact.value).map((fact) => (
            <div key={fact.label} className="contents">
              <dt className="whitespace-nowrap text-ink-muted">{fact.label}</dt>
              <dd className="m-0 min-w-0 text-start font-bold [overflow-wrap:anywhere]">
                <span dir={fact.ltr ? "ltr" : "auto"} className="[unicode-bidi:isolate]">{fact.value}</span>
              </dd>
            </div>
          ))}
        </dl>
      </div>
    </section>
  );
}
