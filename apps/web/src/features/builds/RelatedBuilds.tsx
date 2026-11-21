import type { Schemas } from "@rigx/api-client";
import { getTranslations } from "next-intl/server";
import { BuildCard } from "@/features/builds/BuildCard";
import { Rail } from "@/features/home/Rail";
import { Link } from "@/i18n/navigation";

/** Other published builds at the bottom of a build page; hidden when there are none. */
export async function RelatedBuilds({ builds }: { builds: Schemas["AssemblyPublicView"][] }) {
  if (builds.length === 0) return null;
  const t = await getTranslations("build");

  return (
    <Rail
      title={t("relatedTitle")}
      arrowsOnMobile
      action={
        <Link href="/builds" className="press inline-flex min-h-10 items-center rounded-full px-0 text-sm font-bold text-orange-text hover:text-orange-text-hover">
          {t("relatedMore")}
        </Link>
      }
    >
      {builds.map((build) => (
        <li key={build.code} className="flex w-[320px] shrink-0 md:w-[380px]">
          <BuildCard build={build} />
        </li>
      ))}
    </Rail>
  );
}
