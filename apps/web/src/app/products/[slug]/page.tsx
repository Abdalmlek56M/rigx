import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { catalogMediaUrl } from "@/lib/api-client";
import { getCatalogProductServer, siteUrl } from "@/lib/server-catalog";
import { CatalogProductView } from "@/components/catalog-product-view";

type ProductPageProps = { params: Promise<{ slug: string }> };

export async function generateMetadata({ params }: ProductPageProps): Promise<Metadata> {
  const { slug } = await params;
  const product = await getCatalogProductServer(slug).catch(() => null);
  if (!product) {
    return {
      title: "المنتج غير متاح | RIGX",
      robots: { index: false, follow: false },
    };
  }

  const path = `/products/${encodeURIComponent(product.slug)}`;
  const primaryImage = product.images.find((image) => image.isPrimary) ?? product.images[0];
  const imageUrl = primaryImage ? `${siteUrl()}/api/v1/catalog/media/${primaryImage.storageKey.split("/").map(encodeURIComponent).join("/")}` : undefined;
  const description = product.shortDescription ?? product.description?.slice(0, 160) ?? `${product.name} من ${product.categoryName} في كتالوج RIGX.`;

  return {
    title: `${product.name} | RIGX`,
    description,
    alternates: { canonical: path },
    robots: { index: true, follow: true },
    openGraph: {
      title: `${product.name} | RIGX`,
      description,
      type: "website",
      url: path,
      siteName: "RIGX",
      images: imageUrl ? [{ url: imageUrl, alt: primaryImage?.altText || product.name }] : undefined,
    },
    twitter: {
      card: "summary_large_image",
      title: `${product.name} | RIGX`,
      description,
      images: imageUrl ? [imageUrl] : undefined,
    },
  };
}

export default async function ProductDetailsPage({ params }: ProductPageProps) {
  const { slug } = await params;
  const product = await getCatalogProductServer(slug);
  if (!product) notFound();

  const primaryImage = product.images.find((image) => image.isPrimary) ?? product.images[0];
  const effectivePrice = product.variants
    .filter((variant) => variant.isActive && variant.price !== null)
    .reduce((minimum, variant) => Math.min(minimum, variant.price ?? minimum), product.basePrice);
  const productUrl = `${siteUrl()}/products/${encodeURIComponent(product.slug)}`;
  const structuredData = {
    "@context": "https://schema.org",
    "@type": "Product",
    name: product.name,
    sku: product.sku,
    description: product.description ?? product.shortDescription ?? undefined,
    category: product.categoryName,
    brand: product.brandName ? { "@type": "Brand", name: product.brandName } : undefined,
    image: primaryImage
      ? `${siteUrl()}${catalogMediaUrl(primaryImage.storageKey)}`
      : undefined,
    offers: {
      "@type": "Offer",
      priceCurrency: "EGP",
      price: effectivePrice,
      url: productUrl,
    },
  };

  return (
    <main className="mx-auto w-full max-w-6xl px-5 py-10 sm:px-8 sm:py-16">
      <script
        type="application/ld+json"
        dangerouslySetInnerHTML={{ __html: JSON.stringify(structuredData).replace(/</g, "\\u003c") }}
      />
      <CatalogProductView product={product} />
    </main>
  );
}
