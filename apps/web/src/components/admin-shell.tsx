"use client";

import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { type ReactNode, useEffect, useMemo, useRef, useState } from "react";
import { logoutCurrent } from "@/lib/api-client";
import { useLocale } from "@/lib/i18n";
import { useSession } from "@/components/session-provider";
import { useTheme } from "@/components/theme-provider";
import { Icon, type IconName } from "@/components/icons";
import { LoadingCard, Message, SecondaryButton } from "@/components/ui";

type NavLink = {
  href: string;
  label: string;
  icon: IconName;
};

type FutureModule = {
  label: string;
  icon: IconName;
  dependency: string;
};

export function AdminShell({ children }: { children: ReactNode }) {
  const { messages, toggleLocale } = useLocale();
  const { theme, toggleTheme } = useTheme();
  const { status, user, markSignedOut, refresh } = useSession();
  const pathname = usePathname();
  const router = useRouter();
  const [drawerOpen, setDrawerOpen] = useState(false);
  const [signingOut, setSigningOut] = useState(false);
  const [actionError, setActionError] = useState<string | null>(null);
  const drawerRef = useRef<HTMLDivElement>(null);
  const closeButtonRef = useRef<HTMLButtonElement>(null);

  const mayViewCatalog = user?.permissions.includes("Catalog.View") ?? false;
  const mayManageProducts = user?.permissions.includes("Catalog.ManageProducts") ?? false;
  const mayManageTaxonomy = user?.permissions.includes("Catalog.ManageTaxonomy") ?? false;
  const mayManageMedia = user?.permissions.includes("Catalog.ManageMedia") ?? false;
  const mayModerateReviews = mayManageProducts;
  const mayManageUsers = user?.permissions.includes("System.ManageUsers") ?? false;
  const mayViewPurchasing = user?.permissions.includes("Purchasing.View") ?? false;
  const mayViewInventory = user?.permissions.includes("Inventory.View") ?? false;
  const mayViewSales = user?.permissions.some((permission) => permission.startsWith("Sales.") || permission.startsWith("Finance.")) ?? false;
  const mayManageStoreSettings = user?.permissions.includes("System.StoreSettings") ?? false;
  const mayViewAuditLogs = user?.permissions.includes("System.ViewAuditLogs") ?? false;
  const mayAccessAdmin = user?.permissions.some((permission) =>
    ["Catalog.", "Purchasing.", "Inventory.", "Sales.", "Invoices.", "Finance.", "System."]
      .some((prefix) => permission.startsWith(prefix))) ?? false;
  const pageTitle = getPageTitle(pathname ?? "/admin", messages);

  useEffect(() => {
    if (status === "anonymous" && !signingOut) {
      router.replace(`/login?returnTo=${encodeURIComponent(pathname || "/admin")}`);
    }
  }, [pathname, router, signingOut, status]);

  useEffect(() => {
    setDrawerOpen(false);
  }, [pathname]);

  useEffect(() => {
    if (!drawerOpen) return;
    const previousOverflow = document.body.style.overflow;
    const previousFocus = document.activeElement as HTMLElement | null;
    document.body.style.overflow = "hidden";
    closeButtonRef.current?.focus();
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        setDrawerOpen(false);
        return;
      }
      if (event.key !== "Tab" || !drawerRef.current) return;
      const focusable = Array.from(drawerRef.current.querySelectorAll<HTMLElement>(
        'a[href], button:not([disabled]), [tabindex]:not([tabindex="-1"])'));
      if (focusable.length === 0) return;
      const first = focusable[0];
      const last = focusable[focusable.length - 1];
      if (event.shiftKey && document.activeElement === first) {
        event.preventDefault();
        last.focus();
      } else if (!event.shiftKey && document.activeElement === last) {
        event.preventDefault();
        first.focus();
      }
    };
    window.addEventListener("keydown", onKeyDown);
    return () => {
      document.body.style.overflow = previousOverflow;
      window.removeEventListener("keydown", onKeyDown);
      previousFocus?.focus();
    };
  }, [drawerOpen]);

  const catalogLinks = useMemo<NavLink[]>(() => [
    ...(mayViewCatalog ? [{ href: "/admin/catalog", label: messages.adminCatalogWorkspace, icon: "box" as const }] : []),
    ...(mayManageProducts ? [{ href: "/admin/catalog/products/new", label: messages.adminNewProduct, icon: "box" as const }] : []),
    ...(mayManageTaxonomy ? [{ href: "/admin/catalog/categories", label: messages.adminCategoriesNav, icon: "tags" as const }] : []),
    ...(mayManageTaxonomy ? [{ href: "/admin/catalog/brands", label: messages.adminBrandsNav, icon: "tags" as const }] : []),
    ...(mayManageMedia ? [{ href: "/admin/catalog#media", label: messages.adminMediaNav, icon: "image" as const }] : []),
    ...(mayManageMedia ? [{ href: "/admin/media", label: messages.adminMediaLibraryNav, icon: "image" as const }] : []),
    ...(mayManageMedia ? [{ href: "/admin/media/videos", label: messages.adminVideosTitle, icon: "image" as const }] : []),
  ], [mayManageMedia, mayManageProducts, mayManageTaxonomy, mayViewCatalog, messages]);

  const storefrontLinks = useMemo<NavLink[]>(() => [
    ...(mayViewCatalog ? [{ href: "/admin/home-content", label: messages.adminHomeContentNav, icon: "store" as const }] : []),
    ...(mayManageStoreSettings ? [{ href: "/admin/content/articles", label: messages.adminArticlesNav, icon: "receipt" as const }] : []),
    ...(mayManageStoreSettings ? [{ href: "/admin/content/faqs", label: messages.adminFaqsNav, icon: "users" as const }] : []),
    ...(mayManageStoreSettings ? [{ href: "/admin/content/policies", label: messages.adminPoliciesNav, icon: "receipt" as const }] : []),
    ...(mayManageStoreSettings ? [{ href: "/admin/content/offers", label: messages.navOffers, icon: "tags" as const }] : []),
    ...(mayManageProducts ? [{ href: "/admin/pc-builder", label: messages.adminPcBuilderTitle, icon: "cpu" as const }] : []),
    ...(mayManageStoreSettings ? [{ href: "/admin/storefront/settings", label: messages.adminStoreSettingsNav, icon: "settings" as const }] : []),
  ], [mayManageStoreSettings, mayViewCatalog, messages]);

  const identityLinks = useMemo<NavLink[]>(() => [
    ...(mayManageUsers
      ? [
          { href: "/admin/identity/users", label: messages.adminUsersNav, icon: "users" as const },
          { href: "/admin/identity/roles", label: messages.adminRolesNav, icon: "users" as const },
        ]
      : []),
    ...(mayModerateReviews
      ? [{ href: "/admin/reviews", label: messages.adminReviewsTitle, icon: "check" as const }]
      : []),
    ...(mayViewAuditLogs
      ? [{ href: "/admin/audit", label: messages.adminAuditLogsNav, icon: "receipt" as const }]
      : []),
  ], [mayManageUsers, mayModerateReviews, mayViewAuditLogs, messages]);

  const purchasingLinks = useMemo<NavLink[]>(() => mayViewPurchasing
    ? [
        { href: "/admin/purchasing/suppliers", label: messages.adminSuppliersNav, icon: "users" },
        { href: "/admin/purchasing/drafts", label: messages.draftsTitle, icon: "receipt" },
      ]
    : [], [mayViewPurchasing, messages]);

  const inventoryLinks = useMemo<NavLink[]>(() => mayViewInventory
    ? [
        { href: "/admin/inventory/warehouses", label: messages.adminWarehousesNav, icon: "warehouse" },
        { href: "/admin/inventory/stock", label: messages.retailStockTitle, icon: "box" },
      ]
    : [], [mayViewInventory, messages]);

  const salesLinks = useMemo<NavLink[]>(() => mayViewSales
    ? [
        { href: "/admin/orders", label: messages.retailOrdersTitle, icon: "receipt" },
        { href: "/admin/reports", label: messages.financeTitle, icon: "dashboard" },
        { href: "/admin/contact", label: messages.adminContactTitle, icon: "mail" },
        { href: "/admin/partners", label: messages.retailPartnerTitle, icon: "users" },
      ]
    : [], [mayViewSales, messages]);

  const futureModules = useMemo<FutureModule[]>(() => [], []);

  async function signOut() {
    setSigningOut(true);
    setActionError(null);
    try {
      await logoutCurrent();
      markSignedOut();
      router.replace("/login?notice=signed-out");
    } catch {
      setActionError(messages.serviceUnavailable);
      setSigningOut(false);
    }
  }

  if (status === "loading" || signingOut) {
    return <AdminCenteredState><LoadingCard /></AdminCenteredState>;
  }

  if (status === "unavailable") {
    return (
      <AdminCenteredState>
        <Message tone="error">{messages.adminSessionUnavailable}</Message>
        <SecondaryButton type="button" onClick={() => void refresh()} className="mt-4">
          {messages.adminSessionRetry}
        </SecondaryButton>
      </AdminCenteredState>
    );
  }

  if (status === "anonymous") {
    return <AdminCenteredState><Message tone="info">{messages.adminSessionChecking}</Message></AdminCenteredState>;
  }

  if (!mayAccessAdmin) {
    return (
      <AdminCenteredState>
        <Message tone="error">{messages.adminAccessDenied}</Message>
        <Link href="/" className="mt-4 inline-flex font-bold text-[var(--ds-accent)]">{messages.adminBackToStore}</Link>
      </AdminCenteredState>
    );
  }

  const sidebar = (
    <AdminSidebar
      pathname={pathname ?? "/admin"}
      messages={messages}
      catalogLinks={catalogLinks}
      storefrontLinks={storefrontLinks}
      identityLinks={identityLinks}
      purchasingLinks={purchasingLinks}
      inventoryLinks={inventoryLinks}
      salesLinks={salesLinks}
      futureModules={futureModules}
    />
  );

  return (
    <div className="rigx-admin-shell min-h-screen text-[var(--ds-text)]">
      <div className="lg:grid lg:min-h-screen lg:grid-cols-[17rem_minmax(0,1fr)]">
        <aside className="rigx-admin-sidebar admin-sidebar sticky top-0 hidden h-dvh overflow-y-auto overscroll-contain text-white lg:block">
          {sidebar}
        </aside>

        <div className="min-w-0">
          <header className="rigx-admin-header sticky top-0 z-30 border-b">
            <div className="flex min-h-[4.75rem] items-center gap-3 px-4 py-3 sm:px-6">
              <button
                type="button"
                onClick={() => setDrawerOpen(true)}
                aria-label={messages.adminOpenMenu}
                aria-expanded={drawerOpen}
                className="grid min-h-11 min-w-11 place-items-center rounded-xl border border-[var(--ds-border)] text-[var(--ds-text)] lg:hidden"
              >
                <Icon name="menu" />
              </button>
              <div className="min-w-0 flex-1">
                <nav className="flex items-center gap-2 text-xs text-[var(--ds-text-muted)]" aria-label={messages.adminWorkspace}>
                  <Link href="/admin" className="hover:text-[var(--ds-accent)]">{messages.adminBreadcrumbRoot}</Link>
                  {pathname !== "/admin" ? <><Icon name="chevron" className="h-3.5 w-3.5" /><span className="truncate">{pageTitle}</span></> : null}
                </nav>
                <p className="mt-1 truncate text-lg font-black text-[var(--ds-text)] sm:text-xl">{pageTitle}</p>
              </div>
              <div className="flex items-center gap-1">
                <button
                  type="button"
                  onClick={toggleLocale}
                  className="min-h-11 rounded-xl px-3 text-sm font-bold text-[var(--ds-text-muted)] transition hover:bg-[var(--ds-surface-muted)] hover:text-[var(--ds-text)]"
                  dir="ltr"
                >
                  {messages.languageButton}
                </button>
                <button
                  type="button"
                  onClick={toggleTheme}
                  aria-label={theme === "dark" ? messages.switchToLight : messages.switchToDark}
                  title={theme === "dark" ? messages.switchToLight : messages.switchToDark}
                  className="grid min-h-11 min-w-11 place-items-center rounded-xl border border-[var(--ds-border)] text-[var(--ds-text)] transition hover:bg-[var(--ds-surface-muted)]"
                >
                  <Icon name={theme === "dark" ? "sun" : "moon"} />
                </button>
                <details className="relative">
                  <summary className="flex min-h-11 cursor-pointer list-none items-center gap-2 rounded-xl border border-[var(--ds-border)] px-3 text-sm font-bold text-[var(--ds-text)] marker:hidden transition hover:bg-[var(--ds-surface-muted)]">
                    <span className="grid h-7 w-7 place-items-center rounded-full bg-[var(--ds-accent-strong)] text-xs text-[var(--ds-on-accent)]">{user?.fullName?.slice(0, 1)}</span>
                    <span className="hidden max-w-32 truncate sm:inline">{user?.fullName}</span>
                  </summary>
                  <div className="absolute end-0 top-[3.25rem] z-50 grid min-w-60 gap-1 rounded-2xl border border-[var(--ds-border)] bg-[var(--ds-surface)] p-2 shadow-xl">
                    <div className="border-b border-[var(--ds-border)] px-3 py-2">
                      <p className="truncate font-bold text-[var(--ds-text)]">{user?.fullName}</p>
                      <p className="truncate text-xs text-[var(--ds-text-muted)]" dir="ltr">{user?.email}</p>
                    </div>
                    <Link href="/" className="flex items-center gap-2 rounded-lg px-3 py-2 text-sm font-semibold text-[var(--ds-text)] transition hover:bg-[var(--ds-surface-muted)]">
                      <Icon name="store" className="h-4 w-4" /> {messages.adminBackToStore}
                    </Link>
                    <Link href="/account/security" className="rounded-lg px-3 py-2 text-sm font-semibold text-[var(--ds-text)] transition hover:bg-[var(--ds-surface-muted)]">
                      {messages.navAccount}
                    </Link>
                    <button type="button" onClick={() => void signOut()} className="flex items-center gap-2 rounded-lg px-3 py-2 text-start text-sm font-semibold text-[var(--ds-danger)] transition hover:bg-[var(--ds-surface-muted)]">
                      <Icon name="logout" className="h-4 w-4" /> {messages.adminSignOut}
                    </button>
                  </div>
                </details>
              </div>
            </div>
            {actionError ? <div className="px-4 pb-3 sm:px-6"><Message tone="error">{actionError}</Message></div> : null}
          </header>

          <div id="main-content" className="admin-content min-w-0">{children}</div>
        </div>
      </div>

      {drawerOpen ? (
        <div className="fixed inset-0 z-50 lg:hidden" role="dialog" aria-modal="true" aria-label={messages.adminOpenMenu}>
          <button type="button" aria-label={messages.adminCloseMenu} onClick={() => setDrawerOpen(false)} className="absolute inset-0 bg-slate-950/60 backdrop-blur-sm" />
          <div ref={drawerRef} className="rigx-admin-sidebar admin-sidebar absolute inset-y-0 start-0 w-[min(20rem,88vw)] overflow-y-auto text-white shadow-2xl">
            <button
              ref={closeButtonRef}
              type="button"
              onClick={() => setDrawerOpen(false)}
              aria-label={messages.adminCloseMenu}
              className="absolute end-3 top-3 grid min-h-10 min-w-10 place-items-center rounded-lg text-slate-300 hover:bg-white/10 hover:text-white"
            >
              <Icon name="close" />
            </button>
            {sidebar}
          </div>
        </div>
      ) : null}
    </div>
  );
}

