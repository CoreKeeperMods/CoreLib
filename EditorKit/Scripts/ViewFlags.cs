using System;

namespace EditorKit.Scripts
{
    [Flags]
    public enum ViewFlags
    {
        None = 0,
        AdaptiveTop = 1,
        AdaptiveBottom = 2,
        SkewBack = 4,
        SkewFront = 8
    }
}