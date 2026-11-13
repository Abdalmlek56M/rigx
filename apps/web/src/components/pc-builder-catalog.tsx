"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { getPcBuilds, type PcBuild } from "@/lib/api-client";
import { apiErrorMessage } from "@/lib/form-errors";
import { useLocale } from "@/lib/i18n";
import { LoadingCard, Message } from "@/components/ui";

export function PcBuilderCatalog() {
  const { messages, locale } = useLocale();
  const isArabic = locale === "ar";
  const [builds, setBuilds] = useState<PcBuild[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let active = true;
    void getPcBuilds()
      .then((items) => { if (active) setBuilds(items); })
      .catch((requestError) => { if (active) setError(apiErrorMessage(requestError, messages)); })
      .finally(() => { if (active) setLoading(false); });
    return () => { active = false; };
  }, [messages]);

  return (
    <>
      <div className="page-hero compact-hero">
        <div>
          <span className="eyebrow">RIGX CONFIGURATOR / 02</span>
          <h1>{isArabic ? <>ابنِ جهازك <em>خطوة بخطوة.</em></> : <>Build your PC <em>step by step.</em></>}</h1>
          <p>
            {isArabic
              ? "اختر قالبًا، راجع التوافق والسعر من بيانات الكتالوج والمخزون، ثم احفظ إعدادك قبل الشراء."
              : "Pick a template, review compatibility and price from live catalog and inventory data, then save your configuration before buying."}
          </p>
        </div>
        <div className="builder-status"><span className="pulse" /> CONFIGURATION / TEMPLATES</div>
      </div>

      {loading ? <LoadingCard /> : error ? (
        <Message tone="error">{error}</Message>
      ) : builds.length === 0 ? (
        <div className="order-empty compact-empty">
          <div className="empty-icon">+</div>
          <h3>{isArabic ? "لا قوالب متاحة" : "No templates available"}</h3>
          <p>{isArabic ? "تُنشأ قوالب PC Builder من إدارة RIGX." : "PC Builder templates are created from RIGX admin."}</p>
        </div>
      ) : (
        <div className="builder-layout">
          <div className="builder-main-column">
            <div className="builder-slots glass-panel">
              {builds.map((build, index) => (
                <article className="builder-slot" key={build.id}>
                  <span className="slot-icon">{String(index + 1).padStart(2, "0")}</span>
                  <div className="slot-data">
                    <b>{isArabic ? build.titleAr : build.titleEn}</b>
                    <small>{build.slots.length} {isArabic ? "خانات مكوّنات" : "component slots"}</small>
                  </div>
                  <Link href={`/pc-builder/${build.id}`} className="slot-action" aria-label={messages.pcBuildOpen}>‹</Link>
                </article>
              ))}
            </div>
            <p className="inline-note">
              {isArabic
                ? "التوافق والسعر يُحسبان من بيانات المنتجات الفعلية. الحفظ لا ينشئ طلبًا ولا يحجز مخزونًا."
                : "Compatibility and price use live product data. Saving does not create an order or reserve stock."}
            </p>
          </div>
          <aside className="builder-summary glass-panel">
            <span className="eyebrow">YOUR CONFIGURATION</span>
            <div className="summary-device"><div className="summary-device-glow" /><div className="summary-case"><i /><i /><i /></div></div>
            <h3>{isArabic ? "اختر قالبًا للبدء" : "Pick a template to start"}</h3>
            <p>{isArabic ? "كل قالب يحتوي خانات مكوّنات قابلة للتعديل قبل الحفظ." : "Every template has editable component slots before saving."}</p>
            <div className="summary-progress"><span style={{ width: `${Math.min(100, builds.length * 20)}%` }} /></div>
            <div className="summary-count"><b>{builds.length} / {builds.length}</b><small>{isArabic ? "قوالب متاحة" : "templates available"}</small></div>
            <Link className="btn-primary full-width" href="/products">{isArabic ? "استعرض المكوّنات" : "Browse components"}</Link>
            <small className="summary-disclaimer">
              {isArabic ? "RIGX لا تحجز مكوّنات بناءً على إعداد محفوظ." : "RIGX does not reserve components from a saved configuration."}
            </small>
          </aside>
        </div>
      )}
    </>
  );
}
