export const minimumPasswordScalars = 15;
export const maximumPasswordScalars = 128;

export function countUnicodeScalars(value: string): number {
  let count = 0;
  for (let index = 0; index < value.length; index += 1) {
    const code = value.charCodeAt(index);
    if (code >= 0xd800 && code <= 0xdbff && index + 1 < value.length) {
      const next = value.charCodeAt(index + 1);
      if (next >= 0xdc00 && next <= 0xdfff) index += 1;
    }
    count += 1;
  }
  return count;
}

export function passwordPolicyError(value: string, messages: {
  minimumLength: string;
  maximumLength: string;
}): string | null {
  const count = countUnicodeScalars(value);
  if (count < minimumPasswordScalars) return messages.minimumLength;
  if (count > maximumPasswordScalars) return messages.maximumLength;
  return null;
}
