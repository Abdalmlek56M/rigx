import { getTranslations } from "next-intl/server";
import type { Schemas } from "@rigx/api-client";
import { ButtonLink } from "@/components/ui/Button";
import { parseUsage } from "@/lib/usage";

const STARTS = ["Gaming", "Editing"] as const;

/** "ابدأ من تجميعة جاهزة": opens the first published build of that usage for editing. */
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
        <ButtonLink key={start.usage} href={`/builder?from=${start.code}`} variant="secondary" size="sm">
          {t(start.usage)}
        </ButtonLink>
      ))}
    </div>
  );
}
