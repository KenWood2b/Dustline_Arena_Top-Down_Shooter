using System;
using System.IO;
using DustlineArena.Runtime.Config;
using DustlineArena.Runtime.Navigation;
using DustlineArena.Runtime.Pickups;
using DustlineArena.Runtime.Spawning;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace DustlineArena.Editor
{
    internal static class MinimalArenaMapBuilder
    {
        private const string DefaultScenePath = "Assets/_Project/Scenes/Dustline_Arena_01.unity";
        private const string ScenePathFormat = "Assets/_Project/Scenes/Dustline_Arena_{0:00}.unity";
        private const string EnvironmentPath = "Assets/_Project/Art/ThirdParty/TopDownShooterKit/Environment";
        private const string GenericEnvironmentPath = "Assets/Synty/PolygonGeneric/Models";
        private const string GenericMaterialsPath = "Assets/Synty/PolygonGeneric/Materials";
        private const string PreviewPath = "Assets/_Project/Diagnostics/MinimalArenaPreview.png";
        private const string MapRootName = "Props_ArenaSceneLayout_v1";
        private const float ArenaSize = 56f;
        private const float ArenaHalfSize = ArenaSize * 0.5f;

        private enum ArenaVariant
        {
            StartingYard,
            ContainerGauntlet,
            LastStand,
            OvergrownRuins,
            DepotCrossfire
        }

        private struct MaterialProfile
        {
            public string GroundSurface;
            public string Foliage;
            public string Flowers;
            public string Rock;
            public string Ground;
            public string Metal;
            public string Wood;
            public string Paper;
            public string Default;
        }

        private static MaterialProfile currentMaterialProfile = GetMaterialProfile(ArenaVariant.StartingYard);

        public static void BuildFromCommandLine()
        {
            BuildMap(true);
        }

        public static void BuildAllSceneVariantsFromCommandLine()
        {
            BuildAllSceneVariants(true);
        }

        [MenuItem("Dustline Arena/Rebuild Expanded Arena Map")]
        private static void RebuildFromMenu()
        {
            BuildMap(true);
        }

        [MenuItem("Dustline Arena/Rebuild All Arena Scene Variants")]
        private static void RebuildAllSceneVariantsFromMenu()
        {
            BuildAllSceneVariants(true);
        }

        private static void BuildMap(bool force)
        {
            BuildSceneVariant(DefaultScenePath, ArenaVariant.StartingYard, force, true);
        }

        private static void BuildAllSceneVariants(bool force)
        {
            ArenaVariant[] variants =
            {
                ArenaVariant.StartingYard,
                ArenaVariant.ContainerGauntlet,
                ArenaVariant.LastStand,
                ArenaVariant.OvergrownRuins,
                ArenaVariant.DepotCrossfire
            };

            for (int i = 0; i < variants.Length; i++)
            {
                BuildSceneVariant(string.Format(ScenePathFormat, i + 1), variants[i], force, i == 0);
            }

            Debug.Log("Dustline Arena: all arena scene variants rebuilt.");
        }

        private static void BuildSceneVariant(string scenePath, ArenaVariant variant, bool force, bool renderPreview)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            currentMaterialProfile = GetMaterialProfile(variant);
            GameObject arena = GameObject.Find("Arena");
            if (arena == null)
            {
                Debug.LogError($"Dustline Arena: Arena root was not found in {scenePath}.");
                return;
            }

            Transform existingMap = arena.transform.Find($"{MapRootName}_{variant}");
            if (!force && existingMap != null)
            {
                return;
            }

            RemoveExistingProps(arena.transform);

            GameObject mapRoot = new GameObject($"{MapRootName}_{variant}");
            mapRoot.transform.SetParent(arena.transform, false);

            Transform structures = CreateGroup(mapRoot.transform, "Structures");
            Transform cover = CreateGroup(mapRoot.transform, "Cover");
            Transform dressing = CreateGroup(mapRoot.transform, "Dressing");
            Transform perimeter = CreateGroup(mapRoot.transform, "Perimeter");

            RemoveLegacyWeaponRing(arena);
            ResizeGround(arena.transform);
            BuildVariantLayout(variant, structures, cover, dressing);
            BuildPerimeter(perimeter);
            BuildSpawnAndRewardSystems(arena.transform, variant);
            EnsureRuntimeNavMeshBuilder(arena.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            if (renderPreview)
            {
                RenderPreview();
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Dustline Arena: {scenePath} rebuilt as {variant}.");
        }

        private static RuntimeNavMeshBuilder EnsureRuntimeNavMeshBuilder(Transform arena)
        {
            GameObject existing = GameObject.Find("Runtime NavMesh");
            GameObject navMeshObject = existing == null ? new GameObject("Runtime NavMesh") : existing;
            navMeshObject.transform.SetParent(arena, false);
            navMeshObject.transform.localPosition = Vector3.zero;

            RuntimeNavMeshBuilder builder = navMeshObject.GetComponent<RuntimeNavMeshBuilder>();
            if (builder == null)
            {
                builder = navMeshObject.AddComponent<RuntimeNavMeshBuilder>();
            }

            builder.Configure(arena, new Vector3(70f, 12f, 70f));
            return builder;
        }

        private static void BuildStructures(Transform parent)
        {
            Place(parent, "Container_Long_NW", "Container_Long.fbx", new Vector3(-20f, 0f, 16f), 90f);
            Place(parent, "Container_Long_SE", "Container_Long.fbx", new Vector3(20f, 0f, -16f), 90f);
            Place(parent, "Container_Long_NE", "Container_Long.fbx", new Vector3(18f, 0f, 21f), 0f);
            Place(parent, "Container_Long_SW", "Container_Long.fbx", new Vector3(-18f, 0f, -21f), 0f);
            Place(parent, "Container_Small_NE", "Container_Small.fbx", new Vector3(21f, 0f, 8f), 90f);
            Place(parent, "Container_Small_SW", "Container_Small.fbx", new Vector3(-21f, 0f, -8f), 90f);

            Place(parent, "TrashContainer_West", "TrashContainer.fbx", new Vector3(-20f, 0f, 5f), 90f);
            Place(parent, "TrashContainer_East", "TrashContainer.fbx", new Vector3(20f, 0f, -5f), -90f);
        }

        private static void BuildVariantLayout(ArenaVariant variant, Transform structures, Transform cover, Transform dressing)
        {
            switch (variant)
            {
                case ArenaVariant.ContainerGauntlet:
                    BuildGauntletStructures(structures);
                    BuildGauntletCover(cover);
                    BuildGauntletDressing(dressing);
                    break;
                case ArenaVariant.LastStand:
                    BuildLastStandStructures(structures);
                    BuildLastStandCover(cover);
                    BuildLastStandDressing(dressing);
                    break;
                case ArenaVariant.OvergrownRuins:
                    BuildOvergrownRuinsStructures(structures);
                    BuildOvergrownRuinsCover(cover);
                    BuildOvergrownRuinsDressing(dressing);
                    break;
                case ArenaVariant.DepotCrossfire:
                    BuildDepotCrossfireStructures(structures);
                    BuildDepotCrossfireCover(cover);
                    BuildDepotCrossfireDressing(dressing);
                    break;
                default:
                    BuildStructures(structures);
                    BuildCover(cover);
                    BuildDressing(dressing);
                    break;
            }
        }

        private static void BuildCover(Transform parent)
        {
            Place(parent, "Barrier_North_West", "Barrier_Fixed.fbx", new Vector3(-8f, 0f, 14f), 0f);
            Place(parent, "Barrier_North_East", "Barrier_Large.fbx", new Vector3(8f, 0f, 14f), 180f);
            Place(parent, "Barrier_South_West", "Barrier_Large.fbx", new Vector3(-8f, 0f, -14f), 0f);
            Place(parent, "Barrier_South_East", "Barrier_Fixed.fbx", new Vector3(8f, 0f, -14f), 180f);

            Place(parent, "Barrier_West_North", "Barrier_Large.fbx", new Vector3(-14f, 0f, 8f), 90f);
            Place(parent, "Barrier_West_South", "Barrier_Fixed.fbx", new Vector3(-14f, 0f, -8f), -90f);
            Place(parent, "Barrier_East_North", "Barrier_Fixed.fbx", new Vector3(14f, 0f, 8f), 90f);
            Place(parent, "Barrier_East_South", "Barrier_Large.fbx", new Vector3(14f, 0f, -8f), -90f);

            Place(parent, "SackTrench_NW", "SackTrench.fbx", new Vector3(-10f, 0f, 10f), 45f);
            Place(parent, "SackTrench_NE", "SackTrench.fbx", new Vector3(10f, 0f, 10f), -45f);
            Place(parent, "SackTrench_SW", "SackTrench.fbx", new Vector3(-10f, 0f, -10f), 135f);
            Place(parent, "SackTrench_SE", "SackTrench.fbx", new Vector3(10f, 0f, -10f), -135f);

            Place(parent, "Crate_West", "Crate.fbx", new Vector3(-18f, 0f, 0f), 12f);
            Place(parent, "Crate_East", "Crate.fbx", new Vector3(18f, 0f, 0f), -12f);
            Place(parent, "Crate_North", "Crate.fbx", new Vector3(0f, 0f, 18f), -8f);
            Place(parent, "Crate_South", "Crate.fbx", new Vector3(0f, 0f, -18f), 8f);

            Place(parent, "Barrier_Center_NW", "Barrier_Single.fbx", new Vector3(-5f, 0f, 4f), 35f);
            Place(parent, "Barrier_Center_NE", "Barrier_Single.fbx", new Vector3(5f, 0f, 4f), -35f);
            Place(parent, "Barrier_Center_SW", "Barrier_Single.fbx", new Vector3(-5f, 0f, -4f), 145f);
            Place(parent, "Barrier_Center_SE", "Barrier_Single.fbx", new Vector3(5f, 0f, -4f), -145f);
        }

        private static void BuildDressing(Transform parent)
        {
            Place(parent, "Pallet_NW", "Pallet.fbx", new Vector3(-23f, 0f, 22f), 20f);
            Place(parent, "Pallet_SE", "Pallet.fbx", new Vector3(23f, 0f, -22f), -20f);
            Place(parent, "Barrel_North", "ExplodingBarrel.fbx", new Vector3(3f, 0f, 22f), 0f);
            Place(parent, "Barrel_South", "ExplodingBarrel.fbx", new Vector3(-3f, 0f, -22f), 0f);

            Place(parent, "Cone_NE_A", "TrafficCone.fbx", new Vector3(23f, 0f, 14f), 0f, false);
            Place(parent, "Cone_NE_B", "TrafficCone.fbx", new Vector3(22f, 0f, 15f), 18f, false);
            Place(parent, "Cone_SW_A", "TrafficCone.fbx", new Vector3(-23f, 0f, -14f), 0f, false);
            Place(parent, "Cone_SW_B", "TrafficCone.fbx", new Vector3(-22f, 0f, -15f), -18f, false);
        }

        private static void BuildGauntletStructures(Transform parent)
        {
            Place(parent, "Container_North_A", "Container_Long.fbx", new Vector3(-13f, 0f, 12f), 0f);
            Place(parent, "Container_North_B", "Container_Long.fbx", new Vector3(8f, 0f, 12f), 0f);
            Place(parent, "Container_South_A", "Container_Long.fbx", new Vector3(-8f, 0f, -12f), 0f);
            Place(parent, "Container_South_B", "Container_Long.fbx", new Vector3(13f, 0f, -12f), 0f);
            Place(parent, "Container_Block_West", "Container_Small.fbx", new Vector3(-21f, 0f, 0f), 90f);
            Place(parent, "Container_Block_East", "Container_Small.fbx", new Vector3(21f, 0f, 0f), 90f);
            Place(parent, "Trash_North_Pin", "TrashContainer.fbx", new Vector3(-2f, 0f, 9f), 90f);
            Place(parent, "Trash_South_Pin", "TrashContainer.fbx", new Vector3(3f, 0f, -9f), -90f);
        }

        private static void BuildGauntletCover(Transform parent)
        {
            Place(parent, "Barrier_Choke_West_A", "Barrier_Large.fbx", new Vector3(-15f, 0f, 3.8f), 60f);
            Place(parent, "Barrier_Choke_West_B", "Barrier_Fixed.fbx", new Vector3(-15f, 0f, -3.8f), -60f);
            Place(parent, "Barrier_Choke_Mid_A", "Barrier_Single.fbx", new Vector3(-3f, 0f, 4.4f), -35f);
            Place(parent, "Barrier_Choke_Mid_B", "Barrier_Single.fbx", new Vector3(3f, 0f, -4.4f), 145f);
            Place(parent, "Barrier_Choke_East_A", "Barrier_Fixed.fbx", new Vector3(15f, 0f, 3.8f), 60f);
            Place(parent, "Barrier_Choke_East_B", "Barrier_Large.fbx", new Vector3(15f, 0f, -3.8f), -60f);
            Place(parent, "Sacks_North_Gap", "SackTrench.fbx", new Vector3(2f, 0f, 6.8f), 0f);
            Place(parent, "Sacks_South_Gap", "SackTrench.fbx", new Vector3(-2f, 0f, -6.8f), 180f);
        }

        private static void BuildGauntletDressing(Transform parent)
        {
            Place(parent, "Pallet_West", "Pallet.fbx", new Vector3(-22f, 0f, -8f), 30f);
            Place(parent, "Pallet_East", "Pallet.fbx", new Vector3(22f, 0f, 8f), -30f);
            Place(parent, "Barrel_Center_Left", "ExplodingBarrel.fbx", new Vector3(-7f, 0f, 0f), 0f);
            Place(parent, "Barrel_Center_Right", "ExplodingBarrel.fbx", new Vector3(7f, 0f, 0f), 0f);
            Place(parent, "Cone_Exit_A", "TrafficCone.fbx", new Vector3(19f, 0f, 3f), 0f, false);
            Place(parent, "Cone_Exit_B", "TrafficCone.fbx", new Vector3(19f, 0f, -3f), 0f, false);

            PlaceGeneric(parent, "RoadPatch_WestLane", "SM_Gen_Env_Road_Gravel_Straight_01.fbx", new Vector3(-17f, 0.01f, 0f), 90f, 7.5f, false);
            PlaceGeneric(parent, "RoadPatch_EastLane", "SM_Gen_Env_Road_Gravel_Straight_02.fbx", new Vector3(17f, 0.01f, 0f), 90f, 7.5f, false);
            PlaceGeneric(parent, "RoadPatch_Center", "SM_Gen_Env_Road_Gravel_Corner_Large_01.fbx", new Vector3(0f, 0.01f, 0f), 45f, 8f, false);
            PlaceGeneric(parent, "RoadWarning_North", "SM_Gen_Env_Road_Warning_01.fbx", new Vector3(-5f, 0.02f, 17f), 0f, 3.5f, false);
            PlaceGeneric(parent, "RoadWarning_South", "SM_Gen_Env_Road_Warning_01.fbx", new Vector3(5f, 0.02f, -17f), 180f, 3.5f, false);

            PlaceGeneric(parent, "PipeStack_NorthWest", "SM_Gen_Bld_Pipe_Straight_03.fbx", new Vector3(-19f, 0f, 16f), 18f, 4f);
            PlaceGeneric(parent, "PipeValve_SouthEast", "SM_Gen_Bld_Pipe_Valve_01.fbx", new Vector3(18f, 0f, -15f), -25f, 2.2f);
            PlaceGeneric(parent, "MetalBarrel_North", "SM_Gen_Prop_Barrel_Metal_01.fbx", new Vector3(13f, 0f, 16f), 0f, 1.4f);
            PlaceGeneric(parent, "MetalBarrel_South", "SM_Gen_Prop_Barrel_Metal_02.fbx", new Vector3(-13f, 0f, -16f), 0f, 1.4f);
            PlaceGeneric(parent, "BoxScatter_North", "SM_Gen_Prop_Cardboard_Box_04.fbx", new Vector3(0f, 0f, 18f), 22f, 1.5f);
            PlaceGeneric(parent, "BoxScatter_South", "SM_Gen_Prop_Cardboard_Box_05.fbx", new Vector3(0f, 0f, -18f), -22f, 1.5f);
            PlaceGeneric(parent, "Papers_WestLane", "SM_Gen_Prop_Papers_05.fbx", new Vector3(-9f, 0.02f, -13f), 40f, 1.6f, false);
            PlaceGeneric(parent, "Papers_EastLane", "SM_Gen_Prop_Papers_02.fbx", new Vector3(9f, 0.02f, 13f), -40f, 1.6f, false);
        }

        private static void BuildLastStandStructures(Transform parent)
        {
            Place(parent, "Container_NW", "Container_Long.fbx", new Vector3(-18f, 0f, 18f), 45f);
            Place(parent, "Container_NE", "Container_Long.fbx", new Vector3(18f, 0f, 18f), -45f);
            Place(parent, "Container_SW", "Container_Long.fbx", new Vector3(-18f, 0f, -18f), -45f);
            Place(parent, "Container_SE", "Container_Long.fbx", new Vector3(18f, 0f, -18f), 45f);
            Place(parent, "Trash_West", "TrashContainer.fbx", new Vector3(-22f, 0f, 0f), 90f);
            Place(parent, "Trash_East", "TrashContainer.fbx", new Vector3(22f, 0f, 0f), -90f);
        }

        private static void BuildLastStandCover(Transform parent)
        {
            const int count = 8;
            for (int i = 0; i < count; i++)
            {
                float angle = i / (float)count * Mathf.PI * 2f;
                Vector3 position = new Vector3(Mathf.Cos(angle) * 9f, 0f, Mathf.Sin(angle) * 9f);
                float yaw = -angle * Mathf.Rad2Deg + 90f;
                Place(parent, $"Ring_Barrier_{i:00}", i % 2 == 0 ? "Barrier_Large.fbx" : "Barrier_Fixed.fbx", position, yaw);
            }

            Place(parent, "Center_Crate_A", "Crate.fbx", new Vector3(-2.2f, 0f, 0f), 15f);
            Place(parent, "Center_Crate_B", "Crate.fbx", new Vector3(2.2f, 0f, 0f), -15f);
            Place(parent, "Center_Sacks", "SackTrench.fbx", new Vector3(0f, 0f, 3f), 0f);
        }

        private static void BuildLastStandDressing(Transform parent)
        {
            Place(parent, "Barrel_North_West", "ExplodingBarrel.fbx", new Vector3(-6f, 0f, 20f), 0f);
            Place(parent, "Barrel_North_East", "ExplodingBarrel.fbx", new Vector3(6f, 0f, 20f), 0f);
            Place(parent, "Barrel_South_West", "ExplodingBarrel.fbx", new Vector3(-6f, 0f, -20f), 0f);
            Place(parent, "Barrel_South_East", "ExplodingBarrel.fbx", new Vector3(6f, 0f, -20f), 0f);
            Place(parent, "Pallet_North", "Pallet.fbx", new Vector3(0f, 0f, 22f), 0f);
            Place(parent, "Pallet_South", "Pallet.fbx", new Vector3(0f, 0f, -22f), 180f);
        }

        private static void BuildOvergrownRuinsStructures(Transform parent)
        {
            Place(parent, "Container_Buried_NorthWest", "Container_Long.fbx", new Vector3(-18f, 0f, 17f), 35f);
            Place(parent, "Container_Buried_SouthEast", "Container_Long.fbx", new Vector3(18f, 0f, -17f), -145f);
            Place(parent, "Trash_Ruin_West", "TrashContainer.fbx", new Vector3(-21f, 0f, -2f), 95f);
            Place(parent, "Trash_Ruin_East", "TrashContainer.fbx", new Vector3(21f, 0f, 5f), -80f);

            PlaceGeneric(parent, "Cliff_North_A", "SM_Gen_Env_Cliff_01.fbx", new Vector3(-10f, 0f, 23f), 20f, 5.2f);
            PlaceGeneric(parent, "Cliff_North_B", "SM_Gen_Env_Cliff_02.fbx", new Vector3(10f, 0f, 23f), -20f, 5.2f);
            PlaceGeneric(parent, "Cliff_South_A", "SM_Gen_Env_Cliff_03.fbx", new Vector3(-11f, 0f, -23f), 160f, 5.2f);
            PlaceGeneric(parent, "Cliff_South_B", "SM_Gen_Env_Cliff_04.fbx", new Vector3(11f, 0f, -23f), -160f, 5.2f);
        }

        private static void BuildOvergrownRuinsCover(Transform parent)
        {
            Place(parent, "Sacks_West_Ridge", "SackTrench.fbx", new Vector3(-12f, 0f, 8f), 75f);
            Place(parent, "Sacks_East_Ridge", "SackTrench.fbx", new Vector3(12f, 0f, -8f), -105f);
            Place(parent, "Crate_Ruin_Center_A", "Crate.fbx", new Vector3(-3f, 0f, 2f), 18f);
            Place(parent, "Crate_Ruin_Center_B", "Crate.fbx", new Vector3(4f, 0f, -3f), -28f);
            Place(parent, "Barrier_Ruin_North", "Barrier_Single.fbx", new Vector3(4f, 0f, 10f), -45f);
            Place(parent, "Barrier_Ruin_South", "Barrier_Single.fbx", new Vector3(-5f, 0f, -10f), 135f);
            PlaceGeneric(parent, "Cliff_Center_Left", "SM_Gen_Env_Dirt_Cliff_01.fbx", new Vector3(-8f, 0f, -2f), 80f, 3.4f);
            PlaceGeneric(parent, "Cliff_Center_Right", "SM_Gen_Env_Dirt_Cliff_02.fbx", new Vector3(8f, 0f, 2f), -80f, 3.4f);
        }

        private static void BuildOvergrownRuinsDressing(Transform parent)
        {
            Place(parent, "Pallet_Abandoned", "Pallet.fbx", new Vector3(-22f, 0f, 18f), -30f);
            Place(parent, "Barrel_Rust_North", "ExplodingBarrel.fbx", new Vector3(17f, 0f, 17f), 0f);
            Place(parent, "Barrel_Rust_South", "ExplodingBarrel.fbx", new Vector3(-17f, 0f, -17f), 0f);

            for (int i = 0; i < 8; i++)
            {
                float angle = i / 8f * Mathf.PI * 2f;
                float radius = i % 2 == 0 ? 20f : 15f;
                Vector3 position = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                PlaceGeneric(parent, $"Bush_{i:00}", i % 3 == 0 ? "SM_Gen_Env_Bush_Large_01.fbx" : "SM_Gen_Env_Bush_02.fbx", position, -angle * Mathf.Rad2Deg, 2.4f, false);
            }

            PlaceGeneric(parent, "Grass_North", "SM_Gen_Env_Grass_Tall_01.fbx", new Vector3(0f, 0f, 20f), 0f, 3f, false);
            PlaceGeneric(parent, "Grass_South", "SM_Gen_Env_Grass_Tall_02.fbx", new Vector3(0f, 0f, -20f), 180f, 3f, false);
            PlaceGeneric(parent, "Flowers_West", "SM_Gen_Env_Flowers_03.fbx", new Vector3(-18f, 0f, 0f), 60f, 1.8f, false);
            PlaceGeneric(parent, "Flowers_East", "SM_Gen_Env_Flowers_06.fbx", new Vector3(18f, 0f, 0f), -60f, 1.8f, false);

            PlaceGeneric(parent, "DirtScuff_NorthWest", "SM_Gen_Env_Ground_Dirt_03.fbx", new Vector3(-13f, 0.01f, 13f), 25f, 3.8f, false);
            PlaceGeneric(parent, "DirtScuff_SouthEast", "SM_Gen_Env_Ground_Dirt_04.fbx", new Vector3(13f, 0.01f, -13f), -25f, 3.8f, false);
            PlaceGeneric(parent, "DirtPatch_CenterNorth", "SM_Gen_Env_Ground_Dirt_Large_01.fbx", new Vector3(0f, 0.01f, 9f), 12f, 6f, false);
            PlaceGeneric(parent, "DirtPatch_CenterSouth", "SM_Gen_Env_Ground_Dirt_Large_02.fbx", new Vector3(0f, 0.01f, -9f), -12f, 6f, false);
            PlaceGeneric(parent, "PebbleTrail_WestEdge", "SM_Gen_Env_Rock_Pebbles_02.fbx", new Vector3(-22f, 0f, 3f), 90f, 2.4f, false);
            PlaceGeneric(parent, "PebbleTrail_EastEdge", "SM_Gen_Env_Rock_Pebbles_05.fbx", new Vector3(22f, 0f, -3f), -90f, 2.4f, false);

            PlaceGeneric(parent, "Pebbles_North", "SM_Gen_Env_Rock_Pebbles_01.fbx", new Vector3(6f, 0f, 16f), 20f, 2.4f, false);
            PlaceGeneric(parent, "Pebbles_South", "SM_Gen_Env_Rock_Pebbles_04.fbx", new Vector3(-6f, 0f, -16f), -20f, 2.4f, false);
            PlaceGeneric(parent, "SmallRock_West", "SM_Gen_Env_Rock_03.fbx", new Vector3(-18f, 0f, -8f), 35f, 2.2f);
            PlaceGeneric(parent, "SmallRock_East", "SM_Gen_Env_Rock_06.fbx", new Vector3(18f, 0f, 8f), -35f, 2.2f);
            PlaceGeneric(parent, "BushCluster_West", "SM_Gen_Env_Bush_Large_03.fbx", new Vector3(-15f, 0f, 4f), 80f, 2.8f, false);
            PlaceGeneric(parent, "BushCluster_East", "SM_Gen_Env_Bush_Large_04.fbx", new Vector3(15f, 0f, -4f), -80f, 2.8f, false);
            PlaceGeneric(parent, "Fern_CenterLeft", "SM_Gen_Env_Fern_01.fbx", new Vector3(-4f, 0f, 11f), 40f, 1.6f, false);
            PlaceGeneric(parent, "Fern_CenterRight", "SM_Gen_Env_Fern_03.fbx", new Vector3(4f, 0f, -11f), -40f, 1.6f, false);
        }

        private static void BuildDepotCrossfireStructures(Transform parent)
        {
            Place(parent, "Container_Row_North_A", "Container_Long.fbx", new Vector3(-15f, 0f, 18f), 0f);
            Place(parent, "Container_Row_North_B", "Container_Long.fbx", new Vector3(6f, 0f, 18f), 0f);
            Place(parent, "Container_Row_South_A", "Container_Long.fbx", new Vector3(-6f, 0f, -18f), 180f);
            Place(parent, "Container_Row_South_B", "Container_Long.fbx", new Vector3(15f, 0f, -18f), 180f);
            Place(parent, "Container_Block_NW", "Container_Small.fbx", new Vector3(-22f, 0f, 8f), 90f);
            Place(parent, "Container_Block_SE", "Container_Small.fbx", new Vector3(22f, 0f, -8f), 90f);
            PlaceGeneric(parent, "Pipe_North", "SM_Gen_Bld_Pipe_Straight_01.fbx", new Vector3(0f, 0f, 22f), 90f, 6.5f);
            PlaceGeneric(parent, "Pipe_South", "SM_Gen_Bld_Pipe_Straight_02.fbx", new Vector3(0f, 0f, -22f), 90f, 6.5f);
        }

        private static void BuildDepotCrossfireCover(Transform parent)
        {
            Place(parent, "Crossfire_Barrier_West_A", "Barrier_Large.fbx", new Vector3(-12f, 0f, 2f), 25f);
            Place(parent, "Crossfire_Barrier_West_B", "Barrier_Fixed.fbx", new Vector3(-12f, 0f, -5f), -25f);
            Place(parent, "Crossfire_Barrier_East_A", "Barrier_Fixed.fbx", new Vector3(12f, 0f, 5f), 155f);
            Place(parent, "Crossfire_Barrier_East_B", "Barrier_Large.fbx", new Vector3(12f, 0f, -2f), -155f);
            Place(parent, "Crate_Depot_North", "Crate.fbx", new Vector3(1.5f, 0f, 9f), 8f);
            Place(parent, "Crate_Depot_South", "Crate.fbx", new Vector3(-1.5f, 0f, -9f), -8f);
            PlaceGeneric(parent, "BoxStack_West", "SM_Gen_Prop_Cardboard_Box_Preset_01.fbx", new Vector3(-5f, 0f, 0f), 15f, 2.4f);
            PlaceGeneric(parent, "BoxStack_East", "SM_Gen_Prop_Crate_Preset_01.fbx", new Vector3(5f, 0f, 0f), -15f, 2.4f);
        }

        private static void BuildDepotCrossfireDressing(Transform parent)
        {
            Place(parent, "Barrel_Depot_Left", "ExplodingBarrel.fbx", new Vector3(-18f, 0f, 14f), 0f);
            Place(parent, "Barrel_Depot_Right", "ExplodingBarrel.fbx", new Vector3(18f, 0f, -14f), 0f);
            Place(parent, "Cone_Depot_A", "TrafficCone.fbx", new Vector3(-20f, 0f, -10f), 0f, false);
            Place(parent, "Cone_Depot_B", "TrafficCone.fbx", new Vector3(-19f, 0f, -11f), 20f, false);
            Place(parent, "Cone_Depot_C", "TrafficCone.fbx", new Vector3(20f, 0f, 10f), 0f, false);
            Place(parent, "Cone_Depot_D", "TrafficCone.fbx", new Vector3(19f, 0f, 11f), -20f, false);
            PlaceGeneric(parent, "RoofLight_North", "SM_Gen_Prop_Light_Roof_01.fbx", new Vector3(-7f, 0f, 20f), 0f, 1.6f, false);
            PlaceGeneric(parent, "RoofLight_South", "SM_Gen_Prop_Light_Roof_02.fbx", new Vector3(7f, 0f, -20f), 180f, 1.6f, false);
            PlaceGeneric(parent, "Papers_Depot", "SM_Gen_Prop_Papers_03.fbx", new Vector3(0f, 0f, 13f), 30f, 1.2f, false);

            PlaceGeneric(parent, "ParkingPatch_NorthWest", "SM_Gen_Env_Road_Parking_01.fbx", new Vector3(-14f, 0.01f, 12f), 0f, 6f, false);
            PlaceGeneric(parent, "ParkingPatch_SouthEast", "SM_Gen_Env_Road_Parking_04.fbx", new Vector3(14f, 0.01f, -12f), 180f, 6f, false);
            PlaceGeneric(parent, "RoadCrossing_Center", "SM_Gen_Env_Road_Crossing_01.fbx", new Vector3(0f, 0.01f, 0f), 0f, 7f, false);
            PlaceGeneric(parent, "RoadSmall_West", "SM_Gen_Env_Road_Small_02.fbx", new Vector3(-20f, 0.01f, 1f), 90f, 4f, false);
            PlaceGeneric(parent, "RoadSmall_East", "SM_Gen_Env_Road_Small_03.fbx", new Vector3(20f, 0.01f, -1f), -90f, 4f, false);

            PlaceGeneric(parent, "PipeCorner_NorthEast", "SM_Gen_Bld_Pipe_Corner_01.fbx", new Vector3(16f, 0f, 16f), 35f, 2.8f);
            PlaceGeneric(parent, "PipeT_SouthWest", "SM_Gen_Bld_Pipe_T_01.fbx", new Vector3(-16f, 0f, -16f), -35f, 2.8f);
            PlaceGeneric(parent, "MetalBarrel_Depot_North", "SM_Gen_Prop_Barrel_Metal_03.fbx", new Vector3(-4f, 0f, 17f), 0f, 1.4f);
            PlaceGeneric(parent, "MetalBarrel_Depot_South", "SM_Gen_Prop_Barrel_Metal_02.fbx", new Vector3(4f, 0f, -17f), 0f, 1.4f);
            PlaceGeneric(parent, "CardboardScatter_West", "SM_Gen_Prop_Cardboard_Box_02.fbx", new Vector3(-11f, 0f, 10f), 28f, 1.5f);
            PlaceGeneric(parent, "CardboardScatter_East", "SM_Gen_Prop_Cardboard_Box_03.fbx", new Vector3(11f, 0f, -10f), -28f, 1.5f);
            PlaceGeneric(parent, "Planks_North", "SM_Gen_Prop_Plank_01.fbx", new Vector3(7f, 0f, 14f), 70f, 2.2f);
            PlaceGeneric(parent, "Planks_South", "SM_Gen_Prop_Plank_02.fbx", new Vector3(-7f, 0f, -14f), -70f, 2.2f);
            PlaceGeneric(parent, "WallLight_West", "SM_Gen_Prop_Light_Wall_01.fbx", new Vector3(-22f, 0f, 5f), 90f, 1.4f, false);
            PlaceGeneric(parent, "WallLight_East", "SM_Gen_Prop_Light_Wall_01.fbx", new Vector3(22f, 0f, -5f), -90f, 1.4f, false);
        }

        private static void ResizeGround(Transform arena)
        {
            Transform ground = arena.Find("Ground");
            if (ground != null)
            {
                ground.localScale = new Vector3(ArenaSize, 0.2f, ArenaSize);
                EnsureGroundCollider(ground);
                ApplyGroundSurfaceMaterial(ground);
            }
        }

        private static void EnsureGroundCollider(Transform ground)
        {
            Collider[] colliders = ground.GetComponents<Collider>();
            for (int i = colliders.Length - 1; i >= 0; i--)
            {
                if (!colliders[i].isTrigger)
                {
                    Object.DestroyImmediate(colliders[i]);
                }
            }

            BoxCollider collider = ground.gameObject.AddComponent<BoxCollider>();
            collider.size = Vector3.one;
            collider.center = Vector3.zero;
        }

        private static void ApplyGroundSurfaceMaterial(Transform ground)
        {
            Material material = LoadNativeGenericMaterialByName(currentMaterialProfile.GroundSurface);
            if (material == null)
            {
                return;
            }

            foreach (Renderer renderer in ground.GetComponentsInChildren<Renderer>(true))
            {
                renderer.sharedMaterial = material;
                EditorUtility.SetDirty(renderer);
            }
        }

        private static void RemoveLegacyWeaponRing(GameObject arena)
        {
            WeaponPickupRingSpawner legacySpawner = arena.GetComponent<WeaponPickupRingSpawner>();
            if (legacySpawner != null)
            {
                Object.DestroyImmediate(legacySpawner);
            }
        }

        private static void BuildPerimeter(Transform parent)
        {
            const float wallOffset = ArenaHalfSize - 0.5f;
            const float segmentSpacing = 4.2f;
            int segmentCount = Mathf.FloorToInt((ArenaSize - 4f) / segmentSpacing);

            for (int i = 0; i <= segmentCount; i++)
            {
                float coordinate = -ArenaHalfSize + 2f + i * segmentSpacing;
                Place(parent, $"Wall_North_{i:00}", "Barrier_Large.fbx", new Vector3(coordinate, 0f, wallOffset), 0f);
                Place(parent, $"Wall_South_{i:00}", "Barrier_Large.fbx", new Vector3(coordinate, 0f, -wallOffset), 180f);
                Place(parent, $"Wall_East_{i:00}", "Barrier_Large.fbx", new Vector3(wallOffset, 0f, coordinate), 90f);
                Place(parent, $"Wall_West_{i:00}", "Barrier_Large.fbx", new Vector3(-wallOffset, 0f, coordinate), -90f);
            }

            CreateSafetyWall(parent, "SafetyWall_North", new Vector3(0f, 1.5f, ArenaHalfSize), new Vector3(ArenaSize + 2f, 3f, 1f));
            CreateSafetyWall(parent, "SafetyWall_South", new Vector3(0f, 1.5f, -ArenaHalfSize), new Vector3(ArenaSize + 2f, 3f, 1f));
            CreateSafetyWall(parent, "SafetyWall_East", new Vector3(ArenaHalfSize, 1.5f, 0f), new Vector3(1f, 3f, ArenaSize + 2f));
            CreateSafetyWall(parent, "SafetyWall_West", new Vector3(-ArenaHalfSize, 1.5f, 0f), new Vector3(1f, 3f, ArenaSize + 2f));
        }

        private static void CreateSafetyWall(Transform parent, string name, Vector3 position, Vector3 size)
        {
            GameObject wall = new GameObject(name);
            wall.transform.SetParent(parent, false);
            wall.transform.localPosition = position;
            BoxCollider collider = wall.AddComponent<BoxCollider>();
            collider.size = size;
        }

        private static SpawnPoint[] CreateSpawnPoints(Transform parent)
        {
            return CreateSpawnPoints(
                parent,
                new[]
                {
                    new Vector3(-23f, 0.35f, -20f),
                    new Vector3(0f, 0.35f, -24f),
                    new Vector3(23f, 0.35f, -20f),
                    new Vector3(24f, 0.35f, -6f),
                    new Vector3(24f, 0.35f, 10f),
                    new Vector3(18f, 0.35f, 23f),
                    new Vector3(0f, 0.35f, 24f),
                    new Vector3(-18f, 0.35f, 23f),
                    new Vector3(-24f, 0.35f, 10f),
                    new Vector3(-24f, 0.35f, -6f)
                });
        }

        private static SpawnPoint[] CreateGauntletSpawnPoints(Transform parent)
        {
            return CreateSpawnPoints(
                parent,
                new[]
                {
                    new Vector3(-24f, 0.35f, -8f),
                    new Vector3(-24f, 0.35f, 8f),
                    new Vector3(-12f, 0.35f, -20f),
                    new Vector3(0f, 0.35f, -22f),
                    new Vector3(12f, 0.35f, -20f),
                    new Vector3(24f, 0.35f, -8f),
                    new Vector3(24f, 0.35f, 8f),
                    new Vector3(12f, 0.35f, 20f),
                    new Vector3(0f, 0.35f, 22f),
                    new Vector3(-12f, 0.35f, 20f)
                });
        }

        private static SpawnPoint[] CreateLastStandSpawnPoints(Transform parent)
        {
            return CreateSpawnPoints(
                parent,
                new[]
                {
                    new Vector3(-24f, 0.35f, -24f),
                    new Vector3(0f, 0.35f, -26f),
                    new Vector3(24f, 0.35f, -24f),
                    new Vector3(26f, 0.35f, 0f),
                    new Vector3(24f, 0.35f, 24f),
                    new Vector3(0f, 0.35f, 26f),
                    new Vector3(-24f, 0.35f, 24f),
                    new Vector3(-26f, 0.35f, 0f),
                    new Vector3(-18f, 0.35f, 12f),
                    new Vector3(18f, 0.35f, -12f)
                });
        }

        private static SpawnPoint[] CreateSpawnPoints(Transform parent, Vector3[] positions)
        {
            Transform root = CreateGroup(parent, "Spawn Points");
            SpawnPoint[] spawnPoints = new SpawnPoint[positions.Length];
            for (int i = 0; i < positions.Length; i++)
            {
                GameObject point = new GameObject($"Spawn Point {i + 1:00}");
                point.transform.SetParent(root, false);
                point.transform.localPosition = positions[i];

                Vector3 lookDirection = -new Vector3(positions[i].x, 0f, positions[i].z);
                if (lookDirection.sqrMagnitude < 0.001f)
                {
                    lookDirection = Vector3.forward;
                }

                point.transform.localRotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
                spawnPoints[i] = point.AddComponent<SpawnPoint>();
            }

            return spawnPoints;
        }

        private static Transform[] CreateRewardPoints(Transform parent)
        {
            return CreateRewardPoints(
                parent,
                new[]
                {
                    new Vector3(-8f, 0.05f, 0f),
                    new Vector3(8f, 0.05f, 0f),
                    new Vector3(0f, 0.05f, 8f),
                    new Vector3(0f, 0.05f, -8f)
                });
        }

        private static Transform[] CreateGauntletRewardPoints(Transform parent)
        {
            return CreateRewardPoints(
                parent,
                new[]
                {
                    new Vector3(-17f, 0.05f, 0f),
                    new Vector3(0f, 0.05f, 0f),
                    new Vector3(17f, 0.05f, 0f)
                });
        }

        private static Transform[] CreateLastStandRewardPoints(Transform parent)
        {
            return CreateRewardPoints(
                parent,
                new[]
                {
                    new Vector3(-7f, 0.05f, 7f),
                    new Vector3(7f, 0.05f, 7f),
                    new Vector3(-7f, 0.05f, -7f),
                    new Vector3(7f, 0.05f, -7f)
                });
        }

        private static Transform[] CreateRewardPoints(Transform parent, Vector3[] positions)
        {
            Transform root = CreateGroup(parent, "Reward Points");
            Transform[] rewardPoints = new Transform[positions.Length];
            for (int i = 0; i < positions.Length; i++)
            {
                rewardPoints[i] = CreateGroup(root, $"Reward Point {i + 1:00}");
                rewardPoints[i].localPosition = positions[i];
            }

            return rewardPoints;
        }

        private static void BuildSpawnAndRewardSystems(Transform arena, ArenaVariant variant)
        {
            RemoveRootObject("Spawn Points");
            RemoveRootObject("Kill Reward System");
            RemoveRootObject("Location Director");

            GameObject spawnRoot = new GameObject("Spawn Points");
            Vector3[] spawnPositions = GetSpawnPositions(variant);

            SpawnPoint[] spawnPoints = new SpawnPoint[spawnPositions.Length];
            for (int i = 0; i < spawnPositions.Length; i++)
            {
                GameObject point = new GameObject($"Spawn Point {i + 1:00}");
                point.transform.SetParent(spawnRoot.transform, false);
                point.transform.position = spawnPositions[i];
                point.transform.rotation = Quaternion.LookRotation(-spawnPositions[i].normalized, Vector3.up);
                spawnPoints[i] = point.AddComponent<SpawnPoint>();
            }

            WaveSpawner waveSpawner = Object.FindObjectOfType<WaveSpawner>();
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (waveSpawner != null)
            {
                SerializedObject serializedSpawner = new SerializedObject(waveSpawner);
                AssignObjectArray(serializedSpawner.FindProperty("spawnPoints"), spawnPoints);
                serializedSpawner.FindProperty("enemyTarget").objectReferenceValue =
                    player == null ? null : player.transform;
                serializedSpawner.FindProperty("minimumSpawnDistance").floatValue = 15f;
                serializedSpawner.FindProperty("preferOffscreenSpawns").boolValue = true;

                AssignObjectArray(serializedSpawner.FindProperty("waves"), new[]
                {
                    LoadAsset<WaveConfig>("Assets/_Project/Configs/Wave_01.asset"),
                    LoadAsset<WaveConfig>("Assets/_Project/Configs/Wave_02.asset"),
                    LoadAsset<WaveConfig>("Assets/_Project/Configs/Wave_03.asset"),
                    LoadAsset<WaveConfig>("Assets/_Project/Configs/Wave_04.asset")
                });

                serializedSpawner.ApplyModifiedPropertiesWithoutUndo();
            }

            GameObject rewardSystem = new GameObject("Kill Reward System");
            rewardSystem.transform.SetParent(arena, false);
            Transform rewardPointRoot = CreateGroup(rewardSystem.transform, "Reward Points");
            Vector3[] rewardPositions = GetRewardPositions(variant);
            Transform[] rewardPoints = new Transform[rewardPositions.Length];
            for (int i = 0; i < rewardPositions.Length; i++)
            {
                rewardPoints[i] = CreateGroup(rewardPointRoot, $"Reward Point {i + 1}");
                rewardPoints[i].localPosition = rewardPositions[i];
            }

            KillRewardSpawner rewardSpawner = rewardSystem.AddComponent<KillRewardSpawner>();
            rewardSpawner.Configure(
                waveSpawner,
                new[] { 8, 20 },
                new[]
                {
                    LoadAsset<WeaponConfig>("Assets/_Project/Configs/SMGWeaponConfig.asset"),
                    LoadAsset<WeaponConfig>("Assets/_Project/Configs/ShotgunWeaponConfig.asset")
                },
                new[]
                {
                    LoadAsset<GameObject>("Assets/_Project/Art/ThirdParty/TopDownShooterKit/Weapons/SMG.fbx"),
                    LoadAsset<GameObject>("Assets/_Project/Art/ThirdParty/TopDownShooterKit/Weapons/Shotgun.fbx")
                },
                rewardPoints);
        }

        private static Vector3[] GetSpawnPositions(ArenaVariant variant)
        {
            switch (variant)
            {
                case ArenaVariant.ContainerGauntlet:
                    return new[]
                    {
                        new Vector3(-24f, 0.35f, -8f),
                        new Vector3(-24f, 0.35f, 8f),
                        new Vector3(-12f, 0.35f, -20f),
                        new Vector3(0f, 0.35f, -22f),
                        new Vector3(12f, 0.35f, -20f),
                        new Vector3(24f, 0.35f, -8f),
                        new Vector3(24f, 0.35f, 8f),
                        new Vector3(12f, 0.35f, 20f),
                        new Vector3(0f, 0.35f, 22f),
                        new Vector3(-12f, 0.35f, 20f)
                    };
                case ArenaVariant.LastStand:
                    return new[]
                    {
                        new Vector3(-24f, 0.35f, -24f),
                        new Vector3(0f, 0.35f, -26f),
                        new Vector3(24f, 0.35f, -24f),
                        new Vector3(26f, 0.35f, 0f),
                        new Vector3(24f, 0.35f, 24f),
                        new Vector3(0f, 0.35f, 26f),
                        new Vector3(-24f, 0.35f, 24f),
                        new Vector3(-26f, 0.35f, 0f),
                        new Vector3(-18f, 0.35f, 12f),
                        new Vector3(18f, 0.35f, -12f)
                    };
                case ArenaVariant.OvergrownRuins:
                    return new[]
                    {
                        new Vector3(-24f, 0.35f, -16f),
                        new Vector3(-22f, 0.35f, 4f),
                        new Vector3(-15f, 0.35f, 23f),
                        new Vector3(4f, 0.35f, 24f),
                        new Vector3(22f, 0.35f, 13f),
                        new Vector3(24f, 0.35f, -5f),
                        new Vector3(13f, 0.35f, -24f),
                        new Vector3(-6f, 0.35f, -24f)
                    };
                case ArenaVariant.DepotCrossfire:
                    return new[]
                    {
                        new Vector3(-24f, 0.35f, -18f),
                        new Vector3(-24f, 0.35f, 0f),
                        new Vector3(-24f, 0.35f, 18f),
                        new Vector3(-4f, 0.35f, 24f),
                        new Vector3(18f, 0.35f, 24f),
                        new Vector3(24f, 0.35f, 6f),
                        new Vector3(24f, 0.35f, -18f),
                        new Vector3(4f, 0.35f, -24f)
                    };
                default:
                    return new[]
                    {
                        new Vector3(-23f, 0.35f, -20f),
                        new Vector3(0f, 0.35f, -24f),
                        new Vector3(23f, 0.35f, -20f),
                        new Vector3(24f, 0.35f, -6f),
                        new Vector3(24f, 0.35f, 10f),
                        new Vector3(18f, 0.35f, 23f),
                        new Vector3(0f, 0.35f, 24f),
                        new Vector3(-18f, 0.35f, 23f),
                        new Vector3(-24f, 0.35f, 10f),
                        new Vector3(-24f, 0.35f, -6f)
                    };
            }
        }

        private static Vector3[] GetRewardPositions(ArenaVariant variant)
        {
            switch (variant)
            {
                case ArenaVariant.ContainerGauntlet:
                    return new[]
                    {
                        new Vector3(-17f, 0.05f, 0f),
                        new Vector3(0f, 0.05f, 0f),
                        new Vector3(17f, 0.05f, 0f)
                    };
                case ArenaVariant.LastStand:
                    return new[]
                    {
                        new Vector3(-7f, 0.05f, 7f),
                        new Vector3(7f, 0.05f, 7f),
                        new Vector3(-7f, 0.05f, -7f),
                        new Vector3(7f, 0.05f, -7f)
                    };
                case ArenaVariant.OvergrownRuins:
                    return new[]
                    {
                        new Vector3(-10f, 0.05f, 5f),
                        new Vector3(9f, 0.05f, -5f),
                        new Vector3(0f, 0.05f, 12f),
                        new Vector3(0f, 0.05f, -12f)
                    };
                case ArenaVariant.DepotCrossfire:
                    return new[]
                    {
                        new Vector3(-14f, 0.05f, 0f),
                        new Vector3(14f, 0.05f, 0f),
                        new Vector3(0f, 0.05f, 10f),
                        new Vector3(0f, 0.05f, -10f)
                    };
                default:
                    return new[]
                    {
                        new Vector3(-8f, 0.05f, 0f),
                        new Vector3(8f, 0.05f, 0f),
                        new Vector3(0f, 0.05f, 8f),
                        new Vector3(0f, 0.05f, -8f)
                    };
            }
        }

        private static T LoadAsset<T>(string path) where T : Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                throw new InvalidOperationException($"Missing asset: {path}");
            }

            return asset;
        }

        private static void AssignObjectArray<T>(SerializedProperty property, T[] objects) where T : Object
        {
            property.arraySize = objects.Length;
            for (int i = 0; i < objects.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = objects[i];
            }
        }

        private static void RemoveRootObject(string name)
        {
            GameObject existing = GameObject.Find(name);
            if (existing != null)
            {
                Object.DestroyImmediate(existing);
            }
        }

        private static Transform CreateGroup(Transform parent, string name)
        {
            GameObject group = new GameObject(name);
            group.transform.SetParent(parent, false);
            return group.transform;
        }

        private static GameObject Place(
            Transform parent,
            string name,
            string assetName,
            Vector3 position,
            float yaw,
            bool blocking = true)
        {
            string path = $"{EnvironmentPath}/{assetName}";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                throw new InvalidOperationException($"Missing environment asset: {path}");
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.name = name;
            instance.transform.localPosition = position;
            instance.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            instance.transform.localScale = Vector3.one;
            NormalizeScaleAndGround(instance, GetTargetFootprint(assetName), parent.TransformPoint(position).y);

            if (blocking)
            {
                AddGameplayCollider(instance, assetName, GetTargetFootprint(assetName));
            }
            else
            {
                RemoveExistingColliders(instance);
            }

            return instance;
        }

        private static GameObject PlaceGeneric(
            Transform parent,
            string name,
            string assetName,
            Vector3 position,
            float yaw,
            float targetFootprint,
            bool blocking = true)
        {
            string path = $"{GenericEnvironmentPath}/{assetName}";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                throw new InvalidOperationException($"Missing generic environment asset: {path}");
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.name = name;
            instance.transform.localPosition = position;
            instance.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            instance.transform.localScale = Vector3.one;
            NormalizeScaleAndGround(instance, targetFootprint, parent.TransformPoint(position).y);
            ApplyNativeGenericMaterial(instance, assetName);

            if (blocking)
            {
                AddGameplayCollider(instance, assetName, targetFootprint);
            }
            else
            {
                RemoveExistingColliders(instance);
            }

            return instance;
        }

        private static void ApplyNativeGenericMaterial(GameObject target, string assetName)
        {
            Material material = LoadNativeGenericMaterial(assetName);
            if (material == null)
            {
                return;
            }

            foreach (Renderer renderer in target.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.sharedMaterials;
                if (materials == null || materials.Length == 0)
                {
                    renderer.sharedMaterial = material;
                    EditorUtility.SetDirty(renderer);
                    continue;
                }

                for (int i = 0; i < materials.Length; i++)
                {
                    materials[i] = material;
                }

                renderer.sharedMaterials = materials;
                EditorUtility.SetDirty(renderer);
            }
        }

        private static Material LoadNativeGenericMaterial(string assetName)
        {
            string materialName;
            if (ContainsAny(assetName, "Ground_Grass", "River_Grass", "Slope_Grass", "Road", "Ground_Dirt", "River_Dirt", "Ground_Edge", "Slope_Dirt"))
            {
                materialName = currentMaterialProfile.Ground;
            }
            else if (ContainsAny(assetName, "Bush", "Grass", "Fern", "Flowers"))
            {
                materialName = ContainsAny(assetName, "Flowers")
                    ? currentMaterialProfile.Flowers
                    : currentMaterialProfile.Foliage;
            }
            else if (ContainsAny(assetName, "Dirt_Cliff"))
            {
                materialName = currentMaterialProfile.Ground;
            }
            else if (ContainsAny(assetName, "Cliff", "Rock"))
            {
                materialName = currentMaterialProfile.Rock;
            }
            else if (ContainsAny(assetName, "Pipe", "Light", "Beam"))
            {
                materialName = currentMaterialProfile.Metal;
            }
            else if (ContainsAny(assetName, "Barrel_Metal"))
            {
                materialName = currentMaterialProfile.Metal;
            }
            else if (ContainsAny(assetName, "Barrel_Wood"))
            {
                materialName = currentMaterialProfile.Wood;
            }
            else if (ContainsAny(assetName, "Crate", "Box", "Pallet", "Plank"))
            {
                materialName = currentMaterialProfile.Wood;
            }
            else if (ContainsAny(assetName, "Paper"))
            {
                materialName = currentMaterialProfile.Paper;
            }
            else
            {
                materialName = currentMaterialProfile.Default;
            }

            return LoadNativeGenericMaterialByName(materialName);
        }

        private static Material LoadNativeGenericMaterialByName(string materialName)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>($"{GenericMaterialsPath}/{materialName}.mat");
            if (material != null)
            {
                return material;
            }

            return AssetDatabase.LoadAssetAtPath<Material>($"{GenericMaterialsPath}/Alts/{materialName}.mat");
        }

        private static MaterialProfile GetMaterialProfile(ArenaVariant variant)
        {
            switch (variant)
            {
                case ArenaVariant.ContainerGauntlet:
                    return new MaterialProfile
                    {
                        GroundSurface = "Generic_Road",
                        Foliage = "Generic_Ivy",
                        Flowers = "Generic_02_C",
                        Rock = "Generic_Concrete",
                        Ground = "Generic_Road",
                        Metal = "Generic_Concrete",
                        Wood = "Generic_04_B",
                        Paper = "Generic_04_A",
                        Default = "Generic_Road"
                    };
                case ArenaVariant.LastStand:
                    return new MaterialProfile
                    {
                        GroundSurface = "Generic_Dirt",
                        Foliage = "Generic_Ivy",
                        Flowers = "Generic_03_C",
                        Rock = "Generic_Rock",
                        Ground = "Generic_Dirt",
                        Metal = "Generic_Brick",
                        Wood = "Generic_Wood",
                        Paper = "Generic_01_A",
                        Default = "Generic_Brick"
                    };
                case ArenaVariant.OvergrownRuins:
                    return new MaterialProfile
                    {
                        GroundSurface = "Generic_Triplanar_Grass_01",
                        Foliage = "Generic_Triplanar_Grass_01",
                        Flowers = "Generic_03_A",
                        Rock = "Generic_Rock",
                        Ground = "Generic_Dirt",
                        Metal = "Generic_Concrete",
                        Wood = "Generic_Wood",
                        Paper = "Generic_01_C",
                        Default = "Generic_Dirt"
                    };
                case ArenaVariant.DepotCrossfire:
                    return new MaterialProfile
                    {
                        GroundSurface = "Generic_Concrete",
                        Foliage = "Generic_Ivy",
                        Flowers = "Generic_04_C",
                        Rock = "Generic_Plaster",
                        Ground = "Generic_Road",
                        Metal = "Generic_Concrete",
                        Wood = "Generic_04_B",
                        Paper = "Generic_04_A",
                        Default = "Generic_Plaster"
                    };
                default:
                    return new MaterialProfile
                    {
                        GroundSurface = "Generic_Dirt",
                        Foliage = "Generic_Grass",
                        Flowers = "Generic_02_A",
                        Rock = "Generic_Rock",
                        Ground = "Generic_Dirt",
                        Metal = "Generic_Concrete",
                        Wood = "Generic_Wood",
                        Paper = "Generic_01_A",
                        Default = "Generic_Concrete"
                    };
            }
        }

        private static bool ContainsAny(string value, params string[] fragments)
        {
            for (int i = 0; i < fragments.Length; i++)
            {
                if (value.Contains(fragments[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private static float GetTargetFootprint(string assetName)
        {
            switch (assetName)
            {
                case "Container_Long.fbx":
                    return 7.5f;
                case "Container_Small.fbx":
                    return 4.5f;
                case "TrashContainer.fbx":
                    return 3f;
                case "Barrier_Fixed.fbx":
                case "Barrier_Large.fbx":
                    return 4f;
                case "Barrier_Single.fbx":
                    return 2.2f;
                case "SackTrench.fbx":
                    return 4.5f;
                case "Crate.fbx":
                    return 1.8f;
                case "Pallet.fbx":
                    return 2.5f;
                case "ExplodingBarrel.fbx":
                    return 1.2f;
                case "TrafficCone.fbx":
                    return 0.65f;
                default:
                    return 1f;
            }
        }

        private static void NormalizeScaleAndGround(GameObject target, float targetFootprint, float floorY)
        {
            if (!TryGetRendererBounds(target, out Bounds bounds))
            {
                return;
            }

            float currentFootprint = Mathf.Max(bounds.size.x, bounds.size.z);
            if (currentFootprint > Mathf.Epsilon)
            {
                float scale = targetFootprint / currentFootprint;
                target.transform.localScale *= scale;
            }

            if (TryGetRendererBounds(target, out bounds))
            {
                target.transform.position += Vector3.up * (floorY - bounds.min.y);
            }
        }

        private static bool TryGetRendererBounds(GameObject target, out Bounds bounds)
        {
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                bounds = default;
                return false;
            }

            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return true;
        }

        private static void AddGameplayCollider(GameObject target, string assetName, float targetFootprint)
        {
            RemoveExistingColliders(target);

            Vector3 localSize;
            Vector3 localCenter;
            if (IsContainerAsset(assetName) && TryGetLocalRendererBounds(target, out Bounds localBounds))
            {
                localSize = localBounds.size;
                localCenter = localBounds.center;
            }
            else
            {
                Vector3 worldSize = GetGameplayColliderSize(assetName, targetFootprint);
                localSize = WorldSizeToLocalSize(target.transform, worldSize);
                localCenter = new Vector3(0f, localSize.y * 0.5f, 0f);
                if (TryGetLocalRendererBounds(target, out localBounds))
                {
                    localCenter = new Vector3(
                        localBounds.center.x,
                        localBounds.min.y + localSize.y * 0.5f,
                        localBounds.center.z);
                }
            }

            BoxCollider collider = target.AddComponent<BoxCollider>();
            collider.size = localSize;
            collider.center = localCenter;
        }

        private static bool IsContainerAsset(string assetName)
        {
            return assetName.Contains("Container_Long") || assetName.Contains("Container_Small");
        }

        private static bool TryGetLocalRendererBounds(GameObject target, out Bounds bounds)
        {
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
            bounds = default;
            bool hasBounds = false;
            Matrix4x4 rootWorldToLocal = target.transform.worldToLocalMatrix;

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                Bounds localRendererBounds = renderer.localBounds;
                Matrix4x4 rendererLocalToRootLocal = rootWorldToLocal * renderer.transform.localToWorldMatrix;
                EncapsulateLocalPoint(rendererLocalToRootLocal.MultiplyPoint3x4(localRendererBounds.min), ref bounds, ref hasBounds);
                EncapsulateLocalPoint(rendererLocalToRootLocal.MultiplyPoint3x4(localRendererBounds.max), ref bounds, ref hasBounds);
                EncapsulateLocalPoint(rendererLocalToRootLocal.MultiplyPoint3x4(new Vector3(localRendererBounds.min.x, localRendererBounds.min.y, localRendererBounds.max.z)), ref bounds, ref hasBounds);
                EncapsulateLocalPoint(rendererLocalToRootLocal.MultiplyPoint3x4(new Vector3(localRendererBounds.min.x, localRendererBounds.max.y, localRendererBounds.min.z)), ref bounds, ref hasBounds);
                EncapsulateLocalPoint(rendererLocalToRootLocal.MultiplyPoint3x4(new Vector3(localRendererBounds.max.x, localRendererBounds.min.y, localRendererBounds.min.z)), ref bounds, ref hasBounds);
                EncapsulateLocalPoint(rendererLocalToRootLocal.MultiplyPoint3x4(new Vector3(localRendererBounds.min.x, localRendererBounds.max.y, localRendererBounds.max.z)), ref bounds, ref hasBounds);
                EncapsulateLocalPoint(rendererLocalToRootLocal.MultiplyPoint3x4(new Vector3(localRendererBounds.max.x, localRendererBounds.min.y, localRendererBounds.max.z)), ref bounds, ref hasBounds);
                EncapsulateLocalPoint(rendererLocalToRootLocal.MultiplyPoint3x4(new Vector3(localRendererBounds.max.x, localRendererBounds.max.y, localRendererBounds.min.z)), ref bounds, ref hasBounds);
            }

            return hasBounds;
        }

        private static void EncapsulateLocalPoint(Vector3 point, ref Bounds bounds, ref bool hasBounds)
        {
            if (!hasBounds)
            {
                bounds = new Bounds(point, Vector3.zero);
                hasBounds = true;
                return;
            }

            bounds.Encapsulate(point);
        }

        private static Vector3 WorldSizeToLocalSize(Transform target, Vector3 worldSize)
        {
            Vector3 scale = target.lossyScale;
            return new Vector3(
                scale.x == 0f ? worldSize.x : worldSize.x / Mathf.Abs(scale.x),
                scale.y == 0f ? worldSize.y : worldSize.y / Mathf.Abs(scale.y),
                scale.z == 0f ? worldSize.z : worldSize.z / Mathf.Abs(scale.z));
        }

        private static Vector3 GetGameplayColliderSize(string assetName, float targetFootprint)
        {
            if (assetName.Contains("Container_Long"))
            {
                return new Vector3(7.3f, 2.25f, 2.45f);
            }

            if (assetName.Contains("Container_Small"))
            {
                return new Vector3(4.35f, 2f, 2.35f);
            }

            if (assetName.Contains("TrashContainer"))
            {
                return new Vector3(2.8f, 1.35f, 1.8f);
            }

            if (assetName.Contains("Barrier_Large") || assetName.Contains("Barrier_Fixed"))
            {
                return new Vector3(3.8f, 1.15f, 0.55f);
            }

            if (assetName.Contains("Barrier_Single"))
            {
                return new Vector3(2f, 1.05f, 0.45f);
            }

            if (assetName.Contains("SackTrench") || assetName.Contains("Sack"))
            {
                return new Vector3(targetFootprint * 0.9f, 0.8f, targetFootprint * 0.45f);
            }

            if (assetName.Contains("Crate") || assetName.Contains("Box"))
            {
                return new Vector3(targetFootprint * 0.85f, targetFootprint * 0.85f, targetFootprint * 0.85f);
            }

            if (assetName.Contains("Barrel"))
            {
                return new Vector3(targetFootprint * 0.75f, targetFootprint * 1.1f, targetFootprint * 0.75f);
            }

            if (assetName.Contains("Pipe"))
            {
                return new Vector3(targetFootprint * 0.9f, 0.75f, 0.75f);
            }

            if (assetName.Contains("Cliff"))
            {
                return new Vector3(targetFootprint * 0.85f, 2.1f, targetFootprint * 0.85f);
            }

            if (assetName.Contains("Rock"))
            {
                return new Vector3(targetFootprint * 0.75f, 1.2f, targetFootprint * 0.75f);
            }

            return new Vector3(targetFootprint * 0.8f, 1.2f, targetFootprint * 0.8f);
        }

        private static void RemoveExistingColliders(GameObject target)
        {
            Collider[] colliders = target.GetComponentsInChildren<Collider>(true);
            for (int i = colliders.Length - 1; i >= 0; i--)
            {
                if (!colliders[i].isTrigger)
                {
                    Object.DestroyImmediate(colliders[i]);
                }
            }
        }

        private static void AddBoundsCollider(GameObject target)
        {
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return;
            }

            Bounds localBounds = new Bounds(
                target.transform.InverseTransformPoint(renderers[0].bounds.center),
                Vector3.zero);

            foreach (Renderer renderer in renderers)
            {
                Bounds bounds = renderer.bounds;
                Vector3 min = bounds.min;
                Vector3 max = bounds.max;
                for (int x = 0; x <= 1; x++)
                {
                    for (int y = 0; y <= 1; y++)
                    {
                        for (int z = 0; z <= 1; z++)
                        {
                            Vector3 corner = new Vector3(
                                x == 0 ? min.x : max.x,
                                y == 0 ? min.y : max.y,
                                z == 0 ? min.z : max.z);
                            localBounds.Encapsulate(target.transform.InverseTransformPoint(corner));
                        }
                    }
                }
            }

            BoxCollider collider = target.AddComponent<BoxCollider>();
            collider.center = localBounds.center;
            collider.size = localBounds.size;
        }

        private static void RemoveExistingProps(Transform arena)
        {
            for (int i = arena.childCount - 1; i >= 0; i--)
            {
                Transform child = arena.GetChild(i);
                if (child.name == "Props"
                    || child.name.StartsWith("Props_MinimalArena_", StringComparison.Ordinal)
                    || child.name.StartsWith("Props_ExpandedArena_", StringComparison.Ordinal)
                    || child.name.StartsWith("Props_ArenaSceneLayout_", StringComparison.Ordinal)
                    || child.name == "Kill Reward System")
                {
                    Object.DestroyImmediate(child.gameObject);
                }
            }
        }

        private static void RenderPreview()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            const int width = 1280;
            const int height = 720;
            RenderTexture renderTexture = new RenderTexture(width, height, 24);
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture previousTarget = camera.targetTexture;
            Vector3 previousPosition = camera.transform.position;
            Quaternion previousRotation = camera.transform.rotation;

            try
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                Vector3 focusPoint = player == null ? Vector3.zero : player.transform.position;
                camera.transform.position = focusPoint + new Vector3(0f, 42f, -28f);
                camera.transform.rotation = Quaternion.LookRotation(
                    focusPoint - camera.transform.position,
                    Vector3.up);
                camera.targetTexture = renderTexture;
                RenderTexture.active = renderTexture;
                camera.Render();
                texture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                texture.Apply();

                string absolutePath = Path.GetFullPath(PreviewPath);
                Directory.CreateDirectory(Path.GetDirectoryName(absolutePath));
                File.WriteAllBytes(absolutePath, texture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTarget;
                camera.transform.position = previousPosition;
                camera.transform.rotation = previousRotation;
                RenderTexture.active = previousActive;
                Object.DestroyImmediate(texture);
                Object.DestroyImmediate(renderTexture);
            }
        }
    }
}
