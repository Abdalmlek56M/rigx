"use client";

import { useSyncExternalStore } from "react";
import { useTranslations } from "next-intl";
import { unwrap } from "@rigx/api-client";
import { HeartIcon } from "@/components/ui/icons";
import { EmptyState, LoadBoundary } from "@/features/account/LoadBoundary";
import { ItemCard } from "@/features/catalog/ItemCard";
import { Link } from "@/i18n/navigation";
import { browserApi } from "@/lib/api/browser";
import { required } from "@/lib/api/required";
import { useLoad } from "@/lib/hooks/use-load";
import { favouriteStore } from "./favourite-store";

const fetchFavourites = () => required(unwrap(browserApi().GET("/api/v1/account/favourites")));

/** "المفضلة": the account's hearted items still on sale, as product cards; un-hearting one takes it off the page at once. */
export function FavouritesList() {
  const t = useTranslations("favourites");
  const { state, reload } = useLoad(fetchFavourites);
  const saved = useSyncExternalStore(favouriteStore.subscribe, favouriteStore.getSnapshot, favouriteStore.getServerSnapshot);

  return (
    <div className="flex flex-col gap-4">
      <h2 className="sr-only">{t("title")}</h2>
      <LoadBoundary state={state} reload={reload}>
        {(list) => {
          const items = list.items.filter((item) => saved.includes(item.code));
          return items.length === 0 ? (
            <EmptyState
              icon={HeartIcon} message={t("empty")}
              action={<Link href="/products" className="text-orange-text underline hover:text-orange-text-hover">{t("browse")}</Link>}
            />
          ) : (
            <ul className="m-0 grid list-none grid-cols-1 gap-4 p-0 sm:grid-cols-2 xl:grid-cols-3">
              {items.map((item) => (
                <li key={item.code} className="flex">
                  <ItemCard item={item} />
                </li>
              ))}
            </ul>
          );
        }}
      </LoadBoundary>
    </div>
  );
}
