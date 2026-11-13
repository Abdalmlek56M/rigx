import type { Metadata } from "next";
import { IdentityUsersAdmin } from "@/components/identity-users-admin";

export const metadata: Metadata = {
  title: "Users | RIGX Admin",
};

export default function AdminIdentityUsersPage() {
  return <IdentityUsersAdmin />;
}
