import { useTranslations } from "next-intl";
import { ChevronStartIcon, GearIcon, HandshakeIcon, InboxIcon, WrenchIcon } from "@/components/ui/icons";
import { Link } from "@/i18n/navigation";

const SERVICES = [
  { key: "trade", icon: HandshakeIcon, href: "/services#sell" },
  { key: "request", icon: InboxIcon, href: "/services#service-sourcing" },
  { key: "assemble", icon: WrenchIcon, href: "/services#service-build" },
  { key: "maintain", icon: GearIcon, href: "/services#service-maintenance" }
] as const;

/** Four service teasers, each opening its own part of the services page. */
export function ServiceCards() {
  const t = useTranslations("home.services");
  return (
    <section className="shell py-12 md:py-16">
      <h2 className="sr-only">{t("title")}</h2>
      <ul className="m-0 grid list-none grid-cols-[repeat(auto-fit,minmax(240px,1fr))] gap-3.5 p-0">
        {SERVICES.map(({ key, icon: ServiceIcon, href }) => (
          <li key={key} className="flex">
            <Link href={href} className="path-tile lift press flex w-full flex-col gap-3 rounded-card border border-line bg-surface p-[22px]">
              <span className="flex items-center justify-between">
                <span className="flex h-11 w-11 items-center justify-center rounded-full bg-orange-soft text-orange-text"><ServiceIcon size={20} /></span>
                <span aria-hidden="true" className="path-arrow flex text-muted ltr:-scale-x-100"><ChevronStartIcon size={20} /></span>
              </span>
              <b className="text-lg">{t(`${key}.title`)}</b>
              <span className="text-sm leading-[1.6] text-ink-muted">{t(`${key}.body`)}</span>
            </Link>
          </li>
        ))}
      </ul>
    </section>
  );
}
