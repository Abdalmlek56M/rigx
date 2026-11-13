import type { Metadata } from "next";
import { notFound } from "next/navigation";
import Link from "next/link";
import { getPublishedPolicyServer } from "@/lib/server-content";

const policyKinds: Record<string, { ar: string; en: string }> = {
  Shipping: { ar: "الشحن", en: "Shipping" },
  Returns: { ar: "الاسترجاع", en: "Returns" },
  Exchanges: { ar: "الاستبدال", en: "Exchanges" },
  Warranty: { ar: "الضمان", en: "Warranty" },
  Terms: { ar: "شروط البيع", en: "Terms" },
};

type PolicyPageProps = { params: Promise<{ kind: string }> };

export async function generateMetadata({ params }: PolicyPageProps): Promise<Metadata> {
  const { kind } = await params;
  const policy = policyKinds[kind] ? await getPublishedPolicyServer(kind).catch(() => null) : null;
  if (!policy) {
    return { title: "الصفحة غير متاحة | RIGX", robots: { index: false, follow: false } };
  }
  return {
    title: `${policy.titleAr} | RIGX`,
    description: policy.titleEn,
    alternates: { canonical: `/policies/${kind}` },
    robots: { index: true, follow: true },
  };
}

export default async function PolicyPage({ params }: PolicyPageProps) {
  const { kind } = await params;
  if (!policyKinds[kind]) notFound();
  const policy = await getPublishedPolicyServer(kind);
  if (!policy) notFound();

  return (
    <>
      <div className="page-hero compact-hero">
        <div>
          <span className="eyebrow">RIGX / POLICIES / {policyKinds[kind].en.toUpperCase()}</span>
          <h1>{policy.titleAr}</h1>
          <p dir="ltr">{policy.titleEn}</p>
        </div>
        <div className="builder-status"><span className="pulse" /> VERSION {policy.versionNumber}</div>
      </div>

      <div className="policy-document glass-panel">
        <nav className="product-breadcrumb" aria-label="Breadcrumb">
          <Link href="/">RIGX</Link> <b>/</b> <Link href="/policies/terms">السياسات</Link> <b>/</b> <span>{policyKinds[kind].ar}</span>
        </nav>
        <section className="policy-body" dir="rtl">
          <h2>{policy.titleAr}</h2>
          <div>{policy.bodyAr}</div>
        </section>
        <section className="policy-body" dir="ltr">
          <h2>{policy.titleEn}</h2>
          <div>{policy.bodyEn}</div>
        </section>
        <p className="inline-note">
          هذه النسخة منشورة رسميًا من إدارة محتوى RIGX. لا تُعدّل الصفحة أي قاعدة عمل أو سياسة غير مذكورة في نصها.
        </p>
      </div>
    </>
  );
}
