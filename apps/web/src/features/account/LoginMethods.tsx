"use client";

import { useCallback, useState } from "react";
import { useTranslations } from "next-intl";
import { STEP_UP_HEADER, unwrap } from "@rigx/api-client";
import { GoogleIcon, MailIcon, UnlinkIcon } from "@/components/ui/icons";
import { GoogleButton } from "@/features/auth/GoogleButton";
import { browserApi } from "@/lib/api/browser";
import { fetchLoginMethods } from "./account-api";
import { ConfirmButton } from "./ConfirmButton";
import { LoadBoundary } from "./LoadBoundary";
import { StepUpCode } from "./StepUpCode";
import { useLoad } from "@/lib/hooks/use-load";

function Row({ icon, name, status, action }: { icon: React.ReactNode; name: string; status: string; action?: React.ReactNode }) {
  return (
    <li className="flex flex-wrap items-center justify-between gap-3 border-b border-line py-3 last:border-b-0">
      <div className="flex min-w-0 items-center gap-3">
        <span className="flex h-10 w-10 shrink-0 items-center justify-center rounded-full border border-line bg-paper">{icon}</span>
        <div className="flex min-w-0 flex-col">
        <b className="text-sm">{name}</b>
        <span dir="auto" className="break-all text-sm text-ink-muted [unicode-bidi:isolate]">{status}</span>
        </div>
      </div>
      {action}
    </li>
  );
}

/** Email code is always on; Google can be linked (after a security code) or unlinked. */
export function LoginMethods() {
  const t = useTranslations("account.profile.methods");
  const { state, reload } = useLoad(fetchLoginMethods);
  const [pendingToken, setPendingToken] = useState<string | null>(null);
  const [failed, setFailed] = useState(false);
  const onCredential = useCallback((idToken: string) => setPendingToken(idToken), []);

  async function link(code: string) {
    await unwrap(browserApi().POST("/api/v1/identity/login-methods/google", { body: { idToken: pendingToken }, headers: { [STEP_UP_HEADER]: code } }));
    setPendingToken(null);
    reload();
  }

  async function unlink() {
    setFailed(false);
    try {
      await unwrap(browserApi().DELETE("/api/v1/identity/login-methods/google"));
      reload();
    } catch {
      setFailed(true);
    }
  }

  return (
    <div className="flex flex-col gap-3">
      <h3 className="m-0 text-base font-bold">{t("title")}</h3>
      <LoadBoundary state={state} reload={reload}>
        {(methods) => (
          <>
            <ul className="m-0 list-none p-0">
              <Row icon={<MailIcon size={18} />} name={t("emailCode")} status={t("on")} />
              <Row
                icon={<GoogleIcon size={18} />}
                name="Google"
                status={methods.googleEmail ?? t("notLinked")}
                action={methods.googleEmail ? <ConfirmButton label={t("unlink")} icon={<UnlinkIcon size={16} />} onConfirm={() => void unlink()} /> : undefined}
              />
            </ul>
            {failed ? <p role="alert" className="m-0 text-sm text-error">{t("unlinkFailed")}</p> : null}
            {!methods.googleEmail && pendingToken === null ? <GoogleButton onCredential={onCredential} /> : null}
            {pendingToken !== null ? <StepUpCode onConfirm={link} onCancel={() => setPendingToken(null)} /> : null}
          </>
        )}
      </LoadBoundary>
    </div>
  );
}
