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
export const PhoneOffIcon = (p: IconProps) => (
  <Icon {...p}>
    <path d="M5 4h4l2 5-2.5 1.5a11 11 0 0 0 5 5L15 13l5 2v4a2 2 0 0 1-2 2A16 16 0 0 1 3 6a2 2 0 0 1 2-2z" />
    <path d="M3 3l18 18" />
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

// —— Parts and catalog categories (same 24px grid, 2px round stroke) ——————————————————
export const GpuIcon = (p: IconProps) => (
  <Icon {...p}>
    <rect x="2" y="6" width="20" height="11" rx="2" />
    <circle cx="9" cy="11.5" r="2.5" />
    <circle cx="16" cy="11.5" r="2.5" />
    <path d="M5 17v3M9 17v2" />
  </Icon>
);
export const CpuIcon = (p: IconProps) => (
  <Icon {...p}>
    <rect x="6" y="6" width="12" height="12" rx="2" />
    <rect x="9.5" y="9.5" width="5" height="5" rx="1" />
    <path d="M9 2v4M15 2v4M9 18v4M15 18v4M2 9h4M2 15h4M18 9h4M18 15h4" />
  </Icon>
);
export const BoardIcon = (p: IconProps) => (
  <Icon {...p}>
    <rect x="3" y="3" width="18" height="18" rx="2" />
    <rect x="7" y="7" width="5" height="5" rx="1" />
    <path d="M15 7h2M15 10h2M7 16h10" />
  </Icon>
);
export const RamIcon = (p: IconProps) => (
  <Icon {...p}>
    <path d="M3 7h18v9H3z" />
    <path d="M7 10v3M11 10v3M15 10v3M5 16v3M9 16v3M15 16v3M19 16v3" />
  </Icon>
);
export const StorageIcon = (p: IconProps) => (
  <Icon {...p}>
    <rect x="3" y="5" width="18" height="14" rx="2" />
    <path d="M7 15h6" />
    <circle cx="17" cy="15" r="1" fill="currentColor" />
  </Icon>
);
export const PowerIcon = (p: IconProps) => (
  <Icon {...p}>
    <path d="M13 2L4 14h7l-1 8 9-12h-7z" />
  </Icon>
);
export const FanIcon = (p: IconProps) => (
  <Icon {...p}>
    <circle cx="12" cy="12" r="9" />
    <circle cx="12" cy="12" r="1.5" />
    <path d="M12 10.5c0-3 1-5 3.5-5M13.5 12c3 0 5 1 5 3.5M12 13.5c0 3-1 5-3.5 5M10.5 12c-3 0-5-1-5-3.5" />
  </Icon>
);
export const PencilIcon = (p: IconProps) => (
  <Icon {...p}>
    <path d="M4 20h4L19 9a2.8 2.8 0 0 0-4-4L4 16v4z" />
    <path d="m13.5 6.5 4 4" />
  </Icon>
);
export const PlusIcon = (p: IconProps) => (
  <Icon {...p}>
    <path d="M12 5v14M5 12h14" />
  </Icon>
);
export const MinusIcon = (p: IconProps) => (
  <Icon {...p}>
    <path d="M5 12h14" />
  </Icon>
);
export const ShareIcon = (p: IconProps) => (
  <Icon {...p}>
    <circle cx="18" cy="5" r="3" />
    <circle cx="6" cy="12" r="3" />
    <circle cx="18" cy="19" r="3" />
    <path d="M8.6 10.5l6.8-4M8.6 13.5l6.8 4" />
  </Icon>
);
export const HeadsetIcon = (p: IconProps) => (
  <Icon {...p}>
    <path d="M4 14v-2a8 8 0 0 1 16 0v2" />
    <rect x="3" y="14" width="4" height="6" rx="1.5" />
    <rect x="17" y="14" width="4" height="6" rx="1.5" />
  </Icon>
);
export const CaseIcon = (p: IconProps) => (
  <Icon {...p}>
    <rect x="6" y="2" width="12" height="20" rx="2" />
    <path d="M9 6h6M9 9h6" />
    <circle cx="12" cy="16" r="2" />
  </Icon>
);
export const MonitorIcon = (p: IconProps) => (
  <Icon {...p}>
    <rect x="2" y="4" width="20" height="13" rx="2" />
    <path d="M8 21h8M12 17v4" />
  </Icon>
);

// —— Account, admin areas, empty states ——————————————————————————————————————————
export const BoxIcon = (p: IconProps) => (
  <Icon {...p}>
    <path d="M3 7.5L12 3l9 4.5v9L12 21l-9-4.5z" />
    <path d="M3 7.5l9 4.5 9-4.5M12 12v9" />
  </Icon>
);
export const ShieldIcon = (p: IconProps) => (
  <Icon {...p}>
    <path d="M12 3l8 3v6c0 4.5-3.4 8.2-8 9-4.6-.8-8-4.5-8-9V6z" />
    <path d="M9 12l2 2 4-4" />
  </Icon>
);
export const InboxIcon = (p: IconProps) => (
  <Icon {...p}>
    <path d="M3 13l3-8h12l3 8v6H3z" />
    <path d="M3 13h5l1 2h6l1-2h5" />
  </Icon>
);
export const MapPinIcon = (p: IconProps) => (
  <Icon {...p}>
    <path d="M12 21s-7-6.2-7-11.5a7 7 0 0 1 14 0C19 14.8 12 21 12 21z" />
    <circle cx="12" cy="9.5" r="2.5" />
  </Icon>
);
export const GridIcon = (p: IconProps) => (
  <Icon {...p}>
    <rect x="3" y="3" width="7" height="7" rx="1.5" />
    <rect x="14" y="3" width="7" height="7" rx="1.5" />
    <rect x="3" y="14" width="7" height="7" rx="1.5" />
    <rect x="14" y="14" width="7" height="7" rx="1.5" />
  </Icon>
);
export const ListIcon = (p: IconProps) => (
  <Icon {...p}>
    <rect x="3" y="4" width="18" height="4.5" rx="1.5" />
    <rect x="3" y="10" width="18" height="4.5" rx="1.5" />
    <rect x="3" y="16" width="18" height="4.5" rx="1.5" />
  </Icon>
);
export const RegisterIcon = (p: IconProps) => (
  <Icon {...p}>
    <rect x="3" y="11" width="18" height="10" rx="2" />
    <path d="M6 11V5h8v6M17 11V8M7 15h2M11 15h2M15 15h2M7 18h10" />
  </Icon>
);
export const UsersIcon = (p: IconProps) => (
  <Icon {...p}>
    <circle cx="9" cy="8" r="3.5" />
    <path d="M2.5 20a6.5 6.5 0 0 1 13 0M16 4.5a3.5 3.5 0 0 1 0 7M18.5 14.5A6.5 6.5 0 0 1 21.5 20" />
  </Icon>
);
export const TagIcon = (p: IconProps) => (
  <Icon {...p}>
    <path d="M3 3h8l10 10-8 8L3 11z" />
    <circle cx="7.5" cy="7.5" r="1.5" />
  </Icon>
);
export const TicketIcon = (p: IconProps) => (
  <Icon {...p}>
    <path d="M3 8a2 2 0 0 0 0 4v0a2 2 0 0 1 0 4v2h18v-2a2 2 0 0 1 0-4v0a2 2 0 0 0 0-4V6H3z" />
    <path d="M14 6v2M14 11v2M14 16v2" />
  </Icon>
);
export const TruckIcon = (p: IconProps) => (
  <Icon {...p}>
    <path d="M2 6h12v10H2zM14 9h4l4 4v3h-8" />
    <circle cx="6" cy="18" r="2" />
    <circle cx="18" cy="18" r="2" />
  </Icon>
);
export const FolderIcon = (p: IconProps) => (
  <Icon {...p}>
    <path d="M3 6a2 2 0 0 1 2-2h4l2 2h8a2 2 0 0 1 2 2v10a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z" />
  </Icon>
);
export const HandshakeIcon = (p: IconProps) => (
  <Icon {...p}>
    <path d="M2 12l4-5 4 2 2-1 2 1 4-2 4 5-7 6-2-1-2 1z" />
    <path d="M10 9l-2 3 2 1 3-2" />
  </Icon>
);
export const WalletIcon = (p: IconProps) => (
  <Icon {...p}>
    <path d="M4 6h14a2 2 0 0 1 2 2v10a2 2 0 0 1-2 2H4z" />
    <path d="M4 6a2 2 0 0 1 2-2h10v2M15 13h5" />
  </Icon>
);
export const HelpIcon = (p: IconProps) => (
  <Icon {...p}>
    <circle cx="12" cy="12" r="9" />
    <path d="M9.5 9.5a2.5 2.5 0 1 1 3.5 2.3c-.6.3-1 .9-1 1.6V14M12 17.5v.01" />
  </Icon>
);
export const KeyIcon = (p: IconProps) => (
  <Icon {...p}>
    <circle cx="8" cy="15" r="4" />
    <path d="M11 12l9-9M17 6l3 3M14 9l2 2" />
  </Icon>
);
export const ClockIcon = (p: IconProps) => (
  <Icon {...p}>
    <circle cx="12" cy="12" r="9" />
    <path d="M12 7v5l3 2" />
  </Icon>
);
export const DatabaseIcon = (p: IconProps) => (
  <Icon {...p}>
    <ellipse cx="12" cy="5.5" rx="8" ry="3" />
    <path d="M4 5.5v13c0 1.7 3.6 3 8 3s8-1.3 8-3v-13M4 12c0 1.7 3.6 3 8 3s8-1.3 8-3" />
  </Icon>
);
export const GearIcon = (p: IconProps) => (
  <Icon {...p}>
    <circle cx="12" cy="12" r="3" />
    <path d="M12 2v3M12 19v3M2 12h3M19 12h3M4.9 4.9L7 7M17 17l2.1 2.1M4.9 19.1L7 17M17 7l2.1-2.1" />
  </Icon>
);
export const GlobeIcon = (p: IconProps) => (
  <Icon {...p}>
    <circle cx="12" cy="12" r="9" />
    <path d="M3 12h18M12 3c2.5 2.4 4 5.5 4 9s-1.5 6.6-4 9c-2.5-2.4-4-5.5-4-9s1.5-6.6 4-9z" />
  </Icon>
);
export const SearchIcon = (p: IconProps) => (
  <Icon {...p}>
    <circle cx="11" cy="11" r="7" />
    <path d="M20 20l-4-4" />
  </Icon>
);
export const GamepadIcon = (p: IconProps) => (
  <Icon {...p}>
    <path d="M7 7h10a5 5 0 0 1 4.8 6.4l-.9 3.1a2.5 2.5 0 0 1-4.3.9L14.5 15h-5l-2.1 2.4a2.5 2.5 0 0 1-4.3-.9l-.9-3.1A5 5 0 0 1 7 7z" />
    <path d="M8 10v3M6.5 11.5h3M15.5 11h.01M17.5 12.5h.01" />
  </Icon>
);
export const FilmIcon = (p: IconProps) => (
  <Icon {...p}>
    <rect x="3" y="4" width="18" height="16" rx="2" />
    <path d="M7 4v16M17 4v16M3 9h4M3 15h4M17 9h4M17 15h4" />
  </Icon>
);
export const BookIcon = (p: IconProps) => (
  <Icon {...p}>
    <path d="M4 5a2 2 0 0 1 2-2h13v15H6a2 2 0 0 0-2 2z" />
    <path d="M4 20a2 2 0 0 0 2 1h13v-3M9 7h6" />
  </Icon>
);
export const BroadcastIcon = (p: IconProps) => (
  <Icon {...p}>
    <circle cx="12" cy="12" r="2" />
    <path d="M8.5 8.5a5 5 0 0 0 0 7M15.5 8.5a5 5 0 0 1 0 7M5.6 5.6a9 9 0 0 0 0 12.8M18.4 5.6a9 9 0 0 1 0 12.8" />
  </Icon>
);
export const CheckIcon = (p: IconProps) => (
  <Icon {...p}>
    <path d="M5 12.5l4.5 4.5L19 7.5" />
  </Icon>
);
export const UnlinkIcon = (p: IconProps) => (
  <Icon {...p}>
    <path d="M9.5 14.5l-1.8 1.8a3 3 0 0 1-4.2-4.2l3-3a3 3 0 0 1 4.2 0M14.5 9.5l1.8-1.8a3 3 0 0 1 4.2 4.2l-3 3a3 3 0 0 1-4.2 0" />
    <path d="M4 4l16 16" />
  </Icon>
);
export const GoogleIcon = ({ size = 20 }: IconProps) => (
  <svg width={size} height={size} viewBox="0 0 48 48" aria-hidden="true" className="shrink-0">
    <path fill="#FFC107" d="M43.6 20.5H42V20H24v8h11.3C33.7 32.7 29.2 36 24 36c-6.6 0-12-5.4-12-12s5.4-12 12-12c3.1 0 5.8 1.2 7.9 3.1l5.7-5.7C34 6.1 29.3 4 24 4 12.9 4 4 12.9 4 24s8.9 20 20 20 20-8.9 20-20c0-1.3-.1-2.4-.4-3.5z" />
    <path fill="#FF3D00" d="M6.3 14.7l6.6 4.8C14.7 15.1 19 12 24 12c3.1 0 5.8 1.2 7.9 3.1l5.7-5.7C34 6.1 29.3 4 24 4 16.3 4 9.7 8.3 6.3 14.7z" />
    <path fill="#4CAF50" d="M24 44c5.2 0 9.9-2 13.4-5.2l-6.2-5.2C29.2 35.1 26.7 36 24 36c-5.2 0-9.6-3.3-11.3-8l-6.5 5C9.5 39.6 16.2 44 24 44z" />
    <path fill="#1976D2" d="M43.6 20.5H42V20H24v8h11.3c-.8 2.2-2.2 4.2-4.1 5.6l6.2 5.2C37 39.2 44 34 44 24c0-1.3-.1-2.4-.4-3.5z" />
  </svg>
);
export const TrashIcon = (p: IconProps) => (
  <Icon {...p}>
    <path d="M4 7h16M10 11v6M14 11v6M6 7l1 12a2 2 0 0 0 2 2h6a2 2 0 0 0 2-2l1-12M9 7V4h6v3" />
  </Icon>
);
export const LogoutIcon = (p: IconProps) => (
  <Icon {...p}>
    <path d="M14 4h4a2 2 0 0 1 2 2v12a2 2 0 0 1-2 2h-4M10 16l-4-4 4-4M6 12h10" />
  </Icon>
);

/* Admin action icons (4.10b C): one per verb on the admin buttons. */
export const DownloadIcon = (p: IconProps) => (
  <Icon {...p}>
    <path d="M12 4v11M7 10l5 5 5-5M5 20h14" />
  </Icon>
);
export const UploadIcon = (p: IconProps) => (
  <Icon {...p}>
    <path d="M12 16V5M7 10l5-5 5 5M5 20h14" />
  </Icon>
);
export const PrinterIcon = (p: IconProps) => (
  <Icon {...p}>
    <path d="M7 9V3h10v6M7 17H5a2 2 0 0 1-2-2v-4a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2v4a2 2 0 0 1-2 2h-2" />
    <path d="M7 14h10v7H7z" />
  </Icon>
);
export const RefreshIcon = (p: IconProps) => (
  <Icon {...p}>
    <path d="M20 11a8 8 0 0 0-14.3-4.9L4 8M4 4v4h4M4 13a8 8 0 0 0 14.3 4.9L20 16M20 20v-4h-4" />
  </Icon>
);
export const UndoIcon = (p: IconProps) => (
  <Icon {...p}>
    <path d="M9 14L4 9l5-5" />
    <path d="M4 9h10.5a5.5 5.5 0 0 1 0 11H11" />
  </Icon>
);
export const EyeIcon = (p: IconProps) => (
  <Icon {...p}>
    <path d="M2 12s3.6-7 10-7 10 7 10 7-3.6 7-10 7S2 12 2 12z" />
    <circle cx="12" cy="12" r="3" />
  </Icon>
);
export const SaveIcon = (p: IconProps) => (
  <Icon {...p}>
    <path d="M5 3h11l3 3v13a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2z" />
    <path d="M7 3v5h8V3M7 21v-7h10v7" />
  </Icon>
);
export const SendIcon = (p: IconProps) => (
  <Icon {...p}>
    <path d="M21 3L10 14M21 3l-7 18-4-7-7-4z" />
  </Icon>
);
export const CopyIcon = (p: IconProps) => (
  <Icon {...p}>
    <rect x="9" y="9" width="12" height="12" rx="2" />
    <path d="M5 15H4a1 1 0 0 1-1-1V4a1 1 0 0 1 1-1h10a1 1 0 0 1 1 1v1" />
  </Icon>
);
export const BanIcon = (p: IconProps) => (
  <Icon {...p}>
    <circle cx="12" cy="12" r="9" />
    <path d="M5.6 5.6l12.8 12.8" />
  </Icon>
);
export const ExternalIcon = (p: IconProps) => (
  <Icon {...p}>
    <path d="M14 4h6v6M20 4l-9 9M18 14v5a1 1 0 0 1-1 1H5a1 1 0 0 1-1-1V7a1 1 0 0 1 1-1h5" />
  </Icon>
);
export const ArrowDownIcon = (p: IconProps) => (
  <Icon {...p}>
    <path d="M12 5v14M6 13l6 6 6-6" />
  </Icon>
);
export const SwapIcon = (p: IconProps) => (
  <Icon {...p}>
    <path d="M7 4L3 8l4 4M3 8h14M17 20l4-4-4-4M21 16H7" />
  </Icon>
);
export const ArrowEndIcon = (p: IconProps) => (
  <Icon {...p} className="shrink-0 rtl:-scale-x-100">
    <path d="M5 12h14M13 6l6 6-6 6" />
  </Icon>
);
export const ArrowBackIcon = (p: IconProps) => (
  <Icon {...p} className="shrink-0 rtl:-scale-x-100">
    <path d="M19 12H5M11 6l-6 6 6 6" />
  </Icon>
);
export const HeartIcon = ({ filled = false, ...p }: IconProps & { filled?: boolean }) => (
  <Icon {...p}>
    <path d="M12 20s-7-4.4-7-10a4 4 0 0 1 7-2.6A4 4 0 0 1 19 10c0 5.6-7 10-7 10z" fill={filled ? "currentColor" : "none"} />
  </Icon>
);
export const ThumbUpIcon = ({ filled = false, ...p }: IconProps & { filled?: boolean }) => (
  <Icon {...p}>
    <path d="M7 11v9H4v-9z" fill={filled ? "currentColor" : "none"} />
    <path d="M7 11l4-7a2 2 0 0 1 2 2.2L12.5 9H19a2 2 0 0 1 2 2.4l-1.3 6.2a2 2 0 0 1-2 1.6H7" fill={filled ? "currentColor" : "none"} />
  </Icon>
);
export const LockIcon = (p: IconProps) => (
  <Icon {...p}>
    <rect x="5" y="11" width="14" height="9" rx="2" />
    <path d="M8 11V8a4 4 0 0 1 8 0v3" />
  </Icon>
);
export const LinkIcon = (p: IconProps) => (
  <Icon {...p}>
    <path d="M10 14a4 4 0 0 0 5.7 0l3-3a4 4 0 0 0-5.7-5.7l-1 1" />
    <path d="M14 10a4 4 0 0 0-5.7 0l-3 3a4 4 0 0 0 5.7 5.7l1-1" />
  </Icon>
);
export const BankIcon = (p: IconProps) => (
  <Icon {...p}>
    <path d="M3 9.5L12 4l9 5.5M5 10v8M9.5 10v8M14.5 10v8M19 10v8M3 20h18" />
  </Icon>
);
export const StarIcon = (p: IconProps) => (
  <Icon {...p}>
    <path d="M12 3.5l2.6 5.3 5.9.9-4.25 4.1 1 5.8L12 16.9l-5.25 2.7 1-5.8L3.5 9.7l5.9-.9z" />
  </Icon>
);
export const InfoIcon = (p: IconProps) => (
  <Icon {...p}>
    <circle cx="12" cy="12" r="9" />
    <path d="M12 11v5" />
    <path d="M12 8h.01" />
  </Icon>
);

export const AlertIcon = (p: IconProps) => (
  <Icon {...p}>
    <path d="M12 3l9 16H3z" />
    <path d="M12 9v4" />
    <path d="M12 16h.01" />
  </Icon>
);

export const ExpandIcon = (p: IconProps) => (
  <Icon {...p}>
    <path d="M4 9V4h5M15 4h5v5M20 15v5h-5M9 20H4v-5" />
  </Icon>
);

export const ZoomInIcon = (p: IconProps) => (
  <Icon {...p}>
    <circle cx="11" cy="11" r="7" />
    <path d="M20 20l-4-4M11 8v6M8 11h6" />
  </Icon>
);

export const ZoomOutIcon = (p: IconProps) => (
  <Icon {...p}>
    <circle cx="11" cy="11" r="7" />
    <path d="M20 20l-4-4M8 11h6" />
  </Icon>
);

export const BellIcon = (p: IconProps) => (
  <Icon {...p}>
    <path d="M6 16V11a6 6 0 0 1 12 0v5l1.5 2h-15z" />
    <path d="M10 21h4" />
  </Icon>
);
export const CashIcon = (p: IconProps) => (
  <Icon {...p}>
    <rect x="2" y="6" width="20" height="12" rx="2" />
    <circle cx="12" cy="12" r="2.5" />
    <path d="M6 12h.01M18 12h.01" />
  </Icon>
);
export const KeyboardIcon = (p: IconProps) => (
  <Icon {...p}>
    <rect x="2" y="6" width="20" height="12" rx="2" />
    <path d="M6 10h.01M10 10h.01M14 10h.01M18 10h.01M7 14h10" />
  </Icon>
);
