// File: Systems/MovingAwayStatusSystem.cs
// Purpose: Read-only Options menu snapshot and log report for Moving Away Fix status.

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
    using System.Collections.Generic;
    using System.Text;
    using Unity.Collections;
    using Unity.Entities;

    public sealed partial class MovingAwayStatusSystem : GameSystemBase
    {
        private const int MaxReportSamples = 5;

        private readonly struct WalkingSample
        {
            public readonly Entity Creature;
            public readonly Entity Citizen;
            public readonly Entity Household;
            public readonly ResidentFlags Flags;

            public WalkingSample(Entity creature, Entity citizen, Entity household, ResidentFlags flags)
            {
                Creature = creature;
                Citizen = citizen;
                Household = household;
                Flags = flags;
            }
        }

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

            // Status is Options-only. Do not let this system run as part of simulation.
            Enabled = false;
        }

        protected override void OnUpdate()
        {
        }

        public Snapshot BuildSnapshot()
        {
            return BuildSnapshotInternal(samples: null);
        }

        public string BuildDetailedReport()
        {
            var samples = new List<WalkingSample>(MaxReportSamples);
            Snapshot snapshot = BuildSnapshotInternal(samples);

            string fixState = Mod.Setting?.EnableMovingAwayFix == true
                ? "ON"
                : "OFF";

            var sb = new StringBuilder();

            sb.AppendLine();
            sb.AppendLine("==================== [MAF] MOVING AWAY FIX STATUS ====================");
            sb.AppendLine("Feature: No Highway Walkers");
            sb.AppendLine("Purpose: lets moving-away cims consider public transport again by clearing IgnoreTransport.");
            sb.AppendLine();
            sb.AppendLine($"Fix enabled: {fixState}");
            sb.AppendLine($"Simulation frame: {snapshot.SimulationFrame}");
            sb.AppendLine($"Updated local time: {snapshot.SnapshotTimeLocal:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();
            sb.AppendLine("Compact counts");
            sb.AppendLine($"- Moving away now: {snapshot.MovingAwayNow:N0}");
            sb.AppendLine($"- Moving away and walking/no vehicle: {snapshot.MovingAwayWalking:N0}");
            sb.AppendLine($"- Moving away still IgnoreTransport: {snapshot.MovingAwayStillIgnoreTransport:N0}");
            sb.AppendLine($"- Moving in now: {snapshot.MovingInNow:N0}");
            sb.AppendLine($"- Population monthly moved in: {snapshot.MovedInMonthly:N0}");
            sb.AppendLine($"- Population monthly moved away: {snapshot.MovedAwayMonthly:N0}");
            sb.AppendLine();
            sb.AppendLine("Scene Explorer samples");
            sb.AppendLine("- These are moving-away walkers/no-vehicle samples, not road-classified highway checks.");
            sb.AppendLine("- Use Creature or Citizen IDs in Scene Explorer for spot checks.");

            if (samples.Count == 0)
            {
                sb.AppendLine("- No moving-away walking samples found.");
            }
            else
            {
                for (int i = 0; i < samples.Count; i++)
                {
                    WalkingSample sample = samples[i];

                    sb.Append("- ");
                    sb.Append(i + 1);
                    sb.Append(". Creature ");
                    AppendEntity(sb, sample.Creature);
                    sb.Append(" | Citizen ");
                    AppendEntity(sb, sample.Citizen);
                    sb.Append(" | Household ");
                    AppendEntity(sb, sample.Household);
                    sb.Append(" | Flags=");
                    sb.Append(sample.Flags);
                    sb.AppendLine();
                }
            }

            sb.AppendLine("======================================================================");

            return sb.ToString();
        }

        private Snapshot BuildSnapshotInternal(List<WalkingSample>? samples)
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

                            bool isWalking = IsWalking(creature, currentVehicles);
                            if (isWalking)
                            {
                                movingAwayWalking++;

                                if (samples != null && samples.Count < MaxReportSamples)
                                {
                                    samples.Add(new WalkingSample(creature, citizen, household, resident.m_Flags));
                                }
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

        private static void AppendEntity(StringBuilder sb, Entity entity)
        {
            sb.Append(entity.Index);
            sb.Append(':');
            sb.Append(entity.Version);
        }
    }
}
