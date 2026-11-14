import { useTranslations } from "next-intl";
import { Link } from "@/i18n/navigation";

const path = "flex items-center justify-between gap-3 rounded-[10px] border border-line bg-surface p-5";

/** Hero copy with its two buttons, and the four "where do you start?" paths. */
export function Hero() {
  const t = useTranslations("home.hero");
  const paths = [
    { href: "/products", title: t("paths.part.title"), body: t("paths.part.body") },
    { href: "/builds", title: t("paths.build.title"), body: t("paths.build.body") },
    { href: "/builder", title: t("paths.builder.title"), body: t("paths.builder.body") }
  ];

  return (
    <section className="mx-auto flex max-w-[1240px] flex-wrap items-center gap-10 px-4 pb-10 pt-10 md:gap-12 md:px-6 md:pb-12 md:pt-16">
      <div className="flex min-w-0 flex-1 basis-[420px] flex-col gap-5">
        <span className="font-mono text-[13px] font-semibold tracking-[2px] text-orange-text">RIGX HARDWARE</span>
        <h1 className="m-0 text-[34px] leading-[1.3] font-bold md:text-[56px] md:leading-[1.2]">
          {t("title")} <span className="text-orange-text">{t("titleAccent")}</span>
        </h1>
        <p className="m-0 max-w-[560px] text-base leading-[1.75] text-ink-muted md:text-[19px]">{t("subtitle")}</p>
        <div className="flex flex-col gap-3 sm:flex-row sm:flex-wrap">
          <Link href="/products" className="flex h-[52px] items-center justify-center rounded-control bg-carbon px-[26px] font-bold text-on-carbon">
            {t("browse")}
          </Link>
          <Link href="/#recommend" className="flex h-[52px] items-center justify-center rounded-control bg-orange px-[26px] font-bold text-on-orange">
            {t("recommend")}
          </Link>
        </div>
      </div>
      <div className="flex min-w-0 flex-1 basis-[360px] flex-col gap-2.5">
        <span className="text-sm font-bold text-ink-muted">{t("startHere")}</span>
        {paths.map((entry) => (
          <Link key={entry.href} href={entry.href} className={path}>
            <span className="flex flex-col gap-1">
              <b className="text-lg">{entry.title}</b>
              <span className="text-sm text-ink-muted">{entry.body}</span>
            </span>
            <span aria-hidden="true" className="text-[22px] text-orange-text rtl:-scale-x-100">›</span>
          </Link>
        ))}
        <Link href="/#recommend" className="flex items-center justify-between gap-3 rounded-[10px] bg-carbon p-5 text-on-carbon">
          <span className="flex flex-col gap-1">
            <b className="text-lg">{t("paths.unsure.title")}</b>
            <span className="text-sm text-footer-text">{t("paths.unsure.body")}</span>
          </span>
          <span aria-hidden="true" className="text-[22px] text-orange rtl:-scale-x-100">›</span>
        </Link>
      </div>
    </section>
  );
}
