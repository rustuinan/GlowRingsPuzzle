using System.Collections.Generic;
using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class GameLanguage
{
    internal const string PreferenceKey = "GlowRings.Language";
    internal const string Automatic = "auto";
    internal readonly struct Option
    {
        public readonly string Code, Name;
        public readonly SystemLanguage Language;
        public Option(string code, string name, SystemLanguage language)
        { Code = code; Name = name; Language = language; }
    }
    internal static IReadOnlyList<Option> Options { get; } = Array.AsReadOnly(new[]
    {
        new Option(Automatic, "AUTOMATIC", SystemLanguage.Unknown),
        new Option("tr", "Türkçe", SystemLanguage.Turkish),
        new Option("en", "English", SystemLanguage.English),
        new Option("zh", "简体中文", SystemLanguage.ChineseSimplified),
        new Option("es", "Español", SystemLanguage.Spanish),
        new Option("fr", "Français", SystemLanguage.French),
        new Option("pt", "Português (Brasil)", SystemLanguage.Portuguese),
        new Option("id", "Bahasa Indonesia", SystemLanguage.Indonesian),
        new Option("ru", "Русский", SystemLanguage.Russian)
    });

    internal static Option Preference => FindOption(PlayerPrefs.GetString(PreferenceKey, Automatic));
    internal static string PreferenceName => Text(Preference.Name);

    private static Option FindOption(string code)
    {
        foreach (var option in Options)
            if (option.Code == code) return option;
        return Options[0];
    }

    internal static bool SetPreference(string code)
    {
        var option = FindOption(code);
        if (option.Code != code || option.Code == Preference.Code) return false;
        PlayerPrefs.SetString(PreferenceKey, option.Code);
        PlayerPrefs.Save();
        string previous = Code;
        Configure(option.Code == Automatic ? Application.systemLanguage : option.Language);
        return previous != Code;
    }

    private static readonly Dictionary<string, string> Active = new Dictionary<string, string>();
    private const string TurkishCharacters = "ÇĞİÖŞÜçğıöşü";
    private static readonly char[] TurkishLetters = TurkishCharacters.ToCharArray();
    private static TMP_FontAsset turkishUiFont;
    private static TMP_FontAsset addedLanguageFallback;
    public static string Code { get; private set; } = "en";
    public static bool IsTurkish => Code == "tr";
    public static CultureInfo Culture { get; private set; } = CultureInfo.GetCultureInfo("en-US");

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        var option = Preference;
        Configure(option.Code == Automatic ? Application.systemLanguage : option.Language);
        SceneManager.sceneLoaded -= LocalizeScene;
        SceneManager.sceneLoaded += LocalizeScene;
    }

    private static void Configure(SystemLanguage language)
    {
        string locale;
        switch (language)
        {
            case SystemLanguage.Turkish: locale = "tr-TR"; break;
            case SystemLanguage.Spanish: locale = "es-ES"; break;
            case SystemLanguage.French: locale = "fr-FR"; break;
            case SystemLanguage.Portuguese: locale = "pt-BR"; break;
            case SystemLanguage.Indonesian: locale = "id-ID"; break;
            case SystemLanguage.Russian: locale = "ru-RU"; break;
            case SystemLanguage.Chinese:
            case SystemLanguage.ChineseSimplified:
                locale = Resources.Load<TMP_FontAsset>("Ring Match Chinese SDF") != null ? "zh-CN" : "en-US";
                break;
            default: locale = "en-US"; break;
        }
        Culture = CultureInfo.GetCultureInfo(locale);
        Code = Culture.TwoLetterISOLanguageName;
        Active.Clear();
        SetLanguageFallback(null);
        if (Code == "en") return;
        if (IsTurkish)
        {
            foreach (var entry in Translations) Active.Add(entry.Key, entry.Value);
            turkishUiFont = Resources.Load<TMP_FontAsset>("DailyReward Turkish SDF");
            SetLanguageFallback(turkishUiFont);
            return;
        }
        var source = Resources.Load<TextAsset>("GameTranslations");
        if (source == null) return;
        var lines = source.text.Split('\n');
        int column = Array.IndexOf(lines[0].Trim().Split('|'), Code);
        if (column < 1) return;
        for (int i = 1; i < lines.Length; i++)
        {
            var fields = lines[i].TrimEnd('\r').Split('|');
            if (fields.Length > column && fields[column].Length > 0)
                Active[fields[0].Replace("\\n", "\n")] = fields[column].Replace("\\n", "\n");
        }
        string fontPath = Code == "zh" ? "Ring Match Chinese SDF" :
            Code == "ru" ? "Ring Match Cyrillic SDF" : "Fonts & Materials/LiberationSans SDF - Fallback";
        SetLanguageFallback(Resources.Load<TMP_FontAsset>(fontPath));
    }

    private static void SetLanguageFallback(TMP_FontAsset font)
    {
        var fallbacks = TMP_Settings.fallbackFontAssets;
        if (fallbacks == null) return;
        if (addedLanguageFallback != null) fallbacks.Remove(addedLanguageFallback);
        addedLanguageFallback = null;
        if (font == null || fallbacks.Contains(font)) return;
        fallbacks.Add(font);
        addedLanguageFallback = font;
    }

    internal static TMP_FontAsset UiFont(TMP_FontAsset preferred)
    {
        if (!IsTurkish || turkishUiFont == null) return preferred;
        if (preferred == null) return turkishUiFont;
        foreach (char letter in TurkishLetters)
            if (!preferred.HasCharacter(letter)) return turkishUiFont;
        return preferred;
    }

    internal static void ApplyUiFont(TMP_Text label)
    {
        var font = UiFont(label.font);
        if (font == label.font) return;
        label.font = font;
        label.fontStyle &= ~FontStyles.Bold;
    }

    public static string Choose(string english, string turkish) => IsTurkish ? turkish : Text(english);

    public static string Format(string english, string turkish, params object[] values) =>
        string.Format(Culture, Choose(english, turkish), values);

    public static string Text(string english)
    {
        if (string.IsNullOrEmpty(english)) return english;
        string key = english.Trim();
        if (!Active.TryGetValue(key, out var translated)) return english;
        int start = english.IndexOf(key, StringComparison.Ordinal);
        return english.Substring(0, start) + translated + english.Substring(start + key.Length);
    }

    private static void LocalizeScene(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "MainMenu" && scene.name != "Game") return;
        foreach (var root in scene.GetRootGameObjects())
        foreach (var label in root.GetComponentsInChildren<TMP_Text>(true))
        {
            string translation = Text(label.text);
            if (IsTurkish && (translation != label.text || translation?.IndexOfAny(TurkishLetters) >= 0))
                ApplyUiFont(label);
            if (translation == label.text) continue;
            if (!label.enableAutoSizing)
            {
                label.fontSizeMax = label.fontSize;
                label.fontSizeMin = label.fontSize * .72f;
                label.enableAutoSizing = true;
            }
            label.text = translation;
        }
    }

    private static readonly Dictionary<string, string> Translations = new Dictionary<string, string>
    {
        { "PLAY", "OYNA" }, { "SETTINGS", "AYARLAR" }, { "MUSIC", "MÜZİK" }, { "SOUND", "SES" },
        { "VIBRATION", "TİTREŞİM" },
        { "LANGUAGE", "DİL" }, { "AUTOMATIC", "OTOMATİK" }, { "SYSTEM LANGUAGE", "CİHAZ DİLİ" },
        { "HIGH SCORE", "REKOR" }, { "HIGHSCORE MODE", "REKOR MODU" }, { "SCORE", "PUAN" },
        { "PUZZLE MODE", "BULMACA MODU" }, { "Solve handcrafted ring challenges.", "Özel halka bulmacalarını çöz." },
        { "TRY AGAIN", "TEKRAR DENE" }, { "PUZZLE COMPLETE", "BULMACA TAMAMLANDI" },
        { "DAILY\nREWARD", "GÜNLÜK\nÖDÜL" }, { "DAILY\nSPIN", "GÜNLÜK\nÇARK" }, { "DAILY\nTASKS", "GÜNLÜK\nGÖREV" },
        { "SHOP", "MAĞAZA" }, { "CLAIM", "AL" }, { "CLAIMED", "ALINDI" }, { "DONE", "TAMAM" },
        { "FINISH", "BİTİR" }, { "FREE", "ÜCRETSİZ" }, { "BACK", "GERİ" },
        { "NO MOVES", "HAMLE KALMADI" }, { "GAME OVER", "OYUN BİTTİ" }, { "CONTINUE", "DEVAM ET" },
        { "<  MAIN MENU", "<  ANA MENÜ" }, { "END RUN", "OYUNU BİTİR" },
        { "CHANGE PIECE", "HALKAYI DEĞİŞTİR" }, { "CELL BOMB", "HÜCRE BOMBASI" },
        { "Get the next ring", "Sıradaki halkayı al" }, { "Clear one cell", "Bir hücreyi temizle" },
        { "Use a booster to keep playing.", "Devam etmek için bir güçlendirici kullan." },
        { "TAP A CELL TO CLEAR\nTap bomb again to cancel", "TEMİZLEMEK İÇİN HÜCREYE DOKUN\nİptal için bombaya tekrar dokun" },
        { "DAILY TASKS", "GÜNLÜK GÖREVLER" }, { "IN PROGRESS", "DEVAM EDİYOR" },
        { "Place 15 pieces", "15 parça yerleştir" }, { "Clear 18 rings in matches", "18 halkayı eşleştirerek temizle" },
        { "Earn 600 points", "600 puan kazan" }, { "Progress carries across games.", "İlerlemen maçlar arasında korunur." },
        { "YOUR RECORDS", "REKORLARIN" }, { "STATISTICS", "İSTATİSTİKLER" }, { "ACHIEVEMENTS", "BAŞARIMLAR" },
        { "LIFETIME STATISTICS", "TÜM ZAMANLAR" }, { "RUNS PLAYED", "OYNANAN MAÇ" }, { "PIECES PLACED", "YERLEŞEN PARÇA" },
        { "MATCHING MOVES", "EŞLEŞEN HAMLE" }, { "RINGS CLEARED", "TEMİZLENEN HALKA" }, { "TOTAL POINTS", "TOPLAM PUAN" },
        { "BEST COMBO", "EN İYİ KOMBO" }, { "ALL CLEARS", "TAM TEMİZLEME" }, { "MULTI-MATCH MOVES", "ÇOKLU EŞLEŞME" },
        { "MATCH RATE", "EŞLEŞME ORANI" }, { "BOMBS USED", "KULLANILAN BOMBA" }, { "PIECE CHANGES", "PARÇA DEĞİŞİMİ" },
        { "UNLOCKED", "AÇILDI" }, { "FIRST MOVE", "İLK HAMLE" }, { "Place a piece", "Bir parça yerleştir" },
        { "GETTING STARTED", "BAŞLANGIÇ" }, { "Play 10 runs", "10 maç oyna" },
        { "RING COLLECTOR", "HALKA KOLEKSİYONCUSU" }, { "Clear 100 rings in matches", "100 halkayı eşleştirerek temizle" },
        { "CLEAN SWEEP", "TERTEMİZ" }, { "Clear the entire board in a match", "Eşleşmeyle tahtayı tamamen temizle" },
        { "DOUBLE TAKE", "ÇİFTE HAMLE" }, { "Trigger 5 multi-match moves", "5 çoklu eşleşme yap" },
        { "COMBO MASTER", "KOMBO USTASI" }, { "Reach a x5 combo", "x5 kombo yap" },
        { "HIGH FLYER", "ZİRVEYE DOĞRU" }, { "Reach a score of 2,000", "Bir maçta 2.000 puana ulaş" },
        { "RING VETERAN", "HALKA USTASI" }, { "Place 250 pieces", "250 parça yerleştir" },
        { "DAILY SPIN", "GÜNLÜK ÇARK" }, { "Unlimited free spins - test mode", "Sınırsız ücretsiz çevirme · Test modu" },
        { "SPIN & WIN", "ÇEVİR VE KAZAN" }, { "WIN COINS", "COIN KAZAN" },
        { "Spend your rewards in the shop", "Ödüllerini mağazada harca" },
        { "LAST REWARD", "SON ÖDÜL" }, { "Your reward is in your balance", "Ödülün bakiyende" },
        { "SPINNING", "DÖNÜYOR" }, { "SPINNING...", "DÖNÜYOR..." }, { "RESUMING SPIN", "ÇARK DEVAM EDİYOR" },
        { "GOOD LUCK!", "BOL ŞANS!" }, { "The pointer decides your reward", "Ödülünü işaretçi belirler" },
        { "YOU WON!", "KAZANDIN!" }, { "Added to your balance", "Bakiyene eklendi" },
        { "REWARD PENDING", "ÖDÜL BEKLİYOR" }, { "Reopen to finish collecting your reward", "Ödülünü almak için yeniden aç" },
        { "SPIN", "ÇEVİR" }, { "SPIN USED", "HAK KULLANILDI" }, { "Every spin wins coins", "Her çevirme coin kazandırır" },
        { "ALL CLEAR!", "TAHTA TEMİZ!" }, { "DOUBLE MATCH!", "İKİLİ EŞLEŞME!" }, { "LEGENDARY!", "EFSANEVİ!" },
        { "AMAZING!", "İNANILMAZ!" }, { "SUPER!", "SÜPER!" }, { "GREAT!", "HARİKA!" }, { "GOOD!", "GÜZEL!" }
    };
}
