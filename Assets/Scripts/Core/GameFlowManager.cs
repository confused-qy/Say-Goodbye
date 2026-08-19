using System;
using System.Collections.Generic;
using UnityEngine;

namespace SayGoodbye.Core
{
    [DisallowMultipleComponent]
    public sealed class GameFlowManager : MonoBehaviour
    {
        private const string SaveKey = "SayGoodbye.Save.v1";
        private const string ActionPrefix = "task_action:";

        private readonly HashSet<string> progressFlags = new HashSet<string>();
        private readonly HashSet<string> inventoryItems = new HashSet<string>();
        private readonly HashSet<string> completedMinigames = new HashSet<string>();
        private readonly HashSet<string> viewedDialogues = new HashSet<string>();
        private readonly HashSet<string> confirmedTasks = new HashSet<string>();

        public static GameFlowManager Instance { get; private set; }

        public GameChapter CurrentChapter { get; private set; } = GameChapter.Prologue;
        public int CurrentFlowStep { get; private set; } = 1;
        public string CurrentHospitalView { get; private set; } = "Left";
        public float MusicVolume { get; private set; } = 1f;
        public float SoundVolume { get; private set; } = 1f;
        public bool HasSave { get { return PlayerPrefs.HasKey(SaveKey); } }

        public event Action StateChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void StartNewGame()
        {
            progressFlags.Clear();
            inventoryItems.Clear();
            completedMinigames.Clear();
            viewedDialogues.Clear();
            confirmedTasks.Clear();
            CurrentChapter = GameChapter.Prologue;
            CurrentFlowStep = 1;
            CurrentHospitalView = "Left";
            MusicVolume = 1f;
            SoundVolume = 1f;
            Save();
            NotifyStateChanged();
        }

        public bool LoadGame()
        {
            if (!HasSave)
            {
                return false;
            }

            try
            {
                string json = PlayerPrefs.GetString(SaveKey);
                GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);
                if (data == null)
                {
                    return false;
                }

                Apply(data);
                NotifyStateChanged();
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError("[GameFlow] Failed to load save: " + exception.Message);
                return false;
            }
        }

        public void Save()
        {
            GameSaveData data = CreateSaveData();
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }

        public void ClearSave()
        {
            PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.Save();
            NotifyStateChanged();
        }

        public StoryFlowStep CurrentTask()
        {
            return StoryFlowSequence.Get(CurrentFlowStep);
        }

        public bool ReportHotspotInteraction(string hotspotName, out string feedback)
        {
            StoryFlowStep step = CurrentTask();
            StoryTaskRequirement matched = null;

            foreach (StoryTaskRequirement requirement in step.Requirements)
            {
                if (!requirement.Matches(hotspotName))
                {
                    continue;
                }

                matched = requirement;
                if (IsRequirementCompleted(requirement.Id))
                {
                    continue;
                }

                return CompleteRequirement(step, requirement, out feedback);
            }

            if (matched != null)
            {
                feedback = "这部分已经检查完毕。" + ReadyHint(step);
                return false;
            }

            feedback = "已查看：“" + hotspotName + "”。它不属于当前任务的必需条件。";
            return false;
        }

        public bool ReportTaskAction(string requirementId, out string feedback)
        {
            StoryFlowStep step = CurrentTask();
            StoryTaskRequirement requirement = step.FindRequirement(requirementId);
            if (requirement == null)
            {
                feedback = "这个操作不属于当前任务。";
                return false;
            }

            if (IsRequirementCompleted(requirement.Id))
            {
                feedback = "这一步已经完成。" + ReadyHint(step);
                return false;
            }

            return CompleteRequirement(step, requirement, out feedback);
        }

        public bool IsRequirementCompleted(string requirementId)
        {
            return !string.IsNullOrWhiteSpace(requirementId)
                && progressFlags.Contains(ActionPrefix + requirementId);
        }

        public int CompletedRequirementCount(StoryFlowStep step)
        {
            if (step == null)
            {
                return 0;
            }

            int count = 0;
            foreach (StoryTaskRequirement requirement in step.Requirements)
            {
                if (IsRequirementCompleted(requirement.Id))
                {
                    count++;
                }
            }

            return count;
        }

        public bool IsCurrentTaskReady()
        {
            return IsTaskReady(CurrentTask());
        }

        public bool IsTaskReady(StoryFlowStep step)
        {
            if (step == null || step.Requirements.Length == 0)
            {
                return false;
            }

            foreach (StoryTaskRequirement requirement in step.Requirements)
            {
                if (!IsRequirementCompleted(requirement.Id))
                {
                    return false;
                }
            }

            return true;
        }

        public bool ConfirmCurrentTask(out string destinationScene, out string feedback)
        {
            StoryFlowStep step = CurrentTask();
            destinationScene = step.SceneName;

            if (!IsTaskReady(step))
            {
                feedback = "任务条件还没有全部完成，不能提交。";
                return false;
            }

            if (!confirmedTasks.Add(step.Id))
            {
                feedback = "这个任务已经提交过了。";
                return false;
            }

            int nextIndex = StoryFlowSequence.Clamp(CurrentFlowStep + 1);
            StoryFlowStep next = StoryFlowSequence.Get(nextIndex);
            CurrentFlowStep = nextIndex;
            CurrentChapter = next.Chapter;
            destinationScene = next.SceneName;
            feedback = step.CompletionSummary;
            SaveAndNotify();
            return true;
        }

        public bool IsTaskConfirmed(string taskId)
        {
            return string.IsNullOrEmpty(taskId) || confirmedTasks.Contains(taskId);
        }

