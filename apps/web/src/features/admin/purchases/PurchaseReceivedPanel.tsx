"use client";

import { useTranslations } from "next-intl";
import { Link } from "@/i18n/navigation";
import type { PurchaseReceived } from "./purchases-api";
import { ArrowEndIcon } from "@/components/ui/icons";
import { buttonClass, buttonIconSize } from "../kit/Button";

/** After receiving: the RX codes that now exist in stock (each opens in the inventory) and the kept build, if there is one. */
export function PurchaseReceivedPanel({ result }: { result: PurchaseReceived }) {
  const t = useTranslations("admin.purchases.received");
  const items = result.items.items;
  const assemblyId = result.purchase.assemblyId;

  return (
    <section role="status" className="flex flex-col gap-3 rounded-card border border-orange bg-surface p-5">
      <h2 className="m-0 text-[17px] font-bold">{t("title", { count: items.length })}</h2>
      <ul className="m-0 flex list-none flex-wrap gap-2 p-0">
        {items.map((item) => (
          <li key={item.id}>
            <Link href={`/admin/inventory/${item.id}`} className="inline-flex min-h-11 items-center gap-2 rounded-control border border-input-line bg-paper px-3 text-sm font-bold hover:bg-line">
              <span dir="ltr" className="font-mono">{item.code}</span>
              <span dir="auto" className="max-w-48 truncate font-normal [unicode-bidi:isolate]">{item.modelName}</span>
            </Link>
          </li>
        ))}
      </ul>
      <p className="m-0 text-sm text-ink-muted">{t("photosHint")}</p>
      {assemblyId ? <Link href={`/admin/builds/${assemblyId}`} className={`${buttonClass({ variant: "primary" })} self-start`}><ArrowEndIcon size={buttonIconSize()} />{t("openBuild")}</Link> : null}
      {assemblyId ? <p className="m-0 text-xs text-ink-muted">{t("addPartsHint")}</p> : null}
    </section>
  );
}
