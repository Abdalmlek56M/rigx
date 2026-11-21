"use client";

import { CaseIcon, PencilIcon, TrashIcon } from "@/components/ui/icons";
import { useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { unwrap } from "@rigx/api-client";
import { ButtonLink } from "@/components/ui/Button";
import { Card } from "@/components/ui/Card";
import { InfoField, InfoGrid } from "@/components/ui/InfoGrid";
import { browserApi } from "@/lib/api/browser";
import { parseUsage } from "@/lib/usage";
import { formatDate } from "@/lib/format";
import { fetchBuilds } from "./account-api";
import { ConfirmButton } from "./ConfirmButton";
import { EmptyState, LoadBoundary } from "./LoadBoundary";
import { RowLink } from "./RowAction";
import { useLoad } from "@/lib/hooks/use-load";

/** "تجميعاتي المحفوظة": open one in the builder, or delete it (after a confirmation). */
export function BuildsList() {
  const t = useTranslations("account.builds");
  const tUsage = useTranslations("usage");
  const locale = useLocale();
  const { state, reload } = useLoad(fetchBuilds);
  const [failed, setFailed] = useState(false);

  async function remove(token: string) {
    setFailed(false);
    try {
      await unwrap(browserApi().DELETE("/api/v1/builder/saved/{token}", { params: { path: { token } } }));
      reload();
    } catch {
      setFailed(true);
    }
  }

  return (
    <div className="flex flex-col gap-4">
      <h2 className="sr-only">{t("title")}</h2>
      {failed ? <p role="alert" className="m-0 text-sm text-error">{t("deleteFailed")}</p> : null}
      <LoadBoundary state={state} reload={reload}>
        {(builds) =>
          builds.length === 0 ? (
            <EmptyState icon={CaseIcon} message={t("empty")} action={<ButtonLink href="/builder" variant="secondary" size="sm">{t("start")}</ButtonLink>} />
          ) : (
            <ul className="m-0 flex list-none flex-col gap-3 p-0">
              {builds.map((build) => {
                const usage = parseUsage(build.usage);
                return (
                  <Card
                    key={build.token}
                    as="li"
                    title={<span dir="auto" className="[unicode-bidi:isolate]">{build.name?.trim() || t("unnamed")}</span>}
                    icon={CaseIcon}
                    padding="md"
                    className="gap-3"
                    actions={
                      <div className="flex items-center gap-1.5">
                        <RowLink href={`/builder?saved=${encodeURIComponent(build.token)}`} label={t("open")} icon={PencilIcon} />
                        <ConfirmButton iconOnly label={t("delete")} icon={TrashIcon} onConfirm={() => void remove(build.token)} />
                      </div>
                    }
                  >
                    <InfoGrid columns={2} className="gap-y-3">
                      {usage ? <InfoField label={t("usage")} value={tUsage(usage)} /> : null}
                      <InfoField label={t("partsLabel")} value={t("parts", { count: build.partCount })} />
                      <InfoField label={t("updated")} value={formatDate(build.updatedAtUtc, locale)} />
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
