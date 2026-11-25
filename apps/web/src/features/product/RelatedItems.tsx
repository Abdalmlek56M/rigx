import type { Schemas } from "@rigx/api-client";
import { getTranslations } from "next-intl/server";
import { ItemCard } from "@/features/catalog/ItemCard";
import { ITEM_RAIL_ITEM_CLASS } from "@/features/catalog/item-layout";
import { Rail } from "@/features/home/Rail";
import { Link } from "@/i18n/navigation";

/** The "more like this" rail at the bottom of a product page; hidden when there is nothing to show. */
export async function RelatedItems({ items, moreHref }: { items: Schemas["StoreItemCard"][]; moreHref: string }) {
  if (items.length === 0) return null;
  const t = await getTranslations("product");

  return (
    <Rail
      title={t("relatedTitle")}
      arrowsOnMobile
      action={
        <Link href={moreHref} className="press inline-flex min-h-10 items-center rounded-full px-0 text-sm font-bold text-orange-text hover:text-orange-text-hover">
          {t("relatedMore")}
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
