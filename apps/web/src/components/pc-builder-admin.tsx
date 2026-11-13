"use client";

import { useEffect, useMemo, useState } from "react";
import {
  createPcBuild,
  getAdminPcBuilds,
  getAdminProducts,
  setPcBuildPublished,
  updatePcBuild,
  type CatalogProductSummary,
  type PcBuild,
} from "@/lib/api-client";
import { apiErrorMessage } from "@/lib/form-errors";
import { useLocale } from "@/lib/i18n";
import { LoadingCard, Message, PageIntro, PrimaryButton, SecondaryButton, TextField } from "@/components/ui";

const slotKeys = ["CPU", "Motherboard", "RAM", "GPU", "Storage", "PSU", "Case", "Cooling", "Monitor", "Peripherals"];

type OptionDraft = { productId: string; isDefault: boolean };
type SlotDraft = { slotKey: string; labelAr: string; labelEn: string; isRequired: boolean; options: OptionDraft[] };

export function PcBuilderAdmin() {
  const { messages } = useLocale();
  const [builds, setBuilds] = useState<PcBuild[]>([]);
  const [products, setProducts] = useState<CatalogProductSummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const [editing, setEditing] = useState<PcBuild | null>(null);
  const [open, setOpen] = useState(false);
  const [saving, setSaving] = useState(false);
  const [form, setForm] = useState({ titleAr: "", titleEn: "", summaryAr: "", summaryEn: "" });
  const [slots, setSlots] = useState<SlotDraft[]>([]);

  async function load() {
    setLoading(true);
    setError(null);
    try {
      const [buildList, productList] = await Promise.all([getAdminPcBuilds(), getAdminProducts()]);
      setBuilds(buildList);
      setProducts(productList.filter((product) => product.status === "Published"));
    } catch (requestError) {
      setError(apiErrorMessage(requestError, messages));
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  function openNew() {
    setEditing(null);
    setForm({ titleAr: "", titleEn: "", summaryAr: "", summaryEn: "" });
    setSlots([{ slotKey: "CPU", labelAr: "المعالج", labelEn: "CPU", isRequired: true, options: [{ productId: "", isDefault: true }] }]);
    setOpen(true);
  }

  function openEdit(build: PcBuild) {
    setEditing(build);
    setForm({ titleAr: build.titleAr, titleEn: build.titleEn, summaryAr: build.summaryAr ?? "", summaryEn: build.summaryEn ?? "" });
    setSlots(build.slots.map((slot) => ({
      slotKey: slot.slotKey,
      labelAr: slot.labelAr,
      labelEn: slot.labelEn,
      isRequired: slot.isRequired,
      options: slot.options.map((option) => ({ productId: option.productId, isDefault: option.isDefault })),
    })));
    setOpen(true);
  }

  async function save() {
    setSaving(true);
    setError(null);
    setNotice(null);
    const input = {
      titleAr: form.titleAr,
      titleEn: form.titleEn,
      summaryAr: form.summaryAr.trim() || null,
      summaryEn: form.summaryEn.trim() || null,
      slots: slots.map((slot) => ({ ...slot, options: slot.options.filter((option) => option.productId) })),
      rowVersion: editing?.rowVersion,
    };
    try {
      if (editing) await updatePcBuild(editing.id, input as never);
      else await createPcBuild(input as never);
      setNotice(messages.adminPcBuildSaved);
      setOpen(false);
      await load();
    } catch (requestError) {
      setError(apiErrorMessage(requestError, messages));
    } finally {
      setSaving(false);
    }
  }

  async function toggle(build: PcBuild) {
    setError(null);
    try {
      await setPcBuildPublished(build.id, build.status !== "Published", build.rowVersion);
      await load();
    } catch (requestError) {
      setError(apiErrorMessage(requestError, messages));
    }
  }

  const previewTotal = useMemo(() => slots.reduce((total, slot) => {
    const first = slot.options.find((option) => option.productId);
    return total + (first ? products.find((product) => product.id === first.productId)?.basePrice ?? 0 : 0);
  }, 0), [products, slots]);

  return (
    <main className="p-4 sm:p-6 lg:p-8">
      <div className="mx-auto max-w-[80rem]">
        <div className="flex flex-wrap items-start justify-between gap-4">
          <PageIntro eyebrow="RIGX / PC BUILDER" title={messages.adminPcBuilderTitle} lead={messages.adminPcBuilderLead} />
          <PrimaryButton type="button" onClick={openNew}>{messages.adminPcBuildNew}</PrimaryButton>
        </div>
        {notice ? <div className="my-4"><Message tone="success">{notice}</Message></div> : null}
        {error ? <div className="my-4"><Message tone="error">{error}</Message></div> : null}
        {loading ? <LoadingCard /> : builds.length === 0 ? <Message tone="info">{messages.adminPcBuildEmpty}</Message> : (
          <section className="rigx-admin-card divide-y divide-[var(--ds-border)]">
            {builds.map((build) => (
              <article key={build.id} className="flex flex-wrap items-center justify-between gap-3 p-4">
                <div>
                  <p className="font-bold text-[var(--ds-text)]">{build.titleAr}</p>
                  <p className="text-sm text-[var(--ds-text-muted)]" dir="ltr">{build.titleEn} · {build.slots.length} slots</p>
                </div>
                <div className="flex flex-wrap items-center gap-2">
                  <span className="rounded-full border border-[var(--ds-border)] bg-[var(--ds-surface-muted)] px-2.5 py-1 text-xs font-bold">{build.status}</span>
                  <SecondaryButton type="button" onClick={() => openEdit(build)}>{messages.adminEdit}</SecondaryButton>
                  <SecondaryButton type="button" onClick={() => void toggle(build)}>{build.status === "Published" ? messages.adminUnpublish : messages.adminPublish}</SecondaryButton>
                </div>
              </article>
            ))}
          </section>
        )}
      </div>

      {open ? (
        <div className="fixed inset-0 z-[70] overflow-y-auto bg-slate-950/60 p-3 sm:p-6" role="dialog" aria-modal="true">
          <div className="mx-auto grid max-w-6xl gap-5 rounded-2xl border border-[var(--ds-border)] bg-[var(--ds-surface)] p-4 lg:grid-cols-[minmax(0,1.3fr)_minmax(18rem,.7fr)] sm:p-6">
            <div className="grid gap-4">
              <div className="flex items-center justify-between gap-3">
                <h2 className="text-xl font-black">{editing ? messages.adminPcBuildEdit : messages.adminPcBuildNew}</h2>
                <SecondaryButton type="button" onClick={() => setOpen(false)}>{messages.adminCancel}</SecondaryButton>
              </div>
              <div className="grid gap-3 sm:grid-cols-2">
                <TextField label={messages.adminArticleTitleAr} value={form.titleAr} onChange={(event) => setForm((current) => ({ ...current, titleAr: event.target.value }))} />
                <TextField label={messages.adminArticleTitleEn} value={form.titleEn} onChange={(event) => setForm((current) => ({ ...current, titleEn: event.target.value }))} dir="ltr" />
                <TextField label={messages.adminArticleSummaryAr} value={form.summaryAr} onChange={(event) => setForm((current) => ({ ...current, summaryAr: event.target.value }))} />
                <TextField label={messages.adminArticleSummaryEn} value={form.summaryEn} onChange={(event) => setForm((current) => ({ ...current, summaryEn: event.target.value }))} dir="ltr" />
              </div>
              <div className="flex items-center justify-between">
                <p className="font-bold">{messages.adminPcBuildSlot}</p>
                <SecondaryButton type="button" onClick={() => setSlots((current) => [...current, { slotKey: "GPU", labelAr: "", labelEn: "", isRequired: true, options: [{ productId: "", isDefault: true }] }])}>{messages.adminPcBuildAddSlot}</SecondaryButton>
              </div>
              {slots.map((slot, slotIndex) => (
                <div key={slotIndex} className="grid gap-2 rounded-xl border border-[var(--ds-border)] p-3">
                  <div className="grid gap-2 sm:grid-cols-3">
                    <select value={slot.slotKey} onChange={(event) => setSlots((current) => current.map((row, index) => index === slotIndex ? { ...row, slotKey: event.target.value } : row))} className="min-h-11 rounded-xl border border-[var(--ds-border)] bg-[var(--ds-surface)] px-3 text-sm">
                      {slotKeys.map((key) => <option key={key} value={key}>{key}</option>)}
                    </select>
                    <input value={slot.labelAr} onChange={(event) => setSlots((current) => current.map((row, index) => index === slotIndex ? { ...row, labelAr: event.target.value } : row))} placeholder={messages.adminPcBuildSlotLabelAr} className="min-h-11 rounded-xl border border-[var(--ds-border)] bg-[var(--ds-surface)] px-3 text-sm" />
                    <input value={slot.labelEn} onChange={(event) => setSlots((current) => current.map((row, index) => index === slotIndex ? { ...row, labelEn: event.target.value } : row))} placeholder={messages.adminPcBuildSlotLabelEn} dir="ltr" className="min-h-11 rounded-xl border border-[var(--ds-border)] bg-[var(--ds-surface)] px-3 text-sm" />
                  </div>
                  <label className="flex items-center gap-2 text-sm"><input type="checkbox" checked={slot.isRequired} onChange={(event) => setSlots((current) => current.map((row, index) => index === slotIndex ? { ...row, isRequired: event.target.checked } : row))} />{messages.adminPcBuildRequired}</label>
                  {slot.options.map((option, optionIndex) => (
                    <div key={optionIndex} className="grid gap-2 sm:grid-cols-[1fr_auto_auto]">
                      <select value={option.productId} onChange={(event) => setSlots((current) => current.map((row, index) => index === slotIndex ? { ...row, options: row.options.map((entry, optionKey) => optionKey === optionIndex ? { ...entry, productId: event.target.value } : entry) } : row))} className="min-h-11 rounded-xl border border-[var(--ds-border)] bg-[var(--ds-surface)] px-3 text-sm">
                        <option value="">{messages.adminPcBuildChooseProduct}</option>
                        {products.map((product) => <option key={product.id} value={product.id}>{product.name} · {product.sku}</option>)}
                      </select>
                      <label className="flex items-center gap-1 text-xs"><input type="radio" name={`default-${slotIndex}`} checked={option.isDefault} onChange={() => setSlots((current) => current.map((row, index) => index === slotIndex ? { ...row, options: row.options.map((entry, optionKey) => ({ ...entry, isDefault: optionKey === optionIndex })) } : row))} />{messages.adminPcBuildDefaultOption}</label>
                      <SecondaryButton type="button" onClick={() => setSlots((current) => current.map((row, index) => index === slotIndex ? { ...row, options: row.options.filter((_, optionKey) => optionKey !== optionIndex) } : row))}>{messages.adminDelete}</SecondaryButton>
                    </div>
                  ))}
                  <SecondaryButton type="button" onClick={() => setSlots((current) => current.map((row, index) => index === slotIndex ? { ...row, options: [...row.options, { productId: "", isDefault: false }] } : row))}>{messages.adminPcBuildAddOption}</SecondaryButton>
                </div>
              ))}
              <div className="flex flex-wrap gap-2">
                <PrimaryButton type="button" onClick={() => void save()} pending={saving}>{messages.adminSaveChanges}</PrimaryButton>
                <SecondaryButton type="button" onClick={() => setOpen(false)}>{messages.adminCancel}</SecondaryButton>
              </div>
            </div>
            <aside className="grid h-fit gap-3 rounded-xl border border-[var(--ds-border)] bg-[var(--ds-surface-muted)] p-4 lg:sticky lg:top-4">
              <p className="text-xs font-bold uppercase tracking-[0.14em] text-[var(--ds-accent-strong)]">{messages.adminPcBuildPreview}</p>
              <h3 className="text-xl font-black">{form.titleAr || form.titleEn || "—"}</h3>
              <p className="text-sm text-[var(--ds-text-muted)]">{form.summaryAr || form.summaryEn}</p>
              <ul className="grid gap-2 text-sm">
                {slots.map((slot, index) => {
                  const product = products.find((entry) => entry.id === slot.options.find((option) => option.productId)?.productId);
                  return <li key={index} className="flex justify-between gap-2"><span>{slot.labelAr || slot.slotKey}</span><span className="text-[var(--ds-text-muted)]">{product?.name ?? "—"}</span></li>;
                })}
              </ul>
              <p className="font-bold">{messages.pcBuildTotal}: {previewTotal.toLocaleString()} EGP</p>
            </aside>
          </div>
        </div>
      ) : null}
    </main>
  );
}
