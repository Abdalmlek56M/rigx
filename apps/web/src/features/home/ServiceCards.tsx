import type { ComponentType } from "react";
import { useTranslations } from "next-intl";
import { cardClass } from "@/components/ui/Card";
import { ChevronStartIcon, GearIcon, InboxIcon, SwapIcon, ThermometerIcon, WrenchIcon } from "@/components/ui/icons";
import { Link } from "@/i18n/navigation";

interface Service {
  key: "trade" | "request" | "assemble" | "maintain";
  icon: ComponentType<{ size?: number }>;
  href: string;
  /** The icon turns half a turn on hover. */
  turns?: true;
  /** Runs hot and cools on hover, with the example temperature reading. */
  heat?: true;
}

const SERVICES: readonly Service[] = [
  { key: "trade", icon: SwapIcon, href: "/services#sell", turns: true },
  { key: "request", icon: InboxIcon, href: "/services#service-sourcing" },
  { key: "assemble", icon: WrenchIcon, href: "/services#service-build" },
  { key: "maintain", icon: GearIcon, href: "/services#service-maintenance", heat: true }
];

/** An illustration, never a measurement: the temperature before and after a cleaning. Labelled as an example on the card. */
const EXAMPLE_BEFORE = 85;
const EXAMPLE_AFTER = 62;

/** The reading counts down from 85° to 62° in CSS (`.heat-reading`); screen readers get the same numbers as text. */
function ExampleGauge({ label }: { label: string }) {
  return (
    <span className="mt-auto flex items-end gap-4 pt-1">
      <span className="flex min-w-0 flex-1 flex-col gap-2">
        <span aria-hidden="true" className="gauge"><span className="gauge-fill"><span className="gauge-fill-cool" /></span></span>
        <span className="text-xs text-ink-muted">{label}</span>
      </span>
      <span aria-hidden="true" dir="ltr" className="heat-reading flex items-center gap-1 font-mono text-[28px] leading-none font-bold tabular-nums">
        <ThermometerIcon size={20} />
      </span>
      <span className="sr-only">{EXAMPLE_BEFORE}° → {EXAMPLE_AFTER}°</span>
    </span>
  );
}

/** Four service teasers, each opening its own part of the services page. The swap icon turns on hover; the maintenance card runs hot and cools on hover. */
export function ServiceCards() {
  const t = useTranslations("home.services");
  return (
    <section className="shell py-12 md:py-16">
      <h2 className="sr-only">{t("title")}</h2>
      <ul className="m-0 grid list-none grid-cols-[repeat(auto-fit,minmax(240px,1fr))] gap-3.5 p-0">
        {SERVICES.map((service) => {
          const ServiceIcon = service.icon;
          return (
            <li key={service.key} className="flex">
              <Link href={service.href} className={`${cardClass({ padding: "md", interactive: true })} path-tile h-full w-full gap-3 ${service.heat ? "heat-card" : ""}`}>
                <span className="flex items-center justify-between">
                  <span className={`flex h-11 w-11 items-center justify-center rounded-full ${service.heat ? "heat-icon" : "bg-orange-soft text-orange-text"}`}>
                    <span className={service.turns ? "swap-icon flex" : "flex"}><ServiceIcon size={20} /></span>
                  </span>
                  <span aria-hidden="true" className="path-arrow flex text-muted ltr:-scale-x-100"><ChevronStartIcon size={20} /></span>
                </span>
                <b className="text-lg">{t(`${service.key}.title`)}</b>
                <span className="text-sm leading-[1.6] text-ink-muted">{t(`${service.key}.body`)}</span>
                {service.heat ? <ExampleGauge label={t("maintain.example")} /> : null}
              </Link>
            </li>
          );
        })}
      </ul>
    </section>
  );
}
