import { getTranslations } from "next-intl/server";
import type { CategoryNode } from "@/lib/categories";
import type { Brand } from "@/lib/storefront";
import { localizedName } from "@/lib/localized-name";
import type { CatalogState } from "./catalog-query";

/** Page title: the category's name (with the brand), else the brand, else the search text, else the condition, else "all products". */
export async function catalogTitle(state: CatalogState, category: CategoryNode | undefined, brand: Brand | undefined, locale: string): Promise<string> {
  const t = await getTranslations("catalog.title");
  if (category) {
    const name = localizedName(category, locale);
    return brand ? `${name} ${brand.name}` : name;
  }
  if (brand) return brand.name;
  if (state.search) return t("search", { query: state.search });
  if (state.condition) return t(state.condition === "New" ? "new" : "used");
  return t("all");
}
