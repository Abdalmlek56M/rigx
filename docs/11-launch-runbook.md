# RIGX — خطوات الإطلاق (للمالك)

> كل خطوة هنا بتعملها إنت بإيدك. **ماتبعتش أي باسورد أو مفتاح أو كود في الشات** (ولا لـ Claude ولا لـ DeepSeek). اللي بيتبعت في الشات: ناتج أوامر الفحص بس.
> الأوامر اللي بتبدأ بـ `sudo` بتتكتب على السيرفر بعد ما تدخل بـ SSH. الأوامر اللي مكتوب جنبها **(على جهازك)** بتتكتب في PowerShell على الويندوز.
> آخر مراجعة: 19 نوفمبر 2026. التفاصيل التقنية في `docs/06-operations.md` قسم "Production".

## الترتيب
1. فحص السيرفر (وتبعت الناتج)
2. مفتاح SSH وقفل الباسورد
3. Cloudflare والشهادة
4. الكود والإعدادات على السيرفر
5. Gmail (باسورد التطبيقات)
6. Google sign-in
7. Sentry
8. التشغيل و nginx
9. حساب المالك
10. النسخ الاحتياطي برّه السيرفر
11. Search Console و Analytics و Meta pixel
12. سياسة الخصوصية، واستيراد المخزون، والمراجعة الأخيرة

---

## 1. فحص السيرفر
ادخل السيرفر زي ما بتدخل عادي، واكتب:
```bash
sudo ss -tlnp
docker ps
docker network ls
docker network inspect $(docker network ls -q) | grep -E '"Name"|"Subnet"'
nginx -v
docker compose version
```
**ابعت الناتج في الشات** (مافيهوش أسرار). منه هنتأكد إن البورتات `3001` و`8081` والشبكة `172.29.208.0/24` فاضيين ومش هيلمسوا الموقعين الشغالين. لو في تعارض هقولك تغيّر إيه.

## 2. مفتاح SSH وقفل الدخول بالباسورد
**(على جهازك)** افتح PowerShell:
```powershell
ssh-keygen -t ed25519 -C "rigx-owner"
```
دوس Enter على المكان الافتراضي، واكتب passphrase تفتكرها (أو سيبها فاضية).
بعدين ابعت المفتاح العام للسيرفر (غيّر `USER` و`IP` باللي عندك):
```powershell
type $env:USERPROFILE\.ssh\id_ed25519.pub | ssh USER@IP "mkdir -p ~/.ssh && chmod 700 ~/.ssh && cat >> ~/.ssh/authorized_keys && chmod 600 ~/.ssh/authorized_keys"
```
جرّب في **نافذة جديدة**: `ssh USER@IP`. لو دخل من غير ما يسأل على باسورد السيرفر، كمّل:
```bash
echo -e "PasswordAuthentication no\nKbdInteractiveAuthentication no" | sudo tee /etc/ssh/sshd_config.d/01-rigx-no-password.conf
sudo sshd -t && sudo systemctl reload ssh
```
**سيب النافذة القديمة مفتوحة** وجرّب تدخل تاني من نافذة جديدة. لو دخل، تمام. لو ماعرفش، ارجع للنافذة القديمة واكتب `sudo rm /etc/ssh/sshd_config.d/01-rigx-no-password.conf && sudo systemctl reload ssh` وابعتلي.
> احتفظ بنسخة من فولدر `.ssh` اللي على جهازك في مكان آمن (فلاشة). من غيره مش هتعرف تدخل السيرفر.

## 3. Cloudflare والشهادة
1. ادخل dash.cloudflare.com ← اختار `rigxhardware.com` ← **DNS** ← **Records**.
2. **Add record**: النوع `A`، الاسم `@`، الـ IPv4 = IP السيرفر، و**Proxy status برتقاني (Proxied)** ← Save.
3. سجل تاني: `A`، الاسم `www`، نفس الـ IP، برتقاني ← Save. (لو السجلين موجودين، اتأكد إنهم برتقاني بس.) **ماتلمسش السجلات التانية**.
4. **SSL/TLS** ← **Overview**: شوف المكتوب.
   - لو مكتوب **Full (strict)**: تمام، كمّل.
   - لو حاجة تانية: **ماتغيّرهاش** (ممكن توقّع الدومينات الفرعية التانية). بدلها: **Rules** ← **Configuration Rules** ← **Create rule** ← الاسم `RIGX strict SSL` ← **Custom filter expression**: Field `Hostname`، Operator `equals`، Value `rigxhardware.com`، ودوس **Or** وزوّد نفس الشرط بـ `www.rigxhardware.com` ← تحت **SSL** اختار **Full (strict)** ← Deploy.
