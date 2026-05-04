// File: Settings/Setting.cs

namespace MovingAwayFix
{
    using Colossal.IO.AssetDatabase;
    using Game.Modding;
    using Game.Settings;
    using Game.UI;
    using System;
    using Unity.Entities;
    using UnityEngine;

    [FileLocation("ModsSettings/MovingAwayFix/MovingAwayFix")]
    [SettingsUITabOrder(ActionsTab, AboutTab)]
    [SettingsUIGroupOrder(
        BehaviorGroup,
        StatusGroup,
        DebugGroup,
        AboutInfoGroup,
        AboutLinksGroup
    )]
    [SettingsUIShowGroupName(
        BehaviorGroup,
        StatusGroup,
        DebugGroup,
        AboutInfoGroup,
        AboutLinksGroup
    )]
    public sealed class Setting : ModSetting
    {
        public const string ActionsTab = "Actions";
        public const string AboutTab = "About";

        public const string BehaviorGroup = "Behavior";
        public const string StatusGroup = "Status";
        public const string DebugGroup = "Debug";
        public const string AboutInfoGroup = "Info";
        public const string AboutLinksGroup = "Support Links";

        private const string UrlParadox =
            "https://mods.paradoxplaza.com/authors/River-mochi/cities_skylines_2?games=cities_skylines_2&orderBy=desc&sortBy=best&time=alltime";

        private const string UrlDiscord = "https://discord.gg/HTav7ARPs2";

        public Setting(IMod mod) : base(mod)
        {
            SetDefaults();
        }

        [SettingsUISection(ActionsTab, BehaviorGroup)]
        public bool EnableMovingAwayFix
        {
            get; set;
        }

        [SettingsUISection(ActionsTab, StatusGroup)]
        public string StatusMovingAway
        {
            get
            {
                RefreshStatusSafe();
                return MovingAwayStatus.MovingAwayRow;
            }
        }

        [SettingsUISection(ActionsTab, StatusGroup)]
        public string StatusMovingIn
        {
            get
            {
                RefreshStatusSafe();
                return MovingAwayStatus.MovingInRow;
            }
        }

        [SettingsUISection(ActionsTab, StatusGroup)]
        public string StatusMonthly
        {
            get
            {
                RefreshStatusSafe();
                return MovingAwayStatus.MonthlyRow;
            }
        }

        [SettingsUISection(ActionsTab, StatusGroup)]
        public string StatusNote
        {
            get
            {
                RefreshStatusSafe();
                return MovingAwayStatus.NoteRow;
            }
        }

        [SettingsUISection(AboutTab, DebugGroup)]
        public bool EnableDebugLogging
        {
            get; set;
        }

        [SettingsUISection(AboutTab, AboutInfoGroup)]
        public string NameDisplay => Mod.ModName;

        [SettingsUISection(AboutTab, AboutInfoGroup)]
        public string VersionDisplay => Mod.ModVersion;

        [SettingsUIButtonGroup(AboutLinksGroup)]
        [SettingsUIButton]
        [SettingsUISection(AboutTab, AboutLinksGroup)]
        public bool OpenParadoxMods
        {
            set
            {
                if (!value)
                    return;

                try
                {
                    Application.OpenURL(UrlParadox);
                }
                catch (Exception)
                {
                }
            }
        }

        [SettingsUIButtonGroup(AboutLinksGroup)]
        [SettingsUIButton]
        [SettingsUISection(AboutTab, AboutLinksGroup)]
        public bool OpenDiscord
        {
            set
            {
                if (!value)
                    return;

                try
                {
                    Application.OpenURL(UrlDiscord);
                }
                catch (Exception)
                {
                }
            }
        }

        public override void SetDefaults()
        {
            EnableMovingAwayFix = true;
            EnableDebugLogging = false;
        }

        private static void RefreshStatusSafe()
        {
            try
            {
                MovingAwayStatus.RefreshForOptionsUi();
            }
            catch
            {
            }
        }
    }
}
