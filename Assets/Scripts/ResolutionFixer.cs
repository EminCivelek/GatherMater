using UnityEngine;

public static class ResolutionFixer
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void EnsureEvenResolution()
    {
        int w = Screen.width;
        int h = Screen.height;

        int evenW = w % 2 == 0 ? w : w + 1;
        int evenH = h % 2 == 0 ? h : h + 1;

        if (evenW != w || evenH != h)
            Screen.SetResolution(evenW, evenH, Screen.fullScreenMode, Screen.currentResolution.refreshRateRatio);
    }
}
