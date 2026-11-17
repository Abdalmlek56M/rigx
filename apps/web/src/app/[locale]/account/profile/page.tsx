import type { Metadata } from "next";
import { ProfilePanel } from "@/features/account/ProfilePanel";
import { accountMetadata, enterAccountPage, type AccountPageProps } from "@/features/account/account-page";

export function generateMetadata(props: AccountPageProps): Promise<Metadata> {
  return accountMetadata(props, "profile");
}

export default async function ProfilePage(props: AccountPageProps) {
  await enterAccountPage(props);
  return <ProfilePanel />;
}
