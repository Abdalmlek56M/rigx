# DeepSeek brief 07 — Formal Arabic over the whole site (after brief 06 is merged)

Read `AGENTS.md` first. Branch `deepseek/07-formal-arabic` from the latest `main` (after brief 06). **Text only**: change wording in `apps/web/messages/ar.json` (outside `admin.*` first, then `admin.*`), and the visible Arabic text in the e-mail templates under `src/` ONLY if you find them in the web app; do not edit .NET files (list them in the report for Opus). No layout, class or logic change.

## Rule
The owner: "all site text formal; I cannot write 'لحد كذا'." Replace every colloquial Egyptian word or form with clear formal Arabic (فصحى مبسّطة) that a shop customer understands at once. Keep it short and friendly in tone, never slangy; keep brand words (RIGX, InstaPay, RX codes, GPU names).

Examples (not a full list): عايز → ترغب / تريد · محتار → هل تحتاج إلى مساعدة في الاختيار؟ · بنجمّع وبننضّف وبنشتري القديم → نجمّع الأجهزة وننظّفها ونشتري القطع المستعملة · هتستخدم الجهاز في إيه؟ → فيمَ ستستخدم الجهاز؟ · مفيش نتايج → لا توجد نتائج مطابقة · لسه → بعدُ / حتى الآن · دلوقتي → الآن · كمان → أيضًا · لحد → حتى · ابعت لنا → أرسل إلينا · قولنا → أخبرنا · مش → لا / ليس · فيه → يوجد · اتسجّل → تم تسجيله · بقت → أصبحت.

## Scope and method
1. Search `ar.json` for colloquial markers (a starting list: عايز، محتار، هتـ، بنـ، مفيش، مش ، لسه، دلوقتي، كمان، لحد، فين، ازاي، ايه/إيه، ده، دي، دا، اللي، علشان، عشان، قولنا، ابعت، خد، هات، تمنها، بتـ). Review every hit in context; many are fine in formal Arabic (e.g. "اللي" is not, but "التي/الذي" is).
2. Cover every namespace the customer can see: home, header, footer, catalog, product, builds, builder, cart, checkout, account, tracking, warranty, invoice, requests, services, policies titles, faq UI, errors, auth, notifications, 404.
3. FAQ and policy bodies come from the database, not `ar.json`: list in the report which seeded texts (if any) in `src/` look colloquial, without editing them.
4. English strings stay as they are unless they have an obvious typo.
5. Do not rename keys. Keep every placeholder (`{amount}`, `{count}`) and ICU plural form intact; the parity test of `ar.json` and `en.json` must pass.

## Done when
- `npm run typecheck`, `npm run lint`, `npm test`, `npm run build` pass.
- The report lists: the number of keys changed per top-level namespace, ten before → after examples, remaining colloquial hits you left on purpose (with the reason), and the .NET / database texts Opus should review.