5. **SSL/TLS** ← **Edge Certificates**: شغّل **Always Use HTTPS**.
6. **SSL/TLS** ← **Origin Server** ← **Create Certificate** ← سيب الاختيارات (RSA، `rigxhardware.com` و`*.rigxhardware.com`، 15 سنة) ← Create.
7. هيظهرلك مربعين. على السيرفر:
```bash
sudo mkdir -p /etc/ssl/rigx && sudo chmod 700 /etc/ssl/rigx
sudo nano /etc/ssl/rigx/origin.pem
```
الصق **Origin Certificate** (المربع الأول كله) ← `Ctrl+O` Enter ← `Ctrl+X`.
```bash
sudo nano /etc/ssl/rigx/origin.key
```
الصق **Private key** (المربع التاني) ← احفظ واقفل.
```bash
sudo chmod 600 /etc/ssl/rigx/origin.key
```
> الـ Private key مابيظهرش تاني في Cloudflare. ماتبعتهوش لحد.

## 4. الكود والإعدادات على السيرفر
**مفتاح القراءة من GitHub (deploy key):**
```bash
ssh-keygen -t ed25519 -f ~/.ssh/rigx_deploy -N "" -C "rigx-vps"
cat ~/.ssh/rigx_deploy.pub
```
انسخ السطر اللي ظهر ← github.com/Abdalmlek56M/rigx ← **Settings** ← **Deploy keys** ← **Add deploy key** ← Title `VPS` ← الصق ← **سيب "Allow write access" مقفول** ← Add key.
```bash
printf "Host github-rigx\n  HostName github.com\n  User git\n  IdentityFile ~/.ssh/rigx_deploy\n  IdentitiesOnly yes\n" >> ~/.ssh/config
sudo mkdir -p /srv/rigx && sudo chown "$USER" /srv/rigx
git clone git@github-rigx:Abdalmlek56M/rigx.git /srv/rigx/app
cd /srv/rigx/app
bash deploy/init-server.sh
```
السكربت بيعمل فولدر النسخ الاحتياطي والشهادة الداخلية، وبيعمل `deploy/.env` بباسوردات عشوائية للداتابيز. **مابيطبعش أي سر.** الباقي في الملف ده هتملاه في الخطوات الجاية بـ:
```bash
nano /srv/rigx/app/deploy/.env
```
(`Ctrl+O` Enter للحفظ، `Ctrl+X` للخروج.)

## 5. Gmail: باسورد التطبيقات
1. ادخل بحساب `rigxhardware@gmail.com` على myaccount.google.com ← **Security** (الأمان).
2. **2-Step Verification** لازم تبقى **On**. لو مقفولة شغّلها برقم موبايلك.
3. في خانة البحث فوق اكتب **App passwords** وافتحها ← الاسم `RIGX server` ← Create.
4. هيظهر كود 16 حرف. افتح `deploy/.env` على السيرفر واكتبه جنب `SMTP_APP_PASSWORD=` **من غير مسافات**. احفظ.
> الكود ده بيتكتب في السيرفر بس. لو اتسرب: امسحه من نفس الصفحة واعمل واحد جديد.

## 6. Google sign-in
1. console.cloud.google.com بحساب `rigxhardware@gmail.com`. **لو ظهرلك "Start free" أو "Activate" للفوترة: اقفله.** تسجيل الدخول بجوجل مش محتاج فيزا ولا فوترة.
2. فوق جنب الشعار ← **Select a project** ← **New project** ← الاسم `RIGX` ← Create، واختاره.
3. القائمة ← **Google Auth Platform** ← **Get started**:
   - App name: `RIGX`
   - User support email: `rigxhardware@gmail.com`
   - Audience: **External**
   - Contact email: `rigxhardware@gmail.com` ← وافق ← Create.
