/** The part of the upfront amount that is a deposit: only what exceeds the shipping fee. Zero when the upfront is shipping alone. */
export function depositOf(upfrontAmount: number, shippingFee: number): number {
  return upfrontAmount > shippingFee ? upfrontAmount - shippingFee : 0;
}

/** A payment method that is paid by sending money (InstaPay or a wallet): the store's transfer instructions belong with it. */
export function isTransfer(method: string | null | undefined): boolean {
  return method === "InstaPay" || method === "Wallet";
}
