"use client";

import { useEffect, type ReactNode } from "react";
import { useTranslations } from "next-intl";
import { loginHref } from "@/features/auth/auth-rules";
import { signOut } from "@/features/auth/session-store";
import { useSession } from "@/features/auth/use-session";
import { Link, usePathname, useRouter } from "@/i18n/navigation";
import { BoxIcon, CaseIcon, DocumentIcon, InboxIcon, MapPinIcon, ShieldIcon, UserIcon } from "@/components/ui/icons";
import { ACCOUNT_NAV, isActiveNav } from "./account-labels";
import { SkeletonRows } from "./LoadBoundary";

const NAV_ICONS = {
  orders: BoxIcon, warranties: ShieldIcon, invoices: DocumentIcon, requests: InboxIcon, builds: CaseIcon, addresses: MapPinIcon, profile: UserIcon
} as const;

/** The frame of every account page: greeting, side nav (tabs on mobile), sign-out; sends signed-out visitors to sign-in. */
export function AccountShell({ children }: { children: ReactNode }) {
  const t = useTranslations("account");
  const session = useSession();
  const router = useRouter();
  const pathname = usePathname();

  useEffect(() => {
    if (session.status === "signedOut") router.replace(loginHref(pathname));
  }, [session.status, router, pathname]);

  async function leave() {
    await signOut();
    router.replace("/");
  }

  const user = session.status === "signedIn" ? session.user : null;

  return (
    <section className="mx-auto max-w-[1240px] px-4 pb-16 pt-8 md:px-6 md:pt-10">
      <header className="mb-5 flex flex-wrap items-center justify-between gap-3">
        <h1 className="m-0 text-2xl font-bold md:text-[32px]">
          {user ? t("greeting", { name: user.fullName || user.email }) : <span aria-hidden="true" className="inline-block h-8 w-56 animate-pulse rounded-control bg-line" />}
        </h1>
        {user ? (
          <button type="button" onClick={leave} className="h-11 rounded-control border border-input-line bg-surface px-4 text-sm font-bold hover:bg-line">
            {t("signOut")}
          </button>
        ) : null}
      </header>

      <div className="grid items-start gap-5 md:grid-cols-[220px_1fr]">
        <nav aria-label={t("navLabel")} className="-mx-4 flex gap-1 overflow-x-auto px-4 md:mx-0 md:flex-col md:overflow-visible md:px-0">
          {ACCOUNT_NAV.map((item) => {
            const active = isActiveNav(item.href, pathname);
            const NavIcon = NAV_ICONS[item.key];
            return (
              <Link
                key={item.href} href={item.href} aria-current={active ? "page" : undefined}
                className={`flex min-h-11 shrink-0 items-center gap-2.5 whitespace-nowrap rounded-control px-4 text-sm font-bold ${active ? "bg-carbon text-on-carbon" : "text-ink hover:bg-line"}`}
              >
                <NavIcon size={18} />
                {t(`nav.${item.key}`)}
              </Link>
            );
          })}
        </nav>
        <div className="min-w-0">{user ? children : <SkeletonRows />}</div>
      </div>
    </section>
  );
}
