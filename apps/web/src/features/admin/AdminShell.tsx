"use client";

import { Suspense, useEffect, useState, type ReactNode } from "react";
import { useTranslations } from "next-intl";
import { setTheme, useTheme } from "@/components/layout/use-theme";
import { cardClass } from "@/components/ui/Card";
import { MenuIcon, SearchIcon } from "@/components/ui/icons";
import { Logo } from "@/components/ui/Logo";
import { ModalSheet } from "@/components/ui/ModalSheet";
import { loginHref } from "@/features/auth/auth-rules";
import { signOut } from "@/features/auth/session-store";
import { useSession } from "@/features/auth/use-session";
import { Link, usePathname, useRouter } from "@/i18n/navigation";
import { ADMIN_HOME, areaFor, canOpen, firstAreaHref, isStaff, visibleNav } from "./admin-nav";
import { AdminSidebar } from "./AdminSidebar";
import { useAdminBadges } from "./badges/use-admin-badges";
import { useCollapsedGroups } from "./use-collapsed-groups";
import { ConfirmDialog } from "./kit/ConfirmDialog";
import { nextDensity } from "./kit/density-rules";
import { ToastProvider } from "./kit/Toast";
import { useDensity } from "./kit/use-density";
import { CommandPalette } from "./palette/CommandPalette";
import { isPaletteShortcut, type PaletteCommand } from "./palette/palette-rules";

function Blank() {
  return <div aria-hidden="true" className="min-h-dvh bg-paper" />;
}

/** A full message in place of a page: no admin access at all, or not this area. */
function Forbidden({ withStoreLink }: { withStoreLink: boolean }) {
  const t = useTranslations("admin.shell.forbidden");
  return (
    <section role="alert" className={cardClass({ padding: "lg", className: "items-start gap-3" })}>
      <h1 className="m-0 text-2xl font-bold">{t("title")}</h1>
      <p className="m-0 text-ink-muted">{t("body")}</p>
      {withStoreLink ? <Link href="/" className="font-bold text-orange-text underline hover:text-orange-text-hover">{t("store")}</Link> : null}
    </section>
  );
}

/**
 * The frame of every admin page: signed-out visitors go to sign-in, customers get a 403, staff get the sidebar of the areas
 * their permissions open, and a page they cannot open shows a 403 in place. `/admin` sends staff without the dashboard to
 * their first area.
 */
export function AdminShell({ children }: { children: ReactNode }) {
  const t = useTranslations("admin.shell");
  const tPalette = useTranslations("admin.palette");
  const session = useSession();
  const router = useRouter();
  const pathname = usePathname();
  const [menuOpen, setMenuOpen] = useState(false);
  const [confirmingLeave, setConfirmingLeave] = useState(false);
  const [paletteOpen, setPaletteOpen] = useState(false);
  const theme = useTheme();

  const user = session.status === "signedIn" ? session.user : null;
  const permissions = user?.permissions ?? [];
  const area = areaFor(pathname);
  const allowed = area === null || canOpen(area, permissions);
  const sawBadges = permissions.includes("Sales.ViewOrders") || permissions.includes("Requests.View");
  const badges = useAdminBadges(user && sawBadges ? user.username : null, area?.key ?? null);
  const landing = user && pathname === ADMIN_HOME && !allowed ? firstAreaHref(permissions) : null;
  const [density, setDensity] = useDensity(user?.username ?? "");
  const [collapsed, toggleGroup] = useCollapsedGroups(user?.username ?? "");

  // Ctrl+K opens and closes the command palette from anywhere in the admin.
  useEffect(() => {
    const toggle = (event: KeyboardEvent) => {
      if (!isPaletteShortcut(event)) return;
      event.preventDefault();
      setPaletteOpen((open) => !open);
    };
    window.addEventListener("keydown", toggle);
    return () => window.removeEventListener("keydown", toggle);
  }, []);

  useEffect(() => {
    if (session.status === "signedOut") router.replace(loginHref(pathname));
  }, [session.status, router, pathname]);

  useEffect(() => {
    if (landing) router.replace(landing);
  }, [landing, router]);

  function runCommand(command: PaletteCommand) {
    if (command === "toggleTheme") setTheme(theme === "dark" ? "light" : "dark");
    else setDensity(nextDensity(density));
  }

  async function leave() {
    await signOut();
    router.replace("/");
  }

  if (!user || landing) return <Blank />;
  if (!isStaff(permissions)) {
    return <main className="mx-auto max-w-[720px] px-4 py-16"><Forbidden withStoreLink /></main>;
  }

  const groups = visibleNav(permissions);
  const closeMenu = () => setMenuOpen(false);
  const openPalette = () => {
    closeMenu();
    setPaletteOpen(true);
  };

  return (
    <ToastProvider>
      <ConfirmDialog
        open={confirmingLeave}
        title={t("signOutConfirm.title")}
        body={t("signOutConfirm.body")}
        confirmLabel={t("signOutConfirm.action")}
        onConfirm={leave}
        onClose={() => setConfirmingLeave(false)}
      />
      <CommandPalette open={paletteOpen} permissions={permissions} onClose={() => setPaletteOpen(false)} onCommand={runCommand} />
      <div data-density={density} className="flex min-h-dvh bg-paper [--row-py:0.625rem] data-[density=compact]:[--row-py:0.25rem]">
        <div className="sticky top-0 hidden h-dvh w-60 shrink-0 md:block">
          <AdminSidebar user={user} groups={groups} active={area} badges={badges} collapsed={collapsed} onToggleGroup={toggleGroup} onSignOut={() => setConfirmingLeave(true)} onOpenPalette={openPalette} />
        </div>

        <div className="flex min-w-0 flex-1 flex-col">
          <header className="sticky top-0 z-10 flex h-14 items-center justify-between bg-chrome px-4 md:hidden">
            <button type="button" onClick={() => setMenuOpen(true)} aria-label={t("openMenu")} className="flex h-11 w-11 items-center justify-center text-on-carbon">
              <MenuIcon />
            </button>
            <Link href="/admin" aria-label={t("home")} className="text-on-carbon"><Logo width={88} /></Link>
            <button type="button" onClick={openPalette} aria-label={tPalette("open")} aria-haspopup="dialog" className="flex h-11 w-11 items-center justify-center text-on-carbon">
              <SearchIcon />
            </button>
          </header>
          <ModalSheet open={menuOpen} onClose={closeMenu} label={t("navLabel")} edge="start" fullHeight>
            <AdminSidebar user={user} groups={groups} active={area} badges={badges} collapsed={collapsed} onToggleGroup={toggleGroup} onNavigate={closeMenu} onSignOut={() => setConfirmingLeave(true)} onOpenPalette={openPalette} />
          </ModalSheet>

          <main className="flex min-w-0 flex-1 flex-col gap-[18px] px-4 pb-10 pt-6 md:px-7">
            {allowed ? <Suspense fallback={null}>{children}</Suspense> : <Forbidden withStoreLink={false} />}
          </main>
        </div>
      </div>
    </ToastProvider>
  );
}
