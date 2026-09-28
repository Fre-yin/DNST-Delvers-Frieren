#if BEPINEX
using global::Refactor;
#else
using Il2CppRefactor;
#endif
#if BEPINEX
using global::Refactor.Util;
#else
using Il2CppRefactor.Util;
#endif
#if BEPINEX
using global::Util;
#else
using Il2CppUtil;
#endif
#if BEPINEX
using global::Util.Sheet;
#else
using Il2CppUtil.Sheet;
#endif

namespace FrierenPortrait;

internal static class FrierenIds
{
    internal const string PortraitKey = "Danny_Frieren_ElfFemale";
    // Keep the original gameplay/profile template and serialized custom ID.
    // Only the head hair and eye color come from the user's "Def Not Frieren".
    internal const string TemplateKey = "UNITVISUAL_ElfSlimUnisex_10";
    internal const string VisualSourceKey = "UNITVISUAL_ElfSlimUnisex_12";
    internal const string ProfileKey = FrierenUniqueCandidateRules.ProfileKey;
    internal const HairType CustomHairType = (HairType)10086;
}
