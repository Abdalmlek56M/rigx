"use client";

import { isConflict } from "@/lib/api/conflict";
import { useSubmit } from "../kit/use-submit";
import type { Order } from "./orders-api";

/** What every order action component receives: the order, how to take its new state, and what to do when it changed meanwhile. */
export interface OrderActionProps {
  order: Order;
  onUpdated: (order: Order) => void;
  onStale: () => void;
}

/** A save that returns the updated order: `apply` puts it on screen (a 409 reloads the order first and rethrows); `run` adds the form's busy/error handling. */
export function useOrderAction({ onUpdated, onStale }: Pick<OrderActionProps, "onUpdated" | "onStale">) {
  const submit = useSubmit();
  const apply = async (action: () => Promise<Order>) => {
    try {
      onUpdated(await action());
    } catch (error) {
      if (isConflict(error)) onStale();
      throw error;
    }
  };
  const run = (action: () => Promise<Order>, success: string) => submit.run(() => apply(action), success);
  return { submit, run, apply };
}
