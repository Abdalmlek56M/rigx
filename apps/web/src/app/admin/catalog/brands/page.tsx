import type { Metadata } from "next";
import { CatalogTaxonomyWorkspace } from "@/components/catalog-taxonomy-workspace";

export const metadata: Metadata = { title: "Brands | RIGX Admin", robots: { index: false, follow: false } };

export default function AdminBrandsPage() {
  return <CatalogTaxonomyWorkspace initialTab="brand" />;
}
