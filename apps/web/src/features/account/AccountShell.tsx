"use client";

import { useEffect, useRef, type ReactNode } from "react";
import { useTranslations } from "next-intl";
import { loginHref } from "@/features/auth/auth-rules";
import { signOut } from "@/features/auth/session-store";
import { useSession } from "@/features/auth/use-session";
import { Link, usePathname, useRouter } from "@/i18n/navigation";
import { cardClass } from "@/components/ui/Card";
import { PageHeader } from "@/components/ui/PageHeader";
import { SkeletonRows } from "@/components/ui/Skeleton";
import { useTabIndicator } from "@/components/ui/use-tab-indicator";
import { BoxIcon, CaseIcon, DocumentIcon, HeartIcon, InboxIcon, LogoutIcon, MapPinIcon, ShieldIcon, UserIcon } from "@/components/ui/icons";
import { ConfirmButton } from "./ConfirmButton";
import { ACCOUNT_NAV, isActiveNav } from "./account-labels";

const NAV_ICONS = {
  orders: BoxIcon, favourites: HeartIcon, warranties: ShieldIcon, invoices: DocumentIcon, requests: InboxIcon, builds: CaseIcon, addresses: MapPinIcon, profile: UserIcon
} as const;

/** The frame of every account page: greeting, side nav (tabs on mobile), sign-out; sends signed-out visitors to sign-in. */
export function AccountShell({ children }: { children: ReactNode }) {
  const t = useTranslations("account");
  const session = useSession();
  const router = useRouter();
  const pathname = usePathname();

  const activeLink = useRef<HTMLAnchorElement | null>(null);
  const navRow = useTabIndicator<HTMLElement>(ACCOUNT_NAV.find((item) => isActiveNav(item.href, pathname))?.href);

  useEffect(() => {
    if (session.status === "signedOut") router.replace(loginHref(pathname));
  }, [session.status, router, pathname]);

  // On a phone the tabs scroll sideways: bring the current page's tab into view.
  useEffect(() => {
    activeLink.current?.scrollIntoView({ inline: "center", block: "nearest" });
  }, [pathname, session.status]);

  async function leave() {
    await signOut();
    router.replace("/");
  }

  const user = session.status === "signedIn" ? session.user : null;

  return (
    <section className="shell scroll-mt-36 pb-16 md:scroll-mt-24">
      <PageHeader
        contained={false}
        title={user ? t("greeting", { name: user.fullName || user.email }) : <span aria-hidden="true" className="inline-block h-8 w-56 animate-pulse rounded-control bg-line" />}
        actions={user ? <ConfirmButton iconOnly label={t("signOut")} icon={LogoutIcon} onConfirm={() => void leave()} /> : undefined}
      />
      <div className="grid items-start gap-5 md:grid-cols-[240px_minmax(0,1fr)]">
        <nav ref={navRow} aria-label={t("navLabel")} className={cardClass({ padding: "sm", className: "tabs-slide -mx-4 flex-row gap-1 overflow-x-auto px-4 md:mx-0 md:flex-col md:overflow-visible md:px-4" })}>
          <span aria-hidden="true" className="tab-indicator" />
          {ACCOUNT_NAV.map((item) => {
            const active = isActiveNav(item.href, pathname);
            const NavIcon = NAV_ICONS[item.key];
            return (
              <Link
                key={item.href} href={item.href} aria-current={active ? "page" : undefined} data-active={active} ref={active ? activeLink : undefined}
                className={`relative flex min-h-11 shrink-0 items-center gap-2.5 whitespace-nowrap rounded-control px-3 text-sm font-bold transition-colors ${active ? "" : "text-ink hover:bg-paper"}`}
              >
                <NavIcon size={18} />
                {t(`nav.${item.key}`)}
              </Link>
            );
          })}
        </nav>
        <div className="min-w-0">{user ? children : <SkeletonRows rows={3} />}</div>
      </div>
    </section>
  );
}
