using System.Collections.Generic;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine.Group;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine.Node;

namespace AnoMech.Core.Native.Implementations.Interop;

// Whether a native SharedGroup (MapEffects' arena pieces) has actually finished streaming in on
// this client.
internal static unsafe class LayoutInstanceDiagnostics
{
    // HavePrimary/IsPrimaryLoaded/IsPrimaryReady only say the object loaded; IsActive/
    // WantToBeActive are the activation state, and GetGraphics() null despite IsPrimaryReady
    // means the renderable scene object was never created.
    public static string Describe(uint layoutId)
    {
        var world = LayoutWorld.Instance();
        if (world == null) return "LayoutWorld null";
        var instance = world->GetLayoutInstance(InstanceType.SharedGroup, layoutId);
        if (instance == null) return "GetLayoutInstance returned null (no such SharedGroup instance on this client)";
        return Describe((SharedGroupLayoutInstance*)instance);
    }

    // For an instance already held by pointer (an EObj's own actor+0x108, which has no layout id).
    public static string Describe(SharedGroupLayoutInstance* sg)
    {
        if (sg == null) return "SharedGroupLayoutInstance null (SG context not attached to the actor yet)";
        var instance = (ILayoutInstance*)sg;
        var layer = instance->Layer;
        var layerDesc = layer == null ? "Layer=null" : $"Layer=0x{layer->LayerGroupId:X}(flags=0x{layer->Flags:X})";
        var idDesc = $"Id={instance->Id.Type}/{instance->Id.LayerKey:X}:{instance->Id.InstanceKey:X} SubId=0x{instance->SubId:X} Flags1-3=0x{instance->Flags1:X}/0x{instance->Flags2:X}/0x{instance->Flags3:X}";
        var graphics = instance->GetGraphics();
        var graphics2 = instance->GetGraphics2();
        var timelinePlaying = sg->IsTimelinePlaying(sg->PlayingTimelineIndex);
        // The parent's Graphics can read null while it renders fine: the mesh usually belongs to
        // child instances (a prefab's BgParts), so children are listed too, capped at 16 (the
        // engine's FixedSizeArray16) and one level deep. A timeline-driven SGB (the statue
        // props) with children active but nothing visible points at the timelines.
        var timelineCount = sg->TimeLineContainer.Instances.Count;
        var playing = new List<int>();
        for (var i = 0; i < 16; i++)
            if (sg->IsTimelineIndexValid((uint)i) && sg->IsTimelinePlaying((uint)i)) playing.Add(i);
        var timelineDesc = $"Timelines={timelineCount} playing=[{string.Join(",", playing)}]";
        var childCount = sg->Instances.Instances.Count;
        var childSummaries = new List<string>();
        for (var i = 0; i < childCount && i < 16; i++)
        {
            var child = (ChildNodeInstance*)sg->Instances.Instances[i].Value;
            var childInstance = child != null ? child->Instance : null;
            childSummaries.Add($"[{i}]={FormatChild(childInstance, depth: 1)}");
        }
        return $"{layerDesc} {idDesc} HavePrimary={instance->HavePrimary()} IsPrimaryLoaded={instance->IsPrimaryLoaded()} IsPrimaryReady={instance->IsPrimaryReady()} "
             + $"IsActive={instance->IsActive} WantToBeActive={instance->WantToBeActive()} Graphics=0x{(nint)graphics:X} Graphics2=0x{(nint)graphics2:X} "
             + $"TimelineObject=0x{(nint)sg->TimelineObject:X} PlayingTimelineIndex=0x{sg->PlayingTimelineIndex:X} IsTimelinePlaying={timelinePlaying} {timelineDesc} "
             + $"PrefabFlags1=0x{sg->PrefabFlags1:X} PrefabFlags2=0x{sg->PrefabFlags2:X} ChildCount={childCount} Children=[{string.Join("; ", childSummaries)}]";
    }

    public static string DescribeLive(uint layoutId)
    {
        var world = LayoutWorld.Instance();
        if (world == null || layoutId == 0) return "no SharedGroup";
        var instance = world->GetLayoutInstance(InstanceType.SharedGroup, layoutId);
        if (instance == null) return "SharedGroup not loaded";
        var live = new List<string>();
        CollectLive(instance, 3, live);
        return $"SG active={instance->IsActive} \"{PrimaryPath(instance)}\" live[{live.Count}]=[{string.Join("; ", live)}]";
    }

