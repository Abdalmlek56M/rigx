import type { SVGProps } from "react";

export type IconName =
  | "search"
  | "menu"
  | "close"
  | "moon"
  | "sun"
  | "eye"
  | "eyeOff"
  | "dashboard"
  | "box"
  | "tags"
  | "image"
  | "users"
  | "shoppingBag"
  | "warehouse"
  | "receipt"
  | "mail"
  | "settings"
  | "logout"
  | "store"
  | "chevron"
  | "share"
  | "whatsapp"
  | "compare"
  | "zoom"
  | "check"
  | "trash"
  | "plus"
  | "minus"
  | "cpu"
  | "gpu"
  | "motherboard"
  | "ram"
  | "hardDrive"
  | "fan"
  | "power"
  | "pcCase"
  | "peripherals"
  | "sparkles"
  | "tools"
  | "user"
  | "filter"
  | "facebook"
  | "telegram"
  | "tiktok"
  | "youtube"
  | "instagram"
  | "heart";

const paths: Record<IconName, React.ReactNode> = {
  search: <><circle cx="11" cy="11" r="6.5" /><path d="m16 16 4.2 4.2" /></>,
  menu: <path d="M4 7h16M4 12h16M4 17h16" />,
  close: <path d="m6 6 12 12M18 6 6 18" />,
  moon: <path d="M20 15.2A8 8 0 0 1 8.8 4 8 8 0 1 0 20 15.2Z" />,
  sun: <><circle cx="12" cy="12" r="3.5" /><path d="M12 2v2M12 20v2M4.9 4.9l1.4 1.4M17.7 17.7l1.4 1.4M2 12h2M20 12h2M4.9 19.1l1.4-1.4M17.7 6.3l1.4-1.4" /></>,
  eye: <><path d="M2.5 12s3.5-6 9.5-6 9.5 6 9.5 6-3.5 6-9.5 6-9.5-6-9.5-6Z" /><circle cx="12" cy="12" r="2.75" /></>,
  eyeOff: <><path d="m3 3 18 18" /><path d="M10.6 6.2A10.9 10.9 0 0 1 12 6c6 0 9.5 6 9.5 6a15.7 15.7 0 0 1-3.1 3.7" /><path d="M6.2 6.7C3.9 8.4 2.5 12 2.5 12s3.5 6 9.5 6c1.4 0 2.7-.3 3.8-.8" /><path d="M9.9 9.9a3 3 0 0 0 4.2 4.2" /></>,
  dashboard: <><rect x="3.5" y="3.5" width="7" height="7" rx="1.4" /><rect x="13.5" y="3.5" width="7" height="7" rx="1.4" /><rect x="3.5" y="13.5" width="7" height="7" rx="1.4" /><rect x="13.5" y="13.5" width="7" height="7" rx="1.4" /></>,
  box: <><path d="m4 7 8-4 8 4-8 4-8-4Z" /><path d="m4 7 8 4 8-4v10l-8 4-8-4V7Z" /><path d="M12 11v10" /></>,
  tags: <><path d="M4 12.5 12.5 4H20v7.5L11.5 20 4 12.5Z" /><circle cx="16.5" cy="7.5" r="1" /></>,
  image: <><rect x="3.5" y="4.5" width="17" height="15" rx="2" /><circle cx="9" cy="10" r="1.5" /><path d="m5 17 4.5-4 3 2.5 2.5-2 4 3.5" /></>,
  users: <><circle cx="9" cy="8" r="3" /><path d="M3.5 20a5.5 5.5 0 0 1 11 0M16 5.5a3 3 0 0 1 0 5.8M17 14.5a4.5 4.5 0 0 1 3.5 4.3" /></>,
  shoppingBag: <><path d="M5 8h14l-1 12H6L5 8Z" /><path d="M9 9V6a3 3 0 0 1 6 0v3" /></>,
  warehouse: <><path d="M3.5 20V8l8.5-4 8.5 4v12" /><path d="M7 20v-7h10v7M7 16h10" /></>,
  receipt: <><path d="M6 3.5h12v17l-3-2-3 2-3-2-3 2v-17Z" /><path d="M9 8h6M9 12h6" /></>,
  mail: <><rect x="3.5" y="5" width="17" height="14" rx="2" /><path d="m4 7 8 6 8-6" /></>,
  settings: <><circle cx="12" cy="12" r="3" /><path d="M19.4 15a1.7 1.7 0 0 0 .34 1.88l.06.06-2.86 2.86-.06-.06A1.7 1.7 0 0 0 15 19.4a1.7 1.7 0 0 0-1 .6 1.7 1.7 0 0 0-.4 1.1V21H9.5v-.1A1.7 1.7 0 0 0 8.4 19.4a1.7 1.7 0 0 0-1.88.34l-.06.06-2.86-2.86.06-.06A1.7 1.7 0 0 0 4 15a1.7 1.7 0 0 0-.6-1 1.7 1.7 0 0 0-1.1-.4H2V9.5h.3A1.7 1.7 0 0 0 4 8.4a1.7 1.7 0 0 0-.34-1.88l-.06-.06L6.46 3.6l.06.06A1.7 1.7 0 0 0 8.4 4a1.7 1.7 0 0 0 1-.6 1.7 1.7 0 0 0 .4-1.1V2h4v.3A1.7 1.7 0 0 0 15 4a1.7 1.7 0 0 0 1.88-.34l.06-.06 2.86 2.86-.06.06A1.7 1.7 0 0 0 19.4 8.4c.14.37.36.7.65 1 .28.27.63.41 1 .4h.3v4h-.3a1.7 1.7 0 0 0-1.65 1.2Z" /></>,
  logout: <><path d="M10 4H5.5v16H10M14 8l4 4-4 4M8 12h10" /></>,
  store: <><path d="M4 10h16M5 10V5h14v5M6 10v10h12V10M9 20v-5h6v5" /><path d="M3.5 10 5 5h14l1.5 5a3 3 0 0 1-5 1 3 3 0 0 1-7 0 3 3 0 0 1-5-1Z" /></>,
  chevron: <path d="m9 6 6 6-6 6" />,
  share: <><circle cx="18" cy="5" r="2.5" /><circle cx="6" cy="12" r="2.5" /><circle cx="18" cy="19" r="2.5" /><path d="m8.2 10.8 7.6-4.5M8.2 13.2l7.6 4.5" /></>,
  whatsapp: <><path d="M20 11.6a8 8 0 0 1-11.8 7L4 20l1.4-4A8 8 0 1 1 20 11.6Z" /><path d="M8.8 8.1c.5 2.7 2.4 4.6 5.1 5.1l1.2-1.2c.3-.3.7-.3 1-.1l1.5.8c.4.2.5.6.4 1-.4 1.4-1.7 2.2-3.2 1.9-3.8-.7-6.8-3.7-7.5-7.5-.3-1.5.5-2.8 1.9-3.2.4-.1.8 0 1 .4l.8 1.5c.2.3.2.7-.1 1l-1.1 1.2Z" /></>,
  compare: <><path d="M8 4v16M16 4v16" /><path d="m4 8 4-4 4 4M12 16l4 4 4-4" /></>,
  zoom: <><circle cx="10.5" cy="10.5" r="5.5" /><path d="m15 15 4.5 4.5M10.5 8v5M8 10.5h5" /></>,
  check: <path d="m5 12 4 4L19 6" />,
  trash: <><path d="M4 7h16M9 3h6l1 4H8l1-4ZM7 7l1 14h8l1-14M10 11v6M14 11v6" /></>,
  plus: <path d="M12 5v14M5 12h14" />,
  minus: <path d="M5 12h14" />,
  cpu: <><rect x="4" y="4" width="16" height="16" rx="2" /><rect x="9" y="9" width="6" height="6" /><path d="M9 1v3M15 1v3M9 20v3M15 20v3M20 9h3M20 15h3M1 9h3M1 15h3" /></>,
  gpu: <><rect x="3" y="6" width="18" height="12" rx="2" /><circle cx="8.5" cy="12" r="2.5" /><circle cx="15.5" cy="12" r="2.5" /><path d="M2 9v6M7 18v2M17 18v2" /></>,
  motherboard: <><rect x="4" y="4" width="16" height="16" rx="2" /><rect x="7" y="7" width="4" height="4" /><path d="M15 7v4M17 7v4M7 15h6M15 15h2v2" /></>,
  ram: <><path d="M3 8h18v8H3z" /><path d="M6 16v2M10 16v2M14 16v2M18 16v2M6 10h2v3H6zM11 10h2v3h-2zM16 10h2v3h-2z" /></>,
  hardDrive: <><rect x="4" y="5" width="16" height="14" rx="2" /><circle cx="8" cy="15" r="1" /><path d="M12 15h4M4 12h16" /></>,
  fan: <><circle cx="12" cy="12" r="8.5" /><path d="M12 12c-2-2-1-5 1-5s3 3 1 5m-2-2c2-2 5-1 5 1s-3 3-5 1m2 2c2 2 1 5-1 5s-3-3-1-5m2-2c-2 2-5 1-5-1s3-3 5-1" /><circle cx="12" cy="12" r="2" /></>,
  power: <path d="M18.36 6.64a9 9 0 1 1-12.73 0M12 2v10" />,
  pcCase: <><rect x="6" y="3" width="12" height="18" rx="2" /><circle cx="12" cy="7" r="1" /><path d="M10 16h4M10 18h4" /></>,
  peripherals: <><rect x="8" y="4" width="8" height="16" rx="4" /><path d="M12 4v5" /></>,
  sparkles: <path d="m12 3 2.5 5.5L20 11l-5.5 2.5L12 19l-2.5-5.5L4 11l5.5-2.5Z" />,
  tools: <path d="m14.7 6.3 3 3-9.4 9.4H5.3v-3L14.7 6.3ZM18 3l3 3-2 2-3-3 2-2Z" />,
  user: <><circle cx="12" cy="8" r="4" /><path d="M5 20a7 7 0 0 1 14 0" /></>,
  filter: <polygon points="22 3 2 3 10 12.46 10 19 14 21 14 12.46 22 3" />,
  facebook: <path d="M14 8h3V5h-3a4 4 0 0 0-4 4v3H7v3h3v6h3v-6h3l1-3h-4V9a1 1 0 0 1 1-1Z" />,
  telegram: <><path d="m21 4-4 16-5-5-4 4v-6l9-6-11 5-3-2 18-6Z" /></>,
  tiktok: <><path d="M14 4v11a4 4 0 1 1-3-3.9" /><path d="M14 4c1 3 3 5 6 5" /></>,
  youtube: <><rect x="3" y="6" width="18" height="12" rx="3" /><path d="m10 9 5 3-5 3V9Z" /></>,
  instagram: <><rect x="3.5" y="3.5" width="17" height="17" rx="4" /><circle cx="12" cy="12" r="4" /><circle cx="17" cy="7" r="1" /></>,
  heart: <path d="M12 20s-7.5-4.6-7.5-10A4.5 4.5 0 0 1 12 6.4 4.5 4.5 0 0 1 19.5 10c0 5.4-7.5 10-7.5 10Z" />,
};

export function Icon({
  name,
  className = "h-5 w-5",
  ...props
}: SVGProps<SVGSVGElement> & { name: IconName; className?: string }) {
  return (
    <svg
      viewBox="0 0 24 24"
      aria-hidden="true"
      className={className}
      fill="none"
      stroke="currentColor"
      strokeWidth="1.8"
      strokeLinecap="round"
      strokeLinejoin="round"
      {...props}
    >
      {paths[name]}
    </svg>
  );
}
