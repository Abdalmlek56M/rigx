"use client";

import { useTranslations } from "next-intl";
import { LocaleSwitch } from "@/components/layout/LocaleSwitch";
import { ThemeToggle } from "@/components/layout/ThemeToggle";
import {
  BoxIcon, CaseIcon, ClockIcon, DatabaseIcon, DocumentIcon, FolderIcon, GearIcon, GridIcon, HandshakeIcon, HelpIcon, InboxIcon, KeyIcon,
  MegaphoneIcon, RegisterIcon, ShieldIcon, StarIcon, StorageIcon, TagIcon, TruckIcon, UsersIcon, WalletIcon
} from "@/components/ui/icons";
import { Logo } from "@/components/ui/Logo";
import type { SessionUser } from "@/features/auth/session-store";
import { Link } from "@/i18n/navigation";
import type { AdminArea, AdminAreaKey, AdminGroup } from "./admin-nav";

const AREA_ICONS: Record<AdminAreaKey, typeof GridIcon> = {
  dashboard: GridIcon, pos: RegisterIcon, orders: BoxIcon, customers: UsersIcon, offers: TagIcon,
  inventory: StorageIcon, builds: CaseIcon, purchases: TruckIcon, stockImport: DocumentIcon, categories: FolderIcon, brands: ShieldIcon,
  partners: HandshakeIcon, expenses: WalletIcon,
  requests: InboxIcon, campaigns: MegaphoneIcon, tiers: StarIcon,
  articles: DocumentIcon, faq: HelpIcon, policies: DocumentIcon,
  users: KeyIcon, audit: ClockIcon, system: DatabaseIcon, settings: GearIcon
};

interface AdminSidebarProps {
  user: SessionUser;
  groups: readonly AdminGroup[];
  active: AdminArea | null;
  onNavigate?: () => void;
  onSignOut: () => void;
}

const KNOWN_ROLES = ["Owner"] as const;
type KnownRole = (typeof KNOWN_ROLES)[number];

const isKnownRole = (code: string): code is KnownRole => KNOWN_ROLES.some((known) => known === code);

/** The dark admin sidebar: logo, one link per area the user may open (grouped), and the user with theme, language and sign-out. */
export function AdminSidebar({ user, groups, active, onNavigate, onSignOut }: AdminSidebarProps) {
  const t = useTranslations("admin");
  const role = user.roles.find(isKnownRole);
  const name = user.fullName || user.email;

  return (
    <nav aria-label={t("shell.navLabel")} className="flex h-full flex-col gap-1 overflow-y-auto bg-chrome px-3 py-[18px] text-footer-text">
      <div className="flex items-center justify-between px-2 pb-3">
        <Link href="/admin" onClick={onNavigate} aria-label={t("shell.home")} className="text-on-carbon"><Logo width={96} /></Link>
        <span className="font-mono text-[10px] tracking-[1px] text-muted">ADMIN</span>
      </div>

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

      <div className="mt-auto flex flex-col gap-3 border-t border-footer-line px-2 pt-3.5">
        <div className="flex items-center gap-2.5">
          <span aria-hidden="true" className="flex h-[34px] w-[34px] shrink-0 items-center justify-center rounded-full bg-footer-line font-bold text-on-carbon">
            {name.charAt(0).toUpperCase()}
          </span>
          <span className="flex min-w-0 flex-col text-[13px]">
            <b className="truncate text-on-carbon">{name}</b>
            <span className="text-xs">{role ? t(`shell.roles.${role}`) : t("shell.roles.staff")}</span>
          </span>
        </div>
        <div className="flex flex-wrap items-center gap-2">
          <ThemeToggle />
          <LocaleSwitch />
          <Link href="/" className="flex h-11 items-center rounded-control border border-header-line px-3 text-sm font-bold text-on-carbon">{t("shell.store")}</Link>
          <button type="button" onClick={onSignOut} className="flex h-11 items-center rounded-control px-3 text-sm font-bold text-on-carbon hover:underline">{t("shell.signOut")}</button>
        </div>
      </div>
    </nav>
  );
}
