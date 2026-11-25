"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { Button } from "@/components/ui/Button";
import { TextField } from "@/components/ui/TextField";
import { CopyIcon, ShareIcon } from "@/components/ui/icons";
import { useSession } from "@/features/auth/use-session";
import { SHARE_NAME_MAX } from "./builder-state";
import type { ShareLink } from "./share-link";
import type { Visibility } from "./build-visibility";
import { SignInDialog } from "./SignInDialog";
import { VisibilityPicker } from "./VisibilityPicker";

interface ShareBoxProps {
  /** There are parts and their check is done. */
  canShare: boolean;
  shared: ShareLink | null;
  builderPath: string;
  onCopyLink: () => void;
  name: string;
  onNameChange: (value: string) => void;
  visibility: Visibility;
  onVisibilityChange: (value: Visibility) => void;
  shareFailed: boolean;
}

/** The note after the link was copied: what was saved and who can open it. */
function copiedKey(shared: ShareLink): "copiedPlain" | "copiedSaved" | "copiedPrivate" | "copiedPublic" {
  if (!shared.saved) return "copiedPlain";
  return shared.visibility === "Private" ? "copiedPrivate" : shared.visibility === "Public" ? "copiedPublic" : "copiedSaved";
}

/**
 * "مشاركة التجميعة" as its own framed box: a heading with a line on what the link does, the optional name, who can open it, and the copy button.
 * A signed-out visitor sees the same three choices; picking private or public opens the sign-in window in the middle of the screen.
 */
export function ShareBox({ canShare, shared, builderPath, onCopyLink, name, onNameChange, visibility, onVisibilityChange, shareFailed }: ShareBoxProps) {
  const t = useTranslations("builder.actions");
  const session = useSession();
  const [asking, setAsking] = useState(false);

  function choose(value: Visibility) {
    if (session.status === "signedIn") onVisibilityChange(value);
    else if (value !== "Link") setAsking(true);
  }

  return (
    <section aria-labelledby="share-box-title" className="flex flex-col gap-4 rounded-card border border-line bg-paper/50 p-4">
      <div className="flex items-start gap-3">
        <span aria-hidden="true" className="flex size-9 shrink-0 items-center justify-center rounded-full bg-orange-soft text-orange-text">
          <ShareIcon size={17} />
        </span>
        <span className="flex min-w-0 flex-col gap-0.5">
          <h3 id="share-box-title" className="m-0 text-sm font-bold">{t("shareTitle")}</h3>
          <span className="type-label">{t("shareHelp")}</span>
        </span>
      </div>
      <TextField
        label={t("shareName")}
        labelEnd={<span className="rounded-full bg-paper px-2 py-0.5 text-[11px] font-semibold text-ink-muted">{t("optional")}</span>}
        name="share-name"
        value={name}
        onChange={onNameChange}
        maxLength={SHARE_NAME_MAX}
        placeholder={t("shareNamePlaceholder")}
      />
      <div className="flex flex-col gap-2">
        <span className="text-xs font-bold">{t("whoSees")}</span>
        <VisibilityPicker value={session.status === "signedIn" ? visibility : "Link"} onChange={choose} busy={session.status === "loading"} />
      </div>
      <div className="flex flex-col gap-1.5">
        <Button type="button" variant="secondary" icon={CopyIcon} disabled={!canShare} onClick={onCopyLink} className="w-full">
          {t("copyLink")}
        </Button>
        <span role="status" className={`text-xs leading-5 empty:hidden ${shareFailed ? "text-error-text" : "text-success"}`}>
          {shareFailed ? t("shareFailed") : shared ? t(copiedKey(shared)) : ""}
        </span>
      </div>
      <SignInDialog open={asking} onClose={() => setAsking(false)} next={builderPath} />
    </section>
  );
}
