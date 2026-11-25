"use client";

import { useSyncExternalStore } from "react";
import { useTranslations } from "next-intl";
import { unwrap } from "@rigx/api-client";
import { ButtonLink } from "@/components/ui/Button";
import { HeartIcon } from "@/components/ui/icons";
import { EmptyState, LoadBoundary } from "@/features/account/LoadBoundary";
import { BuildCard } from "@/features/builds/BuildCard";
import { CommunityBuildCard } from "@/features/community/CommunityBuildCard";
import { ItemCard } from "@/features/catalog/ItemCard";
import { ITEM_GRID_CLASS } from "@/features/catalog/item-layout";
import { browserApi } from "@/lib/api/browser";
import { required } from "@/lib/api/required";
import { useLoad } from "@/lib/hooks/use-load";
import { favouriteStore } from "./favourite-store";

const fetchFavourites = () => required(unwrap(browserApi().GET("/api/v1/account/favourites")));

/** "المفضلة": the account's hearted items and builds still on sale; un-hearting one takes it off the page at once. */
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
          const builds = list.builds.filter((build) => saved.includes(build.code));
          const shared = list.sharedBuilds;
          if (items.length === 0 && builds.length === 0 && shared.length === 0) {
            return (
              <EmptyState
                icon={HeartIcon} message={t("empty")}
                action={<ButtonLink href="/products" variant="secondary" size="sm">{t("browse")}</ButtonLink>}
              />
            );
          }
          return (
            <div className="flex flex-col gap-6">
              {items.length > 0 ? (
                <ul className={`m-0 grid list-none gap-3 p-0 md:gap-4 ${ITEM_GRID_CLASS}`}>
                  {items.map((item) => (
                    <li key={item.code} className="flex">
                      <ItemCard item={item} />
                    </li>
                  ))}
                </ul>
              ) : null}
              {builds.length > 0 ? (
                <div className="flex flex-col gap-3">
                  {items.length > 0 ? <h2 className="m-0 type-section-title">{t("buildsTitle")}</h2> : null}
                  <ul className="m-0 grid list-none grid-cols-1 gap-4 p-0 xl:grid-cols-2">
                    {builds.map((build) => (
                      <li key={build.code} className="flex">
                        <BuildCard build={build} />
                      </li>
                    ))}
                  </ul>
                </div>
              ) : null}
              {shared.length > 0 ? (
                <div className="flex flex-col gap-3">
                  <h2 className="m-0 type-section-title">{t("sharedTitle")}</h2>
                  <ul className="m-0 grid list-none grid-cols-1 gap-4 p-0 xl:grid-cols-2">
                    {shared.map((build) => (
                      <li key={build.token} className="flex">
                        <CommunityBuildCard build={build} />
                      </li>
                    ))}
                  </ul>
                </div>
              ) : null}
            </div>
          );
        }}
      </LoadBoundary>
    </div>
  );
}
