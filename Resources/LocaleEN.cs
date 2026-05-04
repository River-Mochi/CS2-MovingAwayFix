// File: Resources/LocaleEN.cs
// Purpose: English Options UI strings for Moving Away Fix.

namespace MovingAwayFix
{
    using Colossal;
    using System.Collections.Generic;

    public sealed class LocaleEN : IDictionarySource
    {
        private readonly Setting m_Setting;

        public LocaleEN(Setting setting)
        {
            m_Setting = setting;
        }

        public IEnumerable<KeyValuePair<string, string>> ReadEntries(
            IList<IDictionaryEntryError> errors,
            Dictionary<string, int> indexCounts)
        {
            string title = Mod.ShortName;
            if (!string.IsNullOrEmpty(Mod.ModVersion))
            {
                title = title + " (" + Mod.ModVersion + ")";
            }

            return new Dictionary<string, string>
            {
                { m_Setting.GetSettingsLocaleID(), title },

                { m_Setting.GetOptionTabLocaleID(Setting.ActionsTab), "Actions" },
                { m_Setting.GetOptionTabLocaleID(Setting.AboutTab), "About" },

                { m_Setting.GetOptionGroupLocaleID(Setting.BehaviorGroup), "Fix" },
                { m_Setting.GetOptionGroupLocaleID(Setting.StatusGroup), "Status" },
                { m_Setting.GetOptionGroupLocaleID(Setting.DebugGroup), "Debug / Logging" },
                { m_Setting.GetOptionGroupLocaleID(Setting.AboutInfoGroup), "Info" },
                { m_Setting.GetOptionGroupLocaleID(Setting.AboutLinksGroup), "Support Links" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.EnableMovingAwayFix)), "No Highway Walkers" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.EnableMovingAwayFix)),
                    "Clears IgnoreTransport from moving-away residents so vanilla pathfinding can consider public transport again.\n" +
                    "The mod also marks their path obsolete so vanilla repaths them right away.\n" +
                    "Tip: a direct bus or rail connection to the outside connection helps a lot." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.StatusMovingAway)), "Moving away" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.StatusMovingAway)),
                    "Current moving-away residents. Status scans only when the Options menu asks for this row." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.StatusMovingIn)), "Moving in" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.StatusMovingIn)),
                    "Current active moving-in residents. This is informational only." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.StatusMonthly)), "Population monthly" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.StatusMonthly)),
                    "Moved-in and moved-away monthly totals from the vanilla Population infoview statistics." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.StatusNote)), "Status note" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.StatusNote)),
                    "Timestamp for the cached Options-menu status snapshot." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.EnableDebugLogging)), "Enable debug logging" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.EnableDebugLogging)),
                    "Logs how many moving-away residents were corrected in each update.\n" +
                    "Disable for normal gameplay." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.NameDisplay)), "Mod" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.NameDisplay)), "Display name of this mod." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.VersionDisplay)), "Version" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.VersionDisplay)), "Current mod version." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OpenParadoxMods)), "Paradox Mods" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OpenParadoxMods)), "Opens Paradox Mods website for the author's mods." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OpenDiscord)), "Discord" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OpenDiscord)), "Opens Discord community support in a browser." },

                { MovingAwayStatus.KeyStatusNotLoaded, "Status not loaded." },
                { MovingAwayStatus.KeyNoCity, "No city loaded, run the city for a bit to get data." },

                { MovingAwayStatus.KeyMovingAwayRow,
                    "{0} now | {1} walking | {2} still Ignore Transport" },

                { MovingAwayStatus.KeyMovingInRow,
                    "{0} active now" },

                { MovingAwayStatus.KeyMonthlyRow,
                    "{0} moved in/mo. | {1} moved away/mo." },

                { MovingAwayStatus.KeyNoteRow,
                    "{0} | Options-only scan | updated {1}" },
            };
        }

        public void Unload()
        {
        }
    }
}
