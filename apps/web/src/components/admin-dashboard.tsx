"use client";

import Link from "next/link";
import { useEffect, useMemo, useState } from "react";
import {
  getAdminBrands,
  getAdminCategories,
  getAdminProducts,
  type CatalogBrand,
  type CatalogCategory,
  type CatalogProductSummary,
} from "@/lib/api-client";
import { apiErrorMessage } from "@/lib/form-errors";
import { useLocale } from "@/lib/i18n";
import { useSession } from "@/components/session-provider";
import { Icon, type IconName } from "@/components/icons";
import { LoadingCard, Message, SecondaryButton } from "@/components/ui";
import { AdminAnalytics } from "@/components/admin-analytics";

type DashboardData = {
  categories: CatalogCategory[];
  brands: CatalogBrand[];
  products: CatalogProductSummary[];
};

export function AdminDashboard() {
  const { messages } = useLocale();
  const { user } = useSession();
  const mayViewCatalog = user?.permissions.includes("Catalog.View") ?? false;
  const mayViewPurchasing = user?.permissions.includes("Purchasing.View") ?? false;
  const mayManageUsers = user?.permissions.includes("System.ManageUsers") ?? false;
  const [data, setData] = useState<DashboardData | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [attempt, setAttempt] = useState(0);

  useEffect(() => {
    if (!mayViewCatalog) return;
    let active = true;
    setError(null);
    void Promise.all([getAdminCategories(), getAdminBrands(), getAdminProducts()])
      .then(([categories, brands, products]) => {
        if (active) setData({ categories, brands, products });
      })
      .catch((requestError) => {
        if (active) setError(apiErrorMessage(requestError, messages));
      });
    return () => {
      active = false;
    };
  }, [attempt, mayViewCatalog, messages]);

  const metrics = useMemo(() => {
    if (!data) return null;
    const categories = flattenCategories(data.categories);
    const published = data.products.filter((product) => product.status === "Published");
    const drafts = data.products.filter((product) => product.status === "Draft");
    return {
      published,
      drafts,
      activeCategories: categories.filter((category) => category.isActive).length,
      activeBrands: data.brands.filter((brand) => brand.isActive).length,
      recentProducts: published.slice(0, 5),
    };
  }, [data]);

  if (!mayViewCatalog) {
    return (
      <main className="p-5 sm:p-7">
        <Message tone="info">{messages.adminDashboardLimited}</Message>
        <div className="mt-4 flex flex-wrap gap-2">
          {mayViewPurchasing ? (
            <Link href="/admin/purchasing/suppliers" className="rigx-button rigx-button-secondary">
              {messages.adminSuppliersNav}
            </Link>
          ) : null}
          {mayManageUsers ? (
            <Link href="/admin/identity/users" className="rigx-button rigx-button-secondary">
              {messages.adminUsersNav}
            </Link>
          ) : null}
        </div>
      </main>
    );
  }

  if (!data && !error) return <LoadingCard />;
  if (error) {
    return (
      <main className="p-5 sm:p-7">
        <Message tone="error">{error}</Message>
        <SecondaryButton type="button" onClick={() => setAttempt((value) => value + 1)} className="mt-4">
          {messages.retry}
        </SecondaryButton>
      </main>
    );
  }
  if (!data || !metrics) return null;

  const mayManageProducts = user?.permissions.includes("Catalog.ManageProducts") ?? false;
  const mayManageTaxonomy = user?.permissions.includes("Catalog.ManageTaxonomy") ?? false;
  const mayManageMedia = user?.permissions.includes("Catalog.ManageMedia") ?? false;
  const totalProducts = metrics.published.length + metrics.drafts.length;
  const publishedPercent = totalProducts === 0 ? 0 : Math.round(metrics.published.length / totalProducts * 100);
  const draftPercent = totalProducts === 0 ? 0 : 100 - publishedPercent;

  return (
    <main className="p-4 sm:p-6 lg:p-8">
      <div className="mx-auto max-w-[80rem]">
        <header className="mb-7 flex flex-wrap items-end justify-between gap-4">
          <div>
            <p className="text-sm font-bold text-[var(--ds-accent)]">{messages.adminGroupOverview}</p>
            <h1 className="mt-2 text-3xl font-black tracking-tight text-[var(--ds-text)] sm:text-4xl">{messages.adminDashboardTitle}</h1>
            <p className="mt-2 max-w-3xl text-sm leading-7 text-[var(--ds-text-muted)] sm:text-base">{messages.adminDashboardLead}</p>
          </div>
          <p className="rounded-full border border-emerald-200 bg-emerald-50 px-3 py-1.5 text-xs font-bold text-emerald-800">
            {messages.adminDataFromCatalog}
          </p>
        </header>

        <section className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4" aria-label={messages.adminDashboardTitle}>
          <MetricCard icon="box" label={messages.adminMetricPublishedProducts} value={metrics.published.length} accent="accent" />
          <MetricCard icon="receipt" label={messages.adminMetricDraftProducts} value={metrics.drafts.length} accent="neutral" />
          <MetricCard icon="tags" label={messages.adminMetricActiveCategories} value={metrics.activeCategories} accent="accent" />
          <MetricCard icon="store" label={messages.adminMetricActiveBrands} value={metrics.activeBrands} accent="success" />
        </section>

        <AdminAnalytics />

        <div className="mt-6 grid gap-6 xl:grid-cols-[minmax(0,1.2fr)_minmax(20rem,.8fr)]">
          <section className="ds-surface overflow-hidden rounded-2xl" aria-labelledby="catalog-composition-heading">
            <div className="border-b border-[var(--ds-border)] px-5 py-4">
              <h2 id="catalog-composition-heading" className="text-lg font-black text-[var(--ds-text)]">{messages.adminCatalogComposition}</h2>
              <p className="mt-1 text-sm text-[var(--ds-text-muted)]">{messages.adminCatalogCompositionLead}</p>
            </div>
            <div className="p-5">
              <div className="flex h-4 overflow-hidden rounded-full bg-[var(--ds-surface-muted)]" role="img" aria-label={`${messages.adminMetricPublishedProducts}: ${publishedPercent}%`}>
                <span className="bg-[var(--ds-accent)]" style={{ width: `${publishedPercent}%` }} />
                <span className="bg-[var(--ds-text-muted)]" style={{ width: `${draftPercent}%` }} />
              </div>
              <div className="mt-5 grid gap-3 sm:grid-cols-2">
                <Legend label={messages.adminMetricPublishedProducts} value={metrics.published.length} tone="bg-[var(--ds-accent)]" />
                <Legend label={messages.adminMetricDraftProducts} value={metrics.drafts.length} tone="bg-[var(--ds-text-muted)]" />
              </div>
            </div>
          </section>

          <section className="ds-surface rounded-2xl p-5">
            <h2 className="text-lg font-black text-[var(--ds-text)]">{messages.adminQuickActions}</h2>
            <p className="mt-1 text-sm leading-6 text-[var(--ds-text-muted)]">{messages.adminQuickActionsLead}</p>
            <p className="mt-3 rounded-xl border border-[var(--ds-border)] bg-[var(--ds-accent-soft)] px-3 py-2 text-xs leading-5 text-[var(--ds-accent-strong)]">
              {messages.adminStorefrontRule}
            </p>
            <div className="mt-5 grid gap-2">
              {mayManageProducts ? <QuickAction href="/admin/catalog/products/new" icon="box" label={messages.adminNewProduct} /> : null}
              <QuickAction href="/admin/catalog" icon="dashboard" label={messages.adminManageCatalog} />
              {mayManageTaxonomy ? <QuickAction href="/admin/catalog#categories" icon="tags" label={messages.adminCategoriesNav} /> : null}
              {mayManageMedia ? <QuickAction href="/admin/catalog#media" icon="image" label={messages.adminMediaMaintenance} /> : null}
            </div>
          </section>
        </div>

        <div className="mt-6 grid gap-6 xl:grid-cols-[minmax(0,1.1fr)_minmax(20rem,.9fr)]">
          <section className="ds-surface overflow-hidden rounded-2xl">
            <div className="border-b border-[var(--ds-border)] px-5 py-4">
              <h2 className="text-lg font-black text-[var(--ds-text)]">{messages.adminRecentProducts}</h2>
              <p className="mt-1 text-sm text-[var(--ds-text-muted)]">{messages.adminRecentProductsLead}</p>
            </div>
            {metrics.recentProducts.length === 0 ? (
              <p className="px-5 py-10 text-center text-sm text-[var(--ds-text-muted)]">{messages.adminNoPublishedProducts}</p>
            ) : (
              <div className="divide-y divide-[var(--ds-border)]">
                {metrics.recentProducts.map((product) => (
                  <Link key={product.id} href={`/admin/catalog/products/${product.id}`} className="flex flex-wrap items-center gap-3 px-5 py-3 transition hover:bg-[var(--ds-surface-muted)]">
                    <span className="grid h-9 w-9 place-items-center rounded-lg bg-[var(--ds-accent-soft)] text-[var(--ds-accent)]"><Icon name="box" className="h-4 w-4" /></span>
                    <span className="min-w-0 flex-1">
                      <span className="block truncate font-bold text-[var(--ds-text)]">{product.name}</span>
                      <span className="block text-xs text-[var(--ds-text-muted)]" dir="ltr">{product.sku}</span>
                    </span>
                    <span className="rounded-full border border-emerald-200 bg-emerald-50 px-2.5 py-1 text-xs font-bold text-emerald-800">{messages.catalogPublished}</span>
                    <Icon name="chevron" className="h-4 w-4 rtl:rotate-180" />
                  </Link>
                ))}
              </div>
            )}
          </section>
        </div>
      </div>
    </main>
  );
}

