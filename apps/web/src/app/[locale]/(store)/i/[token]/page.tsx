import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { hasLocale } from "next-intl";
import { getTranslations, setRequestLocale } from "next-intl/server";
import { DocumentFrame } from "@/components/document/DocumentFrame";
import { DocumentIcon } from "@/components/ui/icons";
import { INVOICE_WIDTH } from "@/features/invoice/invoice-layout";
import { InvoiceSheet } from "@/features/invoice/InvoiceSheet";
import { loadInvoice } from "@/features/invoice/load-invoice";
import { invoicePageCss } from "@/features/invoice/page-footer";
import { PrintButton } from "@/features/invoice/PrintButton";
import { routing } from "@/i18n/routing";
import { loadStorefront } from "@/lib/storefront";

interface Props {
  params: Promise<{ locale: string; token: string }>;
}

export async function generateMetadata({ params }: Props): Promise<Metadata> {
  const { locale } = await params;
  if (!hasLocale(routing.locales, locale)) return {};
  const t = await getTranslations({ locale, namespace: "invoice" });
  return { title: t("title"), robots: { index: false } };
}

export default async function InvoicePage({ params }: Props) {
  const { locale, token } = await params;
  if (!hasLocale(routing.locales, locale)) notFound();
  setRequestLocale(locale);
  const [document, storefront] = await Promise.all([loadInvoice(token), loadStorefront()]);
  if (!document) notFound();
  const t = await getTranslations({ locale, namespace: "invoice" });
  const number = document.invoice.number;
  const pageCss = invoicePageCss(`${document.branding.storeName} · ${t("toolbar", { number })}`, t.raw("pageOf") as string, locale === "ar" ? "rtl" : "ltr");

  return (
    <div className="mx-auto max-w-[860px] px-4 pb-16 pt-8 md:px-6 md:pt-10 print:max-w-none print:p-0">
      <style dangerouslySetInnerHTML={{ __html: pageCss }} />
      <DocumentFrame
        width={INVOICE_WIDTH} fileKind="invoice" fileLabel={number} actions={<PrintButton />}
        leading={<span className="flex items-center gap-2 px-1 font-bold"><DocumentIcon size={18} /><bdi>{t("toolbar", { number })}</bdi></span>}
      >
        <InvoiceSheet document={document} storefront={storefront} />
      </DocumentFrame>
    </div>
  );
}
