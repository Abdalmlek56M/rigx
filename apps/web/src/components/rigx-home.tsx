"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import {
  getCatalogCategories,
  getCatalogProducts,
  getPublicArticles,
  getPublicFaqs,
  getPublishedBuilds,
  type CatalogCategory,
  type CatalogProductSummary,
  type ContentArticle,
  type ContentFaq,
  type PublishedBuild,
} from "@/lib/api-client";
import { useLocale } from "@/lib/i18n";
import { localizedCategoryName } from "@/lib/category-labels";
import { RigxProductCard } from "@/components/rigx-product-card";
import { currencyLabel, formatCatalogPrice } from "@/lib/storefront";
import { LoadingCard } from "@/components/ui";
import { Icon, type IconName } from "@/components/icons";

const departmentIcons: Record<string, IconName> = {
  "graphics-cards": "gpu",
  processors: "cpu",
  motherboards: "motherboard",
  memory: "ram",
  storage: "hardDrive",
  "power-supplies": "power",
  cooling: "fan",
  cases: "pcCase",
  monitors: "monitor" as IconName,
  peripherals: "peripherals",
};

const benefits = [
  { ar: "تواصل مباشر", en: "Direct contact", noteAr: "عبر نموذج التواصل في RIGX", noteEn: "Through the RIGX contact form", icon: "whatsapp" as IconName },
  { ar: "متابعة الطلب", en: "Order follow-up", noteAr: "حسب حالة الطلب المسجلة", noteEn: "Based on recorded order status", icon: "tools" as IconName },
  { ar: "معلومات الضمان", en: "Warranty information", noteAr: "تُعرض حسب بيانات الطلب", noteEn: "Shown per order data", icon: "check" as IconName },
  { ar: "استلام بالاتفاق", en: "Pickup by arrangement", noteAr: "تنسيق مباشر دون نشر عنوان", noteEn: "Coordinated directly, address private", icon: "store" as IconName },
];

