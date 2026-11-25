import type { ComponentType, ReactNode } from "react";
import { useTranslations } from "next-intl";
import { Card } from "@/components/ui/Card";
import { BoxIcon, InfoIcon, MapPinIcon, ShieldIcon, TruckIcon, UndoIcon } from "@/components/ui/icons";
import { Link } from "@/i18n/navigation";
import type { StorefrontData } from "@/lib/storefront";

const linkClass = "font-semibold text-orange-text underline hover:text-orange-text-hover";

function InfoRow({ icon: Icon, label, children }: { icon: ComponentType<{ size?: number }>; label: string; children: ReactNode }) {
  return (
    <div className="flex gap-3 py-3.5 first:pt-0 last:pb-0 [&+&]:border-t [&+&]:border-line md:py-0 md:[&+&]:border-t-0">
      <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-orange-soft text-orange-text">
        <Icon size={18} />
      </span>
      <div className="flex min-w-0 flex-1 flex-col gap-1.5">
        <span className="type-label">{label}</span>
        <div className="text-sm leading-6">{children}</div>
      </div>
    </div>
  );
}

/** Warranty, returns and pickup/shipping, from the item and the store settings; one row each, three columns from `md` (the card spans the page width). */
export function ProductInfoBoxes({ warrantyLabel, storefront }: { warrantyLabel: string | null; storefront: Pick<StorefrontData, "pickupEnabled" | "pickupAddress"> }) {
  const t = useTranslations("product.info");
  const { pickupEnabled, pickupAddress } = storefront;

  return (
    <Card title={t("title")} icon={InfoIcon} padding="md" className="gap-2">
      <div className="flex flex-col md:grid md:grid-cols-3 md:gap-x-8">
        <InfoRow icon={ShieldIcon} label={t("warranty")}>
          <div className="flex flex-wrap items-center gap-x-2 gap-y-1">
            {warrantyLabel ? <span className="type-value">{warrantyLabel}</span> : null}
            <Link href="/policies/warranty" className={linkClass}>{t("warrantyLink")}</Link>
          </div>
        </InfoRow>
        <InfoRow icon={UndoIcon} label={t("returns")}>
          <Link href="/policies/returns" className={linkClass}>{t("returnsLink")}</Link>
        </InfoRow>
        <InfoRow icon={BoxIcon} label={t("pickupTitle")}>
          <ul className="m-0 flex list-none flex-col gap-2 p-0">
            {pickupEnabled && pickupAddress ? (
              <li className="flex items-start gap-2">
                <span aria-hidden="true" className="mt-1 inline-flex text-orange-text"><MapPinIcon size={15} /></span>
                <span className="flex min-w-0 flex-col">
                  <span className="type-value">{t("pickupFrom")}</span>
                  <span className="text-xs text-ink-muted">{pickupAddress}</span>
                </span>
              </li>
            ) : null}
            <li className="flex items-center gap-2 type-value">
              <span aria-hidden="true" className="inline-flex text-orange-text"><TruckIcon size={15} /></span>
              {t("shippingEverywhere")}
            </li>
          </ul>
        </InfoRow>
      </div>
    </Card>
  );
}
