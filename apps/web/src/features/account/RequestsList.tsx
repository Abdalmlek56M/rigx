"use client";

import { InboxIcon } from "@/components/ui/icons";
import { useLocale, useTranslations } from "next-intl";
import { formatDate, formatEgp } from "@/lib/format";
import { fetchRequests } from "./account-api";
import { requestStatusKey, requestTypeKey } from "./account-labels";
import { EmptyState, LoadBoundary } from "./LoadBoundary";
import { useLoad } from "@/lib/hooks/use-load";

/** "طلباتي الخاصة": the sell / trade / upgrade / service requests, each with where it stands and any offer. */
export function RequestsList() {
  const t = useTranslations("account.requests");
  const locale = useLocale();
  const { state, reload } = useLoad(fetchRequests);

  return (
    <div className="flex flex-col gap-4">
      <h2 className="m-0 text-xl font-bold">{t("title")}</h2>
      <LoadBoundary state={state} reload={reload}>
        {(requests) =>
          requests.length === 0 ? (
            <EmptyState icon={InboxIcon} message={t("empty")} />
          ) : (
            <ul className="m-0 flex list-none flex-col gap-3 p-0">
              {requests.map((request) => (
                <li key={request.code} className="flex flex-col gap-2 rounded-[10px] border border-line bg-surface p-5">
                  <div className="flex flex-wrap items-baseline justify-between gap-x-4 gap-y-1">
                    <span dir="ltr" className="font-mono text-sm font-semibold text-orange-text">{request.code}</span>
                    <span className="text-sm text-ink-muted">{formatDate(request.createdAtUtc, locale)}</span>
                  </div>
                  <div className="flex flex-wrap items-center justify-between gap-2">
                    <b>{t(`type.${requestTypeKey(request.type)}`)}</b>
                    <span className="rounded-full bg-line px-3 py-1 text-xs font-bold">{t(`status.${requestStatusKey(request.status)}`)}</span>
                  </div>
                  {request.item ? <p dir="auto" className="m-0 text-sm text-ink-muted [unicode-bidi:isolate]">{request.item}</p> : null}
                  {request.offerAmount !== null ? <p className="m-0 text-sm">{t("offer")} <b>{formatEgp(request.offerAmount, locale)}</b></p> : null}
                </li>
              ))}
            </ul>
          )
        }
      </LoadBoundary>
    </div>
  );
}
