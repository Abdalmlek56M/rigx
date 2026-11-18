"use client";

import type { ReactNode } from "react";
import { useTranslations } from "next-intl";
import { isConflict } from "@/lib/api/conflict";
import { FormMessage } from "../kit/PageHeader";
import { useToast } from "../kit/Toast";
import { useSubmit } from "../kit/use-submit";
import type { SettingsView } from "./settings-rules";

interface SectionFormProps {
  title: string;
  hint: string;
  onSubmit: () => void;
  busy: boolean;
  error: string | null;
  dialog?: ReactNode;
  children: ReactNode;
}

/** The shared frame of every settings section: title, hint, the fields, the server message and one save button. */
export function SectionForm({ title, hint, onSubmit, busy, error, dialog, children }: SectionFormProps) {
  const t = useTranslations("admin.settings");
  return (
    <form
      className="flex min-w-0 flex-col gap-4"
      onSubmit={(event) => {
        event.preventDefault();
        onSubmit();
      }}
    >
      <div className="flex flex-col gap-1">
        <h2 className="m-0 text-lg font-bold">{title}</h2>
        <p className="m-0 text-sm text-ink-muted">{hint}</p>
      </div>
      {children}
      <FormMessage message={error} />
      <div>
        <button type="submit" disabled={busy} className="h-11 rounded-control bg-carbon px-5 font-bold text-on-carbon disabled:opacity-50">
          {t("save")}
        </button>
      </div>
      {dialog}
    </form>
  );
}

/**
 * A section's save: the API returns the whole settings view (with the next rowVersion), so it replaces what the screen holds.
 * A 409 means another user saved first: show the server message and reload every form.
 */
export function useSectionSave(rowVersion: number, onSaved: (view: SettingsView) => void, onStale: () => void) {
  const submit = useSubmit();
  const toast = useToast();
  const t = useTranslations("admin.settings");

  async function save(action: (rowVersion: number) => Promise<SettingsView>) {
    await submit.run(async () => {
      try {
        onSaved(await action(rowVersion));
      } catch (error) {
        if (isConflict(error)) {
          toast(error instanceof Error ? error.message : t("conflict"), "error");
          onStale();
        }
        throw error;
      }
    }, t("saved"));
  }

  return { submit, save };
}
