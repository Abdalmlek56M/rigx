import type { ReactNode } from "react";

/** Re-mounts on every navigation, so each storefront page gets the short fade-in (`.page-in`, off with reduced motion). */
export default function StoreTemplate({ children }: { children: ReactNode }) {
  return <div className="page-in">{children}</div>;
}
