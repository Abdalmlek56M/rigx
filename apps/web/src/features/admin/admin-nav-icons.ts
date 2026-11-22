import {
  BankIcon, BoxIcon, CaseIcon, ClockIcon, DatabaseIcon, DocumentIcon, FolderIcon, GearIcon, GridIcon, HandshakeIcon, HelpIcon,
  InboxIcon, KeyIcon, RegisterIcon, ShieldIcon, StorageIcon, TagIcon, TicketIcon, TruckIcon, UsersIcon, WalletIcon
} from "@/components/ui/icons";
import type { AdminAreaKey } from "./admin-nav";

/** The sidebar icon of each admin area; the command palette shows the same one beside a page. */
export const AREA_ICONS: Record<AdminAreaKey, typeof GridIcon> = {
  dashboard: GridIcon, pos: RegisterIcon, orders: BoxIcon, customers: UsersIcon, offers: TagIcon, coupons: TicketIcon,
  inventory: StorageIcon, builds: CaseIcon, purchases: TruckIcon, stockImport: DocumentIcon, categories: FolderIcon, brands: ShieldIcon,
  partners: HandshakeIcon, expenses: WalletIcon, accounts: BankIcon,
  requests: InboxIcon,
  articles: DocumentIcon, faq: HelpIcon, policies: DocumentIcon, customerBuilds: CaseIcon,
  users: KeyIcon, audit: ClockIcon, system: DatabaseIcon, settings: GearIcon
};
