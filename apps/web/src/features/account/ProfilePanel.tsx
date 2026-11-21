"use client";

import { useTranslations } from "next-intl";
import { useSession } from "@/features/auth/use-session";
import { Card } from "@/components/ui/Card";
import { KeyIcon } from "@/components/ui/icons";
import { AvatarEditor } from "./AvatarEditor";
import { EmailChange } from "./EmailChange";
import { LoginMethods } from "./LoginMethods";
import { NewDeviceAlerts } from "./NewDeviceAlerts";
import { ProfileForm } from "./ProfileForm";
import { SessionsList } from "./SessionsList";

/** "بياناتي والأمان": profile, picture, email, login methods, new-device alerts and signed-in devices. */
export function ProfilePanel() {
  const t = useTranslations("account.profile");
  const session = useSession();
  if (session.status !== "signedIn") return null;
  const { user } = session;

  return (
    <div className="flex flex-col gap-4">
      <h2 className="sr-only">{t("title")}</h2>
      <Card padding="md" className="gap-4">
        <AvatarEditor user={user} />
        <ProfileForm user={user} />
      </Card>
      <Card title={t("security")} icon={KeyIcon} padding="md" className="gap-4">
        <EmailChange email={user.email} />
        <LoginMethods />
        <NewDeviceAlerts />
      </Card>
      <Card padding="md" className="gap-4">
        <SessionsList />
      </Card>
    </div>
  );
}
