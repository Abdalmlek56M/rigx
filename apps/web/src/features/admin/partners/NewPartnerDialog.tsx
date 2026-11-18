"use client";

import { useTranslations } from "next-intl";
import type { Partner } from "../expenses/expenses-api";
import { Dialog } from "../kit/Dialog";
import { PartnerForm } from "./PartnerForm";

/** "شريك جديد": the agreement form in a dialog; the new partner opens below the list once created. */
export function NewPartnerDialog({ today, onCreated, onClose }: { today: string; onCreated: (partner: Partner) => void; onClose: () => void }) {
  const t = useTranslations("admin.partners.form");
  return (
    <Dialog open onClose={onClose} title={t("newTitle")}>
      <PartnerForm partner={null} today={today} canEdit onSaved={onCreated} onCancel={onClose} />
    </Dialog>
  );
}
