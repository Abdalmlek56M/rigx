"use client";

import { useTranslations } from "next-intl";
import { IconButton } from "@/components/ui/Button";
import { CloseIcon } from "@/components/ui/icons";
import { ModalSheet } from "@/components/ui/ModalSheet";
import { LoginFlow } from "@/features/auth/LoginFlow";

/** Sign-in inside checkout: the sheet hosts the same email-code / Google flow and closes when the person is in. */
export function SignInSheet({ open, onClose }: { open: boolean; onClose: () => void }) {
  const t = useTranslations("checkout.customer");
  const tc = useTranslations("common");

  return (
    <ModalSheet open={open} onClose={onClose} label={t("loginTitle")} edge="end">
      <div className="flex h-full flex-col gap-5 overflow-y-auto p-5 sm:p-6">
        <div className="flex justify-end">
          <IconButton icon={CloseIcon} label={tc("close")} variant="secondary" onClick={onClose} />
        </div>
        <LoginFlow next={undefined} onSignedIn={onClose} />
      </div>
    </ModalSheet>
  );
}
