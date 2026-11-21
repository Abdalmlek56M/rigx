import { getTranslations } from "next-intl/server";
import { SkeletonCards } from "@/components/ui/Skeleton";

/** The catalog grid's loading state: card skeletons in the same grid, so the page does not jump when results arrive. */
export default async function ProductsLoading() {
  const t = await getTranslations("catalog");
  return (
    <section className="shell flex flex-col gap-5 pb-16 pt-10">
      <SkeletonCards count={9} label={t("loading")} />
    </section>
  );
}
