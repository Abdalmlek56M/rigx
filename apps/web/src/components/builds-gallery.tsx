"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { useCart } from "@/components/cart-provider";
import { LoadingCard, Message } from "@/components/ui";
import { getPublishedBuilds, type PublishedBuild } from "@/lib/api-client";
import { useLocale } from "@/lib/i18n";
import { currencyLabel, formatCatalogPrice } from "@/lib/storefront";
import { Icon } from "@/components/icons";

export function BuildsGallery() {
  const { messages, locale } = useLocale();
  const isArabic = locale === "ar";
  const { addItem, openCart } = useCart();
  const [builds, setBuilds] = useState<PublishedBuild[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    void getPublishedBuilds()
      .then(setBuilds)
      .catch(() => setError(messages.buildsLoadError))
      .finally(() => setLoading(false));
  }, [messages.buildsLoadError]);

  function title(build: PublishedBuild) {
    return (locale === "ar" ? build.titleAr : build.titleEn) ?? build.titleEn ?? build.titleAr ?? "RIGX build";
  }

  function add(build: PublishedBuild) {
    const status = addItem({
      productId: build.id,
      assemblyId: build.id,
      slug: `builds/${build.id}`,
      name: title(build),
      sku: "BUILD",
      variantId: null,
      variantName: messages.buildsVariant,
      unitPrice: build.salePrice,
      imageStorageKey: null,
      categorySlug: "builds",
      quantity: 1,
    });
    if (status === "added" || status === "incremented") openCart();
  }

  return (
    <>
      <div className="page-hero builds-hero">
        <div>
          <span className="eyebrow">SYSTEMS / RIGX BUILDS</span>
          <h1>{isArabic ? <>تجميعات <em>جاهزة.</em></> : <>Ready <em>builds.</em></>}</h1>
          <p>
            {isArabic
              ? "تجميعات منشورة من وحدات مخزون RIGX الفعلية، مع المكوّنات والحالة والسعر الرسمي."
              : "Published builds made from actual RIGX inventory units, with components, condition, and official price."}
          </p>
          <div className="hero-ctas">
            <Link className="btn-primary" href="/pc-builder">{isArabic ? "افتح PC Builder" : "Open PC Builder"} <b>‹</b></Link>
            <Link className="btn-ghost" href="/products">{isArabic ? "استعرض القطع" : "Browse components"} <b>‹</b></Link>
          </div>
        </div>
        <div className="builds-hero-art" aria-hidden="true">
          <div className="build-tower build-tower-xl"><span /><span /><span /></div>
          <div className="orbit-ring" />
        </div>
      </div>

      {error ? <Message tone="error">{error}</Message> : null}
      {loading ? <LoadingCard /> : builds.length === 0 ? (
        <div className="order-empty compact-empty">
          <div className="empty-icon">+</div>
          <h3>{isArabic ? "لا تجميعات منشورة" : "No published builds"}</h3>
          <p>{isArabic ? "تُنشر التجميعات بعد تجهيز وحدات المخزون." : "Builds appear once their inventory units are prepared."}</p>
        </div>
      ) : (
        <>
          <p className="inline-note" style={{ marginBottom: 14 }}>
            {isArabic
              ? "الكمية محدودة بحسب الوحدات المتاحة. إضافة التجميع إلى السلة لا تحجز المخزون قبل تأكيد الطلب."
              : "Availability is limited by prepared units. Adding a build to the cart does not reserve stock before the order is confirmed."}
          </p>
          <div className="build-grid builds-list">
            {builds.map((build, index) => (
              <article className={`build-card glass-panel${index === 0 ? " build-card-large" : ""}`} key={build.id}>
                <div className={`build-image ${index === 0 ? "build-image-orange" : index % 2 === 1 ? "build-image-blue" : "build-image-purple"}`}>
                  <div className="build-tower" aria-hidden="true"><span /><span /><span /></div>
                </div>
                <div className="build-content">
                  <div className="build-meta">
                    <span>RIGX / SYSTEM</span>
                    <span>{String(index + 1).padStart(2, "0")} / {String(builds.length).padStart(2, "0")}</span>
                  </div>
                  <h3>{title(build)}</h3>
                  <p>{isArabic ? `${build.units.length} مكوّنات من وحدات مخزون منشورة.` : `${build.units.length} components from published inventory units.`}</p>
                  <div className="build-parts">
                    {build.units.slice(0, 5).map((unit, unitIndex) => <span key={`${unit.name}-${unitIndex}`}>{unit.name}</span>)}
                  </div>
                  <div className="build-price" dir="ltr">{formatCatalogPrice(build.salePrice)} {currencyLabel(locale)}</div>
                  <button type="button" className="btn-outline" onClick={() => add(build)}>
                    <Icon name="shoppingBag" /> {messages.buildsAdd} <b>+</b>
                  </button>
                </div>
              </article>
            ))}
          </div>
        </>
      )}
    </>
  );
}
