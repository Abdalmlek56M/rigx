# Admin Security Audit Viewer

## الغرض
عرض للقراءة فقط لسجل `security_audit_events` دون كشف أي Secret أو بيانات جلسة.

## API
`GET /api/v1/admin/audit/events` بصلاحية `System.ViewAuditLogs`.

Filters: `fromUtc`, `toUtc`, `eventType` (substring ILIKE)، `outcome`, `actorUserAccountId`, `subjectUserAccountId`, `correlationId`, `search`, `page`, `pageSize` (max 100).
Response: `{ items, page, pageSize, total }`.

## Redaction
- الـDTO لا يحتوي `SessionKeyHash` أو `ProtectedTicket` أو IP أصلًا.
- Metadata تُعرض بمفاتيح Whitelist فقط: kind/emailKind/attempts/deadLetter/roles/reason/addressId/... إلخ.
- أي مفتاح يحتوي token/secret/password/hash/ip/email/url/cookie/ticket يُحجب.
- القيم Arrays تُدمج كنص، وتُقطع إلى 256 حرفًا.

## القيود
- لا Export.
- لا تعديل/حذف لأي حدث.
- لا Recursive Audit عند القراءة.
- Actor/Subject يظهران كـGUID فقط (بدون بريد/اسم).