        public bool SetFlowStepForTesting(int stepIndex)
        {
            if (!Application.isEditor && !Debug.isDebugBuild)
            {
                Debug.LogWarning("[GameFlow] Testing navigation is disabled in release builds.");
                return false;
            }

            int clamped = StoryFlowSequence.Clamp(stepIndex);
            StoryFlowStep step = StoryFlowSequence.Get(clamped);
            CurrentFlowStep = clamped;
            CurrentChapter = step.Chapter;
            SaveAndNotify();
            return true;
        }

        public string MoveFlowStepForTesting(int delta)
        {
            SetFlowStepForTesting(CurrentFlowStep + delta);
            return CurrentTask().SceneName;
        }

        public bool HasFlag(string id)
        {
            return !string.IsNullOrWhiteSpace(id) && progressFlags.Contains(id);
        }

        public bool SetFlag(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || !progressFlags.Add(id))
            {
                return false;
            }

            SaveAndNotify();
            return true;
        }

        public bool IsNodeCompleted(string id)
        {
            return HasFlag(id);
        }

        public void RaiseStoryEvent(string id)
        {
            SetFlag(id);
        }

        public bool HasItem(string id)
        {
            return !string.IsNullOrWhiteSpace(id) && inventoryItems.Contains(id);
        }

        public bool AddItem(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || !inventoryItems.Add(id))
            {
                return false;
            }

            SaveAndNotify();
            return true;
        }

        public bool RemoveItem(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || !inventoryItems.Remove(id))
            {
                return false;
            }

            SaveAndNotify();
            return true;
        }

        public bool IsMinigameCompleted(string id)
        {
            return !string.IsNullOrWhiteSpace(id) && completedMinigames.Contains(id);
        }

        public bool CompleteMinigame(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || !completedMinigames.Add(id))
            {
                return false;
            }

            SaveAndNotify();
            return true;
        }

        public bool HasViewedDialogue(string id)
        {
            return !string.IsNullOrWhiteSpace(id) && viewedDialogues.Contains(id);
        }

        public bool MarkDialogueViewed(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || !viewedDialogues.Add(id))
            {
                return false;
            }

            SaveAndNotify();
            return true;
        }

        public void SetHospitalView(string view)
        {
            if (string.IsNullOrWhiteSpace(view) || CurrentHospitalView == view)
            {
                return;
            }

            CurrentHospitalView = view;
            SaveAndNotify();
        }

        public void SetVolume(float music, float sound)
        {
            MusicVolume = Mathf.Clamp01(music);
            SoundVolume = Mathf.Clamp01(sound);
            SaveAndNotify();
        }

        private bool CompleteRequirement(StoryFlowStep step, StoryTaskRequirement requirement, out string feedback)
        {
            if (!string.IsNullOrEmpty(requirement.PrerequisiteId)
                && !IsRequirementCompleted(requirement.PrerequisiteId))
            {
                StoryTaskRequirement prerequisite = step.FindRequirement(requirement.PrerequisiteId);
                string label = prerequisite != null ? prerequisite.Label : "前置任务";
                feedback = "现在还不能完成这一步。请先完成：“" + label + "”。";
                return false;
            }

            progressFlags.Add(ActionPrefix + requirement.Id);
            feedback = requirement.Feedback + ReadyHint(step);
            SaveAndNotify();
            return true;
        }

        private string ReadyHint(StoryFlowStep step)
        {
            return IsTaskReady(step) ? "\n目标已经全部满足，请点击“任务确认”。" : string.Empty;
        }

        private GameSaveData CreateSaveData()
        {
            return new GameSaveData
            {
                version = 3,
                currentChapter = CurrentChapter,
                currentFlowStep = CurrentFlowStep,
                progressFlags = new List<string>(progressFlags),
                inventoryItems = new List<string>(inventoryItems),
                completedMinigames = new List<string>(completedMinigames),
                viewedDialogues = new List<string>(viewedDialogues),
                confirmedTasks = new List<string>(confirmedTasks),
                currentHospitalView = CurrentHospitalView,
                musicVolume = MusicVolume,
                soundVolume = SoundVolume
            };
        }

        private void Apply(GameSaveData data)
        {
            CurrentFlowStep = data.version >= 2
                ? StoryFlowSequence.Clamp(data.currentFlowStep)
                : StoryFlowSequence.FindFirstForChapter(data.currentChapter);
            CurrentChapter = StoryFlowSequence.Get(CurrentFlowStep).Chapter;
            CurrentHospitalView = string.IsNullOrWhiteSpace(data.currentHospitalView) ? "Left" : data.currentHospitalView;
            MusicVolume = Mathf.Clamp01(data.musicVolume);
            SoundVolume = Mathf.Clamp01(data.soundVolume);
            Replace(progressFlags, data.progressFlags);
            Replace(inventoryItems, data.inventoryItems);
            Replace(completedMinigames, data.completedMinigames);
            Replace(viewedDialogues, data.viewedDialogues);
            Replace(confirmedTasks, data.confirmedTasks);

            if (data.version < 3)
            {
                for (int index = 1; index < CurrentFlowStep; index++)
                {
                    confirmedTasks.Add(StoryFlowSequence.Get(index).Id);
                }
            }
        }

        private static void Replace(HashSet<string> target, List<string> source)
        {
            target.Clear();
            if (source == null)
            {
                return;
            }

            foreach (string value in source)
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    target.Add(value);
                }
            }
        }

        private void SaveAndNotify()
        {
            Save();
            NotifyStateChanged();
        }

        private void NotifyStateChanged()
        {
            if (StateChanged != null)
            {
                StateChanged();
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                Save();
            }
        }

        private void OnApplicationQuit()
        {
            Save();
        }
    }
}
