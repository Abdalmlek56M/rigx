import type { Metadata } from "next";
import { WarrantiesList } from "@/features/account/WarrantiesList";
import { accountMetadata, enterAccountPage, loadAccountContact, type AccountPageProps } from "@/features/account/account-page";

export function generateMetadata(props: AccountPageProps): Promise<Metadata> {
  return accountMetadata(props, "warranties");
}

export default async function WarrantiesPage(props: AccountPageProps) {
  await enterAccountPage(props);
  const contact = await loadAccountContact();
  return <WarrantiesList whatsApp={{ number: contact.number, warrantyTemplate: contact.warrantyTemplate }} />;
}
