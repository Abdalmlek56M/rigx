"use client";

import { useTranslations } from "next-intl";
import { ImageSlot } from "../kit/ImageSlot";
import { deleteLogo, uploadLogo, type SettingsView, type StoreLogoKind } from "./settings-api";

/** One logo: preview, upload or change, paste and remove (the settings form keeps the rest of the section). */
export function LogoCard({ kind, storageKey, onSaved }: { kind: StoreLogoKind; storageKey: string | null; onSaved: (view: SettingsView) => void }) {
  const t = useTranslations(`admin.settings.logo.${kind}`);
  return (
    <ImageSlot
      storageKey={storageKey} canManage
      text={{
        title: t("title"), hint: t("hint"), alt: t("alt"), upload: t("upload"), change: t("change"), remove: t("remove"),
        removeTitle: t("removeTitle"), removeBody: t("removeBody"), removed: t("removed"), failed: t("failed"), saved: t("saved")
      }}
      onUpload={async (file) => onSaved(await uploadLogo(kind, file))}
      onRemove={async () => onSaved(await deleteLogo(kind))}
    />
  );
}
