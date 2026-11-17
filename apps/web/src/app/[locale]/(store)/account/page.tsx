import type { Metadata } from "next";
import { OrdersList } from "@/features/account/OrdersList";
import { accountMetadata, enterAccountPage, type AccountPageProps } from "@/features/account/account-page";

export function generateMetadata(props: AccountPageProps): Promise<Metadata> {
  return accountMetadata(props, "orders");
}

export default async function OrdersPage(props: AccountPageProps) {
  await enterAccountPage(props);
  return <OrdersList />;
}
