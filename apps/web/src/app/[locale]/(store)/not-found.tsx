import { useTranslations } from "next-intl";
import { ButtonLink } from "@/components/ui/Button";
import { EmptyState } from "@/components/ui/EmptyState";
import { SearchIcon } from "@/components/ui/icons";

export default function NotFound() {
  const t = useTranslations("notFound");

  return (
    <div className="shell py-16">
      <EmptyState
        icon={SearchIcon}
        title={t("title")}
        action={<ButtonLink href="/" variant="secondary">{t("home")}</ButtonLink>}
      />
    </div>
  );
}
