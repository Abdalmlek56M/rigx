/// <summary>Short Egyptian-Arabic demo text for the Development policies and FAQs (roadmap 4.4g).</summary>
internal static class PreviewDemoContent
{
    public static PreviewDemoPolicy[] Policies() =>
    [
        new("Shipping", "سياسة الشحن", "Shipping policy",
            "بنشحن لكل محافظات مصر. رسوم الشحن بتظهر في صفحة الطلب حسب المحافظة، والاستلام من المحل في العجمي مجاني بموعد مسبق. "
            + "بنجهز الطلب خلال يوم عمل، والتوصيل غالبًا من يومين لخمسة أيام عمل حسب المحافظة. "
            + "لو الطلب فوق 10,000 جنيه بيتطلب عربون 10% من قيمة القطع بخلاف الشحن.",
            "We ship to every governorate in Egypt. The fee is shown at checkout by governorate, and pickup from the store in "
            + "Alexandria is free by appointment. Orders are prepared within one business day and usually arrive in two to five "
            + "business days; orders above 10,000 EGP need a 10% deposit on the parts in addition to shipping."),
        new("Returns", "سياسة الاسترجاع", "Returns policy",
            "تقدر ترجع القطعة خلال 14 يوم من الاستلام بشرط إنها في نفس حالتها وبكامل مشتملاتها. "
            + "بعد وصول المرتجع بنفحصه، وبعد القبول بنرد المبلغ على نفس طريقة الدفع.",
            "You may return an item within 14 days of delivery, unused and complete. We inspect the returned item and, once "
            + "accepted, refund it to the same payment method."),
        new("Exchanges", "سياسة الاستبدال", "Exchanges policy",
            "الاستبدال متاح خلال 14 يوم من الاستلام لو القطعة في نفس حالتها. لو القطعة الجديدة أغلى بتدفع الفرق، ولو أرخص بنرد الفرق.",
            "Exchanges are available within 14 days of delivery for an item in the same condition. You pay the difference when "
            + "the new item costs more, and we refund it when it costs less."),
        new("Warranty", "سياسة الضمان", "Warranty policy",
            "الضمان بيبدأ من تاريخ الفاتورة ومحتاج استيكر RIGX سليم على القطعة. "
            + "عشان تتابع ضمانك اكتب رقم الموبايل أو رقم الفاتورة أو كود القطعة في صفحة الضمان.",
            "Warranty starts on the invoice date and needs an intact RIGX sticker on the item. To check your warranty, enter "
            + "your phone number, the invoice number or the item code on the warranty page."),
        new("Terms", "الشروط والأحكام", "Terms and conditions",
            "باستخدامك الموقع وبإتمام أي طلب، إنت موافق على سياسات الشحن والاسترجاع والاستبدال والضمان. "
            + "الأسعار بالجنيه المصري وبتتأكد قبل تأكيد الطلب. بياناتك بتستخدم لتجهيز الطلب والتواصل معاك بس.",
            "By using the site and placing an order you accept the shipping, returns, exchanges and warranty policies. Prices "
            + "are in Egyptian pounds and are confirmed before the order is accepted. Your data is used only to fulfil the "
            + "order and contact you.")
    ];

    public static PreviewDemoFaq[] Faqs() =>
    [
        new("الشحن بيوصل لكل المحافظات؟", "Do you ship to every governorate?",
            "أيوه، بنشحن لكل محافظات مصر. رسوم الشحن ووقت التوصيل بيظهروا في صفحة الطلب حسب المحافظة.",
            "Yes, we ship to every governorate in Egypt. The fee and delivery time are shown at checkout by governorate.",
            "الشحن"),
        new("أستلم من المحل إزاي؟", "How does store pickup work?",
            "اختار الاستلام من المحل وقت الطلب، وهنكلمك نحدد ميعاد. المحل في العجمي، الإسكندرية.",
            "Choose pickup at checkout and we will call you to agree on a time. The store is in El Agamy, Alexandria.",
            "الاستلام"),
        new("القطعة المستعملة بتيجي إزاي؟", "What does a used item mean?",
            "كل قطعة مستعملة بتتفحص عندنا. لو عندك سؤال عن حالة قطعة معينة كلمنا على واتساب.",
            "Every used item is checked by us. If you have a question about a specific item, message us on WhatsApp.",
            "القطع"),
        new("الضمان بيشتغل إزاي؟", "How does the warranty work?",
            "الضمان بيبدأ من الفاتورة ومحتاج الاستيكر سليم. ابحث برقم الموبايل أو رقم الفاتورة أو كود القطعة في صفحة الضمان.",
            "Warranty starts on the invoice and needs an intact sticker. Search by phone, invoice number or item code on the "
            + "warranty page.",
            "الضمان"),
        new("أقدر أدفع إزاي؟", "How can I pay?",
            "كاش عند الاستلام، أو InstaPay، أو محفظة. الاستلام من المحل مفيش عليه دفع مقدم.",
            "Cash on delivery, InstaPay or a mobile wallet. Store pickup needs nothing upfront.",
            "الدفع"),
        new("أقدر أستبدل أو أرجّع؟", "Can I exchange or return an item?",
            "أيوه خلال 14 يوم من الاستلام بشرط إن القطعة في نفس حالتها وبكامل مشتملاتها.",
            "Yes, within 14 days of delivery, as long as the item is unused and complete.",
            "الاسترجاع")
    ];
}

internal sealed record PreviewDemoPolicy(string Kind, string TitleAr, string TitleEn, string BodyAr, string BodyEn);

internal sealed record PreviewDemoFaq(string QuestionAr, string QuestionEn, string AnswerAr, string AnswerEn, string Topic);
