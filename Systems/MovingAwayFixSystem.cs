namespace MovingAwayFix
{
    using CS2Shared.RiverMochi;
    using Game;
    using Game.Agents;
    using Game.Citizens;
    using Game.Common;
    using Game.Creatures;
    using Game.Pathfind;
    using Game.Tools;
    using Unity.Burst.Intrinsics;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Jobs;

    public sealed partial class MovingAwayFixSystem : GameSystemBase
    {
        private EntityQuery m_Query;

        private ComponentLookup<HouseholdMember> m_HouseholdMemberLookup;
        private ComponentLookup<MovingAway> m_MovingAwayLookup;

        public override int GetUpdateInterval(SystemUpdatePhase phase)
        {
            return 1024;
        }

        protected override void OnCreate()
        {
            base.OnCreate();

            m_Query = SystemAPI.QueryBuilder()
                .WithAll<Resident, PathOwner>()
                .WithNone<Deleted, Temp>()
                .Build();

            m_HouseholdMemberLookup = GetComponentLookup<HouseholdMember>(isReadOnly: true);
            m_MovingAwayLookup = GetComponentLookup<MovingAway>(isReadOnly: true);

            RequireForUpdate(m_Query);
            Enabled = false;
        }

        protected override void OnGameLoadingComplete(Colossal.Serialization.Entities.Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);

            bool isRealGame =
                mode == GameMode.Game &&
                (purpose == Colossal.Serialization.Entities.Purpose.NewGame ||
                 purpose == Colossal.Serialization.Entities.Purpose.LoadGame);

            if (!isRealGame)
                return;

            Enabled = true;
        }

        protected override void OnUpdate()
        {
            var setting = Mod.Setting;
            if (setting == null || !setting.EnableMovingAwayFix)
                return;

            m_HouseholdMemberLookup.Update(this);
            m_MovingAwayLookup.Update(this);

            bool trackClearedCount = setting.EnableDebugLogging;
            NativeArray<int> cleared = default;

            if (trackClearedCount)
                cleared = new NativeArray<int>(1, Allocator.TempJob);

            var job = new ClearIgnoreTransportJob
            {
                ResidentType = SystemAPI.GetComponentTypeHandle<Resident>(isReadOnly: false),
                PathOwnerType = SystemAPI.GetComponentTypeHandle<PathOwner>(isReadOnly: false),
                HouseholdMembers = m_HouseholdMemberLookup,
                MovingAways = m_MovingAwayLookup,
                TrackClearedCount = trackClearedCount,
                ClearedCount = cleared,
            };

            JobHandle handle = trackClearedCount
                ? job.Schedule(m_Query, Dependency)
                : job.ScheduleParallel(m_Query, Dependency);

            if (trackClearedCount)
            {
                handle.Complete();

                int clearedCount = cleared[0];
                cleared.Dispose();

                if (clearedCount > 0)
                {
                    LogUtils.Info(
                        Mod.s_Log,
                        () => $"{Mod.ModTag} cleared {clearedCount:N0} IgnoreTransport flags from moving-away residents.");
                }
            }

            Dependency = handle;
        }

        private struct ClearIgnoreTransportJob : IJobChunk
        {
            public ComponentTypeHandle<Resident> ResidentType;
            public ComponentTypeHandle<PathOwner> PathOwnerType;

            [ReadOnly] public ComponentLookup<HouseholdMember> HouseholdMembers;
            [ReadOnly] public ComponentLookup<MovingAway> MovingAways;

            public bool TrackClearedCount;
            public NativeArray<int> ClearedCount;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<Resident> residents = chunk.GetNativeArray(ref ResidentType);
                NativeArray<PathOwner> pathOwners = chunk.GetNativeArray(ref PathOwnerType);

                for (int i = 0; i < residents.Length; i++)
                {
                    Resident resident = residents[i];

                    if ((resident.m_Flags & ResidentFlags.IgnoreTransport) == 0)
                        continue;

                    Entity citizen = resident.m_Citizen;
                    if (citizen == Entity.Null || !HouseholdMembers.HasComponent(citizen))
                        continue;

                    Entity household = HouseholdMembers[citizen].m_Household;
                    if (household == Entity.Null || !MovingAways.HasComponent(household))
                        continue;

                    resident.m_Flags &= ~ResidentFlags.IgnoreTransport;
                    residents[i] = resident;

                    PathOwner pathOwner = pathOwners[i];
                    pathOwner.m_State &= ~PathFlags.Failed;
                    pathOwner.m_State |= PathFlags.Obsolete;
                    pathOwners[i] = pathOwner;

                    if (TrackClearedCount)
                        ClearedCount[0] = ClearedCount[0] + 1;
                }
            }
        }
    }
}
