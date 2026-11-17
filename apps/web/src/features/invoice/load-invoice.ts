import "server-only";
import { ApiError, unwrap, type Schemas } from "@rigx/api-client";
import { required } from "@/lib/api/required";
import { createServerApi } from "@/lib/api/server";

const NOT_FOUND = 404;

/** The invoice behind a link, read fresh on every request; `null` when the link is unknown. Other failures throw. */
export async function loadInvoice(token: string): Promise<Schemas["InvoiceDocument"] | null> {
  try {
    return await required(unwrap(createServerApi({ fresh: true }).GET("/api/v1/invoices/{token}", { params: { path: { token } } })));
  } catch (error) {
    if (error instanceof ApiError && error.status === NOT_FOUND) return null;
    throw error;
  }
}
