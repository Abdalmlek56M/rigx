"use client";

import { useCallback, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { listSeparator } from "@/lib/format";
import { useLoad } from "@/lib/hooks/use-load";
import { SearchBox } from "../kit/FilterBar";
import { useToast } from "../kit/Toast";
import { usePermissions } from "../use-permissions";
import { SellerFormCard, type Governorate } from "./SellerFormCard";
import { fetchSeller, findSellers, type Seller, type SellerSummary } from "./purchases-api";
import { looksLikePhone, sellerQuery, toSummary } from "./purchase-rules";
import { PencilIcon, PlusIcon, SwapIcon } from "@/components/ui/icons";
import { Button } from "../kit/Button";

const MIN_QUERY = 3;

type Editing = { seller: Seller | null; phone: string } | null;

/** The seller: look one up by phone or name, pick a match or create one; a picked seller can be changed or (with the permission) edited. */
export function SellerPicker({ seller, readOnly, missing = false, governorates, onPick }: {
  seller: SellerSummary | null;
  readOnly: boolean;
  missing?: boolean;
  governorates: readonly Governorate[];
  onPick: (seller: SellerSummary | null) => void;
}) {
  const t = useTranslations("admin.purchases.seller");
  const locale = useLocale();
  const toast = useToast();
  const { has } = usePermissions();
  const canManage = has("Purchasing.ManageSuppliers");
  const [query, setQuery] = useState("");
  const [editing, setEditing] = useState<Editing>(null);
  const matches = useLoad(useCallback(() => (query.length >= MIN_QUERY ? findSellers(sellerQuery(query)) : Promise.resolve([])), [query]));

  async function edit() {
    if (!seller) return;
    try {
      setEditing({ seller: await fetchSeller(seller.id), phone: "" });
    } catch {
      toast(t("loadFailed"), "error");
    }
  }

  async function reloadSeller(current: Seller | null) {
    if (current) setEditing({ seller: await fetchSeller(current.id), phone: "" });
  }

  const saved = (next: Seller) => {
    onPick(toSummary(next));
    setEditing(null);
  };

  return (
    <section data-field="seller" className={`flex flex-col gap-3 rounded-card border bg-surface p-5 ${missing ? "border-error" : "border-line"}`}>
      <h2 className="m-0 text-[17px] font-bold">{t("title")}</h2>
      {missing ? <p role="alert" className="m-0 text-sm font-bold text-error-text">{t("required")}</p> : null}
      {editing ? (
        <SellerFormCard
          key={editing.seller?.rowVersion ?? "new"} seller={editing.seller} phone={editing.phone} governorates={governorates}
          onSaved={saved} onStale={() => void reloadSeller(editing.seller)}
          onCancel={() => setEditing(null)}
        />
      ) : seller ? (
        <div className="flex flex-wrap items-center justify-between gap-3 rounded-control bg-paper p-3">
          <div className="flex flex-col gap-0.5">
            <span dir="auto" className="font-bold [unicode-bidi:isolate]">{seller.name}</span>
            <span className="text-xs text-ink-muted">{t(`kinds.${seller.kind === "Trader" || seller.kind === "Company" ? seller.kind : "Person"}`)}{listSeparator(locale)}<span dir="ltr" className="font-mono">{seller.phone}</span></span>
          </div>
          {readOnly ? null : (
            <div className="flex flex-wrap gap-2">
              {canManage ? <Button icon={PencilIcon} onClick={() => void edit()}>{t("edit")}</Button> : null}
              <Button icon={SwapIcon} onClick={() => onPick(null)}>{t("change")}</Button>
            </div>
          )}
        </div>
      ) : (
        <div className="flex flex-col gap-3">
          <SearchBox label={t("search")} value={query} onChange={setQuery} />
          {query.length >= MIN_QUERY && matches.state.status === "ready" ? (
            <ul className="m-0 flex list-none flex-col gap-1.5 p-0">
              {matches.state.data.map((match) => (
                <li key={match.id}>
                  <button type="button" onClick={() => onPick(toSummary(match))} className="flex min-h-11 w-full flex-wrap items-center justify-between gap-2 rounded-control border border-line bg-paper px-3 py-2 text-start text-sm">
                    <span dir="auto" className="font-bold [unicode-bidi:isolate]">{match.name}</span>
                    <span dir="ltr" className="font-mono text-xs text-ink-muted">{match.phone}</span>
                  </button>
                </li>
              ))}
              {matches.state.data.length === 0 ? <li className="text-sm text-ink-muted">{t("noMatch")}</li> : null}
            </ul>
          ) : null}
          {matches.state.status === "error" ? <p role="alert" className="m-0 text-sm font-bold text-error-text">{t("searchFailed")}</p> : null}
          {canManage ? <Button icon={PlusIcon} className="self-start" onClick={() => setEditing({ seller: null, phone: looksLikePhone(query) ? query : "" })}>{t("new")}</Button> : null}
        </div>
      )}
    </section>
  );
}
