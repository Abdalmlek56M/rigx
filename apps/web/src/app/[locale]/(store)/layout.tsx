import type { ReactNode } from "react";
import { SiteShell } from "@/components/layout/SiteShell";

/** Every storefront page sits in the site frame; the admin has its own. */
export default function StoreLayout({ children }: { children: ReactNode }) {
  return <SiteShell>{children}</SiteShell>;
}
