"use client";

import { useCallback, useState } from "react";
import { useTranslations } from "next-intl";
import { Link } from "@/i18n/navigation";
import { useLoad } from "@/lib/hooks/use-load";
import { fetchBrands } from "../brands/brands-api";
import { fetchCategories } from "../categories/categories-api";
import { fetchWarrantyLabels } from "../inventory/inventory-api";
import { ErrorPanel, LoadingCard } from "../kit/LoadState";
import { PageHeader } from "../kit/PageHeader";
import { PhotoGallery } from "../stock/PhotoGallery";
import { usePartners } from "../stock/use-partners";
import { usePermissions } from "../use-permissions";
import { BuildActions } from "./BuildActions";
import { BuildAllocations } from "./BuildAllocations";
import { BuildCardForm } from "./BuildCardForm";
import { BuildParts } from "./BuildParts";
import { BuildPlannedParts } from "./BuildPlannedParts";
import { BuildStatusBadge } from "./BuildStatusBadge";
import { BuildSummary } from "./BuildSummary";
import { deleteBuildPhoto, fetchBuild, reorderBuildPhotos, uploadBuildPhoto, type Build } from "./builds-api";
import { isLocked } from "./build-rules";
import { ArrowBackIcon } from "@/components/ui/icons";
import { cardClass } from "@/components/ui/Card";
import { buttonClass, buttonIconSize } from "../kit/Button";

/** `/admin/builds/[id]` (ADM-Build): the card, parts, revenue split and photos. Every write sends the build's row version and shows the build the server returned. */
export function BuildEditor({ id }: { id: string }) {
  const t = useTranslations("admin.builds");
  const { has } = usePermissions();
  const partners = usePartners(has("Finance.ViewReports"));
  const canReadWarranties = has("System.StoreSettings");
  const load = useCallback(async () => {
    const [build, tree, brands, warrantyLabels] = await Promise.all([
      fetchBuild(id),
      fetchCategories().catch(() => []),
      fetchBrands().catch(() => []),
      canReadWarranties ? fetchWarrantyLabels().catch(() => null) : Promise.resolve(null)
    ]);
    return { build, lookups: { tree, brands, warrantyLabels } };
  }, [id, canReadWarranties]);
  const { state, reload } = useLoad(load);
  // The build as the last write returned it; a 409 drops it and reads the build again.
  const [changed, setChanged] = useState<Build | null>(null);
  const stale = () => {
    setChanged(null);
    reload();
  };

  const back = <Link href="/admin/builds" className={buttonClass()}><ArrowBackIcon size={buttonIconSize()} />{t("back")}</Link>;
  if (state.status === "loading") {
    return (
      <>
        <PageHeader title={t("detailTitle")} actions={back} />
        <LoadingCard className="h-64" label={t("loading")} />
      </>
    );
  }
  if (state.status === "error") {
    return (
      <>
        <PageHeader title={t("detailTitle")} actions={back} />
        <ErrorPanel message={t("failed")} retryLabel={t("retry")} onRetry={reload} />
      </>
    );
  }

  const build = changed ?? state.data.build;
  const canEdit = has("Catalog.ManageProducts") && !isLocked(build);
  const action = { build, onChanged: setChanged, onStale: stale };
  const lookups = { ...state.data.lookups, partners };
  const refresh = async () => setChanged(await fetchBuild(id));

  return (
    <>
      <PageHeader
        title={build.code}
        actions={
          <>
            <BuildStatusBadge status={build.status} />
            {back}
          </>
        }
      />
      <section className={cardClass({ className: "gap-3" })}>
        <h2 dir="auto" className="m-0 text-[17px] font-bold [unicode-bidi:isolate]">{build.name}</h2>
        {isLocked(build) ? <p role="status" className="m-0 rounded-control bg-line px-3 py-2 text-sm font-bold text-ink-muted">{t("lockedNote")}</p> : null}
        <BuildActions {...action} canEdit={has("Catalog.ManageProducts")} />
      </section>
      <div className="grid items-start gap-4 xl:grid-cols-[minmax(0,1fr)_minmax(0,1fr)]">
        <BuildCardForm key={build.rowVersion} {...action} warrantyLabels={state.data.lookups.warrantyLabels} canEdit={canEdit} />
        <div className="flex min-w-0 flex-col gap-4">
          <BuildSummary build={build} />
          <PhotoGallery
            photos={build.photos} name={build.name} canManage={canEdit} onChanged={refresh}
            upload={(file) => uploadBuildPhoto(build.id, file)} remove={(storageKey) => deleteBuildPhoto(build.id, storageKey)}
            reorder={(keys) => reorderBuildPhotos(build.id, keys)}
          />
        </div>
      </div>
      <BuildParts {...action} canEdit={canEdit} lookups={lookups} />
      <BuildPlannedParts key={`planned-${build.rowVersion}`} {...action} canEdit={canEdit} lookups={lookups} />
      <BuildAllocations key={build.rowVersion} {...action} canEdit={canEdit} />
    </>
  );
}
