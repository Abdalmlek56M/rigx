"use client";

import { useCallback, useState } from "react";
import { useTranslations } from "next-intl";
import { useSession } from "@/features/auth/use-session";
import { Link } from "@/i18n/navigation";
import { useLoad } from "@/lib/hooks/use-load";
import { NoteEditor, OrdersTable, WarrantiesTable } from "../customers/CustomerActivityTabs";
import { PageHeader } from "../kit/PageHeader";
import { StaffAccountControls } from "./StaffAccountControls";
import { StaffDetailsForm } from "./StaffDetailsForm";
import { StaffProfileCard } from "./StaffProfileCard";
import { useBuiltInRoleLabel } from "./use-role-label";
import { UserRestrictions } from "./UserRestrictions";
import { UserRolesCard } from "./UserRolesCard";
import { isSameAccount, roleNames, staffTab, staffTabs, type StaffTab } from "./user-rules.ts";
import { fetchRoles, fetchStaffProfile, saveStaffNote } from "./users-api";
import { ArrowBackIcon, RefreshIcon } from "@/components/ui/icons";
import { Button, buttonClass, buttonIconSize } from "../kit/Button";

const tabButton = "h-10 whitespace-nowrap rounded-control px-3.5 text-[13px]";

/** ADM-User profile: who the staff member is, account controls, and tabs for details, roles, purchases, note and restrictions. */
export function StaffProfileScreen({ id }: { id: string }) {
  const t = useTranslations("admin.users");
  const session = useSession();
  const builtIn = useBuiltInRoleLabel();
  const [rawTab, setRawTab] = useState<string>("details");
  const load = useCallback(async () => {
    const [profile, roles] = await Promise.all([fetchStaffProfile(id), fetchRoles()]);
    return { profile, roles };
  }, [id]);
  const { state, reload } = useLoad(load);

  if (state.status === "loading") return <div role="status" aria-label={t("profile.loading")} className="h-64 animate-pulse rounded-card border border-line bg-surface" />;
  if (state.status === "error") {
    return (
      <div role="alert" className="flex flex-col items-start gap-3 rounded-card border border-line bg-surface p-6">
        <p className="m-0 font-bold">{t("failed")}</p>
        <div className="flex flex-wrap gap-2">
          <Button variant="primary" icon={RefreshIcon} onClick={reload}>{t("retry")}</Button>
          <Link href="/admin/users" className={buttonClass()}><ArrowBackIcon size={buttonIconSize()} />{t("profile.back")}</Link>
        </div>
      </div>
    );
  }

  const { profile, roles } = state.data;
  const { user, account, sales } = profile;
  const isSelf = session.status === "signedIn" && isSameAccount(user.email, session.user.email);
  const tab = staffTab(rawTab, isSelf);
  const tabLabel = (key: StaffTab) => key === "purchases" ? t("profile.tabs.purchases", { count: sales.orders.length }) : t(`profile.tabs.${key}`);

  return (
    <>
      <PageHeader
        title={user.fullName}
        actions={<Link href="/admin/users" className={buttonClass()}><ArrowBackIcon size={buttonIconSize()} />{t("profile.back")}</Link>}
      />
      <div className="grid gap-4 lg:grid-cols-[340px_minmax(0,1fr)] lg:items-start">
        <div className="flex min-w-0 flex-col gap-4">
          <StaffProfileCard profile={profile} roleNames={roleNames(user, roles, builtIn)} />
          <StaffAccountControls user={user} isSelf={isSelf} onChanged={reload} />
        </div>
        <section className="flex min-w-0 flex-col gap-3.5 rounded-card border border-line bg-surface p-5">
          <div role="group" aria-label={t("profile.sections")} className="flex flex-wrap gap-1 rounded-card border border-line bg-surface p-1">
            {staffTabs(isSelf).map((key) => (
              <button
                key={key} type="button" aria-pressed={tab === key} onClick={() => setRawTab(key)}
                className={`${tabButton} ${tab === key ? "bg-carbon font-bold text-on-carbon" : "font-medium hover:bg-line"}`}
              >
                {tabLabel(key)}
              </button>
            ))}
          </div>
          {tab === "details" ? <StaffDetailsForm key={`${user.fullName}|${user.phoneNumber}`} user={user} onSaved={reload} /> : null}
          {tab === "roles" ? <UserRolesCard key={user.roleIds.join(",")} user={user} roles={roles} onSaved={reload} /> : null}
          {tab === "purchases" ? (
            <div className="flex flex-col gap-4">
              <OrdersTable orders={sales.orders} onRetry={reload} />
              {sales.warranties.length > 0 ? <WarrantiesTable warranties={sales.warranties} onRetry={reload} /> : null}
            </div>
          ) : null}
          {tab === "notes" ? (
            <NoteEditor
              key={account.note ?? ""} note={account.note} canManage save={(note) => saveStaffNote(user.id, note)} onSaved={reload}
              hint={t("profile.noteHint")}
            />
          ) : null}
          {tab === "restrictions" ? <UserRestrictions userId={user.id} /> : null}
        </section>
      </div>
    </>
  );
}
