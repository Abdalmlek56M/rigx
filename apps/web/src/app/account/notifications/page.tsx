import type { Metadata } from "next";
import { AccountNotifications } from "@/components/account-notifications";

export const metadata: Metadata = { title: "Notifications | RIGX", robots: { index: false, follow: false } };

export default function AccountNotificationsPage() {
  return <AccountNotifications />;
}
