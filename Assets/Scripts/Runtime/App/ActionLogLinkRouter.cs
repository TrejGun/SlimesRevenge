namespace SlimesRevenge
{
    /// <summary>
    /// Opens <see cref="PopupHost"/> cards from action-log link parts.
    /// </summary>
    public static class ActionLogLinkRouter
    {
        /// <summary>
        /// Invoked when <see cref="Open"/> does not handle the part.
        /// Args: part, host (always non-null after <see cref="PopupHost.Ensure"/>).
        /// </summary>
        public static event System.Action<ActionLogPart, PopupHost> CustomOpen;

        public static void Open(ActionLogPart part)
        {
            if (!part.IsLink)
            {
                return;
            }

            var host = PopupHost.Ensure();
            switch (part.Kind)
            {
                case ActionLogLinkKind.Status when part.StatusSnapshot != null:
                    host.Push(new StatusCard(part.StatusSnapshot.Value));
                    return;
                case ActionLogLinkKind.Substance when part.SubstanceSnapshot != null:
                    host.Push(new SubstanceCard(part.SubstanceSnapshot));
                    return;
                case ActionLogLinkKind.Creature when part.CreatureKind != null:
                    host.Push(new CreatureKindCard(part.CreatureKind.Value));
                    return;
                default:
                    CustomOpen?.Invoke(part, host);
                    break;
            }
        }

        /// <summary>Opens the first linked part of a line (compat for tests / single-link rows).</summary>
        public static void Open(ActionLogLine line)
        {
            if (line == null)
            {
                return;
            }

            for (var i = 0; i < line.Parts.Count; i++)
            {
                if (line.Parts[i].IsLink)
                {
                    Open(line.Parts[i]);
                    return;
                }
            }
        }
    }
}
