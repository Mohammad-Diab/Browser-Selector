using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Markup;

namespace BrowserSelector
{
    /// <summary>UI strings in English and Arabic. Arabic also flips the layout (RTL).</summary>
    public static class Loc
    {
        public static bool IsArabic { get; private set; }

        public static FlowDirection Flow => IsArabic ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

        public static void Init(string language)
        {
            IsArabic = language == "ar"
                || (language != "en" && CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar");
        }

        public static string T(string key)
        {
            if (IsArabic && Ar.TryGetValue(key, out var ar)) return ar;
            return En.TryGetValue(key, out var en) ? en : key;
        }

        public static string F(string key, params object[] args) => string.Format(T(key), args);

        static readonly Dictionary<string, string> En = new Dictionary<string, string>
        {
            ["Picker.Copy"] = "Copy link (Ctrl+C)",
            ["Picker.Settings"] = "Settings",
            ["Picker.Close"] = "Close (Esc)",
            ["Picker.Private"] = "Private window",
            ["Picker.Remember"] = "Always open {0} this way",
            ["Picker.Hint"] = "1–9 pick · Enter default · Shift private · Esc cancel",
            ["Picker.Default"] = "Enter",
            ["Picker.NoBrowsers"] = "No browsers found. Copy the link and open it yourself.",
            ["Picker.File"] = "Local file",
            ["Picker.Copied"] = "Link copied",

            ["Error.Launch"] = "Couldn't open {0}.\n\n{1}",
            ["Error.BadLink"] = "Browser Selector opens web links (http, https) and local files only:\n\n{0}",
            ["Error.Title"] = "Browser Selector",
            ["Error.SetAside"] = "Your settings file couldn't be read, so it was set aside and the default settings are in use. Your site rules are still in:\n\n{0}\n\nFix that file and rename it back to settings.json to get them back.",
            ["Error.RuleNotSaved"] = "The link opened, but the rule for {0} wasn't saved:\n\n{1}",
            ["Error.Crash"] = "Something went wrong:\n\n{0}",
            ["Error.CrashLink"] = "Something went wrong:\n\n{0}\n\nThe link: {1}\n(Ctrl+C copies this message.)",

            ["Settings.Title"] = "Browser Selector — Settings",
            ["Tab.General"] = "General",
            ["Tab.Rules"] = "Site rules",
            ["Tab.Browsers"] = "Browsers",
            ["Tab.About"] = "About",

            ["Reg.Header"] = "Default browser",
            ["Reg.NotRegistered"] = "Browser Selector is not registered as a browser yet.",
            ["Reg.Registered"] = "Registered as a browser.",
            ["Reg.OtherPath"] = "Registered from another folder. Register again to use this copy.",
            ["Reg.IsDefault"] = "It is your default browser: links open the picker.",
            ["Reg.NotDefault"] = "It is not your default browser yet.",
            ["Reg.Register"] = "Register",
            ["Reg.OpenSettings"] = "Open Default apps",
            ["Reg.Unregister"] = "Unregister",
            ["Reg.Steps"] = "Windows lets only you choose the default browser. In Default apps, select Browser Selector, then \"Set default\".",
            ["Reg.Done"] = "Registered. Windows Default apps will open now: select Browser Selector, then \"Set default\".",
            ["Reg.Removed"] = "Unregistered. If it was your default browser, pick another one in Default apps.",
            ["Reg.Failed"] = "Couldn't change the registration:\n\n{0}",

            ["Open.Header"] = "When you open a link",
            ["Open.Ask"] = "Ask me every time",
            ["Open.AskHint"] = "The picker shows up. Enter opens your main browser.",
            ["Open.Auto"] = "Open it in my main browser",
            ["Open.AutoHint"] = "Site rules are the exceptions. To choose anyway, hold Shift while you open the link.",
            ["Open.EditRules"] = "Edit site rules",
            ["Open.Main"] = "Main browser",
            ["Gen.Advanced"] = "Advanced options",
            ["Gen.ShowProfiles"] = "Show each browser profile in the picker",
            ["Gen.Language"] = "Language",
            ["Lang.Auto"] = "Same as Windows",
            ["Gen.Try"] = "Try it",
            ["Gen.TryHint"] = "Paste a link. \"Open picker\" always asks; \"Open like a link\" does what a real click would. Nothing changes in Windows.",
            ["Gen.TryButton"] = "Open picker",
            ["Gen.TryRules"] = "Open like a link",
            ["Gen.RuleMatched"] = "Rule {0} → {1}: opened without asking.",
            ["Gen.RuleMissing"] = "Rule {0} points to a browser or profile that isn't available, so the picker opened.",
            ["Gen.NoRule"] = "No rule matches {0}, so the picker opened.",
            ["Gen.MainOpened"] = "No rule matches {0}: opened in your main browser, {1}.",
            ["Gen.MainMissing"] = "Your main browser (or its profile) isn't available, so the picker opened.",

            ["Rules.Hint"] = "Links to these sites open right away, without asking. A rule also covers subdomains.",
            ["Rules.Site"] = "Site",
            ["Rules.OpensWith"] = "Opens with",
            ["Rules.Add"] = "Add rule",
            ["Rules.Remove"] = "Remove",
            ["Rules.Private"] = "private",
            ["Rules.Missing"] = "not installed",
            ["Rules.ProfileMissing"] = "profile not found",
            ["Rules.Empty"] = "No rules yet. Tick \"Always open … this way\" in the picker, or add one here.",
            ["Rules.BadDomain"] = "Type a site like example.com.",
            ["Rules.PrivateBox"] = "Private",

            ["Browsers.Hint"] = "The browsers Windows knows about. Install or remove a browser, then refresh.",
            ["Browsers.Profiles"] = "Profiles: {0}",
            ["Browsers.Refresh"] = "Refresh",
            ["Browsers.None"] = "No browsers found.",

            ["About.Version"] = "Version {0}",
            ["About.Text"] = "Asks which browser to open each link with.",
            ["About.SettingsFile"] = "Settings file",
            ["About.License"] = "Free and open source, MIT license.",
        };

        static readonly Dictionary<string, string> Ar = new Dictionary<string, string>
        {
            ["Picker.Copy"] = "نسخ الرابط (Ctrl+C)",
            ["Picker.Settings"] = "الإعدادات",
            ["Picker.Close"] = "إغلاق (Esc)",
            ["Picker.Private"] = "نافذة خاصة",
            ["Picker.Remember"] = "افتح {0} هكذا دائماً",
            ["Picker.Hint"] = "الأرقام للاختيار · Enter الافتراضي · Shift خاص · Esc إلغاء",
            ["Picker.Default"] = "Enter",
            ["Picker.NoBrowsers"] = "لم يُعثر على متصفحات. انسخ الرابط وافتحه بنفسك.",
            ["Picker.File"] = "ملف محلي",
            ["Picker.Copied"] = "تم نسخ الرابط",

            ["Error.Launch"] = "تعذّر فتح {0}.\n\n{1}",
            ["Error.BadLink"] = "Browser Selector يفتح روابط الويب (http و https) والملفات المحلية فقط:\n\n{0}",
            ["Error.Title"] = "Browser Selector",
            ["Error.SetAside"] = "تعذّرت قراءة ملف الإعدادات، فنُقل جانباً وتُستخدم الإعدادات الافتراضية الآن. قواعد مواقعك ما زالت محفوظة في:\n\n{0}\n\nأصلح هذا الملف وأعد تسميته إلى settings.json لاستعادتها.",
            ["Error.RuleNotSaved"] = "فُتح الرابط، لكن لم تُحفظ القاعدة لـ {0}:\n\n{1}",
            ["Error.Crash"] = "حدث خطأ:\n\n{0}",
            ["Error.CrashLink"] = "حدث خطأ:\n\n{0}\n\nالرابط: {1}\n(Ctrl+C ينسخ هذه الرسالة.)",

            ["Settings.Title"] = "Browser Selector — الإعدادات",
            ["Tab.General"] = "عام",
            ["Tab.Rules"] = "قواعد المواقع",
            ["Tab.Browsers"] = "المتصفحات",
            ["Tab.About"] = "حول",

            ["Reg.Header"] = "المتصفح الافتراضي",
            ["Reg.NotRegistered"] = "Browser Selector غير مسجّل كمتصفح بعد.",
            ["Reg.Registered"] = "مسجّل كمتصفح.",
            ["Reg.OtherPath"] = "مسجّل من مجلد آخر. سجّله من جديد لاستخدام هذه النسخة.",
            ["Reg.IsDefault"] = "وهو متصفحك الافتراضي: الروابط تفتح نافذة الاختيار.",
            ["Reg.NotDefault"] = "لكنه ليس متصفحك الافتراضي بعد.",
            ["Reg.Register"] = "تسجيل",
            ["Reg.OpenSettings"] = "فتح التطبيقات الافتراضية",
            ["Reg.Unregister"] = "إلغاء التسجيل",
            ["Reg.Steps"] = "Windows يسمح لك وحدك باختيار المتصفح الافتراضي. في التطبيقات الافتراضية اختر Browser Selector ثم \"تعيين كافتراضي\".",
            ["Reg.Done"] = "تم التسجيل. ستُفتح الآن التطبيقات الافتراضية في Windows: اختر Browser Selector ثم \"تعيين كافتراضي\".",
            ["Reg.Removed"] = "تم إلغاء التسجيل. إذا كان متصفحك الافتراضي فاختر متصفحاً آخر في التطبيقات الافتراضية.",
            ["Reg.Failed"] = "تعذّر تغيير التسجيل:\n\n{0}",

            ["Open.Header"] = "عند فتح رابط",
            ["Open.Ask"] = "اسألني في كل مرة",
            ["Open.AskHint"] = "تظهر نافذة الاختيار، وزر Enter يفتح متصفحك الأساسي.",
            ["Open.Auto"] = "افتحه في متصفحي الأساسي",
            ["Open.AutoHint"] = "قواعد المواقع هي الاستثناءات. لتختار بنفسك، اضغط Shift مع فتح الرابط.",
            ["Open.EditRules"] = "تعديل قواعد المواقع",
            ["Open.Main"] = "المتصفح الأساسي",
            ["Gen.Advanced"] = "خيارات متقدمة",
            ["Gen.ShowProfiles"] = "إظهار كل بروفايل للمتصفح في نافذة الاختيار",
            ["Gen.Language"] = "اللغة",
            ["Lang.Auto"] = "مثل Windows",
            ["Gen.Try"] = "جرّبه",
            ["Gen.TryHint"] = "الصق رابطاً. \"افتح نافذة الاختيار\" يسأل دائماً، و\"افتح كرابط\" يتصرّف كما لو ضغطت الرابط فعلاً. لا يتغيّر شيء في Windows.",
            ["Gen.TryButton"] = "افتح نافذة الاختيار",
            ["Gen.TryRules"] = "افتح كرابط",
            ["Gen.RuleMatched"] = "القاعدة {0} ← {1}: فُتح بدون سؤال.",
            ["Gen.RuleMissing"] = "القاعدة {0} تشير إلى متصفح أو بروفايل غير متاح، لذلك ظهرت نافذة الاختيار.",
            ["Gen.NoRule"] = "لا توجد قاعدة تطابق {0}، لذلك ظهرت نافذة الاختيار.",
            ["Gen.MainOpened"] = "لا توجد قاعدة تطابق {0}: فُتح في متصفحك الأساسي، {1}.",
            ["Gen.MainMissing"] = "متصفحك الأساسي (أو البروفايل) غير متاح، لذلك ظهرت نافذة الاختيار.",

            ["Rules.Hint"] = "روابط هذه المواقع تُفتح مباشرةً بدون سؤال. القاعدة تشمل النطاقات الفرعية أيضاً.",
            ["Rules.Site"] = "الموقع",
            ["Rules.OpensWith"] = "يُفتح بـ",
            ["Rules.Add"] = "إضافة قاعدة",
            ["Rules.Remove"] = "حذف",
            ["Rules.Private"] = "خاص",
            ["Rules.Missing"] = "غير مثبّت",
            ["Rules.ProfileMissing"] = "البروفايل غير موجود",
            ["Rules.Empty"] = "لا توجد قواعد بعد. فعّل \"افتح … هكذا دائماً\" في نافذة الاختيار، أو أضف قاعدة هنا.",
            ["Rules.BadDomain"] = "اكتب موقعاً مثل example.com.",
            ["Rules.PrivateBox"] = "خاص",

            ["Browsers.Hint"] = "المتصفحات التي يعرفها Windows. بعد تثبيت متصفح أو حذفه اضغط تحديث.",
            ["Browsers.Profiles"] = "البروفايلات: {0}",
            ["Browsers.Refresh"] = "تحديث",
            ["Browsers.None"] = "لم يُعثر على متصفحات.",

            ["About.Version"] = "الإصدار {0}",
            ["About.Text"] = "يسألك بأي متصفح تفتح كل رابط.",
            ["About.SettingsFile"] = "ملف الإعدادات",
            ["About.License"] = "مجاني ومفتوح المصدر، برخصة MIT.",
        };
    }

    /// <summary>XAML: Text="{local:Tr Picker.Private}".</summary>
    [MarkupExtensionReturnType(typeof(string))]
    public sealed class TrExtension : MarkupExtension
    {
        public TrExtension(string key) { Key = key; }
        public string Key { get; set; }
        public override object ProvideValue(IServiceProvider serviceProvider) => Loc.T(Key);
    }
}
