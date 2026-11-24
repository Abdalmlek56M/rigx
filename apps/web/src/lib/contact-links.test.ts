import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { contactHref, contactLinkProps, displayPhone, distinctPhone, findLink, NEW_TAB } from "./contact.ts";
import type { ContactLink } from "./storefront.ts";

const link = (platform: string, value: string): ContactLink => ({ platform, value, showOnSite: true, showInFloatingButton: false });

describe("contact link addresses", () => {
  it("builds a wa.me link, a tel link and keeps a social URL as it is", () => {
    assert.equal(contactHref(link("WhatsApp", "201289186840")), "https://wa.me/201289186840");
    assert.equal(contactHref(link("Phone", "201289186840")), "tel:+201289186840");
    assert.equal(contactHref(link("Facebook", "https://facebook.com/rigx")), "https://facebook.com/rigx");
  });

  it("opens every link in a new tab except a phone call", () => {
    assert.deepEqual(contactLinkProps(link("Phone", "201289186840")), { href: "tel:+201289186840" });
    assert.deepEqual(contactLinkProps(link("Instagram", "https://instagram.com/rigx")), { href: "https://instagram.com/rigx", ...NEW_TAB });
    assert.equal(NEW_TAB.rel.includes("noopener"), true);
  });

  it("finds a link by platform, or nothing", () => {
    const links = [link("WhatsApp", "1"), link("Phone", "2")];
    assert.equal(findLink(links, "Phone")?.value, "2");
    assert.equal(findLink(links, "TikTok"), undefined);
    assert.equal(findLink([], "Phone"), undefined);
  });
});

describe("phone display", () => {
  it("reads an international Egyptian number locally with spaces", () => {
    assert.equal(displayPhone("201289186840"), "0128 918 6840");
  });

  it("keeps a local number and groups it the same way", () => {
    assert.equal(displayPhone("01289186840"), "0128 918 6840");
  });

  it("leaves a number of another length without spaces", () => {
    assert.equal(displayPhone("0233333333"), "0233333333");
    assert.equal(displayPhone("447911123456"), "447911123456");
  });
});

describe("the phone link next to WhatsApp", () => {
  it("is dropped when it is the same number as WhatsApp, whatever the separators", () => {
    const links = [link("WhatsApp", "201289186840"), link("Phone", "+20 128 918 6840")];
    assert.equal(distinctPhone(links), undefined);
  });

  it("stays when the numbers differ or WhatsApp is missing", () => {
    assert.equal(distinctPhone([link("WhatsApp", "201289186840"), link("Phone", "201000000000")])?.value, "201000000000");
    assert.equal(distinctPhone([link("Phone", "201000000000")])?.value, "201000000000");
  });

  it("is missing when there is no phone link", () => {
    assert.equal(distinctPhone([link("WhatsApp", "201289186840")]), undefined);
  });
});
