/** One label/value pair in an audit detail; nothing when the value is empty. */
export function AuditField({ label, value, mono = false }: { label: string; value: string | null; mono?: boolean }) {
  if (!value) return null;
  return (
    <div className="flex flex-col gap-0.5">
      <dt className="text-xs text-ink-muted">{label}</dt>
      <dd dir={mono ? "ltr" : "auto"} className={`m-0 text-sm ${mono ? "font-mono text-[13px]" : ""} [overflow-wrap:anywhere]`}>{value}</dd>
    </div>
  );
}
