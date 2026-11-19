import { useTranslations } from "next-intl";
import { CpuIcon, GpuIcon, PlusIcon, PowerIcon, RamIcon } from "@/components/ui/icons";
import { Link } from "@/i18n/navigation";

const SLOTS = [
  { name: "CPU", icon: CpuIcon },
  { name: "GPU", icon: GpuIcon },
  { name: "RAM", icon: RamIcon },
  { name: "PSU", icon: PowerIcon }
] as const;

/** The light "build it yourself" card next to the recommend widget, with a sketch of the builder's empty slots. */
export function BuilderPromo() {
  const t = useTranslations("home.builderPromo");
  return (
    <div className="accent-panel relative flex flex-1 basis-[420px] flex-col gap-4 overflow-hidden rounded-panel border p-6 md:p-8">
      <div className="flex flex-col gap-1">
        <p className="m-0 text-[15px] font-bold text-orange-text">{t("eyebrow")}</p>
        <h2 className="m-0 text-[28px] font-bold">{t("title")}</h2>
      </div>
      <p className="m-0 max-w-[52ch] text-[15px] leading-[1.75] text-ink-muted">{t("body")}</p>
      <ul aria-hidden="true" className="m-0 grid list-none grid-cols-2 gap-2 p-0 sm:grid-cols-4">
        {SLOTS.map(({ name, icon: SlotIcon }) => (
          <li
            key={name}
            dir="ltr"
            className="flex items-center justify-between gap-2 rounded-card border border-dashed border-input-line bg-surface px-3 py-2.5 text-xs font-semibold text-ink-muted"
          >
            <span className="flex items-center gap-2">
              <span className="text-orange-text"><SlotIcon size={16} /></span>
              {name}
            </span>
            <PlusIcon size={14} />
          </li>
        ))}
      </ul>
      <Link href="/builder" className="press mt-1 flex h-[50px] items-center self-start rounded-full bg-orange px-6 font-bold text-on-orange transition-[filter] hover:brightness-105">
        {t("start")}
      </Link>
    </div>
  );
}
