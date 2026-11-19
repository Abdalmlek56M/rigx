import { useMessages } from "next-intl";
import { labelOf } from "./audit-rules";

type Label = (value: string) => string;

export interface AuditLabels {
  module: Label;
  entity: Label;
  action: Label;
  actor: Label;
  field: Label;
  /** A stored code (status, condition, payment method…) in the words the rest of the admin uses for it. */
  value: (entity: string, field: string, value: string) => string;
}

/** Readable names for the log's module, table, action, actor kind, changed field and coded values; anything unknown shows as stored. */
export function useAuditLabels(): AuditLabels {
  const messages = useMessages();
  const { labels } = messages.admin.audit;
  const { orders, requests, purchases } = messages.admin;
  const methods = { ...purchases.payment.methods, ...orders.methods };
  const values: Readonly<Record<string, Readonly<Record<string, string>>>> = {
    Condition: messages.common.condition,
    InspectionCondition: messages.common.condition,
    PaymentStatus: orders.paymentStatuses,
    PaymentMethod: methods,
    Method: methods,
    FulfillmentMethod: orders.fulfillmentKinds,
    Channel: orders.channels
  };
  const statuses: Readonly<Record<string, Readonly<Record<string, string>>>> = { store_orders: orders.statuses, customer_requests: requests.statuses };
  return {
    module: (value) => labelOf(labels.modules, value),
    entity: (value) => labelOf(labels.entities, value),
    action: (value) => labelOf(labels.actions, value),
    actor: (value) => labelOf(labels.actors, value),
    field: (value) => labelOf(labels.fields, value),
    value: (entity, field, value) => {
      const [group, key] = field === "Status" ? [statuses, entity] : [values, field];
      const names = Object.hasOwn(group, key) ? group[key] : undefined;
      return names ? labelOf(names, value) : value;
    }
  };
}
