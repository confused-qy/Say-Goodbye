namespace SayGoodbye.Core
{
    public static class SceneCatalog
    {
        public const string Boot = "00_Boot";
        public const string Prologue = "01_Prologue";
        public const string Hospital = "02_Hospital";
        public const string LivingRoom = "03_LivingRoom";
        public const string Bedroom = "04_Bedroom";
        public const string Corridor = "05_Corridor";
        public const string Kitchen = "06_Kitchen";
        public const string Guitar = "10_Guitar";
        public const string Makeup = "11_Makeup";
        public const string Sunflower = "12_Sunflower";
        public const string Cooking = "13_Cooking";
        public const string FamilyPuzzle = "14_FamilyPuzzle";
        public const string EndingComic = "20_EndingComic";
        public const string Epilogue = "21_Epilogue";
        public const string GameComplete = "22_GameComplete";

        public static string ForChapter(GameChapter chapter)
        {
            switch (chapter)
            {
                case GameChapter.WishOne:
                case GameChapter.WishTwo:
                case GameChapter.WishThree:
                    return Hospital;
                case GameChapter.EndingComic:
                    return EndingComic;
                case GameChapter.Epilogue:
                    return Epilogue;
                case GameChapter.GameCompleted:
                    return GameComplete;
                default:
                    return Prologue;
            }
        }
    }
}
