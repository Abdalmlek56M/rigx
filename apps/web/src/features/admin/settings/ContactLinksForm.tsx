"use client";

import { useState } from "react";
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
  emptyContactLink,
  type ContactLinksForm as Form
} from "./contact-rules";
import { saveContactLinks } from "./settings-api";
import type { SectionProps } from "./settings-rules";

/** ADM-Settings — contact links: platform, value, where it shows and its display order. */
export function ContactLinksForm({ view, onSaved, onStale }: SectionProps) {
  const t = useTranslations("admin.settings.contactLinks");
  const { submit, save } = useSectionSave(view.rowVersion, onSaved, onStale);
  const [form, setForm] = useState<Form>(() => contactLinksForm(view.contactLinks));

  return (
    <SectionForm
      title={t("title")} hint={t("hint")} busy={submit.busy} error={submit.errors.general} dialog={submit.dialog}
      onSubmit={() => void save((rowVersion) => saveContactLinks(contactLinksValue(form), rowVersion))}
    >
      <ListEditor
        label={t("listLabel")} items={form.links} onChange={(links) => setForm({ links })}
        create={emptyContactLink} addLabel={t("add")} max={20}
        renderItem={(link, update) => (
          <div className="flex min-w-0 flex-col gap-3">
            <div className="grid gap-3 sm:grid-cols-[minmax(0,200px)_minmax(0,1fr)]">
              <SelectField
                label={t("platform")} name="platform" value={link.platform}
                onChange={(platform) => update({ ...link, platform })}
                options={CONTACT_PLATFORMS.map((platform) => ({ value: platform, label: t(`platforms.${platform}`) }))}
              />
              <TextField
                label={contactLinkIsNumber(link.platform) ? t("number") : t("url")} name="value" ltr
                value={link.value} required onChange={(value) => update({ ...link, value })}
                placeholder={contactLinkIsNumber(link.platform) ? "201012345678" : "https://"}
              />
            </div>
            <div className="grid gap-1 sm:grid-cols-3">
              <CheckField label={t("showOnSite")} checked={link.showOnSite} onChange={(showOnSite) => update({ ...link, showOnSite })} />
              <CheckField label={t("showOnInvoice")} checked={link.showOnInvoice} onChange={(showOnInvoice) => update({ ...link, showOnInvoice })} />
              <CheckField label={t("showInFloatingButton")} checked={link.showInFloatingButton} onChange={(showInFloatingButton) => update({ ...link, showInFloatingButton })} />
            </div>
          </div>
        )}
      />
      <p className="m-0 text-xs text-ink-muted">{t("numberHint")}</p>
    </SectionForm>
  );
}
