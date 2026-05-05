// File: Systems/MovingAwayStatus.cs
// Purpose: UI-facing cached status rows and log report trigger for the Options menu.

namespace MovingAwayFix
{
    using CS2Shared.RiverMochi;
    using Game;
    using Game.SceneFlow;
    using Unity.Entities;

    public static class MovingAwayStatus
    {
        internal const string KeyStatusNotLoaded = "MAF_STATUS_NOT_LOADED";
        internal const string KeyNoCity = "MAF_STATUS_NO_CITY";

        internal const string KeyMovingAwayRow = "MAF_STATUS_MOVING_AWAY_ROW";
        internal const string KeyMovingInRow = "MAF_STATUS_MOVING_IN_ROW";
        internal const string KeyMonthlyRow = "MAF_STATUS_MONTHLY_ROW";
        internal const string KeyNoteRow = "MAF_STATUS_NOTE_ROW";

        private const string FallbackStatusNotLoaded = "Status not loaded.";
        private const string FallbackNoCity = "No city loaded, run the city for a bit to get data.";

        private const string FallbackMovingAwayRow =
            "{0} leaving | {1} walking | {2} highway | {3} IgnoreTransport";

        private const string FallbackMovingInRow =
            "{0} active now";

        private const string FallbackMonthlyRow =
            "{0} in/mo | {1} out/mo";

        private const string FallbackNoteRow =
            "{0} | updated {1}";

        public static string MovingAwayRow { get; private set; } = string.Empty;
        public static string MovingInRow { get; private set; } = string.Empty;
        public static string MonthlyRow { get; private set; } = string.Empty;
        public static string NoteRow { get; private set; } = string.Empty;

        private static bool s_HasSnapshot;
        private static uint s_LastSnapshotSimulationFrame = uint.MaxValue;

        // Clears cached rows when loading state changes or status becomes invalid.
        public static void InvalidateCache()
        {
            s_HasSnapshot = false;
            s_LastSnapshotSimulationFrame = uint.MaxValue;

            MovingAwayRow = LocaleUtils.Localize(KeyStatusNotLoaded, FallbackStatusNotLoaded);
            MovingInRow = LocaleUtils.Localize(KeyStatusNotLoaded, FallbackStatusNotLoaded);
            MonthlyRow = LocaleUtils.Localize(KeyStatusNotLoaded, FallbackStatusNotLoaded);
            NoteRow = LocaleUtils.Localize(KeyStatusNotLoaded, FallbackStatusNotLoaded);
        }

        // Called by Options UI string getters. This must stay safe and quiet.
        public static void RefreshForOptionsUi()
        {
            try
            {
                RefreshForOptionsUiCore();
            }
            catch
            {
                SetStatusNotLoaded();
            }
        }

        // Button action: writes a one-time detailed report to the mod log.
        public static void LogDetailedReport()
        {
            try
            {
                World world = World.DefaultGameObjectInjectionWorld;
                if (world == null || !world.IsCreated)
                {
                    LogUtils.Info(Mod.s_Log, () => $"{Mod.ModTag} Status report: no world is available.");
                    return;
                }

                GameManager gm = GameManager.instance;
                if (gm == null || !gm.gameMode.IsGame())
                {
                    LogUtils.Info(Mod.s_Log, () => $"{Mod.ModTag} Status report: no city loaded.");
                    return;
                }

                MovingAwayStatusSystem statusSystem =
                    world.GetOrCreateSystemManaged<MovingAwayStatusSystem>();

                string report = statusSystem.BuildDetailedReport();
                LogUtils.Info(Mod.s_Log, () => report);
            }
            catch
            {
                LogUtils.Info(Mod.s_Log, () => $"{Mod.ModTag} Status report failed.");
            }
        }

        private static void RefreshForOptionsUiCore()
        {
            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
            {
                SetNoCity();
                return;
            }

            // IsGame means a city/game session is loaded. Main-menu Options gets no-city text.
            GameManager gm = GameManager.instance;
            if (gm == null || !gm.gameMode.IsGame())
            {
                SetNoCity();
                return;
            }

            MovingAwayStatusSystem statusSystem =
                world.GetOrCreateSystemManaged<MovingAwayStatusSystem>();

            uint simulationFrame = statusSystem.CurrentSimulationFrame;

            // Options pauses the city. Same simulation frame means cached rows are still current.
            if (s_HasSnapshot && s_LastSnapshotSimulationFrame == simulationFrame)
            {
                return;
            }

            MovingAwayStatusSystem.Snapshot snapshot = statusSystem.BuildSnapshot();
            ApplySnapshot(snapshot);

            s_HasSnapshot = true;
            s_LastSnapshotSimulationFrame = simulationFrame;
        }

        // Converts raw snapshot numbers into compact player-facing rows.
        private static void ApplySnapshot(MovingAwayStatusSystem.Snapshot snapshot)
        {
            MovingAwayRow = LocaleUtils.SafeFormat(
                KeyMovingAwayRow,
                FallbackMovingAwayRow,
                LocaleUtils.FormatN0(snapshot.MovingAwayNow),
                LocaleUtils.FormatN0(snapshot.MovingAwayWalking),
                LocaleUtils.FormatN0(snapshot.MovingAwayHighwayWalking),
                LocaleUtils.FormatN0(snapshot.MovingAwayStillIgnoreTransport));

            MovingInRow = LocaleUtils.SafeFormat(
                KeyMovingInRow,
                FallbackMovingInRow,
                LocaleUtils.FormatN0(snapshot.MovingInNow));

            MonthlyRow = LocaleUtils.SafeFormat(
                KeyMonthlyRow,
                FallbackMonthlyRow,
                LocaleUtils.FormatN0(snapshot.MovedInMonthly),
                LocaleUtils.FormatN0(snapshot.MovedAwayMonthly));

            string fixState = Mod.Setting?.EnableMovingAwayFix == true
                ? "Fix ON"
                : "Fix OFF";

            string updated = snapshot.SnapshotTimeLocal.ToString("HH:mm:ss");

            NoteRow = LocaleUtils.SafeFormat(
                KeyNoteRow,
                FallbackNoteRow,
                fixState,
                updated);
        }

        // Opening mod Options before a city is loaded should not poke city ECS data.
        private static void SetNoCity()
        {
            s_HasSnapshot = false;
            s_LastSnapshotSimulationFrame = uint.MaxValue;

            MovingAwayRow = LocaleUtils.Localize(KeyNoCity, FallbackNoCity);
            MovingInRow = string.Empty;
            MonthlyRow = string.Empty;
            NoteRow = string.Empty;
        }

        // Used only when status refresh fails unexpectedly.
        private static void SetStatusNotLoaded()
        {
            s_HasSnapshot = false;
            s_LastSnapshotSimulationFrame = uint.MaxValue;

            MovingAwayRow = LocaleUtils.Localize(KeyStatusNotLoaded, FallbackStatusNotLoaded);
            MovingInRow = LocaleUtils.Localize(KeyStatusNotLoaded, FallbackStatusNotLoaded);
            MonthlyRow = LocaleUtils.Localize(KeyStatusNotLoaded, FallbackStatusNotLoaded);
            NoteRow = LocaleUtils.Localize(KeyStatusNotLoaded, FallbackStatusNotLoaded);
        }
    }
}
