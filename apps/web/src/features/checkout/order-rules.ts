/** The part of the upfront amount that is a deposit: only what exceeds the shipping fee. Zero when the upfront is shipping alone. */
export function depositOf(upfrontAmount: number, shippingFee: number): number {
  return upfrontAmount > shippingFee ? upfrontAmount - shippingFee : 0;
}
