"use client";

import { useId, useState, type ReactNode } from "react";
import { ChevronDownIcon } from "@/components/ui/icons";

/**
 * One filter group with a title row that folds it away and back. Open by default; the choice survives clicking a filter, since
 * the catalog re-renders in place. The body slides with a grid-row transition and is inert while folded.
 */
export function FilterGroup({ icon, title, children }: { icon: ReactNode; title: string; children: ReactNode }) {
  const [open, setOpen] = useState(true);
  const bodyId = useId();

  return (
    <section className="flex flex-col border-b border-line pb-3 last:border-b-0 last:pb-0">
      <h3 className="m-0">
        <button
          type="button"
          aria-expanded={open}
          aria-controls={bodyId}
          onClick={() => setOpen((value) => !value)}
          className="flex min-h-10 w-full items-center gap-2 rounded-control text-start text-sm font-bold transition-colors hover:text-orange-text lg:min-h-9"
        >
          <span aria-hidden="true" className="flex text-orange-text">{icon}</span>
          <span className="min-w-0 flex-1">{title}</span>
          <span aria-hidden="true" className={`flex text-ink-muted transition-transform duration-200 motion-reduce:transition-none ${open ? "rotate-180" : ""}`}>
            <ChevronDownIcon size={16} />
          </span>
        </button>
      </h3>
      <div
        id={bodyId}
        inert={!open}
        className={`grid transition-[grid-template-rows] duration-200 ease-out motion-reduce:transition-none ${open ? "grid-rows-[1fr]" : "grid-rows-[0fr]"}`}
      >
        <div className="min-h-0 overflow-hidden">
          <div className="pt-1">{children}</div>
        </div>
      </div>
    </section>
  );
}
