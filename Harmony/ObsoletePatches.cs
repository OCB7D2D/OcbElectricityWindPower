using HarmonyLib;
using System.Reflection;
using UnityEngine;

#pragma warning disable IDE0051 // Remove unused private members

// Patches no longer used after A21 refactoring
// Only kept in case we find they are still needed
// Can be removed once compatibility was stabilized

public class ObsoletePatches
{


    // Let's hope that the way of calling methods this way doesn't have too much overhead!
    static readonly FieldInfo FieldIsPowered = AccessTools.Field(typeof(PowerItem), "isPowered");
    static readonly FieldInfo FieldHasChangesLocal = AccessTools.Field(typeof(PowerItem), "hasChangesLocal");
    static readonly MethodInfo MethodIsPoweredChanged = AccessTools.Method(typeof(PowerItem), "IsPoweredChanged");
    static readonly MethodInfo HandleParentTriggering = AccessTools.Method(typeof(PowerTrigger), "HandleParentTriggering");
    static readonly MethodInfo MethodAddPowerToBatteries = AccessTools.Method(typeof(PowerBatteryBank), "AddPowerToBatteries");
    static readonly MethodInfo MethodPowerTriggerCheckForActiveChange = AccessTools.Method(typeof(PowerTrigger), "CheckForActiveChange");
    static readonly MethodInfo MethodPowerTimerRelayCheckForActiveChange = AccessTools.Method(typeof(PowerTimerRelay), "CheckForActiveChange");

    /*
    [HarmonyPriority(Priority.Low)]
    [HarmonyPatch(typeof(PowerBatteryBank))]
    [HarmonyPatch("HandlePowerReceived")]
    public class PowerBatteryBank_HandlePowerReceived
    {
        static bool Prefix(PowerBatteryBank __instance, ref ushort power)
        {
            if (HasElectricityOverhaul) return false;
            __instance.LastPowerUsed = 0;
            if (__instance.LastPowerReceived != power)
            {
                __instance.LastPowerReceived = power;
                FieldHasChangesLocal.SetValue(__instance, true);
                for (int index = 0; index < __instance.Children.Count; ++index)
                    __instance.Children[index].HandleDisconnect();
            }
            if (__instance.IsOn && power > 0)
            {
                MethodAddPowerToBatteries
                    .Invoke(__instance, new object[] { Mathf.Min(__instance.InputPerTick, power) });
                power -= __instance.LastInputAmount;
            }
            if (!__instance.PowerChildren())
                return false;
            for (int index = 0; index < __instance.Children.Count; ++index)
            {
                __instance.Children[index].HandlePowerReceived(ref power);
            }
            return false;
        }
    }
    */
    /*
    [HarmonyPriority(Priority.Low)]
    [HarmonyPatch(typeof(PowerConsumerToggle))]
    [HarmonyPatch("HandlePowerReceived")]
    public class PowerConsumerToggle_HandlePowerReceived
    {
        static bool Prefix(PowerConsumerToggle __instance, ref ushort power)
        {
            if (HasElectricityOverhaul) return false;
            ushort num = (ushort)Mathf.Min(__instance.RequiredPower, power);
            bool newPowered = num == __instance.RequiredPower;

            if (newPowered != (bool)FieldIsPowered.GetValue(__instance))
            {
                FieldIsPowered.SetValue(__instance, newPowered);
                MethodIsPoweredChanged.Invoke(__instance, new object[] { newPowered });
                // __instance.IsPoweredChanged(newPowered);
                if (__instance.TileEntity != null)
                    __instance.TileEntity.SetModified();
            }
            power -= num;
            if (!__instance.PowerChildren())
                return false;
            for (int index = 0; index < __instance.Children.Count; ++index)
            {
                __instance.Children[index].HandlePowerReceived(ref power);
            }
            return false;
        }
    }
    */
    /*
    [HarmonyPriority(Priority.Low)]
    [HarmonyPatch(typeof(PowerItem))]
    [HarmonyPatch("HandlePowerReceived")]
    public class PowerItem_HandlePowerReceived
    {
        static bool Prefix(PowerItem __instance, ref ushort power,
            ref bool ___isPowered)
        {
            if (HasElectricityOverhaul) return false;
            ushort consume = (ushort)Mathf.Min(__instance.RequiredPower, power);
            bool newPowered = consume == __instance.RequiredPower;
            if (newPowered != ___isPowered)
            {
                ___isPowered = newPowered;
                MethodIsPoweredChanged.Invoke(__instance, new object[] { newPowered });
                // __instance.IsPoweredChanged(newPowered);
                if (__instance.TileEntity != null)
                    __instance.TileEntity.SetModified();
            }
            power -= consume;
            if (!__instance.PowerChildren())
                return false;
            for (int index = 0; index < __instance.Children.Count; ++index)
            {
                __instance.Children[index].HandlePowerReceived(ref power);
            }
            return false;
        }
    }
    */

    /*
    [HarmonyPriority(Priority.Low)]
    [HarmonyPatch(typeof(PowerTimerRelay))]
    [HarmonyPatch("HandlePowerReceived")]
    public class PowerTimerRelay_HandlePowerReceived
    {
        static bool Prefix(PowerTimerRelay __instance, ref ushort power)
        {
            if (HasElectricityOverhaul) return false;
            ushort consume = (ushort)Mathf.Min(__instance.RequiredPower, power);
            FieldIsPowered.SetValue(__instance, consume == __instance.RequiredPower);
            power -= consume;
            MethodPowerTimerRelayCheckForActiveChange
                .Invoke(__instance, new object[] { });
            if (!__instance.PowerChildren())
                return false;
            for (int index = 0; index < __instance.Children.Count; ++index)
            {
                __instance.Children[index].HandlePowerReceived(ref power);
            }
            return false;
        }
    }
    */
    /*
    [HarmonyPriority(Priority.Low)]
    [HarmonyPatch(typeof(PowerTrigger))]
    [HarmonyPatch("HandlePowerReceived")]
    public class PowerTrigger_HandlePowerReceived
    {
        static bool Prefix(PowerTimerRelay __instance, ref ushort power)
        {
            if (HasElectricityOverhaul) return false;
            ushort consume = (ushort)Mathf.Min(__instance.RequiredPower, power);
            FieldIsPowered.SetValue(__instance, consume == __instance.RequiredPower);
            power -= consume;
            MethodPowerTriggerCheckForActiveChange
                .Invoke(__instance, new object[] { });
            if (!__instance.PowerChildren())
                return false;
            for (int index = 0; index < __instance.Children.Count; ++index)
            {
                if (__instance.Children[index] is PowerTrigger)
                {
                    PowerTrigger child = __instance.Children[index] as PowerTrigger;
                    HandleParentTriggering
                        .Invoke(__instance, new object[] { child });
                    if ((__instance.TriggerType == PowerTrigger.TriggerTypes.Motion || __instance.TriggerType == PowerTrigger.TriggerTypes.PressurePlate
                        || __instance.TriggerType == PowerTrigger.TriggerTypes.TripWire) && (child.TriggerType == PowerTrigger.TriggerTypes.Motion
                        || child.TriggerType == PowerTrigger.TriggerTypes.PressurePlate || child.TriggerType == PowerTrigger.TriggerTypes.TripWire))
                        child.HandlePowerReceived(ref power);
                    else if (__instance.IsActive)
                        child.HandlePowerReceived(ref power);
                }
                else if (__instance.IsActive)
                    __instance.Children[index].HandlePowerReceived(ref power);
            }
            return false;
        }
    }
    */


}
