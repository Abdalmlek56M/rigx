import type { ComponentType, ReactNode } from "react";
import { useTranslations } from "next-intl";
import { Card } from "@/components/ui/Card";
import { InfoIcon, ShieldIcon, TruckIcon, UndoIcon, WalletIcon } from "@/components/ui/icons";
import { Link } from "@/i18n/navigation";
import type { StorefrontData } from "@/lib/storefront";

const linkClass = "font-semibold text-orange-text underline hover:text-orange-text-hover";

function InfoRow({ icon: Icon, label, children }: { icon: ComponentType<{ size?: number }>; label: string; children: ReactNode }) {
  return (
    <div className="flex gap-3 py-3.5 first:pt-0 last:pb-0 [&+&]:border-t [&+&]:border-line">
      <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-paper text-ink-muted">
        <Icon size={18} />
      </span>
      <div className="flex min-w-0 flex-1 flex-col gap-1.5">
        <span className="type-label">{label}</span>
        <div className="text-sm leading-6">{children}</div>
      </div>
    </div>
  );
}

/** Warranty, returns, payment methods and pickup/shipping, from the item and the store settings; one row each. */
export function ProductInfoBoxes({ warrantyLabel, storefront }: { warrantyLabel: string | null; storefront: StorefrontData }) {
  const t = useTranslations("product.info");
  const { payment, pickupEnabled, pickupAddress } = storefront;
  const methods = [
    payment.cash ? { key: "cash", label: t("payment.cash"), ltr: false } : null,
    payment.instaPay ? { key: "instaPay", label: t("payment.instaPay"), ltr: true } : null,
    payment.wallet ? { key: "wallet", label: t("payment.wallet"), ltr: false } : null
  ].filter((method): method is { key: string; label: string; ltr: boolean } => method !== null);

  return (
    <Card title={t("title")} icon={InfoIcon} padding="md" className="gap-2">
      <div className="flex flex-col">
        <InfoRow icon={ShieldIcon} label={t("warranty")}>
          <div className="flex flex-wrap items-center gap-x-2 gap-y-1">
            {warrantyLabel ? <span className="type-value">{warrantyLabel}</span> : null}
            <Link href="/policies/warranty" className={linkClass}>{t("warrantyLink")}</Link>
          </div>
        </InfoRow>
        <InfoRow icon={UndoIcon} label={t("returns")}>
          <Link href="/policies/returns" className={linkClass}>{t("returnsLink")}</Link>
        </InfoRow>
        {methods.length > 0 ? (
          <InfoRow icon={WalletIcon} label={t("paymentTitle")}>
            <div className="flex flex-wrap gap-1.5">
              {methods.map((method) => (
                <span
                  key={method.key}
                  dir={method.ltr ? "ltr" : undefined}
                  className="inline-flex items-center rounded-full border border-line bg-paper px-2.5 py-1 text-xs font-bold"
                >
                  {method.label}
                </span>
              ))}
            </div>
          </InfoRow>
        ) : null}
        <InfoRow icon={TruckIcon} label={t("pickupTitle")}>
          <div className="flex flex-col gap-3">
            {pickupEnabled && pickupAddress ? (
              <div className="flex flex-col gap-0.5">
                <span className="text-xs text-ink-muted">{t("pickupFrom")}</span>
                <span className="type-value">{pickupAddress}</span>
              </div>
            ) : null}
            <div className="flex flex-col gap-0.5">
              <span className="text-xs text-ink-muted">{t("shippingTitle")}</span>
              <span className="type-value">{t("shippingAll")}</span>
            </div>
          </div>
        </InfoRow>
      </div>
    </Card>
  );
}
