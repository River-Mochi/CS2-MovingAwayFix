// File: Systems/MovingAwayStatusSystem.cs
// Snapshot counts for the Options UI. This system does not run during simulation updates.

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
            public readonly long MovingAwayCitizenTotal;
            public readonly long MovingAwayCreatureTotal;
            public readonly long MovingAwayIgnoreTransportNow;
            public readonly long MovingInCreatureTotal;
            public readonly long MovedInThisMonth;
            public readonly long MovedAwayThisMonth;
            public readonly DateTime SnapshotTimeLocal;

            public Snapshot(
                long movingAwayCitizenTotal,
                long movingAwayCreatureTotal,
                long movingAwayIgnoreTransportNow,
                long movingInCreatureTotal,
                long movedInThisMonth,
                long movedAwayThisMonth,
                DateTime snapshotTimeLocal)
            {
                MovingAwayCitizenTotal = movingAwayCitizenTotal;
                MovingAwayCreatureTotal = movingAwayCreatureTotal;
                MovingAwayIgnoreTransportNow = movingAwayIgnoreTransportNow;
                MovingInCreatureTotal = movingInCreatureTotal;
                MovedInThisMonth = movedInThisMonth;
                MovedAwayThisMonth = movedAwayThisMonth;
                SnapshotTimeLocal = snapshotTimeLocal;
            }
        }

        private CityStatisticsSystem m_CityStatisticsSystem = null!;
        private EntityQuery m_HouseholdMemberQuery;
        private EntityQuery m_CreatureResidentQuery;

        protected override void OnCreate()
        {
            base.OnCreate();

            m_CityStatisticsSystem = World.GetOrCreateSystemManaged<CityStatisticsSystem>();

            m_HouseholdMemberQuery = GetEntityQuery(
                ComponentType.ReadOnly<HouseholdMember>(),
                ComponentType.Exclude<Deleted>(),
                ComponentType.Exclude<Temp>());

            m_CreatureResidentQuery = GetEntityQuery(
                ComponentType.ReadOnly<Game.Creatures.Resident>(),
                ComponentType.ReadOnly<PathOwner>(),
                ComponentType.ReadOnly<PathElement>(),
                ComponentType.Exclude<Deleted>(),
                ComponentType.Exclude<Destroyed>(),
                ComponentType.Exclude<Temp>(),
                ComponentType.Exclude<Unspawned>());

            Enabled = false;
        }

        protected override void OnUpdate()
        {
        }

        public Snapshot BuildSnapshot()
        {
            ComponentLookup<HouseholdMember> householdMembers = GetComponentLookup<HouseholdMember>(isReadOnly: true);
            ComponentLookup<MovingAway> movingAways = GetComponentLookup<MovingAway>(isReadOnly: true);
            ComponentLookup<Household> households = GetComponentLookup<Household>(isReadOnly: true);
            ComponentLookup<TravelPurpose> travelPurposes = GetComponentLookup<TravelPurpose>(isReadOnly: true);
            ComponentLookup<CurrentBuilding> currentBuildings = GetComponentLookup<CurrentBuilding>(isReadOnly: true);

            long movingAwayCitizenTotal = CountMovingAwayCitizens(
                movingAways,
                householdMembers);

            CountActiveCreatures(
                householdMembers,
                movingAways,
                households,
                travelPurposes,
                currentBuildings,
                out long movingAwayCreatureTotal,
                out long movingAwayIgnoreTransportNow,
                out long movingInCreatureTotal);

            int movedInThisMonth = m_CityStatisticsSystem.GetStatisticValue(StatisticType.CitizensMovedIn);
            int movedAwayThisMonth = m_CityStatisticsSystem.GetStatisticValue(StatisticType.CitizensMovedAway);

            return new Snapshot(
                movingAwayCitizenTotal,
                movingAwayCreatureTotal,
                movingAwayIgnoreTransportNow,
                movingInCreatureTotal,
                movedInThisMonth,
                movedAwayThisMonth,
                DateTime.Now);
        }

        private long CountMovingAwayCitizens(
            ComponentLookup<MovingAway> movingAways,
            ComponentLookup<HouseholdMember> householdMembers)
        {
            long total = 0;

            using (NativeArray<Entity> citizens = m_HouseholdMemberQuery.ToEntityArray(Allocator.Temp))
            {
                for (int i = 0; i < citizens.Length; i++)
                {
                    Entity citizen = citizens[i];

                    if (!householdMembers.HasComponent(citizen))
                    {
                        continue;
                    }

                    Entity household = householdMembers[citizen].m_Household;

                    if (IsMovingAway(citizen, household, movingAways))
                    {
                        total++;
                    }
                }
            }

            return total;
        }

        private void CountActiveCreatures(
            ComponentLookup<HouseholdMember> householdMembers,
            ComponentLookup<MovingAway> movingAways,
            ComponentLookup<Household> households,
            ComponentLookup<TravelPurpose> travelPurposes,
            ComponentLookup<CurrentBuilding> currentBuildings,
            out long movingAwayCreatureTotal,
            out long movingAwayIgnoreTransportNow,
            out long movingInCreatureTotal)
        {
            movingAwayCreatureTotal = 0;
            movingAwayIgnoreTransportNow = 0;
            movingInCreatureTotal = 0;

            using (NativeArray<Entity> entities = m_CreatureResidentQuery.ToEntityArray(Allocator.Temp))
            {
                for (int i = 0; i < entities.Length; i++)
                {
                    Entity creatureEntity = entities[i];
                    Game.Creatures.Resident resident = EntityManager.GetComponentData<Game.Creatures.Resident>(creatureEntity);

                    Entity citizen = resident.m_Citizen;
                    if (citizen == Entity.Null || !householdMembers.HasComponent(citizen))
                    {
                        continue;
                    }

                    Entity household = householdMembers[citizen].m_Household;

                    if (IsMovingAway(citizen, household, movingAways))
                    {
                        movingAwayCreatureTotal++;

                        if ((resident.m_Flags & ResidentFlags.IgnoreTransport) != ResidentFlags.None)
                        {
                            movingAwayIgnoreTransportNow++;
                        }

                        continue;
                    }

                    if (IsMovingIn(citizen, household, households, travelPurposes, currentBuildings))
                    {
                        movingInCreatureTotal++;
                    }
                }
            }
        }

        private static bool IsMovingAway(
            Entity citizen,
            Entity household,
            ComponentLookup<MovingAway> movingAways)
        {
            if (household != Entity.Null && movingAways.HasComponent(household))
            {
                return true;
            }

            return citizen != Entity.Null && movingAways.HasComponent(citizen);
        }

        private static bool IsMovingIn(
            Entity citizen,
            Entity household,
            ComponentLookup<Household> households,
            ComponentLookup<TravelPurpose> travelPurposes,
            ComponentLookup<CurrentBuilding> currentBuildings)
        {
            if (citizen == Entity.Null || household == Entity.Null)
            {
                return false;
            }

            if (!households.HasComponent(household))
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

            Household householdData = households[household];
            return (householdData.m_Flags & HouseholdFlags.MovedIn) == 0;
        }
    }
}
