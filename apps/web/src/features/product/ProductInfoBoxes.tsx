import { useTranslations } from "next-intl";
import { Card } from "@/components/ui/Card";
import { InfoField, InfoGrid } from "@/components/ui/InfoGrid";
import { InfoIcon, ShieldIcon, TruckIcon, UndoIcon, WalletIcon } from "@/components/ui/icons";
import { Link } from "@/i18n/navigation";
import type { StorefrontData } from "@/lib/storefront";

const linkClass = "text-orange-text underline hover:text-orange-text-hover";

/** Warranty, returns, payment methods and pickup/shipping, from the item and the store settings. */
export function ProductInfoBoxes({ warrantyLabel, storefront }: { warrantyLabel: string | null; storefront: StorefrontData }) {
  const t = useTranslations("product.info");
  const { payment, pickupEnabled, pickupAddress } = storefront;
  const methods = [payment.cash && t("payment.cash"), payment.instaPay && t("payment.instaPay"), payment.wallet && t("payment.wallet")].filter(Boolean);

  return (
    <Card title={t("title")} icon={InfoIcon} padding="md" className="gap-3">
      <InfoGrid columns={2} className="gap-y-3.5">
        <InfoField
          label={t("warranty")}
          icon={ShieldIcon}
          value={<><span>{warrantyLabel ?? t("warranty")}</span>{" "}<Link href="/policies/warranty" className={linkClass}>{t("warrantyLink")}</Link></>}
        />
        <InfoField
          label={t("returns")}
          icon={UndoIcon}
          value={<Link href="/policies/returns" className={linkClass}>{t("returnsLink")}</Link>}
        />
        {methods.length > 0 ? <InfoField label={t("paymentTitle")} icon={WalletIcon} value={methods.join("، ")} /> : null}
        <InfoField
          label={t("pickupTitle")}
          icon={TruckIcon}
          value={pickupEnabled && pickupAddress ? t("pickupAndShipping", { address: pickupAddress }) : t("shippingOnly")}
        />
      </InfoGrid>
    </Card>
  );
}
