# دليل نشر التحديثات — ChickenDist

## الإعداد لمرة واحدة فقط

### 1. رفع كود المصدر على GitHub

```bash
# من جهازك (Windows)
cd مسار_المشروع
git init
git remote add origin https://github.com/ssssssdssssd3-cell/ChickenDist.git
git add .
git commit -m "Initial commit"
git push -u origin main
```

### 2. إنشاء Personal Access Token

1. افتح: https://github.com/settings/tokens/new
2. اسم التوكن: `UPDATES_REPO_TOKEN`
3. صلاحيات: ✅ **repo** (كامل)
4. اضغط **Generate token** وانسخه فوراً

### 3. إضافة التوكن لـ Secrets

1. افتح repo المصدر على GitHub
2. Settings → Secrets and variables → Actions
3. اضغط **New repository secret**
4. الاسم: `UPDATES_REPO_TOKEN`
5. القيمة: الـ token اللي نسخته

---

## كل مرة تريد إصدار تحديث جديد

### على جهازك (3 خطوات فقط):

```bash
# 1. اعمل commit للتعديلات
git add .
git commit -m "وصف التعديلات هنا"

# 2. أنشئ tag برقم الإصدار الجديد مع ملاحظة التحديث
git tag -a v1.0.20 -m "إصلاح مشاكل Transaction في المبيعات والمدفوعات
إصلاح خصم الأصناف اليدوي
إصلاح تجميد الواجهة أثناء التحديث"

# 3. ارفع الكود والـ tag
git push origin main
git push origin v1.0.20
```

### ماذا يحدث تلقائياً بعدها؟

```
GitHub Actions يعمل على الفور:

[1] Build        → يبني ChickenDist.exe على Windows
[2] SHA256       → يحسب checksum للـ .exe
[3] Deploy       → يرفع الـ .exe لـ ChickenDistUpdates repo
[4] update.txt   → يحدّث version + sha256 + changelog
[5] Release      → ينشئ GitHub Release رسمي

الوقت الكلي: ~3-5 دقائق

عند فتح العميل للبرنامج التالي مرة:
→ يقرأ update.txt → يجد إصدار جديد → يعرض رسالة التحديث
```

---

## متابعة حالة البناء

- افتح: `https://github.com/ssssssdssssd3-cell/ChickenDist/actions`
- ستجد الـ workflow يعمل فور رفع الـ tag
- أي خطأ ستجده بالتفصيل في الـ logs

## هيكل الـ Repos

```
ssssssdssssd3-cell/ChickenDist          ← كود المصدر (private)
ssssssdssssd3-cell/ChickenDistUpdates   ← ملفات التحديث (public)
    ├── ChickenDist.bin   ← الـ .exe الجديد
    └── update.txt        ← معلومات الإصدار
```
