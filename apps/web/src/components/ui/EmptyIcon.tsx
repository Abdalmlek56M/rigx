import type { ComponentType } from "react";

/** The quiet round mark above an empty state ("السلة فاضية", "مفيش نتايج"). */
export function EmptyIcon({ icon: Glyph }: { icon: ComponentType<{ size?: number }> }) {
  return (
    <span aria-hidden="true" className="flex h-14 w-14 items-center justify-center rounded-full bg-paper text-ink-muted">
      <Glyph size={26} />
    </span>
  );
}
