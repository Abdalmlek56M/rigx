"use client";

import { useEffect, useRef, type ReactNode } from "react";

interface ModalSheetProps {
  open: boolean;
  onClose: () => void;
  label: string;
  /** Desktop edge: `start` or `end` of the reading direction. Mobile is always a bottom sheet unless `fullHeight`. */
  edge: "start" | "end";
  /** Mobile: side panel instead of a bottom sheet. */
  fullHeight?: boolean;
  children: ReactNode;
}

/**
 * A native modal `<dialog>`: focus trap, Escape, inert page and focus return to the opener come from the platform.
 * Page scroll is locked while open.
 */
export function ModalSheet({ open, onClose, label, edge, fullHeight = false, children }: ModalSheetProps) {
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

  const mobile = fullHeight
    ? "inset-y-0 h-full max-h-none w-[min(320px,88vw)]"
    : "inset-x-0 bottom-0 top-auto h-auto max-h-[88dvh] w-full rounded-t-panel";
  const desktop = `md:inset-y-0 md:h-full md:max-h-none md:w-[400px] md:rounded-none ${edge === "end" ? "md:end-0 md:start-auto" : "md:start-0 md:end-auto"}`;
  const mobileEdge = fullHeight ? (edge === "end" ? "end-0 start-auto" : "start-0 end-auto") : "";

  return (
    <dialog
      ref={ref}
      aria-label={label}
      onClose={onClose}
      onClick={(event) => {
        if (event.target === event.currentTarget) onClose();
      }}
      className={`fixed m-0 max-w-none overflow-hidden border-0 bg-surface p-0 text-ink shadow-[0_0_40px_rgba(21,23,27,.25)] backdrop:bg-carbon/50 ${mobile} ${mobileEdge} ${desktop}`}
    >
      {open ? children : null}
    </dialog>
  );
}
