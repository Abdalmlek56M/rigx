# Account Center

## Routes
- `/account` — Overview (بوابة الحساب).
- `/account/profile` — Personal Information + Avatar.
- `/account/security` — Password/Logout.
- `/account/sessions` — Sessions/Devices.
- `/account/addresses` — Saved Addresses.
- `/account/wishlist` — Wishlist.
- `/account/reviews` — My Reviews + Store Review.

## Overview
يعرض Avatar/Name/Username/Email/Verification/Phone وملخص:
- Sessions count.
- Addresses count.
- Wishlist count.
- Reviews count.
- Orders: عنصر معطّل صراحة حتى وجود Sales/Orders حقيقية.
- Marketing: معطّل حتى سياسة Consent معتمدة.

## Avatar
- `POST /api/v1/identity/profile/avatar` multipart.
- `DELETE /api/v1/identity/profile/avatar`.
- يخزن `user_accounts.avatar_storage_key` ويستخدم Catalog Media Storage تحت `avatars/{accountId}` (Original + WebP + Thumbnail).
- JPG/PNG/WebP فقط عبر فحص فك الصورة، حدود الحجم/الأبعاد من `CatalogMediaOptions`.
- Fallback بالحروف.
- يظهر في المراجعات فقط عند `ShowAvatar=true`.

## Navigation
`AccountNav` موحّد مع `aria-current="page"`، وBack Link إلى `/account`.

## Sensitive changes
- تغيير كلمة المرور يتطلب Current Password.
- لا تغيير نهائي للبريد/الهاتف دون تحقق؛ الحقول الحساسة تبقى Read-only حتى اعتماد قناة تحقق.
