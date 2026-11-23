"use client";

import { useCallback, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { formatDate } from "@/lib/format";
import { useLoad } from "@/lib/hooks/use-load";
import { cairoToday } from "../dashboard/dashboard-rules";
import { DataTable, type Column } from "../kit/DataTable";
import { ErrorPanel, LoadingCard } from "../kit/LoadState";
import { PageHeader } from "../kit/PageHeader";
import { useUrlFilters } from "../kit/use-url-filters";
import { EditSheet } from "../kit/EditSheet";
import { OfferStatusBadge } from "./OfferActions";
import { OfferForm } from "./OfferForm";
import { offerDiscount } from "./offer-rules";
import { fetchOffers, fetchProducts, type Offer } from "./offers-api";
import { PlusIcon } from "@/components/ui/icons";
import { Button } from "../kit/Button";

const FILTERS = { id: "" };

/** ADM-Offers (the offers tab): the list, then the form for the selected or a new offer. */
export function Offers() {
  const t = useTranslations("admin.offers");
  const locale = useLocale();
  const { values, set } = useUrlFilters(FILTERS);
  const [creating, setCreating] = useState(false);
  const load = useCallback(async () => {
    const [offers, products] = await Promise.all([fetchOffers(), fetchProducts().catch(() => [])]);
    return { offers, products };
  }, []);
  const { state, reload } = useLoad(load);
  const data = state.status === "ready" ? state.data : { offers: [], products: [] };
  const activeId = creating ? null : values.id || null;
  const selected = data.offers.find((offer) => offer.id === activeId) ?? null;
  const today = cairoToday(new Date());
  const closeSheet = () => { setCreating(false); set({ id: "" }); };

  function discount(offer: Offer): string {
    const range = offerDiscount(offer.items);
    if (!range) return t("none");
    return range.min === range.max ? `${range.min}%` : `${range.min}–${range.max}%`;
  }

  const columns: Column<Offer>[] = [
    {
      key: "title",
      header: t("columns.title"),
      cell: (offer) => (
        <button type="button" aria-current={offer.id === activeId ? "true" : undefined} onClick={() => { setCreating(false); set({ id: offer.id }); }} className={`flex min-w-0 flex-col items-start gap-0.5 text-start ${offer.id === activeId ? "text-orange-text" : ""}`}>
          <span dir="auto" className="max-w-64 truncate font-bold [unicode-bidi:isolate]">{offer.titleAr}</span>
          <span className="text-xs text-ink-muted">{offer.titleEn}</span>
        </button>
      )
    },
    {
      key: "target", header: t("columns.target"),
      cell: (offer) => <span className="text-sm">{offer.items.length > 0 ? t("products", { count: offer.items.length }) : t("none")}</span>
    },
    { key: "discount", header: t("columns.discount"), align: "end", cell: (offer) => <span className="whitespace-nowrap font-bold">{discount(offer)}</span> },
    {
      key: "period", header: t("columns.period"),
      cell: (offer) => <span className="whitespace-nowrap text-xs">{formatDate(offer.startsAtUtc, locale)}{offer.endsAtUtc ? ` – ${formatDate(offer.endsAtUtc, locale)}` : ""}</span>
    },
    { key: "status", header: t("columns.status"), cell: (offer) => <OfferStatusBadge status={offer.status} /> }
  ];

  return (
    <>
      <PageHeader
        title={t("title")}
        actions={
          <Button variant="primary" icon={PlusIcon} onClick={() => { setCreating(true); set({ id: "" }); }}>{t("new")}</Button>
        }
      />
      {state.status === "error" ? (
        <ErrorPanel message={t("failed")} retryLabel={t("retry")} onRetry={reload} />
      ) : (
        <>
          {state.status === "loading" ? <LoadingCard className="h-48" /> : (
            <DataTable label={t("title")} columns={columns} state={{ status: "ready", data: data.offers }} onRetry={reload} rowKey={(offer) => offer.id} empty={t("empty")} />
          )}
          <EditSheet open={creating || selected !== null} title={creating ? t("newTitle") : t("editTitle")} onClose={closeSheet}>
            {creating ? (
              <OfferForm key="new" offer={null} products={data.products} today={today} onSaved={(saved) => { setCreating(false); set({ id: saved.id }); reload(); }} onStale={reload} onCancel={closeSheet} onReload={reload} />
            ) : selected ? (
              <OfferForm key={`${selected.id}-${selected.rowVersion}`} offer={selected} products={data.products} today={today} onSaved={(saved) => { set({ id: saved.id }); reload(); }} onStale={reload} onCancel={closeSheet} onReload={reload} />
            ) : null}
          </EditSheet>
        </>
      )}
    </>
  );
}
