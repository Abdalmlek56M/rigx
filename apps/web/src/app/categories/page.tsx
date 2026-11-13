"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { getCatalogCategories, type CatalogCategory } from "@/lib/api-client";
import { apiErrorMessage } from "@/lib/form-errors";
import { useLocale } from "@/lib/i18n";
import { localizedCategoryName, technicalCategoryName } from "@/lib/category-labels";
import { LoadingCard, Message, SecondaryButton } from "@/components/ui";
import { Icon, type IconName } from "@/components/icons";

const icons: Record<string, IconName> = {
  "graphics-cards": "gpu",
  processors: "cpu",
  motherboards: "motherboard",
  memory: "ram",
  storage: "hardDrive",
  "power-supplies": "power",
  cooling: "fan",
  cases: "pcCase",
  peripherals: "peripherals",
};

export default function CategoriesPage() {
  const { messages, locale } = useLocale();
  const isArabic = locale === "ar";
  const [categories, setCategories] = useState<CatalogCategory[] | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [attempt, setAttempt] = useState(0);

  useEffect(() => {
    let active = true;
    setError(null);
    void getCatalogCategories()
      .then((items) => { if (active) setCategories(items.filter((item) => item.isActive && item.isEffectivelyActive)); })
      .catch((requestError) => { if (active) setError(apiErrorMessage(requestError, messages)); });
    return () => { active = false; };
  }, [attempt, messages]);

  if (!categories && !error) return <LoadingCard />;

  return (
    <>
      <div className="page-hero compact-hero">
        <div>
          <span className="eyebrow">HARDWARE TAXONOMY / RIGX</span>
          <h1>{isArabic ? <>كل <em>الأقسام.</em></> : <>Every <em>department.</em></>}</h1>
          <p>{isArabic ? "تصفح أقسام الكتالوج الفعلية وانتقل مباشرة إلى منتجاتها المنشورة." : "Browse the live catalog departments and jump straight to their published products."}</p>
        </div>
        <div className="hero-side-stats">
          <span><b>{categories ? String(categories.length).padStart(2, "0") : "—"}</b><small>{isArabic ? "قسم" : "departments"}</small></span>
        </div>
      </div>

      {error ? (
        <div className="grid gap-3">
          <Message tone="error">{error}</Message>
          <SecondaryButton type="button" onClick={() => setAttempt((value) => value + 1)}>{messages.retry}</SecondaryButton>
        </div>
      ) : null}

      {categories ? (
        <div className="category-grid">
          {categories.map((category) => (
            <Link key={category.id} className="category-card" href={`/products?category=${encodeURIComponent(category.slug)}`}>
              <span className="category-icon"><Icon name={icons[category.slug] ?? "box"} /></span>
              <b>{localizedCategoryName(category.slug, category.name, locale)}</b>
              <small dir="ltr">{technicalCategoryName(category.slug, category.name)}</small>
              <i>‹</i>
            </Link>
          ))}
        </div>
      ) : null}

      {categories ? (
        <section className="section-block" style={{ marginTop: 34 }}>
          <div className="section-heading">
            <div>
              <span className="eyebrow">DEPARTMENT DETAIL / RIGX</span>
              <h2>{isArabic ? <>الأقسام <em>الفرعية.</em></> : <>Sub-<em>departments.</em></>}</h2>
            </div>
          </div>
          <div className="content-blocks">
            {categories.filter((category) => category.children.length > 0).map((category) => (
              <article className="glass-panel" key={category.id} style={{ padding: 16 }}>
                <h3>{localizedCategoryName(category.slug, category.name, locale)}</h3>
                <div className="build-parts" style={{ marginTop: 10 }}>
                  {category.children.map((child) => (
                    <Link key={child.id} href={`/products?category=${encodeURIComponent(child.slug)}`} className="spec-pill">
                      {localizedCategoryName(child.slug, child.name, locale)}
                    </Link>
                  ))}
                </div>
              </article>
            ))}
          </div>
        </section>
      ) : null}
    </>
  );
}
