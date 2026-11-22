"use client";

import { useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { POLICY_KINDS } from "@/features/policies/policy-body";
import { formatDateTimeFull } from "@/lib/format";
import { useLoad } from "@/lib/hooks/use-load";
import { FormMessage, PageHeader } from "../kit/PageHeader";
import { toFormErrors } from "../kit/form-errors";
import { useReveal } from "../kit/use-reveal";
import { useUrlFilters } from "../kit/use-url-filters";
import { useToast } from "../kit/Toast";
import { createPolicyVersion, fetchPolicies, updatePolicy } from "./policies-api";
import { PolicyEditor } from "./PolicyEditor";
import { PolicyHistory } from "./PolicyHistory";
import { kindSummary } from "./policy-rules";
import { PencilIcon, PlusIcon, RefreshIcon } from "@/components/ui/icons";
import { Button } from "../kit/Button";

const FILTERS = { kind: "" };

/** ADM-POLICIES: the six policies on the start side, the editor of the selected one on the end side. */
export function PoliciesScreen() {
  const t = useTranslations("admin.policies");
  const kindsText = useTranslations("policies.kinds");
  const locale = useLocale();
  const toast = useToast();
  const { values, set } = useUrlFilters(FILTERS);
  const { state, reload } = useLoad(fetchPolicies);
  const [starting, setStarting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const policies = state.status === "ready" ? state.data : [];
  const active = POLICY_KINDS.find((kind) => kind.slug === values.kind) ?? POLICY_KINDS[0];
  const summary = kindSummary(active.api, policies);
  const panelRef = useReveal<HTMLElement>(values.kind || null);

  /** A new draft starts from the live text, so the owner edits instead of retyping. */
  async function startDraft() {
    setStarting(true);
    setError(null);
    try {
      const draft = await createPolicyVersion(active.api);
      if (summary.published) {
        const { titleAr, titleEn, bodyAr, bodyEn } = summary.published;
        await updatePolicy(draft.id, { titleAr, titleEn, bodyAr, bodyEn, rowVersion: draft.rowVersion });
      }
      toast(t("draftStarted"));
    } catch (failure) {
      setError(toFormErrors(failure, t("failed")).general ?? t("failed"));
    } finally {
      setStarting(false);
      reload();
    }
  }

  return (
    <>
      <PageHeader title={t("title")} />
      {state.status === "error" ? (
        <div role="alert" className="flex flex-col items-start gap-3 rounded-card border border-line bg-surface p-6">
          <p className="m-0 font-bold">{t("failed")}</p>
          <Button variant="primary" icon={RefreshIcon} onClick={reload}>{t("retry")}</Button>
        </div>
      ) : (
        <div className="grid gap-4 lg:grid-cols-[minmax(0,360px)_minmax(0,1fr)] lg:items-start">
          {state.status === "loading" ? <div aria-hidden="true" className="h-64 animate-pulse rounded-card border border-line bg-surface" /> : (
            <ul className="m-0 flex list-none flex-col gap-2 p-0">
              {POLICY_KINDS.map((kind) => {
                const info = kindSummary(kind.api, policies);
                const current = kind.slug === active.slug;
                return (
                  <li key={kind.slug}>
                    <button type="button" aria-current={current ? "true" : undefined} onClick={() => set({ kind: kind.slug })} className={`flex w-full flex-col items-start gap-1 rounded-card border bg-surface p-4 text-start ${current ? "border-orange" : "border-line"}`}>
                      <span className="font-bold">{kindsText(kind.slug)}</span>
                      {info.published ? (
                        <span className="text-xs text-ink-muted">
                          {t("publishedInfo", { version: info.published.versionNumber, date: formatDateTimeFull(info.published.publishedAtUtc ?? info.published.updatedAtUtc, locale) })}
                        </span>
                      ) : <span className="text-xs text-error-text">{t("notPublished")}</span>}
                      {info.draft ? <span className="text-xs font-bold text-orange-text">{t("draftWaiting", { version: info.draft.versionNumber })}</span> : null}
                    </button>
                  </li>
                );
              })}
            </ul>
          )}
          <section ref={panelRef} className="scroll-mt-20 flex min-w-0 flex-col gap-4 rounded-card border border-line bg-surface p-5">
            <h2 className="m-0 text-[17px] font-bold">{kindsText(active.slug)}</h2>
            {state.status === "loading" ? null : summary.draft ? (
              <PolicyEditor key={`${summary.draft.id}-${summary.draft.rowVersion}`} policy={summary.draft} onSaved={reload} onStale={reload} />
            ) : (
              <div className="flex flex-col items-start gap-2">
                <p className="m-0 text-sm text-ink-muted">{summary.published ? t("startFromPublished") : t("startFirst")}</p>
                <Button variant="primary" icon={summary.published ? PlusIcon : PencilIcon} className="self-start" loading={starting} onClick={() => void startDraft()}>
                  {summary.published ? t("newVersion") : t("write")}
                </Button>
                <FormMessage message={error} />
              </div>
            )}
            {state.status === "ready" ? <PolicyHistory versions={summary.versions} /> : null}
          </section>
        </div>
      )}
    </>
  );
}
