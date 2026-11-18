"use client";

import { useCallback, useState } from "react";
import { useTranslations } from "next-intl";
import { useLoad } from "@/lib/hooks/use-load";
import { SearchBox } from "../kit/FilterBar";
import { useToast } from "../kit/Toast";
import { usePermissions } from "../use-permissions";
import { SellerFormCard, type Governorate } from "./SellerFormCard";
import { fetchSeller, findSellers, type Seller, type SellerSummary } from "./purchases-api";
import { looksLikePhone, sellerQuery, toSummary } from "./purchase-rules";

const MIN_QUERY = 3;
const button = "h-11 rounded-control border border-input-line bg-surface px-4 text-sm font-bold";

type Editing = { seller: Seller | null; phone: string } | null;

/** The seller: look one up by phone or name, pick a match or create one; a picked seller can be changed or (with the permission) edited. */
export function SellerPicker({ seller, readOnly, governorates, onPick }: {
  seller: SellerSummary | null;
  readOnly: boolean;
  governorates: readonly Governorate[];
  onPick: (seller: SellerSummary | null) => void;
}) {
  const t = useTranslations("admin.purchases.seller");
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
    <section className="flex flex-col gap-3 rounded-[10px] border border-line bg-surface p-5">
      <h2 className="m-0 text-[17px] font-bold">{t("title")}</h2>
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
            <span className="text-xs text-ink-muted">{t(`kinds.${seller.kind === "Trader" || seller.kind === "Company" ? seller.kind : "Person"}`)} · <span dir="ltr" className="font-mono">{seller.phone}</span></span>
          </div>
          {readOnly ? null : (
            <div className="flex flex-wrap gap-2">
              {canManage ? <button type="button" onClick={() => void edit()} className={button}>{t("edit")}</button> : null}
              <button type="button" onClick={() => onPick(null)} className={button}>{t("change")}</button>
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
          {matches.state.status === "error" ? <p role="alert" className="m-0 text-sm font-bold text-error">{t("searchFailed")}</p> : null}
          {canManage ? <button type="button" onClick={() => setEditing({ seller: null, phone: looksLikePhone(query) ? query : "" })} className={`${button} self-start`}>{t("new")}</button> : null}
        </div>
      )}
    </section>
  );
}