    public static void CollectLiveEffects(ILayoutInstance* inst, int depth, List<string> into)
    {
        if (inst == null || !inst->IsActive) return;
        if (inst->Id.Type == InstanceType.Vfx) { into.Add(DescribeVfx(inst)); return; }
        if (inst->Id.Type == InstanceType.Light) { into.Add(DescribeLight(inst)); return; }
        if (depth <= 0 || inst->Id.Type != InstanceType.SharedGroup) return;
        var sg = (SharedGroupLayoutInstance*)inst;
        var count = sg->Instances.Instances.Count;
        for (var i = 0; i < count && i < 16; i++)
        {
            var child = (ChildNodeInstance*)sg->Instances.Instances[i].Value;
            if (child != null) CollectLiveEffects(child->Instance, depth - 1, into);
        }
    }

    private static void CollectLive(ILayoutInstance* sgInstance, int depth, List<string> into)
    {
        var sg = (SharedGroupLayoutInstance*)sgInstance;
        var count = sg->Instances.Instances.Count;
        for (var i = 0; i < count && i < 16; i++)
        {
            var child = (ChildNodeInstance*)sg->Instances.Instances[i].Value;
            var inst = child != null ? child->Instance : null;
            if (inst == null || !inst->IsActive) continue;
            if (inst->Id.Type == InstanceType.SharedGroup && depth > 0) { CollectLive(inst, depth - 1, into); continue; }
            into.Add(inst->Id.Type switch
            {
                InstanceType.Vfx => DescribeVfx(inst),
                InstanceType.Light => DescribeLight(inst),
                _ => $"{inst->Id.Type} \"{PrimaryPath(inst)}\"",
            });
        }
    }

    private static string DescribeVfx(ILayoutInstance* inst)
    {
        var vfx = (FFXIVClientStructs.FFXIV.Client.LayoutEngine.Layer.VfxLayoutInstance*)inst;
        var c = vfx->Color;
        return $"Vfx \"{PrimaryPath(inst)}\" rgba=({c.R},{c.G},{c.B},{c.A}) drawn={vfx->GraphicsObject != null}";
    }

    private static string DescribeLight(ILayoutInstance* inst)
    {
        var light = (FFXIVClientStructs.FFXIV.Client.LayoutEngine.Layer.LightLayoutInstance*)inst;
        var c = light->Color;
        return $"Light {light->LightType} rgb=({c.X:F2},{c.Y:F2},{c.Z:F2}) w={c.W:F2} drawn={light->GraphicsObject != null}";
    }

    private static string PrimaryPath(ILayoutInstance* inst)
    {
        var path = inst->GetPrimaryPath();
        return path.HasValue ? path.ToString() : "";
    }

    private static string FormatChild(ILayoutInstance* childInstance, int depth)
    {
        if (childInstance == null) return "null";
        var childGraphics = childInstance->GetGraphics();
        var summary = $"Type={childInstance->Id.Type} HavePrimary={childInstance->HavePrimary()} "
            + $"IsPrimaryLoaded={childInstance->IsPrimaryLoaded()} IsActive={childInstance->IsActive} Graphics=0x{(nint)childGraphics:X}";
        // The BgPart's model path is what identifies an arena piece; nothing else on the struct
        // says what loaded.
        if (childInstance->Id.Type == InstanceType.BgPart && childGraphics != null)
        {
            var bgObject = (BgObject*)childGraphics;
            var handle = bgObject->ModelResourceHandle;
            summary += handle != null ? $" ModelPath=\"{handle->FileName}\"" : " ModelPath=(no ModelResourceHandle)";
        }
        if (depth <= 0 || childInstance->Id.Type != InstanceType.SharedGroup) return summary;
        var nested = (SharedGroupLayoutInstance*)childInstance;
        var nestedCount = nested->Instances.Instances.Count;
        var nestedSummaries = new List<string>();
        for (var i = 0; i < nestedCount && i < 16; i++)
        {
            var nestedChild = (ChildNodeInstance*)nested->Instances.Instances[i].Value;
            var nestedInstance = nestedChild != null ? nestedChild->Instance : null;
            nestedSummaries.Add($"[{i}]={FormatChild(nestedInstance, depth: depth - 1)}");
        }
        return $"{summary} NestedChildCount={nestedCount} NestedChildren=[{string.Join("; ", nestedSummaries)}]";
    }
}
