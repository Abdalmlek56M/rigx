import type { ReactNode } from "react";
import { useTranslations } from "next-intl";
import { CaseIcon, ChevronStartIcon, CpuIcon, HelpIcon, SearchIcon } from "@/components/ui/icons";
import { Link } from "@/i18n/navigation";

function PathTile({ href, icon, title, body, dark = false }: { href: string; icon: ReactNode; title: string; body: string; dark?: boolean }) {
  return (
    <Link
      href={href}
      className={`path-tile lift press group flex min-h-[148px] flex-col justify-between gap-4 rounded-card border p-5 ${dark ? "border-carbon bg-carbon text-on-carbon" : "border-line bg-surface"}`}
    >
      <span className="flex items-start justify-between gap-3">
        <span className={`flex h-11 w-11 items-center justify-center rounded-full ${dark ? "bg-orange text-on-orange" : "bg-orange-soft text-orange-text"}`}>{icon}</span>
        <span aria-hidden="true" className={`path-arrow flex ltr:-scale-x-100 ${dark ? "text-orange" : "text-muted"}`}>
          <ChevronStartIcon size={20} />
        </span>
      </span>
      <span className="flex flex-col gap-1">
        <b className="text-[17px] leading-snug">{title}</b>
        <span className={`text-sm leading-relaxed ${dark ? "text-footer-text" : "text-ink-muted"}`}>{body}</span>
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
            <Link href="/products" className="press flex h-[54px] items-center justify-center gap-2 rounded-full bg-carbon px-7 font-bold text-on-carbon transition-[filter] hover:brightness-125">
              {t("browse")}
            </Link>
            <Link href="/#recommend" className="press flex h-[54px] items-center justify-center rounded-full bg-orange px-7 font-bold text-on-orange shadow-[0_12px_28px_-12px_rgba(255,106,43,.9)] transition-[filter] hover:brightness-105">
              {t("recommend")}
            </Link>
          </div>
        </div>
        <div className="flex min-w-0 flex-col gap-3">
          <h2 className="m-0 font-sans text-sm font-bold text-ink-muted">{t("startHere")}</h2>
          <div className="grid gap-3 sm:grid-cols-2">
            <PathTile href="/products" icon={<SearchIcon size={20} />} title={t("paths.part.title")} body={t("paths.part.body")} />
            <PathTile href="/builds" icon={<CaseIcon size={20} />} title={t("paths.build.title")} body={t("paths.build.body")} />
            <PathTile href="/builder" icon={<CpuIcon size={20} />} title={t("paths.builder.title")} body={t("paths.builder.body")} />
            <PathTile href="/#recommend" icon={<HelpIcon size={20} />} title={t("paths.unsure.title")} body={t("paths.unsure.body")} dark />
          </div>
        </div>
      </div>
    </section>
  );
}
