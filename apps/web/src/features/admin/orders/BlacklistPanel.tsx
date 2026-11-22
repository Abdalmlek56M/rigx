"use client";

import { useCallback, useState } from "react";
import { useTranslations } from "next-intl";
import { useLoad } from "@/lib/hooks/use-load";
import { ConfirmDialog } from "../kit/ConfirmDialog";
import { useToast } from "../kit/Toast";
import { useUndoDelete } from "../system/use-undo-delete";
import { blacklistTitle, maskPhone } from "./blacklist-labels";
import { fetchBlacklist, removeFromBlacklist, type BlacklistEntry } from "./orders-api";
import { RefreshIcon, TrashIcon } from "@/components/ui/icons";
import { Button } from "../kit/Button";

/** The blacklisted customers: recognised by phone, email or address; staff with approval rights can remove one. Entries are added from an order. */
export function BlacklistPanel({ reloadKey, canManage, onChanged }: { reloadKey: number; canManage: boolean; onChanged: () => void }) {
  const t = useTranslations("admin.orders.blacklist");
  const toast = useToast();
  const undoDelete = useUndoDelete();
  // A new key is a new load: the list reads again after an entry was added on an order.
  const { state, reload } = useLoad(useCallback(() => fetchBlacklist().then((entries) => ({ key: reloadKey, entries })), [reloadKey]));
  const [removing, setRemoving] = useState<BlacklistEntry | null>(null);

  return (
    <section className="flex flex-col gap-3 rounded-card border border-line bg-surface p-5">
      <h2 className="m-0 text-[17px] font-bold">{t("title")}</h2>
      <p className="m-0 text-sm text-ink-muted">{t("note")}</p>
      {state.status === "loading" ? <div role="status" className="h-16 animate-pulse rounded-control bg-line" /> : null}
      {state.status === "error" ? (
        <div role="alert" className="flex flex-wrap items-center gap-3">
          <p className="m-0 font-bold">{t("failed")}</p>
          <Button variant="primary" icon={RefreshIcon} onClick={reload}>{t("retry")}</Button>
        </div>
      ) : null}
      {state.status === "ready" && state.data.entries.length === 0 ? <p className="m-0 font-bold text-ink-muted">{t("empty")}</p> : null}
      {state.status === "ready" ? (
        <ul className="m-0 flex list-none flex-col p-0">
          {state.data.entries.map((entry) => (
            <li key={entry.id} className="flex flex-wrap items-center justify-between gap-3 border-t border-line py-3 first:border-t-0">
              <div className="flex min-w-0 flex-col gap-0.5">
                <span className="flex flex-wrap items-center gap-2 font-bold">
                  <span dir="auto">{blacklistTitle(entry) ?? t("unnamed")}</span>
                  {entry.phoneNumber ? <span dir="ltr" className="font-mono text-xs font-normal text-ink-muted">{maskPhone(entry.phoneNumber)}</span> : null}
                </span>
                <span dir="auto" className="text-sm text-ink-muted">{entry.reason}</span>
              </div>
              {canManage ? <Button icon={TrashIcon} onClick={() => setRemoving(entry)}>{t("remove")}</Button> : null}
            </li>
          ))}
        </ul>
      ) : null}
      <ConfirmDialog
        open={removing !== null} title={t("removeTitle", { name: removing ? blacklistTitle(removing) ?? t("unnamed") : "" })} body={t("removeBody")}
        confirmLabel={t("remove")} danger onClose={() => setRemoving(null)}
        onConfirm={async () => {
          if (!removing) return;
          try {
            await removeFromBlacklist(removing.id);
            undoDelete({ kind: "blacklist", id: removing.id, message: t("removed"), onRestored: onChanged });
            onChanged();
          } catch (error) {
            toast(t("removeFailed"), "error");
            throw error;
          }
        }}
      />
    </section>
  );
}
