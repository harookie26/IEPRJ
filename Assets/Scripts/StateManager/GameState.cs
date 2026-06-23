public static class GameState
{
    private static int activeCutsceneCount;

    public static bool IsCutsceneActive => activeCutsceneCount > 0;

    // Returns true only when this begins the first active cutscene.
    public static bool BeginCutscene()
    {
        activeCutsceneCount++;
        return activeCutsceneCount == 1;
    }

    // Returns true only when this ends the final active cutscene.
    public static bool EndCutscene()
    {
        activeCutsceneCount = System.Math.Max(0, activeCutsceneCount - 1);
        return activeCutsceneCount == 0;
    }
}
