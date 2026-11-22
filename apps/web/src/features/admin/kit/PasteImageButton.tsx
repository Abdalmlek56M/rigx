"use client";

import { useSyncExternalStore } from "react";
import { useTranslations } from "next-intl";
import { ClipboardIcon } from "@/components/ui/icons";
import { IconButton } from "./Button";
import { readClipboardImage } from "./clipboard-image";
import { useToast } from "./Toast";

const noSubscription = () => () => undefined;
const browserCanRead = () => typeof navigator.clipboard?.read === "function";

/** A button that reads the picture on the clipboard (a phone has no Ctrl+V); shown only where the browser can read the clipboard. */
export function PasteImageButton({ onImage, disabled = false }: { onImage: (file: File) => void; disabled?: boolean }) {
  const t = useTranslations("admin.kit.paste");
  const toast = useToast();
  const canRead = useSyncExternalStore(noSubscription, browserCanRead, () => false);
  if (!canRead) return null;

  async function paste() {
    try {
      const file = await readClipboardImage();
      if (file) onImage(file);
      else toast(t("empty"), "info");
    } catch {
      toast(t("denied"), "error");
    }
  }

  return <IconButton icon={ClipboardIcon} label={t("button")} variant="secondary" disabled={disabled} onClick={() => void paste()} />;
}