function AdminSidebar({
  pathname,
  messages,
  catalogLinks,
  storefrontLinks,
  identityLinks,
  purchasingLinks,
  inventoryLinks,
  salesLinks,
  futureModules,
}: {
  pathname: string;
  messages: ReturnType<typeof useLocale>["messages"];
  catalogLinks: NavLink[];
  storefrontLinks: NavLink[];
  identityLinks: NavLink[];
  purchasingLinks: NavLink[];
  inventoryLinks: NavLink[];
  salesLinks: NavLink[];
  futureModules: FutureModule[];
}) {
  return (
    <div className="admin-sidebar-inner">
      <Link href="/admin" className="admin-brand">
        <span className="brand-mark">RIG<span>X</span></span>
        <span>
          
          <small>{messages.adminWorkspace}</small>
        </span>
      </Link>

      <nav className="admin-nav-stack" aria-label={messages.adminWorkspace}>
        <NavGroup title={messages.adminGroupOverview}>
          <NavItem href="/admin" pathname={pathname} icon="dashboard" label={messages.adminDashboardNav} />
        </NavGroup>
        {catalogLinks.length > 0 ? (
          <NavGroup title={messages.adminGroupCatalog}>
            {catalogLinks.map((item) => <NavItem key={`${item.href}-${item.label}`} href={item.href} pathname={pathname} icon={item.icon} label={item.label} />)}
          </NavGroup>
        ) : null}
        {storefrontLinks.length > 0 ? (
          <NavGroup title={messages.adminGroupStorefront}>
            {storefrontLinks.map((item) => <NavItem key={`${item.href}-${item.label}`} href={item.href} pathname={pathname} icon={item.icon} label={item.label} />)}
          </NavGroup>
        ) : null}
        {identityLinks.length > 0 ? (
          <NavGroup title={messages.adminGroupIdentity}>
            {identityLinks.map((item) => <NavItem key={`${item.href}-${item.label}`} href={item.href} pathname={pathname} icon={item.icon} label={item.label} />)}
          </NavGroup>
        ) : null}
        {purchasingLinks.length > 0 ? (
          <NavGroup title={messages.adminGroupPurchasing}>
            {purchasingLinks.map((item) => <NavItem key={`${item.href}-${item.label}`} href={item.href} pathname={pathname} icon={item.icon} label={item.label} />)}
          </NavGroup>
        ) : null}
        {inventoryLinks.length > 0 ? (
          <NavGroup title={messages.adminGroupInventory}>
            {inventoryLinks.map((item) => <NavItem key={`${item.href}-${item.label}`} href={item.href} pathname={pathname} icon={item.icon} label={item.label} />)}
          </NavGroup>
        ) : null}
        {salesLinks.length > 0 ? (
          <NavGroup title={messages.adminGroupSales}>
            {salesLinks.map((item) => <NavItem key={`${item.href}-${item.label}`} href={item.href} pathname={pathname} icon={item.icon} label={item.label} />)}
          </NavGroup>
        ) : null}
        {futureModules.length > 0 ? (
          <NavGroup title={messages.adminFutureModules}>
            {futureModules.map((item) => (
              <div key={`${item.label}-${item.dependency}`} aria-disabled="true" className="flex items-center gap-3 rounded-lg px-3 py-2 text-sm text-slate-500">
                <Icon name={item.icon} className="h-4 w-4 shrink-0" />
                <span className="min-w-0 flex-1 truncate">{item.label}</span>
                <span className="rounded border border-white/10 px-1.5 py-0.5 text-[10px] text-slate-500">{item.dependency}</span>
              </div>
            ))}
          </NavGroup>
        ) : null}
      </nav>

      <div className="admin-side-footer">
        <span className="admin-dot" /> {messages.adminAvailableNow}: {messages.adminAvailableModules}
      </div>
    </div>
  );
}

