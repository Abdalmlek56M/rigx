/** The admin areas, their sidebar groups and the permission each needs. Labels live in the `admin.nav` messages. Pure, so it is tested. */
export type AdminAreaKey =
  | "dashboard" | "pos" | "orders" | "customers" | "offers" | "coupons"
  | "inventory" | "builds" | "purchases" | "stockImport" | "categories" | "brands"
  | "partners" | "expenses"
  | "requests"
  | "articles" | "faq" | "policies"
  | "users" | "audit" | "system" | "settings";

export type AdminGroupKey = "home" | "sales" | "stock" | "money" | "customers" | "content" | "system";

export interface AdminArea {
  key: AdminAreaKey;
  href: string;
  /** Any one of these opens the area (and shows it in the sidebar). */
  anyOf: readonly string[];
}

export interface AdminGroup {
  key: AdminGroupKey;
  areas: readonly AdminArea[];
}

export const ADMIN_HOME = "/admin";

export const ADMIN_NAV: readonly AdminGroup[] = [
  { key: "home", areas: [{ key: "dashboard", href: ADMIN_HOME, anyOf: ["Finance.ViewReports"] }] },
  {
    key: "sales",
    areas: [
      { key: "pos", href: "/admin/pos", anyOf: ["Sales.CreatePOS"] },
      { key: "orders", href: "/admin/orders", anyOf: ["Sales.ViewOrders"] },
      { key: "customers", href: "/admin/customers", anyOf: ["Customers.View"] },
      { key: "offers", href: "/admin/offers", anyOf: ["System.StoreSettings"] },
      { key: "coupons", href: "/admin/coupons", anyOf: ["System.StoreSettings"] }
    ]
  },
  {
    key: "stock",
    areas: [
      { key: "inventory", href: "/admin/inventory", anyOf: ["Inventory.View"] },
      { key: "builds", href: "/admin/builds", anyOf: ["Catalog.ManageProducts"] },
      { key: "purchases", href: "/admin/purchases", anyOf: ["Purchasing.View"] },
      { key: "stockImport", href: "/admin/stock/import", anyOf: ["Purchasing.ReceiveGoods"] },
      { key: "categories", href: "/admin/categories", anyOf: ["Catalog.View"] },
      { key: "brands", href: "/admin/brands", anyOf: ["Catalog.View"] }
    ]
  },
  {
    key: "money",
    areas: [
      { key: "partners", href: "/admin/partners", anyOf: ["Finance.ViewReports"] },
      { key: "expenses", href: "/admin/expenses", anyOf: ["Finance.ViewReports"] }
    ]
  },
  {
    key: "customers",
    areas: [
      // Campaigns and customer tiers come back here in Phase 5, when their pages exist (4.10b owner decision).
      { key: "requests", href: "/admin/requests", anyOf: ["Requests.View"] }
    ]
  },
  {
    key: "content",
    areas: [
      { key: "articles", href: "/admin/articles", anyOf: ["System.StoreSettings"] },
      { key: "faq", href: "/admin/faq", anyOf: ["System.StoreSettings"] },
      { key: "policies", href: "/admin/policies", anyOf: ["System.StoreSettings"] }
    ]
  },
  {
    key: "system",
    areas: [
      { key: "users", href: "/admin/users", anyOf: ["System.ManageUsers"] },
      { key: "audit", href: "/admin/audit", anyOf: ["System.ViewAuditLogs"] },
      { key: "system", href: "/admin/system", anyOf: ["System.Trash", "System.ManageBackups"] },
      { key: "settings", href: "/admin/settings", anyOf: ["System.StoreSettings"] }
    ]
  }
];

export function canOpen(area: AdminArea, permissions: readonly string[]): boolean {
  return area.anyOf.some((permission) => permissions.includes(permission));
}

/** The sidebar for these permissions: areas the user cannot open are hidden, and so are empty groups. */
export function visibleNav(permissions: readonly string[]): AdminGroup[] {
  return ADMIN_NAV
    .map((group) => ({ key: group.key, areas: group.areas.filter((area) => canOpen(area, permissions)) }))
    .filter((group) => group.areas.length > 0);
}

/** Staff = anyone who can open at least one admin area (a customer has no permissions). */
export function isStaff(permissions: readonly string[]): boolean {
  return visibleNav(permissions).length > 0;
}

/** The area `pathname` (without the locale) belongs to; detail pages belong to their list. `null` outside the known areas. */
export function areaFor(pathname: string): AdminArea | null {
  for (const group of ADMIN_NAV) {
    for (const area of group.areas) {
      if (area.href === ADMIN_HOME ? pathname === ADMIN_HOME : pathname === area.href || pathname.startsWith(`${area.href}/`)) return area;
    }
  }
  return null;
}

/** Where a staff member lands on `/admin` when they cannot see the dashboard: their first area. */
export function firstAreaHref(permissions: readonly string[]): string | null {
  return visibleNav(permissions)[0]?.areas[0]?.href ?? null;
}
