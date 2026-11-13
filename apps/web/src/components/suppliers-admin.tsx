"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import {
  createSupplier,
  getSupplierFinancials,
  getSuppliers,
  updateSupplier,
  updateSupplierFinancials,
  type Supplier,
  type SupplierFinancials,
  type SupplierPage,
} from "@/lib/api-client";
import { apiErrorMessage } from "@/lib/form-errors";
import { useLocale } from "@/lib/i18n";
import { useSession } from "@/components/session-provider";
import {
  LoadingCard,
  Message,
  PageIntro,
  PrimaryButton,
  SecondaryButton,
  SideDrawer,
  TextField,
} from "@/components/ui";

const emptyPage: SupplierPage = { items: [], page: 1, pageSize: 25, totalCount: 0 };

type SupplierForm = {
  companyName: string;
  phoneNumber: string;
  email: string;
  taxIdNumber: string;
  commercialRegistration: string;
  contactPersonName: string;
  contactPersonPhone: string;
  paymentTermsDays: number;
  notes: string;
  isActive: boolean;
};

const emptyForm: SupplierForm = {
  companyName: "",
  phoneNumber: "",
  email: "",
  taxIdNumber: "",
  commercialRegistration: "",
  contactPersonName: "",
  contactPersonPhone: "",
  paymentTermsDays: 0,
  notes: "",
  isActive: true,
};

