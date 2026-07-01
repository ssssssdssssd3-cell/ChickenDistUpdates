# ChickenDist — نظام توزيع الدواجن

[![Build ChickenDist](https://github.com/YOUR_USERNAME/YOUR_REPO/actions/workflows/build.yml/badge.svg)](https://github.com/YOUR_USERNAME/YOUR_REPO/actions/workflows/build.yml)

نظام إدارة متكامل لتوزيع الدواجن — مبيعات، مشتريات، مخزون، مناديب، حسابات عملاء، تقارير.

---

## 🚀 البناء التلقائي (GitHub Actions)

الـ workflow يبني البرنامج تلقائياً عند كل Push أو Pull Request.

**لتحميل آخر build:**
1. افتح صفحة **Actions** في الـ repo
2. اختر آخر run ناجح
3. من قسم **Artifacts** حمّل `ChickenDist-Release`

**لإنشاء Release رسمي:**
```bash
git tag v1.0.0
git push origin v1.0.0
```
سيُنشئ الـ workflow Release تلقائياً مع ملف ZIP جاهز للتحميل.

---

## 🛠️ البناء اليدوي

### متطلبات
- Windows 10/11
- .NET SDK 6+ أو Visual Studio 2019/2022
- SQL Server 2016+ أو SQL Server Express (مجاني)

### خطوات البناء

**من Visual Studio:**
```
1. افتح ChickenDist/ChickenDist.csproj
2. اختر Configuration: Release
3. Build → Build Solution (Ctrl+Shift+B)
4. الناتج في: ChickenDist/bin/Release/net48/
```

**من Command Line:**
```bash
cd ChickenDist
dotnet restore
dotnet publish -c Release -f net48 -o ../publish/
```

---

## 🗄️ إعداد قاعدة البيانات

```sql
-- شغّل هذا السكريبت مرة واحدة فقط على SQL Server
-- الملف موجود في: ChickenDist/Database/Script.sql
```

من SQL Server Management Studio:
1. افتح `Database/Script.sql`
2. نفّذ (F5)
3. ستُنشأ قاعدة البيانات `ChickenDistDB` تلقائياً

---

## ⚙️ إعداد الاتصال

عدّل `App.config` (أو من داخل البرنامج → الإعدادات):

```xml
<connectionStrings>
  <add name="ChickenDB"
       connectionString="Server=YOUR_SERVER;Database=ChickenDistDB;Integrated Security=True;"
       providerName="System.Data.SqlClient"/>
</connectionStrings>
```

---

## 📁 هيكل المشروع

```
ChickenDist/
├── Core/               — مكونات مشتركة (DbHelper, Session, Logger, UpdateManager)
├── DAL/                — طبقة الوصول للبيانات (Sales, Purchase, Inventory, Core)
├── Forms/              — واجهات المستخدم (WinForms)
├── Database/           — سكريبت إنشاء قاعدة البيانات
├── Program.cs          — نقطة الدخول
└── App.config          — إعدادات الاتصال
```

---

## 📋 متطلبات التشغيل

| المكوّن | الإصدار |
|--------|--------|
| Windows | 10 / 11 |
| .NET Framework | 4.8 |
| SQL Server | 2016+ أو Express |
| RAM | 512 MB كحد أدنى |
| مساحة | 50 MB |
