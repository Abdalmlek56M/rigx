"use client";

import { useTranslations } from "next-intl";
import { ImageSlot } from "../kit/ImageSlot";
import { usePermissions } from "../use-permissions";
import { deleteBrandLogo, uploadBrandLogo, type Brand } from "./brands-api";

/** The brand logo: upload, change, paste and remove (the text save keeps the current key). */
export function BrandLogoCard({ brand, onChanged }: { brand: Brand; onChanged: (saved: Brand) => void }) {
  const t = useTranslations("admin.brands.logo");
  const { has } = usePermissions();
  return (
    <ImageSlot
      storageKey={brand.logoStorageKey ?? null} canManage={has("Catalog.ManageMedia")}
      text={{
        title: t("title"), hint: t("hint"), alt: t("alt", { name: brand.name }), upload: t("upload"), change: t("change"), remove: t("remove"),
        removeTitle: t("removeTitle"), removeBody: t("removeBody"), removed: t("removed"), failed: t("failed")
      }}
      onUpload={async (file) => onChanged(await uploadBrandLogo(brand.id, file))}
      onRemove={async () => onChanged(await deleteBrandLogo(brand.id))}
    />
  );
}
