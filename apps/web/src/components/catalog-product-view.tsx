"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { getCatalogProducts, type CatalogProduct, type CatalogProductSummary } from "@/lib/api-client";
import { useLocale } from "@/lib/i18n";
import { localizedCategoryName } from "@/lib/category-labels";
import { Message } from "@/components/ui";
import { ProductImage } from "@/components/product-image";
import { ProductShareButton, WhatsAppProductButton } from "@/components/product-actions";
import { WishlistButton } from "@/components/wishlist-button";
import { RigxProductCard } from "@/components/rigx-product-card";
import { ProductReviews } from "@/components/product-reviews";
import { useCart } from "@/components/cart-provider";
import { currencyLabel, formatCatalogPrice } from "@/lib/storefront";

export function CatalogProductView({
  product,
  preview = false,
}: {
  product: CatalogProduct;
  preview?: boolean;
}) {
  const { messages, locale } = useLocale();
  const router = useRouter();
  const { addItem } = useCart();
  const categoryName = localizedCategoryName(product.categorySlug, product.categoryName, locale);
  const activeVariants = product.variants.filter((variant) => variant.isActive);
  const [selectedVariantId, setSelectedVariantId] = useState<string | null>(null);
  const [similarProducts, setSimilarProducts] = useState<CatalogProductSummary[]>([]);

  useEffect(() => {
    const active = product.variants.filter((variant) => variant.isActive);
    setSelectedVariantId(active.length === 1 ? active[0].id : null);
    if (preview) {
      setSimilarProducts([]);
      return;
    }
    let activeRequest = true;
    void getCatalogProducts({ category: product.categorySlug, sort: "newest", pageSize: 5 })
      .then((page) => {
        if (activeRequest) setSimilarProducts(page.items.filter((item) => item.id !== product.id).slice(0, 4));
      })
      .catch(() => {
        if (activeRequest) setSimilarProducts([]);
      });
    return () => { activeRequest = false; };
  }, [preview, product.categorySlug, product.id, product.variants]);

  const selectedVariant =
    activeVariants.find((variant) => variant.id === selectedVariantId) ??
    (activeVariants.length === 1 ? activeVariants[0] : null);
  const requiresVariantChoice = activeVariants.length > 1 && selectedVariant === null;
  const effectivePrice = selectedVariant?.price ?? product.basePrice;
  const visibleSpecifications = product.specifications.filter(
    (specification) => specification.variantId === null || specification.variantId === selectedVariant?.id,
  );
  const groups = Object.groupBy(visibleSpecifications, (specification) => specification.groupName);
  const primaryImage = product.images.find((image) => image.isPrimary) ?? product.images[0];
  const cartItem = {
    productId: product.id,
    slug: product.slug,
    name: product.name,
    sku: selectedVariant?.sku ?? product.sku,
    variantId: selectedVariant?.id ?? null,
    variantName: selectedVariant?.name ?? null,
    unitPrice: effectivePrice,
    imageStorageKey: primaryImage?.storageKey ?? null,
    categorySlug: product.categorySlug,
  };

  function buyNow() {
    const outcome = addItem(cartItem);
    if (outcome === "added" || outcome === "incremented") router.push("/checkout");
  }

  return (
    <>
      {preview ? (
        <div className="mb-5">
          <Message tone="info">{messages.adminDraftPreview} — {product.status === "Draft" ? messages.catalogDraft : messages.catalogPublished}</Message>
        </div>
      ) : null}

      <div className="product-page-toolbar">
        <Link className="btn-ghost" href={preview ? `/admin/catalog/products/${product.id}` : "/products"}>
          ‹ {preview ? messages.adminEdit : messages.navProducts}
        </Link>
        <div className="product-breadcrumb">
          <Link href="/products">{messages.navProducts}</Link> <b>/</b>{" "}
          <Link href={`/products?category=${encodeURIComponent(product.categorySlug)}`}>{categoryName}</Link> <b>/</b>{" "}
          <span>{product.name}</span>
        </div>
      </div>

      <div className="product-page-grid">
        <article className="product-page-gallery glass-panel">
          <div className="product-page-image-stage">
            {product.condition === "Used" ? <span className="sample-badge">{messages.catalogConditionUsed}</span> : null}
            <ProductImage product={{ slug: product.slug, sku: product.sku, categorySlug: product.categorySlug, name: product.name }} alt={product.name} loading="eager" />
          </div>
          <div className="product-gallery-caption">
            <span>{product.brandName ?? "RIGX"}</span>
            <span dir="ltr">{selectedVariant?.sku ?? product.sku}</span>
          </div>
        </article>

        <article className="product-page-summary glass-panel">
          <span className="eyebrow">{categoryName.toUpperCase()} / {(product.brandName ?? "RIGX").toUpperCase()}</span>
          <h1>{product.name}</h1>
          <p>{product.shortDescription ?? product.description ?? categoryName}</p>

          {activeVariants.length > 0 ? (
            <div className="product-variant-list" role="group" aria-label={messages.catalogChooseVariant}>
              {activeVariants.map((variant) => (
                <button
                  type="button"
                  key={variant.id}
                  onClick={() => setSelectedVariantId(variant.id)}
                  aria-pressed={selectedVariant?.id === variant.id}
                  className={`product-variant${selectedVariant?.id === variant.id ? " is-selected" : ""}`}
                >
                  <span>{variant.name}</span>
                  <small dir="ltr">{formatCatalogPrice(variant.price ?? product.basePrice)} {currencyLabel(locale)}</small>
                </button>
              ))}
            </div>
          ) : null}

          <div className="product-price-placeholder">
            <b dir="ltr">{formatCatalogPrice(effectivePrice)} {currencyLabel(locale)}</b>
            <small dir="ltr">SKU: {selectedVariant?.sku ?? product.sku}</small>
          </div>

          {requiresVariantChoice ? <p className="inline-note">{messages.catalogChooseVariant}</p> : null}

          <div className="product-page-actions">
            <button className="btn-primary" type="button" onClick={buyNow} disabled={requiresVariantChoice}>
              {messages.buyNow} <b>‹</b>
            </button>
            <button
              className="btn-outline"
              type="button"
              disabled={requiresVariantChoice}
              onClick={() => addItem(cartItem)}
            >
              {messages.cartAdd} <b>+</b>
            </button>
            <ProductShareButton slug={product.slug} name={product.name} sku={product.sku} />
          </div>

          <div className="product-page-service-note">
            <span>{locale === "ar" ? "بيانات المنتج" : "Product data"}</span>
            <span>{locale === "ar" ? "التوافر حسب المخزون" : "Availability per inventory"}</span>
            <span>{locale === "ar" ? "استلام بالاتفاق" : "Pickup by arrangement"}</span>
          </div>

          <div className="product-secondary-actions">
            <WishlistButton
              product={{
                id: product.id, name: product.name, slug: product.slug, sku: product.sku,
                basePrice: product.basePrice, imageStorageKey: primaryImage?.storageKey ?? null,
                categorySlug: product.categorySlug,
              }}
            />
            <WhatsAppProductButton slug={product.slug} name={product.name} sku={selectedVariant?.sku ?? product.sku} />
          </div>

          {product.description ? <p className="product-long-description" dir="auto">{product.description}</p> : null}
        </article>
      </div>

      {visibleSpecifications.length > 0 ? (
        <section className="product-detail-section glass-panel">
          <div className="detail-section-heading">
            <span className="eyebrow">RIGX / SPECIFICATIONS</span>
            <h2>{messages.catalogSpecifications}</h2>
          </div>
          <div className="product-spec-groups">
            {Object.entries(groups).map(([group, specifications]) => (
              <div className="product-spec-group" key={group}>
                <h3>{group}</h3>
                <dl>
                  {specifications?.map((specification) => (
                    <div key={specification.id}>
                      <dt>{specification.name}</dt>
                      <dd dir="auto">{specification.value}</dd>
                    </div>
                  ))}
                </dl>
              </div>
            ))}
          </div>
        </section>
      ) : null}

      {!preview ? <ProductReviews productId={product.id} /> : null}

      {!preview ? (
        <section className="related-section product-detail-section">
          <div className="detail-section-heading">
            <span className="eyebrow">RELATED HARDWARE</span>
            <h2>{messages.similarProducts}</h2>
            <p>{locale === "ar" ? "منتجات أخرى من نفس القسم المنشور." : "More published products from the same department."}</p>
          </div>
          {similarProducts.length > 0 ? (
            <div className="related-products product-grid">
              {similarProducts.map((item) => <RigxProductCard key={item.id} product={item} />)}
            </div>
          ) : null}
          <Link className="text-link" href={`/products?category=${encodeURIComponent(product.categorySlug)}`}>
            {messages.viewAllProducts} <b>‹</b>
          </Link>
        </section>
      ) : null}
    </>
  );
}
