"use client";

import { useEffect, useState } from "react";
import { getSecurityAuditEvents, type SecurityAuditItem, type SecurityAuditPage } from "@/lib/api-client";
import { apiErrorMessage } from "@/lib/form-errors";
import { useLocale } from "@/lib/i18n";
import { LoadingCard, Message, PageIntro, PrimaryButton, SecondaryButton, SideDrawer, TextField } from "@/components/ui";

const outcomes = ["", "Succeeded", "Failed", "Rejected", "Recorded"];

export function SecurityAuditAdmin() {
  const { messages } = useLocale();
  const [page, setPage] = useState<SecurityAuditPage | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [filters, setFilters] = useState({
    eventType: "",
    outcome: "",
    search: "",
    fromUtc: "",
    toUtc: "",
  });
  const [selected, setSelected] = useState<SecurityAuditItem | null>(null);

  async function load(nextPage = 1, currentFilters = filters) {
    setLoading(true);
    setError(null);
    try {
      setPage(await getSecurityAuditEvents({
        page: nextPage,
        pageSize: 25,
        eventType: currentFilters.eventType || undefined,
        outcome: currentFilters.outcome || undefined,
        search: currentFilters.search || undefined,
        fromUtc: currentFilters.fromUtc ? new Date(currentFilters.fromUtc).toISOString() : undefined,
        toUtc: currentFilters.toUtc ? new Date(currentFilters.toUtc).toISOString() : undefined,
      }));
    } catch (requestError) {
      setError(apiErrorMessage(requestError, messages));
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void load(1);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const totalPages = page ? Math.max(1, Math.ceil(page.total / page.pageSize)) : 1;

  return (
    <main className="rigx-identity-admin mx-auto grid w-full max-w-6xl gap-6 px-4 py-8 sm:px-6">
      <PageIntro eyebrow="RIGX / SECURITY" title={messages.auditTitle} lead={messages.auditLead} />

      <form
        className="grid gap-3 rounded-2xl border border-[var(--ds-border)] bg-[var(--ds-surface)] p-4 sm:grid-cols-2 lg:grid-cols-3"
        onSubmit={(event) => {
          event.preventDefault();
          void load(1);
        }}
      >
        <TextField label={messages.auditEventType} value={filters.eventType} onChange={(event) => setFilters((current) => ({ ...current, eventType: event.target.value }))} dir="ltr" />
        <label className="grid gap-1.5 text-sm font-semibold text-[var(--ds-text)]">
          {messages.auditOutcome}
          <select
            value={filters.outcome}
            onChange={(event) => setFilters((current) => ({ ...current, outcome: event.target.value }))}
            className="min-h-11 rounded-xl border border-[var(--ds-border)] bg-[var(--ds-surface)] px-3 text-sm"
          >
            {outcomes.map((outcome) => (
              <option key={outcome || "all"} value={outcome}>{outcome || "All"}</option>
            ))}
          </select>
        </label>
        <TextField label={messages.auditSearch} value={filters.search} onChange={(event) => setFilters((current) => ({ ...current, search: event.target.value }))} />
        <TextField label={messages.auditFrom} value={filters.fromUtc} onChange={(event) => setFilters((current) => ({ ...current, fromUtc: event.target.value }))} dir="ltr" />
        <TextField label={messages.auditTo} value={filters.toUtc} onChange={(event) => setFilters((current) => ({ ...current, toUtc: event.target.value }))} dir="ltr" />
        <div className="flex items-end gap-2">
          <PrimaryButton type="submit">{messages.adminMediaSearch}</PrimaryButton>
          <SecondaryButton type="button" onClick={() => { const cleared = { eventType: "", outcome: "", search: "", fromUtc: "", toUtc: "" }; setFilters(cleared); void load(1, cleared); }}>
            {messages.adminCancel}
          </SecondaryButton>
        </div>
      </form>

      {error ? <Message tone="error">{error}</Message> : null}
      {loading ? <LoadingCard /> : !page || page.items.length === 0 ? (
        <Message tone="info">{messages.auditEmpty}</Message>
      ) : (
        <>
          <section className="rigx-admin-card divide-y divide-[var(--ds-border)]">
            {page.items.map((item) => (
              <article key={item.id} className="flex flex-wrap items-center justify-between gap-3 p-4">
                <div className="min-w-0">
                  <p className="font-bold text-[var(--ds-text)]" dir="ltr">{item.eventType}</p>
                  <p className="text-xs text-[var(--ds-text-muted)]" dir="ltr">
                    {new Date(item.occurredAtUtc).toLocaleString("en-GB")} · {item.actorKind}
                  </p>
                </div>
                <div className="flex flex-wrap items-center gap-2">
                  <span className="rounded-full border border-[var(--ds-border)] bg-[var(--ds-surface-muted)] px-2.5 py-1 text-xs font-bold text-[var(--ds-text-muted)]">
                    {item.outcome}
                  </span>
                  <SecondaryButton type="button" onClick={() => setSelected(item)}>{messages.auditDetails}</SecondaryButton>
                </div>
              </article>
            ))}
          </section>
          <div className="flex items-center justify-between">
            <SecondaryButton type="button" disabled={(page.page ?? 1) <= 1} onClick={() => void load(page.page - 1)}>
              {messages.adminCancel}
            </SecondaryButton>
            <span className="text-sm text-[var(--ds-text-muted)]" dir="ltr">{page.page} / {totalPages}</span>
            <SecondaryButton type="button" disabled={page.page >= totalPages} onClick={() => void load(page.page + 1)}>
              {messages.retry}
            </SecondaryButton>
          </div>
        </>
      )}

      <SideDrawer open={selected !== null} title={messages.auditDetails} closeLabel={messages.adminCancel} onClose={() => setSelected(null)}>
        {selected ? (
          <dl className="grid gap-2 text-sm">
            <div className="flex justify-between gap-2"><dt className="font-semibold">{messages.auditEventType}</dt><dd dir="ltr">{selected.eventType}</dd></div>
            <div className="flex justify-between gap-2"><dt className="font-semibold">{messages.auditOutcome}</dt><dd dir="ltr">{selected.outcome}</dd></div>
            <div className="flex justify-between gap-2"><dt className="font-semibold">{messages.auditActor}</dt><dd dir="ltr">{selected.actorUserAccountId ?? selected.actorKind}</dd></div>
            <div className="flex justify-between gap-2"><dt className="font-semibold">{messages.auditSubject}</dt><dd dir="ltr">{selected.subjectUserAccountId ?? "-"}</dd></div>
            <div className="flex justify-between gap-2"><dt className="font-semibold">{messages.auditCorrelation}</dt><dd dir="ltr">{selected.correlationId ?? "-"}</dd></div>
            <div className="mt-3 grid gap-1">
              <dt className="font-semibold">{messages.auditMetadata}</dt>
              <dd>
                {Object.keys(selected.metadata).length === 0 ? "-" : (
                  <ul className="grid gap-1 rounded-xl bg-[var(--ds-surface-muted)] p-3 text-xs" dir="ltr">
                    {Object.entries(selected.metadata).map(([key, value]) => (
                      <li key={key}><span className="font-bold">{key}</span>: {value}</li>
                    ))}
                  </ul>
                )}
              </dd>
            </div>
          </dl>
        ) : null}
      </SideDrawer>
    </main>
  );
}
