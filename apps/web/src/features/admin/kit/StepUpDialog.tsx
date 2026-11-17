"use client";

import { useCallback, useState, type ReactNode } from "react";
import { useTranslations } from "next-intl";
import { StepUpCode } from "@/features/account/StepUpCode";
import { Dialog } from "./Dialog";
import { stepUpHeaders } from "./form-errors";

/** A protected request, run with the step-up headers; it throws when it fails (a wrong code keeps the dialog open). */
export type StepUpAction = (headers: Record<string, string>) => Promise<void>;

/**
 * Destructive admin actions need the code emailed to the signed-in user (`X-Step-Up-Code`).
 * `confirm(action)` opens the dialog; once the code is typed, `action` runs with the header and the dialog closes on success.
 * Render `dialog` once in the page.
 */
export function useStepUp(): { confirm: (action: StepUpAction) => void; dialog: ReactNode } {
  const t = useTranslations("admin.kit");
  const [action, setAction] = useState<StepUpAction | null>(null);
  const close = useCallback(() => setAction(null), []);
  const confirm = useCallback((next: StepUpAction) => setAction(() => next), []);

  const dialog = (
    <Dialog open={action !== null} onClose={close} title={t("stepUpTitle")}>
      {action ? (
        <StepUpCode
          onCancel={close}
          onConfirm={async (code) => {
            await action(stepUpHeaders(code));
            close();
          }}
        />
      ) : null}
    </Dialog>
  );

  return { confirm, dialog };
}
