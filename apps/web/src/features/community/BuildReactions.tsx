"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { unwrap } from "@rigx/api-client";
import { Button } from "@/components/ui/Button";
import { EyeIcon, HeartIcon, ThumbUpIcon } from "@/components/ui/icons";
import { loginHref } from "@/features/auth/auth-rules";
import { useSession } from "@/features/auth/use-session";
import { usePathname, useRouter } from "@/i18n/navigation";
import { browserApi } from "@/lib/api/browser";
import { required } from "@/lib/api/required";
import { isOn, toggled, type ReactionKind, type Reactions } from "./reaction-rules";

/** Sends the reaction to the API: pressing it again takes it back. Both calls are idempotent and answer with the new counts. */
function send(token: string, kind: ReactionKind, remove: boolean): Promise<Reactions> {
  const params = { params: { path: { token, kind } } };
  const api = browserApi();
  return required(unwrap(remove ? api.DELETE("/api/v1/builder/saved/{token}/reactions/{kind}", params) : api.PUT("/api/v1/builder/saved/{token}/reactions/{kind}", params)));
}

/**
 * A heart (it goes to the favourites) and a like on a shared build, each with its count, and beside them how many visitors opened it. A visitor who is not signed in is taken to sign in
 * first; a failed save flips the button back and says so. The server's answer replaces the counts the screen guessed.
 */
export function BuildReactions({ token, initial }: { token: string; initial: Reactions }) {
  const t = useTranslations("reactions");
  const session = useSession();
  const router = useRouter();
  const pathname = usePathname();
  const [reactions, setReactions] = useState(initial);
  const [failed, setFailed] = useState(false);

  async function press(kind: ReactionKind) {
    if (session.status === "loading") return;
    if (session.status === "signedOut") {
      router.push(loginHref(pathname));
      return;
    }
    const before = reactions;
    setFailed(false);
    setReactions(toggled(before, kind));
    try {
      setReactions(await send(token, kind, isOn(before, kind)));
    } catch {
      setReactions(before);
      setFailed(true);
    }
  }

  const buttons = [
    { kind: "Heart" as const, icon: HeartIcon, count: reactions.hearts, on: reactions.hearted, label: t(reactions.hearted ? "heartRemove" : "heartAdd") },
    { kind: "Like" as const, icon: ThumbUpIcon, count: reactions.likes, on: reactions.liked, label: t(reactions.liked ? "likeRemove" : "likeAdd") }
  ];

  return (
    <div className="flex flex-wrap items-center gap-2">
      <span
        title={t("views", { count: reactions.views })} aria-label={t("views", { count: reactions.views })}
        className="inline-flex h-9 items-center gap-1.5 rounded-control px-2 text-[13px] font-bold text-ink-muted"
      >
        <EyeIcon size={16} />
        <span dir="ltr" className="font-mono tabular-nums">{reactions.views}</span>
      </span>
      {buttons.map(({ kind, icon: ReactionIcon, count, on, label }) => (
        <Button
          key={kind}
          type="button"
          variant="secondary"
          size="sm"
          aria-label={label}
          aria-pressed={on}
          title={session.status === "signedOut" ? t("signIn") : label}
          icon={({ size }: { size?: number }) => <ReactionIcon size={size} filled={on} />}
          onClick={() => void press(kind)}
          className={on ? "text-orange-text" : "text-ink-muted"}
        >
          <span dir="ltr" className="font-mono tabular-nums">{count}</span>
        </Button>
      ))}
      {failed ? <span role="alert" className="text-xs text-error-text">{t("failed")}</span> : null}
    </div>
  );
}
