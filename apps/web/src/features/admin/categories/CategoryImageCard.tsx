"use client";

import { useTranslations } from "next-intl";
import { ImageSlot } from "../kit/ImageSlot";
import { usePermissions } from "../use-permissions";
import { deleteCategoryImage, uploadCategoryImage, type Category } from "./categories-api";

/** The category picture: upload, change, paste and remove (the text save keeps the current key). */
export function CategoryImageCard({ category, onChanged }: { category: Category; onChanged: (saved: Category) => void }) {
  const t = useTranslations("admin.categories.image");
  const { has } = usePermissions();
  return (
    <ImageSlot
      storageKey={category.iconStorageKey ?? null} canManage={has("Catalog.ManageMedia")}
      text={{
        title: t("title"), hint: t("hint"), alt: t("alt", { name: category.name }), upload: t("upload"), change: t("change"), remove: t("remove"),
        removeTitle: t("removeTitle"), removeBody: t("removeBody"), removed: t("removed"), failed: t("failed")
      }}
      onUpload={async (file) => onChanged(await uploadCategoryImage(category.id, file))}
      onRemove={async () => onChanged(await deleteCategoryImage(category.id))}
    />
  );
}
