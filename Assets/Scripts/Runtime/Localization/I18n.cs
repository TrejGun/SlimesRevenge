using System;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

namespace SlimesRevenge
{
    public static class I18n
    {
        public const string English = "en";
        public const string Russian = "ru";

        private static StringTable table;

        public static Locale Current { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Boot()
        {
            Use(DefaultCode());
        }

        public static void Use(string code)
        {
            var loaded = Resources.Load<StringTable>(code);
            if (loaded == null)
            {
                throw new InvalidOperationException($"No string table for locale '{code}'.");
            }

            table = loaded;
            if (Current != null)
            {
                UnityEngine.Object.DestroyImmediate(Current);
            }

            Current = Locale.CreateLocale(loaded.LocaleIdentifier);
            Current.hideFlags = HideFlags.HideAndDontSave;
        }

        public static string Get(string key)
        {
            if (table == null)
            {
                Use(DefaultCode());
            }

            var entry = table.GetEntry(key);
            if (entry == null || string.IsNullOrEmpty(entry.LocalizedValue))
            {
                throw new InvalidOperationException($"Missing translation '{key}'.");
            }

            return entry.LocalizedValue;
        }

        private static string DefaultCode()
        {
            return Application.systemLanguage == SystemLanguage.Russian ? Russian : English;
        }
    }
}
