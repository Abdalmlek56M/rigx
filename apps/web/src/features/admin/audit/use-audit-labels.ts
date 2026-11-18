import { useMessages } from "next-intl";
import { labelOf } from "./audit-rules";

/** Readable names for the log's module, table, action and actor kind; anything the messages do not know shows as stored. */
export function useAuditLabels(): Record<"module" | "entity" | "action" | "actor", (value: string) => string> {
  const { labels } = useMessages().admin.audit;
  return {
    module: (value) => labelOf(labels.modules, value),
    entity: (value) => labelOf(labels.entities, value),
    action: (value) => labelOf(labels.actions, value),
    actor: (value) => labelOf(labels.actors, value)
  };
}
