import { CatalogProductEditor } from "@/components/catalog-product-editor";

export default async function EditCatalogProductPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  return <CatalogProductEditor productId={id} />;
}
