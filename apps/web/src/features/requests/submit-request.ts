import { ApiError, unwrap, type Schemas } from "@rigx/api-client";
import { browserApi } from "@/lib/api/browser";
import { required } from "@/lib/api/required";

export type RequestType = "Sell" | "TradeIn" | "Upgrade" | "NotifyMe" | "Support" | "Contact" | "Service" | "Complaint" | "Suggestion";
export type RequestFields = Partial<Omit<Schemas["CustomerRequestSubmission"], "type" | "website">>;

export type SubmitOutcome =
  | { status: "sent"; code: string | null; photoUploadToken: string | null }
  /** 409: nothing to wait for (a notify-me for a model that is available now). */
  | { status: "conflict" }
  /** 400/422: the server rejected a field. */
  | { status: "invalid" }
  | { status: "failed" };

const CONFLICT = 409;
const REJECTED = [400, 422];

function submission(type: RequestType, fields: RequestFields): Schemas["CustomerRequestSubmission"] {
  return {
    type, name: null, phone: null, email: null, governorate: null, source: null, subject: null, message: null, partType: null,
    modelName: null, ownedFor: null, itemCode: null, serviceKind: null, preferredDate: null, ...fields, website: null
  };
}

/** Sends one customer request (notify-me, sell, service...) and says what happened; it never throws. */
export async function submitRequest(type: RequestType, fields: RequestFields): Promise<SubmitOutcome> {
  try {
    const receipt = await required(unwrap(browserApi().POST("/api/v1/requests", { body: submission(type, fields) })));
    return { status: "sent", code: receipt.code, photoUploadToken: receipt.photoUploadToken ?? null };
  } catch (error) {
    if (error instanceof ApiError && error.status === CONFLICT) return { status: "conflict" };
    if (error instanceof ApiError && REJECTED.includes(error.status)) return { status: "invalid" };
    return { status: "failed" };
  }
}
