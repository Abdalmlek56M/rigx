import type { ComponentType, ReactNode } from "react";
import { Card } from "./Card";

interface EmptyStateProps {
  /** The outline mark above the title. */
  icon: ComponentType<{ size?: number }>;
  title: string;
  /** One line that tells the person what to do next. */
  help?: string;
  /** One action at most (a button or a link). */
  action?: ReactNode;
  className?: string;
}

/** The empty state of any list: an icon, a title, one line and one action, centred in a card. */
export function EmptyState({ icon: Icon, title, help, action, className }: EmptyStateProps) {
  return (
    <Card className={`items-center gap-3 text-center ${className ?? ""}`} padding="lg">
      <span aria-hidden="true" className="flex h-14 w-14 items-center justify-center rounded-full bg-paper text-ink-muted">
        <Icon size={26} />
      </span>
      <div className="flex flex-col gap-1">
        <h2 className="m-0 type-section-title">{title}</h2>
        {help ? <p className="m-0 text-sm leading-6 text-ink-muted">{help}</p> : null}
      </div>
      {action}
    </Card>
  );
}
