"use client";

import { useState, type ReactNode } from "react";

export interface LatestTab {
  id: string;
  label: string;
  content: ReactNode;
}

/** The "وصل حديثًا" heading with an "all / used / new" toggle over lists the server already rendered. */
export function LatestTabs({ title, groupLabel, tabs }: { title: string; groupLabel: string; tabs: LatestTab[] }) {
  const [active, setActive] = useState(tabs[0]?.id);

  return (
    <div className="flex flex-col gap-5">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h2 className="m-0 text-[22px] font-bold md:text-[30px]">{title}</h2>
        <div role="group" aria-label={groupLabel} className="flex gap-1.5">
          {tabs.map((tab) => (
            <button
              key={tab.id}
              type="button"
              aria-pressed={tab.id === active}
              onClick={() => setActive(tab.id)}
              className={`h-11 rounded-control px-4 text-sm ${tab.id === active ? "bg-carbon text-on-carbon" : "border border-input-line"}`}
            >
              {tab.label}
            </button>
          ))}
        </div>
      </div>
      {tabs.map((tab) => (
        <div key={tab.id} hidden={tab.id !== active}>
          {tab.content}
        </div>
      ))}
    </div>
  );
}
