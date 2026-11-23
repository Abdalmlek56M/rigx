"use client";

import { useCallback, useState } from "react";
import { useTranslations } from "next-intl";
import { Link } from "@/i18n/navigation";
import { useLoad } from "@/lib/hooks/use-load";
import { ErrorPanel, LoadingCard } from "../kit/LoadState";
import { PageHeader } from "../kit/PageHeader";
import { usePermissions } from "../use-permissions";
import { CustomerAccountActions } from "./CustomerAccountActions";
import { CustomerActivityTabs } from "./CustomerActivityTabs";
import { CustomerProfileCard } from "./CustomerProfileCard";
import { blacklistEntry } from "./customer-rules";
import { fetchBlacklist, fetchCustomer, fetchShippingRates } from "./customers-api";
import { ViewAsDialog } from "./ViewAsDialog";
import { ArrowBackIcon, EyeIcon } from "@/components/ui/icons";
import { Button, buttonClass, buttonIconSize } from "../kit/Button";

/** ADM-Customer 360 view: the account, its actions and the customer's orders, warranties, requests and note. */
export function CustomerDetail({ id }: { id: string }) {
  const t = useTranslations("admin.customers");
  const { has } = usePermissions();
  const canManage = has("Customers.Manage");
  const canViewBlacklist = has("Sales.ViewCustomerContact");
  const canApprove = has("Sales.ApproveOrder");
  const [viewAs, setViewAs] = useState(false);
  const load = useCallback(async () => {
    const [profile, rates, blacklist] = await Promise.all([
      fetchCustomer(id),
      fetchShippingRates(),
      canViewBlacklist ? fetchBlacklist() : Promise.resolve([])
    ]);
    return { profile, rates, blacklist };
  }, [id, canViewBlacklist]);
  const { state, reload } = useLoad(load);

  if (state.status === "loading") return <LoadingCard className="h-64" label={t("loading")} />;
  if (state.status === "error") {
    return (
      <ErrorPanel message={t("failed")} retryLabel={t("retry")} onRetry={reload} />
    );
  }

  const { profile, rates, blacklist } = state.data;
  const entry = blacklistEntry(blacklist, profile.account.phone, profile.account.email);

  return (
    <>
      <PageHeader
        title={profile.account.name}
        actions={
          <>
            <Link href="/admin/customers" className={buttonClass()}><ArrowBackIcon size={buttonIconSize()} />{t("back")}</Link>
            <Button icon={EyeIcon} onClick={() => setViewAs(true)}>{t("viewAs")}</Button>
          </>
        }
      />
      <div className="grid gap-4 lg:grid-cols-[340px_minmax(0,1fr)] lg:items-start">
        <div className="flex flex-col gap-4">
          <CustomerProfileCard profile={profile} rates={rates} />
          <CustomerAccountActions
            profile={profile} blacklist={entry} canManage={canManage} canViewBlacklist={canViewBlacklist}
            canApprove={canApprove} onChanged={reload}
          />
        </div>
        <CustomerActivityTabs profile={profile} canManage={canManage} onRetry={reload} onChanged={reload} />
      </div>
      {viewAs ? <ViewAsDialog id={id} onClose={() => setViewAs(false)} /> : null}
    </>
  );
}
