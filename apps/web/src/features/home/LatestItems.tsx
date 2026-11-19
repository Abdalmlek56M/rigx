import { getTranslations } from "next-intl/server";
import { unwrap, type Schemas } from "@rigx/api-client";
import { ItemCard } from "@/features/catalog/ItemCard";
import { required } from "@/lib/api/required";
import { createServerApi } from "@/lib/api/server";
import type { Condition } from "@/lib/condition";
import { LatestTabs } from "./LatestTabs";

const COUNT = 4;

/** Newest available items; an API failure just means an empty list. */
async function newest(condition: Condition | null): Promise<Schemas["StoreItemCard"][]> {
  try {
    const page = await required(
      unwrap(
        createServerApi().GET("/api/v1/store/items", {
          params: { query: { inStock: true, sort: "newest", pageSize: COUNT, ...(condition ? { condition } : {}) } }
        })
      )
    );
    return page.items;
  } catch {
    return [];
  }
}

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
  const [all, used, fresh] = await Promise.all([newest(null), newest("Used"), newest("New")]);
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
