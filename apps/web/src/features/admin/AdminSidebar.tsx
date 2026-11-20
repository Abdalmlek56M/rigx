"use client";

import { useLocale, useTranslations } from "next-intl";
import {
  BoxIcon, CartIcon, CaseIcon, ClockIcon, DatabaseIcon, DocumentIcon, FolderIcon, GearIcon, GridIcon, HandshakeIcon, HelpIcon,
  InboxIcon, KeyIcon, RegisterIcon, ShieldIcon, StorageIcon, TagIcon, TicketIcon, TruckIcon, UsersIcon, WalletIcon
} from "@/components/ui/icons";
import { Logo } from "@/components/ui/Logo";
import type { SessionUser } from "@/features/auth/session-store";
import { Link, usePathname } from "@/i18n/navigation";
import type { AdminArea, AdminAreaKey, AdminGroup } from "./admin-nav";
import { AdminUserMenu } from "./AdminUserMenu";

const AREA_ICONS: Record<AdminAreaKey, typeof GridIcon> = {
  dashboard: GridIcon, pos: RegisterIcon, orders: BoxIcon, customers: UsersIcon, offers: TagIcon, coupons: TicketIcon,
  inventory: StorageIcon, builds: CaseIcon, purchases: TruckIcon, stockImport: DocumentIcon, categories: FolderIcon, brands: ShieldIcon,
  partners: HandshakeIcon, expenses: WalletIcon,
  requests: InboxIcon,
  articles: DocumentIcon, faq: HelpIcon, policies: DocumentIcon,
  users: KeyIcon, audit: ClockIcon, system: DatabaseIcon, settings: GearIcon
};

/** A quiet control on the dark sidebar (store link, language). */
const SIDEBAR_CHIP =
  "flex h-10 items-center rounded-control border border-footer-line text-sm font-bold text-on-carbon hover:bg-footer-line " +
  "focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-orange";

interface AdminSidebarProps {
  user: SessionUser;
  groups: readonly AdminGroup[];
  active: AdminArea | null;
  onNavigate?: () => void;
  onSignOut: () => void;
}

/** The dark admin sidebar: logo and store link, one link per area the user may open, and the user menu at the bottom. */
export function AdminSidebar({ user, groups, active, onNavigate, onSignOut }: AdminSidebarProps) {
  const t = useTranslations("admin");
  const tLocale = useTranslations("locale");
  const pathname = usePathname();
  const otherLocale = useLocale() === "ar" ? "en" : "ar";

  return (
    <nav aria-label={t("shell.navLabel")} className="flex h-full flex-col bg-chrome px-3 py-[18px] text-footer-text">
      <div className="flex items-center justify-between px-2 pb-3">
        <Link href="/admin" onClick={onNavigate} aria-label={t("shell.home")} className="text-on-carbon"><Logo width={96} /></Link>
        <span className="font-mono text-[10px] tracking-[1px] text-muted">ADMIN</span>
      </div>
      <div className="flex gap-2 px-2 pb-3">
        <Link
          href="/" target="_blank" rel="noopener" onClick={onNavigate}
          className={`${SIDEBAR_CHIP} flex-1 justify-center gap-2`}
        >
          <CartIcon size={16} />
          {t("shell.viewStore")}
        </Link>
        <Link
          href={pathname} locale={otherLocale} hrefLang={otherLocale} onClick={onNavigate} aria-label={tLocale("switchAria")}
          className={`${SIDEBAR_CHIP} w-12 justify-center font-mono text-xs`}
        >
          {tLocale("switchLabel")}
        </Link>
      </div>

      <div className="flex min-h-0 flex-1 flex-col gap-1 overflow-y-auto [scrollbar-color:var(--color-footer-line)_transparent] [scrollbar-width:thin]">
        {groups.map((group) => (
          <div key={group.key} className="flex flex-col gap-0.5">
            <span className="px-3 pb-1 pt-2.5 text-[11px] tracking-[.5px] text-muted">{t(`nav.groups.${group.key}`)}</span>
            {group.areas.map((area) => {
              const current = area === active;
              const AreaIcon = AREA_ICONS[area.key];
              return (
                <Link
                  key={area.key} href={area.href} onClick={onNavigate} aria-current={current ? "page" : undefined}
                  className={`flex min-h-11 items-center gap-2.5 rounded-control px-3 text-sm ${current ? "bg-orange font-bold text-on-orange" : "hover:bg-footer-line hover:text-on-carbon"}`}
                >
                  <AreaIcon size={18} />
                  {t(`nav.areas.${area.key}`)}
                </Link>
              );
            })}
          </div>
        ))}
      </div>

      <div className="shrink-0 border-t border-footer-line px-2 pt-3.5">
        <AdminUserMenu user={user} onSignOut={onSignOut} onNavigate={onNavigate} />
      </div>
    </nav>
  );
}
