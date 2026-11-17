import type { Metadata } from "next";
import { RequestsList } from "@/features/account/RequestsList";
import { accountMetadata, enterAccountPage, type AccountPageProps } from "@/features/account/account-page";

export function generateMetadata(props: AccountPageProps): Promise<Metadata> {
  return accountMetadata(props, "requests");
}

export default async function RequestsPage(props: AccountPageProps) {
  await enterAccountPage(props);
  return <RequestsList />;
}
