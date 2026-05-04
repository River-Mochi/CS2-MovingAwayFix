// File: Systems/MovingAwayStatus.cs
// Purpose: UI-facing cached status rows for the Options menu.

namespace MovingAwayFix
{
    using CS2Shared.RiverMochi;
    using Game;
    using Game.SceneFlow;
    using System;
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
            "Moving away: {0} now | {1} walking | {2} still IgnoreTransport";

        private const string FallbackMovingInRow =
            "Moving in: {0} active now";

        private const string FallbackMonthlyRow =
            "Population infoview: {0} moved in/month | {1} moved away/month";

        private const string FallbackNoteRow =
            "{0} | updated {1} | Options-only scan";

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

        private static void RefreshForOptionsUiCore()
        {
            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
            {
                SetNoCity();
                return;
            }

            // IsGame means a city/game session is loaded. Main menu gets the no-city message.
            GameManager gm = GameManager.instance;
            if (gm == null || !gm.gameMode.IsGame())
            {
                SetNoCity();
                return;
            }

            MovingAwayStatusSystem statusSystem =
                world.GetOrCreateSystemManaged<MovingAwayStatusSystem>();

            uint simulationFrame = statusSystem.CurrentSimulationFrame;

            // Options menu pauses the city, so the sim frame should not advance there.
            // Same frame = reuse cached rows and avoid repeated status scans.
            if (s_HasSnapshot && s_LastSnapshotSimulationFrame == simulationFrame)
            {
#if DEBUG
                DebugLogStatusCache($"reuse frame={simulationFrame}");
#endif
                return;
            }

            MovingAwayStatusSystem.Snapshot snapshot = statusSystem.BuildSnapshot();
            ApplySnapshot(snapshot);

            s_HasSnapshot = true;
            s_LastSnapshotSimulationFrame = simulationFrame;

#if DEBUG
            DebugLogStatusCache(
                $"build frame={simulationFrame}, movingAway={snapshot.MovingAwayNow:N0}, walking={snapshot.MovingAwayWalking:N0}, stillIgnoreTransport={snapshot.MovingAwayStillIgnoreTransport:N0}");
#endif
        }

        // Converts the raw snapshot numbers into localized player-facing rows.
        private static void ApplySnapshot(MovingAwayStatusSystem.Snapshot snapshot)
        {
            MovingAwayRow = LocaleUtils.SafeFormat(
                KeyMovingAwayRow,
                FallbackMovingAwayRow,
                LocaleUtils.FormatN0(snapshot.MovingAwayNow),
                LocaleUtils.FormatN0(snapshot.MovingAwayWalking),
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

        // Used before a city exists
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

#if DEBUG
        // Temporary DEBUG-only proof that Options status scans once per sim frame.
        private static void DebugLogStatusCache(string message)
        {
            try
            {
                LogUtils.Info(Mod.s_Log, () => $"{Mod.ModTag} Status cache: {message}");
            }
            catch
            {
            }
        }
#endif
    }
}
