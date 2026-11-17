import type { Metadata } from "next";
import { BuildsList } from "@/features/account/BuildsList";
import { accountMetadata, enterAccountPage, type AccountPageProps } from "@/features/account/account-page";

export function generateMetadata(props: AccountPageProps): Promise<Metadata> {
  return accountMetadata(props, "builds");
}

export default async function BuildsPage(props: AccountPageProps) {
  await enterAccountPage(props);
  return <BuildsList />;
}
