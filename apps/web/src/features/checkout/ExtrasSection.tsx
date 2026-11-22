"use client";

import { useId, useState, type ReactNode } from "react";
import { useTranslations } from "next-intl";
import { Card } from "@/components/ui/Card";
import { TextField } from "@/components/ui/TextField";
import { textAreaClass } from "@/components/ui/field-class";
import { BroadcastIcon, ChevronDownIcon, GamepadIcon, PencilIcon, PlusIcon } from "@/components/ui/icons";
import type { ButtonIcon } from "@/components/ui/Button";
import { CHECKOUT_USAGES } from "./checkout-request";
import { ChipRadio, useFieldError, type SectionProps } from "./form-parts";

/**
 * One optional row: a closed button with an icon and the action ("أضف ملاحظة"); it opens its field in place, and shows what
 * was entered while closed. A row whose value is already filled (a restored draft) starts open.
 */
function OptionalRow({ icon: Icon, label, summary, startOpen, children }: {
  icon: ButtonIcon;
  label: string;
  /** What was entered, shown while the row is closed. */
  summary: string | null;
  startOpen: boolean;
  children: ReactNode;
}) {
  const [open, setOpen] = useState(startOpen);
  const panelId = useId();

  return (
    <div className="rounded-card border border-line">
      <button
        type="button"
        aria-expanded={open}
        aria-controls={panelId}
        onClick={() => setOpen(!open)}
        className="press flex min-h-12 w-full items-center gap-3 rounded-card px-3.5 text-start text-sm font-bold transition-colors hover:bg-paper"
      >
        <span aria-hidden="true" className="flex size-8 shrink-0 items-center justify-center rounded-full bg-orange-soft text-orange-text"><Icon size={16} /></span>
        <span className="min-w-0 flex-1">{label}</span>
        {!open && summary ? <span className="max-w-[40%] truncate text-xs font-normal text-ink-muted">{summary}</span> : null}
        <span aria-hidden="true" className={`text-ink-muted transition-transform duration-200 motion-reduce:transition-none ${open ? "rotate-180" : ""}`}>{open ? <ChevronDownIcon size={18} /> : <PlusIcon size={18} />}</span>
      </button>
      {open ? <div id={panelId} className="flex flex-col gap-3 border-t border-line p-3.5">{children}</div> : null}
    </div>
  );
}

/** Notes, "what will you use it for" and "where did you hear about us": all optional, so each is a closed row that opens on demand. */
export function ExtrasSection({ form, set, errors, leadSources }: SectionProps & { leadSources: string[] }) {
  const t = useTranslations("checkout.extras");
  const tc = useTranslations("checkout");
  const error = useFieldError(errors);

  return (
    <Card title={<>{t("title")}<span className="ms-2 align-middle rounded-full bg-paper px-2 py-0.5 text-[11px] font-normal text-ink-muted">{tc("optional")}</span></>} padding="md" className="gap-3">
      <OptionalRow icon={PencilIcon} label={t("addNote")} summary={form.notes.trim() || null} startOpen={form.notes !== ""}>
        <textarea
          name="notes" aria-label={t("notes")} rows={3} value={form.notes} onChange={(event) => set("notes", event.target.value)}
          className={textAreaClass}
        />
      </OptionalRow>
      <OptionalRow icon={GamepadIcon} label={t("usage")} summary={form.usage ? t(`usages.${form.usage}`) : null} startOpen={form.usage !== null}>
        <div role="radiogroup" aria-label={t("usage")} className="flex flex-wrap gap-2">
          {CHECKOUT_USAGES.map((usage) => (
            <ChipRadio
              key={usage} name="usage" label={t(`usages.${usage}`)} selected={form.usage === usage}
              onSelect={() => set("usage", usage)} onClear={() => set("usage", null)}
            />
          ))}
        </div>
        {form.usage === "Other" ? (
          <TextField label={t("usageNote")} name="usageNote" value={form.usageNote} onChange={(v) => set("usageNote", v)} required error={error("usageNote")} />
        ) : null}
      </OptionalRow>
      {leadSources.length > 0 ? (
        <OptionalRow icon={BroadcastIcon} label={t("leadSource")} summary={form.leadSource} startOpen={form.leadSource !== null}>
          <p className="m-0 text-sm text-ink-muted">{t("leadSourceHint")}</p>
          <div role="radiogroup" aria-label={t("leadSource")} className="flex flex-wrap gap-2">
            {leadSources.map((source) => (
              <ChipRadio
                key={source} name="leadSource" label={source} selected={form.leadSource === source}
                onSelect={() => set("leadSource", source)} onClear={() => set("leadSource", null)}
              />
            ))}
          </div>
        </OptionalRow>
      ) : null}
    </Card>
  );
}
