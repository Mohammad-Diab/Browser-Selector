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
            ["Error.BadLink"] = "This doesn't look like a link or a file:\n\n{0}",
            ["Error.Title"] = "Browser Selector",

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

            ["Gen.Quick"] = "Quick choice",
            ["Gen.EnterOpens"] = "Enter opens",
            ["Gen.FirstInList"] = "The first browser in the list",
            ["Gen.ShowProfiles"] = "Show each browser profile in the picker",
            ["Gen.Language"] = "Language",
            ["Lang.Auto"] = "Same as Windows",
            ["Gen.Try"] = "Try it",
            ["Gen.TryHint"] = "Paste a link to see the picker. Nothing changes in Windows.",
            ["Gen.TryButton"] = "Open picker",

            ["Rules.Hint"] = "Links to these sites open right away, without asking. A rule also covers subdomains.",
            ["Rules.Site"] = "Site",
            ["Rules.OpensWith"] = "Opens with",
            ["Rules.Add"] = "Add rule",
            ["Rules.Remove"] = "Remove",
            ["Rules.Private"] = "private",
            ["Rules.Missing"] = "not installed",
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
            ["Picker.Hint"] = "1–9 للاختيار · Enter الافتراضي · Shift خاص · Esc إلغاء",
            ["Picker.Default"] = "Enter",
            ["Picker.NoBrowsers"] = "لم يُعثر على متصفحات. انسخ الرابط وافتحه بنفسك.",
            ["Picker.File"] = "ملف محلي",
            ["Picker.Copied"] = "تم نسخ الرابط",

            ["Error.Launch"] = "تعذّر فتح {0}.\n\n{1}",
            ["Error.BadLink"] = "هذا لا يبدو رابطاً أو ملفاً:\n\n{0}",
            ["Error.Title"] = "Browser Selector",

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

            ["Gen.Quick"] = "الاختيار السريع",
            ["Gen.EnterOpens"] = "زر Enter يفتح",
            ["Gen.FirstInList"] = "أول متصفح في القائمة",
            ["Gen.ShowProfiles"] = "إظهار كل بروفايل للمتصفح في نافذة الاختيار",
            ["Gen.Language"] = "اللغة",
            ["Lang.Auto"] = "مثل Windows",
            ["Gen.Try"] = "جرّبه",
            ["Gen.TryHint"] = "الصق رابطاً لترى نافذة الاختيار. لا يتغيّر شيء في Windows.",
            ["Gen.TryButton"] = "افتح نافذة الاختيار",

            ["Rules.Hint"] = "روابط هذه المواقع تُفتح مباشرةً بدون سؤال. القاعدة تشمل النطاقات الفرعية أيضاً.",
            ["Rules.Site"] = "الموقع",
            ["Rules.OpensWith"] = "يُفتح بـ",
            ["Rules.Add"] = "إضافة قاعدة",
            ["Rules.Remove"] = "حذف",
            ["Rules.Private"] = "خاص",
            ["Rules.Missing"] = "غير مثبّت",
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
