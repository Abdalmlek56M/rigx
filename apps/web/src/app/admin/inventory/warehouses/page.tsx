import type { Metadata } from "next";
import { WarehousesAdmin } from "@/components/warehouses-admin";

export const metadata: Metadata = {
  title: "Warehouses | RIGX Admin",
};

export default function AdminWarehousesPage() {
  return <WarehousesAdmin />;
}
