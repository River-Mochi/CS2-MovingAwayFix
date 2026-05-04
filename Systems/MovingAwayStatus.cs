// File: Systems/MovingAwayStatus.cs
// UI-facing cached status lines for the Options UI.

namespace MovingAwayFix
{
    using Game;
    using Game.SceneFlow;
    using System;
    using Unity.Entities;
    using UnityEngine;

    public static class MovingAwayStatus
    {
        private const int NewOptionsVisitFrameGap = 30;

        private const string StatusNotLoaded = "Status not loaded.";
        private const string NoCity = "No city loaded, run the city for a bit to get data.";
        private const string Disabled = "Moving-away fix is OFF.";

        public static string MovingAwayRow { get; private set; } = StatusNotLoaded;
        public static string MovingInRow { get; private set; } = StatusNotLoaded;
        public static string MonthlyRow { get; private set; } = StatusNotLoaded;
        public static string NoteRow { get; private set; } = StatusNotLoaded;

        private static int s_LastOptionsUiFrame = -100000;
        private static bool s_WasInGame;
        private static bool s_HasSnapshotThisOptionsVisit;

        public static void MarkDirty()
        {
            s_HasSnapshotThisOptionsVisit = false;
        }

        public static void RefreshForOptionsUi()
        {
            int frame = Time.frameCount;
            bool newOptionsVisit = s_LastOptionsUiFrame < 0 || frame - s_LastOptionsUiFrame > NewOptionsVisitFrameGap;
            s_LastOptionsUiFrame = frame;

            if (!newOptionsVisit && s_HasSnapshotThisOptionsVisit)
            {
                return;
            }

            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
            {
                SetNoCity();
                return;
            }

            GameManager gm = GameManager.instance;
            bool isGame = gm != null && gm.gameMode.IsGame();

            if (isGame != s_WasInGame)
            {
                s_WasInGame = isGame;
                s_HasSnapshotThisOptionsVisit = false;
            }

            if (!isGame)
            {
                SetNoCity();
                s_HasSnapshotThisOptionsVisit = true;
                return;
            }

            Setting? setting = Mod.Setting;
            if (setting == null || !setting.EnableMovingAwayFix)
            {
                SetDisabled();
                s_HasSnapshotThisOptionsVisit = true;
                return;
            }

            BuildSnapshotSafe(world);
            s_HasSnapshotThisOptionsVisit = true;
        }

        private static void BuildSnapshotSafe(World world)
        {
            try
            {
                MovingAwayStatusSystem system = world.GetOrCreateSystemManaged<MovingAwayStatusSystem>();
                MovingAwayStatusSystem.Snapshot snapshot = system.BuildSnapshot();

                string updated = snapshot.SnapshotTimeLocal.ToString("HH:mm:ss");

                MovingAwayRow =
                    $"Moving away: {FormatN0(snapshot.MovingAwayCitizenTotal)} citizens | " +
                    $"{FormatN0(snapshot.MovingAwayCreatureTotal)} active/walking | " +
                    $"{FormatN0(snapshot.MovingAwayIgnoreTransportNow)} still IgnoreTransport";

                MovingInRow =
                    $"Moving in now: {FormatN0(snapshot.MovingInCreatureTotal)} active residents";

                MonthlyRow =
                    $"This month: {FormatN0(snapshot.MovedInThisMonth)} moved in | " +
                    $"{FormatN0(snapshot.MovedAwayThisMonth)} moved away";

                NoteRow =
                    $"Updated {updated}. Status scan runs only from the Options menu.";
            }
            catch
            {
                MovingAwayRow = StatusNotLoaded;
                MovingInRow = StatusNotLoaded;
                MonthlyRow = StatusNotLoaded;
                NoteRow = StatusNotLoaded;
            }
        }

        private static void SetNoCity()
        {
            MovingAwayRow = NoCity;
            MovingInRow = string.Empty;
            MonthlyRow = string.Empty;
            NoteRow = string.Empty;
        }

        private static void SetDisabled()
        {
            MovingAwayRow = Disabled;
            MovingInRow = string.Empty;
            MonthlyRow = string.Empty;
            NoteRow = "Turn ON the fix to refresh live moving-away status.";
        }

        private static string FormatN0(long value)
        {
            return value.ToString("N0");
        }
    }
}
