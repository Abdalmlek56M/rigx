"use client";

import { useTranslations } from "next-intl";
import { useToast, useUndoToast } from "../kit/Toast";
import { usePermissions } from "../use-permissions";
import { restoreTrash } from "./system-api";

interface DeletedEntry {
  /** The trash kind the delete filed it under (`TRASH_KINDS`). */
  kind: string;
  id: string;
  /** What the toast says ("تم الحذف"). */
  message: string;
  /** Reads the list again after the entry came back. */
  onRestored: () => void;
}

/**
 * After a delete that goes to the trash: the toast offers "تراجع", which restores the entry. Restoring needs `System.Trash`, so a person
 * without it gets the plain toast (the entry still waits in the trash for someone who has it).
 */
export function useUndoDelete(): (entry: DeletedEntry) => void {
  const t = useTranslations("admin.kit.undo");
  const { has } = usePermissions();
  const toast = useToast();
  const undoToast = useUndoToast();

  return ({ kind, id, message, onRestored }) => {
    if (!has("System.Trash")) {
      toast(message);
      return;
    }
    undoToast(message, async () => {
      try {
        await restoreTrash(kind, id);
        toast(t("restored"));
        onRestored();
      } catch {
        toast(t("failed"), "error");
      }
    });
  };
}
