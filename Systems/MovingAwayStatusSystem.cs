// File: Systems/MovingAwayStatusSystem.cs
// Purpose: Read-only Options menu snapshot and log report for Moving Away Fix status.

namespace MovingAwayFix
{
    using Game;
    using Game.Agents;
    using Game.Citizens;
    using Game.City;
    using Game.Common;
    using Game.Net;
    using Game.Objects;
    using Game.Pathfind;
    using Game.Prefabs;
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

        private readonly struct HighwayWalkerSample
        {
            public readonly Entity Creature;
            public readonly Entity Citizen;
            public readonly Entity Household;
            public readonly Entity Lane;
            public readonly Game.Creatures.ResidentFlags Flags;
            public readonly string HighwayMatch;

            public HighwayWalkerSample(
                Entity creature,
                Entity citizen,
                Entity household,
                Entity lane,
                Game.Creatures.ResidentFlags flags,
                string highwayMatch)
            {
                Creature = creature;
                Citizen = citizen;
                Household = household;
                Lane = lane;
                Flags = flags;
                HighwayMatch = highwayMatch;
            }
        }

        public readonly struct Snapshot
        {
            public readonly long MovingAwayNow;
            public readonly long MovingAwayWalking;
            public readonly long MovingAwayHighwayWalking;
            public readonly long MovingAwayStillIgnoreTransport;
            public readonly long MovingInNow;
            public readonly long MovedInMonthly;
            public readonly long MovedAwayMonthly;
            public readonly uint SimulationFrame;
            public readonly DateTime SnapshotTimeLocal;

            public Snapshot(
                long movingAwayNow,
                long movingAwayWalking,
                long movingAwayHighwayWalking,
                long movingAwayStillIgnoreTransport,
                long movingInNow,
                long movedInMonthly,
                long movedAwayMonthly,
                uint simulationFrame,
                DateTime snapshotTimeLocal)
            {
                MovingAwayNow = movingAwayNow;
                MovingAwayWalking = movingAwayWalking;
                MovingAwayHighwayWalking = movingAwayHighwayWalking;
                MovingAwayStillIgnoreTransport = movingAwayStillIgnoreTransport;
                MovingInNow = movingInNow;
                MovedInMonthly = movedInMonthly;
                MovedAwayMonthly = movedAwayMonthly;
                SimulationFrame = simulationFrame;
                SnapshotTimeLocal = snapshotTimeLocal;
            }
        }

        private CityStatisticsSystem m_CityStatisticsSystem = null!;
        private PrefabSystem m_PrefabSystem = null!;
        private SimulationSystem m_SimulationSystem = null!;
        private EntityQuery m_ResidentQuery;

        public uint CurrentSimulationFrame => m_SimulationSystem.frameIndex;

        protected override void OnCreate()
        {
            base.OnCreate();

            m_CityStatisticsSystem = World.GetOrCreateSystemManaged<CityStatisticsSystem>();
            m_PrefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            m_SimulationSystem = World.GetOrCreateSystemManaged<SimulationSystem>();

            m_ResidentQuery = SystemAPI.QueryBuilder()
                .WithAll<Game.Creatures.Resident, Game.Creatures.Human, PathOwner>()
                .WithNone<Deleted, Destroyed, Temp>()
                .WithNone<Unspawned>()
                .Build();

            // Status is Options-only. Do not let this system run during live simulation.
            Enabled = false;
        }

        protected override void OnUpdate()
        {
        }

        public Snapshot BuildSnapshot()
        {
            var samples = new List<HighwayWalkerSample>(0);
            return BuildSnapshotInternal(collectSamples: false, samples: samples);
        }

