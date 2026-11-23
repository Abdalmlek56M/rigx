"use client";

import { useLocale, useTranslations } from "next-intl";
import { StatusBadge } from "@/components/ui/StatusBadge";
import { cardClass } from "@/components/ui/Card";
import { Link } from "@/i18n/navigation";
import { formatEgp } from "@/lib/format";
import { parseUsage } from "@/lib/usage";
import { initials } from "@/features/account/profile-rules";
import { usePermissions } from "../use-permissions";
import type { CustomerProfile, ShippingRates } from "./customers-api";
import { governorateName, monthYear } from "./customer-rules";

function Row({ label, value, mono = false, wrap = false }: { label: string; value: string; mono?: boolean; wrap?: boolean }) {
  return (
    <div className="flex justify-between gap-4 text-sm">
      <dt className="shrink-0 text-ink-muted">{label}</dt>
      <dd dir={mono ? "ltr" : "auto"} className={`m-0 min-w-0 text-end ${mono ? "font-mono text-[13px]" : ""} ${wrap ? "break-all" : ""}`}>{value}</dd>
    </div>
  );
}

/** The customer card: identity, sign-in methods, source, usage and the sales summary (read only — no API edits the profile). */
export function CustomerProfileCard({ profile, rates }: { profile: CustomerProfile; rates: ShippingRates }) {
  const t = useTranslations("admin.customers");
  const usageLabels = useTranslations("usage");
  const locale = useLocale();
  const { has } = usePermissions();
  const account = profile.account;
  const usage = parseUsage(profile.sales.stats.usage);
  const governorate = governorateName(rates.governorates, profile.governorate);

  return (
    <section className={cardClass({ className: "gap-4" })}>
      <div className="flex items-center gap-3">
        <span aria-hidden="true" className="flex h-[52px] w-[52px] shrink-0 items-center justify-center rounded-full bg-carbon text-lg font-bold text-on-carbon">
          {initials(account.name)}
        </span>
        <span className="flex min-w-0 flex-col gap-0.5">
          <b className="text-lg">{account.name}</b>
          <span className="text-xs text-ink-muted">{t("memberSince", { date: monthYear(account.createdAtUtc, locale) })}</span>
        </span>
      </div>

      <dl className="m-0 flex flex-col gap-2.5">
        <Row label={t("email")} value={account.email} mono wrap />
        <Row label={t("columns.phone")} value={account.phone ?? t("none")} mono />
        {governorate ? <Row label={t("governorate")} value={governorate} /> : null}
      </dl>

      <div className="flex flex-col gap-1.5 text-[13px]">
        <b>{t("loginMethods")}</b>
        <span className="flex flex-wrap gap-1.5">
          {profile.loginMethods.emailCode ? <StatusBadge tone="success">{t("emailCode")}</StatusBadge> : null}
          {profile.loginMethods.googleEmail ? <StatusBadge tone="neutral">{t("google")}</StatusBadge> : null}
        </span>
      </div>

      <dl className="m-0 flex flex-col gap-2.5 border-t border-line pt-3">
        <Row label={t("leadSource")} value={profile.sales.stats.leadSource ?? t("none")} />
        <Row label={t("usage")} value={usage ? usageLabels(usage) : t("none")} />
        <Row label={t("totalSpent")} value={formatEgp(profile.sales.stats.totalSpent, locale)} />
        <Row label={t("orderCount")} value={String(profile.sales.stats.orderCount)} />
        <Row label={t("activeSessions")} value={String(account.activeSessions)} />
      </dl>

      {profile.linkedStaff.length > 0 ? (
        <div className="flex flex-col gap-1.5 border-t border-line pt-3 text-sm">
          <b>{t("staffAccount")}</b>
          {profile.linkedStaff.map((staff) => has("System.ManageUsers") ? (
            <Link key={staff.id} href={`/admin/users/${staff.id}`} className="font-bold text-orange-text hover:underline">{staff.name}</Link>
          ) : (
            <span key={staff.id} className="font-bold">{staff.name}</span>
          ))}
        </div>
      ) : null}
    </section>
  );
}
