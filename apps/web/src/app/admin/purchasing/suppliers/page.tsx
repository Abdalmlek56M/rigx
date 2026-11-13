import type { Metadata } from "next";
import { SuppliersAdmin } from "@/components/suppliers-admin";

export const metadata: Metadata = {
  title: "Suppliers | RIGX Admin",
};

export default function AdminSuppliersPage() {
  return <SuppliersAdmin />;
}