        public string BuildDetailedReport()
        {
            var samples = new List<HighwayWalkerSample>(MaxReportSamples);
            Snapshot snapshot = BuildSnapshotInternal(collectSamples: true, samples: samples);

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
            sb.AppendLine($"- Moving away walking on highway: {snapshot.MovingAwayHighwayWalking:N0}");
            sb.AppendLine($"- Moving away still IgnoreTransport: {snapshot.MovingAwayStillIgnoreTransport:N0}");
            sb.AppendLine($"- Moving in now: {snapshot.MovingInNow:N0}");
            sb.AppendLine($"- Population monthly moved in: {snapshot.MovedInMonthly:N0}");
            sb.AppendLine($"- Population monthly moved away: {snapshot.MovedAwayMonthly:N0}");
            sb.AppendLine();
            sb.AppendLine("Scene Explorer samples");
            sb.AppendLine("- These are moving-away walkers whose current lane/owner/prefab chain matched Highway.");
            sb.AppendLine("- Use Creature, Citizen, or Lane IDs in Scene Explorer for spot checks.");

            if (samples.Count == 0)
            {
                sb.AppendLine("- No moving-away highway walker samples found.");
            }
            else
            {
                for (int i = 0; i < samples.Count; i++)
                {
                    HighwayWalkerSample sample = samples[i];

                    sb.Append("- ");
                    sb.Append(i + 1);
                    sb.Append(". Creature ");
                    AppendEntity(sb, sample.Creature);
                    sb.Append(" | Citizen ");
                    AppendEntity(sb, sample.Citizen);
                    sb.Append(" | Lane ");
                    AppendEntity(sb, sample.Lane);
                    sb.Append(" | Household ");
                    AppendEntity(sb, sample.Household);
                    sb.Append(" | Flags=");
                    sb.Append(sample.Flags);
                    sb.Append(" | Match=");
                    sb.Append(sample.HighwayMatch);
                    sb.AppendLine();
                }
            }

            sb.AppendLine("======================================================================");

            return sb.ToString();
        }

