"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { isConflict } from "@/lib/api/conflict";
import { Dialog } from "../kit/Dialog";
import { FormMessage } from "../kit/PageHeader";
import { useSubmit } from "../kit/use-submit";
import { PartnerFields } from "../stock/PartnerFields";
import { amountText } from "../stock/stock-fields";
import type { PartnerSource } from "../stock/use-partners";
import { setUnitPartner, type StockUnit } from "./inventory-api";
import { unitPercent } from "./inventory-rules";
import { SaveIcon } from "@/components/ui/icons";
import { Button } from "../kit/Button";

/** Changes who financed one unit (and the share); sends the unit's own row version, and a 409 reads the item again. */
export function UnitPartnerDialog({ unit, partners, onSaved, onStale, onClose }: {
  unit: StockUnit;
  partners: PartnerSource;
  onSaved: () => Promise<void>;
  onStale: () => void;
  onClose: () => void;
}) {
  const t = useTranslations("admin.inventory.units.partnerDialog");
  const submit = useSubmit();
  const [partnerId, setPartnerId] = useState(unit.partnerId ?? "");
  const [percent, setPercent] = useState(amountText(unit.partnerPercentOverride));

  async function save() {
    await submit.run(async () => {
      try {
        await setUnitPartner(unit.id, partnerId || null, unitPercent(partnerId, percent), unit.rowVersion);
      } catch (error) {
        if (isConflict(error)) onStale();
        throw error;
      }
      await onSaved();
      onClose();
    }, t("saved"));
  }

  return (
    <Dialog open onClose={onClose} title={t("title")}>
      <PartnerFields
        source={partners} keepId={unit.partnerId} partnerId={partnerId} percent={percent} namePrefix="unitPartner"
        onChange={(patch) => {
          if (patch.partnerId !== undefined) setPartnerId(patch.partnerId);
          if (patch.percent !== undefined) setPercent(patch.percent);
        }}
        errors={{ partnerId: submit.errors.fields.partnerId, percent: submit.errors.fields.percent }}
      />
      <FormMessage message={submit.errors.general} />
      <div className="flex flex-wrap justify-end gap-2">
        <Button variant="ghost" onClick={onClose}>{t("cancel")}</Button>
        <Button variant="primary" icon={SaveIcon} loading={submit.busy} onClick={() => void save()}>{t("save")}</Button>
      </div>
      {submit.dialog}
    </Dialog>
  );
}
