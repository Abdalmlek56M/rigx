"use client";

import { useEffect, useId, useRef, type ReactNode } from "react";

interface DialogProps {
  open: boolean;
  onClose: () => void;
  title: string;
  children: ReactNode;
  /** A wider dialog for previews and tables. */
  wide?: boolean;
}

/** A centred native modal `<dialog>` (focus trap, Escape and focus return come from the platform); closes on the backdrop. */
export function Dialog({ open, onClose, title, children, wide = false }: DialogProps) {
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
      // Only a close the person made (Escape) reports back; closing because `open` turned false must not, or a flow that swaps
      // one dialog for the next would reset itself.
      onClose={() => {
        if (open) onClose();
      }}
      onClick={(event) => {
        if (event.target === event.currentTarget) onClose();
      }}
      className={`admin-dialog m-auto ${wide ? "w-[min(760px,calc(100vw-32px))]" : "w-[min(440px,calc(100vw-32px))]"} rounded-panel border-0 bg-surface p-0 text-ink shadow-[0_0_40px_rgba(21,23,27,.25)] backdrop:bg-carbon/50`}
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
