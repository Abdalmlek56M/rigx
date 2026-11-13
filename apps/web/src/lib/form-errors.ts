import { ApiError } from "@/lib/api-client";

type CommonMessages = {
  requestFailed: string;
  rateLimited: string;
  serviceUnavailable: string;
};

export function apiErrorMessage(error: unknown, messages: CommonMessages): string {
  if (error instanceof ApiError) {
    if (error.status === 429) return messages.rateLimited;
    if (error.status >= 500) return messages.serviceUnavailable;
  }
  return messages.requestFailed;
}

export function fieldError(error: unknown, field: string): string | undefined {
  if (!(error instanceof ApiError)) return undefined;
  const values = error.payload?.errors?.[field];
  return values?.[0];
}
