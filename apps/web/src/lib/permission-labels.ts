import type { Locale } from "@/lib/i18n";

export type PermissionCopy = {
  labelAr: string;
  labelEn: string;
  descriptionAr: string;
  descriptionEn: string;
  groupAr: string;
  groupEn: string;
};

export const permissionCopy: Record<string, PermissionCopy> = {
  "Catalog.View": {
    labelAr: "عرض الكتالوج",
    labelEn: "View catalog",
    descriptionAr: "استعراض المنتجات والتصنيفات والماركات وصور الكتالوج.",
    descriptionEn: "Browse products, categories, brands, and catalog media.",
    groupAr: "الكتالوج",
    groupEn: "Catalog",
  },
  "Catalog.ManageProducts": {
    labelAr: "إدارة المنتجات",
    labelEn: "Manage products",
    descriptionAr: "إنشاء وتعديل المنتجات والإصدارات والمواصفات والنشر.",
    descriptionEn: "Create and edit products, variants, specifications, and publishing.",
    groupAr: "الكتالوج",
    groupEn: "Catalog",
  },
  "Catalog.ManageTaxonomy": {
    labelAr: "إدارة التصنيفات والماركات",
    labelEn: "Manage taxonomy",
    descriptionAr: "إنشاء وتعديل التصنيفات والماركات وترتيبها وتفعيلها.",
    descriptionEn: "Create and edit categories, brands, ordering, and activation.",
    groupAr: "الكتالوج",
    groupEn: "Catalog",
  },
  "Catalog.ManageMedia": {
    labelAr: "إدارة وسائط الكتالوج",
    labelEn: "Manage catalog media",
    descriptionAr: "رفع صور المنتجات والتصنيفات والماركات وحذف الوسائط.",
    descriptionEn: "Upload and delete product, category, and brand media.",
    groupAr: "الكتالوج",
    groupEn: "Catalog",
  },
  "Sales.ViewOrders": {
    labelAr: "عرض الطلبات",
    labelEn: "View orders",
    descriptionAr: "استعراض الطلبات وبيانات العملاء اللازمة للمتابعة.",
    descriptionEn: "View orders and the customer data needed for follow-up.",
    groupAr: "المبيعات",
    groupEn: "Sales",
  },
  "Sales.LogCommunication": {
    labelAr: "تسجيل التواصل",
    labelEn: "Log communication",
    descriptionAr: "تسجيل ملاحظات التواصل مع العميل على الطلب.",
    descriptionEn: "Record customer communication notes on an order.",
    groupAr: "المبيعات",
    groupEn: "Sales",
  },
  "Sales.ApproveOrder": {
    labelAr: "اعتماد الطلبات",
    labelEn: "Approve orders",
    descriptionAr: "اعتماد الطلب بعد المراجعة والانتقال به لمرحلة التجهيز.",
    descriptionEn: "Approve an order after review and move it to fulfillment.",
    groupAr: "المبيعات",
    groupEn: "Sales",
  },
  "Sales.CreatePOS": {
    labelAr: "إنشاء بيع نقطة البيع",
    labelEn: "Create POS sale",
    descriptionAr: "إنشاء بيع مباشر من المعرض عبر نقطة البيع.",
    descriptionEn: "Create a direct in-store sale from the POS.",
    groupAr: "المبيعات",
    groupEn: "Sales",
  },
  "Sales.OverrideMinimumPrice": {
    labelAr: "تجاوز الحد الأدنى للسعر",
    labelEn: "Override minimum price",
    descriptionAr: "بيع بسعر أقل من الحد الأدنى المعتمد.",
    descriptionEn: "Sell below the approved minimum price.",
    groupAr: "المبيعات",
    groupEn: "Sales",
  },
  "Sales.FulfillStorePickup": {
    labelAr: "تنفيذ الاستلام من المقر",
    labelEn: "Fulfill pickup",
    descriptionAr: "تسليم طلب استلام من المقر وتوثيق المستلم.",
    descriptionEn: "Hand over a pickup order and record the recipient.",
    groupAr: "المبيعات",
    groupEn: "Sales",
  },
  "Sales.ProcessReturn": {
    labelAr: "معالجة المرتجعات",
    labelEn: "Process returns",
    descriptionAr: "فحص المرتجع واعتماده وربطه بالتسوية.",
    descriptionEn: "Inspect, approve, and settle a returned item.",
    groupAr: "المبيعات",
    groupEn: "Sales",
  },
  "Invoices.ManageTemplates": {
    labelAr: "إدارة قوالب الفواتير",
    labelEn: "Manage invoice templates",
    descriptionAr: "إنشاء وتعديل قوالب الفواتير وإصداراتها.",
    descriptionEn: "Create and edit invoice templates and versions.",
    groupAr: "الفواتير",
    groupEn: "Invoices",
  },
  "Invoices.ReviewDraft": {
    labelAr: "مراجعة مسودة الفاتورة",
    labelEn: "Review invoice draft",
    descriptionAr: "مراجعة مسودة الفاتورة قبل الإصدار النهائي.",
    descriptionEn: "Review an invoice draft before final issue.",
    groupAr: "الفواتير",
    groupEn: "Invoices",
  },
  "Invoices.IssueFinal": {
    labelAr: "إصدار الفاتورة النهائية",
    labelEn: "Issue final invoice",
    descriptionAr: "إصدار فاتورة نهائية وتثبيت رقمها ولقطتها.",
    descriptionEn: "Issue a final invoice with its immutable number and snapshot.",
    groupAr: "الفواتير",
    groupEn: "Invoices",
  },
  "Invoices.Reprint": {
    labelAr: "إعادة طباعة الفواتير",
    labelEn: "Reprint invoices",
    descriptionAr: "إعادة طباعة نسخة مطابقة من فاتورة صادرة.",
    descriptionEn: "Reprint a matching copy of an issued invoice.",
    groupAr: "الفواتير",
    groupEn: "Invoices",
  },
  "Inventory.View": {
    labelAr: "عرض المخزون",
    labelEn: "View inventory",
    descriptionAr: "استعراض الأرصدة والمستودعات وبيانات المخزون.",
    descriptionEn: "View balances, warehouses, and inventory data.",
    groupAr: "المخزون",
    groupEn: "Inventory",
  },
  "Inventory.Transfer": {
    labelAr: "تحويل المخزون",
    labelEn: "Transfer stock",
    descriptionAr: "إنشاء تحويل بين المستودعات ومتابعة استلامه.",
    descriptionEn: "Create warehouse transfers and track their receipt.",
    groupAr: "المخزون",
    groupEn: "Inventory",
  },
  "Inventory.StockCount": {
    labelAr: "الجرد الفعلي",
    labelEn: "Stock count",
    descriptionAr: "بدء جلسات الجرد وتسجيل الكميات الفعلية.",
    descriptionEn: "Start stock counts and record physical quantities.",
    groupAr: "المخزون",
    groupEn: "Inventory",
  },
  "Inventory.StockAdjust": {
    labelAr: "تسوية المخزون",
    labelEn: "Adjust stock",
    descriptionAr: "اعتماد فروق الجرد والعجز والزيادة.",
    descriptionEn: "Approve count differences and shrinkage or surplus.",
    groupAr: "المخزون",
    groupEn: "Inventory",
  },
  "Inventory.ManageSerials": {
    labelAr: "إدارة السيريالات",
    labelEn: "Manage serials",
    descriptionAr: "فحص وتعديل وتتبع الأرقام التسلسلية.",
    descriptionEn: "Inspect, edit, and trace serial numbers.",
    groupAr: "المخزون",
    groupEn: "Inventory",
  },
  "Inventory.ManageWarehouses": {
    labelAr: "إدارة المستودعات",
    labelEn: "Manage warehouses",
    descriptionAr: "إنشاء وتعديل دليل المستودعات دون أرصدة أو حركات.",
    descriptionEn: "Create and edit warehouse master data without balances or movements.",
    groupAr: "المخزون",
    groupEn: "Inventory",
  },
  "Purchasing.View": {
    labelAr: "عرض المشتريات",
    labelEn: "View purchasing",
    descriptionAr: "استعراض الموردين وأوامر وفواتير الشراء العامة.",
    descriptionEn: "View suppliers and general purchase orders and bills.",
    groupAr: "المشتريات",
    groupEn: "Purchasing",
  },
  "Purchasing.ManageSuppliers": {
    labelAr: "إدارة الموردين",
    labelEn: "Manage suppliers",
    descriptionAr: "إنشاء وتعديل الموردين وأصنافهم وربطهم بالشخص.",
    descriptionEn: "Create and edit suppliers, their products, and person links.",
    groupAr: "المشتريات",
    groupEn: "Purchasing",
  },
  "Purchasing.CreateOrder": {
    labelAr: "إنشاء أمر شراء",
    labelEn: "Create purchase order",
    descriptionAr: "إنشاء أمر شراء مسودة.",
    descriptionEn: "Create a draft purchase order.",
    groupAr: "المشتريات",
    groupEn: "Purchasing",
  },
  "Purchasing.ReceiveGoods": {
    labelAr: "استلام البضائع",
    labelEn: "Receive goods",
    descriptionAr: "استلام شحنة المورد وفحص السيريالات.",
    descriptionEn: "Receive a supplier shipment and inspect serials.",
    groupAr: "المشتريات",
    groupEn: "Purchasing",
  },
  "Purchasing.PostBill": {
    labelAr: "ترحيل فاتورة الشراء",
    labelEn: "Post purchase bill",
    descriptionAr: "ترحيل فاتورة الشراء ماليًا.",
    descriptionEn: "Post a purchase bill financially.",
    groupAr: "المشتريات",
    groupEn: "Purchasing",
  },
  "Purchasing.ViewSupplierFinancials": {
    labelAr: "عرض بيانات المورد المالية",
    labelEn: "View supplier financials",
    descriptionAr: "قراءة بيانات البنك والحساب وIBAN مع Audit.",
    descriptionEn: "Read bank, account, and IBAN with audit.",
    groupAr: "المشتريات",
    groupEn: "Purchasing",
  },
  "Purchasing.ManageSupplierFinancials": {
    labelAr: "إدارة بيانات المورد المالية",
    labelEn: "Manage supplier financials",
    descriptionAr: "تعديل بيانات البنك والحساب وIBAN مع RowVersion وAudit.",
    descriptionEn: "Edit bank, account, and IBAN with row version and audit.",
    groupAr: "المشتريات",
    groupEn: "Purchasing",
  },
  "Finance.ViewVaults": {
    labelAr: "عرض الخزائن",
    labelEn: "View vaults",
    descriptionAr: "استعراض أرصدة الخزائن والحركات النقدية.",
    descriptionEn: "View vault balances and cash movements.",
    groupAr: "المالية",
    groupEn: "Finance",
  },
  "Finance.VerifyPayment": {
    labelAr: "التحقق من المدفوعات",
    labelEn: "Verify payments",
    descriptionAr: "مراجعة إيصالات الدفع واعتمادها.",
    descriptionEn: "Review and approve payment receipts.",
    groupAr: "المالية",
    groupEn: "Finance",
  },
  "Finance.Disburse": {
    labelAr: "الصرف من الخزينة",
    labelEn: "Disburse",
    descriptionAr: "تسجيل صرف نقدي من الخزينة.",
    descriptionEn: "Record a cash disbursement from a vault.",
    groupAr: "المالية",
    groupEn: "Finance",
  },
  "Finance.ManageExpenses": {
    labelAr: "إدارة المصروفات",
    labelEn: "Manage expenses",
    descriptionAr: "تسجيل وتعديل المصروفات التشغيلية.",
    descriptionEn: "Record and edit operating expenses.",
    groupAr: "المالية",
    groupEn: "Finance",
  },
  "Finance.ViewReports": {
    labelAr: "عرض التقارير المالية",
    labelEn: "View finance reports",
    descriptionAr: "استعراض الأرباح والتكاليف والتقارير المالية.",
    descriptionEn: "View profit, cost, and financial reports.",
    groupAr: "المالية",
    groupEn: "Finance",
  },
  "System.ManageUsers": {
    labelAr: "إدارة المستخدمين والأدوار",
    labelEn: "Manage users and roles",
    descriptionAr: "إنشاء المستخدمين والأدوار وتعديل الصلاحيات ضمن حدود التفويض.",
    descriptionEn: "Create users and roles and edit permissions within delegation limits.",
    groupAr: "النظام",
    groupEn: "System",
  },
  "System.ViewAuditLogs": {
    labelAr: "عرض سجل التدقيق",
    labelEn: "View audit logs",
    descriptionAr: "استعراض أحداث الأمان والتدقيق.",
    descriptionEn: "View security and audit events.",
    groupAr: "النظام",
    groupEn: "System",
  },
  "System.ViewErrorLogs": {
    labelAr: "عرض سجلات الأخطاء",
    labelEn: "View error logs",
    descriptionAr: "استعراض سجلات أخطاء التشغيل.",
    descriptionEn: "View operational error logs.",
    groupAr: "النظام",
    groupEn: "System",
  },
  "System.ExecuteAllowlistedAction": {
    labelAr: "تنفيذ إجراء تشغيلي",
    labelEn: "Execute allowlisted action",
    descriptionAr: "تنفيذ إجراءات تشغيلية مصرح بها فقط.",
    descriptionEn: "Execute only explicitly allowlisted operational actions.",
    groupAr: "النظام",
    groupEn: "System",
  },
  "System.ManageBackups": {
    labelAr: "إدارة النسخ الاحتياطية",
    labelEn: "Manage backups",
    descriptionAr: "إنشاء النسخ الاحتياطية واستعادتها.",
    descriptionEn: "Create and restore backups.",
    groupAr: "النظام",
    groupEn: "System",
  },
  "System.StoreSettings": {
    labelAr: "إدارة إعدادات المتجر",
    labelEn: "Manage store settings",
    descriptionAr: "تعديل واتساب وروابط التواصل ومعلومات الاستلام ونصوص الثقة.",
    descriptionEn: "Edit WhatsApp, social links, pickup details, and trust copy.",
    groupAr: "النظام",
    groupEn: "System",
  },
};

export function permissionLabel(key: string, locale: Locale): string {
  const copy = permissionCopy[key];
  return copy ? (locale === "ar" ? copy.labelAr : copy.labelEn) : key;
}

export function permissionDescription(key: string, locale: Locale): string {
  const copy = permissionCopy[key];
  return copy ? (locale === "ar" ? copy.descriptionAr : copy.descriptionEn) : "";
}

export function permissionGroup(key: string, locale: Locale): string {
  const copy = permissionCopy[key];
  if (copy) return locale === "ar" ? copy.groupAr : copy.groupEn;
  return key.split(".")[0] ?? key;
}
