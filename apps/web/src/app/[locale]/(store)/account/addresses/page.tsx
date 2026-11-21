import type { Metadata } from "next";
import { loadShippingRates } from "@/features/checkout/load-shipping-rates";
import { AddressesPanel } from "@/features/account/AddressesPanel";
import { accountMetadata, enterAccountPage, type AccountPageProps } from "@/features/account/account-page";

export function generateMetadata(props: AccountPageProps): Promise<Metadata> {
  return accountMetadata(props, "addresses");
}

export default async function AddressesPage(props: AccountPageProps) {
  await enterAccountPage(props);
  const rates = await loadShippingRates();
  return <AddressesPanel governorates={rates?.governorates ?? []} />;
}
