"use client";

import { InboxIcon } from "@/components/ui/icons";
import { useLocale, useTranslations } from "next-intl";
import { Card } from "@/components/ui/Card";
import { InfoField, InfoGrid } from "@/components/ui/InfoGrid";
import { StatusBadge } from "@/components/ui/StatusBadge";
import { requestTone } from "@/components/ui/status-badge-rules";
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
      <h2 className="sr-only">{t("title")}</h2>
      <LoadBoundary state={state} reload={reload}>
        {(requests) =>
          requests.length === 0 ? (
            <EmptyState icon={InboxIcon} message={t("empty")} />
          ) : (
            <ul className="m-0 flex list-none flex-col gap-3 p-0">
              {requests.map((request) => {
                const status = requestStatusKey(request.status);
                return (
                  <Card
                    key={request.code}
                    as="li"
                    title={<span dir="ltr" className="font-mono text-orange-text">{request.code}</span>}
                    icon={InboxIcon}
                    padding="md"
                    className="gap-3"
                    actions={<StatusBadge tone={requestTone(status)}>{t(`status.${status}`)}</StatusBadge>}
                  >
                    <InfoGrid columns={2} className="gap-y-3">
                      <InfoField label={t("typeLabel")} value={t(`type.${requestTypeKey(request.type)}`)} />
                      <InfoField label={t("date")} value={formatDate(request.createdAtUtc, locale)} />
                      {request.item ? <InfoField label={t("item")} value={request.item} wide /> : null}
                      {request.offerAmount !== null ? <InfoField label={t("offerLabel")} value={formatEgp(request.offerAmount, locale)} /> : null}
                    </InfoGrid>
                  </Card>
                );
              })}
            </ul>
          )
        }
      </LoadBoundary>
    </div>
  );
}
