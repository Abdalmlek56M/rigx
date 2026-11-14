"use client";

import { useState } from "react";
import { InterestSheet } from "./InterestSheet";

interface InterestButtonProps {
  /** What the visitor registers interest in; shown in the sheet and sent as the subject. */
  subject: string;
  /** The button text ("سجّل اهتمامك" / "بلغني لما يفتح"). */
  label: string;
  className: string;
}

/** A button that opens the interest sheet for a service that is not open yet. */
export function InterestButton({ subject, label, className }: InterestButtonProps) {
  const [open, setOpen] = useState(false);

  return (
    <>
      <button type="button" aria-haspopup="dialog" onClick={() => setOpen(true)} className={className}>
        {label}
      </button>
      <InterestSheet open={open} onClose={() => setOpen(false)} subject={subject} />
    </>
  );
}
