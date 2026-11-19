import { useTranslations } from "next-intl";
import { Link } from "@/i18n/navigation";

export default function AdminNotFound() {
  const t = useTranslations("admin.shell.notFound");
  return (
    <section className="flex flex-col items-start gap-3 rounded-card border border-line bg-surface p-8">
      <h1 className="m-0 text-2xl font-bold">{t("title")}</h1>
      <Link href="/admin" className="font-bold text-orange-text underline hover:text-orange-text-hover">{t("home")}</Link>
    </section>
  );
}
