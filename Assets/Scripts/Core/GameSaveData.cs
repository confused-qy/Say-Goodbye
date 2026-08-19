using System;
using System.Collections.Generic;

namespace SayGoodbye.Core
{
    [Serializable]
    public sealed class GameSaveData
    {
        public int version = 3;
        public GameChapter currentChapter = GameChapter.Prologue;
        public int currentFlowStep = 1;
        public List<string> progressFlags = new List<string>();
        public List<string> inventoryItems = new List<string>();
        public List<string> completedMinigames = new List<string>();
        public List<string> viewedDialogues = new List<string>();
        public List<string> confirmedTasks = new List<string>();
        public string currentHospitalView = "Left";
        public float musicVolume = 1f;
        public float soundVolume = 1f;
    }
}
