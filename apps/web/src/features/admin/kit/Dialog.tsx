"use client";

import { useEffect, useId, useRef, type ReactNode } from "react";

interface DialogProps {
  open: boolean;
  onClose: () => void;
  title: string;
  children: ReactNode;
}

/** A centred native modal `<dialog>` (focus trap, Escape and focus return come from the platform); closes on the backdrop. */
export function Dialog({ open, onClose, title, children }: DialogProps) {
  const ref = useRef<HTMLDialogElement>(null);
  const titleId = useId();

  useEffect(() => {
    const dialog = ref.current;
    if (!dialog) return;
    if (open && !dialog.open) dialog.showModal();
    if (!open && dialog.open) dialog.close();
  }, [open]);

  return (
    <dialog
      ref={ref}
      aria-labelledby={titleId}
      onClose={onClose}
      onClick={(event) => {
        if (event.target === event.currentTarget) onClose();
      }}
      className="m-auto w-[min(440px,calc(100vw-32px))] rounded-panel border-0 bg-surface p-0 text-ink shadow-[0_0_40px_rgba(21,23,27,.25)] backdrop:bg-carbon/50"
    >
      {open ? (
        <div className="flex flex-col gap-4 p-5">
          <h2 id={titleId} className="m-0 text-lg font-bold">{title}</h2>
          {children}
        </div>
      ) : null}
    </dialog>
  );
}
