import { getTranslations } from "next-intl/server";
import { categoryHref } from "@/components/layout/nav-links";
import { ChevronStartIcon } from "@/components/ui/icons";
import { ItemCard } from "@/features/catalog/ItemCard";
import { ITEM_RAIL_ITEM_CLASS } from "@/features/catalog/item-layout";
import { Link } from "@/i18n/navigation";
import { categoryBySlug, type CategoryNode } from "@/lib/categories";
import { newestItems } from "./newest-items";
import { Rail } from "./Rail";

/** The base graphics-cards category (`BaseCatalog` in the Catalog module). */
const GPU_SLUG = "graphics-cards";
const COUNT = 10;

/** "كروت الشاشة": the newest available graphics cards as a draggable rail of product cards; hidden when there are none. */
export async function GpuRail({ tree }: { tree: CategoryNode[] }) {
  const category = categoryBySlug(tree, GPU_SLUG);
  if (!category) return null;
  const items = await newestItems(COUNT, { categoryId: category.id });
  if (items.length === 0) return null;
  const t = await getTranslations("home.gpus");

  return (
    <Rail
      title={t("title")}
      action={
        <Link href={categoryHref(GPU_SLUG)} className="press inline-flex min-h-10 items-center gap-1.5 rounded-full px-2 text-sm font-bold text-orange-text hover:text-orange-text-hover">
          {t("seeAll")}
          <span aria-hidden="true" className="flex ltr:-scale-x-100"><ChevronStartIcon size={16} /></span>
        </Link>
      }
    >
      {items.map((item) => (
        <li key={item.code} className={ITEM_RAIL_ITEM_CLASS}>
          <ItemCard item={item} />
        </li>
      ))}
    </Rail>
  );
}
