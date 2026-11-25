"use client";

import { useTranslations } from "next-intl";
import { Button, ButtonLink } from "@/components/ui/Button";
import { ModalSheet } from "@/components/ui/ModalSheet";
import { LockIcon, UserIcon } from "@/components/ui/icons";
import { loginHref } from "@/features/auth/auth-rules";

interface SignInDialogProps {
  open: boolean;
  onClose: () => void;
  /** Where sign-in comes back to: the builder with the same parts. */
  next: string;
}

/** The window in the middle of the screen that asks a signed-out visitor to sign in before keeping a build private or showing it publicly. */
export function SignInDialog({ open, onClose, next }: SignInDialogProps) {
  const t = useTranslations("builder.signIn");

  return (
    <ModalSheet open={open} onClose={onClose} label={t("title")} edge="center">
      <div className="flex flex-col items-center gap-4 px-5 pb-5 pt-7 text-center sm:px-7">
        <span aria-hidden="true" className="flex size-14 items-center justify-center rounded-full bg-orange-soft text-orange-text shadow-[0_0_0_6px_color-mix(in_srgb,var(--orange-soft)_50%,transparent)]">
          <LockIcon size={24} />
        </span>
        <div className="flex flex-col gap-2">
          <h2 className="type-section-title m-0">{t("title")}</h2>
          <p className="m-0 text-sm leading-6 text-ink-muted">{t("body")}</p>
        </div>
        <div className="flex w-full flex-col gap-2 pt-1 sm:flex-row">
          <ButtonLink href={loginHref(next)} variant="accent" icon={UserIcon} className="flex-1">{t("signIn")}</ButtonLink>
          <Button type="button" variant="ghost" onClick={onClose} className="flex-1">{t("later")}</Button>
        </div>
      </div>
    </ModalSheet>
  );
}