4. **Branding**:
   - Application home page: `https://rigxhardware.com`
   - Application privacy policy link: `https://rigxhardware.com/policies/privacy`
   - Application terms of service link: `https://rigxhardware.com/policies/terms`
   - Authorized domains: `rigxhardware.com` ← Save.
5. **Audience** ← **Publish app** ← Confirm (يبقى "In production"). إحنا بنطلب الاسم والإيميل بس، فمش محتاج مراجعة من جوجل.
6. **Clients** ← **Create client** ← Application type **Web application** ← الاسم `RIGX web`:
   - **Authorized JavaScript origins** ← Add URI ← `https://rigxhardware.com`
   - **Authorized redirect URIs**: **سيبها فاضية**. زرار جوجل عندنا بيفتح نافذة صغيرة ويرجع على نفس الصفحة، مش بيعمل redirect.
   - Create.
7. انسخ **Client ID** (آخره `.apps.googleusercontent.com`) وحطه في `deploy/.env` جنب `GOOGLE_CLIENT_ID=`. الـ Client secret **مش محتاجينه**، ماتحطوش في أي حتة.

## 7. Sentry (تتبع الأخطاء + مراقبة الموقع)
1. sentry.io ← اتأكد إن الخطة **Developer (Free)** (Settings ← Subscription). ماتفعّلش تجربة مدفوعة.
2. **Projects** ← **Create Project** ← اختار **ASP.NET Core** ← الاسم `rigx-api` ← Create. هيظهر **DSN** (لينك بيبدأ بـ `https://`). حطه في `deploy/.env` جنب `SENTRY_DSN_API=`.
3. مشروع تاني: **Next.js** ← الاسم `rigx-web` ← حط الـ DSN جنب `SENTRY_DSN_WEB=`. (لو الصفحة طلبت تشغّل wizard أو أمر `npx`، تجاهله. الكود جاهز.)
4. المراقبة (بعد خطوة 8، لما الموقع يشتغل): **Insights** ← **Uptime** (أو **Alerts** ← **Create Alert** ← **Uptime Monitor**) ← URL: `https://rigxhardware.com/health` ← Interval: 5 دقايق ← الإشعار على إيميلك ← Save.
> لو الموقع وقع هيوصلك إيميل. `/health` بيتأكد إن الـ API والداتابيز شغالين.

## 8. التشغيل و nginx
**أ. شغّل RIGX** (أول مرة بتاخد 10–15 دقيقة):
```bash
cd /srv/rigx/app
docker compose -f deploy/compose.prod.yaml up -d --build
docker compose -f deploy/compose.prod.yaml ps
curl -s http://127.0.0.1:8081/health/ready; echo
curl -sI http://127.0.0.1:3001/ | head -n 1
```
المفروض: `api` و`web` و`postgres` حالتهم `running (healthy)` أو `Up`، و`migrate` حالته `Exited (0)`، والسطرين الأخيرين `Healthy` و`HTTP/1.1 200`. لو حاجة تانية ابعت ناتج:
```bash
docker compose -f deploy/compose.prod.yaml logs --tail 80 migrate api web
```
(اتأكد إن الناتج مافيهوش باسورد قبل ما تبعته. الـ logs مابتطبعش أسرار في العادي.)

**ب. nginx:**
```bash
sudo cp deploy/nginx/rigx-cloudflare-real-ip.conf /etc/nginx/snippets/
sudo cp deploy/nginx/rigxhardware.com.conf /etc/nginx/sites-available/rigxhardware.com
sudo ln -s /etc/nginx/sites-available/rigxhardware.com /etc/nginx/sites-enabled/rigxhardware.com
sudo nginx -t
```
لازم يقول `syntax is ok` و`test is successful`. **لو قال error: ماتعملش reload**، ابعت الناتج. لو تمام:
```bash
sudo systemctl reload nginx
```
افتح `https://rigxhardware.com` من موبايلك. وكمان افتح الموقعين التانيين واتأكد إنهم شغالين زي ما هم.

