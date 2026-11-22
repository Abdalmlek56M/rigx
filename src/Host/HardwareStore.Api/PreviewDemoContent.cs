/// <summary>Short formal-Arabic demo text for the Development policies and FAQs (roadmap 4.4g).</summary>
internal static class PreviewDemoContent
{
    public static PreviewDemoPolicy[] Policies() =>
    [
        new("Shipping", "سياسة الشحن", "Shipping policy",
            "نشحن إلى جميع محافظات مصر. تظهر رسوم الشحن في صفحة الطلب بحسب المحافظة، والاستلام من المحل في العجمي مجاني بموعد مسبق. "
            + "نجهّز الطلب خلال يوم عمل، ويصل غالبًا خلال يومين إلى خمسة أيام عمل بحسب المحافظة. "
            + "إذا تجاوزت قيمة الطلب 10,000 جنيه فيُطلب عربون بنسبة 10% من قيمة القطع، بخلاف الشحن.",
            "We ship to every governorate in Egypt. The fee is shown at checkout by governorate, and pickup from the store in "
            + "Alexandria is free by appointment. Orders are prepared within one business day and usually arrive in two to five "
            + "business days; orders above 10,000 EGP need a 10% deposit on the parts in addition to shipping."),
        new("Returns", "سياسة الاسترجاع", "Returns policy",
            "يمكنك استرجاع القطعة خلال 14 يومًا من الاستلام، بشرط أن تكون بحالتها نفسها وبكامل مشتملاتها. "
            + "نفحص المرتجع عند وصوله، وبعد القبول نردّ المبلغ بطريقة الدفع نفسها.",
            "You may return an item within 14 days of delivery, unused and complete. We inspect the returned item and, once "
            + "accepted, refund it to the same payment method."),
        new("Exchanges", "سياسة الاستبدال", "Exchanges policy",
            "الاستبدال متاح خلال 14 يومًا من الاستلام إذا كانت القطعة بحالتها نفسها. إن كانت القطعة الجديدة أغلى فتدفع الفرق، وإن كانت أرخص فنردّ إليك الفرق.",
            "Exchanges are available within 14 days of delivery for an item in the same condition. You pay the difference when "
            + "the new item costs more, and we refund it when it costs less."),
        new("Warranty", "سياسة الضمان", "Warranty policy",
            "يبدأ الضمان من تاريخ الفاتورة، ويشترط سلامة استيكر RIGX على القطعة. "
            + "لمتابعة ضمانك اكتب رقم الموبايل أو رقم الفاتورة أو كود القطعة في صفحة الضمان.",
            "Warranty starts on the invoice date and needs an intact RIGX sticker on the item. To check your warranty, enter "
            + "your phone number, the invoice number or the item code on the warranty page."),
        new("Terms", "الشروط والأحكام", "Terms and conditions",
            "باستخدامك الموقع وإتمامك أي طلب، فإنك توافق على سياسات الشحن والاسترجاع والاستبدال والضمان. "
            + "الأسعار بالجنيه المصري، وتُؤكَّد قبل قبول الطلب. تُستخدم بياناتك لتجهيز الطلب والتواصل معك فقط.",
            "By using the site and placing an order you accept the shipping, returns, exchanges and warranty policies. Prices "
            + "are in Egyptian pounds and are confirmed before the order is accepted. Your data is used only to fulfil the "
            + "order and contact you.")
    ];

    public static PreviewDemoFaq[] Faqs() =>
    [
        new("هل يصل الشحن إلى جميع المحافظات؟", "Do you ship to every governorate?",
            "نعم، نشحن إلى جميع محافظات مصر. تظهر رسوم الشحن ومدة التوصيل في صفحة الطلب بحسب المحافظة.",
            "Yes, we ship to every governorate in Egypt. The fee and delivery time are shown at checkout by governorate.",
            "الشحن"),
        new("كيف أستلم من المحل؟", "How does store pickup work?",
            "اختر الاستلام من المحل عند الطلب، وسنتواصل معك لتحديد موعد. المحل في العجمي، الإسكندرية.",
            "Choose pickup at checkout and we will call you to agree on a time. The store is in El Agamy, Alexandria.",
            "الاستلام"),
        new("ما المقصود بالقطعة المستعملة؟", "What does a used item mean?",
            "تُفحص كل قطعة مستعملة عندنا قبل البيع. إن كان لديك سؤال عن حالة قطعة معينة فراسلنا على واتساب.",
            "Every used item is checked by us. If you have a question about a specific item, message us on WhatsApp.",
            "القطع"),
        new("كيف يعمل الضمان؟", "How does the warranty work?",
            "يبدأ الضمان من تاريخ الفاتورة، ويشترط سلامة الاستيكر. ابحث برقم الموبايل أو رقم الفاتورة أو كود القطعة في صفحة الضمان.",
            "Warranty starts on the invoice and needs an intact sticker. Search by phone, invoice number or item code on the "
            + "warranty page.",
            "الضمان"),
        new("كيف أدفع؟", "How can I pay?",
            "الدفع نقدًا عند الاستلام، أو عبر InstaPay، أو بمحفظة إلكترونية. الاستلام من المحل لا يتطلب دفعًا مقدمًا.",
            "Cash on delivery, InstaPay or a mobile wallet. Store pickup needs nothing upfront.",
            "الدفع"),
        new("هل أستطيع الاستبدال أو الاسترجاع؟", "Can I exchange or return an item?",
            "نعم، خلال 14 يومًا من الاستلام، بشرط أن تكون القطعة بحالتها نفسها وبكامل مشتملاتها.",
            "Yes, within 14 days of delivery, as long as the item is unused and complete.",
            "الاسترجاع")
    ];
}

internal sealed record PreviewDemoPolicy(string Kind, string TitleAr, string TitleEn, string BodyAr, string BodyEn);

internal sealed record PreviewDemoFaq(string QuestionAr, string QuestionEn, string AnswerAr, string AnswerEn, string Topic);
