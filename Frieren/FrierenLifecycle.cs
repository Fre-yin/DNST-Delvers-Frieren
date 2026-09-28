namespace FrierenPortrait;

internal static class FrierenLifecycle
{
    internal static void ResetSceneState()
    {
        BookloverItemUse.ClearConsumptionAuthorizations();
        FrierenMagicSkills.ResetLifecycle();
        FrierenRecruitment.ResetLifecycle();
        FrierenLocalization.ResetLifecycle();
    }
}
