"use client";

import { useEffect, useMemo, useState } from "react";
import {
  createRetailAssembly, disassembleRetailAssembly, getPurchaseDrafts, getRetailAssemblies, getRetailUnits,
  receivePurchaseDraft, setRetailAssemblyAllocations, setRetailAssemblyPublished, updateRetailAssembly,
  type PurchaseDraft, type RetailAssembly, type RetailUnit,
} from "@/lib/api-client";
import { apiErrorMessage } from "@/lib/form-errors";
import { useLocale } from "@/lib/i18n";
import { LoadingCard, Message, PageIntro, PrimaryButton, SecondaryButton } from "@/components/ui";

export function RetailStockAdmin() {
  const { messages } = useLocale();
  const [units, setUnits] = useState<RetailUnit[]>([]);
  const [assemblies, setAssemblies] = useState<RetailAssembly[]>([]);
  const [drafts, setDrafts] = useState<PurchaseDraft[]>([]);
  const [selectedDraft, setSelectedDraft] = useState("");
  const [selectedUnits, setSelectedUnits] = useState<string[]>([]);
  const [assemblyName, setAssemblyName] = useState("");
  const [prices, setPrices] = useState<Record<string, string>>({});
  const [allocations, setAllocations] = useState<Record<string, Record<string, string>>>({});
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);

  async function load() {
    setLoading(true);
    try {
      const [unitList, assemblyList, draftPage] = await Promise.all([
        getRetailUnits(), getRetailAssemblies(), getPurchaseDrafts({ pageSize: 100 }),
      ]);
      setUnits(unitList); setAssemblies(assemblyList);
      setDrafts(draftPage.items.filter((draft) => draft.status === "Draft"));
      setAllocations((current) => {
        const next = { ...current };
        assemblyList.forEach((assembly) => {
          next[assembly.id] = Object.fromEntries(
            assembly.units.map((unit) => [unit.unitId, next[assembly.id]?.[unit.unitId] ?? String(unit.allocationPercent)]),
          );
        });
        return next;
      });
    } catch (requestError) { setError(apiErrorMessage(requestError, messages)); }
    finally { setLoading(false); }
  }
  useEffect(() => { void load(); /* eslint-disable-next-line */ }, []);

  const available = useMemo(() => units.filter((unit) => unit.status === "Available"), [units]);

  async function receive() {
    if (!selectedDraft) return;
    setError(null);
    try { await receivePurchaseDraft(selectedDraft, null); setNotice(messages.retailReceived); setSelectedDraft(""); await load(); }
    catch (requestError) { setError(apiErrorMessage(requestError, messages)); }
  }
  async function assemble() {
    if (!assemblyName.trim() || selectedUnits.length === 0) return;
    try { await createRetailAssembly(assemblyName.trim(), selectedUnits); setNotice(messages.retailAssemblyCreated); setAssemblyName(""); setSelectedUnits([]); await load(); }
    catch (requestError) { setError(apiErrorMessage(requestError, messages)); }
  }
  async function savePrice(assembly: RetailAssembly) {
    const salePrice = Number(prices[assembly.id] ?? assembly.salePrice);
    if (!Number.isFinite(salePrice) || salePrice < 0) { setError(messages.retailInvalidPrice); return; }
    setError(null);
    try {
      await updateRetailAssembly(assembly.id, {
        name: assembly.name, publicTitleAr: assembly.publicTitleAr, publicTitleEn: assembly.publicTitleEn,
        salePrice, rowVersion: assembly.rowVersion,
      });
      setNotice(messages.retailSaved);
      await load();
    } catch (requestError) { setError(apiErrorMessage(requestError, messages)); }
  }
  async function saveAllocations(assembly: RetailAssembly) {
    const rows = assembly.units.map((unit) => ({
      unitId: unit.unitId,
      percent: Number(allocations[assembly.id]?.[unit.unitId] ?? unit.allocationPercent),
    }));
    if (rows.some((row) => !Number.isFinite(row.percent) || row.percent < 0 || row.percent > 100)) {
      setError(messages.retailInvalidAllocation); return;
    }
    if (Math.abs(rows.reduce((sum, row) => sum + row.percent, 0) - 100) > 0.01) {
      setError(messages.retailAllocationSum); return;
    }
    setError(null);
    try {
      await setRetailAssemblyAllocations(assembly.id, rows, assembly.rowVersion);
      setNotice(messages.retailSaved);
      await load();
    } catch (requestError) { setError(apiErrorMessage(requestError, messages)); }
  }
  async function togglePublish(assembly: RetailAssembly) {
    try { await setRetailAssemblyPublished(assembly.id, assembly.status !== "Published", assembly.rowVersion); await load(); }
    catch (requestError) { setError(apiErrorMessage(requestError, messages)); }
  }
  async function disassemble(assembly: RetailAssembly) {
    try { await disassembleRetailAssembly(assembly.id, assembly.rowVersion); await load(); }
    catch (requestError) { setError(apiErrorMessage(requestError, messages)); }
  }

  return (
    <main className="p-4 sm:p-6 lg:p-8"><div className="mx-auto grid max-w-6xl gap-6">
      <PageIntro eyebrow="RIGX / RETAIL" title={messages.retailStockTitle} lead={messages.retailStockLead} />
      {notice ? <Message tone="success">{notice}</Message> : null}
      {error ? <Message tone="error">{error}</Message> : null}
      {loading ? <LoadingCard /> : <>
        <section className="rigx-admin-card p-4">
          <h2 className="font-bold">{messages.retailReceiveTitle}</h2>
          <div className="mt-3 flex flex-wrap gap-2">
            <select value={selectedDraft} onChange={(event) => setSelectedDraft(event.target.value)} className="min-h-11 flex-1 rounded-xl border border-[var(--ds-border)] bg-[var(--ds-surface)] px-3 text-sm">
              <option value="">{messages.retailChooseDraft}</option>
              {drafts.map((draft) => <option key={draft.id} value={draft.id}>{draft.supplierCompanyName ?? draft.marketplaceName ?? draft.externalSellerName ?? draft.id} · {draft.items.length} items</option>)}
            </select>
            <PrimaryButton type="button" onClick={() => void receive()} disabled={!selectedDraft}>{messages.retailReceiveAction}</PrimaryButton>
          </div>
        </section>

        <section className="rigx-admin-card p-4">
          <h2 className="font-bold">{messages.retailUnitsTitle} ({available.length})</h2>
          <div className="mt-3 grid max-h-80 gap-2 overflow-y-auto">
            {available.length === 0 ? <p className="text-sm text-[var(--ds-text-muted)]">{messages.retailNoAvailableUnits}</p> : available.map((unit) => (
              <label key={unit.id} className="flex items-center gap-3 rounded-lg border border-[var(--ds-border)] p-2 text-sm">
                <input type="checkbox" checked={selectedUnits.includes(unit.id)} onChange={(event) => setSelectedUnits((current) => event.target.checked ? [...current, unit.id] : current.filter((id) => id !== unit.id))} />
                <span className="flex-1">{unit.name} <span className="text-[var(--ds-text-muted)]" dir="ltr">{unit.sku} · {unit.condition}</span></span>
                <span dir="ltr">{unit.acquisitionCost?.toLocaleString() ?? messages.retailHidden}</span>
              </label>
            ))}
          </div>
          <div className="mt-3 flex flex-wrap gap-2">
            <input value={assemblyName} onChange={(event) => setAssemblyName(event.target.value)} placeholder={messages.retailAssemblyName} className="min-h-11 flex-1 rounded-xl border border-[var(--ds-border)] bg-[var(--ds-surface)] px-3 text-sm" />
            <SecondaryButton type="button" onClick={() => void assemble()} disabled={!assemblyName.trim() || selectedUnits.length === 0}>{messages.retailCreateAssembly}</SecondaryButton>
          </div>
        </section>

        <section className="rigx-admin-card divide-y divide-[var(--ds-border)]">
          {assemblies.length === 0 ? <Message tone="info">{messages.retailNoAssemblies}</Message> : assemblies.map((assembly) => {
            const editable = assembly.status !== "Sold" && assembly.status !== "Disassembled";
            return (
              <article key={assembly.id} className="grid gap-3 p-4">
                <div className="flex flex-wrap items-center justify-between gap-2">
                  <div>
                    <p className="font-bold">{assembly.name}</p>
                    <p className="text-sm text-[var(--ds-text-muted)]">
                      {assembly.units.length} {messages.retailUnitsLabel} · {messages.retailComponentCost} {assembly.componentCost?.toLocaleString() ?? messages.retailHidden} EGP ·
                      {" "}{messages.retailSalePrice} {assembly.salePrice.toLocaleString()} EGP · {assembly.status}
                    </p>
                  </div>
                  {editable ? (
                    <div className="flex flex-wrap items-end gap-2">
                      <label className="grid gap-1 text-xs font-bold">
                        <span>{messages.retailSalePrice}</span>
                        <input
                          value={prices[assembly.id] ?? String(assembly.salePrice)}
                          onChange={(event) => setPrices((current) => ({ ...current, [assembly.id]: event.target.value }))}
                          inputMode="decimal" dir="ltr"
                          className="min-h-10 w-28 rounded-lg border border-[var(--ds-border)] bg-[var(--ds-surface)] px-2 text-sm"
                          aria-label={`${messages.retailSalePrice} ${assembly.name}`}
                        />
                      </label>
                      <SecondaryButton type="button" onClick={() => void savePrice(assembly)}>{messages.retailSavePrice}</SecondaryButton>
                      <SecondaryButton type="button" onClick={() => void togglePublish(assembly)}>{assembly.status === "Published" ? messages.adminUnpublish : messages.adminPublish}</SecondaryButton>
                      <SecondaryButton type="button" onClick={() => void disassemble(assembly)}>{messages.retailDisassemble}</SecondaryButton>
                    </div>
                  ) : null}
                </div>
                {editable ? (
                  <div className="grid gap-2 rounded-xl border border-[var(--ds-border)] p-3">
                    <p className="text-xs font-bold text-[var(--ds-text-muted)]">{messages.retailAllocationHint}</p>
                    {assembly.units.map((unit) => (
                      <div key={unit.unitId} className="flex flex-wrap items-center gap-2 text-sm">
                        <span className="flex-1">{unit.name} <span className="text-[var(--ds-text-muted)]" dir="ltr">{unit.sku}</span></span>
                        <input
                          value={allocations[assembly.id]?.[unit.unitId] ?? String(unit.allocationPercent)}
                          onChange={(event) => setAllocations((current) => ({
                            ...current,
                            [assembly.id]: { ...(current[assembly.id] ?? {}), [unit.unitId]: event.target.value },
                          }))}
                          inputMode="decimal" dir="ltr" aria-label={`${messages.retailAllocationPercent} ${assembly.name} ${unit.name}`}
                          className="min-h-9 w-20 rounded-lg border border-[var(--ds-border)] bg-[var(--ds-surface)] px-2 text-sm"
                        />
                        <span className="text-xs text-[var(--ds-text-muted)]">% · {unit.allocatedAmount?.toLocaleString() ?? messages.retailHidden} EGP</span>
                      </div>
                    ))}
                    <div>
                      <SecondaryButton type="button" onClick={() => void saveAllocations(assembly)}>{messages.retailSaveAllocations}</SecondaryButton>
                    </div>
                  </div>
                ) : null}
              </article>
            );
          })}
        </section>
      </>}
    </div></main>
  );
}
