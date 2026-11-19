"use client";

import { useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { formatEgp } from "@/lib/format";
import { TextField } from "@/components/ui/TextField";
import { isConflict } from "@/lib/api/conflict";
import { FormMessage } from "../kit/PageHeader";
import { useSubmit } from "../kit/use-submit";
import { canOffer } from "./request-labels";
import { saveOffer, type RequestDetail } from "./requests-api";
import { SaveIcon } from "@/components/ui/icons";
import { Button } from "../kit/Button";

/** The price offer (and the internal expected resale) for a sell / trade-in / upgrade request. */
export function RequestOfferForm({ request, canManage, onSaved, onStale }: { request: RequestDetail; canManage: boolean; onSaved: () => void; onStale: () => void }) {
  const t = useTranslations("admin.requests.offer");
  const locale = useLocale();
  const submit = useSubmit();
  const [amount, setAmount] = useState(request.offerAmount?.toString() ?? "");
  const [expected, setExpected] = useState(request.expectedResale?.toString() ?? "");
  const amountValue = Number(amount);
  const valid = amount.trim() !== "" && Number.isFinite(amountValue) && amountValue > 0;

  async function save() {
    if (!valid) return;
    await submit.run(async () => {
      try {
        const resale = expected.trim() === "" ? null : Number(expected);
        await saveOffer(request.id, amountValue, Number.isFinite(resale) ? resale : null, request.rowVersion);
        onSaved();
      } catch (error) {
        if (isConflict(error)) onStale();
        throw error;
      }
    }, t("saved"));
  }

  return (
    <section className="flex flex-col gap-3 border-t border-line pt-4">
      <h3 className="m-0 text-[15px] font-bold">{t("title")}</h3>
      {request.offerAmount !== null ? (
        <p className="m-0 text-sm">
          {t("current", {
            offer: formatEgp(request.offerAmount, locale),
            resale: request.expectedResale !== null ? formatEgp(request.expectedResale, locale) : t("none")
          })}
        </p>
      ) : null}
      {canOffer(request.type, request.status) && canManage ? (
        <form
          className="flex flex-col gap-3"
          onSubmit={(event) => {
            event.preventDefault();
            void save();
          }}
        >
          <div className="grid gap-3 sm:grid-cols-2">
            <TextField
              label={t("amount")} name="offerAmount" type="number" ltr value={amount} onChange={setAmount}
              error={submit.errors.fields.offerAmount}
            />
            <TextField
              label={t("expected")} name="expectedResale" type="number" ltr value={expected} onChange={setExpected}
              error={submit.errors.fields.expectedResale}
            />
          </div>
          <p className="m-0 text-xs text-ink-muted">{t("expectedHint")}</p>
          <FormMessage message={submit.errors.general} />
          <Button type="submit" variant="primary" icon={SaveIcon} className="self-start" loading={submit.busy} disabled={!valid}>
            {t("save")}
          </Button>
          {submit.dialog}
        </form>
      ) : (
        <p className="m-0 text-sm text-ink-muted">{t("notAllowed")}</p>
      )}
    </section>
  );
}
