import type { Metadata } from "next";
import { accountMetadata, enterAccountPage, loadAccountContact } from "@/features/account/account-page";
import { OrderDetail } from "@/features/account/OrderDetail";

interface Props {
  params: Promise<{ locale: string; id: string }>;
}

export function generateMetadata(props: Props): Promise<Metadata> {
  return accountMetadata(props, "order");
}

export default async function OrderPage(props: Props) {
  await enterAccountPage(props);
  const { id } = await props.params;
  const contact = await loadAccountContact();
  return <OrderDetail id={id} whatsApp={{ number: contact.number, template: contact.orderTemplate }} pickupAddress={contact.pickupAddress} />;
}
