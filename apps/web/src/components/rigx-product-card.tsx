"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import type { CatalogProductSummary } from "@/lib/api-client";
import { useLocale } from "@/lib/i18n";
import { localizedCategoryName } from "@/lib/category-labels";
import { useCart } from "@/components/cart-provider";
import { WishlistButton } from "@/components/wishlist-button";
import { ProductShareButton } from "@/components/product-actions";
import { ProductImage } from "@/components/product-image";
import { currencyLabel, formatCatalogPrice } from "@/lib/storefront";
import { Icon } from "@/components/icons";

export function RigxProductCard({ product }: { product: CatalogProductSummary }) {
  const { locale, messages } = useLocale();
  const { addItem } = useCart();
  const router = useRouter();
  const detailHref = `/products/${encodeURIComponent(product.slug)}`;
  const hasFromPrice = product.fromPrice < product.basePrice;
  const price = formatCatalogPrice(hasFromPrice ? product.fromPrice : product.basePrice);
  const specs = (product.specifications ?? []).slice(0, 3);

  return (
    <article className="product-card">
      <div
        className="product-image-stage"
        role="button"
        tabIndex={0}
        aria-label={product.name}
        onClick={() => router.push(detailHref)}
        onKeyDown={(event) => {
          if (event.key === "Enter" || event.key === " ") {
            event.preventDefault();
            router.push(detailHref);
          }
        }}
      >
        <span className="product-brand">{(product.brandName ?? "RIGX").toUpperCase()}</span>
        <WishlistButton
          compact
          product={{
            id: product.id,
            name: product.name,
            slug: product.slug,
            sku: product.sku,
            basePrice: product.basePrice,
            imageStorageKey: product.primaryImageStorageKey,
            categorySlug: product.categorySlug,
          }}
        />
        <ProductImage product={product} alt={product.name} />
      </div>
      <div className="product-content">
        <span className="product-category">{localizedCategoryName(product.categorySlug, product.categoryName, locale).toUpperCase()}</span>
        <h3><Link href={detailHref}>{product.name}</Link></h3>
        <p className="product-subtitle" dir="ltr">{product.sku}</p>
        {specs.length > 0 ? (
          <div className="spec-row">
            {specs.map((spec) => (
              <span className="spec-pill" key={`${spec.groupName}-${spec.name}`}>{spec.value}</span>
            ))}
          </div>
        ) : null}
        <div className="product-actions">
          <span className="no-price">
            {hasFromPrice ? messages.catalogFromPrice : ""}{" "}
            <b className="product-price">{price} <small>{currencyLabel(locale)}</small></b>
          </span>
          <div className="product-action-buttons">
            <ProductShareButton slug={product.slug} name={product.name} sku={product.sku} compact />
            {hasFromPrice ? (
              <Link href={detailHref} className="product-add-btn" aria-label={messages.catalogChooseVariant}>
                <Icon name="shoppingBag" />
                <span>{messages.viewProduct}</span>
              </Link>
            ) : (
              <button
                type="button"
                className="product-add-btn"
                onClick={() => addItem({
                  productId: product.id,
                  slug: product.slug,
                  name: product.name,
                  sku: product.sku,
                  variantId: null,
                  variantName: null,
                  unitPrice: product.basePrice,
                  imageStorageKey: product.primaryImageStorageKey,
                  categorySlug: product.categorySlug,
                })}
              >
                <Icon name="shoppingBag" />
                <span>{messages.cartAdd}</span>
              </button>
            )}
          </div>
        </div>
      </div>
    </article>
  );
}
