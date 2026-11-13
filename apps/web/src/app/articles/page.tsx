import type { Metadata } from "next";
import Link from "next/link";
import { getPublishedArticlesServer } from "@/lib/server-content";
import { catalogMediaUrl } from "@/lib/api-client";

export const metadata: Metadata = {
  title: "المقالات والأدلة | RIGX",
  description: "مقالات وأدلة تقنية من فريق RIGX حول المكوّنات والتوافق والبناء.",
  alternates: { canonical: "/articles" },
  openGraph: {
    title: "المقالات والأدلة | RIGX",
    description: "مقالات وأدلة تقنية من فريق RIGX.",
    type: "website",
    url: "/articles",
    siteName: "RIGX",
  },
};

export default async function ArticlesPage() {
  const page = await getPublishedArticlesServer(1, 30).catch(() => null);
  const articles = page?.items ?? [];

  return (
    <>
      <div className="page-hero compact-hero">
        <div>
          <span className="eyebrow">RIGX / JOURNAL</span>
          <h1>المقالات <em>والأدلة.</em></h1>
          <p>أدلة تقنية مكتوبة حول التوافق والاستخدام والحقائق التي تهم البناء.</p>
        </div>
        <div className="hero-side-stats">
          <span><b>{String(articles.length).padStart(2, "0")}</b><small>مقال منشور</small></span>
        </div>
      </div>

      {articles.length === 0 ? (
        <div className="order-empty compact-empty">
          <div className="empty-icon">+</div>
          <h3>لا توجد مقالات منشورة</h3>
          <p>ستظهر هنا المقالات التي ينشرها فريق RIGX من إدارة المحتوى.</p>
        </div>
      ) : (
        <div className="article-grid">
          {articles.map((article, index) => (
            <article key={article.id} className="article-card glass-panel">
              {article.mainImageStorageKey ? (
                <img src={catalogMediaUrl(article.mainImageStorageKey)} alt={article.titleEn} loading="lazy" className="article-image" />
              ) : (
                <div className={`article-visual ${index % 3 === 0 ? "visual-cpu" : index % 3 === 1 ? "visual-power" : "visual-memory"}`} aria-hidden="true">
                  {String(index + 1).padStart(2, "0")}
                </div>
              )}
              <div className="article-copy">
                <span className="eyebrow">{article.categorySlug ? article.categorySlug.toUpperCase() : "RIGX / KNOWLEDGE"}</span>
                <h3><Link href={`/articles/${encodeURIComponent(article.slug)}`}>{article.titleAr}</Link></h3>
                <p dir="ltr">{article.titleEn}</p>
                <p className="line-clamp-3">{article.summaryAr}</p>
                <Link href={`/articles/${encodeURIComponent(article.slug)}`} className="text-link">
                  اقرأ المقال <b>‹</b>
                </Link>
              </div>
            </article>
          ))}
        </div>
      )}
    </>
  );
}