        private Snapshot BuildSnapshotInternal(bool collectSamples, List<HighwayWalkerSample> samples)
        {
            EntityTypeHandle entityType = GetEntityTypeHandle();

            ComponentTypeHandle<Game.Creatures.Resident> residentType =
                SystemAPI.GetComponentTypeHandle<Game.Creatures.Resident>(isReadOnly: true);

            ComponentLookup<HouseholdMember> householdMembers =
                SystemAPI.GetComponentLookup<HouseholdMember>(isReadOnly: true);

            ComponentLookup<MovingAway> movingAways =
                SystemAPI.GetComponentLookup<MovingAway>(isReadOnly: true);

            ComponentLookup<Game.Creatures.CurrentVehicle> currentVehicles =
                SystemAPI.GetComponentLookup<Game.Creatures.CurrentVehicle>(isReadOnly: true);

            ComponentLookup<Game.Creatures.HumanCurrentLane> currentLanes =
                SystemAPI.GetComponentLookup<Game.Creatures.HumanCurrentLane>(isReadOnly: true);

            ComponentLookup<Owner> owners =
                SystemAPI.GetComponentLookup<Owner>(isReadOnly: true);

            ComponentLookup<Aggregated> aggregated =
                SystemAPI.GetComponentLookup<Aggregated>(isReadOnly: true);

            ComponentLookup<PrefabRef> prefabRefs =
                SystemAPI.GetComponentLookup<PrefabRef>(isReadOnly: true);

            ComponentLookup<NetGeometryData> netGeometryData =
                SystemAPI.GetComponentLookup<NetGeometryData>(isReadOnly: true);

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
            long movingAwayHighwayWalking = 0;
            long movingAwayStillIgnoreTransport = 0;
            long movingInNow = 0;

            using (NativeArray<ArchetypeChunk> chunks = m_ResidentQuery.ToArchetypeChunkArray(Allocator.Temp))
            {
                for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
                {
                    ArchetypeChunk chunk = chunks[chunkIndex];

                    NativeArray<Entity> entities = chunk.GetNativeArray(entityType);
                    NativeArray<Game.Creatures.Resident> residents = chunk.GetNativeArray(ref residentType);

                    for (int i = 0; i < residents.Length; i++)
                    {
                        Entity creature = entities[i];
                        Game.Creatures.Resident resident = residents[i];

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

                                if (TryGetHighwayLaneMatch(
                                        creature,
                                        currentLanes,
                                        owners,
                                        aggregated,
                                        prefabRefs,
                                        netGeometryData,
                                        out Entity lane,
                                        out string highwayMatch))
                                {
                                    movingAwayHighwayWalking++;

                                    if (collectSamples && samples.Count < MaxReportSamples)
                                    {
                                        samples.Add(new HighwayWalkerSample(
                                            creature,
                                            citizen,
                                            household,
                                            lane,
                                            resident.m_Flags,
                                            highwayMatch));
                                    }
                                }
                            }

                            if ((resident.m_Flags & Game.Creatures.ResidentFlags.IgnoreTransport) != 0)
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
                movingAwayHighwayWalking: movingAwayHighwayWalking,
                movingAwayStillIgnoreTransport: movingAwayStillIgnoreTransport,
                movingInNow: movingInNow,
                movedInMonthly: movedInMonthly,
                movedAwayMonthly: movedAwayMonthly,
                simulationFrame: m_SimulationSystem.frameIndex,
                snapshotTimeLocal: DateTime.Now);
        }

        private static bool IsWalking(
            Entity creature,
            ComponentLookup<Game.Creatures.CurrentVehicle> currentVehicles)
        {
            if (!currentVehicles.HasComponent(creature))
            {
                return true;
            }

            Game.Creatures.CurrentVehicle currentVehicle = currentVehicles[creature];

            return currentVehicle.m_Vehicle == Entity.Null ||
                   currentVehicle.m_Vehicle == creature;
        }

        private bool TryGetHighwayLaneMatch(
            Entity creature,
            ComponentLookup<Game.Creatures.HumanCurrentLane> currentLanes,
            ComponentLookup<Owner> owners,
            ComponentLookup<Aggregated> aggregated,
            ComponentLookup<PrefabRef> prefabRefs,
            ComponentLookup<NetGeometryData> netGeometryData,
            out Entity lane,
            out string highwayMatch)
        {
            lane = Entity.Null;
            highwayMatch = string.Empty;

            if (!currentLanes.HasComponent(creature))
            {
                return false;
            }

            lane = currentLanes[creature].m_Lane;
            if (lane == Entity.Null)
            {
                return false;
            }

            Entity current = lane;

            // Current lane can be "Highway Pedestrian Lane 2"; owners can lead to edge/road/aggregate.
            for (int depth = 0; depth < 8; depth++)
            {
                if (current == Entity.Null)
                {
                    break;
                }

                if (TryGetHighwayMatchOnEntity(current, aggregated, prefabRefs, netGeometryData, out highwayMatch))
                {
                    return true;
                }

                if (!owners.HasComponent(current))
                {
                    break;
                }

                current = owners[current].m_Owner;
            }

            return false;
        }

        private bool TryGetHighwayMatchOnEntity(
            Entity entity,
            ComponentLookup<Aggregated> aggregated,
            ComponentLookup<PrefabRef> prefabRefs,
            ComponentLookup<NetGeometryData> netGeometryData,
            out string highwayMatch)
        {
            highwayMatch = string.Empty;

            // Edge entities can point to an aggregate entity such as "Highway".
            if (aggregated.HasComponent(entity))
            {
                Entity aggregate = aggregated[entity].m_Aggregate;
                if (TryGetPrefabNameContainingHighway(aggregate, prefabRefs, "aggregated", out highwayMatch))
                {
                    return true;
                }
            }

            if (!prefabRefs.HasComponent(entity))
            {
                return false;
            }

            Entity prefabEntity = prefabRefs[entity].m_Prefab;

            // Direct prefab name catches examples like "Highway Pedestrian Lane 2".
            if (TryGetHighwayPrefabName(prefabEntity, "prefab", out highwayMatch))
            {
                return true;
            }

            // Road geometry can point to AggregateNetPrefab "Highway".
            if (netGeometryData.HasComponent(prefabEntity))
            {
                Entity aggregateType = netGeometryData[prefabEntity].m_AggregateType;
                if (TryGetHighwayPrefabName(aggregateType, "aggregateType", out highwayMatch))
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryGetPrefabNameContainingHighway(
            Entity entity,
            ComponentLookup<PrefabRef> prefabRefs,
            string source,
            out string highwayMatch)
        {
            highwayMatch = string.Empty;

            if (entity == Entity.Null || !prefabRefs.HasComponent(entity))
            {
                return false;
            }

            Entity prefabEntity = prefabRefs[entity].m_Prefab;
            return TryGetHighwayPrefabName(prefabEntity, source, out highwayMatch);
        }

        private bool TryGetHighwayPrefabName(Entity prefabEntity, string source, out string highwayMatch)
        {
            highwayMatch = string.Empty;

            if (prefabEntity == Entity.Null)
            {
                return false;
            }

            if (!m_PrefabSystem.TryGetPrefab(prefabEntity, out PrefabBase prefabBase))
            {
                return false;
            }

            string prefabName = prefabBase.name;
            if (string.IsNullOrEmpty(prefabName))
            {
                return false;
            }

            if (prefabName.IndexOf("Highway", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return false;
            }

            highwayMatch = source + " " + FormatEntity(prefabEntity) + " " + prefabName;
            return true;
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

        private static string FormatEntity(Entity entity)
        {
            return entity.Index + ":" + entity.Version;
        }
    }
}
