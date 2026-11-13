"use client";

import type { ReactNode } from "react";
import { AccountNav } from "@/components/account-nav";

/** Reference account layout: fixed navigation rail plus a single content panel. */
export function AccountShell({ children }: { children: ReactNode }) {
  return (
    <main className="account-page">
      <AccountNav />
      <section className="account-main glass-panel">{children}</section>
    </main>
  );
}
