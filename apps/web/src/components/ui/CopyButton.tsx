"use client";

import { useState } from "react";
import { CheckIcon, CopyIcon } from "./icons";
import { IconButton } from "./Button";

interface CopyButtonProps {
  value: string;
  /** The accessible name and hover tip, for example "نسخ رقم الهاتف". */
  label: string;
  /** The tip after a successful copy. */
  copiedLabel: string;
}

/** Copies a code, phone or e-mail with one icon press and confirms it on the icon for a moment. */
export function CopyButton({ value, label, copiedLabel }: CopyButtonProps) {
  const [copied, setCopied] = useState(false);

  async function copy(): Promise<void> {
    try {
      await navigator.clipboard.writeText(value);
      setCopied(true);
      window.setTimeout(() => setCopied(false), 1600);
    } catch {
      // The clipboard can be blocked (older browser, no permission): the value stays selectable in the page.
    }
  }

  return (
    <IconButton
      icon={copied ? CheckIcon : CopyIcon}
      label={copied ? copiedLabel : label}
      size="sm"
      variant="ghost"
      onClick={() => void copy()}
    />
  );
}
