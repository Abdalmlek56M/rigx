"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import {
  completeOrder, createManualOrder, createOrderReturn, getAdminOrders, getAdminProducts, getOrderReturns,
  getRetailAssemblies, inspectOrderReturn, rejectOrderReturn, updateOrderPayment, updateOrderStatus, waiveOrderPayment,
  type CatalogProductSummary, type RetailAssembly, type ReturnRecord, type StoreOrder, type StoreOrderItem,
} from "@/lib/api-client";
import { apiErrorMessage } from "@/lib/form-errors";
import { useLocale } from "@/lib/i18n";
import { LoadingCard, Message, PageIntro, PrimaryButton, SecondaryButton } from "@/components/ui";

const nextStatuses: Record<string, string[]> = {
  New: ["Confirmed", "Cancelled"], Confirmed: ["Preparing", "Cancelled"], Preparing: ["Ready", "Cancelled"],
  Ready: ["Delivered", "Cancelled"], Delivered: ["Cancelled"], Completed: [], Cancelled: [],
};

export function RetailOrdersAdmin() {
  const { messages } = useLocale();
  const [orders, setOrders] = useState<StoreOrder[]>([]);
  const [returns, setReturns] = useState<ReturnRecord[]>([]);
  const [products, setProducts] = useState<CatalogProductSummary[]>([]);
  const [assemblies, setAssemblies] = useState<RetailAssembly[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const [manualOpen, setManualOpen] = useState(false);
  const [manual, setManual] = useState({
    customerName: "", phoneNumber: "", addressText: "", leadSource: "Manual",
    fulfillment: "Delivery", paymentMethod: "CashOnDelivery", kind: "Build", targetId: "", quantity: "1",
  });
  const [waivingId, setWaivingId] = useState<string | null>(null);
  const [waiverReason, setWaiverReason] = useState("");
  const [returnDraft, setReturnDraft] = useState<{ orderId: string; item: StoreOrderItem; unitId: string; refund: string; reason: string } | null>(null);
  const [inspectDraft, setInspectDraft] = useState<{ id: string; condition: string; notes: string } | null>(null);

  async function load() {
    setLoading(true);
    try {
      const [orderList, returnList, productList, assemblyList] = await Promise.all([
        getAdminOrders(), getOrderReturns(), getAdminProducts(), getRetailAssemblies(),
      ]);
      setOrders(orderList); setReturns(returnList);
      setProducts(productList.filter((product) => product.status === "Published"));
      setAssemblies(assemblyList.filter((assembly) => assembly.status === "Published"));
    } catch (requestError) { setError(apiErrorMessage(requestError, messages)); }
    finally { setLoading(false); }
  }
  useEffect(() => { void load(); /* eslint-disable-next-line */ }, []);

  function replace(order: StoreOrder) {
    setOrders((current) => current.map((item) => item.id === order.id ? order : item));
  }

  async function change(order: StoreOrder, status: string) {
    setError(null);
    try { replace(await updateOrderStatus(order.id, status, order.rowVersion)); }
    catch (requestError) { setError(apiErrorMessage(requestError, messages)); }
  }
  async function pay(order: StoreOrder) {
    setError(null);
    try { replace(await updateOrderPayment(order.id, "Paid", order.rowVersion)); }
    catch (requestError) { setError(apiErrorMessage(requestError, messages)); }
  }
  async function waive(order: StoreOrder) {
    if (waiverReason.trim().length < 5) { setError(messages.retailWaiverReasonRequired); return; }
    setError(null);
    try {
      replace(await waiveOrderPayment(order.id, waiverReason.trim(), order.rowVersion));
      setWaivingId(null); setWaiverReason(""); setNotice(messages.retailSaved);
    } catch (requestError) { setError(apiErrorMessage(requestError, messages)); }
  }
  async function complete(order: StoreOrder) {
    setError(null);
    try { replace(await completeOrder(order.id, order.rowVersion)); setNotice(messages.retailOrderCompleted); }
    catch (requestError) { setError(apiErrorMessage(requestError, messages)); }
  }
  async function submitReturn() {
    if (!returnDraft) return;
    if (!returnDraft.reason.trim()) { setError(messages.retailReturnReasonRequired); return; }
    const refund = Number(returnDraft.refund);
    if (!Number.isFinite(refund) || refund < 0) { setError(messages.retailInvalidPrice); return; }
    setError(null);
    try {
      await createOrderReturn({ orderId: returnDraft.orderId, orderItemId: returnDraft.item.id, unitId: returnDraft.unitId, refundAmount: refund, reason: returnDraft.reason.trim() });
      setReturnDraft(null); setNotice(messages.retailReturnOpened); await load();
    } catch (requestError) { setError(apiErrorMessage(requestError, messages)); }
  }
  async function inspect() {
    if (!inspectDraft) return;
    setError(null);
    try {
      await inspectOrderReturn(inspectDraft.id, inspectDraft.condition, inspectDraft.notes.trim() || null, returns.find((item) => item.id === inspectDraft.id)?.rowVersion ?? 0);
      setInspectDraft(null); setNotice(messages.retailReturnInspected); await load();
    } catch (requestError) { setError(apiErrorMessage(requestError, messages)); }
  }
  async function reject(entity: ReturnRecord) {
    setError(null);
    try { await rejectOrderReturn(entity.id, "Rejected after inspection", entity.rowVersion); setNotice(messages.retailSaved); await load(); }
    catch (requestError) { setError(apiErrorMessage(requestError, messages)); }
  }

  function paymentReady(order: StoreOrder) {
    return order.paymentRequired ? order.paymentStatus === "Paid" : order.paymentStatus === "Waived";
  }
  async function submitManual() {
    const customerName = manual.customerName.trim();
    const phoneNumber = manual.phoneNumber.trim();
    if (!customerName || !phoneNumber || !manual.targetId) { setError(messages.retailManualRequired); return; }
    const quantity = Math.max(1, Math.min(20, Number(manual.quantity) || 1));
    const assembly = manual.kind === "Build" ? assemblies.find((item) => item.id === manual.targetId) : undefined;
    const product = manual.kind === "Product" ? products.find((item) => item.id === manual.targetId) : undefined;
    const targetName = assembly?.name ?? product?.name;
    if (!targetName) { setError(messages.retailManualRequired); return; }
    setError(null);
    try {
      await createManualOrder({
        customerName, phoneNumber,
        addressText: manual.fulfillment === "Delivery" ? manual.addressText.trim() || null : null,
        fulfillmentMethod: manual.fulfillment,
        paymentMethod: manual.fulfillment === "Delivery" ? manual.paymentMethod : "ToBeArranged",
        notes: null,
        leadSource: manual.leadSource,
        paymentRequired: manual.fulfillment === "Delivery",
        items: [assembly
          ? { assemblyId: assembly.id, name: assembly.name, quantity: 1, unitPrice: assembly.salePrice }
          : { productId: product!.id, name: product!.name, quantity, unitPrice: product!.basePrice }],
      });
      setNotice(messages.retailManualCreated);
      setManual({ ...manual, targetId: "", customerName: "", phoneNumber: "", addressText: "" });
      setManualOpen(false);
      await load();
    } catch (requestError) { setError(apiErrorMessage(requestError, messages)); }
  }
  function alreadyReturned(orderId: string, unitId: string) {
    return returns.some((item) => item.orderId === orderId && item.unitId === unitId && item.status !== "Rejected");
  }

  return (
    <main className="p-4 sm:p-6 lg:p-8"><div className="mx-auto grid max-w-6xl gap-5">
      <PageIntro eyebrow="RIGX / SALES" title={messages.retailOrdersTitle} lead={messages.retailOrdersLead} />
      {notice ? <Message tone="success">{notice}</Message> : null}
      {error ? <Message tone="error">{error}</Message> : null}
      <section className="rigx-admin-card p-4">
        <div className="flex flex-wrap items-center justify-between gap-2">
          <h2 className="font-bold">{messages.retailManualTitle}</h2>
          <SecondaryButton type="button" onClick={() => setManualOpen((current) => !current)}>
            {manualOpen ? messages.retailCancel : messages.retailManualAdd}
          </SecondaryButton>
        </div>
        {manualOpen ? (
          <div className="mt-3 grid gap-2 sm:grid-cols-2">
            <input value={manual.customerName} onChange={(event) => setManual({ ...manual, customerName: event.target.value })} placeholder={messages.retailCustomerName} className="min-h-11 rounded-xl border border-[var(--ds-border)] bg-[var(--ds-surface)] px-3 text-sm" />
            <input value={manual.phoneNumber} onChange={(event) => setManual({ ...manual, phoneNumber: event.target.value })} placeholder={messages.retailCustomerPhone} inputMode="tel" dir="ltr" className="min-h-11 rounded-xl border border-[var(--ds-border)] bg-[var(--ds-surface)] px-3 text-sm" />
            <input value={manual.addressText} onChange={(event) => setManual({ ...manual, addressText: event.target.value })} placeholder={messages.retailCustomerAddress} className="min-h-11 rounded-xl border border-[var(--ds-border)] bg-[var(--ds-surface)] px-3 text-sm sm:col-span-2" />
            <select value={manual.fulfillment} onChange={(event) => setManual({ ...manual, fulfillment: event.target.value })} className="min-h-11 rounded-xl border border-[var(--ds-border)] bg-[var(--ds-surface)] px-3 text-sm">
              <option value="Delivery">{messages.checkoutDelivery}</option>
              <option value="Pickup">{messages.checkoutPickup}</option>
            </select>
            {manual.fulfillment === "Delivery" ? (
              <select value={manual.paymentMethod} onChange={(event) => setManual({ ...manual, paymentMethod: event.target.value })} className="min-h-11 rounded-xl border border-[var(--ds-border)] bg-[var(--ds-surface)] px-3 text-sm">
                <option value="CashOnDelivery">{messages.checkoutCod}</option>
                <option value="BankTransfer">{messages.checkoutBank}</option>
                <option value="MobileWalletTransfer">{messages.checkoutWallet}</option>
              </select>
            ) : <span className="text-sm text-[var(--ds-text-muted)]">{messages.checkoutPickupNoPayment}</span>}
            <select value={manual.kind} onChange={(event) => setManual({ ...manual, kind: event.target.value, targetId: "" })} className="min-h-11 rounded-xl border border-[var(--ds-border)] bg-[var(--ds-surface)] px-3 text-sm">
              <option value="Build">{messages.retailManualBuild}</option>
              <option value="Product">{messages.retailManualProduct}</option>
            </select>
            <select value={manual.targetId} onChange={(event) => setManual({ ...manual, targetId: event.target.value })} className="min-h-11 rounded-xl border border-[var(--ds-border)] bg-[var(--ds-surface)] px-3 text-sm">
              <option value="">{messages.retailManualChoose}</option>
              {manual.kind === "Build"
                ? assemblies.map((item) => <option key={item.id} value={item.id}>{item.name} · {item.salePrice.toLocaleString()} EGP</option>)
                : products.map((item) => <option key={item.id} value={item.id}>{item.name} · {item.basePrice.toLocaleString()} EGP</option>)}
            </select>
            <select value={manual.leadSource} onChange={(event) => setManual({ ...manual, leadSource: event.target.value })} className="min-h-11 rounded-xl border border-[var(--ds-border)] bg-[var(--ds-surface)] px-3 text-sm">
              {["Manual", "Phone", "Social"].map((source) => <option key={source} value={source}>{source}</option>)}
            </select>
            {manual.kind === "Product" ? (
              <input value={manual.quantity} onChange={(event) => setManual({ ...manual, quantity: event.target.value })} inputMode="numeric" dir="ltr" aria-label={messages.retailQuantity} className="min-h-11 rounded-xl border border-[var(--ds-border)] bg-[var(--ds-surface)] px-3 text-sm" />
            ) : null}
            <div className="sm:col-span-2">
              <PrimaryButton type="button" onClick={() => void submitManual()}>{messages.retailManualCreate}</PrimaryButton>
            </div>
          </div>
        ) : null}
      </section>
      {loading ? <LoadingCard /> : (
        <>
          {orders.length === 0 ? <Message tone="info">{messages.retailNoOrders}</Message> : (
            <section className="grid gap-3">
              {orders.map((order) => (
                <article key={order.id} className="rigx-admin-card grid gap-2 p-4">
                  <div className="flex flex-wrap items-center justify-between gap-2">
                    <Link href={`/admin/orders/${order.id}`} className="font-bold" dir="ltr">{order.reference}</Link>
                    <span className="text-xs font-bold">{order.status} · {order.paymentStatus}{order.paymentRequired ? "" : ` · ${messages.retailPaymentNotRequired}`}</span>
                  </div>
                  <p className="text-sm text-[var(--ds-text-muted)]">{order.customerName} · <span dir="ltr">{order.phoneNumber}</span></p>
                  <ul className="grid gap-1 text-sm">
                    {order.items.map((item) => (
                      <li key={item.id} className="grid gap-1">
                        <div className="flex justify-between gap-2">
                          <span>{item.name} × {item.quantity}</span>
                          <span dir="ltr">{item.lineTotal.toLocaleString()}</span>
                        </div>
                        {(item.discountAmount > 0 || item.directExpenseAmount > 0) ? (
                          <span className="text-xs text-[var(--ds-text-muted)]" dir="ltr">
                            {messages.retailDiscount} {item.discountAmount.toLocaleString()} · {messages.retailDirectExpense} {item.directExpenseAmount.toLocaleString()}
                          </span>
                        ) : null}
                        {order.status === "Completed" || order.status === "Delivered" ? (
                          <div className="grid gap-1">
                            {item.components.map((component) => {
                              const unit = component.unitId;
                              return (
                                <div key={component.id} className="flex flex-wrap items-center justify-between gap-2 text-xs">
                                  <span dir="ltr">{component.unitId.slice(0, 8)} · {component.allocationPercent}% · {messages.retailNetProfit} {component.netProfit.toLocaleString()}</span>
                                  {!alreadyReturned(order.id, unit) && order.status !== "Cancelled" ? (
                                    <SecondaryButton type="button" onClick={() => setReturnDraft({
                                      orderId: order.id, item, unitId: unit,
                                      refund: String(Math.max(0, component.revenueAmount - component.discountAmount)),
                                      reason: "",
                                    })}>{messages.retailOpenReturn}</SecondaryButton>
                                  ) : null}
                                </div>
                              );
                            })}
                          </div>
                        ) : null}
                      </li>
                    ))}
                  </ul>
                  <p className="font-bold" dir="ltr">{order.total.toLocaleString()} EGP</p>
                  {order.refundedAmount > 0 ? <p className="text-xs text-[var(--ds-text-muted)]">{messages.retailRefunded} <span dir="ltr">{order.refundedAmount.toLocaleString()}</span></p> : null}

                  {returnDraft && returnDraft.orderId === order.id ? (
                    <div className="grid gap-2 rounded-xl border border-[var(--ds-border)] p-3 sm:grid-cols-2">
                      <label className="grid gap-1 text-xs font-bold">
                        <span>{messages.retailRefundAmount}</span>
                        <input value={returnDraft.refund} onChange={(event) => setReturnDraft({ ...returnDraft, refund: event.target.value })} inputMode="decimal" dir="ltr" className="min-h-10 rounded-lg border border-[var(--ds-border)] bg-[var(--ds-surface)] px-2 text-sm" />
                      </label>
                      <label className="grid gap-1 text-xs font-bold">
                        <span>{messages.retailReturnReason}</span>
                        <input value={returnDraft.reason} onChange={(event) => setReturnDraft({ ...returnDraft, reason: event.target.value })} className="min-h-10 rounded-lg border border-[var(--ds-border)] bg-[var(--ds-surface)] px-2 text-sm" />
                      </label>
                      <div className="flex gap-2 sm:col-span-2">
                        <SecondaryButton type="button" onClick={() => void submitReturn()}>{messages.retailOpenReturn}</SecondaryButton>
                        <SecondaryButton type="button" onClick={() => setReturnDraft(null)}>{messages.retailCancel}</SecondaryButton>
                      </div>
                    </div>
                  ) : null}

                  <div className="flex flex-wrap items-center gap-2">
                    {(nextStatuses[order.status] ?? []).map((status) => (
                      <SecondaryButton key={status} type="button" onClick={() => void change(order, status)}>{status}</SecondaryButton>
                    ))}
                    {order.status === "Delivered" && order.paymentRequired && order.paymentStatus !== "Paid" ? (
                      <SecondaryButton type="button" onClick={() => void pay(order)}>{messages.retailMarkPaid}</SecondaryButton>
                    ) : null}
                    {order.status === "Delivered" && order.paymentStatus !== "Paid" && order.paymentStatus !== "Waived" ? (
                      <SecondaryButton type="button" onClick={() => { setWaivingId(order.id); setWaiverReason(""); }}>{messages.retailWaivePayment}</SecondaryButton>
                    ) : null}
                    {order.status === "Delivered" && paymentReady(order) ? (
                      <SecondaryButton type="button" onClick={() => void complete(order)}>{messages.retailCompleteOrder}</SecondaryButton>
                    ) : null}
                    {order.paymentStatus === "Paid" || order.paymentStatus === "Waived" || order.paymentStatus === "Refunded" ? null : order.status === "New" || order.status === "Confirmed" || order.status === "Preparing" || order.status === "Ready" ? (
                      <SecondaryButton type="button" onClick={() => void pay(order)}>{messages.retailMarkPaid}</SecondaryButton>
                    ) : null}
                  </div>
                  {waivingId === order.id ? (
                    <div className="grid gap-2 rounded-xl border border-[var(--ds-border)] p-3 sm:grid-cols-[1fr_auto]">
                      <input value={waiverReason} onChange={(event) => setWaiverReason(event.target.value)} placeholder={messages.retailWaiverReason} className="min-h-10 rounded-lg border border-[var(--ds-border)] bg-[var(--ds-surface)] px-2 text-sm" />
                      <div className="flex gap-2">
                        <SecondaryButton type="button" onClick={() => void waive(order)}>{messages.retailWaiveConfirm}</SecondaryButton>
                        <SecondaryButton type="button" onClick={() => setWaivingId(null)}>{messages.retailCancel}</SecondaryButton>
                      </div>
                    </div>
                  ) : null}
                </article>
              ))}
            </section>
          )}

          <section className="rigx-admin-card p-4">
            <h2 className="font-bold">{messages.retailReturnsTitle}</h2>
            {returns.length === 0 ? <Message tone="info">{messages.retailNoReturns}</Message> : (
              <div className="mt-3 grid gap-3">
                {returns.map((entity) => (
                  <article key={entity.id} className="grid gap-2 rounded-xl border border-[var(--ds-border)] p-3 text-sm">
                    <div className="flex flex-wrap items-center justify-between gap-2">
                      <span dir="ltr">{entity.unitId.slice(0, 8)} · {entity.refundAmount.toLocaleString()} EGP</span>
                      <span className="text-xs font-bold">{entity.status}{entity.inspectionCondition ? ` · ${entity.inspectionCondition}` : ""}</span>
                    </div>
                    <p className="text-[var(--ds-text-muted)]">{entity.reason}</p>
                    {entity.status === "PendingInspection" ? (
                      inspectDraft?.id === entity.id ? (
                        <div className="grid gap-2 sm:grid-cols-[140px_1fr_auto]">
                          <select value={inspectDraft.condition} onChange={(event) => setInspectDraft({ ...inspectDraft, condition: event.target.value })} className="min-h-10 rounded-lg border border-[var(--ds-border)] bg-[var(--ds-surface)] px-2 text-sm">
                            {["New", "Used", "Open Box", "Scrap"].map((condition) => <option key={condition} value={condition}>{condition}</option>)}
                          </select>
                          <input value={inspectDraft.notes} onChange={(event) => setInspectDraft({ ...inspectDraft, notes: event.target.value })} placeholder={messages.retailInspectionNotes} className="min-h-10 rounded-lg border border-[var(--ds-border)] bg-[var(--ds-surface)] px-2 text-sm" />
                          <div className="flex gap-2">
                            <SecondaryButton type="button" onClick={() => void inspect()}>{messages.retailInspect}</SecondaryButton>
                            <SecondaryButton type="button" onClick={() => setInspectDraft(null)}>{messages.retailCancel}</SecondaryButton>
                          </div>
                        </div>
                      ) : (
                        <div className="flex flex-wrap gap-2">
                          <SecondaryButton type="button" onClick={() => setInspectDraft({ id: entity.id, condition: "New", notes: "" })}>{messages.retailInspect}</SecondaryButton>
                          <SecondaryButton type="button" onClick={() => void reject(entity)}>{messages.retailReject}</SecondaryButton>
                        </div>
                      )
                    ) : null}
                  </article>
                ))}
              </div>
            )}
          </section>
        </>
      )}
    </div></main>
  );
}
