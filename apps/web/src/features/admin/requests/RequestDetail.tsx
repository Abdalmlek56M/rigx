"use client";

import { useCallback } from "react";
import { useLocale, useTranslations } from "next-intl";
import { formatDateTime, formatEgp } from "@/lib/format";
import { useLoad } from "@/lib/hooks/use-load";
import { usePermissions } from "../use-permissions";
import { useSubmit } from "../kit/use-submit";
import { RequestActions } from "./RequestActions";
import { RequestNoteForm } from "./RequestNoteForm";
import { RequestOfferForm } from "./RequestOfferForm";
import { RequestStatusBadge } from "./RequestBadges";
import { canMarkBackInStockSent, requestTitle, requestTrail, requestTypeKey, type TrailStep } from "./request-labels";
import { fetchRequest, fetchWhatsAppReply, markBackInStockSent, requestPhotoUrl, type RequestDetail as Detail } from "./requests-api";

function Field({ label, value, mono = false }: { label: string; value: string | null; mono?: boolean }) {
  if (!value) return null;
  return (
    <div className="flex flex-col gap-0.5">
      <dt className="text-xs text-ink-muted">{label}</dt>
      <dd dir={mono ? "ltr" : "auto"} className={`m-0 text-sm ${mono ? "font-mono" : "font-bold"}`}>{value}</dd>
    </div>
  );
}

function Trail({ steps, current }: { steps: readonly TrailStep[]; current: number }) {
  const t = useTranslations("admin.requests.trail");
  return (
    <ol aria-label={t("label")} className="m-0 flex list-none flex-wrap gap-1.5 p-0">
      {steps.map((step, index) => (
        <li
          key={step}
          aria-current={index === current ? "step" : undefined}
          className={`min-w-20 flex-1 rounded-control px-1 py-2 text-center text-xs font-bold ${index === current ? "bg-carbon text-on-carbon" : index < current ? "bg-orange/15 text-orange-text" : "bg-paper text-ink-muted"}`}
        >
          {t(step)}
        </li>
      ))}
    </ol>
  );
}

function BackInStock({ request, onSaved }: { request: Detail; onSaved: () => void }) {
  const t = useTranslations("admin.requests.backInStock");
  const locale = useLocale();
  const submit = useSubmit();
  if (!canMarkBackInStockSent(request)) return null;
  return (
    <section className="flex flex-col gap-2 rounded-control border border-line bg-paper p-3">
      <h3 className="m-0 text-sm font-bold">{t("title")}</h3>
      <p className="m-0 text-sm">
        {t("available", { code: request.availableItemCode ?? "", date: request.availableAtUtc ? formatDateTime(request.availableAtUtc, locale) : "" })}
      </p>
      <button
        type="button" disabled={submit.busy}
        onClick={() => void submit.run(async () => {
          await markBackInStockSent(request.id);
          onSaved();
        }, t("marked"))}
        className="h-11 self-start rounded-control bg-carbon px-4 text-sm font-bold text-on-carbon disabled:opacity-50"
      >
        {t("markSent")}
      </button>
      {submit.dialog}
    </section>
  );
}

