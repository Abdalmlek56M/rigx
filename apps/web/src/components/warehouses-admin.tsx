"use client";

import { useEffect, useState } from "react";
import {
  createWarehouse,
  getWarehouses,
  updateWarehouse,
  type Warehouse,
  type WarehousePage,
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

const emptyPage: WarehousePage = { items: [], page: 1, pageSize: 25, totalCount: 0 };
const emptyForm = { code: "", name: "", address: "", contactPhone: "", managerName: "", notes: "", isActive: true };

export function WarehousesAdmin() {
  const { messages } = useLocale();
  const { status, user } = useSession();
  const mayView = user?.permissions.includes("Inventory.View") ?? false;
  const mayManage = user?.permissions.includes("Inventory.ManageWarehouses") ?? false;

  const [page, setPage] = useState(1);
  const [search, setSearch] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [activeFilter, setActiveFilter] = useState("all");
  const [data, setData] = useState<WarehousePage>(emptyPage);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const [editing, setEditing] = useState<Warehouse | null>(null);
  const [creating, setCreating] = useState(false);
  const [form, setForm] = useState(emptyForm);
  const [rowVersion, setRowVersion] = useState<number | null>(null);
  const [saving, setSaving] = useState(false);

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
    getWarehouses({
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

  async function reload() {
    const next = await getWarehouses({
      search: debouncedSearch || undefined,
      isActive: activeFilter === "all" ? undefined : activeFilter === "active",
      page,
      pageSize: 25,
    });
    setData(next);
  }

  function openCreate() {
    setCreating(true);
    setEditing(null);
    setForm(emptyForm);
    setRowVersion(null);
    setError(null);
    setNotice(null);
  }

  function openEdit(warehouse: Warehouse) {
    setCreating(false);
    setEditing(warehouse);
    setForm({
      code: warehouse.code,
      name: warehouse.name,
      address: warehouse.address ?? "",
      contactPhone: warehouse.contactPhone ?? "",
      managerName: warehouse.managerName ?? "",
      notes: warehouse.notes ?? "",
      isActive: warehouse.isActive,
    });
    setRowVersion(warehouse.rowVersion);
    setError(null);
    setNotice(null);
  }

  async function save() {
    setSaving(true);
    setError(null);
    setNotice(null);
    const common = {
      code: form.code,
      name: form.name,
      address: form.address.trim() || null,
      contactPhone: form.contactPhone.trim() || null,
      managerName: form.managerName.trim() || null,
      notes: form.notes.trim() || null,
      isActive: form.isActive,
    };
    try {
      if (creating) {
        await createWarehouse(common);
        setNotice(messages.warehouseCreated);
      } else if (editing) {
        if (rowVersion === null) {
          setError(messages.warehouseStale);
          return;
        }
        await updateWarehouse(editing.id, { ...common, rowVersion });
        setNotice(messages.warehouseUpdated);
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
          <PageIntro eyebrow={messages.adminGroupInventory} title={messages.warehouseTitle} lead={messages.warehouseLead} />
        </div>
        {mayManage ? <PrimaryButton type="button" onClick={openCreate}>{messages.warehouseNew}</PrimaryButton> : null}
      </div>

      {notice ? <Message tone="success">{notice}</Message> : null}
      {error ? <Message tone="error">{error}</Message> : null}

      <section className="rigx-admin-card mt-5">
        <div className="rigx-admin-card-head">
          <div>
            <h2>{messages.warehouseTitle}</h2>
            <p>{messages.adminIdentityTotal}: {data.totalCount}</p>
          </div>
          <div className="flex flex-wrap items-center gap-2">
            <label className="sr-only" htmlFor="warehouse-search">{messages.warehouseSearch}</label>
            <input
              id="warehouse-search"
              type="search"
              value={search}
              onChange={(event) => setSearch(event.target.value)}
              placeholder={messages.warehouseSearch}
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
          <p className="p-8 text-center text-sm text-[var(--ds-text-muted)]">{messages.warehouseEmpty}</p>
        ) : (
          <>
            <div className="grid gap-3 p-4 md:hidden">
              {data.items.map((warehouse) => (
                <article key={warehouse.id} className="rounded-xl border border-[var(--ds-border)] bg-[var(--ds-surface-muted)] p-3">
                  <div className="flex items-start justify-between gap-2">
                    <div className="min-w-0">
                      <p className="font-bold text-[var(--ds-text)]">{warehouse.name}</p>
                      <p className="font-mono text-xs text-[var(--ds-accent)]" dir="ltr">{warehouse.code}</p>
                    </div>
                    <span className={`rigx-admin-status ${warehouse.isActive ? "is-published" : "is-draft"}`}>
                      {warehouse.isActive ? messages.adminActive : messages.adminInactive}
                    </span>
                  </div>
                  <p className="mt-2 text-xs text-[var(--ds-text-muted)]">{warehouse.address ?? "—"}</p>
                  {mayManage ? (
                    <div className="mt-3">
                      <SecondaryButton type="button" onClick={() => openEdit(warehouse)}>{messages.warehouseEdit}</SecondaryButton>
                    </div>
                  ) : null}
                </article>
              ))}
            </div>
            <div className="hidden overflow-x-auto md:block">
              <table className="w-full min-w-[40rem] text-sm">
                <caption className="sr-only">{messages.warehouseTitle}</caption>
                <thead className="bg-[var(--ds-surface-muted)] text-[var(--ds-text-muted)]">
                  <tr>
                    <th className="p-4 text-start">{messages.warehouseCode}</th>
                    <th className="p-4 text-start">{messages.warehouseName}</th>
                    <th className="p-4 text-start">{messages.warehouseAddress}</th>
                    <th className="p-4 text-start">{messages.adminIdentityStatus}</th>
                    <th className="p-4 text-start">{messages.adminActions}</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-[var(--ds-border)]">
                  {data.items.map((warehouse) => (
                    <tr key={warehouse.id} className="transition hover:bg-[var(--ds-surface-muted)]">
                      <td className="p-4 font-mono text-xs font-bold text-[var(--ds-accent)]" dir="ltr">{warehouse.code}</td>
                      <td className="p-4 font-bold text-[var(--ds-text)]">{warehouse.name}</td>
                      <td className="p-4 text-[var(--ds-text-muted)]">{warehouse.address ?? "—"}</td>
                      <td className="p-4">
                        <span className={`rigx-admin-status ${warehouse.isActive ? "is-published" : "is-draft"}`}>
                          {warehouse.isActive ? messages.adminActive : messages.adminInactive}
                        </span>
                      </td>
                      <td className="p-4">
                        {mayManage ? <SecondaryButton type="button" onClick={() => openEdit(warehouse)}>{messages.warehouseEdit}</SecondaryButton> : null}
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
            <SecondaryButton type="button" disabled={page <= 1} onClick={() => setPage((value) => Math.max(1, value - 1))}>{messages.adminPrevious}</SecondaryButton>
            <SecondaryButton type="button" disabled={page >= pageCount} onClick={() => setPage((value) => Math.min(pageCount, value + 1))}>{messages.adminNext}</SecondaryButton>
          </div>
        </div>
      </section>

      <SideDrawer
        open={drawerOpen}
        title={creating ? messages.warehouseNew : messages.warehouseEdit}
        closeLabel={messages.adminCancel}
        onClose={() => {
          if (saving) return;
          setCreating(false);
          setEditing(null);
        }}
      >
        <div className="grid gap-4">
          <TextField
            label={messages.warehouseCode}
            value={form.code}
            onChange={(event) => setForm((current) => ({ ...current, code: event.target.value }))}
            dir="ltr"
            required
          />
          <TextField
            label={messages.warehouseName}
            value={form.name}
            onChange={(event) => setForm((current) => ({ ...current, name: event.target.value }))}
            required
          />
          <TextField
            label={messages.warehouseAddress}
            value={form.address}
            onChange={(event) => setForm((current) => ({ ...current, address: event.target.value }))}
          />
          <TextField
            label={messages.warehouseContactPhone}
            value={form.contactPhone}
            onChange={(event) => setForm((current) => ({ ...current, contactPhone: event.target.value }))}
            dir="ltr"
            inputMode="tel"
          />
          <TextField
            label={messages.warehouseManager}
            value={form.managerName}
            onChange={(event) => setForm((current) => ({ ...current, managerName: event.target.value }))}
          />
          <TextField
            label={messages.warehouseNotes}
            value={form.notes}
            onChange={(event) => setForm((current) => ({ ...current, notes: event.target.value }))}
          />
          <label className="flex min-h-11 items-center gap-2 text-sm font-semibold text-[var(--ds-text)]">
            <input type="checkbox" checked={form.isActive} onChange={(event) => setForm((current) => ({ ...current, isActive: event.target.checked }))} className="h-4 w-4" />
            {messages.warehouseActive}
          </label>
          <div className="flex flex-wrap gap-2">
            <PrimaryButton type="button" onClick={() => void save()} pending={saving} disabled={!mayManage}>
              {creating ? messages.warehouseCreate : messages.adminSaveChanges}
            </PrimaryButton>
            <SecondaryButton type="button" onClick={() => { setCreating(false); setEditing(null); }} disabled={saving}>
              {messages.adminCancel}
            </SecondaryButton>
          </div>
        </div>
      </SideDrawer>
    </main>
  );
}
