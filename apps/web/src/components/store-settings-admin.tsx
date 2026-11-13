"use client";

import { useEffect, useState } from "react";
import {
  getAdminStoreSettings,
  updateAdminStoreSettings,
} from "@/lib/api-client";
import { apiErrorMessage } from "@/lib/form-errors";
import { useLocale } from "@/lib/i18n";
import { useSession } from "@/components/session-provider";
import { LoadingCard, Message, PageIntro, PrimaryButton, TextField } from "@/components/ui";

type Form = {
  whatsAppNumber: string;
  whatsAppMessage: string;
  facebookUrl: string;
  telegramUrl: string;
  tikTokUrl: string;
  youTubeUrl: string;
  instagramUrl: string;
  pickupAddress: string;
  pickupHours: string;
  pickupInstructions: string;
  warrantySummary: string;
  serviceSummary: string;
  consultationSummary: string;
};

const emptyForm: Form = {
  whatsAppNumber: "",
  whatsAppMessage: "",
  facebookUrl: "",
  telegramUrl: "",
  tikTokUrl: "",
  youTubeUrl: "",
  instagramUrl: "",
  pickupAddress: "",
  pickupHours: "",
  pickupInstructions: "",
  warrantySummary: "",
  serviceSummary: "",
  consultationSummary: "",
};

