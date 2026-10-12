namespace AnoMech.Core.Native.Interfaces;

// The zone's LGB layout instances.
public interface ILayoutFunctions
{
    // A one-shot SetActive(false) is reconciled back by the engine within a frame or two, so the
    // layer's instances are held inactive by ReassertSuppressedLayers each tick until cleared.
    // Returns how many instances the layer has.
    int SuppressLayer(ushort layerKey);
    void ReassertSuppressedLayers();
    void ClearSuppressedLayers();

    string DescribeActiveLayers();
    string DescribeLiveEffects();
}
