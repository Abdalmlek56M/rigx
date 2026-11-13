"use client";

import { useState } from "react";
import { Icon } from "@/components/icons";
import { useLocale } from "@/lib/i18n";
import {
  configuredWhatsAppNumber,
  productCanonicalUrl,
  whatsappProductInquiryUrl,
} from "@/lib/storefront";

type ProductActionProps = {
  slug: string;
  name: string;
  sku: string;
  compact?: boolean;
};

export function ProductShareButton({ slug, name, compact = false }: ProductActionProps) {
  const { messages } = useLocale();
  const [open, setOpen] = useState(false);
  const [status, setStatus] = useState<string | null>(null);
  const origin = typeof window === "undefined" ? undefined : window.location.origin;
  const url = productCanonicalUrl(slug, origin);
  const encodedUrl = encodeURIComponent(url);
  const encodedText = encodeURIComponent(`${name} | RIGX`);

  async function copyLink() {
    try {
      await navigator.clipboard.writeText(url);
      setStatus(messages.shareCopied);
    } catch {
      setStatus(messages.shareFailed);
    }
  }

  return (
    <>
      <button
        type="button"
        onClick={() => {
          setStatus(null);
          setOpen(true);
        }}
        aria-label={messages.shareProduct}
        title={messages.shareProduct}
        className="inline-flex min-h-10 items-center justify-center gap-2 rounded-xl border border-[var(--ds-border)] bg-[var(--ds-surface)] px-3 text-sm font-semibold text-[var(--ds-text)] transition hover:border-[var(--ds-accent)] hover:bg-[var(--ds-surface-muted)] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--ds-accent)]"
      >
        <Icon name="share" className="h-4 w-4" />
        {compact ? null : <span>{messages.shareProduct}</span>}
      </button>
      {open ? (
        <div className="fixed inset-0 z-[80] grid place-items-center px-4" role="dialog" aria-modal="true" aria-labelledby="rigx-share-title">
          <button
            type="button"
            aria-label={messages.shareClose}
            className="absolute inset-0 bg-slate-950/65 backdrop-blur-sm"
            onClick={() => setOpen(false)}
          />
          <section className="ds-surface relative w-full max-w-md rounded-2xl p-6">
            <div className="flex items-start justify-between gap-3">
              <div>
                <h2 id="rigx-share-title" className="text-xl font-black text-[var(--ds-text)]">{messages.shareDialogTitle}</h2>
                <p className="mt-1 text-sm text-[var(--ds-text-muted)]" dir="auto">{name}</p>
              </div>
              <button type="button" onClick={() => setOpen(false)} aria-label={messages.shareClose} className="rounded-lg p-2 text-[var(--ds-text-muted)] hover:bg-[var(--ds-surface-muted)]">
                <Icon name="close" className="h-4 w-4" />
              </button>
            </div>
            <p className="mt-3 break-all rounded-xl bg-[var(--ds-surface-muted)] px-3 py-2 text-xs text-[var(--ds-text-muted)]" dir="ltr">{url}</p>
            <div className="mt-4 grid gap-2">
              <button type="button" onClick={() => void copyLink()} className="rigx-button rigx-button-primary justify-center">{messages.shareCopy}</button>
              <a href={`https://wa.me/?text=${encodedText}%20${encodedUrl}`} target="_blank" rel="noopener noreferrer" className="rigx-button rigx-button-secondary justify-center">{messages.shareOnWhatsApp}</a>
              <a href={`https://t.me/share/url?url=${encodedUrl}&text=${encodedText}`} target="_blank" rel="noopener noreferrer" className="rigx-button rigx-button-secondary justify-center">{messages.shareOnTelegram}</a>
              <a href={`https://www.facebook.com/sharer/sharer.php?u=${encodedUrl}`} target="_blank" rel="noopener noreferrer" className="rigx-button rigx-button-secondary justify-center">{messages.shareOnFacebook}</a>
            </div>
            {status ? <p role="status" className="mt-3 text-sm font-semibold text-[var(--ds-text-muted)]">{status}</p> : null}
          </section>
        </div>
      ) : null}
    </>
  );
}

export function WhatsAppProductButton({ slug, name, sku, compact = false }: ProductActionProps) {
  const { messages, locale } = useLocale();
  const phone = configuredWhatsAppNumber();
  if (!phone) return null;
  const canonicalUrl = productCanonicalUrl(slug, typeof window === "undefined" ? undefined : window.location.origin);
  const href = whatsappProductInquiryUrl({ phone, productName: name, sku, canonicalUrl, locale });

  return (
    <a
      href={href}
      target="_blank"
      rel="noopener noreferrer"
      aria-label={messages.whatsappInquiry}
      title={messages.whatsappInquiry}
      className="inline-flex min-h-10 items-center justify-center gap-2 rounded-xl bg-[var(--ds-whatsapp)] px-3 text-sm font-bold text-white transition hover:brightness-110 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--ds-whatsapp)]"
    >
      <Icon name="whatsapp" className="h-4 w-4" />
      {compact ? null : <span>{messages.whatsappInquiry}</span>}
    </a>
  );
}
