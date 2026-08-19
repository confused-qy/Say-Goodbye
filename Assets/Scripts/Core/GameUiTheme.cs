using UnityEngine;

namespace SayGoodbye.Core
{
    public static class GameUiTheme
    {
        public static readonly Color Ink = new Color(0.035f, 0.047f, 0.063f, 1f);
        public static readonly Color Panel = new Color(0.07f, 0.085f, 0.10f, 0.96f);
        public static readonly Color Current = new Color(0.86f, 0.57f, 0.25f, 1f);
        public static readonly Color Unlocked = new Color(0.16f, 0.52f, 0.48f, 1f);
        public static readonly Color Locked = new Color(0.25f, 0.28f, 0.30f, 1f);
        public static readonly Color Pending = new Color(0.50f, 0.36f, 0.21f, 1f);
        public static readonly Color MutedText = new Color(0.68f, 0.72f, 0.73f, 1f);

        private static Font chineseFont;

        public static Font ChineseFont
        {
            get
            {
                if (chineseFont == null)
                {
                    chineseFont = Resources.Load<Font>("Fonts/SayGoodbyeChineseSubset");
                    if (chineseFont == null)
                    {
                        chineseFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    }
                }

                return chineseFont;
            }
        }
    }
}
