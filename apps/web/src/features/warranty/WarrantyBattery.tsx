import type { CSSProperties } from "react";
import { useTranslations } from "next-intl";
import { DaysCountUp } from "./DaysCountUp";
import type { Battery } from "./warranty-battery";

/** A battery that shows how much of the warranty is left (green, amber, red) beside the days that remain; an expired one is empty. */
export function WarrantyBattery({ battery, days }: { battery: Battery; days: number | null }) {
  const t = useTranslations("account.warranties");
  const percent = Math.round(battery.charge * 100);

  return (
    <div className="flex items-center gap-4">
      <div
        role="meter"
        aria-label={t("batteryLabel")}
        aria-valuemin={0}
        aria-valuemax={100}
        aria-valuenow={percent}
        data-tone={battery.tone}
        className="battery w-full max-w-[220px]"
      >
        <div className="battery-fill" style={{ "--charge": battery.charge } as CSSProperties} />
      </div>
      {days !== null ? <span className="type-value whitespace-nowrap"><DaysCountUp days={days} /></span> : null}
    </div>
  );
}
