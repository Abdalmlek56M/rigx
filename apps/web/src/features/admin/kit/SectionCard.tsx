import type { ComponentType, ReactNode } from "react";
import { cardClass } from "@/components/ui/Card";

/** A calm admin section: the store's card (same surface and padding) with an icon and a title row, then its content. */
export function SectionCard({ title, icon: Icon, actions, children, className }: {
  title: string;
  icon?: ComponentType<{ size?: number }>;
  actions?: ReactNode;
  children: ReactNode;
  className?: string;
}) {
  return (
    <section className={cardClass({ className: `gap-3 ${className ?? ""}` })}>
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h2 className="m-0 flex min-w-0 items-center gap-2 type-section-title">
          {Icon ? <span aria-hidden="true" className="flex text-ink-muted"><Icon size={18} /></span> : null}
          {title}
        </h2>
        {actions ? <div className="flex flex-wrap items-center gap-2">{actions}</div> : null}
      </div>
      {children}
    </section>
  );
}
