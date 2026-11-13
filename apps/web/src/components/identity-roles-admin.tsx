"use client";

import { useEffect, useMemo, useState } from "react";
import {
  createAdminIdentityRole,
  getAdminIdentityPermissions,
  getAdminIdentityRoles,
  updateAdminIdentityRolePermissions,
  type AdminIdentityRole,
} from "@/lib/api-client";
import { apiErrorMessage } from "@/lib/form-errors";
import { useLocale } from "@/lib/i18n";
import { permissionDescription, permissionGroup, permissionLabel } from "@/lib/permission-labels";
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

const fixedRoles = new Set(["Owner", "Customer"]);

export function IdentityRolesAdmin() {
  const { locale, messages } = useLocale();
  const { status, user } = useSession();
  const mayManage = user?.permissions.includes("System.ManageUsers") ?? false;
  const [roles, setRoles] = useState<AdminIdentityRole[]>([]);
  const [permissions, setPermissions] = useState<string[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const [editing, setEditing] = useState<AdminIdentityRole | null>(null);
  const [creating, setCreating] = useState(false);
  const [code, setCode] = useState("");
  const [name, setName] = useState("");
  const [selectedPermissions, setSelectedPermissions] = useState<string[]>([]);
  const [permissionSearch, setPermissionSearch] = useState("");
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    if (status !== "authenticated" || !mayManage) return;
    let active = true;
    setLoading(true);
    setError(null);
    Promise.all([getAdminIdentityRoles(), getAdminIdentityPermissions()])
      .then(([nextRoles, nextPermissions]) => {
        if (!active) return;
        setRoles(nextRoles);
        setPermissions(nextPermissions);
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

  const permissionGroups = useMemo(() => {
    const term = permissionSearch.trim().toLocaleLowerCase(locale);
    const filtered = permissions.filter((permission) => {
      if (!term) return true;
      return permission.toLocaleLowerCase(locale).includes(term) ||
        permissionLabel(permission, locale).toLocaleLowerCase(locale).includes(term) ||
        permissionDescription(permission, locale).toLocaleLowerCase(locale).includes(term);
    });
    const groups = new Map<string, string[]>();
    for (const permission of filtered) {
      const group = permissionGroup(permission, locale);
      const items = groups.get(group) ?? [];
      items.push(permission);
      groups.set(group, items);
    }
    return Array.from(groups.entries()).sort(([left], [right]) => left.localeCompare(right, locale));
  }, [locale, permissionSearch, permissions]);

  function openRole(role: AdminIdentityRole) {
    setEditing(role);
    setCreating(false);
    setCode(role.code);
    setName(role.name);
    setSelectedPermissions(role.permissionCodes);
    setPermissionSearch("");
    setError(null);
    setNotice(null);
  }

  function openCreate() {
    setEditing(null);
    setCreating(true);
    setCode("");
    setName("");
    setSelectedPermissions([]);
    setPermissionSearch("");
    setError(null);
    setNotice(null);
  }

  function togglePermission(permission: string) {
    setSelectedPermissions((current) =>
      current.includes(permission)
        ? current.filter((item) => item !== permission)
        : [...current, permission],
    );
  }

  async function save() {
    setSaving(true);
    setError(null);
    setNotice(null);
    try {
      if (creating) {
        await createAdminIdentityRole({ code, name, permissionCodes: selectedPermissions });
        setNotice(messages.adminIdentityRoleSaved);
      } else if (editing) {
        await updateAdminIdentityRolePermissions(editing.id, selectedPermissions);
        setNotice(messages.adminIdentityRoleSaved);
      }
      setEditing(null);
      setCreating(false);
      const nextRoles = await getAdminIdentityRoles();
      setRoles(nextRoles);
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

  const drawerOpen = creating || editing !== null;
  const fixed = editing !== null && fixedRoles.has(editing.code);

  return (
    <main className="rigx-identity-admin mx-auto w-full max-w-6xl px-4 py-8 sm:px-6">
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div className="min-w-0 flex-1">
          <PageIntro
            eyebrow={messages.adminGroupIdentity}
            title={messages.adminIdentityRolesTitle}
            lead={messages.adminIdentityRolesLead}
          />
        </div>
        <PrimaryButton type="button" onClick={openCreate}>
          {messages.adminIdentityNewRole}
        </PrimaryButton>
      </div>

      {notice ? <Message tone="success">{notice}</Message> : null}
      {error ? <Message tone="error">{error}</Message> : null}

      <section className="rigx-admin-card mt-5">
        <div className="rigx-admin-card-head">
          <div>
            <h2>{messages.adminIdentityRolesTitle}</h2>
            <p>{messages.adminIdentityPermissionHint}</p>
          </div>
          <span className="rigx-admin-count">{roles.length}</span>
        </div>

        {loading ? (
          <div className="p-6"><LoadingCard /></div>
        ) : roles.length === 0 ? (
          <p className="p-8 text-center text-sm text-[var(--ds-text-muted)]">{messages.adminIdentityNoRoles}</p>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full min-w-[46rem] text-sm">
              <caption className="sr-only">{messages.adminIdentityRolesTitle}</caption>
              <thead className="bg-[var(--ds-surface-muted)] text-[var(--ds-text-muted)]">
                <tr>
                  <th className="p-4 text-start">{messages.adminIdentityRoleCode}</th>
                  <th className="p-4 text-start">{messages.adminIdentityRoleName}</th>
                  <th className="p-4 text-start">{messages.adminIdentityUsersColumn}</th>
                  <th className="p-4 text-start">{messages.adminIdentityPermissionsColumn}</th>
                  <th className="p-4 text-start">{messages.adminActions}</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-[var(--ds-border)]">
                {roles.map((role) => {
                  const isFixed = fixedRoles.has(role.code);
                  return (
                    <tr key={role.id} className="transition hover:bg-[var(--ds-surface-muted)]">
                      <td className="p-4 font-mono text-xs font-bold text-[var(--ds-accent)]" dir="ltr">{role.code}</td>
                      <td className="p-4 font-semibold text-[var(--ds-text)]">
                        {role.name}
                        {isFixed ? <span className="ms-2 text-xs text-[var(--ds-text-muted)]">({messages.adminIdentityRoleFixed})</span> : null}
                      </td>
                      <td className="p-4 text-[var(--ds-text-muted)]">{role.userCount}</td>
                      <td className="p-4 text-[var(--ds-text-muted)]">{role.permissionCodes.length}</td>
                      <td className="p-4">
                        <SecondaryButton type="button" onClick={() => openRole(role)}>
                          {messages.adminIdentityEditPermissions}
                        </SecondaryButton>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}
      </section>

      <SideDrawer
        open={drawerOpen}
        title={creating ? messages.adminIdentityNewRole : messages.adminIdentityEditPermissions}
        closeLabel={messages.adminCancel}
        onClose={() => {
          if (saving) return;
          setEditing(null);
          setCreating(false);
        }}
      >
        <div className="grid gap-4">
          {creating ? (
            <>
              <TextField
                label={messages.adminIdentityRoleCode}
                value={code}
                onChange={(event) => setCode(event.target.value)}
                required
                hint={messages.adminIdentityRoleCodeHint}
              />
              <TextField
                label={messages.adminIdentityRoleName}
                value={name}
                onChange={(event) => setName(event.target.value)}
                required
              />
            </>
          ) : (
            <div className="rounded-xl border border-[var(--ds-border)] bg-[var(--ds-surface-muted)] p-3">
              <p className="font-mono text-sm font-bold text-[var(--ds-accent)]" dir="ltr">{editing?.code}</p>
              <p className="text-xs text-[var(--ds-text-muted)]">{editing?.name}</p>
              {fixed ? <p className="mt-2 text-xs font-semibold text-[var(--ds-danger)]">{messages.adminIdentityRoleFixed}</p> : null}
            </div>
          )}

          <div className="grid gap-3">
            <label className="sr-only" htmlFor="permission-search">{messages.adminPermissionSearch}</label>
            <input
              id="permission-search"
              type="search"
              value={permissionSearch}
              onChange={(event) => setPermissionSearch(event.target.value)}
              placeholder={messages.adminPermissionSearch}
              className="rigx-admin-input"
            />
            {permissionGroups.map(([group, items]) => (
              <fieldset key={group} className="rounded-xl border border-[var(--ds-border)] p-3">
                <legend className="px-1 text-xs font-bold uppercase tracking-wide text-[var(--ds-text-muted)]">{group}</legend>
                <div className="grid gap-1.5 sm:grid-cols-2">
                  {items.map((permission) => (
                    <label key={permission} className="flex min-h-11 items-start gap-2 rounded-lg p-1.5 text-xs text-[var(--ds-text)] transition hover:bg-[var(--ds-surface-muted)]">
                      <input
                        type="checkbox"
                        checked={selectedPermissions.includes(permission)}
                        onChange={() => togglePermission(permission)}
                        disabled={fixed}
                        className="mt-0.5 h-4 w-4 shrink-0"
                      />
                      <span className="grid min-w-0 gap-0.5">
                        <span className="font-semibold">
                          {permissionLabel(permission, locale)}
                          <span dir="ltr" className="ms-1.5 font-mono text-[9px] font-normal text-[var(--ds-text-muted)]">{permission}</span>
                        </span>
                        <span className="text-[10px] leading-4 text-[var(--ds-text-muted)]">{permissionDescription(permission, locale)}</span>
                      </span>
                    </label>
                  ))}
                </div>
              </fieldset>
            ))}
          </div>

          <div className="flex flex-wrap gap-2">
            <PrimaryButton type="button" onClick={() => void save()} pending={saving} disabled={fixed}>
              {creating ? messages.adminIdentityCreateRole : messages.adminSaveChanges}
            </PrimaryButton>
            <SecondaryButton type="button" onClick={() => {
              setEditing(null);
              setCreating(false);
            }} disabled={saving}>
              {messages.adminCancel}
            </SecondaryButton>
          </div>
        </div>
      </SideDrawer>
    </main>
  );
}
