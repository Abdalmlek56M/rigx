import { getTranslations } from "next-intl/server";
import { SkeletonCards } from "@/components/ui/Skeleton";

/** The builds list's loading state: card skeletons in the same grid. */
export default async function BuildsLoading() {
  const t = await getTranslations("builds");
  return (
    <section className="shell flex flex-col gap-5 pb-16 pt-10">
      <SkeletonCards count={4} label={t("loading")} />
    </section>
  );
}
