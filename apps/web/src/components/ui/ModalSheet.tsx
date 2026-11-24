"use client";

import { useEffect, useRef, type ReactNode } from "react";

interface ModalSheetProps {
  open: boolean;
  onClose: () => void;
  label: string;
  /**
   * Desktop edge: `start` or `end` of the reading direction (mobile is then a bottom sheet unless `fullHeight`),
   * or `center` for a window in the middle of the screen on every size (a confirmation, a quick look).
   */
  edge: "start" | "end" | "center";
  /** Mobile: side panel instead of a bottom sheet. */
  fullHeight?: boolean;
  /** A roomy panel for a detail view (about 760 px on desktop, the whole screen on a phone) instead of the 400 px default. */
  wide?: boolean;
  children: ReactNode;
}

/**
 * A native modal `<dialog>`: focus trap, Escape, inert page and focus return to the opener come from the platform.
 * Page scroll is locked while open.
 */
export function ModalSheet({ open, onClose, label, edge, fullHeight = false, wide = false, children }: ModalSheetProps) {
  const ref = useRef<HTMLDialogElement>(null);

  useEffect(() => {
    const dialog = ref.current;
    if (!dialog) return;
    if (open && !dialog.open) dialog.showModal();
    if (!open && dialog.open) dialog.close();
    document.body.style.overflow = open ? "hidden" : "";
    return () => {
      document.body.style.overflow = "";
    };
  }, [open]);

  const centered = edge === "center";
  const mobile = fullHeight
    ? `inset-y-0 h-full max-h-none ${wide ? "w-full" : "w-[min(320px,88vw)]"}`
    : "inset-x-0 bottom-0 top-auto h-auto max-h-[88dvh] w-full rounded-t-panel pb-[env(safe-area-inset-bottom)] md:pb-0";
  const desktop = `md:inset-y-0 md:h-full md:max-h-none ${wide ? "md:w-[min(760px,92vw)]" : "md:w-[400px]"} md:rounded-none ${edge === "end" ? "md:end-0 md:start-auto" : "md:start-0 md:end-auto"}`;
  const mobileEdge = fullHeight ? (edge === "end" ? "end-0 start-auto" : "start-0 end-auto") : "";
  const placement = centered
    ? `m-auto h-fit max-h-[88dvh] rounded-panel ${wide ? "w-[min(760px,calc(100vw-32px))]" : "w-[min(440px,calc(100vw-32px))]"}`
    : `m-0 ${mobile} ${mobileEdge} ${desktop}`;

  return (
    <dialog
      ref={ref}
      aria-label={label}
      data-sheet={centered ? "center" : fullHeight ? "side" : "bottom"}
      data-edge={edge}
      onClose={onClose}
      onClick={(event) => {
        if (event.target === event.currentTarget) onClose();
      }}
      className={`fixed max-w-none overflow-hidden border-0 bg-surface p-0 text-ink shadow-[0_0_40px_rgba(21,23,27,.25)] backdrop:bg-black/50 ${placement}`}
    >
      {open ? children : null}
    </dialog>
  );
}
