import type { Schemas } from "@rigx/api-client";
import { getTranslations } from "next-intl/server";
import { Card } from "@/components/ui/Card";
import { SwapIcon } from "@/components/ui/icons";
import { ItemCard } from "@/features/catalog/ItemCard";
import { ITEM_GRID_CLASS } from "@/features/catalog/item-layout";

/** On a sold-out product: two or three items that are in stock now, so the visitor does not leave the page empty-handed. */
export async function SoldOutAlternatives({ items }: { items: Schemas["StoreItemCard"][] }) {
  if (items.length === 0) return null;
  const t = await getTranslations("product");

  return (
    <section className="shell pb-8">
      <Card title={t("alternatives.title")} icon={SwapIcon} padding="md">
        <p className="m-0 text-sm text-ink-muted">{t("alternatives.lead")}</p>
        <ul className={`m-0 grid list-none gap-3 p-0 md:gap-4 ${ITEM_GRID_CLASS}`}>
          {items.map((item, index) => (
            <li key={item.code} className={`flex ${index === 2 ? "max-md:hidden" : ""}`}>
              <ItemCard item={item} />
            </li>
          ))}
        </ul>
      </Card>
    </section>
  );
}
