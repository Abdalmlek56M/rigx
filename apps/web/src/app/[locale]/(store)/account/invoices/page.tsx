import type { Metadata } from "next";
import { InvoicesList } from "@/features/account/InvoicesList";
import { accountMetadata, enterAccountPage, type AccountPageProps } from "@/features/account/account-page";

export function generateMetadata(props: AccountPageProps): Promise<Metadata> {
  return accountMetadata(props, "invoices");
}

export default async function InvoicesPage(props: AccountPageProps) {
  await enterAccountPage(props);
  return <InvoicesList />;
}
