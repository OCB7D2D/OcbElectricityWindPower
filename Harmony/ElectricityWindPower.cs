using HarmonyLib;
using UnityEngine;
using System.Reflection;
using static ElectricityWindPowerScript;

#pragma warning disable IDE0051 // Remove unused private members

public class ElectricityWindPower : IModApi
{

    public void InitMod(Mod mod)
    {
        Log.Out("Loading OCB Electricity Windmill Patch: " + GetType().ToString());

        var harmony = new Harmony(GetType().ToString());
        harmony.PatchAll(Assembly.GetExecutingAssembly());
        ModEvents.GameAwake.RegisterHandler(GameAwakeHandler);
    }
    public static void GameAwakeHandler()
    {
    }

    [HarmonyPatch(typeof(PowerSolarPanel))]
    [HarmonyPatch("get_HasLight")]
    public class PowerSolarPanel_GetHasLight
    {
        static void Postfix(PowerSolarPanel __instance,
            ref bool __result)
        {
            var props = Block.list[__instance.BlockID].Properties;
            if (!props.Values.ContainsKey("IsWindmill")) return;
            __result = true;
        }
    }

    [HarmonyPatch(typeof(PowerSolarPanel))]
    [HarmonyPatch("CheckLightLevel")]
    public class PowerSolarPanel_CheckLightLevel
    {
        static void Postfix(
            PowerSolarPanel __instance,
            ref ushort ___MaxPower,
            ref ushort ___MaxOutput,
            ref ushort ___CurrentPower,
            ref float ___lightUpdateTime)
        {
            // Only apply logic to windmills
            var props = Block.list[__instance.BlockID].Properties;
            if (!props.Values.ContainsKey("IsWindmill")) return;
            World world = GameManager.Instance.World;

            // Wait quite a bit longer to update wind
            // We may send changes often over the wire
            if (GameManager.IsDedicatedServer)
                ___lightUpdateTime += 28f;

            // Add a very slight variation to ensure we don't all
            // get called again on the same frame, distribute the
            // load across different frame updates (just to be sure).
            ___lightUpdateTime += Random.Range(-0.1f, 0.1f);

            // Do some Perlin noise sampling, we basically move from
            // left to right and from top to bottom in the 2d noise
            // The factors define how fast values change over time

            // Sample in y direction
            float speed = Mathf.PerlinNoise(
                __instance.Position.z * WorldPerlinScale,
                __instance.Position.x * WorldPerlinScale
                + world.worldTime * PerlinSpeedFactor);

            // Calculate max output power for the current wind situation
            // Current power always matches for solar sources
            ushort lastMaxOutput = ___MaxOutput;
            ___MaxOutput = (ushort)(___MaxPower * speed);
            ___CurrentPower = ___MaxOutput;
            if (lastMaxOutput == ___MaxOutput) return;
            // Probably causing a lot of traffic!?
            __instance.SendHasLocalChangesToRoot();
        }
    }

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


    // BELOW ARE ONLY CORE ELECTRICITY BUGFIXES, MOVE THESE TO OUR BUGFIXE REPO?

    [HarmonyPatch(typeof(ItemActionConnectPower))]
    [HarmonyPatch("OnHoldingUpdate")]
    public class ItemActionConnectPower_OnHoldingUpdate
    {
        static void Prefix(ref ItemActionData _actionData)
        {
            if (_actionData.invData.hitInfo.bHitValid)
            {
                // Fix hit position if block is a child of a multi-dim block
                BlockValue block = _actionData.invData.hitInfo.hit.blockValue;
                if (!Block.list[block.type].Properties.Values.ContainsKey("IsWindmill")) return;
                if (block.ischild)
                {
                    _actionData.invData.hitInfo.hit.blockPos += block.parent;
                    _actionData.invData.hitInfo.hit.blockValue = 
                        _actionData.invData.world.GetBlock(
                            _actionData.invData.hitInfo.hit.blockPos);
                }
            }
        }
    }

    [HarmonyPatch(typeof(ItemActionDisconnectPower))]
    [HarmonyPatch("OnHoldingUpdate")]
    public class ItemActionDisconnectPower_OnHoldingUpdate
    {
        static void Prefix(ref ItemActionData _actionData)
        {
            if (_actionData.invData.hitInfo.bHitValid)
            {
                // Fix hit position if block is a child of a multi-dim block
                BlockValue block = _actionData.invData.hitInfo.hit.blockValue;
                if (!Block.list[block.type].Properties.Values.ContainsKey("IsWindmill")) return;
                if (block.ischild)
                {
                    _actionData.invData.hitInfo.hit.blockPos += block.parent;
                    _actionData.invData.hitInfo.hit.blockValue =
                        _actionData.invData.world.GetBlock(
                            _actionData.invData.hitInfo.hit.blockPos);

                }
            }
        }
    }

    // Let's hope that the way of calling methods this way doesn't have too much overhead!
    static readonly FieldInfo FieldIsPowered = AccessTools.Field(typeof(PowerItem), "isPowered");
    static readonly FieldInfo FieldHasChangesLocal = AccessTools.Field(typeof(PowerItem), "hasChangesLocal");
    static readonly MethodInfo MethodIsPoweredChanged = AccessTools.Method(typeof(PowerItem), "IsPoweredChanged");
    static readonly MethodInfo HandleParentTriggering = AccessTools.Method(typeof(PowerTrigger), "HandleParentTriggering");
    static readonly MethodInfo MethodAddPowerToBatteries = AccessTools.Method(typeof(PowerBatteryBank), "AddPowerToBatteries");
    static readonly MethodInfo MethodPowerTriggerCheckForActiveChange = AccessTools.Method(typeof(PowerTrigger), "CheckForActiveChange");
    static readonly MethodInfo MethodPowerTimerRelayCheckForActiveChange = AccessTools.Method(typeof(PowerTimerRelay), "CheckForActiveChange");

    // Check if electricity overhaul mod was already applied, if so, skip execution of patched methods (ToDo: find a way to avoid patching at all in that case!?)
    static readonly bool HasElectricityOverhaul = AccessTools.FindIncludingBaseTypes(typeof(PowerSource), t => t.GetField("LentConsumed", AccessTools.all)) != null;

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
                MethodIsPoweredChanged.Invoke(__instance, new object[]{ newPowered });
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
                .Invoke(__instance, new object[] {});
            if (!__instance.PowerChildren())
                return false;
            for (int index = 0; index < __instance.Children.Count; ++index)
            {
                __instance.Children[index].HandlePowerReceived(ref power);
            }
            return false;
        }
    }

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
}
