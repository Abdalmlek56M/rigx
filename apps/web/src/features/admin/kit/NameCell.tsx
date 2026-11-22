import type { ReactNode } from "react";

interface NameCellProps {
  /** The person or company, shown as typed (never forced to capitals). */
  name: ReactNode;
  /** A phone number or code: shown under the name in mono, always left-to-right. */
  detail?: string | null;
}

/** The name-and-phone cell of every admin table: a bold name with the phone under it, so lists read the same everywhere. */
export function NameCell({ name, detail }: NameCellProps) {
  return (
    <span className="flex min-w-0 flex-col gap-0.5">
      <span dir="auto" className="font-bold [unicode-bidi:isolate]">{name}</span>
      {detail ? <span dir="ltr" className="text-start font-mono text-xs text-ink-muted">{detail}</span> : null}
    </span>
  );
}
