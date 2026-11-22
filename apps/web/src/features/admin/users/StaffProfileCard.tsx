"use client";

import { useLocale, useTranslations } from "next-intl";
import { StatusBadge } from "@/components/ui/StatusBadge";
import { Link } from "@/i18n/navigation";
import { formatDate, formatDateTime, formatEgp } from "@/lib/format";
import { initials } from "@/features/account/profile-rules";
import { usePermissions } from "../use-permissions";
import { isCustomerOnly } from "./user-rules.ts";
import type { StaffProfile } from "./users-api";

function Row({ label, value, mono = false }: { label: string; value: string; mono?: boolean }) {
  return (
    <div className="flex justify-between gap-4 text-sm">
      <dt className="shrink-0 text-ink-muted">{label}</dt>
      <dd dir={mono ? "ltr" : "auto"} className={`m-0 min-w-0 text-end ${mono ? "break-all font-mono text-[13px]" : ""}`}>{value}</dd>
    </div>
  );
}

/** Who the staff member is: contact, roles, status, sign-in activity, what they bought and their customer record. */
export function StaffProfileCard({ profile, roleNames }: { profile: StaffProfile; roleNames: readonly string[] }) {
  const t = useTranslations("admin.users.profile");
  const status = useTranslations("admin.users.status");
  const locale = useLocale();
  const { has } = usePermissions();
  const { user, account, sales, linkedCustomers } = profile;
  // A customer account is its own customer record; a staff account links to the customer sharing its email or phone.
  const ownRecord = isCustomerOnly(user.roleCodes);
  const records = ownRecord ? [{ id: user.id, name: user.fullName, personCode: account.personCode }] : linkedCustomers;

  return (
    <section className="flex flex-col gap-4 rounded-card border border-line bg-surface p-5">
      <div className="flex items-center gap-3">
        <span aria-hidden="true" className="flex size-[52px] shrink-0 items-center justify-center rounded-full bg-carbon text-lg font-bold text-on-carbon">
          {initials(user.fullName)}
        </span>
        <span className="flex min-w-0 flex-col gap-1">
          <b className="text-lg [overflow-wrap:anywhere]">{user.fullName}</b>
          <span className="flex flex-wrap gap-1.5">
            <StatusBadge tone={user.isActive ? "success" : "danger"}>{user.isActive ? status("active") : status("inactive")}</StatusBadge>
            {roleNames.map((name) => <StatusBadge key={name} tone="neutral">{name}</StatusBadge>)}
          </span>
        </span>
      </div>

      <dl className="m-0 flex flex-col gap-2.5">
        <Row label={t("email")} value={user.email} mono />
        <Row label={t("phone")} value={user.phoneNumber ?? t("none")} mono={user.phoneNumber != null} />
        <Row label={t("code")} value={account.personCode} mono />
        <Row label={t("joined")} value={formatDate(account.createdAtUtc, locale)} />
        <Row label={t("lastActivity")} value={account.lastActivityAtUtc ? formatDateTime(account.lastActivityAtUtc, locale) : t("never")} />
        <Row label={t("activeSessions")} value={String(account.activeSessions)} />
      </dl>

      <dl className="m-0 flex flex-col gap-2.5 border-t border-line pt-3">
        <Row label={t("orderCount")} value={String(sales.stats.orderCount)} />
        <Row label={t("totalSpent")} value={formatEgp(sales.stats.totalSpent, locale)} />
      </dl>

      <div className="flex flex-col gap-1.5 border-t border-line pt-3 text-sm">
        <b>{t("customerRecord")}</b>
        {records.length === 0 ? <span className="text-ink-muted">{t("noCustomerRecord")}</span> : null}
        {records.map((customer) => has("Customers.View") ? (
          <Link key={customer.id} href={`/admin/customers/${customer.id}`} className="font-bold text-orange-text hover:underline">
            {customer.name} <span dir="ltr" className="font-mono text-xs">· {customer.personCode}</span>
          </Link>
        ) : (
          <span key={customer.id} className="font-bold">{customer.name} <span dir="ltr" className="font-mono text-xs">· {customer.personCode}</span></span>
        ))}
        {!ownRecord && records.length > 0 ? <span className="text-xs text-ink-muted">{t("linkRule")}</span> : null}
      </div>
    </section>
  );
}
