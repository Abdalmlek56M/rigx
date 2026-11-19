"use client";

import { useLocale, useTranslations } from "next-intl";
import { formatDateTimeFull } from "@/lib/format";
import type { Policy } from "./policies-api";
import { PolicyStatusBadge } from "./PolicyStatusBadge";

/** Read-only list of a kind's versions, newest first. */
export function PolicyHistory({ versions }: { versions: readonly Policy[] }) {
  const t = useTranslations("admin.policies");
  const locale = useLocale();
  return (
    <section className="flex flex-col gap-2">
      <h3 className="m-0 text-[15px] font-bold">{t("history")}</h3>
      {versions.length === 0 ? <p className="m-0 text-sm text-ink-muted">{t("noVersions")}</p> : (
        <ul className="m-0 flex list-none flex-col gap-1.5 p-0">
          {versions.map((version) => (
            <li key={version.id} className="flex flex-wrap items-center gap-x-3 gap-y-1 text-sm">
              <span className="font-bold">{t("version", { version: version.versionNumber })}</span>
              <PolicyStatusBadge status={version.status} />
              <span dir="ltr" className="font-mono text-xs text-ink-muted">{formatDateTimeFull(version.publishedAtUtc ?? version.updatedAtUtc, locale)}</span>
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}
