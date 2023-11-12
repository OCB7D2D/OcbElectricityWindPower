using HarmonyLib;
using System.Reflection;

#pragma warning disable IDE0051 // Remove unused private members

public class ElectricityWindPower : IModApi
{

    // Check if electricity overhaul mod is loaded
    // If so, skip execution of patched methods
    public static bool HasElectricityOverhaul = false;

    // ####################################################################
    // ####################################################################

    public void InitMod(Mod mod)
    {
        Log.Out("OCB Harmony Patch: " + GetType().ToString());
        Harmony harmony = new Harmony(GetType().ToString());
        // ToDo: find a way to avoid patching at all for relevant ones
        HasElectricityOverhaul = ModManager.ModLoaded("OcbElectricityOverhaul");
        harmony.PatchAll(Assembly.GetExecutingAssembly());
        // ModEvents.GameAwake.RegisterHandler(OnGameAwake);
        // HasElectricityOverhaul = false;
    }
    // ####################################################################
    // ####################################################################

    [HarmonyPatch(typeof(PowerSolarPanel), "get_HasLight")]
    public class PowerSolarPanel_GetHasLight
    {
        static void Postfix(PowerSolarPanel __instance, ref bool __result)
        {
            var props = Block.list[__instance.BlockID].Properties;
            if (!props.Values.ContainsKey("IsWindmill")) return;
            __result = true;
        }
    }

    // ####################################################################
    // ####################################################################

    [HarmonyPatch(typeof(BlockPowerSource))]
    [HarmonyPatch("OnBlockEntityTransformAfterActivated")]
    public class BlockPowerSource_OnBlockEntityTransformAfterActivated
    {
        static void Postfix(Vector3i _blockPos, BlockEntityData _ebcd)
        {
            if (!_ebcd.bHasTransform) return;
            if (_ebcd.transform.GetComponentInChildren<ElectricityWindPowerScript>() is ElectricityWindPowerScript script)
            {
                // Tell the script our position in the world
                // Using that as seed for perlin noise
                script.SeedX = _blockPos.x;
                script.SeedZ = _blockPos.z;
            }
        }
    }

    // ####################################################################
    // ####################################################################

    [HarmonyPatch(typeof(BlockPowerSource))]
    [HarmonyPatch("ActivateBlock")]
    public class BlockPowerSource_ActivateBlock
    {
        static void Postfix(BlockPowerSource __instance,
            WorldBase _world, int _cIdx, Vector3i _blockPos, bool isOn)
        {
            var props = Block.list[__instance.blockID].Properties;
            if (!props.Values.ContainsKey("IsWindmill")) return;
            if (_world.ChunkClusters[_cIdx] is ChunkCluster chunkCluster)
            {
                if (chunkCluster.GetChunkSync(
                        World.toChunkXZ(_blockPos.x),
                        World.toChunkY(_blockPos.y),
                        World.toChunkXZ(_blockPos.z)
                    ) is IChunk chunkSync)
                {
                    if (chunkSync.GetBlockEntity(_blockPos) is BlockEntityData blockEntity)
                    {
                        if (blockEntity.transform.GetComponentInChildren<ElectricityWindPowerScript>() is ElectricityWindPowerScript script)
                        {
                            if (isOn) script.Enable();
                            else script.Disable();
                        }
                    }
                }

            }
        }
    }

    // ####################################################################
    // ####################################################################

}
