"use client";

import { useTranslations } from "next-intl";
import { SelectField } from "@/components/ui/SelectField";
import { TextField } from "@/components/ui/TextField";
import { CheckField } from "./CheckField";
import { ListEditor } from "./ListEditor";
import { SectionForm, useSectionSave } from "./SectionForm";
import {
  CONTACT_PLATFORMS,
  contactLinkIsNumber,
  contactLinksForm,
  contactLinksValue,
  emptyContactLink
} from "./contact-rules";
import { saveContactLinks } from "./settings-api";
import type { SectionProps } from "./settings-rules";
import { useSectionDraft } from "./use-section-draft";

const ROW = "md:grid-cols-[200px_minmax(0,1fr)]";

/** ADM-Settings — contact links: platform and value on one line, where it shows (site, invoice, floating button) under the chevron. */
export function ContactLinksForm({ view, onSaved, onStale }: SectionProps) {
  const t = useTranslations("admin.settings.contactLinks");
  const { submit, save } = useSectionSave(view.rowVersion, onSaved, onStale);
  const { form, setForm, dirty, reset } = useSectionDraft(contactLinksForm(view.contactLinks));

  return (
    <SectionForm
      title={t("title")} hint={t("hint")} dirty={dirty} onReset={reset} busy={submit.busy} error={submit.errors.general} dialog={submit.dialog}
      onSubmit={() => void save((rowVersion) => saveContactLinks(contactLinksValue(form), rowVersion))}
    >
      <ListEditor
        label={t("listLabel")} items={form.links} onChange={(links) => setForm({ links })}
        create={emptyContactLink} addLabel={t("add")} max={20}
        columns={{ labels: [t("platform"), t("value")], className: ROW }}
        renderItem={(link, update) => (
          <div className={`grid gap-2 ${ROW}`}>
            <SelectField
              label={t("platform")} name="platform" value={link.platform} labelClassName="md:sr-only"
              onChange={(platform) => update({ ...link, platform })}
              options={CONTACT_PLATFORMS.map((platform) => ({ value: platform, label: t(`platforms.${platform}`) }))}
            />
            <TextField
              label={contactLinkIsNumber(link.platform) ? t("number") : t("url")} name="value" ltr labelClassName="md:sr-only"
              value={link.value} required onChange={(value) => update({ ...link, value })}
              placeholder={contactLinkIsNumber(link.platform) ? "201012345678" : "https://"}
            />
          </div>
        )}
        renderDetails={(link, update) => (
          <div className="grid gap-1 sm:grid-cols-3">
            <CheckField label={t("showOnSite")} checked={link.showOnSite} onChange={(showOnSite) => update({ ...link, showOnSite })} />
            <CheckField label={t("showOnInvoice")} checked={link.showOnInvoice} onChange={(showOnInvoice) => update({ ...link, showOnInvoice })} />
            <CheckField label={t("showInFloatingButton")} checked={link.showInFloatingButton} onChange={(showInFloatingButton) => update({ ...link, showInFloatingButton })} />
          </div>
        )}
      />
      <p className="m-0 text-xs text-ink-muted">{t("numberHint")}</p>
    </SectionForm>
  );
}
