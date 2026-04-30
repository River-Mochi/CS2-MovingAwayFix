namespace MovingAwayFix
{
    using Colossal.IO.AssetDatabase;
    using Colossal.Localization;
    using Colossal.Logging;
    using CS2Shared.RiverMochi;
    using Game;
    using Game.Modding;
    using Game.SceneFlow;
    using Game.Simulation;
    using System;
    using System.Reflection;

    public sealed class Mod : IMod
    {
        public const string ModName = "Moving Away Fix";
        public const string ModId = "MovingAwayFix";
        public const string ModTag = "[MAF]";
        public const string ShortName = "Moving Away Fix";

        private static bool s_BannerLogged;

        public static readonly string ModVersion =
            Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0";

        public static readonly ILog s_Log =
            LogManager.GetLogger(ModId).SetShowsErrorsInUI(false);

        public static Setting? Setting
        {
            get; private set;
        }

        public void OnLoad(UpdateSystem updateSystem)
        {
            LogUtils.Configure(ModId);
            ShellOpen.Configure(s_Log, ModId, ModTag);

            if (!s_BannerLogged)
            {
                s_BannerLogged = true;
                LogUtils.Info(s_Log, () => $"{ModId} {ModTag} v{ModVersion} OnLoad");
            }

            Setting setting = new Setting(this);
            Setting = setting;

            try
            {
                LocalizationManager? lm = GameManager.instance?.localizationManager;
                if (lm != null)
                {
                    lm.AddSource("en-US", new LocaleEN(setting));
                }
                else
                {
                    LogUtils.WarnOnce(
                        s_Log,
                        key: "LocalizationManagerNull",
                        messageFactory: () => $"{ModTag} LocalizationManager is null; skipping locale registration.");
                }
            }
            catch (Exception ex)
            {
                LogUtils.WarnOnce(
                    s_Log,
                    key: "LocaleRegistrationFailed",
                    messageFactory: () => $"{ModTag} Locale registration failed; Options UI text may be missing.",
                    exception: ex);
            }

            try
            {
                Setting defaults = new Setting(this);
                AssetDatabase.global.LoadSettings(ModId, setting, defaults, userSetting: true);
            }
            catch (Exception ex)
            {
                LogUtils.WarnOnce(
                    s_Log,
                    key: "LoadSettingsFailed",
                    messageFactory: () => $"{ModTag} LoadSettings failed; using defaults.",
                    exception: ex);
            }

            try
            {
                setting.RegisterInOptionsUI();
            }
            catch (Exception ex)
            {
                LogUtils.WarnOnce(
                    s_Log,
                    key: "RegisterOptionsFailed",
                    messageFactory: () => $"{ModTag} RegisterInOptionsUI failed; mod options may be missing.",
                    exception: ex);
            }

            updateSystem.UpdateBefore<MovingAwayFixSystem, ResidentAISystem>(SystemUpdatePhase.GameSimulation);
        }

        public void OnDispose()
        {
            LogUtils.Info(s_Log, () => $"{ModTag} OnDispose");

            try
            {
                Setting?.UnregisterInOptionsUI();
            }
            catch (Exception ex)
            {
                LogUtils.WarnOnce(
                    s_Log,
                    key: "UnregisterOptionsFailed",
                    messageFactory: () => $"{ModTag} UnregisterInOptionsUI failed.",
                    exception: ex);
            }

            Setting = null;
        }
    }
}
