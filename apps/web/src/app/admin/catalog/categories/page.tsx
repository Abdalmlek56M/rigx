import type { Metadata } from "next";
import { CatalogTaxonomyWorkspace } from "@/components/catalog-taxonomy-workspace";

export const metadata: Metadata = { title: "Categories | RIGX Admin", robots: { index: false, follow: false } };

export default function AdminCategoriesPage() {
  return <CatalogTaxonomyWorkspace initialTab="category" />;
}
