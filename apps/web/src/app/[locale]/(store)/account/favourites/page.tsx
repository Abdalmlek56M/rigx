import type { Metadata } from "next";
import { accountMetadata, enterAccountPage, type AccountPageProps } from "@/features/account/account-page";
import { FavouritesList } from "@/features/favourites/FavouritesList";

export function generateMetadata(props: AccountPageProps): Promise<Metadata> {
  return accountMetadata(props, "favourites");
}

export default async function FavouritesPage(props: AccountPageProps) {
  await enterAccountPage(props);
  return <FavouritesList />;
}
