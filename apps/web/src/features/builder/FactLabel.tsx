import type { ComponentType } from "react";

/** The start of a row in the build's facts: a small orange icon in a soft circle, then the label. */
export function FactLabel({ icon: Icon, children }: { icon: ComponentType<{ size?: number }>; children: string }) {
  return (
    <span className="flex min-w-0 items-center gap-2.5 text-ink-muted">
      <span aria-hidden="true" className="flex size-8 shrink-0 items-center justify-center rounded-full bg-orange-soft text-orange-text">
        <Icon size={16} />
      </span>
      <span className="whitespace-nowrap font-semibold">{children}</span>
    </span>
  );
}
