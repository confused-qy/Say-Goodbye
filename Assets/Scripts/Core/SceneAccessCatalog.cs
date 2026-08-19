using System;
using UnityEngine.SceneManagement;

namespace SayGoodbye.Core
{
    public enum SceneAccessState
    {
        Locked,
        Unlocked,
        Current
    }

    public sealed class SceneAccessNode
    {
        public readonly string SceneName;
        public readonly string DisplayName;
        public readonly string GroupName;
        public readonly string RequiredTaskId;

        public SceneAccessNode(string sceneName, string displayName, string groupName, string requiredTaskId)
        {
            SceneName = sceneName;
            DisplayName = displayName;
            GroupName = groupName;
            RequiredTaskId = requiredTaskId;
        }
    }

    public static class SceneAccessCatalog
    {
        public static readonly SceneAccessNode[] Nodes =
        {
            new SceneAccessNode(SceneCatalog.Prologue, "序章", "故事入口", string.Empty),
            new SceneAccessNode(SceneCatalog.Hospital, "安宁病房", "现实地点", "Prologue"),
            new SceneAccessNode(SceneCatalog.Corridor, "医院走廊", "现实地点", "Wish3_Start"),
            new SceneAccessNode(SceneCatalog.LivingRoom, "记忆中的客厅", "记忆之家", "Wish1_HospitalTape"),
            new SceneAccessNode(SceneCatalog.Bedroom, "记忆中的卧室", "记忆之家", "Wish1_MemoryTape"),
            new SceneAccessNode(SceneCatalog.Kitchen, "记忆中的厨房", "记忆之家", "Wish3_Clock"),
            new SceneAccessNode(SceneCatalog.Guitar, "吉他编曲", "心愿活动", "Wish1_Volunteer"),
            new SceneAccessNode(SceneCatalog.Makeup, "整理妆容", "心愿活动", "Wish2_Start"),
            new SceneAccessNode(SceneCatalog.Sunflower, "向日葵机关", "心愿活动", "Wish2_SunflowerStart"),
            new SceneAccessNode(SceneCatalog.Cooking, "记忆料理", "心愿活动", "Wish3_Kitchen"),
            new SceneAccessNode(SceneCatalog.FamilyPuzzle, "全家福拼图", "心愿活动", "Wish3_Photo"),
            new SceneAccessNode(SceneCatalog.EndingComic, "好好告别", "尾声", "Wish3_Puzzle"),
            new SceneAccessNode(SceneCatalog.Epilogue, "三个月后", "尾声", "Ending"),
            new SceneAccessNode(SceneCatalog.GameComplete, "故事完成", "尾声", "Epilogue")
        };

        public static SceneAccessNode Find(string sceneName)
        {
            foreach (SceneAccessNode node in Nodes)
            {
                if (string.Equals(node.SceneName, sceneName, StringComparison.Ordinal))
                {
                    return node;
                }
            }

            return null;
        }

        public static string DisplayNameFor(string sceneName)
        {
            if (string.Equals(sceneName, SceneCatalog.Boot, StringComparison.Ordinal))
            {
                return "标题";
            }

            SceneAccessNode node = Find(sceneName);
            return node != null ? node.DisplayName : sceneName;
        }

        public static bool IsUnlocked(string sceneName, GameFlowManager flow)
        {
            SceneAccessNode node = Find(sceneName);
            return node == null || flow == null || flow.IsTaskConfirmed(node.RequiredTaskId);
        }

        public static SceneAccessState StateFor(SceneAccessNode node, GameFlowManager flow)
        {
            if (node == null)
            {
                return SceneAccessState.Locked;
            }

            if (string.Equals(SceneManager.GetActiveScene().name, node.SceneName, StringComparison.Ordinal))
            {
                return SceneAccessState.Current;
            }

            return flow == null || flow.IsTaskConfirmed(node.RequiredTaskId)
                ? SceneAccessState.Unlocked
                : SceneAccessState.Locked;
        }

        public static string RequirementLabel(SceneAccessNode node)
        {
            if (node == null || string.IsNullOrEmpty(node.RequiredTaskId))
            {
                return string.Empty;
            }

            StoryFlowStep required = StoryFlowSequence.FindById(node.RequiredTaskId);
            return required != null ? required.TaskTitle : node.RequiredTaskId;
        }
    }
}
