import type { Metadata } from "next";
import { AccountAddresses } from "@/components/account-addresses";

export const metadata: Metadata = {
  title: "Addresses | RIGX",
};

export default function AccountAddressesPage() {
  return <AccountAddresses />;
}
