import type { ComponentType, ReactNode } from "react";

/** A calm admin section: a bordered card with an icon and a title row, then its content. */
export function SectionCard({ title, icon: Icon, actions, children, className }: {
  title: string;
  icon?: ComponentType<{ size?: number }>;
  actions?: ReactNode;
  children: ReactNode;
  className?: string;
}) {
  return (
    <section className={`flex min-w-0 flex-col gap-3 rounded-card border border-line bg-surface p-5 ${className ?? ""}`}>
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h2 className="m-0 flex items-center gap-2 text-[17px] font-bold">
          {Icon ? <span className="text-ink-muted"><Icon size={18} /></span> : null}
          {title}
        </h2>
        {actions ? <div className="flex flex-wrap items-center gap-2">{actions}</div> : null}
      </div>
      {children}
    </section>
  );
}
