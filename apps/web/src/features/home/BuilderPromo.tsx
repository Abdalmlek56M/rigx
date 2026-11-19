import { useTranslations } from "next-intl";
import { CheckIcon, CpuIcon, GpuIcon, PowerIcon, RamIcon } from "@/components/ui/icons";
import { Link } from "@/i18n/navigation";

const SLOTS = [
  { name: "CPU", icon: CpuIcon, done: true },
  { name: "GPU", icon: GpuIcon, done: true },
  { name: "RAM", icon: RamIcon, done: false },
  { name: "PSU", icon: PowerIcon, done: false }
] as const;

/** The dark "build it yourself" card next to the recommend widget, with a sketch of the builder's slots. */
export function BuilderPromo() {
  const t = useTranslations("home.builderPromo");
  return (
    <div className="relative flex flex-1 basis-[420px] flex-col gap-4 overflow-hidden rounded-panel bg-carbon p-6 text-on-carbon md:p-8">
      <div className="flex flex-col gap-1">
        <h2 className="m-0 text-[28px] font-bold">{t("title")}</h2>
        <p className="m-0 text-[15px] text-orange">{t("eyebrow")}</p>
      </div>
      <p className="m-0 max-w-[52ch] text-[15px] leading-[1.75] text-footer-text">{t("body")}</p>
      <ul aria-hidden="true" className="m-0 grid list-none grid-cols-2 gap-2 p-0 sm:grid-cols-4">
        {SLOTS.map(({ name, icon: SlotIcon, done }) => (
          <li
            key={name}
            dir="ltr"
            className={`flex items-center justify-between gap-2 rounded-card border px-3 py-2.5 text-xs font-semibold ${done ? "border-orange/40 bg-orange/10 text-on-carbon" : "border-footer-line text-footer-text"}`}
          >
            <span className="flex items-center gap-2">
              <SlotIcon size={16} />
              {name}
            </span>
            {done ? <span className="text-orange"><CheckIcon size={14} /></span> : null}
          </li>
        ))}
      </ul>
      <Link href="/builder" className="press mt-1 flex h-[50px] items-center self-start rounded-full bg-orange px-6 font-bold text-on-orange transition-[filter] hover:brightness-105">
        {t("start")}
      </Link>
    </div>
  );
}
