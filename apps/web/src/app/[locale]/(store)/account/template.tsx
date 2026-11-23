import type { ReactNode } from "react";

/** Next scrolls the first element of a new page into view; the margin keeps it below the sticky header (129 px on a phone, 79 px from md). The side nav stays mounted (its indicator slides); only the page beside it re-mounts and rises in (`.page-in`, off with reduced motion). */
export default function AccountTemplate({ children }: { children: ReactNode }) {
  return <div className="page-in *:scroll-mt-36 md:*:scroll-mt-24">{children}</div>;
}