export function SuppliersAdmin() {
  const { messages } = useLocale();
  const { status, user } = useSession();
  const mayView = user?.permissions.includes("Purchasing.View") ?? false;
  const mayManage = user?.permissions.includes("Purchasing.ManageSuppliers") ?? false;
  const mayViewFinancials = user?.permissions.includes("Purchasing.ViewSupplierFinancials") ?? false;
  const mayManageFinancials = user?.permissions.includes("Purchasing.ManageSupplierFinancials") ?? false;

  const [page, setPage] = useState(1);
  const [search, setSearch] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [activeFilter, setActiveFilter] = useState("all");
  const [data, setData] = useState<SupplierPage>(emptyPage);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const [editing, setEditing] = useState<Supplier | null>(null);
  const [creating, setCreating] = useState(false);
  const [form, setForm] = useState<SupplierForm>(emptyForm);
  const [rowVersion, setRowVersion] = useState<number | null>(null);
  const [saving, setSaving] = useState(false);
  const [financialSupplier, setFinancialSupplier] = useState<Supplier | null>(null);
  const [financials, setFinancials] = useState<SupplierFinancials | null>(null);
  const [financialForm, setFinancialForm] = useState({ bankName: "", bankAccountNumber: "", bankIban: "" });
  const [financialLoading, setFinancialLoading] = useState(false);
  const [financialSaving, setFinancialSaving] = useState(false);
  const [financialError, setFinancialError] = useState<string | null>(null);

  useEffect(() => {
    const timer = window.setTimeout(() => {
      setDebouncedSearch(search.trim());
      setPage(1);
    }, 250);
    return () => window.clearTimeout(timer);
  }, [search]);

  useEffect(() => {
    if (status !== "authenticated" || !mayView) return;
    let active = true;
    setLoading(true);
    setError(null);
    getSuppliers({
      search: debouncedSearch || undefined,
      isActive: activeFilter === "all" ? undefined : activeFilter === "active",
      page,
      pageSize: 25,
    })
      .then((next) => {
        if (active) setData(next);
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
  }, [activeFilter, debouncedSearch, mayView, messages, page, status]);

  const pageCount = Math.max(1, Math.ceil(data.totalCount / data.pageSize));

  function openCreate() {
    setCreating(true);
    setEditing(null);
    setForm(emptyForm);
    setRowVersion(null);
    setError(null);
    setNotice(null);
  }

  function openEdit(supplier: Supplier) {
    setCreating(false);
    setEditing(supplier);
    setFinancialSupplier(null);
    setForm({
      companyName: supplier.companyName,
      phoneNumber: supplier.phoneNumber ?? "",
      email: supplier.email ?? "",
      taxIdNumber: supplier.taxIdNumber ?? "",
      commercialRegistration: supplier.commercialRegistration ?? "",
      contactPersonName: supplier.contactPersonName ?? "",
      contactPersonPhone: supplier.contactPersonPhone ?? "",
      paymentTermsDays: supplier.paymentTermsDays,
      notes: supplier.notes ?? "",
      isActive: supplier.isActive,
    });
    setRowVersion(supplier.rowVersion);
    setError(null);
    setNotice(null);
  }

  function update<K extends keyof SupplierForm>(key: K, value: SupplierForm[K]) {
    setForm((current) => ({ ...current, [key]: value }));
  }

  async function reload() {
    const next = await getSuppliers({
      search: debouncedSearch || undefined,
      isActive: activeFilter === "all" ? undefined : activeFilter === "active",
      page,
      pageSize: 25,
    });
    setData(next);
  }

  async function openFinancials(supplier: Supplier) {
    setCreating(false);
    setEditing(null);
    setFinancialSupplier(supplier);
    setFinancials(null);
    setFinancialForm({ bankName: "", bankAccountNumber: "", bankIban: "" });
    setFinancialError(null);
    setFinancialLoading(true);
    try {
      const next = await getSupplierFinancials(supplier.id);
      setFinancials(next);
      setFinancialForm({
        bankName: next.bankName ?? "",
        bankAccountNumber: next.bankAccountNumber ?? "",
        bankIban: next.bankIban ?? "",
      });
    } catch (requestError) {
      setFinancialError(apiErrorMessage(requestError, messages));
    } finally {
      setFinancialLoading(false);
    }
  }

  async function saveFinancials() {
    if (!financialSupplier || !financials) return;
    setFinancialSaving(true);
    setFinancialError(null);
    try {
      const updated = await updateSupplierFinancials(financialSupplier.id, {
        bankName: financialForm.bankName.trim() || null,
        bankAccountNumber: financialForm.bankAccountNumber.trim() || null,
        bankIban: financialForm.bankIban.trim() || null,
        rowVersion: financials.rowVersion,
      });
      setFinancials(updated);
      setNotice(messages.supplierFinancialsSaved);
      setFinancialSupplier(null);
      await reload();
    } catch (requestError) {
      setFinancialError(apiErrorMessage(requestError, messages));
    } finally {
      setFinancialSaving(false);
    }
  }

  async function save() {
    setSaving(true);
    setError(null);
    setNotice(null);
    const common = {
      companyName: form.companyName,
      phoneNumber: form.phoneNumber.trim() || null,
      email: form.email.trim() || null,
      taxIdNumber: form.taxIdNumber.trim() || null,
      commercialRegistration: form.commercialRegistration.trim() || null,
      contactPersonName: form.contactPersonName.trim() || null,
      contactPersonPhone: form.contactPersonPhone.trim() || null,
      paymentTermsDays: form.paymentTermsDays,
      notes: form.notes.trim() || null,
      isActive: form.isActive,
    };
    try {
      if (creating) {
        await createSupplier(common);
        setNotice(messages.supplierCreated);
      } else if (editing) {
        if (rowVersion === null) {
          setError(messages.supplierStaleVersion);
          return;
        }
        await updateSupplier(editing.id, { ...common, rowVersion });
        setNotice(messages.supplierUpdated);
      }
      setCreating(false);
      setEditing(null);
      await reload();
    } catch (requestError) {
      setError(apiErrorMessage(requestError, messages));
    } finally {
      setSaving(false);
    }
  }

  if (status === "loading") return <LoadingCard />;
  if (!mayView) {
    return (
      <main className="mx-auto w-full max-w-5xl px-5 py-10">
        <Message tone="error">{messages.adminAccessDenied}</Message>
      </main>
    );
  }

  const drawerOpen = creating || editing !== null;

  return (
    <main className="rigx-identity-admin mx-auto w-full max-w-6xl px-4 py-8 sm:px-6">
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div className="min-w-0 flex-1">
          <PageIntro eyebrow={messages.adminGroupPurchasing} title={messages.supplierTitle} lead={messages.supplierLead} />
        </div>
        {mayManage ? (
          <PrimaryButton type="button" onClick={openCreate}>{messages.supplierNew}</PrimaryButton>
        ) : null}
      </div>

      {notice ? <Message tone="success">{notice}</Message> : null}
      {error ? <Message tone="error">{error}</Message> : null}

      <section className="rigx-admin-card mt-5">
        <div className="rigx-admin-card-head">
          <div>
            <h2>{messages.supplierTitle}</h2>
            <p>{messages.adminIdentityTotal}: {data.totalCount}</p>
          </div>
          <div className="flex flex-wrap items-center gap-2">
            <label className="sr-only" htmlFor="supplier-search">{messages.supplierSearch}</label>
            <input
              id="supplier-search"
              type="search"
              value={search}
              onChange={(event) => setSearch(event.target.value)}
              placeholder={messages.supplierSearch}
              className="rigx-admin-input min-w-64"
            />
            <select
              value={activeFilter}
              onChange={(event) => {
                setActiveFilter(event.target.value);
                setPage(1);
              }}
              className="rigx-admin-input w-auto"
              aria-label={messages.adminStatusFilter}
            >
              <option value="all">{messages.adminIdentityAllStatuses}</option>
              <option value="active">{messages.adminIdentityActiveOnly}</option>
              <option value="inactive">{messages.adminIdentityInactiveOnly}</option>
            </select>
          </div>
        </div>

        {loading ? (
          <div className="p-6"><LoadingCard /></div>
        ) : data.items.length === 0 ? (
          <p className="p-8 text-center text-sm text-[var(--ds-text-muted)]">{messages.supplierNoResults}</p>
        ) : (
          <>
            <div className="grid gap-3 p-4 md:hidden">
              {data.items.map((supplier) => (
                <article key={supplier.id} className="rounded-xl border border-[var(--ds-border)] bg-[var(--ds-surface-muted)] p-3">
                  <div className="flex items-start justify-between gap-2">
                    <div className="min-w-0">
                      <p className="truncate font-bold text-[var(--ds-text)]">{supplier.companyName}</p>
                      <p className="text-[10px] text-[var(--ds-text-muted)]" dir="ltr">{supplier.personCode}</p>
                    </div>
                    <span className={`rigx-admin-status ${supplier.isActive ? "is-published" : "is-draft"}`}>
                      {supplier.isActive ? messages.adminActive : messages.adminInactive}
                    </span>
                  </div>
                  <dl className="mt-3 grid gap-1.5 text-xs">
                    <div className="flex justify-between gap-2">
                      <dt className="text-[var(--ds-text-muted)]">{messages.supplierContactName}</dt>
                      <dd className="text-[var(--ds-text)]">{supplier.contactPersonName ?? "—"}</dd>
                    </div>
                    <div className="flex justify-between gap-2">
                      <dt className="text-[var(--ds-text-muted)]">{messages.supplierPhone}</dt>
                      <dd className="text-[var(--ds-text)]" dir="ltr">{supplier.phoneNumber ?? supplier.email ?? "—"}</dd>
                    </div>
                    <div className="flex justify-between gap-2">
                      <dt className="text-[var(--ds-text-muted)]">{messages.supplierTaxId}</dt>
                      <dd className="font-mono text-[var(--ds-text)]" dir="ltr">{supplier.taxIdNumber ?? "—"}</dd>
                    </div>
                    <div className="flex justify-between gap-2">
                      <dt className="text-[var(--ds-text-muted)]">{messages.supplierPaymentTerms}</dt>
                      <dd className="text-[var(--ds-text)]">{supplier.paymentTermsDays} {messages.supplierPaymentTermsUnit}</dd>
                    </div>
                  </dl>
                  {mayView ? (
                    <div className="mt-3 flex flex-wrap gap-2">
                      {mayManage ? (
                        <SecondaryButton type="button" onClick={() => openEdit(supplier)}>{messages.supplierEdit}</SecondaryButton>
                      ) : null}
                      {mayViewFinancials ? (
                        <SecondaryButton type="button" onClick={() => void openFinancials(supplier)}>{messages.supplierViewFinancials}</SecondaryButton>
                      ) : null}
                      <Link
                        href={`/admin/purchasing/suppliers/${supplier.id}/products`}
                        className="inline-flex min-h-11 items-center justify-center rounded-xl border border-[var(--ds-border)] bg-[var(--ds-surface)] px-4 text-sm font-semibold text-[var(--ds-text)] transition hover:border-[var(--ds-accent)] hover:bg-[var(--ds-surface-muted)]"
                      >
                        {messages.supplierProductsTitle}
                      </Link>
                    </div>
                  ) : null}
                </article>
              ))}
            </div>
            <div className="hidden overflow-x-auto md:block">
              <table className="w-full min-w-[58rem] text-sm">
                <caption className="sr-only">{messages.supplierTitle}</caption>
                <thead className="bg-[var(--ds-surface-muted)] text-[var(--ds-text-muted)]">
                  <tr>
                    <th className="p-4 text-start">{messages.supplierCompanyName}</th>
                    <th className="p-4 text-start">{messages.supplierContactName}</th>
                    <th className="p-4 text-start">{messages.supplierPhone}</th>
                    <th className="p-4 text-start">{messages.supplierTaxId}</th>
                    <th className="p-4 text-start">{messages.supplierPaymentTerms}</th>
                    <th className="p-4 text-start">{messages.adminIdentityStatus}</th>
                    <th className="p-4 text-start">{messages.adminActions}</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-[var(--ds-border)]">
                  {data.items.map((supplier) => (
                    <tr key={supplier.id} className="transition hover:bg-[var(--ds-surface-muted)]">
                      <td className="p-4">
                        <span className="block font-bold text-[var(--ds-text)]">{supplier.companyName}</span>
                        <span className="block text-xs text-[var(--ds-text-muted)]" dir="ltr">{supplier.personCode}</span>
                      </td>
                      <td className="p-4 text-[var(--ds-text-muted)]">
                        {supplier.contactPersonName ?? "—"}
                        {supplier.contactPersonPhone ? <span className="block text-xs" dir="ltr">{supplier.contactPersonPhone}</span> : null}
                      </td>
                      <td className="p-4 text-[var(--ds-text-muted)]" dir="ltr">{supplier.phoneNumber ?? supplier.email ?? "—"}</td>
                      <td className="p-4 font-mono text-xs text-[var(--ds-text-muted)]" dir="ltr">{supplier.taxIdNumber ?? "—"}</td>
                      <td className="p-4 text-[var(--ds-text-muted)]">{supplier.paymentTermsDays} {messages.supplierPaymentTermsUnit}</td>
                      <td className="p-4">
                        <span className={`rigx-admin-status ${supplier.isActive ? "is-published" : "is-draft"}`}>
                          {supplier.isActive ? messages.adminActive : messages.adminInactive}
                        </span>
                      </td>
                      <td className="p-4">
                        <div className="flex flex-wrap gap-2">
                          {mayManage ? (
                            <SecondaryButton type="button" onClick={() => openEdit(supplier)}>{messages.supplierEdit}</SecondaryButton>
                          ) : null}
                          {mayViewFinancials ? (
                            <SecondaryButton type="button" onClick={() => void openFinancials(supplier)}>{messages.supplierViewFinancials}</SecondaryButton>
                          ) : null}
                          <Link
                            href={`/admin/purchasing/suppliers/${supplier.id}/products`}
                            className="inline-flex min-h-11 items-center justify-center rounded-xl border border-[var(--ds-border)] bg-[var(--ds-surface)] px-4 text-sm font-semibold text-[var(--ds-text)] transition hover:border-[var(--ds-accent)] hover:bg-[var(--ds-surface-muted)]"
                          >
                            {messages.supplierProductsTitle}
                          </Link>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </>
        )}

        <div className="flex flex-wrap items-center justify-between gap-3 border-t border-[var(--ds-border)] p-4">
          <p className="text-sm text-[var(--ds-text-muted)]">{messages.adminPage} {data.page} / {pageCount}</p>
          <div className="flex gap-2">
            <SecondaryButton type="button" disabled={page <= 1} onClick={() => setPage((value) => Math.max(1, value - 1))}>
              {messages.adminPrevious}
            </SecondaryButton>
            <SecondaryButton type="button" disabled={page >= pageCount} onClick={() => setPage((value) => Math.min(pageCount, value + 1))}>
              {messages.adminNext}
            </SecondaryButton>
          </div>
        </div>
      </section>

      <SideDrawer
        open={drawerOpen}
        title={creating ? messages.supplierNew : messages.supplierEdit}
        closeLabel={messages.adminCancel}
        onClose={() => {
          if (saving) return;
          setCreating(false);
          setEditing(null);
        }}
      >
        <div className="grid gap-4">
          <TextField
            label={messages.supplierCompanyName}
            value={form.companyName}
            onChange={(event) => update("companyName", event.target.value)}
            required
          />
          <div className="grid gap-4 sm:grid-cols-2">
            <TextField
              label={messages.supplierPhone}
              value={form.phoneNumber ?? ""}
              onChange={(event) => update("phoneNumber", event.target.value)}
              inputMode="tel"
              dir="ltr"
            />
            <TextField
              label={messages.supplierEmail}
              value={form.email ?? ""}
              onChange={(event) => update("email", event.target.value)}
              type="email"
              dir="ltr"
            />
          </div>
          <div className="grid gap-4 sm:grid-cols-2">
            <TextField
              label={messages.supplierTaxId}
              value={form.taxIdNumber ?? ""}
              onChange={(event) => update("taxIdNumber", event.target.value)}
              dir="ltr"
            />
            <TextField
              label={messages.supplierCommercialRegistration}
              value={form.commercialRegistration ?? ""}
              onChange={(event) => update("commercialRegistration", event.target.value)}
              dir="ltr"
            />
          </div>
          <div className="grid gap-4 sm:grid-cols-2">
            <TextField
              label={messages.supplierContactName}
              value={form.contactPersonName ?? ""}
              onChange={(event) => update("contactPersonName", event.target.value)}
            />
            <TextField
              label={messages.supplierContactPhone}
              value={form.contactPersonPhone ?? ""}
              onChange={(event) => update("contactPersonPhone", event.target.value)}
              inputMode="tel"
              dir="ltr"
            />
          </div>
          <div className="grid gap-4 sm:grid-cols-2">
            <TextField
              label={messages.supplierPaymentTerms}
              value={String(form.paymentTermsDays)}
              onChange={(event) => update("paymentTermsDays", Number(event.target.value) || 0)}
              type="number"
              min={0}
              max={3650}
              hint={messages.supplierTermsHint}
            />
          </div>
          <label className="grid gap-1.5 text-sm font-semibold text-[var(--ds-text)]">
            {messages.supplierNotes}
            <textarea
              value={form.notes ?? ""}
              onChange={(event) => update("notes", event.target.value)}
              rows={3}
              className="rigx-admin-input"
            />
          </label>
          <label className="flex min-h-11 items-center gap-2 text-sm font-semibold text-[var(--ds-text)]">
            <input type="checkbox" checked={form.isActive} onChange={(event) => update("isActive", event.target.checked)} className="h-4 w-4" />
            {messages.supplierActive}
          </label>
          <div className="flex flex-wrap gap-2">
            <PrimaryButton type="button" onClick={() => void save()} pending={saving} disabled={!mayManage}>
              {creating ? messages.supplierCreate : messages.adminSaveChanges}
            </PrimaryButton>
            <SecondaryButton type="button" onClick={() => { setCreating(false); setEditing(null); }} disabled={saving}>
              {messages.adminCancel}
            </SecondaryButton>
          </div>
        </div>
      </SideDrawer>

      <SideDrawer
        open={financialSupplier !== null}
        title={`${messages.supplierFinancials}: ${financialSupplier?.companyName ?? ""}`}
        closeLabel={messages.adminCancel}
        onClose={() => {
          if (financialSaving) return;
          setFinancialSupplier(null);
          setFinancialError(null);
        }}
      >
        <div className="grid gap-4">
          {financialLoading ? <LoadingCard /> : null}
          {financialError ? <Message tone="error">{financialError}</Message> : null}
          {financials ? (
            <>
              <div className="rounded-xl border border-[var(--ds-border)] bg-[var(--ds-surface-muted)] p-3 text-sm">
                <p className="font-bold text-[var(--ds-text)]">{financialSupplier?.companyName}</p>
                <p className="text-xs text-[var(--ds-text-muted)]" dir="ltr">{financialSupplier?.personCode}</p>
              </div>
              <TextField
                label={messages.supplierBankName}
                value={financialForm.bankName}
                onChange={(event) => setFinancialForm((current) => ({ ...current, bankName: event.target.value }))}
                disabled={!mayManageFinancials}
              />
              <TextField
                label={messages.supplierBankAccount}
                value={financialForm.bankAccountNumber}
                onChange={(event) => setFinancialForm((current) => ({ ...current, bankAccountNumber: event.target.value }))}
                dir="ltr"
                disabled={!mayManageFinancials}
              />
              <TextField
                label={messages.supplierBankIban}
                value={financialForm.bankIban}
                onChange={(event) => setFinancialForm((current) => ({ ...current, bankIban: event.target.value }))}
                dir="ltr"
                disabled={!mayManageFinancials}
              />
              <p className="text-xs leading-5 text-[var(--ds-text-muted)]">{messages.supplierBankHint}</p>
              <div className="flex flex-wrap gap-2">
                {mayManageFinancials ? (
                  <PrimaryButton type="button" onClick={() => void saveFinancials()} pending={financialSaving}>
                    {messages.adminSaveChanges}
                  </PrimaryButton>
                ) : null}
                <SecondaryButton type="button" onClick={() => setFinancialSupplier(null)} disabled={financialSaving}>
                  {messages.adminCancel}
                </SecondaryButton>
              </div>
            </>
          ) : null}
        </div>
      </SideDrawer>
    </main>
  );
}
