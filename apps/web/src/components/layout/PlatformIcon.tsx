import type { ReactElement } from "react";
import {
  FacebookIcon, InstagramIcon, PhoneIcon, TelegramIcon, TikTokIcon, WhatsAppIcon, YouTubeIcon
} from "@/components/ui/icons";
import { Platform } from "@/lib/contact";

const ICONS: Record<string, (props: { size?: number }) => ReactElement> = {
  [Platform.WhatsApp]: WhatsAppIcon,
  [Platform.Phone]: PhoneIcon,
  [Platform.Telegram]: TelegramIcon,
  [Platform.Facebook]: FacebookIcon,
  [Platform.Instagram]: InstagramIcon,
  [Platform.TikTok]: TikTokIcon,
  [Platform.YouTube]: YouTubeIcon
};

export function PlatformIcon({ platform, size }: { platform: string; size?: number }) {
  const Component = ICONS[platform];
  return Component ? <Component {...(size ? { size } : {})} /> : null;
}
