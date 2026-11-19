"use client";

import { useCallback, useState } from "react";
import { useTranslations } from "next-intl";
import { useLoad } from "@/lib/hooks/use-load";
import { PageHeader } from "../kit/PageHeader";
import { useReveal } from "../kit/use-reveal";
import { usePermissions } from "../use-permissions";
import { BrandForm } from "./BrandForm";
import { filterBrands } from "./brand-rules";
import { fetchBrands, type Brand } from "./brands-api";

const button = "inline-flex h-11 items-center whitespace-nowrap rounded-control px-4 text-sm font-bold";

/** ADM-Brands: the brand list (with a search) on the start side and the edit / create panel on the end side. */
export function Brands() {
  const t = useTranslations("admin.brands");
  const { has } = usePermissions();
  const canManage = has("Catalog.ManageTaxonomy");
  const { state, reload } = useLoad(useCallback(() => fetchBrands(), []));
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [creating, setCreating] = useState(false);
  const [query, setQuery] = useState("");

  const brands = state.status === "ready" ? state.data : [];
  const shown = filterBrands(brands, query);
  const selected = creating ? null : brands.find((brand) => brand.id === selectedId) ?? null;
  const panelRef = useReveal<HTMLElement>(creating ? "new" : selectedId);

  const saved = (brand: Brand) => {
    setCreating(false);
    setSelectedId(brand.id);
    reload();
  };

  return (
    <>
      <PageHeader
        title={t("title")}
        actions={canManage ? (
          <button type="button" onClick={() => { setCreating(true); setSelectedId(null); }} className={`${button} bg-carbon text-on-carbon`}>
            {t("new")}
          </button>
        ) : null}
      />
      {state.status === "error" ? (
        <div role="alert" className="flex flex-col items-start gap-3 rounded-card border border-line bg-surface p-6">
          <p className="m-0 font-bold">{t("failed")}</p>
          <button type="button" onClick={reload} className={`${button} bg-carbon text-on-carbon`}>{t("retry")}</button>
        </div>
      ) : (
        <div className="grid gap-4 lg:grid-cols-[360px_minmax(0,1fr)] lg:items-start">
          <div className="flex flex-col gap-2 rounded-card border border-line bg-surface p-3">
            <label htmlFor="brand-search" className="sr-only">{t("search")}</label>
            <input
              id="brand-search" type="search" value={query} onChange={(event) => setQuery(event.target.value)} placeholder={t("search")}
              autoComplete="off" className="h-11 w-full rounded-control border border-input-line bg-surface px-3 text-sm"
            />
            {state.status === "loading" ? <div aria-hidden="true" className="h-64 animate-pulse rounded-control bg-paper" /> : shown.length === 0 ? (
              <p className="m-0 p-3 text-sm text-ink-muted">{brands.length === 0 ? t("emptyList") : t("noMatch")}</p>
            ) : (
              <ul aria-label={t("listLabel")} className="m-0 flex max-h-[60dvh] list-none flex-col overflow-y-auto overscroll-contain p-0">
                {shown.map((brand) => {
                  const active = brand.id === selected?.id;
                  return (
                    <li key={brand.id}>
                      <button
                        type="button" aria-current={active ? "true" : undefined}
                        onClick={() => { setSelectedId(brand.id); setCreating(false); }}
                        className={`flex min-h-11 w-full items-center justify-between gap-2 rounded-control px-3 text-start text-sm hover:bg-paper ${active ? "bg-orange/10 font-bold" : ""}`}
                      >
                        <span dir="ltr" className="[unicode-bidi:isolate]">{brand.name}</span>
                        {brand.isActive ? null : <span className="rounded-full bg-paper px-2 py-0.5 text-xs text-ink-muted">{t("off")}</span>}
                      </button>
                    </li>
                  );
                })}
              </ul>
            )}
          </div>

          <section ref={panelRef} className="scroll-mt-20 flex min-w-0 flex-col gap-4 rounded-card border border-line bg-surface p-5">
            {creating ? (
              <>
                <h2 className="m-0 text-[17px] font-bold">{t("createTitle")}</h2>
                <BrandForm key="new" brand={null} canManage={canManage} onSaved={saved} onStale={reload} onCancel={() => setCreating(false)} />
              </>
            ) : selected ? (
              <>
                <h2 className="m-0 text-[17px] font-bold">{t("editTitle", { name: selected.name })}</h2>
                <BrandForm
                  key={`${selected.id}-${selected.rowVersion}`} brand={selected} canManage={canManage}
                  onSaved={saved} onStale={reload} onCancel={() => setSelectedId(null)}
                />
              </>
            ) : (
              <p className="m-0 text-sm text-ink-muted">{t("empty")}</p>
            )}
          </section>
        </div>
      )}
    </>
  );
}
