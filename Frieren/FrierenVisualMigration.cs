using Il2CppInterop.Runtime;
#if BEPINEX
using global::Refactor;
#else
using Il2CppRefactor;
#endif
#if BEPINEX
using global::Refactor.Component;
#else
using Il2CppRefactor.Component;
#endif

namespace FrierenPortrait;

// Earlier test builds copied the blue eyes of profile 10. The user's original
// "Def Not Frieren" is profile 12 with green eyes. The custom hair ID stays
// stable; its three sprites now contain the lightened profile-12 hair shape.
internal static class FrierenVisualMigration
{
    internal static bool Normalize(UnitProfileComponentSaveData saved)
    {
        if (saved == null || saved.ProfileKey != FrierenIds.ProfileKey
            || saved.HairType != FrierenIds.CustomHairType
            || saved.EyesType != EyesType.Blue) return false;
        saved.EyesType = EyesType.Green;
        return true;
    }
}
