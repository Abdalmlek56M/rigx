import type { SVGProps } from "react";

function Icon({ children, size = 20, ...rest }: SVGProps<SVGSVGElement> & { size?: number }) {
  return (
    <svg
      width={size}
      height={size}
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth={2}
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
      className="shrink-0"
      {...rest}
    >
      {children}
    </svg>
  );
}

type IconProps = { size?: number };

const solid = { fill: "currentColor", stroke: "none" } as const;

export const WhatsAppIcon = (p: IconProps) => (
  <Icon {...p}>
    <path d="M4 20l1.3-3.9A8 8 0 1 1 8.2 19z" />
    <path d="M9 9.5c0 3 2.5 5.5 5.5 5.5l1-1.5-2-1-1 .8a4 4 0 0 1-1.8-1.8l.8-1-1-2z" {...solid} />
  </Icon>
);
export const TelegramIcon = (p: IconProps) => (
  <Icon {...p}>
    <path d="M21 4L3 11l6 2 2 6 3-4 5 4z" />
    <path d="M9 13l12-9" />
  </Icon>
);
export const FacebookIcon = (p: IconProps) => (
  <Icon {...p}>
    <path d="M14 8h3V4h-3a4 4 0 0 0-4 4v2H7v4h3v8h4v-8h3l1-4h-4V8z" {...solid} />
  </Icon>
);
export const InstagramIcon = (p: IconProps) => (
  <Icon {...p}>
    <rect x="3" y="3" width="18" height="18" rx="5" />
    <circle cx="12" cy="12" r="4" />
    <circle cx="17.5" cy="6.5" r="1" fill="currentColor" />
  </Icon>
);
export const TikTokIcon = (p: IconProps) => (
  <Icon {...p}>
    <path d="M14 3v12a4 4 0 1 1-4-4" />
    <path d="M14 3c.6 2.6 2.4 4.2 5 4.4" />
  </Icon>
);
export const YouTubeIcon = (p: IconProps) => (
  <Icon {...p}>
    <rect x="2" y="5" width="20" height="14" rx="4" />
    <path d="M10 9l5 3-5 3z" fill="currentColor" />
  </Icon>
);
export const PhoneIcon = (p: IconProps) => (
  <Icon {...p}>
    <path d="M5 4h4l2 5-2.5 1.5a11 11 0 0 0 5 5L15 13l5 2v4a2 2 0 0 1-2 2A16 16 0 0 1 3 6a2 2 0 0 1 2-2z" />
  </Icon>
);
export const MailIcon = (p: IconProps) => (
  <Icon {...p}>
    <rect x="3" y="5" width="18" height="14" rx="2" />
    <path d="M3 7l9 6 9-6" />
  </Icon>
);
export const WrenchIcon = (p: IconProps) => (
  <Icon {...p}>
    <path d="M14 6a4 4 0 0 0 5 5l2-2a6 6 0 0 1-8 8l-6 6-3-3 6-6a6 6 0 0 1 8-8z" />
  </Icon>
);
export const DocumentIcon = (p: IconProps) => (
  <Icon {...p}>
    <path d="M6 2h9l5 5v15H6zM14 2v6h6M9 13h8M9 17h8" />
  </Icon>
);
export const UserIcon = (p: IconProps) => (
  <Icon {...p}>
    <circle cx="12" cy="8" r="4" />
    <path d="M4 21a8 8 0 0 1 16 0" />
  </Icon>
);
export const CartIcon = (p: IconProps) => (
  <Icon {...p}>
    <path d="M3 4h2l2.4 11h11.2L21 8H7" />
    <circle cx="9" cy="20" r="1.5" />
    <circle cx="18" cy="20" r="1.5" />
  </Icon>
);
export const MenuIcon = (p: IconProps) => (
  <Icon {...p}>
    <path d="M4 7h16M4 12h16M4 17h16" />
  </Icon>
);
export const CloseIcon = (p: IconProps) => (
  <Icon {...p}>
    <path d="M6 6l12 12M18 6L6 18" />
  </Icon>
);
export const MoonIcon = (p: IconProps) => (
  <Icon {...p}>
    <path d="M20 14.5A8 8 0 0 1 9.5 4a8 8 0 1 0 10.5 10.5z" />
  </Icon>
);
export const SunIcon = (p: IconProps) => (
  <Icon {...p}>
    <circle cx="12" cy="12" r="4" />
    <path d="M12 2v2M12 20v2M2 12h2M20 12h2M4.9 4.9l1.4 1.4M17.7 17.7l1.4 1.4M4.9 19.1l1.4-1.4M17.7 6.3l1.4-1.4" />
  </Icon>
);
export const ChevronDownIcon = (p: IconProps) => (
  <Icon {...p}>
    <path d="M6 9l6 6 6-6" />
  </Icon>
);
export const ArrowUpIcon = (p: IconProps) => (
  <Icon {...p}>
    <path d="M12 19V5M5 12l7-7 7 7" />
  </Icon>
);

export const ImagePlaceholderIcon = ({ size = 104 }: IconProps) => (
  <svg width={size} height={Math.round(size * 0.54)} viewBox="0 0 120 64" fill="none" stroke="currentColor" strokeWidth={2} aria-hidden="true">
    <rect x="2" y="6" width="116" height="48" rx="4" />
    <circle cx="36" cy="30" r="16" />
    <circle cx="84" cy="30" r="16" />
  </svg>
);
export const ChevronStartIcon = (p: IconProps) => (
  <Icon {...p}>
    <path d="M15 6l-6 6 6 6" />
  </Icon>
);
export const FilterIcon = (p: IconProps) => (
  <Icon {...p}>
    <path d="M4 5h16l-6 8v6l-4-2v-4z" />
  </Icon>
);
