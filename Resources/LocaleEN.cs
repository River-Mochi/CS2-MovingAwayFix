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
                    "Fixes moving-out cims who get stuck walking for miles on highways instead of using nearby public transport.\n" +
                    "Clears IgnoreTransport from moving-away residents so vanilla pathfinding can consider public transport again.\n"

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.StatusMovingAway)), "Moving out" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.StatusMovingAway)),
                    "Current moving-away residents. For the best performance, Status scans only when in the Options menu, never in the city per frame." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.StatusMovingIn)), "Moving in" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.StatusMovingIn)),
                    "Current active moving-in residents. This is informational only." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.StatusMonthly)), "Monthly" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.StatusMonthly)),
                    "Moved-in and moved-away monthly totals from the vanilla Population infoview panel." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.StatusNote)), "Updated" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.StatusNote)),
                    "Timestamp for the cached Options-menu status snapshot." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.StatsToLog)), "Stats to Log" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.StatsToLog)),
                    "Writes current status counts and 5 walking moving-away sample IDs to the mod log for Scene Explorer checks." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OpenLog)), "Open Log" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OpenLog)),
                    "Opens <MovingAwayFix.log> when possible, otherwise opens the <Logs/> folder." },

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
                    "{0} leaving | {1} walking | {2} IgnoreTransport" },

                { MovingAwayStatus.KeyMovingInRow,
                    "{0} active now" },

                { MovingAwayStatus.KeyMonthlyRow,
                    "{0} in/mo | {1} out/mo" },

                { MovingAwayStatus.KeyNoteRow,
                    "{0} | updated {1}" },
            };
        }

        public void Unload()
        {
        }
    }
}
