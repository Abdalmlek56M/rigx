"use client";

import { CaseIcon, PencilIcon, TrashIcon } from "@/components/ui/icons";
import { useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { unwrap } from "@rigx/api-client";
import { Link } from "@/i18n/navigation";
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
            <EmptyState icon={CaseIcon} message={t("empty")} action={<Link href="/builder" className="text-orange-text underline hover:text-orange-text-hover">{t("start")}</Link>} />
          ) : (
            <ul className="m-0 flex list-none flex-col gap-3 p-0">
              {builds.map((build) => {
                const usage = parseUsage(build.usage);
                return (
                  <li key={build.token} className="flex flex-wrap items-center justify-between gap-3 rounded-card border border-line bg-surface p-5">
                    <div className="flex min-w-0 flex-col gap-1">
                      <b dir="auto" className="[unicode-bidi:isolate]">{build.name?.trim() || t("unnamed")}</b>
                      <span className="text-sm text-ink-muted">
                        {usage ? `${tUsage(usage)} · ` : ""}{t("parts", { count: build.partCount })} · {formatDate(build.updatedAtUtc, locale)}
                      </span>
                    </div>
                    <div className="flex items-center gap-1.5">
                      <RowLink href={`/builder?saved=${encodeURIComponent(build.token)}`} label={t("open")} icon={PencilIcon} />
                      <ConfirmButton iconOnly label={t("delete")} icon={<TrashIcon size={16} />} onConfirm={() => void remove(build.token)} />
                    </div>
                  </li>
                );
              })}
            </ul>
          )
        }
      </LoadBoundary>
    </div>
  );
}
