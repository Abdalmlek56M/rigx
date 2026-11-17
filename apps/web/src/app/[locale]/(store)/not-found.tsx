import { useTranslations } from "next-intl";
import { Link } from "@/i18n/navigation";

export default function NotFound() {
  const t = useTranslations("notFound");

  return (
    <section className="mx-auto flex max-w-[1240px] flex-col items-start gap-4 px-4 py-16 md:px-6">
      <h1 className="m-0 text-3xl font-bold">{t("title")}</h1>
      <Link href="/" className="text-orange-text underline hover:text-orange-text-hover">
        {t("home")}
      </Link>
    </section>
  );
}
