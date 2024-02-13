using HarmonyLib;
using UnityEngine;

public class ElectricityDegradation
{

    // ####################################################################
    // ####################################################################

    public static float MaxQuality = 6f;

    // ####################################################################
    // ####################################################################

    [HarmonyPatch(typeof(PowerSource), "RefreshPowerStats")]
    public class PowerSourceRefreshPowerStatsPatch
    {
        static bool Prefix(PowerSource __instance)
        {
            __instance.SlotCount = __instance.MaxOutput = 0;
            for (int index = 0; index < __instance.Stacks.Length; ++index)
            {
                var stack = __instance.Stacks[index];
                if (stack.IsEmpty()) continue;
                if (stack.itemValue.MaxUseTimes <= 0 || stack.itemValue.MaxUseTimes > stack.itemValue.UseTimes)
                {
                    __instance.MaxOutput += (ushort)(__instance.OutputPerStack *
                        (double)Mathf.Lerp(0.5f, 1f, stack.itemValue.Quality / MaxQuality));
                }
                __instance.SlotCount++;
            }
            if (__instance.BlockID == 0 && __instance.TileEntity != null)
            {
                __instance.BlockID = (ushort)GameManager.Instance.World
                    .GetBlock(__instance.TileEntity.ToWorldPos()).type;
                __instance.SetValuesFromBlock();
            }
            // Keep it barely functional if all slots are degraded
            // Otherwise you can't turn it off to take them out ...
            if (__instance.SlotCount != 0 && __instance.MaxOutput == 0)
                __instance.MaxOutput = __instance.SlotCount;
            if (__instance.MaxPower == 0) // this is required!
                __instance.MaxPower = __instance.MaxOutput;
            if (__instance.RequiredPower != 0) return false;
            __instance.RequiredPower = __instance.MaxOutput;
            return false;
        }
    }

    // ####################################################################
    // ####################################################################

    [HarmonyPatch(typeof(PowerSolarPanel), "HandleSendPower")]
    public class PowerSolarPanelHandleSendPowerPatch
    {

        static readonly float Chance = 0.005f;
        static readonly float Degrade = 15f;

        static void Prefix(PowerSolarPanel __instance,
            ref bool ___hasChangesLocal)
        {
            if (__instance.IsOn == false) return;
            // if (__instance.LastPowerUsed <= 0) return;
            bool changed = false; bool recalc = false;
            for (int i = 0; i < __instance.Stacks.Length; i++)
            {
                ItemStack slot = __instance.Stacks[i];
                if (slot.IsEmpty()) continue;
                var iv = slot.itemValue;
                if (!iv.HasQuality) continue;
                if (iv.MaxUseTimes <= 0) continue;
                if (iv.UseTimes >= iv.MaxUseTimes) continue;
                if (Random.value > Chance) continue;
                int reduce = (int)(Degrade * Random.value) + 1;
                iv.UseTimes = Mathf.Min(iv.UseTimes + reduce, iv.MaxUseTimes);
                changed = true; recalc |= iv.UseTimes >= iv.MaxUseTimes;
            }
            if (recalc) __instance.SetSlots(__instance.Stacks);
            if (changed) __instance.TileEntity?.MarkChanged();
            ___hasChangesLocal |= recalc;
        }
    }

    // ####################################################################
    // ####################################################################

}
