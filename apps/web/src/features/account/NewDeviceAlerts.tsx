"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { unwrap } from "@rigx/api-client";
import { browserApi } from "@/lib/api/browser";
import { fetchNewDeviceAlerts } from "./account-api";
import { LoadBoundary } from "./LoadBoundary";
import { useLoad } from "./use-load";

function Toggle({ initial }: { initial: boolean }) {
  const t = useTranslations("account.profile.alerts");
  const [enabled, setEnabled] = useState(initial);
  const [failed, setFailed] = useState(false);

  async function change(next: boolean) {
    setFailed(false);
    try {
      const saved = await unwrap(browserApi().PUT("/api/v1/identity/settings/new-device-alerts", { body: { enabled: next } }));
      setEnabled(saved?.enabled ?? next);
    } catch {
      setFailed(true);
    }
  }

  return (
    <>
      <label className="flex min-h-11 items-center gap-2.5 text-sm">
        <input type="checkbox" checked={enabled} onChange={(event) => void change(event.target.checked)} className="h-[18px] w-[18px] accent-carbon" />
        {t("label")}
      </label>
      {failed ? <p role="alert" className="m-0 text-sm text-error">{t("failed")}</p> : null}
    </>
  );
}

/** "ابعتلي إيميل لما حد يدخل من جهاز جديد". */
export function NewDeviceAlerts() {
  const { state, reload } = useLoad(fetchNewDeviceAlerts);
  return <LoadBoundary state={state} reload={reload}>{(setting) => <Toggle initial={setting.enabled} />}</LoadBoundary>;
}
