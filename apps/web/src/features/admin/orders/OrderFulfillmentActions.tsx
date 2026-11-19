"use client";

import { useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { SelectField } from "@/components/ui/SelectField";
import { TextField } from "@/components/ui/TextField";
import { formatDateTime } from "@/lib/format";
import { usePermissions } from "../use-permissions";
import { FormMessage } from "../kit/PageHeader";
import { setReservation, setShipping } from "./orders-api";
import { hasShipping, holdsReservation, reservationHoursValid } from "./order-rules";
import { useOrderAction, type OrderActionProps } from "./use-order-action";
import { SaveIcon } from "@/components/ui/icons";
import { Button } from "../kit/Button";

function ReservationForm(props: OrderActionProps) {
  const { order } = props;
  const t = useTranslations("admin.orders.fulfillment.reservation");
  const locale = useLocale();
  const { submit, run } = useOrderAction(props);
  const [hours, setHours] = useState("");
  const [mode, setMode] = useState<"notify" | "release">(order.reservationAutoRelease ? "release" : "notify");

  return (
    <form
      className="flex flex-col gap-2"
      onSubmit={(event) => {
        event.preventDefault();
        void run(() => setReservation(order.id, hours.trim() === "" ? null : Number(hours), mode === "release", order.rowVersion), t("done"));
      }}
    >
      <h3 className="m-0 text-sm font-bold">{t("title")}</h3>
      {order.reservedUntilUtc ? <p className="m-0 text-sm">{t("until", { date: formatDateTime(order.reservedUntilUtc, locale) })}</p> : null}
      <TextField label={t("hours")} name="hours" type="number" ltr value={hours} onChange={setHours} error={submit.errors.fields.hours} />
      <p className="m-0 text-xs text-ink-muted">{t("hoursHint")}</p>
      <SelectField
        label={t("whenEnds")} name="autoRelease" value={mode} onChange={setMode} error={submit.errors.fields.autoRelease}
        options={[{ value: "notify", label: t("notify") }, { value: "release", label: t("release") }]}
      />
      <FormMessage message={submit.errors.general} />
      <Button type="submit" icon={SaveIcon} className="self-start" loading={submit.busy} disabled={hours.trim() !== "" && !reservationHoursValid(hours)}>{t("save")}</Button>
      {submit.dialog}
    </form>
  );
}

function ShippingForm(props: OrderActionProps) {
  const { order } = props;
  const t = useTranslations("admin.orders.fulfillment.shipping");
  const { submit, run } = useOrderAction(props);
  const [courier, setCourier] = useState(order.courierName ?? "");
  const [waybill, setWaybill] = useState(order.waybillNumber ?? "");

  return (
    <form
      className="flex flex-col gap-2"
      onSubmit={(event) => {
        event.preventDefault();
        void run(() => setShipping(order.id, courier.trim() || null, waybill.trim() || null, order.rowVersion), t("done"));
      }}
    >
      <h3 className="m-0 text-sm font-bold">{t("title")}</h3>
      <TextField label={t("courier")} name="courierName" value={courier} onChange={(value) => setCourier(value.slice(0, 120))} error={submit.errors.fields.courierName} />
      <TextField label={t("waybill")} name="waybillNumber" ltr value={waybill} onChange={(value) => setWaybill(value.slice(0, 80))} error={submit.errors.fields.waybillNumber} />
      <FormMessage message={submit.errors.general} />
      <Button type="submit" icon={SaveIcon} className="self-start" loading={submit.busy}>{t("save")}</Button>
      {submit.dialog}
    </form>
  );
}

/** The reservation hold (while the order waits to leave) and the courier / waybill of a delivery (needs `Sales.ApproveOrder`). */
export function OrderFulfillmentActions(props: OrderActionProps) {
  const { has } = usePermissions();
  const { order } = props;
  if (!has("Sales.ApproveOrder")) return null;
  const reservation = holdsReservation(order);
  const shipping = hasShipping(order);
  if (!reservation && !shipping) return null;
  return (
    <section className="flex flex-col gap-4 border-t border-line pt-4">
      {reservation ? <ReservationForm {...props} /> : null}
      {shipping ? <ShippingForm {...props} /> : null}
    </section>
  );
}
