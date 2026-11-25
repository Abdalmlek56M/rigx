import type { ComponentType } from "react";
import {
  BoardIcon, BoltIcon, CaseIcon, CpuIcon, FanIcon, GaugeIcon, GpuIcon, LayersIcon, MonitorIcon, PlugIcon, RamIcon, RulerIcon,
  SnowflakeIcon, SparkleIcon, StorageIcon, TagIcon, WifiIcon
} from "./icons";
import { specIconKey, type SpecIconKey } from "./spec-icon-rules";

const ICONS: Record<SpecIconKey, ComponentType<{ size?: number }>> = {
  wifi: WifiIcon, sparkle: SparkleIcon, fan: FanIcon, cooling: SnowflakeIcon, board: BoardIcon, case: CaseIcon, layers: LayersIcon,
  gauge: GaugeIcon, ruler: RulerIcon, power: BoltIcon, plug: PlugIcon, memory: RamIcon, storage: StorageIcon, monitor: MonitorIcon,
  gpu: GpuIcon, cpu: CpuIcon, tag: TagIcon
};

/** The icon of a spec (product specs grid, card facts), in the store's orange. */
export function SpecIcon({ specKey, size = 16 }: { specKey: string; size?: number }) {
  const Match = ICONS[specIconKey(specKey)];
  return <span aria-hidden="true" className="inline-flex text-orange-text"><Match size={size} /></span>;
}
