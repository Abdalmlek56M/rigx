import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { hasLocale } from "next-intl";
import { getTranslations, setRequestLocale } from "next-intl/server";
import { PageHeader } from "@/components/ui/PageHeader";
import { InboxIcon } from "@/components/ui/icons";
import { FeedbackForm } from "@/features/feedback/FeedbackForm";
import { routing } from "@/i18n/routing";
import { pageSeo } from "@/lib/seo/page-seo";

interface Props {
  params: Promise<{ locale: string }>;
}

export async function generateMetadata({ params }: Props): Promise<Metadata> {
  const { locale } = await params;
  if (!hasLocale(routing.locales, locale)) return {};
  const t = await getTranslations({ locale, namespace: "feedback" });
  return { title: t("title"), ...(await pageSeo(locale, "/feedback")) };
}

export default async function FeedbackPage({ params }: Props) {
  const { locale } = await params;
  if (!hasLocale(routing.locales, locale)) notFound();
  setRequestLocale(locale);
  const t = await getTranslations("feedback");

  return (
    <>
      <PageHeader title={t("title")} lead={t("lead")} icon={InboxIcon} />
      <section className="shell pb-14">
        <FeedbackForm />
      </section>
    </>
  );
}