function NavGroup({ title, children }: { title: string; children: ReactNode }) {
  return (
    <div className="admin-nav-group">
      <small>{title}</small>
      <div className="admin-nav-links">{children}</div>
    </div>
  );
}

function NavItem({ href, pathname, icon, label }: { href: string; pathname: string; icon: IconName; label: string }) {
  const hrefPath = href.split("#")[0];
  const active = !href.includes("#") && pathname === hrefPath;
  return (
    <Link
      href={href}
      aria-current={active ? "page" : undefined}
      className={`admin-nav${active ? " active" : ""}`}
    >
      <Icon name={icon} className="h-4 w-4 shrink-0" />
      <span className="truncate">{label}</span>
    </Link>
  );
}

function AdminCenteredState({ children }: { children: ReactNode }) {
  return <div className="grid min-h-screen place-items-center bg-[var(--ds-background)] px-5"><div className="w-full max-w-xl">{children}</div></div>;
}

function getPageTitle(pathname: string, messages: ReturnType<typeof useLocale>["messages"]): string {
  if (pathname === "/admin") return messages.adminDashboardNav;
  if (pathname === "/admin/catalog") return messages.adminCatalogWorkspace;
  if (pathname === "/admin/catalog/categories") return messages.adminCategoriesNav;
  if (pathname === "/admin/catalog/brands") return messages.adminBrandsNav;
  if (pathname === "/admin/home-content") return messages.adminHomeContentNav;
  if (pathname === "/admin/identity/users") return messages.adminIdentityUsersTitle;
  if (pathname === "/admin/identity/roles") return messages.adminIdentityRolesTitle;
  if (pathname === "/admin/purchasing/suppliers") return messages.supplierTitle;
  if (pathname === "/admin/inventory/warehouses") return messages.warehouseTitle;
  if (pathname === "/admin/storefront/settings") return messages.storeSettingsTitle;
  if (pathname === "/admin/content/articles") return messages.adminArticlesNav;
  if (pathname === "/admin/content/faqs") return messages.adminFaqsNav;
  if (pathname === "/admin/content/policies") return messages.adminPoliciesNav;
  if (pathname === "/admin/content/offers") return messages.navOffers;
  if (pathname === "/admin/reviews") return messages.adminReviewsTitle;
  if (pathname === "/admin/purchasing/drafts") return messages.draftsTitle;
  if (pathname === "/admin/pc-builder") return messages.adminPcBuilderTitle;
  if (pathname === "/admin/inventory/stock") return messages.retailStockTitle;
  if (pathname === "/admin/orders") return messages.retailOrdersTitle;
  if (pathname === "/admin/reports") return messages.financeTitle;
  if (pathname === "/admin/contact") return messages.adminContactTitle;
  if (pathname === "/admin/partners") return messages.retailPartnerTitle;
  if (pathname === "/admin/media") return messages.adminMediaLibraryNav;
  if (pathname === "/admin/media/videos") return messages.adminVideosTitle;
  if (pathname === "/admin/audit") return messages.adminAuditLogsNav;
  if (pathname === "/admin/catalog/products/new") return messages.adminNewProduct;
  if (/^\/admin\/catalog\/products\/[^/]+\/preview$/.test(pathname)) return messages.adminDraftPreview;
  if (/^\/admin\/catalog\/products\/[^/]+$/.test(pathname)) return messages.adminEdit;
  return messages.adminWorkspace;
}

