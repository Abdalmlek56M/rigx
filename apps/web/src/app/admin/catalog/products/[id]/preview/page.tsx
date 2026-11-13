import { CatalogPreview } from "@/components/catalog-preview";

export default async function CatalogProductPreviewPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  return <CatalogPreview productId={id} />;
}