## 9. حساب المالك
```bash
cd /srv/rigx/app
docker compose -f deploy/compose.prod.yaml run --rm -e OwnerBootstrap__Enabled=true api --bootstrap-owner
```
هيسألك اسم مستخدم، وإيميلك، واسمك. بعد كده ادخل `https://rigxhardware.com/login` بالإيميل ده، وهيوصلك كود 6 أرقام من `rigxhardware@gmail.com`. لو الكود ماوصلش في دقيقتين، بص في Spam، ولو مش موجود ابعت ناتج `docker compose -f deploy/compose.prod.yaml logs --tail 50 api`.

**بعد أول دخول من لوحة التحكم:** الإعدادات (اسم المحل، الواتساب، الشحن، طرق الدفع)، و**النسخ الاحتياطي** ← "اعمل نسخة دلوقتي" ← بعد ما تخلص ← "اختبار الاسترجاع". لازم ينجح.

## 10. النسخ الاحتياطي برّه السيرفر (Google Drive متشفّر)
السيستم بيعمل نسخة كل يوم الساعة 3 الفجر على السيرفر. هنا بنبعت نسخة **متشفّرة** لـ Google Drive، علشان لو السيرفر ضاع الداتا ماتضيعش.

**أ. (على جهازك)** نزّل rclone لويندوز من الموقع الرسمي rclone.org/downloads ← فك الضغط في فولدر.

**ب. على السيرفر:**
```bash
sudo apt-get update && sudo apt-get install -y rclone
sudo rclone config
```
جاوب كده:
1. `n` (remote جديد) ← الاسم `gdrive` ← النوع اكتب `drive` ← `client_id` و`client_secret` سيبهم فاضيين (Enter) ← `scope` اختار `1` ← الباقي Enter ← **"Use web browser to automatically authenticate?"** اكتب `n`.
2. هيطبعلك أمر شكله `rclone authorize "drive" "..."`. انسخه، و**(على جهازك)** في PowerShell جوّه فولدر rclone اكتب `.\rclone.exe authorize "drive" "..."` (نفس اللي اتنسخ). المتصفح هيفتح ← ادخل بـ `rigxhardware@gmail.com` ← Allow. هيطبعلك كود طويل في PowerShell ← انسخه والصقه في السيرفر.
3. `Configure this as a Shared Drive?` ← `n` ← `y` (Keep this remote).
4. remote تاني: `n` ← الاسم `rigxbackup` ← النوع `crypt` ← `remote` اكتب `gdrive:rigx-backups` ← `filename_encryption` اختار `1` (standard) ← `directory_name_encryption` ← `1` (true).
5. **Password**: اختار `g` (generate) ← الطول `128` ← `y`. **انسخ الباسورد ده واحفظه في مكان آمن برّه السيرفر** (مدير باسوردات أو ورقة في مكان آمن). من غيره النسخة المتشفّرة مالهاش لازمة.
6. **Password2 (salt)**: نفس الطريقة `g` ← `128` ← `y` ← واحفظه هو كمان.
7. الباقي Enter ← `y` ← `q` (خروج).

**ج. جرّب واعمل الجدول:**
```bash
sudo /srv/rigx/app/deploy/offsite-backup.sh
```
لازم يقول `done, newest off-host backup: rigx-backup-...zip` (بعد ما تكون عملت نسخة في خطوة 9). بعدين:
```bash
sudo crontab -e
```
(لو سألك على editor اختار `1` nano.) ضيف السطر (بيشتغل كل يوم 3:30 بتوقيت السيرفر، بعد نسخة 3 الفجر بتوقيت القاهرة) ده في الآخر، واحفظ:
```
30 3 * * * /srv/rigx/app/deploy/offsite-backup.sh >> /srv/rigx/offsite-backup.log 2>&1
```
> النسخ ملفاتها محمية: rclone والجدول بيشتغلوا بـ `sudo` علشان كده.
> مرة كل شهر: بص على آخر سطر في `sudo tail /srv/rigx/offsite-backup.log`، واعمل "اختبار الاسترجاع" من لوحة التحكم.

