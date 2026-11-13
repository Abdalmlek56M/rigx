"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import {
  createCustomerAddress,
  deleteCustomerAddress,
  getCustomerAddresses,
  updateCustomerAddress,
  type CustomerAddress,
  type CustomerAddressInput,
} from "@/lib/api-client";
import { apiErrorMessage } from "@/lib/form-errors";
import { useLocale } from "@/lib/i18n";
import { useSession } from "@/components/session-provider";
import { AccountShell } from "@/components/account-shell";
import {
  ConfirmDialog,
  LoadingCard,
  Message,
  PageIntro,
  PrimaryButton,
  SecondaryButton,
  SideDrawer,
  TextField,
} from "@/components/ui";

type Form = { [K in keyof CustomerAddressInput]: CustomerAddressInput[K] extends string | null ? string : CustomerAddressInput[K] };

const emptyForm: Form = {
  label: "Home",
  recipientName: "",
  phoneNumber: "",
  countryCode: "EG",
  countryCallingCode: "+20",
  governorate: "",
  city: "",
  district: "",
  street: "",
  building: "",
  floor: "",
  apartment: "",
  landmark: "",
  postalCode: "",
  instructions: "",
  isDefault: false,
};

export function AccountAddresses() {
  const { messages } = useLocale();
  const { status, user, refresh } = useSession();
  const [addresses, setAddresses] = useState<CustomerAddress[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const [creating, setCreating] = useState(false);
  const [editing, setEditing] = useState<CustomerAddress | null>(null);
  const [form, setForm] = useState<Form>(emptyForm as Form);
  const [rowVersion, setRowVersion] = useState<number | null>(null);
  const [saving, setSaving] = useState(false);
  const [deleting, setDeleting] = useState<CustomerAddress | null>(null);
  const [deletePending, setDeletePending] = useState(false);

  async function load() {
    setLoading(true);
    setError(null);
    try {
      setAddresses(await getCustomerAddresses());
    } catch (requestError) {
      setError(apiErrorMessage(requestError, messages));
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    if (status === "authenticated") void load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [status]);

  if (status === "loading") return <LoadingCard />;
  if (status === "unavailable") {
    return (
      <main className="mx-auto w-full max-w-3xl px-5 py-10">
        <Message tone="error">{messages.accountUnavailable}</Message>
        <SecondaryButton type="button" onClick={() => void refresh()}>{messages.retry}</SecondaryButton>
      </main>
    );
  }
  if (!user) {
    return (
      <main className="mx-auto w-full max-w-3xl px-5 py-10">
        <Message tone="info">{messages.accountRedirecting}</Message>
        <Link href="/login" className="text-sm font-semibold text-[var(--ds-accent)]">{messages.navLogin}</Link>
      </main>
    );
  }

  function openCreate() {
    setCreating(true);
    setEditing(null);
    setForm(emptyForm);
    setRowVersion(null);
    setError(null);
    setNotice(null);
  }

  function openEdit(address: CustomerAddress) {
    setCreating(false);
    setEditing(address);
    setForm({
      label: address.label,
      recipientName: address.recipientName,
      phoneNumber: address.phoneNumber,
      countryCode: address.countryCode,
      countryCallingCode: address.countryCallingCode,
      governorate: address.governorate,
      city: address.city,
      district: address.district ?? "",
      street: address.street,
      building: address.building ?? "",
      floor: address.floor ?? "",
      apartment: address.apartment ?? "",
      landmark: address.landmark ?? "",
      postalCode: address.postalCode ?? "",
      instructions: address.instructions ?? "",
      isDefault: address.isDefault,
    });
    setRowVersion(address.rowVersion);
    setError(null);
    setNotice(null);
  }

  function update<K extends keyof Form>(key: K, value: Form[K]) {
    setForm((current) => ({ ...current, [key]: value }));
  }

  async function save() {
    setSaving(true);
    setError(null);
    setNotice(null);
    const input: CustomerAddressInput = {
      ...form,
      district: form.district.trim() || null,
      building: form.building.trim() || null,
      floor: form.floor.trim() || null,
      apartment: form.apartment.trim() || null,
      landmark: form.landmark.trim() || null,
      postalCode: form.postalCode.trim() || null,
      instructions: form.instructions.trim() || null,
    };
    try {
      if (creating) {
        await createCustomerAddress(input);
        setNotice(messages.addressSaved);
      } else if (editing) {
        if (rowVersion === null) {
          setError(messages.addressStale);
          return;
        }
        await updateCustomerAddress(editing.id, { ...input, rowVersion });
        setNotice(messages.addressSaved);
      }
      setCreating(false);
      setEditing(null);
      await load();
    } catch (requestError) {
      setError(apiErrorMessage(requestError, messages));
    } finally {
      setSaving(false);
    }
  }

  async function confirmDelete() {
    if (!deleting) return;
    setDeletePending(true);
    setError(null);
    try {
      await deleteCustomerAddress(deleting.id, deleting.rowVersion);
      setDeleting(null);
      setNotice(messages.addressDeleted);
      await load();
    } catch (requestError) {
      setError(apiErrorMessage(requestError, messages));
    } finally {
      setDeletePending(false);
    }
  }

  const drawerOpen = creating || editing !== null;

  return (
    <AccountShell>
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div className="min-w-0 flex-1">
          <PageIntro eyebrow={messages.accountEyebrow} title={messages.addressTitle} lead={messages.addressLead} />
        </div>
        <PrimaryButton type="button" onClick={openCreate}>{messages.addressNew}</PrimaryButton>
      </div>
      {notice ? <Message tone="success">{notice}</Message> : null}
      {error ? <Message tone="error">{error}</Message> : null}
      <div className="flex flex-wrap gap-3 text-sm font-semibold">
        <Link href="/account/profile" className="text-[var(--ds-accent)]">{messages.accountProfileTitle}</Link>
        <Link href="/account/sessions" className="text-[var(--ds-accent)]">{messages.accountSessionsTitle}</Link>
      </div>
      {loading ? <LoadingCard /> : addresses.length === 0 ? (
        <Message tone="info">{messages.addressEmpty}</Message>
      ) : (
        <section className="rigx-admin-card">
          <div className="grid gap-3 p-4">
            {addresses.map((address) => (
              <article key={address.id} className="rounded-xl border border-[var(--ds-border)] bg-[var(--ds-surface-muted)] p-4">
                <div className="flex flex-wrap items-start justify-between gap-3">
                  <div>
                    <p className="font-bold text-[var(--ds-text)]">
                      {address.label} — {address.recipientName}
                      {address.isDefault ? <span className="ms-2 rounded-full bg-[var(--ds-accent-soft)] px-2 py-0.5 text-xs text-[var(--ds-accent-strong)]">{messages.addressDefault}</span> : null}
                    </p>
                    <p className="mt-1 text-sm text-[var(--ds-text-muted)]">{address.street}, {address.district ? `${address.district}, ` : ""}{address.city}, {address.governorate}</p>
                    <p className="text-xs text-[var(--ds-text-muted)]" dir="ltr">{address.countryCallingCode} {address.phoneNumber}</p>
                  </div>
                  <div className="flex flex-wrap gap-2">
                    <SecondaryButton type="button" onClick={() => openEdit(address)}>{messages.addressEdit}</SecondaryButton>
                    <SecondaryButton type="button" onClick={() => setDeleting(address)}>{messages.addressDelete}</SecondaryButton>
                  </div>
                </div>
              </article>
            ))}
          </div>
        </section>
      )}

      <SideDrawer
        open={drawerOpen}
        title={creating ? messages.addressNew : messages.addressEdit}
        closeLabel={messages.adminCancel}
        onClose={() => {
          if (saving) return;
          setCreating(false);
          setEditing(null);
        }}
      >
        <div className="grid gap-4">
          <TextField label={messages.addressLabel} value={form.label} onChange={(event) => update("label", event.target.value)} required />
          <TextField label={messages.addressRecipient} value={form.recipientName} onChange={(event) => update("recipientName", event.target.value)} required />
          <div className="grid grid-cols-[6rem_1fr] gap-2">
            <TextField label={messages.addressCountryCode} value={form.countryCode} onChange={(event) => update("countryCode", event.target.value.toUpperCase())} dir="ltr" required />
            <TextField label={messages.addressCallingCode} value={form.countryCallingCode} onChange={(event) => update("countryCallingCode", event.target.value)} dir="ltr" required />
          </div>
          <TextField label={messages.addressPhone} value={form.phoneNumber} onChange={(event) => update("phoneNumber", event.target.value)} inputMode="tel" dir="ltr" required />
          <div className="grid gap-4 sm:grid-cols-2">
            <TextField label={messages.addressGovernorate} value={form.governorate} onChange={(event) => update("governorate", event.target.value)} required />
            <TextField label={messages.addressCity} value={form.city} onChange={(event) => update("city", event.target.value)} required />
          </div>
          <TextField label={messages.addressDistrict} value={form.district} onChange={(event) => update("district", event.target.value)} />
          <TextField label={messages.addressStreet} value={form.street} onChange={(event) => update("street", event.target.value)} required />
          <div className="grid gap-4 sm:grid-cols-3">
            <TextField label={messages.addressBuilding} value={form.building} onChange={(event) => update("building", event.target.value)} />
            <TextField label={messages.addressFloor} value={form.floor} onChange={(event) => update("floor", event.target.value)} />
            <TextField label={messages.addressApartment} value={form.apartment} onChange={(event) => update("apartment", event.target.value)} />
          </div>
          <TextField label={messages.addressLandmark} value={form.landmark} onChange={(event) => update("landmark", event.target.value)} />
          <TextField label={messages.addressPostal} value={form.postalCode} onChange={(event) => update("postalCode", event.target.value)} dir="ltr" />
          <TextField label={messages.addressInstructions} value={form.instructions} onChange={(event) => update("instructions", event.target.value)} />
          <label className="flex min-h-11 items-center gap-2 text-sm font-semibold text-[var(--ds-text)]">
            <input type="checkbox" checked={form.isDefault} onChange={(event) => update("isDefault", event.target.checked)} className="h-4 w-4" />
            {messages.addressMakeDefault}
          </label>
          <div className="flex flex-wrap gap-2">
            <PrimaryButton type="button" onClick={() => void save()} pending={saving}>{messages.adminSaveChanges}</PrimaryButton>
            <SecondaryButton type="button" onClick={() => { setCreating(false); setEditing(null); }} disabled={saving}>{messages.adminCancel}</SecondaryButton>
          </div>
        </div>
      </SideDrawer>

      <ConfirmDialog
        open={deleting !== null}
        title={messages.addressDelete}
        description={messages.addressDeleteConfirm}
        confirmLabel={messages.addressDelete}
        cancelLabel={messages.adminCancel}
        pending={deletePending}
        onConfirm={() => void confirmDelete()}
        onCancel={() => setDeleting(null)}
      />
    </AccountShell>
  );
}

