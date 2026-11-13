import type { Metadata } from "next";
import { AccountSessions } from "@/components/account-sessions";

export const metadata: Metadata = {
  title: "Sessions | RIGX",
};

export default function AccountSessionsPage() {
  return <AccountSessions />;
}
