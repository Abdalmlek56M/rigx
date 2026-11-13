import type { Metadata } from "next";
import { SupplierProductsAdmin } from "@/components/supplier-products-admin";

export const metadata: Metadata = {
  title: "Supplier products | RIGX Admin",
};

export default async function AdminSupplierProductsPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  return <SupplierProductsAdmin supplierId={id} />;
}
