"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { unwrap } from "@rigx/api-client";
import { StatusBadge } from "@/components/ui/StatusBadge";
import { AlertIcon, CheckIcon, ClockIcon } from "@/components/ui/icons";
import { browserApi } from "@/lib/api/browser";
import { required } from "@/lib/api/required";
import { moderationNote, parseVisibility, type Moderation, type Visibility } from "./build-visibility";
import { VisibilityPicker } from "./VisibilityPicker";

const NOTE_TONE = { pending: "warning", approved: "success", rejected: "danger" } as const;
const NOTE_ICON = { pending: ClockIcon, approved: CheckIcon, rejected: AlertIcon } as const;

/**
 * The owner's choice for a saved build (private, by link, public) and, for a public one, where the store's decision stands. A change is saved at once;
 * making a build public sends it to the store for approval.
 */
export function BuildVisibilityControl({ token, visibility, moderation }: { token: string; visibility: Visibility; moderation: Moderation | null }) {
  const t = useTranslations("builder.visibility");
  const [state, setState] = useState({ visibility, moderation });
  const [busy, setBusy] = useState(false);
  const [failed, setFailed] = useState(false);
  const note = moderationNote(state.visibility, state.moderation);
  const NoteIcon = note ? NOTE_ICON[note] : null;

  async function choose(next: Visibility) {
    if (next === state.visibility) return;
    setBusy(true);
    setFailed(false);
    try {
      const saved = await required(unwrap(browserApi().PUT("/api/v1/builder/saved/{token}/visibility", { params: { path: { token } }, body: { visibility: next } })));
      setState({ visibility: parseVisibility(saved.visibility), moderation: saved.moderation ?? null });
    } catch {
      setFailed(true);
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="flex flex-col gap-3">
      <VisibilityPicker value={state.visibility} onChange={(next) => void choose(next)} busy={busy} />
      {note && NoteIcon ? <span className="self-start"><StatusBadge tone={NOTE_TONE[note]} icon={NoteIcon}>{t(`moderation.${note}`)}</StatusBadge></span> : null}
      {failed ? <p role="alert" className="m-0 text-xs text-error-text">{t("failed")}</p> : null}
    </div>
  );
}
