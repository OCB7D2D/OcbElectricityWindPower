using HarmonyLib;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static ElectricityWindPowerScript;

public class ElectricityGenerating
{

    // ####################################################################
    // ####################################################################

    private static float TimeWindFactor(World world, Vector3i position)
    {
        // Sample in y direction
        float speed = Mathf.PerlinNoise(
            position.z * WorldPerlinScale,
            position.x * WorldPerlinScale +
            world.worldTime * PerlinSpeedFactor);
        // Smooth the edges a little bit
        speed = Mathf.SmoothStep(0, 1, speed);
        // Get the height of the regular terrain at position
        var height = world.GetTerrainHeight(position.x, position.z);
        // if y is 10 below world height, factor will be zero
        speed *= Mathf.Lerp(0, 1, Mathf.InverseLerp(height - 10, height, position.y));
        // if y is above 50 up to 125 we gain a bonus up to 25%
        speed *= Mathf.Lerp(1, 1.25f, Mathf.InverseLerp(50, 125, position.y));
        // Make sure to return a minimum and in defined quantums
        return Mathf.Max((int)(speed * 12f) / 12f, 0.05f);
    }

    // ####################################################################
    // ####################################################################

    private static float TimeLightFactor(World world, int x)
    {
        // add small static world position offset to offset big updates
        float time = ((world.worldTime + x * 0.1f) % 24000UL) / 1000f;
        float noon = (world.DawnHour + world.DuskHour) * 0.5f;
        float range = (world.DuskHour - world.DawnHour) * 0.5f;
        float factor = 1 - (range - Mathf.Abs(noon - time)) / range;
        factor = Mathf.Clamp01((1f - factor * factor) * 2f);
        return Mathf.Max((int)(factor * 12f) / 12f, 0.05f);
    }

    // ####################################################################
    // ####################################################################

    [HarmonyPatch(typeof(PowerSolarPanel), "CheckLightLevel")]
    public class PowerSolarPanelCheckLightLevelPatch
    {

        private static readonly HarmonyPropertyProxy<bool> HasLight =
            new HarmonyPropertyProxy<bool>(typeof(PowerSolarPanel), "HasLight");

        // Check for optional field (Electricity Overhaul defines and uses this)
        static readonly FieldInfo FieldLightLevel = AccessTools
            .TypeByName(nameof(PowerSolarPanel))?.GetFields()?
            .FirstOrDefault(field => field.Name == "LightLevel");

        // private static readonly MethodInfo MethodHandleOnOffSound =
        //     AccessTools.Method(typeof(PowerSolarPanel), "HandleOnOffSound");

        static bool Prefix(PowerSolarPanel __instance,
            ref float ___lightUpdateTime, ref byte ___sunLight,
            ref bool ___lastHasLight, ref bool ___hasChangesLocal)
        {
            Log.Out("Into check ligh level");
            // Wait a bit longer to update wind/sun
            // We may send changes often over the wire
            // Add a very slight variation to ensure we don't all
            // get called again on the same frame, distribute the
            // load across different frame updates (just to be sure).
            ___lightUpdateTime = Time.time + Random.Range(15.5f, 16.5f);

            // Get dynamic block light if chunk and tile is loaded
            if (__instance.TileEntity?.GetChunk() is Chunk chunk)
            {
                Vector3i pos = __instance.TileEntity.localChunkPos;
                ___sunLight = chunk.GetLight(pos.x, pos.y, pos.z, Chunk.LIGHT_TYPE.SUN);
            }

            // Set the same stuff as vanilla has
            ___lastHasLight = __instance.HasLight;
            HasLight.Set(__instance, ___sunLight == 15);

            // Get dynamic factor for wind (perlin-noise) or sun-light (from chunk light)
            float factor = Block.list[__instance.BlockID].Properties.Values.ContainsKey("IsWindmill")
                ? TimeWindFactor(GameManager.Instance.World, __instance.Position)
                : TimeLightFactor(GameManager.Instance.World, __instance.Position.x);

            // Update optional field (electricity overhaul)
            if (ElectricityWindPower.HasElectricityOverhaul && FieldLightLevel != null)
                FieldLightLevel.SetValue(__instance, (ushort)(factor * ushort.MaxValue));

            Log.Out("--- Check light level {0} - {1} - {2}", ElectricityWindPower.HasElectricityOverhaul, FieldLightLevel, factor);

            // We keep the power generation to a minimum of 1 watts
            // This will make sure we can always switch it off
            ushort power = (ushort)(Mathf.Max(__instance.MaxPower * factor, 1f));

            // Update power output if required
            if (__instance.MaxOutput != power)
            {
                __instance.MaxOutput = power;
                __instance.TileEntity?.MarkChanged();
                // __instance.SendHasLocalChangesToRoot()
                ___hasChangesLocal |= true;
            }

            // if (___lastHasLight == __instance.HasLight) return false;
            // MethodHandleOnOffSound.Invoke(__instance, null);
            // if (!__instance.HasLight)
            // {
            //     __instance.CurrentPower = (ushort)0;
            //     __instance.HandleDisconnect();
            // }
            // else __instance.SendHasLocalChangesToRoot();

            return false;
        }
    }

    // ####################################################################
    // ####################################################################

}
