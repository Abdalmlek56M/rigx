import type { ReactNode } from "react";
import { useTranslations } from "next-intl";
import { ButtonLink } from "@/components/ui/Button";
import { cardClass } from "@/components/ui/Card";
import { CaseIcon, ChevronStartIcon, CpuIcon, HelpIcon, SearchIcon } from "@/components/ui/icons";
import { Link } from "@/i18n/navigation";

function PathTile({ href, icon, title, body, accent = false }: { href: string; icon: ReactNode; title: string; body: string; accent?: boolean }) {
  return (
    <Link
      href={href}
      className={`${cardClass({ padding: "md", interactive: true })} path-tile group h-full justify-between gap-4 sm:min-h-[148px] max-sm:grid max-sm:grid-cols-[auto_minmax(0,1fr)_auto] max-sm:items-center ${accent ? "accent-panel" : ""}`}
    >
      <span className="flex items-start justify-between gap-3 max-sm:contents">
        <span className={`flex h-11 w-11 items-center justify-center rounded-full max-sm:order-1 ${accent ? "accent-strong" : "bg-orange-soft text-orange-text"}`}>{icon}</span>
        <span aria-hidden="true" className={`path-arrow flex max-sm:order-3 ltr:-scale-x-100 ${accent ? "text-orange-text" : "text-muted"}`}>
          <ChevronStartIcon size={20} />
        </span>
      </span>
      <span className="flex flex-col gap-1 max-sm:order-2">
        <b className="type-section-title">{title}</b>
        <span className="text-sm leading-relaxed text-ink-muted">{body}</span>
      </span>
    </Link>
  );
}

/** Hero: the headline and its two buttons, beside four "where do you start?" tiles, over a warm glow. */
export function Hero() {
  const t = useTranslations("home.hero");

  return (
    <section className="hero-glow relative overflow-hidden border-b border-line">
      <div className="shell grid items-center gap-10 pb-12 pt-10 md:pb-16 md:pt-16 lg:grid-cols-[minmax(0,1.1fr)_minmax(0,1fr)] lg:gap-16">
        <div className="flex min-w-0 flex-col gap-6">
          <h1 className="m-0 text-[36px] leading-[1.25] font-bold md:text-[60px] md:leading-[1.15] xl:text-[68px]">
            {t("title")} <span className="text-orange-text">{t("titleAccent")}</span>
          </h1>
          <p className="m-0 max-w-[560px] text-base leading-[1.8] text-ink-muted md:text-[19px]">{t("subtitle")}</p>
          <div className="flex flex-col gap-3 sm:flex-row sm:flex-wrap">
            <ButtonLink href="/products" variant="primary" icon={SearchIcon} className="max-sm:w-full">{t("browse")}</ButtonLink>
            <ButtonLink href="/#recommend" variant="accent" className="max-sm:w-full">{t("recommend")}</ButtonLink>
          </div>
        </div>
        <div className="flex min-w-0 flex-col gap-3">
          <h2 className="m-0 font-sans text-sm font-bold text-ink-muted">{t("startHere")}</h2>
          <div className="grid items-stretch gap-3 sm:grid-cols-2">
            <PathTile href="/products" icon={<SearchIcon size={20} />} title={t("paths.part.title")} body={t("paths.part.body")} />
            <PathTile href="/builds" icon={<CaseIcon size={20} />} title={t("paths.build.title")} body={t("paths.build.body")} />
            <PathTile href="/builder" icon={<CpuIcon size={20} />} title={t("paths.builder.title")} body={t("paths.builder.body")} />
            <PathTile href="/#recommend" icon={<HelpIcon size={20} />} title={t("paths.unsure.title")} body={t("paths.unsure.body")} accent />
          </div>
        </div>
      </div>
    </section>
  );
}