function flattenCategories(categories: CatalogCategory[]): CatalogCategory[] {
  return categories.flatMap((category) => [category, ...flattenCategories(category.children)]);
}

function MetricCard({ icon, label, value, accent }: { icon: IconName; label: string; value: number; accent: "accent" | "neutral" | "success" }) {
  const colors = {
    accent: "rigx-metric-accent",
    neutral: "rigx-metric-neutral",
    success: "rigx-metric-success",
  };
  return (
    <article aria-label={`${label}: ${value}`} className="ds-surface flex items-center gap-4 rounded-2xl p-5">
      <span className={`grid h-12 w-12 place-items-center rounded-xl border ${colors[accent]}`}>
        <Icon name={icon} />
      </span>
      <span>
        <span className="block text-2xl font-black text-[var(--ds-text)]">{value.toLocaleString()}</span>
        <span className="mt-1 block text-sm font-semibold text-[var(--ds-text-muted)]">{label}</span>
      </span>
    </article>
  );
}

function Legend({ label, value, tone }: { label: string; value: number; tone: string }) {
  return (
    <div className="flex items-center gap-2 rounded-xl border border-[var(--ds-border)] bg-[var(--ds-surface-muted)] px-3 py-2">
      <span className={`h-2.5 w-2.5 rounded-full ${tone}`} />
      <span className="flex-1 text-sm text-[var(--ds-text-muted)]">{label}</span>
      <span className="font-black text-[var(--ds-text)]">{value}</span>
    </div>
  );
}

function QuickAction({ href, icon, label }: { href: string; icon: IconName; label: string }) {
  return (
    <Link href={href} className="flex min-h-11 items-center gap-3 rounded-xl border border-[var(--ds-border)] px-3 text-sm font-bold text-[var(--ds-text)] transition hover:border-[var(--ds-accent)] hover:bg-[var(--ds-accent-soft)] hover:text-[var(--ds-accent-strong)]">
      <Icon name={icon} className="h-4 w-4" />
      <span className="flex-1">{label}</span>
      <Icon name="chevron" className="h-4 w-4 rtl:rotate-180" />
    </Link>
  );
}
