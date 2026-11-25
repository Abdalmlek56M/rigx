import type { ComponentType } from "react";
import { categoryIconKey, type CategoryIconKey } from "./category-icon-rules";
import {
  BoardIcon, BoxIcon, CableIcon, CaseIcon, CpuIcon, FanIcon, GamepadIcon, GpuIcon, HeadsetIcon, KeyboardIcon, LaptopIcon, MonitorIcon,
  MouseIcon, PowerIcon, RamIcon, StorageIcon, WifiIcon
} from "./icons";

const ICONS: Record<CategoryIconKey, ComponentType<{ size?: number }>> = {
  cpu: CpuIcon, gpu: GpuIcon, motherboard: BoardIcon, ram: RamIcon, storage: StorageIcon, power: PowerIcon, cooling: FanIcon,
  case: CaseIcon, monitor: MonitorIcon, keyboard: KeyboardIcon, mouse: MouseIcon, headset: HeadsetIcon, gamepad: GamepadIcon,
  laptop: LaptopIcon, network: WifiIcon, cable: CableIcon, accessories: BoxIcon
};

/** A category's icon: the one the owner picked in the admin (`icon`), else one guessed from its slug. */
export function CategoryIcon({ slug, icon, size }: { slug: string; icon?: string | null; size?: number }) {
  const Match = ICONS[categoryIconKey(slug, icon)];
  return <Match size={size} />;
}

/** One icon of the ready set by its key (the admin picker draws every key). */
export function CategoryIconByKey({ iconKey, size }: { iconKey: CategoryIconKey; size?: number }) {
  const Match = ICONS[iconKey];
  return <Match size={size} />;
}