/** The right-hand panel of the inbox: one request, its photos, offer, decision and follow-up. */
export function RequestDetail({ id, onClose, onChanged }: { id: string; onClose: () => void; onChanged: () => void }) {
  const t = useTranslations("admin.requests");
  const locale = useLocale();
  const { has } = usePermissions();
  const load = useCallback(
    () => Promise.all([fetchRequest(id), fetchWhatsAppReply(id)]).then(([request, whatsApp]) => ({ request, whatsApp })),
    [id]
  );
  const { state, reload } = useLoad(load);
  const canManage = has("Requests.Manage");
  const canConvert = canManage && has("Purchasing.CreateOrder");
  const saved = () => {
    reload();
    onChanged();
  };

  if (state.status === "loading") {
    return <div role="status" aria-label={t("loading")} className="h-64 animate-pulse rounded-card border border-line bg-surface" />;
  }
  if (state.status === "error") {
    return (
      <div role="alert" className="flex flex-col items-start gap-3 rounded-card border border-line bg-surface p-5">
        <p className="m-0 font-bold">{t("detail.failed")}</p>
        <div className="flex flex-wrap gap-2">
          <button type="button" onClick={reload} className="h-11 rounded-control bg-carbon px-5 font-bold text-on-carbon">{t("detail.retry")}</button>
          <button type="button" onClick={onClose} className="h-11 rounded-control border border-input-line bg-surface px-5 font-bold">{t("close")}</button>
        </div>
      </div>
    );
  }

  const { request, whatsApp } = state.data;
  const title = requestTitle(request);
  const trail = requestTrail(request.type, request.status);
  const typeLabel = requestTypeKey(request.type);

  return (
    <section className="flex min-w-0 flex-col gap-4 rounded-card border border-line bg-surface p-5">
      <header className="flex flex-wrap items-start justify-between gap-2">
        <div className="flex min-w-0 flex-wrap items-center gap-2">
          <h2 className="m-0 text-[17px] font-bold">{t("detail.heading", { type: t(`types.${typeLabel}`) })}</h2>
          <span dir="ltr" className="font-mono text-[13px] text-orange-text">{request.code}</span>
          <RequestStatusBadge status={request.status} />
        </div>
        <button type="button" onClick={onClose} aria-label={t("close")} className="h-11 w-11 rounded-control text-xl leading-none text-ink-muted hover:bg-line">×</button>
      </header>

      {trail ? <Trail steps={trail.steps} current={trail.current} /> : null}

      <dl className="m-0 grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
        <Field label={t("detail.request")} value={title} />
        <Field label={t("detail.itemCode")} value={request.itemCode} mono />
        <Field label={t("detail.ownedFor")} value={request.ownedFor} />
        <Field label={t("detail.partType")} value={request.partType} />
        <Field label={t("detail.service")} value={request.serviceKind} />
        <Field label={t("detail.preferredDate")} value={request.preferredDate} mono />
        <Field label={t("detail.customer")} value={`${request.name} · ${request.phone}`} />
        <Field label={t("detail.email")} value={request.email} mono />
        <Field label={t("detail.governorate")} value={request.governorate} />
        <Field label={t("detail.source")} value={request.source} />
        <Field label={t("detail.created")} value={formatDateTime(request.createdAtUtc, locale)} />
        <Field label={t("detail.offer")} value={request.offerAmount !== null ? formatEgp(request.offerAmount, locale) : null} />
      </dl>

      {request.message ? (
        <div className="flex flex-col gap-1.5 border-t border-line pt-4">
          <h3 className="m-0 text-[15px] font-bold">{t("detail.message")}</h3>
          <p className="m-0 whitespace-pre-wrap rounded-control bg-paper p-3 text-sm text-ink-muted">{request.message}</p>
        </div>
      ) : null}

      {request.photoCount > 0 ? (
        <div className="flex flex-col gap-2">
          <h3 className="m-0 text-[15px] font-bold">{t("photos.title")}</h3>
          <div className="grid grid-cols-2 gap-2 sm:grid-cols-4">
            {Array.from({ length: Math.min(request.photoCount, 8) }, (_, index) => (
              // eslint-disable-next-line @next/next/no-img-element -- private admin media needs the session cookie
              <img
                key={index} src={requestPhotoUrl(request.id, index)} alt={t("photos.alt", { name: title, index: index + 1 })}
                loading="lazy" className="aspect-[4/3] w-full rounded-control border border-line object-cover"
              />
            ))}
          </div>
        </div>
      ) : null}

      <RequestOfferForm key={`offer-${request.rowVersion}`} request={request} canManage={canManage} onSaved={saved} onStale={reload} />
      <RequestNoteForm key={`note-${request.rowVersion}`} request={request} canManage={canManage} onSaved={saved} onStale={reload} />
      <RequestActions request={request} canManage={canManage} canConvert={canConvert} onSaved={saved} onStale={reload} />
      <BackInStock request={request} onSaved={saved} />

      <section className="flex flex-col gap-2 border-t border-line pt-4">
        <h3 className="m-0 text-[15px] font-bold">{t("whatsapp.title")}</h3>
        <p className="m-0 whitespace-pre-wrap rounded-control bg-paper p-3 text-sm text-ink-muted">{whatsApp.text}</p>
        <p className="m-0 text-xs text-ink-muted">{t("whatsapp.note")}</p>
        <a href={whatsApp.url} target="_blank" rel="noreferrer" className="inline-flex h-11 items-center self-start rounded-control bg-orange px-4 text-sm font-bold text-on-orange">
          {t("whatsapp.open")}
        </a>
      </section>
    </section>
  );
}