## 11. Search Console و Analytics و Meta pixel
**Search Console:**
1. search.google.com/search-console ← **Add property** ← **Domain** ← `rigxhardware.com` ← Continue.
2. انسخ الـ TXT ← Cloudflare ← DNS ← Add record ← النوع `TXT`، الاسم `@`، المحتوى = اللي نسخته ← Save ← ارجع Search Console ← **Verify** (لو ماتأكدش، استنى ربع ساعة وجرّب تاني).
3. **Sitemaps** ← اكتب `sitemap.xml` ← Submit.
4. **URL inspection** ← `https://rigxhardware.com/` ← **Request indexing**. وجرّب صفحة منتج على search.google.com/test/rich-results.

**Google Analytics 4:**
1. analytics.google.com ← **Start measuring** ← Account name `RIGX` ← Property name `RIGX`، Time zone **Egypt**، Currency **EGP** ← Business details ← Create.
2. Platform **Web** ← URL `rigxhardware.com` ← Stream name `RIGX` ← سيب **Enhanced measurement** شغال ← Create.
3. انسخ **Measurement ID** (`G-XXXXXXX`) وحطه في `deploy/.env` جنب `GA_ID=`.
4. Admin ← **Data retention** ← خليها 14 شهر ← Save.

**Meta pixel:**
1. business.facebook.com ← **Events Manager** ← **Connect data sources** ← **Web** ← الاسم `RIGX` ← Create.
2. لو سألك على الطريقة اختار **Install code manually** وانسخ **Pixel ID** بس (رقم). حطه في `deploy/.env` جنب `META_PIXEL_ID=`. ماتنسخش الكود نفسه.
3. **Business settings** ← **Brand safety** ← **Domains** ← Add ← `rigxhardware.com` ← **DNS TXT** ← حطه في Cloudflare زي Search Console ← Verify.

**بعد ما تحط الأرقام دي في `.env`** لازم تعيد بناء الموقع (الأرقام دي بتتكتب جوّه الموقع وقت البناء):
```bash
cd /srv/rigx/app && docker compose -f deploy/compose.prod.yaml up -d --build web
```
السيستم بيقيس: زيارات الصفحات، وكل ضغطة على زرار واتساب (في GA اسمها `generate_lead`، وفي Meta اسمها `Contact`). ده اللي هيقولك أي إعلان بيجيب رسايل. لوحة التحكم (`/admin`) مش بتتقاس.

## 12. المراجعة الأخيرة
- **السياسات (لازم قبل الإطلاق):** لوحة التحكم ← **السياسات**. السيرفر الجديد مافيهوش أي سياسة، فانسخ الست سياسات المعتمدة (الشحن، الاسترجاع، الاستبدال، الضمان، الشروط، الخصوصية) بالعربي والإنجليزي من `docs/12-policies.md` ← "احفظ المسودة" ← "انشر"، والخطوات بالظبط في أول الملف ده. جوجل بيطلب صفحة الخصوصية دي علشان تسجيل الدخول. (راجع الخصوصية والمرتجع مع المحامي، زي ما في `09-founder-playbook.md` §2.)
- **المخزون:** لوحة التحكم ← **استيراد المخزون** ← نزّل القالب ← **امسح سطر المثال** وانقل بضاعتك فيه (سطر لكل موديل، والمستعمل كمية 1 لكل قطعة أو الكمية الحقيقية) ← ارفعه ← صلّح الأخطاء اللي باللون الأحمر ← استورد. بعد كده ضيف 3 صور لكل قطعة علشان تظهر في الموقع.
- جرّب بنفسك من الموبايل: الدخول بالإيميل، والدخول بجوجل، وطلب تجريبي، وزرار الواتساب، وصفحة الضمان.
- بعد يومين: Search Console ← **Pages** و**Sitemaps** مافيهاش أخطاء، وSentry مافيهوش أخطاء جديدة.

## التحديثات بعد كده
لما يتقالك "في تحديث":
```bash
cd /srv/rigx/app && git pull && docker compose -f deploy/compose.prod.yaml up -d --build
```
الداتابيز بتتحدّث لوحدها قبل ما الـ API يشتغل. لو حاجة وقعت ابعت `docker compose -f deploy/compose.prod.yaml ps`.

## بوابة الدفع (لسه)
الدفع أونلاين بالكارت متصمم ومتوثق بس (`docs/06-operations.md` "Payment gateway")، مش متنفذ. لما تقرر تفتح حساب Paymob، قولّي.
