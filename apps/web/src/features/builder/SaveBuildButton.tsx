"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { unwrap } from "@rigx/api-client";
import { Button } from "@/components/ui/Button";
import { FolderIcon } from "@/components/ui/icons";
import { loginHref } from "@/features/auth/auth-rules";
import { useSession } from "@/features/auth/use-session";
import { usePathname, useRouter } from "@/i18n/navigation";
import { browserApi } from "@/lib/api/browser";
import { required } from "@/lib/api/required";

/**
 * "Save to my builds": keeps a private copy of this build (it may be another customer's, public or shared by link) in the visitor's account, where
 * it can be reopened and edited. Someone who is not signed in is taken to sign in first and comes back to this page.
 */
export function SaveBuildButton({ token }: { token: string }) {
  const t = useTranslations("sharedBuild.save");
  const session = useSession();
  const router = useRouter();
  const pathname = usePathname();
  const [busy, setBusy] = useState(false);
  const [failed, setFailed] = useState(false);

  async function save() {
    if (session.status === "loading") return;
    if (session.status === "signedOut") {
      router.push(loginHref(pathname));
      return;
    }
    setBusy(true);
    setFailed(false);
    try {
      await required(unwrap(browserApi().POST("/api/v1/builder/saved/{token}/copy", { params: { path: { token } } })));
      router.push("/account/builds");
    } catch {
      setFailed(true);
      setBusy(false);
    }
  }

  return (
    <>
      <Button variant="secondary" icon={FolderIcon} loading={busy} onClick={() => void save()} className="w-full">{t("button")}</Button>
      {failed ? <span role="alert" className="text-xs text-error-text">{t("failed")}</span> : null}
    </>
  );
}
