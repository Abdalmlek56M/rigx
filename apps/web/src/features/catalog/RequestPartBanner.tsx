import { useTranslations } from "next-intl";
import { ButtonLink } from "@/components/ui/Button";
import { Card } from "@/components/ui/Card";

/** The accent "can't find it?" banner under the catalog: sends the visitor to the services page to request a part. */
export function RequestPartBanner() {
  const t = useTranslations("catalog.banner");
  return (
    <Card padding="lg" className="accent-panel">
      <div className="flex flex-wrap items-center justify-between gap-4">
        <div className="flex flex-col gap-1">
          <span className="text-lg font-bold">{t("title")}</span>
          <span className="text-sm text-ink-muted">{t("body")}</span>
        </div>
        <ButtonLink href="/services" variant="accent">{t("cta")}</ButtonLink>
      </div>
    </Card>
  );
}