export function RigxHome() {
  const { locale } = useLocale();
  const isArabic = locale === "ar";
  const [categories, setCategories] = useState<CatalogCategory[]>([]);
  const [featured, setFeatured] = useState<CatalogProductSummary[]>([]);
  const [builds, setBuilds] = useState<PublishedBuild[]>([]);
  const [articles, setArticles] = useState<ContentArticle[]>([]);
  const [faqs, setFaqs] = useState<ContentFaq[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let cancelled = false;
    async function load() {
      const [categoryResult, productResult, buildResult, articleResult, faqResult] = await Promise.allSettled([
        getCatalogCategories(),
        getCatalogProducts({ pageSize: 8 }),
        getPublishedBuilds(),
        getPublicArticles({ pageSize: 2 }),
        getPublicFaqs(),
      ]);
      if (cancelled) return;
      if (categoryResult.status === "fulfilled") {
        setCategories(categoryResult.value.filter((item) => item.isActive && item.isEffectivelyActive));
      }
      if (productResult.status === "fulfilled") setFeatured(productResult.value.items);
      if (buildResult.status === "fulfilled") setBuilds(buildResult.value.slice(0, 3));
      if (articleResult.status === "fulfilled") setArticles(articleResult.value.items.slice(0, 2));
      if (faqResult.status === "fulfilled") setFaqs(faqResult.value.slice(0, 6));
      setLoading(false);
    }
    void load();
    return () => { cancelled = true; };
  }, []);

  const departments = categories.slice(0, 8);
  const firstProduct = featured[0];

  return (
    <>
      <section className="hero-grid">
        <article className="hero-main glass-panel">
          <div className="hero-orbit orbit-a" />
          <div className="hero-orbit orbit-b" />
          <div className="hero-scan" />
          <div className="hero-copy">
            <span className="eyebrow"><i className="pulse" /> RIGX HARDWARE / YOUR NEXT BUILD</span>
            <h1>
              {isArabic ? <>قطع تُبنى بوعي.<br /><em>وتجميع يُحسب بدقة.</em></> : <>Components chosen with care.<br /><em>Builds calculated with precision.</em></>}
            </h1>
            <p>
              {isArabic
                ? "من المكوّن إلى الجهاز الكامل: كتالوج واضح، مخزون حقيقي، وتجميعات جاهزة يتابعها فريق RIGX معك خطوة بخطوة."
                : "From a single component to a complete system: a clear catalog, real inventory, and ready builds followed by the RIGX team step by step."}
            </p>
            <div className="hero-ctas">
              <Link className="btn-primary" href="/contact">{isArabic ? "تحدث معنا" : "Talk to us"} <span>‹</span></Link>
              <Link className="btn-ghost" href="/products">{isArabic ? "استعرض المنتجات" : "Browse products"}</Link>
            </div>
          </div>
          <div className="hero-product">
            <div className="hero-product-top"><span>LIVE CATALOG / INVENTORY-AWARE</span><span className="live-indicator">RIGX</span></div>
            <div className="hero-gpu-stage">
              <div className="chip-visual" aria-hidden="true">RIGX</div>
            </div>
            <div className="hero-product-caption">
              <span>{firstProduct ? firstProduct.name : (isArabic ? "كتالوج مكوّنات RIGX" : "RIGX component catalog")}</span>
              <span>{firstProduct ? formatCatalogPrice(firstProduct.fromPrice) + " " + currencyLabel(locale) : "RIGX"}</span>
            </div>
          </div>
          <div className="hero-corner">RIGX // 001</div>
        </article>

        <div className="hero-side">
          <Link className="side-promo promo-cpu glass-panel" href="/products?category=processors">
            <div className="side-promo-copy">
              <span className="eyebrow">PROCESSING POWER</span>
              <h3>{isArabic ? <>المعالجات<br />قوة البناء</> : <>Processors<br />the core</>}</h3>
              <span className="text-link">{isArabic ? "كل المعالجات" : "All processors"} <b>‹</b></span>
            </div>
            <div className="chip-visual" aria-hidden="true">CPU</div>
          </Link>
          <Link className="side-promo promo-memory glass-panel" href="/products?category=memory">
            <div className="side-promo-copy">
              <span className="eyebrow">MEMORY / DDR5</span>
              <h3>{isArabic ? <>ذاكرة أسرع.<br />أداء أنقى.</> : <>Faster memory.<br />Cleaner response.</>}</h3>
              <span className="text-link">{isArabic ? "كل الذاكرة" : "All memory"} <b>‹</b></span>
            </div>
            <div className="chip-visual" aria-hidden="true">RAM</div>
          </Link>
          <Link className="side-promo promo-build glass-panel" href="/builds">
            <div className="side-promo-copy">
              <span className="eyebrow">SYSTEM CONFIGURATION</span>
              <h3>{isArabic ? <>تجميعات<br />جاهزة.</> : <>Ready<br />builds.</>}</h3>
              <span className="text-link">{isArabic ? "كل التجميعات" : "All builds"} <b>‹</b></span>
            </div>
            <div className="mini-tower" aria-hidden="true"><span /><span /><span /></div>
          </Link>
        </div>
      </section>

      <section className="home-service-promos" aria-label={isArabic ? "خدمات RIGX" : "RIGX services"}>
        <article className="service-promo-card consultation-promo glass-panel">
          <div className="service-promo-icon" aria-hidden="true"><Icon name="whatsapp" /></div>
          <div className="service-promo-copy">
            <span className="eyebrow">BUILD GUIDANCE</span>
            <h3>{isArabic ? "لا تعرف من أين تبدأ؟" : "Not sure where to start?"}</h3>
            <p>{isArabic ? "اسأل عن التوافق والاستخدام، وسنساعدك في اختيار المكوّنات المناسبة قبل الشراء." : "Ask about compatibility and use, and we will help choose the right components before you buy."}</p>
            <Link className="service-promo-link" href="/contact">{isArabic ? "تحدث مع فريق RIGX" : "Talk to RIGX"} <b>‹</b></Link>
          </div>
          <span className="service-promo-mark" aria-hidden="true">01</span>
        </article>
        <article className="service-promo-card sourcing-promo glass-panel">
          <div className="service-promo-icon" aria-hidden="true"><Icon name="box" /></div>
          <div className="service-promo-copy">
            <span className="eyebrow">SOURCE IT FOR YOU</span>
            <h3>{isArabic ? "قطعة غير متوفرة؟" : "A part out of stock?"}</h3>
            <p>{isArabic ? "أرسل لنا ما تحتاجه، وسنتحقق من إمكانية توفيره عبر قنوات RIGX والتوريد المعتمدة." : "Send us what you need and we will check availability through RIGX sourcing channels."}</p>
            <Link className="service-promo-link" href="/contact">{isArabic ? "اطلب توفير قطعة" : "Request sourcing"} <b>‹</b></Link>
          </div>
          <span className="service-promo-mark" aria-hidden="true">02</span>
        </article>
      </section>

      <section className="ticker-benefits glass-panel" aria-label={isArabic ? "مزايا RIGX" : "RIGX benefits"}>
        {benefits.map((benefit) => (
          <article className="benefit-card" key={benefit.en}>
            <span className="benefit-symbol"><Icon name={benefit.icon} /></span>
            <p><b>{isArabic ? benefit.ar : benefit.en}</b><small>{isArabic ? benefit.noteAr : benefit.noteEn}</small></p>
          </article>
        ))}
      </section>

      <section className="section-block" id="home-categories">
        <div className="section-heading">
          <div>
            <span className="eyebrow">HARDWARE TAXONOMY / 01</span>
            <h2>{isArabic ? <>من أول <em>قطعة.</em></> : <>Start with <em>one part.</em></>}</h2>
            <p>{isArabic ? "كل الأقسام مرتبطة بالكتالوج الفعلي والمخزون المتاح." : "Every department is wired to the live catalog and available stock."}</p>
          </div>
          <Link className="text-link" href="/categories">{isArabic ? "كل الأقسام" : "All departments"} <b>‹</b></Link>
        </div>
        <div className="category-grid">
          {(departments.length > 0 ? departments : []).map((category) => (
            <Link className="category-card" key={category.id} href={`/products?category=${encodeURIComponent(category.slug)}`}>
              <span className="category-icon"><Icon name={departmentIcons[category.slug] ?? "box"} /></span>
              <b>{localizedCategoryName(category.slug, category.name, locale)}</b>
              <small>{category.slug.replaceAll("-", " ").toUpperCase()}</small>
              <i>‹</i>
            </Link>
          ))}
        </div>
      </section>

      <section className="section-block" id="home-featured">
        <div className="section-heading">
          <div>
            <span className="eyebrow">CURATED HARDWARE / 02</span>
            <h2>{isArabic ? <>أحدث ما <em>وصل.</em></> : <>Latest <em>arrivals.</em></>}</h2>
            <p>{isArabic ? "منتجات منشورة فعليًا في كتالوج RIGX بأسعارها الرسمية." : "Published RIGX catalog products at their official prices."}</p>
          </div>
          <Link className="text-link" href="/products">{isArabic ? "كل المنتجات" : "All products"} <b>‹</b></Link>
        </div>
        {loading ? <LoadingCard /> : featured.length === 0 ? (
          <div className="order-empty compact-empty"><div className="empty-icon">+</div><h3>{isArabic ? "لا منتجات منشورة بعد" : "No published products yet"}</h3><p>{isArabic ? "سيظهر هنا كل ما ينشره فريق RIGX في الكتالوج." : "Everything published to the RIGX catalog will appear here."}</p></div>
        ) : (
          <div className="product-grid">{featured.slice(0, 8).map((product) => <RigxProductCard key={product.id} product={product} />)}</div>
        )}
      </section>

      <section className="full-promo glass-panel" id="home-builder-promo">
        <div className="promo-glow" />
        <div className="promo-copy">
          <span className="eyebrow">YOUR NEXT BUILD / 03</span>
          <h2>{isArabic ? <>ابنِه مرة واحدة.<br /><em>وارجع له دائمًا.</em></> : <>Build it once.<br /><em>Keep it forever.</em></>}</h2>
          <p>{isArabic ? "جمّع مكوّناتك داخل PC Builder، واحفظ الإعداد للرجوع إليه في أي وقت قبل الشراء." : "Assemble your components in PC Builder and keep the configuration to revisit before buying."}</p>
          <Link className="btn-primary" href="/pc-builder">{isArabic ? "افتح PC Builder" : "Open PC Builder"} <b>‹</b></Link>
          <div className="promo-specs"><span>CPU / GPU</span><span>MEMORY</span><span>STORAGE</span><span>POWER</span></div>
        </div>
        <div className="promo-tower-wrap" aria-hidden="true">
          <div className="tower-halo" />
          <div className="tower-case">
            <div className="tower-glass">
              <div className="fan fan-one" /><div className="fan fan-two" /><div className="fan fan-three" /><div className="gpu-bar" />
            </div>
            <div className="tower-front"><i /><i /><i /></div>
          </div>
          <span className="tower-caption">RIGX / CUSTOM CONFIGURATION</span>
        </div>
      </section>

      <section className="section-block" id="home-builds">
        <div className="section-heading">
          <div>
            <span className="eyebrow">READY CONFIGURATIONS / 04</span>
            <h2>{isArabic ? <>تجميعات <em>جاهزة.</em></> : <>Ready <em>builds.</em></>}</h2>
            <p>{isArabic ? "تجميعات منشورة من مخزون RIGX الفعلي." : "Published builds from actual RIGX inventory."}</p>
          </div>
          <Link className="text-link" href="/builds">{isArabic ? "كل التجميعات" : "All builds"} <b>‹</b></Link>
        </div>
        {builds.length === 0 ? (
          <div className="order-empty compact-empty"><div className="empty-icon">+</div><h3>{isArabic ? "لا تجميعات منشورة" : "No published builds"}</h3><p>{isArabic ? "تُنشر التجميعات بعد تجهيز وحدات المخزون لها." : "Builds are published once their inventory units are prepared."}</p></div>
        ) : (
          <div className="build-grid">
            {builds.map((build, index) => (
              <article className={`build-card glass-panel${index === 0 ? " build-card-large" : ""}`} key={build.id}>
                <div className={`build-image ${index === 0 ? "build-image-orange" : index === 1 ? "build-image-blue" : "build-image-purple"}`}>
                  <div className="build-tower" aria-hidden="true"><span /><span /><span /></div>
                </div>
                <div className="build-content">
                  <div className="build-meta"><span>RIGX / SYSTEM</span><span>0{index + 1}</span></div>
                  <h3>{isArabic ? build.titleAr ?? build.titleEn ?? "تجميع RIGX" : build.titleEn ?? build.titleAr ?? "RIGX build"}</h3>
                  <p>{isArabic ? `${build.units.length} مكوّنات مجهزة من مخزون RIGX.` : `${build.units.length} components prepared from RIGX stock.`}</p>
                  <div className="build-parts">{build.units.slice(0, 4).map((unit) => <span key={unit.name + unit.sku}>{unit.name}</span>)}</div>
                  <div className="build-price" dir="ltr">{formatCatalogPrice(build.salePrice)} {currencyLabel(locale)}</div>
                  <Link className="btn-outline" href="/builds">{isArabic ? "تفاصيل التجميع" : "Build details"} <b>‹</b></Link>
                </div>
              </article>
            ))}
          </div>
        )}
      </section>

      <section className="editorial-grid">
        <article className="editorial-card glass-panel">
          <span className="eyebrow">RIGX / KNOWLEDGE</span>
          <h3>{isArabic ? <>كيف تختار<br />توافق مكوّناتك.</> : <>How to choose<br />compatible parts.</>}</h3>
          <p>{isArabic ? "المقالات تركز على التوافق والاستخدام والحقائق التقنية التي تهم البناء." : "Articles focus on compatibility, intended use, and the technical facts that matter."}</p>
          <Link className="text-link" href="/articles">{isArabic ? "اقرأ المقالات" : "Read the journal"} <b>‹</b></Link>
          <div className="editorial-circuits" aria-hidden="true" />
        </article>
        <article className="editorial-card editorial-card-alt glass-panel">
          <span className="eyebrow">POWER SUPPLY GUIDE</span>
          <h3>{isArabic ? <>اختيار PSU<br />ليس اختياريًا.</> : <>Choosing a PSU<br />is not optional.</>}</h3>
          <p>{isArabic ? "الطاقة المناسبة تحمي بقية المكوّنات وتمنع الأعطال غير المتوقعة." : "The right PSU protects every other component and prevents unexpected failures."}</p>
          <Link className="text-link" href="/articles">{isArabic ? "اقرأ الدليل" : "Read the guide"} <b>‹</b></Link>
          <div className="power-symbol" aria-hidden="true">⚡</div>
        </article>
      </section>

      <section className="closing-band">
        <div>
          <span className="eyebrow">RIGX HARDWARE / BUILD WITH CLARITY</span>
          <h2>{isArabic ? <>جاهز تبدأ <em>بناءك القادم.</em></> : <>Ready to start <em>your next build.</em></>}</h2>
          <p>{isArabic ? "ابدأ من الكتالوج أو أرسل لنا احتياجك." : "Start from the catalog or send us your requirement."}</p>
        </div>
        <Link className="btn-primary" href="/products">{isArabic ? "استعرض المنتجات" : "Browse products"} <b>‹</b></Link>
      </section>

      <section className="section-block home-faq-section" id="home-faq">
        <div className="section-heading">
          <div>
            <span className="eyebrow">HELP CENTER / 05</span>
            <h2>{isArabic ? <>أسئلة <em>شائعة.</em></> : <>Common <em>questions.</em></>}</h2>
            <p>{isArabic ? "إجابات من مركز المساعدة الفعلي في RIGX." : "Answers from the live RIGX help center."}</p>
          </div>
          <Link className="text-link" href="/faq">{isArabic ? "كل الأسئلة" : "All questions"} <b>‹</b></Link>
        </div>
        {faqs.length > 0 ? (
          <div className="home-faq-grid">
            {faqs.map((faq, index) => (
              <details className="home-faq-item glass-panel" key={faq.id} open={index === 0}>
                <summary><span>{isArabic ? faq.questionAr : faq.questionEn}</span><b>+</b></summary>
                <p>{isArabic ? faq.answerAr : faq.answerEn}</p>
              </details>
            ))}
          </div>
        ) : null}
      </section>
    </>
  );
}
