"use client";

import { useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { formatEgp } from "@/lib/format";
import { TextField } from "@/components/ui/TextField";
import { DataTable, type Column } from "../kit/DataTable";
import { addOfferItem, filterProducts, itemDiscount, removeOfferItem, type OfferItemForm } from "./offer-rules";
import type { ProductSummary } from "./offers-api";

/** The offer's products: search the catalog, add at a suggested price, then edit each offer price. */
export function OfferItems({ items, products, onChange, onReload }: {
  items: readonly OfferItemForm[];
  products: readonly ProductSummary[];
  onChange: (items: OfferItemForm[]) => void;
  onReload: () => void;
}) {
  const t = useTranslations("admin.offers.items");
  const locale = useLocale();
  const [search, setSearch] = useState("");
  const matches = products.length > 0 ? filterProducts(products, search, items.map((item) => item.productId)) : [];

  function price(productId: string, value: string) {
    onChange(items.map((item) => item.productId === productId ? { ...item, offerPrice: value } : item));
  }

  const columns: Column<OfferItemForm>[] = [
    { key: "product", header: t("product"), cell: (item) => <span className="flex min-w-0 flex-col gap-0.5"><b dir="auto" className="max-w-56 truncate [unicode-bidi:isolate]">{item.name}</b><span dir="ltr" className="font-mono text-xs text-ink-muted">{item.sku}</span></span> },
    { key: "base", header: t("basePrice"), align: "end", cell: (item) => <span className="whitespace-nowrap">{formatEgp(item.basePrice, locale)}</span> },
    {
      key: "price", header: t("offerPrice"), align: "end",
      cell: (item) => (
        <input
          aria-label={t("offerPrice")} type="number" min="0" step="0.01" dir="ltr" value={item.offerPrice}
          onChange={(event) => price(item.productId, event.target.value)}
          className="h-9 w-28 rounded-control border border-input-line bg-surface px-2 text-sm"
        />
      )
    },
    {
      key: "discount", header: t("discount"), align: "end",
      cell: (item) => {
        const value = Number(item.offerPrice);
        if (!Number.isFinite(value) || value <= 0 || value >= item.basePrice) return <span className="text-xs font-bold text-error">{t("invalid")}</span>;
        return <span className="whitespace-nowrap font-bold">{itemDiscount(item.basePrice, value)}%</span>;
      }
    },
    {
      key: "remove", header: "", cell: (item) => (
        <button type="button" onClick={() => onChange(removeOfferItem(items, item.productId))} className="h-9 rounded-control px-2 text-xs font-bold text-error">{t("remove")}</button>
      )
    }
  ];

  return (
    <div className="flex flex-col gap-3">
      <div className="flex flex-col gap-2">
        <TextField label={t("search")} name="productSearch" value={search} onChange={setSearch} placeholder={t("searchPlaceholder")} />
        {matches.length > 0 ? (
          <ul className="m-0 flex list-none flex-col gap-1 rounded-control border border-line p-2">
            {matches.map((product) => (
              <li key={product.id} className="flex flex-wrap items-center gap-2 text-sm">
                <button type="button" onClick={() => onChange(addOfferItem(items, product))} className="min-w-0 flex-1 text-start font-bold hover:text-orange-text">
                  <span dir="auto" className="[unicode-bidi:isolate]">{product.name}</span> <span dir="ltr" className="font-mono text-xs text-ink-muted">{product.sku}</span>
                </button>
                <span className="whitespace-nowrap text-xs text-ink-muted">{formatEgp(product.basePrice, locale)}</span>
                <button type="button" onClick={() => onChange(addOfferItem(items, product))} className="h-9 rounded-control bg-carbon px-3 text-xs font-bold text-on-carbon">{t("add")}</button>
              </li>
            ))}
          </ul>
        ) : search.trim() !== "" ? <p className="m-0 text-xs text-ink-muted">{t("noMatches")}</p> : null}
        {products.length === 0 ? <p className="m-0 text-xs text-ink-muted">{t("noCatalog")}</p> : null}
      </div>
      <DataTable label={t("title")} columns={columns} state={{ status: "ready", data: items }} onRetry={onReload} rowKey={(item) => item.productId} empty={t("empty")} />
    </div>
  );
}
