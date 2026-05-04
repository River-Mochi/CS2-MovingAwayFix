// File: Systems/MovingAwayStatusSystem.cs
// Purpose: Read-only Options menu snapshot for Moving Away Fix status.

namespace MovingAwayFix
{
    using Game;
    using Game.Agents;
    using Game.Citizens;
    using Game.City;
    using Game.Common;
    using Game.Creatures;
    using Game.Objects;
    using Game.Pathfind;
    using Game.Simulation;
    using Game.Tools;
    using System;
    using Unity.Collections;
    using Unity.Entities;

    public sealed partial class MovingAwayStatusSystem : GameSystemBase
    {
        public readonly struct Snapshot
        {
            public readonly long MovingAwayNow;
            public readonly long MovingAwayWalking;
            public readonly long MovingAwayStillIgnoreTransport;
            public readonly long MovingInNow;
            public readonly long MovedInMonthly;
            public readonly long MovedAwayMonthly;
            public readonly uint SimulationFrame;
            public readonly DateTime SnapshotTimeLocal;

            public Snapshot(
                long movingAwayNow,
                long movingAwayWalking,
                long movingAwayStillIgnoreTransport,
                long movingInNow,
                long movedInMonthly,
                long movedAwayMonthly,
                uint simulationFrame,
                DateTime snapshotTimeLocal)
            {
                MovingAwayNow = movingAwayNow;
                MovingAwayWalking = movingAwayWalking;
                MovingAwayStillIgnoreTransport = movingAwayStillIgnoreTransport;
                MovingInNow = movingInNow;
                MovedInMonthly = movedInMonthly;
                MovedAwayMonthly = movedAwayMonthly;
                SimulationFrame = simulationFrame;
                SnapshotTimeLocal = snapshotTimeLocal;
            }
        }

        private CityStatisticsSystem m_CityStatisticsSystem = null!;
        private SimulationSystem m_SimulationSystem = null!;
        private EntityQuery m_ResidentQuery;

        public uint CurrentSimulationFrame => m_SimulationSystem.frameIndex;

        protected override void OnCreate()
        {
            base.OnCreate();

            m_CityStatisticsSystem = World.GetOrCreateSystemManaged<CityStatisticsSystem>();
            m_SimulationSystem = World.GetOrCreateSystemManaged<SimulationSystem>();

            m_ResidentQuery = SystemAPI.QueryBuilder()
                .WithAll<Resident, Human, PathOwner>()
                .WithNone<Deleted, Destroyed, Temp>()
                .WithNone<Unspawned>()
                .Build();

            Enabled = false;
        }

        protected override void OnUpdate()
        {
        }

        public Snapshot BuildSnapshot()
        {
            EntityTypeHandle entityType = GetEntityTypeHandle();
            ComponentTypeHandle<Resident> residentType =
                SystemAPI.GetComponentTypeHandle<Resident>(isReadOnly: true);

            ComponentLookup<HouseholdMember> householdMembers =
                SystemAPI.GetComponentLookup<HouseholdMember>(isReadOnly: true);

            ComponentLookup<MovingAway> movingAways =
                SystemAPI.GetComponentLookup<MovingAway>(isReadOnly: true);

            ComponentLookup<CurrentVehicle> currentVehicles =
                SystemAPI.GetComponentLookup<CurrentVehicle>(isReadOnly: true);

            ComponentLookup<Household> households =
                SystemAPI.GetComponentLookup<Household>(isReadOnly: true);

            ComponentLookup<TouristHousehold> touristHouseholds =
                SystemAPI.GetComponentLookup<TouristHousehold>(isReadOnly: true);

            ComponentLookup<TravelPurpose> travelPurposes =
                SystemAPI.GetComponentLookup<TravelPurpose>(isReadOnly: true);

            ComponentLookup<CurrentBuilding> currentBuildings =
                SystemAPI.GetComponentLookup<CurrentBuilding>(isReadOnly: true);

            long movingAwayNow = 0;
            long movingAwayWalking = 0;
            long movingAwayStillIgnoreTransport = 0;
            long movingInNow = 0;

            using (NativeArray<ArchetypeChunk> chunks = m_ResidentQuery.ToArchetypeChunkArray(Allocator.Temp))
            {
                for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
                {
                    ArchetypeChunk chunk = chunks[chunkIndex];

                    NativeArray<Entity> entities = chunk.GetNativeArray(entityType);
                    NativeArray<Resident> residents = chunk.GetNativeArray(ref residentType);

                    for (int i = 0; i < residents.Length; i++)
                    {
                        Entity creature = entities[i];
                        Resident resident = residents[i];

                        Entity citizen = resident.m_Citizen;
                        if (citizen == Entity.Null || !householdMembers.HasComponent(citizen))
                        {
                            continue;
                        }

                        Entity household = householdMembers[citizen].m_Household;
                        if (household == Entity.Null)
                        {
                            continue;
                        }

                        if (movingAways.HasComponent(household))
                        {
                            movingAwayNow++;

                            if (IsWalking(creature, currentVehicles))
                            {
                                movingAwayWalking++;
                            }

                            if ((resident.m_Flags & ResidentFlags.IgnoreTransport) != 0)
                            {
                                movingAwayStillIgnoreTransport++;
                            }

                            continue;
                        }

                        if (IsMovingInNow(citizen, household, households, touristHouseholds, travelPurposes, currentBuildings))
                        {
                            movingInNow++;
                        }
                    }
                }
            }

            long movedInMonthly = m_CityStatisticsSystem.GetStatisticValue(StatisticType.CitizensMovedIn);
            long movedAwayMonthly = m_CityStatisticsSystem.GetStatisticValue(StatisticType.CitizensMovedAway);

            return new Snapshot(
                movingAwayNow: movingAwayNow,
                movingAwayWalking: movingAwayWalking,
                movingAwayStillIgnoreTransport: movingAwayStillIgnoreTransport,
                movingInNow: movingInNow,
                movedInMonthly: movedInMonthly,
                movedAwayMonthly: movedAwayMonthly,
                simulationFrame: m_SimulationSystem.frameIndex,
                snapshotTimeLocal: DateTime.Now);
        }

        private static bool IsWalking(Entity creature, ComponentLookup<CurrentVehicle> currentVehicles)
        {
            if (!currentVehicles.HasComponent(creature))
            {
                return true;
            }

            CurrentVehicle currentVehicle = currentVehicles[creature];

            return currentVehicle.m_Vehicle == Entity.Null ||
                   currentVehicle.m_Vehicle == creature;
        }

        private static bool IsMovingInNow(
            Entity citizen,
            Entity household,
            ComponentLookup<Household> households,
            ComponentLookup<TouristHousehold> touristHouseholds,
            ComponentLookup<TravelPurpose> travelPurposes,
            ComponentLookup<CurrentBuilding> currentBuildings)
        {
            if (!households.HasComponent(household))
            {
                return false;
            }

            if (touristHouseholds.HasComponent(household))
            {
                return false;
            }

            Household householdData = households[household];
            if ((householdData.m_Flags & HouseholdFlags.MovedIn) != 0)
            {
                return false;
            }

            if (!travelPurposes.HasComponent(citizen))
            {
                return false;
            }

            if (currentBuildings.HasComponent(citizen))
            {
                return false;
            }

            return true;
        }
    }
}
