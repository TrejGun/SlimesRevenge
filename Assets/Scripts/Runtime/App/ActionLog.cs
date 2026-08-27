using System;
using System.Collections.Generic;
using System.Text;

namespace SlimesRevenge
{
    public enum ActionLogLinkKind
    {
        None,
        Status,
        Substance,
        Creature,
    }

    /// <summary>Optional render hint for log lines (view maps this to font/color).</summary>
    public enum ActionLogStyle
    {
        Normal,
        Banner,
    }

    public readonly struct StatusLogRef
    {
        public StatusLogRef(
            string label,
            string description,
            string iconKey,
            int remaining,
            bool permanent
        )
        {
            Label = label ?? string.Empty;
            Description = description ?? string.Empty;
            IconKey = iconKey;
            Remaining = remaining;
            Permanent = permanent;
        }

        public string Label { get; }
        public string Description { get; }
        public string IconKey { get; }
        public int Remaining { get; }
        public bool Permanent { get; }

        public static StatusLogRef From(StatusEffect effect)
        {
            if (effect == null)
            {
                return default;
            }

            return new StatusLogRef(
                effect.Label,
                effect.Description,
                IconCatalog.StatusKey(effect.GetType()),
                effect.Remaining,
                effect.Permanent
            );
        }
    }

    /// <summary>
    /// One piece of a log line: plain text or a typed clickable object.
    /// </summary>
    public readonly struct ActionLogPart
    {
        private ActionLogPart(
            string text,
            ActionLogLinkKind kind,
            StatusLogRef? status,
            Substance substance,
            CreatureKind? creature
        )
        {
            Text = text ?? string.Empty;
            Kind = kind;
            StatusSnapshot = status;
            SubstanceSnapshot = substance;
            CreatureKind = creature;
        }

        public string Text { get; }

        public ActionLogLinkKind Kind { get; }

        public StatusLogRef? StatusSnapshot { get; }

        public Substance SubstanceSnapshot { get; }

        public CreatureKind? CreatureKind { get; }

        public bool IsLink => Kind != ActionLogLinkKind.None;

        public static ActionLogPart Plain(string text) =>
            new ActionLogPart(text, ActionLogLinkKind.None, null, null, null);

        public static ActionLogPart Creature(CreatureKind kind) =>
            new ActionLogPart(I18n.Creature(kind), ActionLogLinkKind.Creature, null, null, kind);

        public static ActionLogPart Substance(Substance substance)
        {
            if (substance == null)
            {
                return Plain(string.Empty);
            }

            return new ActionLogPart(
                substance.Label,
                ActionLogLinkKind.Substance,
                null,
                substance.Clone(),
                null
            );
        }

        public static ActionLogPart Status(StatusEffect effect)
        {
            if (effect == null)
            {
                return Plain(string.Empty);
            }

            var snapshot = StatusLogRef.From(effect);
            return new ActionLogPart(
                snapshot.Label,
                ActionLogLinkKind.Status,
                snapshot,
                null,
                null
            );
        }

        public static ActionLogPart Status(StatusLogRef snapshot) =>
            new ActionLogPart(snapshot.Label, ActionLogLinkKind.Status, snapshot, null, null);
    }

    public sealed class ActionLogLine
    {
        private static readonly ActionLogPart[] EmptyParts = Array.Empty<ActionLogPart>();

        public ActionLogLine(string text)
            : this(string.IsNullOrEmpty(text) ? EmptyParts : new[] { ActionLogPart.Plain(text) })
        { }

        public ActionLogLine(string text, StatusLogRef status)
            : this(new[] { ActionLogPart.Status(status) })
        {
            // Marker lines use the status label as the whole text.
            _ = text;
        }

        public ActionLogLine(string text, Substance substance)
            : this(new[] { ActionLogPart.Substance(substance) })
        {
            _ = text;
        }

        public ActionLogLine(
            IReadOnlyList<ActionLogPart> parts,
            ActionLogStyle style = ActionLogStyle.Normal
        )
        {
            Style = style;
            if (parts == null || parts.Count == 0)
            {
                Parts = EmptyParts;
                Text = string.Empty;
                return;
            }

            var copy = new ActionLogPart[parts.Count];
            var sb = new StringBuilder();
            for (var i = 0; i < parts.Count; i++)
            {
                copy[i] = parts[i];
                sb.Append(parts[i].Text);
            }

            Parts = copy;
            Text = sb.ToString();
        }

        public IReadOnlyList<ActionLogPart> Parts { get; }

        public string Text { get; }

        /// <summary>Visual hint for <see cref="ActionLogView"/> (e.g. run-start banner).</summary>
        public ActionLogStyle Style { get; }

