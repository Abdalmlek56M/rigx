"use client";

import { useState, type ReactNode } from "react";
import { useTranslations } from "next-intl";
import { isStepUpRequired, NO_ERRORS, toFormErrors, type FormErrors } from "./form-errors";
import { useStepUp, type StepUpAction } from "./StepUpDialog";
import { useToast } from "./Toast";

interface Submit {
  busy: boolean;
  errors: FormErrors;
  /**
   * Runs a save: field errors land in `errors`, other failures in `errors.general`, success shows `success` as a toast.
   * When the server asks for the step-up code, the code dialog opens and the same action runs again with it.
   */
  run: (action: StepUpAction, success?: string) => Promise<void>;
  /** The step-up dialog; render it once in the form. */
  dialog: ReactNode;
}

export function useSubmit(): Submit {
  const t = useTranslations("admin.kit");
  const toast = useToast();
  const stepUp = useStepUp();
  const [busy, setBusy] = useState(false);
  const [errors, setErrors] = useState<FormErrors>(NO_ERRORS);

  async function run(action: StepUpAction, success: string = t("saved")) {
    setBusy(true);
    setErrors(NO_ERRORS);
    try {
      await action({});
      toast(success);
    } catch (error) {
      if (isStepUpRequired(error)) {
        stepUp.confirm(async (headers) => {
          await action(headers);
          toast(success);
        });
      } else {
        setErrors(toFormErrors(error, t("failed")));
      }
    } finally {
      setBusy(false);
    }
  }

  return { busy, errors, run, dialog: stepUp.dialog };
}
