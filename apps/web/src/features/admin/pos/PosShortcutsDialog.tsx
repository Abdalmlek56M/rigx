"use client";

import { useTranslations } from "next-intl";
import { Dialog } from "../kit/Dialog";
import { POS_SHORTCUT_KEYS } from "./pos-shortcuts";

/** The list of point-of-sale shortcuts (opened with the keyboard button or the ? key). */
export function PosShortcutsDialog({ open, onClose }: { open: boolean; onClose: () => void }) {
  const t = useTranslations("admin.pos.shortcuts");
  return (
    <Dialog open={open} onClose={onClose} title={t("title")}>
      <dl className="m-0 flex flex-col">
        {POS_SHORTCUT_KEYS.map(({ shortcut, keys }) => (
          <div key={shortcut} className="flex items-center justify-between gap-3 border-t border-line py-2.5 first:border-t-0">
            <dt className="text-sm">{t(`actions.${shortcut}`)}</dt>
            <dd dir="ltr" className="m-0 flex shrink-0 items-center gap-1">
              {keys.map((key) => <kbd key={key} className="rounded-sm border border-line bg-paper px-1.5 py-0.5 font-mono text-xs">{key}</kbd>)}
            </dd>
          </div>
        ))}
      </dl>
      <p className="m-0 text-xs text-ink-muted">{t("scanHint")}</p>
    </Dialog>
  );
}
