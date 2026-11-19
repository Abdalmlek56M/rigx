"use client";

import { useTranslations } from "next-intl";
import { formMessage } from "../form-message";
import { FormMessage } from "../kit/PageHeader";
import { StockUnitPicker } from "./StockUnitPicker";
import { addStockPart } from "./builds-api";
import { useBuildAction, type BuildActionProps } from "./use-build-action";
import { CloseIcon } from "@/components/ui/icons";
import { Button } from "../kit/Button";

/** Add a part from free stock: pick one of the available units and it joins the build at once. */
export function AddStockPartPanel({ onClose, ...action }: BuildActionProps & { onClose: () => void }) {
  const t = useTranslations("admin.builds.addStock");
  const { build } = action;
  const { submit, run } = useBuildAction(action);

  return (
    <div className="flex flex-col gap-3 rounded-control border border-line bg-paper p-3">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h3 className="m-0 text-[15px] font-bold">{t("title")}</h3>
        <Button size="sm" icon={CloseIcon} onClick={onClose}>{t("close")}</Button>
      </div>
      <StockUnitPicker pickedId="" actionLabel={t("add")} onPick={(unit) => void run(() => addStockPart(build.id, unit.id, build.rowVersion), t("added"))} />
      <FormMessage message={formMessage(submit.errors)} />
      {submit.dialog}
    </div>
  );
}
