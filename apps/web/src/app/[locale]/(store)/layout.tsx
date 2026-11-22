import type { ReactNode } from "react";
import { SiteShell } from "@/components/layout/SiteShell";

/**
 * Every storefront page is rendered per request. A copy built during a deploy (API not up yet) or cached while the API
 * was down would show the safe defaults (no language switch, no WhatsApp buttons, no governorates) to the first visitor;
 * the API calls themselves stay cached for a few minutes (`lib/api/server.ts`), so this costs little.
 */
export const dynamic = "force-dynamic";

/** Every storefront page sits in the site frame; the admin has its own. */
export default function StoreLayout({ children }: { children: ReactNode }) {
  return <SiteShell>{children}</SiteShell>;
}
