import { useTranslations } from "next-intl";
import { Link } from "@/i18n/navigation";

const SLOTS = ["CPU", "GPU", "RAM", "PSU"] as const;
const DONE: readonly string[] = ["CPU", "GPU"];

/** The dark "build it yourself" card next to the recommend widget. */
export function BuilderPromo() {
  const t = useTranslations("home.builderPromo");
  return (
    <div className="flex flex-1 basis-[420px] flex-col gap-4 rounded-panel bg-carbon p-6 text-on-carbon md:p-8">
      <span className="text-[13px] font-bold text-orange">{t("eyebrow")}</span>
      <h2 className="m-0 text-[28px] font-bold">{t("title")}</h2>
      <p className="m-0 text-[15px] leading-[1.75] text-footer-text">{t("body")}</p>
      <ul aria-hidden="true" className="m-0 grid list-none grid-cols-4 gap-1.5 p-0">
        {SLOTS.map((slot) => (
          <li key={slot} dir="ltr" className={`rounded bg-field px-1.5 py-2.5 text-center font-mono text-xs ${DONE.includes(slot) ? "" : "text-muted"}`}>
            {slot}
            {DONE.includes(slot) ? " ✓" : ""}
          </li>
        ))}
      </ul>
      <Link href="/builder" className="flex h-[50px] items-center self-start rounded-control bg-on-carbon px-6 font-bold text-carbon">
        {t("start")}
      </Link>
    </div>
  );
}
