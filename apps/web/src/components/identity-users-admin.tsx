"use client";

import { useEffect, useState } from "react";
import {
  assignAdminIdentityRoles,
  createAdminIdentityUser,
  getAdminIdentityRoles,
  getAdminIdentityUsers,
  requestAdminIdentityPasswordReset,
  updateAdminIdentityUser,
  type AdminIdentityRole,
  type AdminIdentityUser,
  type AdminIdentityUserPage,
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

const emptyPage: AdminIdentityUserPage = { items: [], page: 1, pageSize: 25, totalCount: 0 };

export function IdentityUsersAdmin() {
  const { messages } = useLocale();
  const { status, user } = useSession();
  const mayManage = user?.permissions.includes("System.ManageUsers") ?? false;

  const [page, setPage] = useState(1);
  const [search, setSearch] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [activeFilter, setActiveFilter] = useState("all");
  const [data, setData] = useState<AdminIdentityUserPage>(emptyPage);
  const [roles, setRoles] = useState<AdminIdentityRole[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const [editing, setEditing] = useState<AdminIdentityUser | null>(null);
  const [creating, setCreating] = useState(false);
  const [newUsername, setNewUsername] = useState("");
  const [newEmail, setNewEmail] = useState("");
  const [newFullName, setNewFullName] = useState("");
  const [newPhoneNumber, setNewPhoneNumber] = useState("");
  const [newRoleIds, setNewRoleIds] = useState<string[]>([]);
  const [resetSending, setResetSending] = useState(false);
  const [fullName, setFullName] = useState("");
  const [phoneNumber, setPhoneNumber] = useState("");
  const [isActive, setIsActive] = useState(true);
  const [selectedRoleIds, setSelectedRoleIds] = useState<string[]>([]);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    const timer = window.setTimeout(() => {
      setDebouncedSearch(search.trim());
      setPage(1);
    }, 250);
    return () => window.clearTimeout(timer);
  }, [search]);

  useEffect(() => {
    if (status !== "authenticated" || !mayManage) return;
    let active = true;
    setLoading(true);
    setError(null);
    Promise.all([
      getAdminIdentityUsers({
        search: debouncedSearch || undefined,
        isActive: activeFilter === "all" ? undefined : activeFilter === "active",
        page,
        pageSize: 25,
      }),
      roles.length > 0 ? Promise.resolve(roles) : getAdminIdentityRoles(),
    ])
      .then(([nextPage, nextRoles]) => {
        if (!active) return;
        setData(nextPage);
        setRoles(nextRoles);
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
    // `roles` is intentionally excluded; it is loaded together with the first page.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [activeFilter, debouncedSearch, mayManage, messages, page, status]);

  const pageCount = Math.max(1, Math.ceil(data.totalCount / data.pageSize));

  function openEditor(target: AdminIdentityUser) {
    setEditing(target);
    setCreating(false);
    setFullName(target.fullName);
    setPhoneNumber(target.phoneNumber ?? "");
    setIsActive(target.isActive);
    setSelectedRoleIds(target.roleIds);
    setNotice(null);
    setError(null);
  }

  function openCreate() {
    setEditing(null);
    setCreating(true);
    setNewUsername("");
    setNewEmail("");
    setNewFullName("");
    setNewPhoneNumber("");
    setNewRoleIds([]);
    setNotice(null);
    setError(null);
  }

  function toggleNewRole(roleId: string) {
    setNewRoleIds((current) =>
      current.includes(roleId) ? current.filter((id) => id !== roleId) : [...current, roleId],
    );
  }

  async function createUser() {
    if (newRoleIds.length === 0) {
      setError(messages.adminIdentitySelectAtLeastOneRole);
      return;
    }
    setSaving(true);
    setError(null);
    setNotice(null);
    try {
      const result = await createAdminIdentityUser({
        username: newUsername.trim(),
        email: newEmail.trim(),
        fullName: newFullName.trim(),
        phoneNumber: newPhoneNumber.trim() || null,
        roleIds: newRoleIds,
      });
      setNotice(result.emailDispatchQueued ? messages.adminIdentityInviteSent : messages.adminIdentityInviteNotQueued);
      setCreating(false);
      const nextPage = await getAdminIdentityUsers({
        search: debouncedSearch || undefined,
        isActive: activeFilter === "all" ? undefined : activeFilter === "active",
        page,
        pageSize: 25,
      });
      setData(nextPage);
    } catch (requestError) {
      setError(apiErrorMessage(requestError, messages));
    } finally {
      setSaving(false);
    }
  }

  async function sendPasswordReset() {
    if (!editing) return;
    setResetSending(true);
    setError(null);
    setNotice(null);
    try {
      await requestAdminIdentityPasswordReset(editing.id);
      setNotice(messages.adminIdentityPasswordResetSent);
    } catch (requestError) {
      setError(apiErrorMessage(requestError, messages));
    } finally {
      setResetSending(false);
    }
  }

  function toggleRole(roleId: string) {
    setSelectedRoleIds((current) =>
      current.includes(roleId) ? current.filter((id) => id !== roleId) : [...current, roleId],
    );
  }

  async function save() {
    if (!editing) return;
    if (selectedRoleIds.length === 0) {
      setError(messages.adminIdentitySelectAtLeastOneRole);
      return;
    }
    setSaving(true);
    setError(null);
    setNotice(null);
    try {
      await updateAdminIdentityUser(editing.id, {
        fullName,
        phoneNumber: phoneNumber.trim() || null,
        isActive,
      });
      await assignAdminIdentityRoles(editing.id, selectedRoleIds);
      setNotice(messages.adminIdentitySaved);
      setEditing(null);
      const nextPage = await getAdminIdentityUsers({
        search: debouncedSearch || undefined,
        isActive: activeFilter === "all" ? undefined : activeFilter === "active",
        page,
        pageSize: 25,
      });
      setData(nextPage);
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
    <main className="rigx-identity-admin mx-auto w-full max-w-6xl px-4 py-8 sm:px-6">
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div className="min-w-0 flex-1">
          <PageIntro
            eyebrow={messages.adminGroupIdentity}
            title={messages.adminIdentityUsersTitle}
            lead={messages.adminIdentityUsersLead}
          />
        </div>
        <PrimaryButton type="button" onClick={openCreate}>{messages.adminIdentityNewUser}</PrimaryButton>
      </div>

      {notice ? <Message tone="success">{notice}</Message> : null}
      {error ? <Message tone="error">{error}</Message> : null}

      <section className="rigx-admin-card mt-5">
        <div className="rigx-admin-card-head">
          <div>
            <h2>{messages.adminIdentityUsersTitle}</h2>
            <p>{messages.adminIdentityTotal}: {data.totalCount}</p>
          </div>
          <div className="flex flex-wrap items-center gap-2">
            <label className="sr-only" htmlFor="identity-user-search">{messages.adminIdentitySearch}</label>
            <input
              id="identity-user-search"
              type="search"
              value={search}
              onChange={(event) => setSearch(event.target.value)}
              placeholder={messages.adminIdentitySearch}
              className="rigx-admin-input min-w-56"
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
          <p className="p-8 text-center text-sm text-[var(--ds-text-muted)]">{messages.adminIdentityNoUsers}</p>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full min-w-[52rem] text-sm">
              <caption className="sr-only">{messages.adminIdentityUsersTitle}</caption>
              <thead className="bg-[var(--ds-surface-muted)] text-[var(--ds-text-muted)]">
                <tr>
                  <th className="p-4 text-start">{messages.adminIdentityUser}</th>
                  <th className="p-4 text-start">Username</th>
                  <th className="p-4 text-start">{messages.adminIdentityRoles}</th>
                  <th className="p-4 text-start">{messages.adminIdentityStatus}</th>
                  <th className="p-4 text-start">{messages.adminIdentityVerified}</th>
                  <th className="p-4 text-start">{messages.adminActions}</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-[var(--ds-border)]">
                {data.items.map((item) => (
                  <tr key={item.id} className="transition hover:bg-[var(--ds-surface-muted)]">
                    <td className="p-4">
                      <span className="block font-bold text-[var(--ds-text)]">{item.fullName}</span>
                      <span className="block text-xs text-[var(--ds-text-muted)]" dir="ltr">{item.email}</span>
                    </td>
                    <td className="p-4 font-mono text-xs text-[var(--ds-text-muted)]" dir="ltr">{item.username}</td>
                    <td className="p-4">
                      <span className="flex flex-wrap gap-1">
                        {item.roleCodes.map((code) => (
                          <span key={code} className="rigx-identity-chip">{code}</span>
                        ))}
                      </span>
                    </td>
                    <td className="p-4">
                      <span className={`rigx-admin-status ${item.isActive ? "is-published" : "is-draft"}`}>
                        {item.isActive ? messages.adminActive : messages.adminInactive}
                      </span>
                    </td>
                    <td className="p-4 text-xs text-[var(--ds-text-muted)]">
                      {item.isEmailVerified ? messages.adminIdentityVerified : messages.adminIdentityNotVerified}
                    </td>
                    <td className="p-4">
                      <SecondaryButton type="button" onClick={() => openEditor(item)}>
                        {messages.adminIdentityEditUser}
                      </SecondaryButton>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
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
        open={editing !== null || creating}
        title={creating ? messages.adminIdentityCreateUser : messages.adminIdentityEditUser}
        closeLabel={messages.adminCancel}
        onClose={() => {
          if (saving || resetSending) return;
          setCreating(false);
          setEditing(null);
        }}
      >
        {creating ? (
          <div className="grid gap-4">
            <TextField
              label={messages.adminIdentityEmail}
              value={newEmail}
              onChange={(event) => setNewEmail(event.target.value)}
              type="email"
              dir="ltr"
              required
            />
            <TextField
              label={messages.adminIdentityUsername}
              value={newUsername}
              onChange={(event) => setNewUsername(event.target.value)}
              dir="ltr"
              required
            />
            <TextField
              label={messages.adminIdentityFullName}
              value={newFullName}
              onChange={(event) => setNewFullName(event.target.value)}
              required
            />
            <TextField
              label={messages.adminIdentityPhone}
              value={newPhoneNumber}
              onChange={(event) => setNewPhoneNumber(event.target.value)}
              inputMode="tel"
              dir="ltr"
            />
            <fieldset className="grid gap-2 rounded-xl border border-[var(--ds-border)] p-3">
              <legend className="px-1 text-sm font-bold text-[var(--ds-text)]">{messages.adminIdentityRoles}</legend>
              {roles.map((role) => (
                <label key={role.id} className="flex min-h-10 items-center gap-2 text-sm text-[var(--ds-text)]">
                  <input
                    type="checkbox"
                    checked={newRoleIds.includes(role.id)}
                    onChange={() => toggleNewRole(role.id)}
                    className="h-4 w-4"
                  />
                  <span className="font-semibold">{role.code}</span>
                  <span className="text-xs text-[var(--ds-text-muted)]">{role.name}</span>
                </label>
              ))}
            </fieldset>
            <p className="text-xs leading-5 text-[var(--ds-text-muted)]">{messages.adminIdentityInviteHint}</p>
            <div className="flex flex-wrap gap-2">
              <PrimaryButton type="button" onClick={() => void createUser()} pending={saving}>
                {messages.adminIdentityCreateUser}
              </PrimaryButton>
              <SecondaryButton type="button" onClick={() => setCreating(false)} disabled={saving}>
                {messages.adminCancel}
              </SecondaryButton>
            </div>
          </div>
        ) : editing ? (
          <div className="grid gap-4">
            <div className="rounded-xl border border-[var(--ds-border)] bg-[var(--ds-surface-muted)] p-3 text-sm">
              <p className="font-bold text-[var(--ds-text)]">{editing.username}</p>
              <p className="text-xs text-[var(--ds-text-muted)]" dir="ltr">{editing.email}</p>
            </div>
            <TextField
              label={messages.adminIdentityFullName}
              value={fullName}
              onChange={(event) => setFullName(event.target.value)}
              required
            />
            <TextField
              label={messages.adminIdentityPhone}
              value={phoneNumber}
              onChange={(event) => setPhoneNumber(event.target.value)}
              inputMode="tel"
              dir="ltr"
            />
            <label className="flex min-h-11 items-center gap-2 text-sm font-semibold text-[var(--ds-text)]">
              <input
                type="checkbox"
                checked={isActive}
                onChange={(event) => setIsActive(event.target.checked)}
                className="h-4 w-4"
              />
              {messages.adminIdentityAccountActive}
            </label>
            <fieldset className="grid gap-2 rounded-xl border border-[var(--ds-border)] p-3">
              <legend className="px-1 text-sm font-bold text-[var(--ds-text)]">{messages.adminIdentityRoles}</legend>
              {roles.map((role) => (
                <label key={role.id} className="flex min-h-10 items-center gap-2 text-sm text-[var(--ds-text)]">
                  <input
                    type="checkbox"
                    checked={selectedRoleIds.includes(role.id)}
                    onChange={() => toggleRole(role.id)}
                    className="h-4 w-4"
                  />
                  <span className="font-semibold">{role.code}</span>
                  <span className="text-xs text-[var(--ds-text-muted)]">{role.name}</span>
                </label>
              ))}
            </fieldset>
            <p className="text-xs leading-5 text-[var(--ds-text-muted)]">{messages.adminIdentityPermissionHint}</p>
            <div className="flex flex-wrap gap-2">
              <PrimaryButton type="button" onClick={() => void save()} pending={saving}>
                {messages.adminSaveChanges}
              </PrimaryButton>
              <SecondaryButton type="button" onClick={() => void sendPasswordReset()} disabled={saving || resetSending}>
                {resetSending ? messages.adminIdentityPasswordResetSending : messages.adminIdentitySendPasswordReset}
              </SecondaryButton>
              <SecondaryButton type="button" onClick={() => setEditing(null)} disabled={saving || resetSending}>
                {messages.adminCancel}
              </SecondaryButton>
            </div>
          </div>
        ) : null}
      </SideDrawer>
    </main>
  );
}
