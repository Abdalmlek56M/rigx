import { multipart, unwrap } from "@rigx/api-client";
import { browserApi } from "@/lib/api/browser";

/** Attaches photos to a request with the token from its receipt, one by one; says how many were refused or failed. */
export async function uploadRequestPhotos(token: string, photos: readonly File[]): Promise<{ failed: number }> {
  let failed = 0;
  for (const photo of photos) {
    const form = new FormData();
    form.set("token", token);
    form.set("image", photo);
    try {
      await unwrap(browserApi().POST("/api/v1/requests/photos", multipart(form)));
    } catch {
      failed += 1;
    }
  }
  return { failed };
}
