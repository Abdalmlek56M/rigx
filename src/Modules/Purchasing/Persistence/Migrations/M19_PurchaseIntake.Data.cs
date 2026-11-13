namespace HardwareStore.Modules.Purchasing.Persistence.Migrations
{
    /// <summary>
    /// Data step of <c>M19_PurchaseIntake</c>: old suppliers and external sellers become sellers, old drafts become
    /// purchases (same ids, because Retail receipts point at them). Never fails on odd data: a phone that is not a valid
    /// Egyptian mobile (or is already taken) gets a unique placeholder starting with 000.
    /// </summary>
    internal static class PurchaseIntakeData
    {
        public const string CreateSequence = "CREATE SEQUENCE purchase_code_seq;";

        public const string DropSequence = "DROP SEQUENCE purchase_code_seq;";

        public const string ConvertOldRows = """
            CREATE TEMP TABLE _seller_candidates ON COMMIT DROP AS
            SELECT s.id AS seller_id, 'supplier:' || s.id::text AS match_key, 'Company' AS kind,
                   left(s.company_name, 160) AS name,
                   coalesce(nullif(s.contact_person_phone, ''), p.phone_number, '') AS raw_phone,
                   left(concat_ws(' / ', s.contact_person_name, s.notes), 1000) AS notes,
                   'supplier' AS source, s.created_at_utc AS created_at
            FROM supplier_profiles s
            LEFT JOIN persons p ON p.id = s.person_id
            UNION ALL
            SELECT gen_random_uuid(), 'external:' || k.seller_name, 'Person', left(k.seller_name, 160),
                   coalesce(k.contact, ''), NULL, left(k.marketplace, 64), k.created_at
            FROM (
                SELECT coalesce(nullif(trim(external_seller_name), ''), nullif(trim(marketplace_name), ''), 'Unknown seller') AS seller_name,
                       min(external_seller_contact) AS contact, min(marketplace_name) AS marketplace, min(created_at_utc) AS created_at
                FROM purchase_drafts
                WHERE supplier_id IS NULL
                GROUP BY 1
            ) k;

            CREATE TEMP TABLE _seller_phones ON COMMIT DROP AS
            SELECT c.seller_id,
                   CASE
                       WHEN d.digits ~ '^01[0-9]{9}$' THEN d.digits
                       WHEN d.digits ~ '^1[0-9]{9}$' THEN '0' || d.digits
                       ELSE NULL
                   END AS normalized
            FROM _seller_candidates c
            CROSS JOIN LATERAL (
                SELECT CASE
                           WHEN raw.digits LIKE '0020%' THEN substr(raw.digits, 5)
                           WHEN raw.digits LIKE '20%' AND length(raw.digits) = 12 THEN substr(raw.digits, 3)
                           ELSE raw.digits
                       END AS digits
                FROM (SELECT regexp_replace(c.raw_phone, '[^0-9]', '', 'g') AS digits) raw
            ) d;

            INSERT INTO sellers (id, kind, name, phone, national_id, governorate, area, source, notes, created_at_utc, updated_at_utc, row_version)
            SELECT c.seller_id, c.kind, c.name,
                   CASE WHEN ph.normalized IS NOT NULL AND ph.rank_in_phone = 1 THEN ph.normalized
                        ELSE '000' || lpad(c.position::text, 8, '0') END,
                   NULL, NULL, NULL, c.source, c.notes, c.created_at, c.created_at, 1
            FROM (SELECT *, row_number() OVER (ORDER BY created_at, seller_id) AS position FROM _seller_candidates) c
            JOIN (SELECT seller_id, normalized,
                         row_number() OVER (PARTITION BY normalized ORDER BY seller_id) AS rank_in_phone
                  FROM _seller_phones) ph ON ph.seller_id = c.seller_id;

            INSERT INTO purchases (id, code, seller_id, kind, keep_as_build, build_name, status, purchase_date, notes, total,
                                   payment_method, paid_amount, trade_in_value, trade_in_note, remaining, received_at_utc,
                                   assembly_id, created_by_user_account_id, created_at_utc, updated_at_utc, row_version)
            SELECT d.id, 'PO-' || lpad(d.position::text, 4, '0'), s.seller_id, 'Parts', false, NULL, d.status,
                   coalesce(d.purchase_date, d.created_at_utc::date), d.notes, d.total_amount,
                   'Cash', 0, 0, NULL, d.total_amount, NULL, NULL, d.created_by_user_account_id,
                   d.created_at_utc, d.updated_at_utc, 1
            FROM (SELECT *, row_number() OVER (ORDER BY created_at_utc, id) AS position FROM purchase_drafts) d
            JOIN _seller_candidates s ON s.match_key = CASE
                WHEN d.supplier_id IS NOT NULL THEN 'supplier:' || d.supplier_id::text
                ELSE 'external:' || coalesce(nullif(trim(d.external_seller_name), ''), nullif(trim(d.marketplace_name), ''), 'Unknown seller')
            END;

            SELECT setval('purchase_code_seq', count(*)) FROM purchases HAVING count(*) > 0;

            INSERT INTO purchase_lines (id, purchase_id, sort_order, category_id, product_id, model_name, condition, quantity,
                                        unit_cost, line_total, price, is_hidden, has_box)
            SELECT i.id, i.draft_id, i.sort_order, NULL, nullif(i.product_id, '00000000-0000-0000-0000-000000000000'::uuid),
                   left(regexp_replace(trim(coalesce(nullif(trim(i.model), ''), i.item_name)), '\s+', ' ', 'g'), 160),
                   CASE WHEN i.item_condition = 'New' THEN 'New' ELSE 'Used' END,
                   least(i.quantity, 1000), i.unit_price, i.line_total, 0, false, false
            FROM purchase_draft_items i;
            """;
    }
}
