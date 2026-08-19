using System;

namespace SayGoodbye.Core
{
    public sealed class StoryTaskRequirement
    {
        public readonly string Id;
        public readonly string Label;
        public readonly string Feedback;
        public readonly string PrerequisiteId;
        public readonly string[] HotspotNames;

        public StoryTaskRequirement(string id, string label, string feedback, string prerequisiteId, params string[] hotspotNames)
        {
            Id = id;
            Label = label;
            Feedback = feedback;
            PrerequisiteId = prerequisiteId;
            HotspotNames = hotspotNames ?? new string[0];
        }

        public bool Matches(string hotspotName)
        {
            foreach (string candidate in HotspotNames)
            {
                if (string.Equals(candidate, hotspotName, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }

    public sealed class StoryFlowStep
    {
        public readonly string Id;
        public readonly string SceneName;
        public readonly GameChapter Chapter;
        public readonly string Objective;
        public readonly string TaskTitle;
        public readonly string CompletionSummary;
        public readonly StoryTaskRequirement[] Requirements;

        public StoryFlowStep(
            string id,
            string sceneName,
            GameChapter chapter,
            string objective,
            string taskTitle,
            string completionSummary,
            params StoryTaskRequirement[] requirements)
        {
            Id = id;
            SceneName = sceneName;
            Chapter = chapter;
            Objective = objective;
            TaskTitle = taskTitle;
            CompletionSummary = completionSummary;
            Requirements = requirements ?? new StoryTaskRequirement[0];
        }

        public StoryTaskRequirement FindRequirement(string requirementId)
        {
            foreach (StoryTaskRequirement requirement in Requirements)
            {
                if (string.Equals(requirement.Id, requirementId, StringComparison.Ordinal))
                {
                    return requirement;
                }
            }

            return null;
        }
    }

    public static class StoryFlowSequence
    {
        private static readonly StoryFlowStep[] Steps =
        {
            S("Title", SceneCatalog.Boot, GameChapter.Prologue,
                "开始或继续故事。", "标题", "从序章开始。"),

            S("Prologue", SceneCatalog.Prologue, GameChapter.Prologue,
                "阅读林淑珍入住安宁疗护病房的七段开幕和社工独白。", "完成开幕",
                "林淑珍已经入住三号病床，社工陈心怡准备开始第一次访谈。",
                R("read_prologue", "读完七段开幕和陈心怡独白", "开幕和社工独白已经读完，可以确认进入病房。", null)),

            S("Wish1_Introduction", SceneCatalog.Hospital, GameChapter.WishOne,
                "与林淑珍完成首次访谈，再查看个人信息卡。", "第一次见面",
                "陈心怡了解了林淑珍的基本情况，也看见了她的第一个心愿。",
                R("meet_linzhen", "完成首次访谈", "林姨同意让小陈每天来陪她聊聊。", null, "病床 · 林淑珍"),
                R("inspect_patient_card", "查看个人信息卡", "信息卡：林淑珍，1948年5月4日出生，教师，喜欢唱歌和跳舞。", "meet_linzhen", "床尾卡")),

            S("Wish1_Cabinet", SceneCatalog.Hospital, GameChapter.WishOne,
                "用生日解锁资料柜，查看生命画册并取得志愿者名单。", "找到音乐志愿者",
                "资料柜已经打开，第一愿“岁月之歌”和音乐志愿者小熊的联系方式已被记录。",
                R("unlock_records", "用生日 05月04日 解锁资料柜", "生日密码正确，资料柜打开了。", "inspect_patient_card", "文件柜"),
                R("read_life_album", "阅读生命画册与第一愿", "第一愿：我想再唱一次最喜欢的那首歌。", "unlock_records", "文件柜"),
                R("collect_volunteer_list", "取得音乐志愿者名单", "已记下音乐志愿者小熊的联系电话。", "read_life_album", "文件柜")),

            S("Wish1_Volunteer", SceneCatalog.Hospital, GameChapter.WishOne,
                "联系音乐志愿者小熊，并在病房门口迎接他。", "邀请音乐志愿者",
                "小熊已经带着吉他来到病房，可以一起为林姨完成这首歌。",
                R("call_volunteer", "拨通音乐志愿者电话", "小熊答应马上带吉他过来。", null, "工作电话"),
                R("meet_volunteer", "与到达病房的小熊交谈", "小熊已经到达，林姨说起了毕业时常唱的歌。", "call_volunteer", "音乐志愿者 · 小熊")),

            S("Wish1_Guitar", SceneCatalog.Guitar, GameChapter.WishOne,
                "根据琴弦提示补全旋律，录下现场伴奏。", "完成吉他编曲",
                "伴奏完成，小熊把现场录音交给了小陈。",
                R("complete_guitar", "完成琴弦演奏并保存录音", "旋律已经补全，获得【现场录音带】。", null, "完成演奏")),

            S("Wish1_HospitalTape", SceneCatalog.Hospital, GameChapter.WishOne,
                "切到病房右侧，把现场录音带放进床头录音机并播放。", "播放现场录音",
                "林姨听见为她录下的伴奏，记忆中的老房子逐渐清晰。",
                R("switch_hospital_right", "切换到病房右侧", "病房右侧可以看到录音机和储物柜。", null),
                R("insert_live_tape", "把现场录音带放进录音机", "录音带已经放入床头录音机。", "switch_hospital_right", "床头录音机"),
                R("play_live_tape", "播放伴奏并唤起记忆", "林姨轻声说：这是专门为我留下的曲子。", "insert_live_tape", "床头录音机")),

            S("Wish1_MemoryTape", SceneCatalog.LivingRoom, GameChapter.WishOne,
                "打开记忆中的客厅柜子，找到未录完的旧磁带。", "找到旧磁带",
                "一盘停在丈夫离世那天下午的旧磁带，被重新拿到了手中。",
                R("open_memory_cabinet", "打开客厅旧柜子", "柜门打开，里面放着一盘旧磁带。", null, "客厅旧柜子"),
                R("collect_unfinished_tape", "取得未完成的旧磁带", "获得【未完成的旧磁带】。", "open_memory_cabinet", "客厅旧柜子")),

            S("Wish1_Lyrics", SceneCatalog.Bedroom, GameChapter.WishOne,
                "阅读半页手写歌词和高三二班留下的信。", "读完学生来信",
                "跑调和笑声也是十七岁的声音；林老师一直被学生们记得。",
                R("read_half_lyrics", "展开半页《送别》歌词", "纸上写着：长亭外，古道边，芳草碧连天……", null, "手写歌词"),
                R("read_student_letter", "读完高三二班学生来信", "学生们想把当年录坏却最真实的一遍，当面唱给林老师听。", "read_half_lyrics", "手写歌词")),

            S("Wish1_Phone", SceneCatalog.LivingRoom, GameChapter.WishOne,
                "播放旧磁带，接听女儿来电，再收好“未完成的歌”。", "确认第一愿完成",
                "林姨重新听见歌声，也面对了那通没能说完的话。第一愿“岁月之歌”完成。",
                R("play_unfinished_tape", "把旧磁带放入播放机", "旧磁带开始播放，房间里响起久远的歌声。", null, "磁带播放机"),
                R("answer_daughter_call", "接听女儿来电", "电话匆匆挂断，林姨仍有很多话没有说出口。", "play_unfinished_tape", "老电话"),
                R("collect_song_fragment", "取得遗憾碎片“未完成的歌”", "获得【遗憾碎片 · 未完成的歌】。", "answer_daughter_call", "磁带播放机")),

            S("Wish2_Start", SceneCatalog.Hospital, GameChapter.WishTwo,
                "查看第二愿，切到病房右侧取得全套化妆品。", "开始第二愿",
                "第二愿“我的美丽”已经解锁，全套化妆品已放入物品栏。",
                R("open_wish_two", "查看心愿清单中的第二愿", "第二愿：我想学年轻人化一次妆，漂漂亮亮地离开。", null, "心愿清单"),
                R("switch_wish_two_right", "切换到病房右侧", "病房右侧柜子上放着小陈带来的化妆品。", "open_wish_two"),
                R("collect_makeup_kit", "取得全套化妆品", "获得【全套化妆品】。", "switch_wish_two_right", "全套化妆品")),

            S("Wish2_Makeup", SceneCatalog.Makeup, GameChapter.WishTwo,
                "依次完成底妆、腮红和口红，再让林姨确认妆容。", "完成第一次全妆",
                "林姨第一次看见精心打扮的自己，也想起了已经离开多年的阿民。",
                R("apply_base", "完成底妆", "底妆已经完成。", null, "粉底"),
                R("apply_blush", "轻轻补上腮红", "腮红已经完成。", "apply_base", "胭脂"),
                R("apply_lipstick", "完成口红", "口红已经完成。", "apply_blush", "口红"),
                R("confirm_makeup", "让林姨确认妆容", "林姨说：真想让阿民看看。", "apply_lipstick", "确认妆容")),

            S("Wish2_SunflowerStart", SceneCatalog.LivingRoom, GameChapter.WishTwo,
                "调查客厅里的向日葵盆栽，找出隐藏机关。", "发现向日葵机关",
                "触碰一朵花会同时改变相邻花朵，花盆里藏着一把小钥匙。",
                R("inspect_sunflowers", "调查向日葵盆栽", "向日葵机关已经开启。", null, "向日葵盆栽")),

            S("Wish2_Sunflower", SceneCatalog.Sunflower, GameChapter.WishTwo,
                "让九朵向日葵全部盛开，取得小钥匙。", "解开向日葵机关",
                "九朵花已经全部盛开，获得了打开旧收纳盒的小钥匙。",
                R("solve_sunflowers", "确认九朵向日葵全部盛开", "获得【小钥匙】。", null, "确认全部盛开")),

            S("Wish2_StorageBox", SceneCatalog.LivingRoom, GameChapter.WishTwo,
                "用小钥匙打开收纳盒，取得阿民留下的旧胭脂。", "找到阿民的胭脂",
                "收纳盒里保留着阿民的旧物和一盒带着桂花香的胭脂。",
                R("unlock_storage_box", "用小钥匙打开收纳盒", "小钥匙正好能打开这只旧收纳盒。", null, "上锁的储物盒"),
                R("collect_old_rouge", "取得旧胭脂", "获得【旧胭脂】。", "unlock_storage_box", "上锁的储物盒")),

            S("Wish2_Mirror", SceneCatalog.Bedroom, GameChapter.WishTwo,
                "把旧胭脂放到梳妆镜前，读完阿民写给淑珍的信。", "确认第二愿完成",
                "林姨知道，无论是否擦胭脂，阿民都认真爱着她。第二愿“我的美丽”完成。",
                R("place_rouge", "把旧胭脂放到镜前", "镜中的旧时光逐渐清晰。", null, "梳妆镜"),
                R("read_love_letter", "读完阿民的旧日情书", "信末写着：你抹胭脂的样子，我看一辈子也不够。", "place_rouge", "梳妆镜"),
                R("collect_letter_fragment", "取得遗憾碎片“一封旧日情书”", "获得【遗憾碎片 · 一封旧日情书】。", "read_love_letter", "梳妆镜")),

            S("Wish3_Start", SceneCatalog.Hospital, GameChapter.WishThree,
                "查看第三愿，并在林姨同意后联系她的女儿。", "开始第三愿",
                "第三愿“好好相见，好好告别”已经解锁，女儿答应来到病房。",
                R("open_wish_three", "查看心愿清单中的第三愿", "第三愿：我想和女儿说一声“对不起”。", null, "心愿清单"),
                R("call_daughter", "征得林姨同意后联系女儿", "女儿沉默片刻后答应过来。", "open_wish_three", "工作电话")),

            S("Wish3_Arrival", SceneCatalog.Corridor, GameChapter.WishThree,
                "在走廊迎接女儿，再陪她一起进入病房。", "陪女儿进入病房",
                "母女终于见面，林姨的记忆回到了每天傍晚四点的家。",
                R("meet_daughter", "与到达病房门口的女儿交谈", "女儿已经到达，但还不知道第一句话该怎么说。", null, "林姨的女儿"),
                R("return_to_ward_together", "陪她一起进入病房", "母女在病床边坐了下来。", "meet_daughter", "病房门")),

            S("Wish3_Clock", SceneCatalog.LivingRoom, GameChapter.WishThree,
                "调查指向下午四点的时钟，打开厨房记忆。", "回到做饭的傍晚",
                "四点了，该准备女儿最喜欢的红烧鱼和番茄炒蛋了。",
                R("inspect_four_clock", "调查四点钟", "时钟停在下午四点，厨房入口亮了起来。", null, "四点钟")),

            S("Wish3_Kitchen", SceneCatalog.Kitchen, GameChapter.WishThree,
                "从冰箱取得全部食材，再查看旧食谱。", "准备两道家常菜",
                "红烧鱼和番茄炒蛋需要的食材已经准备齐全。",
                R("collect_ingredients", "从冰箱取得全部食材", "鱼、番茄和鸡蛋已经放入食材篮。", null, "冰箱"),
                R("read_old_recipe", "查看旧食谱", "食谱旁写着：六点前做好，她们回家就能吃。", "collect_ingredients", "旧食谱")),

            S("Wish3_Cooking", SceneCatalog.Cooking, GameChapter.WishThree,
                "完成备料、红烧鱼、番茄炒蛋，并在六点摆盘。", "完成记忆中的晚饭",
                "两道菜已经在六点整摆上餐桌，家中电话随即响起。",
                R("prepare_food", "处理全部食材", "食材已经洗净切好。", null, "处理全部食材"),
                R("cook_fish", "完成红烧鱼", "红烧鱼已经出锅。", "prepare_food", "完成红烧鱼"),
                R("cook_eggs", "完成番茄炒蛋", "番茄炒蛋已经出锅。", "cook_fish", "完成番茄炒蛋"),
                R("serve_at_six", "六点整完成摆盘", "两道菜已经摆上餐桌。", "cook_eggs", "六点摆盘")),

            S("Wish3_Photo", SceneCatalog.LivingRoom, GameChapter.WishThree,
                "接听那通没有回家的电话，再调查亮起的全家福。", "面对亲情遗憾",
                "那顿饭最终没有等到家人回来，但全家福仍保存着一家人在一起的时刻。",
                R("answer_family_call", "接听女儿来电", "女儿说这个月不能回来，林姨望着渐渐凉掉的饭菜。", null, "老电话"),
                R("inspect_family_photo", "调查亮起的全家福", "相框中的照片裂成了九块。", "answer_family_call", "全家福相框")),

            S("Wish3_Puzzle", SceneCatalog.FamilyPuzzle, GameChapter.WishThree,
                "重新拼好全家福，取得最后一块遗憾碎片。", "确认第三愿完成",
                "全家福重新完整，母女终于有机会把道歉、爱和告别说出口。第三愿完成。",
                R("complete_family_photo", "确认全家福拼图完成", "获得最后一块遗憾碎片。", null, "确认拼图完成")),

            S("Ending", SceneCatalog.EndingComic, GameChapter.EndingComic,
                "依次阅读母女和解、拥抱、合影与新全家福。", "完成告别漫画",
                "林姨与女儿完成了道歉、道爱与道别，一张新的全家福留了下来。",
                R("ending_frame_one", "阅读第一格：母女和解", "那些埋了很多年的话，终于被说了出来。", null, "第一格 · 母女和解"),
                R("ending_frame_two", "阅读第二格：最后的拥抱", "女儿抱住了打扮漂亮的母亲。", "ending_frame_one", "第二格 · 最后的拥抱"),
                R("ending_frame_three", "阅读第三格：拍一张合照", "小陈提议为母女拍一张新的合照。", "ending_frame_two", "第三格 · 拍摄合照"),
                R("ending_frame_four", "阅读第四格：新的全家福", "画面定格成新的全家福。", "ending_frame_three", "第四格 · 新的全家福")),

            S("Epilogue", SceneCatalog.Epilogue, GameChapter.Epilogue,
                "三个月后回到空病房，播放桌上留下的最后录音。", "听完最后的录音",
                "录音播放完毕。林淑珍的故事结束了，但被认真倾听过的生命仍留在记忆中。",
                R("play_final_recording", "播放并听完最后录音", "最后一段录音已经播放完毕。", null, "最后的录音机")),

            S("Complete", SceneCatalog.GameComplete, GameChapter.GameCompleted,
                "故事已经完成。", "故事完成", "谢谢你陪她好好说了再见。")
        };

        public static int Count { get { return Steps.Length; } }

        public static StoryFlowStep Get(int index)
        {
            return Steps[Clamp(index)];
        }

        public static StoryFlowStep FindById(string id)
        {
            foreach (StoryFlowStep step in Steps)
            {
                if (string.Equals(step.Id, id, StringComparison.Ordinal))
                {
                    return step;
                }
            }

            return null;
        }

        public static int Clamp(int index)
        {
            return Math.Max(0, Math.Min(index, Steps.Length - 1));
        }

        public static int FindFirstForScene(string sceneName)
        {
            for (int index = 0; index < Steps.Length; index++)
            {
                if (string.Equals(Steps[index].SceneName, sceneName, StringComparison.Ordinal))
                {
                    return index;
                }
            }

            return 0;
        }

        public static int FindFirstForChapter(GameChapter chapter)
        {
            for (int index = 0; index < Steps.Length; index++)
            {
                if (Steps[index].Chapter == chapter)
                {
                    return index;
                }
            }

            return 1;
        }

        private static StoryFlowStep S(
            string id,
            string sceneName,
            GameChapter chapter,
            string objective,
            string taskTitle,
            string completionSummary,
            params StoryTaskRequirement[] requirements)
        {
            return new StoryFlowStep(id, sceneName, chapter, objective, taskTitle, completionSummary, requirements);
        }

        private static StoryTaskRequirement R(
            string id,
            string label,
            string feedback,
            string prerequisiteId,
            params string[] hotspotNames)
        {
            return new StoryTaskRequirement(id, label, feedback, prerequisiteId, hotspotNames);
        }
    }
}
