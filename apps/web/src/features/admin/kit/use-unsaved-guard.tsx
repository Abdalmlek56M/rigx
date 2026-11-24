"use client";

import { useCallback, useEffect, useState, type ReactNode } from "react";
import { useRouter } from "next/navigation";
import { useTranslations } from "next-intl";
import { ConfirmDialog } from "./ConfirmDialog";
import { leavingTarget } from "./unsaved-rules";

/**
 * Keeps a screen's unsaved edits from being lost by accident. While `dirty`: closing or reloading the tab asks the browser's own question,
 * a click on a link to another page of the site opens a "leave without saving?" dialog first, and `guard(action)` does the same for a
 * change the screen makes itself (another section of the same page). Render `dialog` once.
 */
export function useUnsavedGuard(dirty: boolean): { dialog: ReactNode; guard: (action: () => void) => void } {
  const t = useTranslations("admin.kit.unsaved");
  const router = useRouter();
  const [pending, setPending] = useState<(() => void) | null>(null);

  useEffect(() => {
    if (!dirty) return;
    const beforeUnload = (event: BeforeUnloadEvent) => {
      event.preventDefault();
      event.returnValue = "";
    };
    const onClick = (event: MouseEvent) => {
      const anchor = event.target instanceof Element ? event.target.closest("a[href]") : null;
      if (!(anchor instanceof HTMLAnchorElement)) return;
      const target = leavingTarget(event, { href: anchor.href, target: anchor.getAttribute("target"), download: anchor.hasAttribute("download") }, window.location);
      if (target === null) return;
      event.preventDefault();
      event.stopPropagation();
      setPending(() => () => router.push(target));
    };
    window.addEventListener("beforeunload", beforeUnload);
    document.addEventListener("click", onClick, true);
    return () => {
      window.removeEventListener("beforeunload", beforeUnload);
      document.removeEventListener("click", onClick, true);
    };
  }, [dirty, router]);

  const guard = useCallback((action: () => void) => {
    if (dirty) setPending(() => action);
    else action();
  }, [dirty]);

  const dialog = (
    <ConfirmDialog
      open={pending !== null} title={t("title")} body={t("body")} confirmLabel={t("leave")} danger
      onClose={() => setPending(null)}
      onConfirm={async () => pending?.()}
    />
  );
  return { dialog, guard };
}
