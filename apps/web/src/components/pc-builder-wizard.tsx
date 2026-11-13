"use client";

import Link from "next/link";
import { useEffect, useMemo, useState } from "react";
import { catalogMediaUrl, evaluatePcBuild, getPcBuild, type PcBuild } from "@/lib/api-client";
import { apiErrorMessage } from "@/lib/form-errors";
import { useLocale } from "@/lib/i18n";
import { currencyLabel, formatCatalogPrice } from "@/lib/storefront";
import { ProductImage } from "@/components/product-image";
import { LoadingCard, Message, PageIntro, PrimaryButton, SecondaryButton } from "@/components/ui";

type Compatibility = Awaited<ReturnType<typeof evaluatePcBuild>>;

export function PcBuilderWizard({ id }: { id: string }) {
  const { messages, locale } = useLocale();
  const [build, setBuild] = useState<PcBuild | null>(null);
  const [selections, setSelections] = useState<Record<string, string>>({});
  const [compatibility, setCompatibility] = useState<Compatibility | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);

  useEffect(() => {
    let active = true;
    setLoading(true);
    void getPcBuild(id)
      .then((template) => {
        if (!active) return;
        setBuild(template);
        const params = new URLSearchParams(window.location.search);
        const shared = new Map((params.get("config") ?? "").split(",").filter(Boolean).map((pair) => pair.split(":") as [string, string]));
        const initial: Record<string, string> = {};
        for (const slot of template.slots) {
          const sharedOption = shared.get(slot.slotKey);
          const fallback = slot.options.find((option) => option.isDefault) ?? slot.options[0];
          if (sharedOption && slot.options.some((option) => option.id === sharedOption)) initial[slot.slotKey] = sharedOption;
          else if (fallback) initial[slot.slotKey] = fallback.id;
        }
        setSelections(initial);
      })
      .catch((requestError) => { if (active) setError(apiErrorMessage(requestError, messages)); })
      .finally(() => { if (active) setLoading(false); });
    return () => { active = false; };
  }, [id, messages]);

  useEffect(() => {
    if (!build || Object.keys(selections).length === 0) return;
    let active = true;
    void evaluatePcBuild(build.id, selections)
      .then((result) => { if (active) setCompatibility(result); })
      .catch(() => { if (active) setCompatibility(null); });
    return () => { active = false; };
  }, [build, selections]);

  const chosen = useMemo(() => {
    if (!build) return [];
    return build.slots.map((slot) => ({
      slot,
      option: slot.options.find((option) => option.id === selections[slot.slotKey]) ?? null,
    }));
  }, [build, selections]);

  function save() {
    if (!build) return;
    try {
      window.localStorage.setItem(`rigx-pc-build-${build.id}`, JSON.stringify(selections));
      setNotice(messages.pcBuildSaved);
    } catch {
      setError(messages.cartStorageUnavailable);
    }
  }

  async function share() {
    if (!build) return;
    const config = Object.entries(selections).map(([slot, option]) => `${slot}:${option}`).join(",");
    const url = `${window.location.origin}/pc-builder/${build.id}?config=${encodeURIComponent(config)}`;
    try {
      await navigator.clipboard.writeText(url);
      setNotice(messages.pcBuildCopied);
    } catch {
      setNotice(url);
    }
  }

  if (loading) return <LoadingCard />;
  if (error) return <main className="mx-auto max-w-3xl px-5 py-10"><Message tone="error">{error}</Message></main>;
  if (!build) return null;

  const status = compatibility?.status ?? "Unconfirmed";
  const statusText = status === "Compatible" ? messages.pcBuildCompatible
    : status === "Incompatible" ? messages.pcBuildIncompatible : messages.pcBuildUnconfirmed;

  return (
    <>
      <div className="page-hero compact-hero">
        <div>
          <span className="eyebrow">RIGX CONFIGURATOR / 02</span>
          <h1>{locale === "ar" ? build.titleAr : build.titleEn}</h1>
          <p>{build.summaryAr ?? build.summaryEn ?? messages.pcBuilderLead}</p>
          <div className="hero-ctas"><Link className="btn-ghost" href="/pc-builder">← {messages.pcBuilderTitle}</Link></div>
        </div>
        <div className="builder-status"><span className="pulse" /> {statusText}</div>
      </div>
      <div className="builder-layout">
        <div className="builder-main-column">
          <div className="builder-slots glass-panel">
          {chosen.map(({ slot, option }) => (
            <article key={slot.id} className="builder-slot">
              <span className="slot-icon">{slot.slotKey.slice(0, 2).toUpperCase()}</span>
              <div className="slot-data">
              <h2 className="slot-title">
                {locale === "ar" ? slot.labelAr : slot.labelEn}
                {slot.isRequired ? <span className="text-[var(--ds-accent)]"> *</span> : null}
              </h2>
              <small dir="ltr">{slot.slotKey}</small>
              <select
                value={selections[slot.slotKey] ?? ""}
                aria-label={locale === "ar" ? slot.labelAr : slot.labelEn}
                onChange={(event) => setSelections((current) => ({ ...current, [slot.slotKey]: event.target.value }))}
                className="builder-select"
              >
                {slot.options.map((entry) => (
                  <option key={entry.id} value={entry.id}>
                    {entry.productName} · {formatCatalogPrice(entry.price)} {currencyLabel(locale)}
                  </option>
                ))}
              </select>
              {option ? (
                <div className="mt-3 flex items-center gap-3">
                  <div className="h-16 w-24 overflow-hidden rounded-lg border border-[var(--ds-border)]">
                    {option.primaryImageKey ? (
                      <img src={catalogMediaUrl(option.primaryImageKey)} alt={option.productName} className="h-full w-full object-contain p-1" />
                    ) : (
                      <ProductImage product={{ slug: option.productSlug, sku: option.sku, categorySlug: option.categorySlug, name: option.productName }} alt={option.productName} compact />
                    )}
                  </div>
                  <div className="text-xs text-[var(--ds-text-muted)]" dir="ltr">
                    <p>{option.sku}</p>
                    <p>{formatCatalogPrice(option.price)} {currencyLabel(locale)}</p>
                  </div>
                </div>
              ) : null}
              </div>
            </article>
          ))}
          </div>
          <p className="inline-note">{messages.pcBuildNote}</p>
        </div>

        <aside className="builder-summary glass-panel">
          <span className="eyebrow">YOUR CONFIGURATION</span>
          <div className="summary-device"><div className="summary-device-glow" /><div className="summary-case"><i /><i /><i /></div></div>
          <h3>{messages.pcBuildSummary}</h3>
          <p className="builder-total" dir="ltr">
            {formatCatalogPrice(compatibility?.totalPrice ?? chosen.reduce((sum, item) => sum + (item.option?.price ?? 0), 0))}
            <small>{currencyLabel(locale)}</small>
          </p>
          <div className="summary-progress"><span style={{ width: `${chosen.length ? Math.round((chosen.filter((item) => item.option).length / chosen.length) * 100) : 0}%` }} /></div>
          <div className="summary-count"><b>{chosen.filter((item) => item.option).length} / {chosen.length}</b><small>{statusText}</small></div>
          {compatibility?.messages.filter((message) => message.level !== "Ok").map((message) => (
            <p key={`${message.code}-${message.slotKey}`} className="inline-note">
              {message.slotKey}{message.otherSlotKey ? ` / ${message.otherSlotKey}` : ""}: {message.code}
            </p>
          ))}
          <div className="product-page-actions">
            <button type="button" className="btn-primary" onClick={save}>{messages.pcBuildSave}</button>
            <button type="button" className="btn-outline" onClick={() => void share()}>{messages.pcBuildShare}</button>
          </div>
          {notice ? <Message tone="success">{notice}</Message> : null}
        </aside>
      </div>
    </>
  );
}