        public bool HasLink
        {
            get
            {
                for (var i = 0; i < Parts.Count; i++)
                {
                    if (Parts[i].IsLink)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>First linked part kind, or <see cref="ActionLogLinkKind.None"/>.</summary>
        public ActionLogLinkKind LinkKind
        {
            get
            {
                for (var i = 0; i < Parts.Count; i++)
                {
                    if (Parts[i].IsLink)
                    {
                        return Parts[i].Kind;
                    }
                }

                return ActionLogLinkKind.None;
            }
        }

        public StatusLogRef? Status
        {
            get
            {
                for (var i = 0; i < Parts.Count; i++)
                {
                    if (Parts[i].Kind == ActionLogLinkKind.Status)
                    {
                        return Parts[i].StatusSnapshot;
                    }
                }

                return null;
            }
        }

        public Substance SubstanceSnapshot
        {
            get
            {
                for (var i = 0; i < Parts.Count; i++)
                {
                    if (Parts[i].Kind == ActionLogLinkKind.Substance)
                    {
                        return Parts[i].SubstanceSnapshot;
                    }
                }

                return null;
            }
        }

        public bool HasLinkKind(ActionLogLinkKind kind)
        {
            for (var i = 0; i < Parts.Count; i++)
            {
                if (Parts[i].Kind == kind)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Build a line from an i18n format template (<c>{0}</c>, <c>{1}</c>, …) and typed args.
        /// Literals stay plain; holes become the corresponding parts (already localized).
        /// </summary>
        public static ActionLogLine Format(string template, params ActionLogPart[] args)
        {
            if (string.IsNullOrEmpty(template))
            {
                return new ActionLogLine(string.Empty);
            }

            args ??= Array.Empty<ActionLogPart>();
            var parts = new List<ActionLogPart>(args.Length * 2 + 1);
            var i = 0;
            while (i < template.Length)
            {
                if (template[i] == '{')
                {
                    if (i + 1 < template.Length && template[i + 1] == '{')
                    {
                        AppendPlain(parts, "{");
                        i += 2;
                        continue;
                    }

                    var end = template.IndexOf('}', i + 1);
                    if (
                        end > i
                        && int.TryParse(template.Substring(i + 1, end - i - 1), out var index)
                        && index >= 0
                        && index < args.Length
                    )
                    {
                        parts.Add(args[index]);
                        i = end + 1;
                        continue;
                    }
                }

                if (template[i] == '}' && i + 1 < template.Length && template[i + 1] == '}')
                {
                    AppendPlain(parts, "}");
                    i += 2;
                    continue;
                }

                var nextBrace = template.IndexOf('{', i);
                if (nextBrace < 0)
                {
                    nextBrace = template.Length;
                }

                AppendPlain(parts, template.Substring(i, nextBrace - i));
                i = nextBrace;
            }

            return new ActionLogLine(parts);
        }

        public static ActionLogLine FormatKey(string textKey, params ActionLogPart[] args) =>
            Format(I18n.Get(textKey), args);

        public static ActionLogLine BannerKey(string textKey, params ActionLogPart[] args)
        {
            var line = FormatKey(textKey, args);
            return new ActionLogLine(line.Parts, ActionLogStyle.Banner);
        }

        private static void AppendPlain(List<ActionLogPart> parts, string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            if (parts.Count > 0 && parts[parts.Count - 1].Kind == ActionLogLinkKind.None)
            {
                var prev = parts[parts.Count - 1];
                parts[parts.Count - 1] = ActionLogPart.Plain(prev.Text + text);
                return;
            }

            parts.Add(ActionLogPart.Plain(text));
        }
    }

    public sealed class ActionLogEntry
    {
        private readonly List<ActionLogLine> details = new List<ActionLogLine>();

        public ActionLogEntry(string headline)
            : this(new ActionLogLine(headline)) { }

        public ActionLogEntry(ActionLogLine headline)
        {
            HeadlineLine = headline ?? new ActionLogLine(string.Empty);
        }

        public ActionLogLine HeadlineLine { get; }

        public string Headline => HeadlineLine.Text;

        public IReadOnlyList<ActionLogLine> Details => details;

        internal void AddDetail(ActionLogLine line)
        {
            if (line == null || string.IsNullOrEmpty(line.Text))
            {
                return;
            }

            details.Add(line);
        }
    }

    /// <summary>
    /// Grouped action history: Begin opens a headline, Detail appends to the innermost open group, End pops.
    /// </summary>
    public static class ActionLog
    {
        public const int Capacity = 100;

        private static readonly List<ActionLogEntry> entries = new List<ActionLogEntry>();
        private static readonly List<ActionLogEntry> open = new List<ActionLogEntry>();

        public static event Action Changed;

        /// <summary>Bumps when the entry list is cleared or a leading entry is trimmed — UI must full-rebuild.</summary>
        public static int Generation { get; private set; }

        public static IReadOnlyList<ActionLogEntry> Entries => entries;

        public static bool HasOpenGroup => open.Count > 0;

        public static void Clear()
        {
            entries.Clear();
            open.Clear();
            Generation++;
            Changed?.Invoke();
        }

        public static void Begin(string headline)
        {
            Begin(new ActionLogLine(headline));
        }

        public static void BeginKey(string textKey, params ActionLogPart[] args)
        {
            Begin(ActionLogLine.FormatKey(textKey, args));
        }

        public static void Begin(ActionLogLine headline)
        {
            var entry = new ActionLogEntry(headline);
            entries.Add(entry);
            open.Add(entry);
            while (entries.Count > Capacity)
            {
                var removed = entries[0];
                entries.RemoveAt(0);
                open.Remove(removed);
                Generation++;
            }

            Changed?.Invoke();
        }

        /// <summary>
        /// Run banner: duel is mode-agnostic; campaign includes Hardcore/Softcore.
        /// </summary>
        public static void AnnounceRunStarted(RunKind kind = RunKind.Campaign)
        {
            if (kind == RunKind.Duel)
            {
                Begin(ActionLogLine.BannerKey(TextKey.LogDuelStarted));
            }
            else
            {
                var modeKey =
                    GameSettings.Mode == GameMode.Softcore
                        ? TextKey.ModeSoftcore
                        : TextKey.ModeHardcore;
                Begin(
                    ActionLogLine.BannerKey(
                        TextKey.LogCampaignStarted,
                        ActionLogPart.Plain(I18n.Get(modeKey))
                    )
                );
            }

            End();
        }

        public static void Detail(string line)
        {
            Detail(new ActionLogLine(line));
        }

        public static void DetailKey(string textKey, params ActionLogPart[] args)
        {
            Detail(ActionLogLine.FormatKey(textKey, args));
        }

        public static void Detail(ActionLogLine line)
        {
            if (open.Count == 0 || line == null || string.IsNullOrEmpty(line.Text))
            {
                return;
            }

            open[open.Count - 1].AddDetail(line);
            Changed?.Invoke();
        }

        public static void DetailGains(CreatureKind who, StatusEffect effect)
        {
            if (effect == null)
            {
                return;
            }

            Detail(
                ActionLogLine.FormatKey(
                    TextKey.LogGains,
                    ActionLogPart.Creature(who),
                    ActionLogPart.Status(effect)
                )
            );
        }

        public static void DetailSubstance(Substance substance)
        {
            if (substance == null)
            {
                return;
            }

            Detail(new ActionLogLine(new[] { ActionLogPart.Substance(substance) }));
        }

        public static void DetailDamage(Creature target, int armorStripped, int applied)
        {
            if (target == null)
            {
                return;
            }

            var isSlime = target is Slime;
            var hp = isSlime ? 0 : applied;
            var volume = isSlime ? applied : 0;
            var line = FormatDamageDetail(armorStripped, hp, volume);
            if (line != null)
            {
                Detail(line);
            }
        }

        public static void DetailHeal(int healed)
        {
            if (healed <= 0)
            {
                return;
            }

            DetailKey(TextKey.LogHeals, ActionLogPart.Plain(healed.ToString()));
        }

        public static void End(bool discardIfEmpty = false)
        {
            if (open.Count == 0)
            {
                return;
            }

            var top = open[open.Count - 1];
            open.RemoveAt(open.Count - 1);
            if (discardIfEmpty && top.Details.Count == 0)
            {
                entries.Remove(top);
                Generation++;
                Changed?.Invoke();
            }
        }

        public static string Dump()
        {
            var sb = new StringBuilder();
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (sb.Length > 0)
                {
                    sb.AppendLine();
                }

                sb.Append(entry.Headline);
                for (var d = 0; d < entry.Details.Count; d++)
                {
                    sb.AppendLine();
                    sb.Append("  ");
                    sb.Append(entry.Details[d].Text);
                }
            }

            return sb.ToString();
        }

        /// <summary>Compose a damage detail; omit zero parts. Returns null if nothing to show.</summary>
        public static string FormatDamageDetail(int armorStripped, int hpLost, int volumeLost)
        {
            var parts = new List<string>(3);
            if (armorStripped > 0)
            {
                parts.Add(string.Format(I18n.Get(TextKey.LogArmor), armorStripped));
            }

            if (hpLost > 0)
            {
                parts.Add(string.Format(I18n.Get(TextKey.LogHp), hpLost));
            }

            if (volumeLost > 0)
            {
                parts.Add(string.Format(I18n.Get(TextKey.LogVolume), volumeLost));
            }

            if (parts.Count == 0)
            {
                return null;
            }

            return string.Format(I18n.Get(TextKey.LogDamageTaken), string.Join(", ", parts));
        }
    }
}
