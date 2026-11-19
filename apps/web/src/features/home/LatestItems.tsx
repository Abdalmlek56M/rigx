import { getTranslations } from "next-intl/server";
import type { Schemas } from "@rigx/api-client";
import { ItemCard } from "@/features/catalog/ItemCard";
import { LatestTabs } from "./LatestTabs";
import { newestItems } from "./newest-items";

const COUNT = 4;

function Grid({ items }: { items: Schemas["StoreItemCard"][] }) {
  return (
    <ul className="m-0 grid list-none grid-cols-[repeat(auto-fill,minmax(170px,1fr))] gap-3 p-0 md:grid-cols-[repeat(auto-fill,minmax(260px,1fr))] md:gap-[18px]">
      {items.map((item) => (
        <li key={item.code} className="flex">
          <ItemCard item={item} />
        </li>
      ))}
    </ul>
  );
}

/** "وصل حديثًا": the four newest available items, switchable between all, used and new. */
export async function LatestItems() {
  const t = await getTranslations("home.latest");
  const tc = await getTranslations("common.condition");
  const [all, used, fresh] = await Promise.all([newestItems(COUNT), newestItems(COUNT, { condition: "Used" }), newestItems(COUNT, { condition: "New" })]);
  if (all.length === 0) return null;

  return (
    <section className="shell pt-8">
      <LatestTabs
        title={t("title")}
        groupLabel={t("group")}
        tabs={[
          { id: "all", label: t("all"), content: <Grid items={all} /> },
          { id: "used", label: tc("Used"), content: <Grid items={used} /> },
          { id: "new", label: tc("New"), content: <Grid items={fresh} /> }
        ]}
      />
    </section>
  );
}
