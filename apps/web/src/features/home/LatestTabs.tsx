"use client";

import { useState, type ReactNode } from "react";
import { useTabIndicator } from "@/components/ui/use-tab-indicator";

export interface LatestTab {
  id: string;
  label: string;
  content: ReactNode;
}

/** The "وصل حديثًا" heading with an "all / used / new" toggle over lists the server already rendered. The graphite pill slides to the chosen tab and its list rises in. */
export function LatestTabs({ title, groupLabel, tabs }: { title: string; groupLabel: string; tabs: LatestTab[] }) {
  const [active, setActive] = useState(tabs[0]?.id);
  const [switched, setSwitched] = useState(false);
  const row = useTabIndicator<HTMLDivElement>(active);

  return (
    <div className="flex flex-col gap-5">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h2 className="m-0 type-display-title title-hook">{title}</h2>
        <div ref={row} role="group" aria-label={groupLabel} className="tabs-slide flex gap-1 rounded-full border border-line bg-surface p-1 [--ind-radius:9999px]">
          <span aria-hidden="true" className="tab-indicator" />
          {tabs.map((tab) => (
            <button
              key={tab.id}
              type="button"
              aria-pressed={tab.id === active}
              data-active={tab.id === active}
              onClick={() => { setActive(tab.id); setSwitched(true); }}
              className={`relative h-9 rounded-full px-4 text-sm font-bold transition-colors ${tab.id === active ? "" : "text-ink hover:bg-paper"}`}
            >
              {tab.label}
            </button>
          ))}
        </div>
      </div>
      {tabs.map((tab) => (
        <div key={tab.id} hidden={tab.id !== active} className={switched ? "tab-rise" : undefined}>
          {tab.content}
        </div>
      ))}
    </div>
  );
}
