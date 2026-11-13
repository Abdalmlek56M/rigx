import type { Metadata } from "next";
import { IdentityRolesAdmin } from "@/components/identity-roles-admin";

export const metadata: Metadata = {
  title: "Roles and permissions | RIGX Admin",
};

export default function AdminIdentityRolesPage() {
  return <IdentityRolesAdmin />;
}
