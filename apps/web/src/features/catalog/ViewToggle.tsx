import { getTranslations } from "next-intl/server";
import { cardClass } from "@/components/ui/Card";
import { GridIcon, ListIcon } from "@/components/ui/icons";
import { Link } from "@/i18n/navigation";
import { catalogHref, type CatalogState, type ViewMode } from "./catalog-query";

const MODES: readonly { mode: ViewMode; icon: typeof GridIcon }[] = [
  { mode: "grid", icon: GridIcon },
  { mode: "list", icon: ListIcon }
];

/** Two icon buttons that switch the results between a grid of cards and one row per item; the choice lives in the URL. */
export async function ViewToggle({ state }: { state: CatalogState }) {
  const t = await getTranslations("catalog.view");
  const selected = MODES.findIndex((entry) => entry.mode === state.view);

  return (
    <div role="group" aria-label={t("label")} className={cardClass({ layout: "grid", padding: "none", className: "relative grid-cols-2 gap-0 p-1" })}>
      <span
        aria-hidden="true"
        style={{ transform: `translateX(calc(var(--dir) * ${Math.max(0, selected)} * 100%))` }}
        className="absolute inset-y-1 start-1 w-[calc((100%-0.5rem)/2)] rounded-control surface-strong transition-transform duration-150 ease-out motion-reduce:transition-none"
      />
      {MODES.map(({ mode, icon: ModeIcon }) => {
        const active = state.view === mode;
        return (
          <Link
            key={mode}
            href={catalogHref(state, { view: mode })}
            aria-label={t(mode)}
            data-tip={t(mode)}
            aria-current={active ? "true" : undefined}
            className={`tip relative z-10 flex h-9 w-10 items-center justify-center rounded-control transition-colors duration-150 motion-reduce:transition-none ${active ? "text-on-carbon" : "text-ink-muted hover:text-ink"}`}
          >
            <ModeIcon size={18} />
          </Link>
        );
      })}
    </div>
  );
}
