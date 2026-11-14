import { useTranslations } from "next-intl";
import { Link } from "@/i18n/navigation";
import type { StorefrontData } from "@/lib/storefront";

const box = "flex flex-col gap-0.5 rounded-control bg-sand/40 p-3 text-sm";
const linkClass = "text-orange-text underline hover:text-orange-text-hover";

/** Warranty, returns, payment methods and pickup/shipping, from the item and the store settings. */
export function ProductInfoBoxes({ warrantyLabel, storefront }: { warrantyLabel: string | null; storefront: StorefrontData }) {
  const t = useTranslations("product.info");
  const { payment, pickupEnabled, pickupAddress } = storefront;
  const methods = [payment.cash && t("payment.cash"), payment.instaPay && t("payment.instaPay"), payment.wallet && t("payment.wallet")].filter(Boolean);

  return (
    <div className="grid grid-cols-2 gap-2">
      <div className={box}>
        <b>{warrantyLabel ?? t("warranty")}</b>
        <Link href="/policies/warranty" className={linkClass}>{t("warrantyLink")}</Link>
      </div>
      <div className={box}>
        <b>{t("returns")}</b>
        <Link href="/policies/returns" className={linkClass}>{t("returnsLink")}</Link>
      </div>
      {methods.length > 0 ? (
        <div className={box}>
          <b>{t("paymentTitle")}</b>
          <span className="text-ink-muted">{methods.join(" · ")}</span>
        </div>
      ) : null}
      <div className={box}>
        <b>{t("pickupTitle")}</b>
        <span className="text-ink-muted">{pickupEnabled && pickupAddress ? t("pickupAndShipping", { address: pickupAddress }) : t("shippingOnly")}</span>
      </div>
    </div>
  );
}
