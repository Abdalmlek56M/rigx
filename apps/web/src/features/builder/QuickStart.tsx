import { getTranslations } from "next-intl/server";
import type { Schemas } from "@rigx/api-client";
import { Link } from "@/i18n/navigation";
import { parseUsage } from "@/lib/usage";

const STARTS = ["Gaming", "Editing"] as const;

/** "ابدأ من تجميعة ألعاب / مونتاج": opens the first published build of that usage for editing. */
export async function QuickStart({ builds }: { builds: Schemas["AssemblyPublicView"][] }) {
  const t = await getTranslations("builder.quickStart");
  const starts = STARTS.flatMap((usage) => {
    const build = builds.find((candidate) => parseUsage(candidate.usage) === usage);
    return build ? [{ usage, code: build.code }] : [];
  });
  if (starts.length === 0) return null;

  return (
    <div role="group" aria-label={t("label")} className="flex flex-wrap gap-1.5">
      {starts.map((start) => (
        <Link
          key={start.usage}
          href={`/builder?from=${start.code}`}
          className="flex h-11 items-center rounded-control border border-input-line bg-surface px-4 text-sm"
        >
          {t(start.usage)}
        </Link>
      ))}
    </div>
  );
}
