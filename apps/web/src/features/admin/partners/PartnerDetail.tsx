"use client";

import { useCallback, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { formatEgp, listSeparator } from "@/lib/format";
import { useLoad } from "@/lib/hooks/use-load";
import { fetchMoneyAccounts, type Partner } from "../expenses/expenses-api";
import { usePermissions } from "../use-permissions";
import { PartnerCapital } from "./PartnerCapital";
import { PartnerForm } from "./PartnerForm";
import { PartnerParts } from "./PartnerParts";
import { PartnerPayouts } from "./PartnerPayouts";
import { PartnerStatement } from "./PartnerStatement";
import { basisKey } from "./partner-rules";
import { fetchPartner } from "./partners-api";
import { CloseIcon, RefreshIcon } from "@/components/ui/icons";
import { cardClass } from "@/components/ui/Card";
import { Button, IconButton } from "../kit/Button";
import { LoadingCard } from "../kit/LoadState";

/** The picked partner: balances, the agreement form, financed parts, capital, payouts and the statement. */
export function PartnerDetail({ id, today, onClose, onChanged }: { id: string; today: string; onClose: () => void; onChanged: () => void }) {
  const t = useTranslations("admin.partners.detail");
  const basis = useTranslations("admin.partners.form.bases");
  const locale = useLocale();
  const { has } = usePermissions();
  const canSeeVaults = has("Finance.ViewVaults");
  const load = useCallback(async () => {
    const [detail, accounts] = await Promise.all([fetchPartner(id), canSeeVaults ? fetchMoneyAccounts().catch(() => null) : Promise.resolve(null)]);
    return { detail, accounts: accounts?.filter((account) => account.isActive) ?? null };
  }, [id, canSeeVaults]);
  const { state, reload } = useLoad(load);
  // The partner as the last save or money movement left it (the rest of the screen stays as it is).
  const [saved, setSaved] = useState<Partner | null>(null);
  // Bumped by every money movement, so the statement reads its range again.
  const [movements, setMovements] = useState(0);

  /** Reads the partner again after a 409 or a capital or payout entry (balances changed); a failed read keeps what is shown. */
  const refresh = () => {
    setMovements((count) => count + 1);
    fetchPartner(id).then((fresh) => setSaved(fresh.partner), () => undefined);
    onChanged();
  };

  if (state.status === "loading") return <LoadingCard className="h-64" label={t("loading")} />;
  if (state.status === "error") {
    return (
      <div role="alert" className={cardClass({ className: "items-start gap-3" })}>
        <p className="m-0 font-bold">{t("failed")}</p>
        <div className="flex flex-wrap gap-2">
          <Button variant="primary" icon={RefreshIcon} onClick={reload}>{t("retry")}</Button>
          <Button icon={CloseIcon} onClick={onClose}>{t("close")}</Button>
        </div>
      </div>
    );
  }

  const { detail, accounts } = state.data;
  const partner = saved ?? detail.partner;
  const figures = [
    { key: "capital", label: t("capital"), value: partner.capitalBalance },
    { key: "due", label: t("due"), value: partner.due },
    { key: "recoverable", label: t("recoverable"), value: partner.recoverable }
  ];

  return (
    <section className={cardClass({ className: "gap-5" })}>
      <header className="flex flex-wrap items-start justify-between gap-2">
        <div className="flex min-w-0 flex-col gap-0.5">
          <h2 className="m-0 text-[17px] font-bold"><span dir="auto" className="[unicode-bidi:isolate]">{partner.name}</span></h2>
          <span className="text-xs text-ink-muted">{t("summary", { percent: partner.defaultPercent, basis: basis(basisKey(partner.basis)) })}{partner.isActive ? "" : `${listSeparator(locale)}${t("inactive")}`}</span>
        </div>
        <IconButton icon={CloseIcon} label={t("close")} onClick={onClose} />
      </header>
      <dl className="m-0 grid grid-cols-1 gap-2 sm:grid-cols-3">
        {figures.map((figure) => (
          <div key={figure.key} className="flex flex-col gap-0.5 rounded-control border border-line bg-paper p-3">
            <dt className="text-xs text-ink-muted">{figure.label}</dt>
            <dd className="m-0 text-[17px] font-bold">{formatEgp(figure.value, locale)}</dd>
          </div>
        ))}
      </dl>
      <div className="grid items-start gap-5 xl:grid-cols-[minmax(0,1fr)_minmax(0,1fr)]">
        <PartnerForm
          key={partner.rowVersion} partner={partner} today={today} canEdit={has("Finance.ManagePartnerAgreements")}
          onSaved={(next) => {
            setSaved(next);
            onChanged();
          }}
          onStale={refresh}
        />
        <PartnerParts parts={detail.financedParts} />
      </div>
      <PartnerCapital partnerId={id} today={today} accounts={accounts} canPost={has("Finance.ManagePartnerAgreements")} onChanged={refresh} />
      <PartnerPayouts partnerId={id} accounts={accounts} canPost={has("Finance.PostPartnerSettlements")} onChanged={refresh} />
      <PartnerStatement partnerId={id} today={today} movements={movements} />
    </section>
  );
}
