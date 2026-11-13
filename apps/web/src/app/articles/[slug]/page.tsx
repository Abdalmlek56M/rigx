import type { Metadata } from "next";
import { notFound } from "next/navigation";
import Link from "next/link";
import { catalogMediaUrl } from "@/lib/api-client";
import { getPublishedArticleServer } from "@/lib/server-content";
import { siteUrl } from "@/lib/server-catalog";

type ArticlePageProps = { params: Promise<{ slug: string }> };

export async function generateMetadata({ params }: ArticlePageProps): Promise<Metadata> {
  const { slug } = await params;
  const article = await getPublishedArticleServer(slug).catch(() => null);
  if (!article) {
    return {
      title: "المقال غير موجود | RIGX",
      robots: { index: false, follow: false },
    };
  }
  const path = article.canonicalUrl ?? `/articles/${encodeURIComponent(article.slug)}`;
  const description = article.seoDescriptionAr ?? article.summaryAr;
  const imageUrl = article.mainImageStorageKey
    ? `${siteUrl()}${catalogMediaUrl(article.mainImageStorageKey)}`
    : undefined;
  return {
    title: article.seoTitleAr ?? `${article.titleAr} | RIGX`,
    description,
    alternates: { canonical: path },
    robots: { index: true, follow: true },
    openGraph: {
      title: article.seoTitleAr ?? article.titleAr,
      description,
      type: "article",
      url: path,
      siteName: "RIGX",
      images: imageUrl ? [{ url: imageUrl, alt: article.titleEn }] : undefined,
      publishedTime: article.publishedAtUtc ?? undefined,
    },
    twitter: {
      card: "summary_large_image",
      title: article.seoTitleEn ?? article.titleEn,
      description: article.seoDescriptionEn ?? article.summaryEn,
      images: imageUrl ? [imageUrl] : undefined,
    },
  };
}

export default async function ArticlePage({ params }: ArticlePageProps) {
  const { slug } = await params;
  const article = await getPublishedArticleServer(slug);
  if (!article) notFound();

  return (
    <>
      <div className="page-hero compact-hero">
        <div>
          <span className="eyebrow">RIGX / JOURNAL</span>
          <h1>{article.titleAr}</h1>
          <p dir="ltr">{article.titleEn}</p>
        </div>
        {article.publishedAtUtc ? (
          <div className="builder-status"><span className="pulse" /> {new Date(article.publishedAtUtc).toLocaleDateString("en-GB")}</div>
        ) : null}
      </div>
      <article className="article-document glass-panel">
        <nav className="product-breadcrumb"><Link href="/articles">← المقالات / Articles</Link></nav>
        {article.mainImageStorageKey ? (
          <img
            src={catalogMediaUrl(article.mainImageStorageKey)}
            alt={article.titleEn}
            className="article-hero-image"
          />
        ) : null}
        <section className="article-body" dir="rtl">
          {article.summaryAr ? <p className="article-summary">{article.summaryAr}</p> : null}
          <div>{article.bodyAr}</div>
        </section>
        <section className="article-body" dir="ltr">
          {article.summaryEn ? <p className="article-summary">{article.summaryEn}</p> : null}
          <div>{article.bodyEn}</div>
        </section>
        {article.tags.length > 0 ? (
          <ul className="build-parts">
            {article.tags.map((tag) => <li key={tag} className="spec-pill">{tag}</li>)}
          </ul>
        ) : null}
        <p className="inline-note">المحتوى منشور من إدارة محتوى RIGX. التفاصيل التقنية تعكس ما ورد في المقال فقط.</p>
      </article>
    </>
  );
}