export function StoreSettingsAdmin() {
  const { messages } = useLocale();
  const { status, user } = useSession();
  const mayManage = user?.permissions.includes("System.StoreSettings") ?? false;
  const [form, setForm] = useState<Form>(emptyForm);
  const [rowVersion, setRowVersion] = useState<number | null>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);

  useEffect(() => {
    if (status !== "authenticated" || !mayManage) return;
    let active = true;
    setLoading(true);
    getAdminStoreSettings()
      .then((settings) => {
        if (!active) return;
        setForm({
          whatsAppNumber: settings.whatsAppNumber ?? "",
          whatsAppMessage: settings.whatsAppMessage ?? "",
          facebookUrl: settings.facebookUrl ?? "",
          telegramUrl: settings.telegramUrl ?? "",
          tikTokUrl: settings.tikTokUrl ?? "",
          youTubeUrl: settings.youTubeUrl ?? "",
          instagramUrl: settings.instagramUrl ?? "",
          pickupAddress: settings.pickupAddress ?? "",
          pickupHours: settings.pickupHours ?? "",
          pickupInstructions: settings.pickupInstructions ?? "",
          warrantySummary: settings.warrantySummary ?? "",
          serviceSummary: settings.serviceSummary ?? "",
          consultationSummary: settings.consultationSummary ?? "",
        });
        setRowVersion(settings.rowVersion);
      })
      .catch((requestError) => {
        if (active) setError(apiErrorMessage(requestError, messages));
      })
      .finally(() => {
        if (active) setLoading(false);
      });
    return () => {
      active = false;
    };
  }, [mayManage, messages, status]);

  function update<K extends keyof Form>(key: K, value: Form[K]) {
    setForm((current) => ({ ...current, [key]: value }));
  }

  async function save() {
    if (rowVersion === null) {
      setError(messages.storeSettingsStale);
      return;
    }
    setSaving(true);
    setError(null);
    setNotice(null);
    try {
      const updated = await updateAdminStoreSettings({
        ...form,
        whatsAppNumber: form.whatsAppNumber.trim() || null,
        whatsAppMessage: form.whatsAppMessage.trim() || null,
        facebookUrl: form.facebookUrl.trim() || null,
        telegramUrl: form.telegramUrl.trim() || null,
        tikTokUrl: form.tikTokUrl.trim() || null,
        youTubeUrl: form.youTubeUrl.trim() || null,
        instagramUrl: form.instagramUrl.trim() || null,
        pickupAddress: form.pickupAddress.trim() || null,
        pickupHours: form.pickupHours.trim() || null,
        pickupInstructions: form.pickupInstructions.trim() || null,
        warrantySummary: form.warrantySummary.trim() || null,
        serviceSummary: form.serviceSummary.trim() || null,
        consultationSummary: form.consultationSummary.trim() || null,
        rowVersion,
      });
      setRowVersion(updated.rowVersion);
      setNotice(messages.storeSettingsSaved);
    } catch (requestError) {
      setError(apiErrorMessage(requestError, messages));
    } finally {
      setSaving(false);
    }
  }

  if (status === "loading") return <LoadingCard />;
  if (!mayManage) {
    return (
      <main className="mx-auto w-full max-w-5xl px-5 py-10">
        <Message tone="error">{messages.adminAccessDenied}</Message>
      </main>
    );
  }

  return (
    <main className="rigx-identity-admin mx-auto w-full max-w-5xl px-4 py-8 sm:px-6">
      <PageIntro eyebrow={messages.adminGroupStorefront} title={messages.storeSettingsTitle} lead={messages.storeSettingsLead} />
      {notice ? <Message tone="success">{notice}</Message> : null}
      {error ? <Message tone="error">{error}</Message> : null}
      {loading ? <LoadingCard /> : (
        <div className="grid gap-5">
          <section className="rigx-admin-card">
            <div className="rigx-admin-card-head"><h2>{messages.storeSettingsWhatsApp}</h2></div>
            <div className="grid gap-4 p-4 sm:grid-cols-2">
              <TextField
                label={messages.storeSettingsWhatsApp}
                value={form.whatsAppNumber}
                onChange={(event) => update("whatsAppNumber", event.target.value)}
                hint={messages.storeSettingsWhatsAppHint}
                dir="ltr"
              />
              <TextField
                label={messages.storeSettingsWhatsAppMessage}
                value={form.whatsAppMessage}
                onChange={(event) => update("whatsAppMessage", event.target.value)}
              />
            </div>
          </section>

          <section className="rigx-admin-card">
            <div className="rigx-admin-card-head"><h2>{messages.storeSettingsSocial}</h2></div>
            <div className="grid gap-4 p-4 sm:grid-cols-2">
              <TextField label={messages.facebook} value={form.facebookUrl} onChange={(event) => update("facebookUrl", event.target.value)} dir="ltr" />
              <TextField label={messages.telegram} value={form.telegramUrl} onChange={(event) => update("telegramUrl", event.target.value)} dir="ltr" />
              <TextField label={messages.tiktok} value={form.tikTokUrl} onChange={(event) => update("tikTokUrl", event.target.value)} dir="ltr" />
              <TextField label={messages.youtube} value={form.youTubeUrl} onChange={(event) => update("youTubeUrl", event.target.value)} dir="ltr" />
              <TextField label={messages.instagram} value={form.instagramUrl} onChange={(event) => update("instagramUrl", event.target.value)} dir="ltr" />
            </div>
          </section>

          <section className="rigx-admin-card">
            <div className="rigx-admin-card-head"><h2>{messages.storeSettingsPickup}</h2></div>
            <div className="grid gap-4 p-4 sm:grid-cols-2">
              <TextField label={messages.storeSettingsPickupAddress} value={form.pickupAddress} onChange={(event) => update("pickupAddress", event.target.value)} />
              <TextField label={messages.storeSettingsPickupHours} value={form.pickupHours} onChange={(event) => update("pickupHours", event.target.value)} />
              <TextField label={messages.storeSettingsPickupInstructions} value={form.pickupInstructions} onChange={(event) => update("pickupInstructions", event.target.value)} />
            </div>
          </section>

          <section className="rigx-admin-card">
            <div className="rigx-admin-card-head"><h2>{messages.storeSettingsTrust}</h2></div>
            <div className="grid gap-4 p-4 sm:grid-cols-3">
              <TextField label={messages.storeSettingsWarranty} value={form.warrantySummary} onChange={(event) => update("warrantySummary", event.target.value)} />
              <TextField label={messages.storeSettingsService} value={form.serviceSummary} onChange={(event) => update("serviceSummary", event.target.value)} />
              <TextField label={messages.storeSettingsConsultation} value={form.consultationSummary} onChange={(event) => update("consultationSummary", event.target.value)} />
            </div>
          </section>

          <div>
            <PrimaryButton type="button" onClick={() => void save()} pending={saving}>
              {messages.adminSaveChanges}
            </PrimaryButton>
          </div>
        </div>
      )}
    </main>
  );
}
