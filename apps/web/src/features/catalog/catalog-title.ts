import { getTranslations } from "next-intl/server";
import type { CategoryNode } from "@/lib/categories";
import type { CatalogState } from "./catalog-query";

/** Page title: the category's name, else the condition, else the search text, else "all products". */
export async function catalogTitle(state: CatalogState, category: CategoryNode | undefined): Promise<string> {
  const t = await getTranslations("catalog.title");
  if (category) return category.name;
  if (state.search) return t("search", { query: state.search });
  if (state.condition) return t(state.condition === "New" ? "new" : "used");
  return t("all");
}
