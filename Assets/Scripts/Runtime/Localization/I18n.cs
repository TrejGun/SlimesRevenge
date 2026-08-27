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

        /// <summary>Like <see cref="Get"/> but never throws — returns <paramref name="fallback"/>.</summary>
        public static string GetOr(string key, string fallback)
        {
            try
            {
                return Get(key);
            }
            catch
            {
                return fallback ?? string.Empty;
            }
        }

        public static string Format(string key, params object[] args)
        {
            return string.Format(Get(key), args);
        }

        public static string FormatOr(string key, string fallbackFormat, params object[] args)
        {
            try
            {
                return string.Format(Get(key), args);
            }
            catch
            {
                return string.Format(fallbackFormat ?? string.Empty, args);
            }
        }

        /// <summary>Localized creature name for logs, menus, and cards.</summary>
        public static string Creature(CreatureKind kind)
        {
            return GetOr(TextKey.ForCreature(kind), kind.ToString());
        }

        private static string DefaultCode()
        {
            return Application.systemLanguage == SystemLanguage.Russian ? Russian : English;
        }
    }
}
