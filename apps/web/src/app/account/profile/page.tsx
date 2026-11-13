import type { Metadata } from "next";
import { AccountProfile } from "@/components/account-profile";

export const metadata: Metadata = {
  title: "Profile | RIGX",
};

export default function AccountProfilePage() {
  return <AccountProfile />;
}
