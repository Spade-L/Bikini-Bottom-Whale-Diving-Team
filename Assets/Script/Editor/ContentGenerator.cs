#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class ContentGenerator
{
// 推进 ContentGenerator 的当前步骤
    private const string Root = "Assets/GameData";
    private const string ClueDir = Root + "/Clues";
// 在 ContentGenerator 中处理 推进 ContentGenerator 的当前步骤
    private const string DlgDir = Root + "/Dialogues";

    private static readonly List<ClueData> allClues = new List<ClueData>();
// 保存 trueEndingRequiredClues 引用
    private static readonly List<ClueData> trueEndingRequiredClues = new List<ClueData>();

    [MenuItem("Trace Me/生成全部内容资产")]
    // 生成全部剧本资源并刷新资源库
    public static void GenerateAll()
    {
        // 先确保父目录存在，避免 CreateAsset 因路径缺失失败
        EnsureFolder(Root);
// 推进 GenerateAll 中的必要步骤
        EnsureFolder(ClueDir);
        EnsureFolder(DlgDir);
        // 清空的是内存列表，不会删除磁盘上的资产
        allClues.Clear();
        trueEndingRequiredClues.Clear();

        // 按场景顺序生成，数据库也保留这一稳定顺序
        GenerateHome();
        trueEndingRequiredClues.AddRange(allClues.GetRange(0, 4));
// 推进 GenerateAll 中的必要步骤（GenerateAll）
        GenerateSchool();

        int beforeStore = allClues.Count;
// 在 GenerateAll 中处理 GenerateStore
        GenerateStore();
        trueEndingRequiredClues.AddRange(allClues.GetRange(beforeStore, 3));

// 设置 GenerateAll 的配置数值
        int beforeAlley = allClues.Count;
        GenerateAlley();
// 使用 GenerateAll 所需功能
        trueEndingRequiredClues.AddRange(allClues.GetRange(beforeAlley, 3));

        int beforePlayground = allClues.Count;
// 在 GenerateAll 中处理 GeneratePlayground
        GeneratePlayground();
        // 游乐场四条核心线索进入真结局门槛，四条补充线索保持可选
        trueEndingRequiredClues.AddRange(allClues.GetRange(beforePlayground, 4));

// 在 GenerateAll 中处理 GenerateRooftop
        GenerateRooftop();
        // 开场与通关对白不属于单个可调查物品
        GenerateSceneIntrosAndClears();
        // 阈值、封锁和门等运行时系统使用独立对白资产
        GenerateSystemDialogues();
        // 事件表依赖前面已创建的对白资产
        GenerateEventTable();
        // 数据库最后写入，确保其引用的是本轮生成的线索
        GenerateClueDatabase();

        // SetDirty 仅标记变更；此处统一持久化到磁盘
        AssetDatabase.SaveAssets();
        // 刷新后 Project 窗口和导入管线才能立即识别新资产
        AssetDatabase.Refresh();
// 输出调试信息
        Debug.Log($"[ContentGenerator] 完成：{allClues.Count} 条线索及全部对话/事件表已生成到 {Root}");
    }

    private static void GenerateHome()
    {
// 推进 GenerateHome 中的必要步骤
        Clue("home_photo", "旧照片",
            "餐桌上倒扣着相框。两个人站在家门口，其中一个的脸被划掉了。",
// 推进 GenerateHome 的当前步骤
            "和{sibling}在家门口的合影。{ta}的脸怎么都看不清。",
            "脸被划掉的那个人，就是你。这张照片里的“两个人”，从来都是同一个人。",
// 推进 GenerateHome 中的必要步骤（GenerateHome）
            Dlg("Dlg_home_photo", false, null,
                ("", "（餐桌上倒扣着一个相框。你把它翻过来——两个人站在家门口，其中一个的脸被划掉了。）"),
// 推进 GenerateHome 的当前步骤（GenerateHome）
                ("我", "这张照片……是在家门口拍的。我和{sibling}。但{ta}的脸……怎么都看不清？")));

        Clue("home_bowls", "两副碗筷",
// 推进 GenerateHome 的当前步骤（GenerateHome）（sibling）
            "餐桌左右两端各有一副碗筷。一副干净，一副积满灰尘，筷子还搭在碗上。",
            "{sibling}走之前用过的碗筷，一直没有收。",
// 推进 GenerateHome 的当前步骤（GenerateHome）（Dlg）
            "那副积灰的碗筷是你摆的。你一直在等一个不会回来的人——等的其实是过去的自己。",
            Dlg("Dlg_home_bowls", false, null,
// 在 GenerateHome 中继续当前处理
                ("", "（餐桌左右两端各有一副碗筷。一副干净，一副积满灰尘，筷子还搭在碗上。）"),
                ("我", "{sibling}走之前还在吃饭吗？怎么感觉像是放了很久很久……")));

// 在 GenerateHome 中处理 Clue
        Clue("home_marks", "身高刻痕",
            "门框侧面从低到高的十几条刻痕，最高的那条标着“18岁”。",
// 在 GenerateHome 中继续当前处理（GenerateHome 后续步骤）
            "{sibling}每年量身高留下的记号，停在18岁。",
            "刻痕只有一列。18岁那年，“{sibling}”消失了——那是你把自己封存起来的年纪。",
// 在 GenerateHome 中处理 Dlg
            Dlg("Dlg_home_marks", false, null,
                ("", "（门框侧面有十几条刻痕，从低到高。最高的一条旁边写着——18岁。）"),
                ("我", "这些刻痕……最高的那条标着18岁。{sibling}走的时候就是18岁。")));

// 在 GenerateHome 中处理 Clue（GenerateHome 后续步骤）
        Clue("home_tinbox", "铁盒子",
            "床底最里面塞着一个生锈的铁盒，里面是一条旧手环。",
// 在 GenerateHome 中继续当前处理（GenerateHome 后续步骤）（111）
            "{sibling}藏起来的手环，内侧刻着名字缩写。",
            "手环是你自己的。缩写相同，因为那本来就是同一个名字。",
// 在 GenerateHome 中处理 Dlg（GenerateHome 后续步骤）
            Dlg("Dlg_home_tinbox", false, null,
                ("", "（床底最里面塞着一个生锈的铁盒。打开——里面有一条旧手环。）"),
// 推进 GenerateHome 的当前步骤（GenerateHome）（xx）
                ("我", "手环内侧有刻字……我的名字缩写是xx，但是{sibling}的名字缩写也是xx.……不愧是{kin}。")));

        Clue("home_piano", "旧钢琴",
// 在 GenerateHome 中继续当前处理（GenerateHome 后续步骤）（121）
            "旧钢琴上落了不少的灰。", "{sibling}不见后，再也没有动过的钢琴。", "琴上的灰尘记录着无人弹奏的日子。",
            Dlg("Dlg_home_piano", false, null,
// 在 GenerateHome 中继续当前处理（GenerateHome 后续步骤）（124）
                ("", "（旧钢琴上落了不少的灰。）"),
                ("我", "自从{sibling}不见之后，我就再也没有动过这个钢琴了。")));
// 在 GenerateHome 中处理 Clue（GenerateHome 后续步骤）（后续处理 2）
        Clue("home_sofa", "沙发上的全家福",
            "不大的沙发上，放着过去家里拍摄的全家福。", "印象中和{sibling}坐在这里看电视，一看就是一个下午。", "全家福留住了过去的一个瞬间。",
// 在 GenerateHome 中处理 Dlg（GenerateHome 后续步骤）（后续处理 2）
            Dlg("Dlg_home_sofa", false, null,
                ("", "（一个不大的沙发，印象中会和{sibling}坐在这里看电视，一看就是一个下午。）"),
// 在 GenerateHome 中继续当前处理（GenerateHome 后续步骤）（133）
                ("我", "每次自己坐在这里，都会觉得心里空荡荡的。"),
                ("我", "真希望早点找到{sibling}。"),
// 推进 GenerateHome 的当前步骤（GenerateHome）（Clue）
                ("", "（沙发上有过去家里拍摄的全家福。）")));
        Clue("home_cabinet", "床头柜里的工具",
// 在 GenerateHome 中继续当前处理（GenerateHome 后续步骤）（139）
            "床头柜里面放着各种杂物工具。", "自己不会操作，{sibling}却很熟练的工具。", "工具仍留在原处。",
            Dlg("Dlg_home_cabinet", false, null,
// 在 GenerateHome 中继续当前处理（GenerateHome 后续步骤）（142）
                ("", "（床头柜里面放着各种杂物工具。）"),
                ("我", "螺丝刀，手电筒，没用完的电工胶布，厚厚的上工手套，大小不一样的剪刀，一圈透明胶带……"),
// 在 GenerateHome 中继续当前处理（GenerateHome 后续步骤）（145）
                ("我", "{sibling}不见之后，我再也没动过这里面的东西。"),
                ("我", "主要还是我不会操作，但是{sibling}很熟练。")));
// 在 GenerateHome 中处理 Clue（GenerateHome 后续步骤）（后续处理 3）
        Clue("home_sink", "厨房的洗手台",
            "洗手台收拾得很干净，不能让屋子里充满臭味。", "以前都是{sibling}做的饭。", "整洁的洗手台也是生活留下的痕迹。",
            Dlg("Dlg_home_sink", false, null,
// 在 GenerateHome 中继续当前处理（GenerateHome 后续步骤）（152）
                ("", "（收拾得很干净，就算{sibling}不见了也不能让屋子里充满臭味。）"),
                ("我", "好久没来这块地方了，不过我也不会进厨房。"),
// 在 GenerateHome 中继续当前处理（GenerateHome 后续步骤）（155）
                ("我", "以前都是{sibling}做的饭。")));
        Clue("home_plant", "墙角的盆栽",
// 在 GenerateHome 中继续当前处理（GenerateHome 后续步骤）（158）
            "{sibling}之前种的盆栽，仍然非常有活力。", "一直悉心照顾的盆栽。", "你的照料让植物继续生长。",
            Dlg("Dlg_home_plant", false, null,
// 在 GenerateHome 中继续当前处理（GenerateHome 后续步骤）（161）
                ("", "（{sibling}之前种的盆栽。）"),
                ("我", "就算所有人都觉得{ta}不存在，我也不会忘记{sibling}的。"),
// 推进 GenerateHome 的当前步骤（GenerateHome）（ta）
                ("我", "从{ta}不见之后，我就一直在悉心照顾这个盆栽，所以非常有活力。")));
        // 用户提供的电视广告对白作为暂定内容；电视仍是补充线索，不加入核心完成条件
        Clue("home_tv", "电视", "客厅里的电视。", "一台电视。", "客厅里的日常物件。",
// 在 GenerateHome 中处理 Dlg（GenerateHome 后续步骤）（后续处理 3）
            Dlg("Dlg_home_tv", false, null,
                ("我", "上面播放着特价商品广告"),
// 推进 GenerateHome 的当前步骤（GenerateHome）（private）
                ("我", "笑到就是赚到，今天开始你就是赢家！"),
                ("我", "永远以优惠时价提供佳品！"),
// 在 GenerateHome 中继续当前处理（GenerateHome 后续步骤）（174）
                ("我", "“不要98，不要998，只要9998！！"),
                ("我", "“呃... ... 认真的吗”")));
    }

    // 生成 GenerateSchool 对应资源
    private static void GenerateSchool()
    {
        int schoolCoreStart = allClues.Count;
// 推进 GenerateSchool 中的必要步骤
        Clue("school_desk", "课桌刻字",
            "靠窗第三排的课桌，桌面上用小刀刻着一个日期。",
// 推进 GenerateSchool 的当前步骤
            "{sibling}的课桌上刻着{ta}消失那天的日期。",
            "日期是你亲手刻下的。那一天，你决定忘记。",
// 推进 GenerateSchool 中的必要步骤（GenerateSchool）
            Dlg("Dlg_school_desk", false, null,
                ("", "（靠窗第三排的课桌。桌面上有用小刀刻出来的痕迹——是一个日期。）"),
// 推进 GenerateSchool 的当前步骤（GenerateSchool）
                ("我", "下面刻着日期……是{sibling}消失的那天。")));

        Clue("school_report", "成绩单",
            "讲台抽屉里泛黄的成绩单，名字被墨水涂掉了。",
// 推进 GenerateSchool 的当前步骤（GenerateSchool）（sibling）
            "应该是{sibling}的成绩单，科目成绩都很高。",
            "名字是你涂掉的。那是你的成绩单。",
// 在 GenerateSchool 中处理 Dlg
            Dlg("Dlg_school_report", false, null,
                ("", "（讲台的抽屉没有锁。里面有一张泛黄的纸——成绩单。名字的位置被墨水涂掉了。）"),
// 在 GenerateSchool 中继续当前处理
                ("我", "名字被涂掉了……但科目成绩都很高。是{sibling}的吧？")));

        Clue("school_locker", "储物柜",
// 在 GenerateSchool 中继续当前处理（GenerateSchool 后续步骤）
            "走廊尽头的铁皮柜。锁是坏的。里面有一幅画：两个人站在操场边，其中一个被撕掉了。",
            "{sibling}的储物柜里留着一幅两个人的画，被撕掉了一半。",
// 推进 GenerateSchool 的当前步骤（GenerateSchool）（Dlg）
            "画里被撕掉的那个人从来不存在。完整的那个人，就是你。",
            Dlg("Dlg_school_locker", false, null,
// 推进 GenerateSchool 的当前步骤（GenerateSchool）（Clue）
                ("", "（走廊尽头的铁皮柜。锁已经坏了，轻轻一拉就开。里面放着一幅画。）"),
                ("我", "画的是两个人站在操场边上……但其中一个被撕掉了。只剩下右边那个。"),
// 在 GenerateSchool 中继续当前处理（GenerateSchool 后续步骤）（216）
                ("", "（右边那个人的脸是完整的——和你一模一样。）")));

        Clue("school_board", "黑板上的字",
// 在 GenerateSchool 中继续当前处理（GenerateSchool 后续步骤）（220）
            "黑板右下角有粉笔写的一行不完整的字。",
            "笔迹很眼熟，像是自己的。什么时候写的？",
// 在 GenerateSchool 中继续当前处理（GenerateSchool 后续步骤）（223）
            "确实是你写的。你比自己以为的记得更多。",
            Dlg("Dlg_school_board", false, null,
// 推进 GenerateSchool 的当前步骤（GenerateSchool）（trueEndingRequiredClues）
                ("", "（黑板右下角，有一行没写完的粉笔字。日光灯在头顶闪了一下。）"),
                ("我", "这粉笔字……是我写的。我记得这笔迹。但我什么时候写的？")));

// 使用 GenerateSchool 所需功能
        trueEndingRequiredClues.AddRange(allClues.GetRange(schoolCoreStart, allClues.Count - schoolCoreStart));

        Clue("school_water_dispenser", "饮水机",
// 在 GenerateSchool 中继续当前处理（GenerateSchool 后续步骤）（234）
            "一台放在墙角的饮水机，水桶早就干了。",
            "一个饮水机，竟然暗藏玄机？",
// 在 GenerateSchool 中继续当前处理（GenerateSchool 后续步骤）（237）
            "你把它移开后，才看见后面藏着的房间。",
            Dlg("Dlg_school_water_dispenser", false, null,
                ("", "（一台放在墙角的饮水机 水桶早就干了）"),
// 在 GenerateSchool 中继续当前处理（GenerateSchool 后续步骤）（241）
                ("我", "饮水机不太稳……好像可以移动？")));

        Dlg("Dlg_school_water_dispenser_reveal", false, null,
// 在 GenerateSchool 中继续当前处理（GenerateSchool 后续步骤）（245）
            ("我", "后面好像有个房间……"));
        Dlg("Dlg_school_water_dispenser_repeat", false, null,
// 在 GenerateSchool 中继续当前处理（GenerateSchool 后续步骤）（248）
            ("我", "一个饮水机，竟然暗藏玄机？"));

        Clue("school_paper_rank", "成绩排名纸",
// 在 GenerateSchool 中继续当前处理（GenerateSchool 后续步骤）（252）
            "饮水机不远处的地上掉了一张纸。",
            "是某次考试的成绩排名，虽然看不太清上面的具体字迹了，不过我们家姓氏很少见，当然也很明显，{sibling}的名字在第一。",
// 在 GenerateSchool 中继续当前处理（GenerateSchool 后续步骤）（255）
            "那张成绩排名记录的，是你曾经取得的成绩。",
            Dlg("Dlg_school_paper_rank", false, null,
// 在 GenerateSchool 中继续当前处理（GenerateSchool 后续步骤）（258）
                ("", "（饮水机不远处的地上掉了一张纸）"),
                ("我", "是某次考试的成绩排名，虽然看不太清上面的具体字迹了，不过我们家姓氏很少见，当然也很明显，{sibling}的名字在第一。")));

// 在 GenerateSchool 中处理 Clue
        Clue("school_paper_counseling", "破碎的纸张",
            "公告栏前面掉了一张破碎的纸张。",
// 在 GenerateSchool 中继续当前处理（GenerateSchool 后续步骤）（265）
            "勉强看得清上面残留的部分字迹，是学校的心理咨询活动吗？",
            "被撕碎的记录，仍然留下了你不愿面对的求助痕迹。",
// 在 GenerateSchool 中处理 Dlg（GenerateSchool 后续步骤）
            Dlg("Dlg_school_paper_counseling", false, null,
                ("", "（公告栏前面掉了一张破碎的纸张）"),
// 在 GenerateSchool 中继续当前处理（GenerateSchool 后续步骤）（271）
                ("我", "勉强看得清上面残留的部分字迹，是学校的心理咨询活动吗？")));

        Clue("school_hidden_room_files", "墙上的纸张",
// 在 GenerateSchool 中继续当前处理（GenerateSchool 后续步骤）（275）
            "地上墙上充满了凌乱的纸张，无一例外的都是关于{sibling}的信息。",
            "家庭住址、盗摄的照片、每次考试的成绩、作业的分数……还有病历？",
// 在 GenerateSchool 中继续当前处理（GenerateSchool 后续步骤）（278）
            "这里记录的不是另一个人，而是你被切割出来的记忆。",
            Dlg("Dlg_school_hidden_room_files", false, null,
// 在 GenerateSchool 中继续当前处理（GenerateSchool 后续步骤）（281）
                ("我", "这…这里是怎么回事……"),
                ("", "（地上墙上充满了凌乱的纸张，无一例外的都是关于{sibling}的信息）"),
                ("我", "家庭住址、盗摄的照片、每次考试的成绩、作业的分数……还有病历？"),
// 在 GenerateSchool 中继续当前处理（GenerateSchool 后续步骤）（285）
                ("我", "{sibling}是生病了吗……"),
                ("我", "不对，这里到底是怎么回事，为什么{sibling}会被人盯上。"),
// 在 GenerateSchool 中继续当前处理（GenerateSchool 后续步骤）（288）
                ("我", "但是，为什么只写了姓氏？")));

        Clue("school_notice_board", "公告栏",
// 在 GenerateSchool 中继续当前处理（GenerateSchool 后续步骤）（292）
            "上面贴了学校的各种活动，以及通知，包括国家规定的节假日，以及寒暑假的调休。",
            "{sibling}真的好辛苦，要参加的东西好多。",
// 在 GenerateSchool 中继续当前处理（GenerateSchool 后续步骤）（295）
            "你把自己的日程和身份，写成了另一个人的生活。",
            Dlg("Dlg_school_notice_board", false, null,
// 在 GenerateSchool 中继续当前处理（GenerateSchool 后续步骤）（298）
                ("", "（上面贴了学校的各种活动，以及通知，包括国家规定的节假日，以及寒暑假的调休）"),
                ("我", "{sibling}真的好辛苦，要参加的东西好多。")));

// 在 GenerateSchool 中处理 Clue（GenerateSchool 后续步骤）
        Clue("school_cleaning_tools", "清洁工具堆",
            "扫帚和拖把之类的东西被一股脑地堆在这个角落中。",
// 在 GenerateSchool 中继续当前处理（GenerateSchool 后续步骤）（305）
            "好乱。",
            "你一直把不愿整理的东西堆在角落里。",
// 在 GenerateSchool 中处理 Dlg（GenerateSchool 后续步骤）（后续处理 2）
            Dlg("Dlg_school_cleaning_tools", false, null,
                ("", "（扫帚和拖把之类的东西被一股脑地堆在这个角落中）"),
// 推进 GenerateSchool 的当前步骤（GenerateSchool）（private）
                ("我", "好乱。")));
    }

    // 生成 GenerateStore 对应资源
    private static void GenerateStore()
    {
// 推进 GenerateStore 中的必要步骤
        Clue("store_note", "收银台便条",
            "收银台旁边，压在计算器下面的手写购物清单。最后一行写着“别忘了买糖”。",
// 推进 GenerateStore 的当前步骤
            "{sibling}留下的购物清单。",
            "购物清单是你写的。“别忘了买糖”——是你留给自己的话。",
// 推进 GenerateStore 中的必要步骤（GenerateStore）
            Dlg("Dlg_store_note", false, null,
                ("", "（收银机旁边，计算器还亮着：0.00。下面压着一张撕下来的手写清单。）"),
// 推进 GenerateStore 的当前步骤（GenerateStore）
                ("我", "一张购物清单……牛奶、面包。最后一行写着“别忘了买糖”。")));

        Clue("store_toy", "货架上的玩具",
            "货架第三层一个落满灰的塑料小猫玩具。",
// 推进 GenerateStore 的当前步骤（GenerateStore）（Dlg）
            "小时候好像也有一个一模一样的。",
            "不是“好像也有一个”。就是这一个。",
// 在 GenerateStore 中处理 Dlg
            Dlg("Dlg_store_toy", false, null,
                ("", "（货架第三层，一个落满灰的塑料小猫玩具。）"),
// 推进 GenerateStore 的当前步骤（GenerateStore）（Clue）
                ("我", "这个玩具……小时候我好像也有一个。")));

        Clue("store_handprint", "门上的手印",
// 在 GenerateStore 中继续当前处理
            "便利店后门把手附近一个清晰的手印，指纹方向是往外推门的。",
            "有人从这里推门出去过。手印不太大。",
// 在 GenerateStore 中继续当前处理（GenerateStore 后续步骤）
            "手印当然是你的大小。是你自己推开了这扇门。",
            Dlg("Dlg_store_handprint", false, null,
// 在 GenerateStore 中继续当前处理（GenerateStore 后续步骤）（348）
                ("", "（便利店后门。接近把手的位置有一个清晰的手印。你下意识把手覆了上去。）"),
                ("我", "……刚好是我的手掌大小。指纹方向……是往外推门的。")));

// 在 GenerateStore 中处理 Clue
        Clue("store_poster", "墙上的海报",
            "墙上贴着各区域商品打折的促销海报。", "好像从{sibling}消失前就贴着了。", "一张留在墙上的旧促销海报。",
// 在 GenerateStore 中处理 Dlg（GenerateStore 后续步骤）
            Dlg("Dlg_store_poster", false, null,
                ("", "（海报上写着这里各区域商品的打折信息。）"),
// 推进 GenerateStore 的当前步骤（GenerateStore）（sibling）
                ("我", "好像从{sibling}消失前就贴着了？")));
        Clue("store_vegetables", "蔬菜区",
// 在 GenerateStore 中继续当前处理（GenerateStore 后续步骤）（361）
            "剩下零零散散的蔬菜，有的已经烂掉，有的品相很差。", "被挑剩的蔬菜。", "这些蔬菜已经放了太久。",
            Dlg("Dlg_store_vegetables", false, null,
// 在 GenerateStore 中继续当前处理（GenerateStore 后续步骤）（364）
                ("", "（剩下一些零零散散的蔬菜。）"),
                ("我", "应该说不愧是被挑剩的吗？"),
// 在 GenerateStore 中继续当前处理（GenerateStore 后续步骤）（367）
                ("", "（剩下的蔬菜有的已经有些烂掉了，还有的品相很差。）")));
        Clue("store_fruit", "水果区",
// 在 GenerateStore 中继续当前处理（GenerateStore 后续步骤）（370）
            "水果区放着蓝莓和南瓜。", "为什么水果区还有南瓜？", "货架上留下了不同的食物。",
            Dlg("Dlg_store_fruit", false, null,
// 推进 GenerateStore 的当前步骤（GenerateStore）（private）
                ("", "（放着一些水果？）"),
                ("我", "为什么，除了蓝莓，还会有南瓜啊？")));
    }

    // 生成 GenerateAlley 对应资源
    private static void GenerateAlley()
    {
// 推进 GenerateAlley 中的必要步骤
        Clue("alley_graffiti", "墙上的涂鸦",
            "小巷左侧墙面约一米五高的位置，黑色马克笔写着：“x…对不起”，前面的字被刮掉了。",
// 推进 GenerateAlley 的当前步骤
            "有人写下的道歉，对象不明。",
            "被刮掉的是你的名字。对不起——是你想对自己说的话。",
// 推进 GenerateAlley 中的必要步骤（GenerateAlley）
            Dlg("Dlg_alley_graffiti", false, null,
                ("", "（小巷左侧墙面上有一行黑色马克笔字：“x…对不起”。前面的字母被人用力刮掉了。）"),
// 推进 GenerateAlley 的当前步骤（GenerateAlley）
                ("我", "什么……对不起？前面被刮掉了……")));

        Clue("alley_cigs", "地上的烟头",
// 推进 GenerateAlley 的当前步骤（GenerateAlley）（Dlg）
            "靠近墙角的一平方米内散落着五六根发黄的烟头。",
            "有人在这里待了很久。是在等谁吗？",
// 在 GenerateAlley 中继续当前处理
            "在这里等待的人是你。你一直在等自己回来。",
            Dlg("Dlg_alley_cigs", false, null,
// 推进 GenerateAlley 的当前步骤（GenerateAlley）（sibling）
                ("", "（靠近墙角的一平方米内散落着五六根烟头，已经发黄了。有人曾在这里站了很久很久。）"),
                ("我", "感觉有人在这里待了很久。“{sibling}”……你是在等我吗？")));

// 在 GenerateAlley 中处理 Clue
        Clue("alley_poster", "旧海报",
            "小巷右侧墙面贴着层层叠叠的旧海报，最上方一条乐队海报下露出旧寻人启事：xx，18岁，以及失踪日期和照片。",
// 推进 GenerateAlley 的当前步骤（GenerateAlley）（xx）
            "一张残缺的寻人启事：“xx，18岁，于……”。是谁贴的？",
            "寻人启事是你贴的。你在寻找的，从一开始就是你自己。",
// 在 GenerateAlley 中处理 Dlg
            Dlg("Dlg_alley_poster", false, null,
                ("", "（小巷右侧墙面贴着层层叠叠的旧海报，拨开最上方一条乐队海报——底下露出一张旧寻人启事。）"),
// 在 GenerateAlley 中继续当前处理（GenerateAlley 后续步骤）
                ("我", "寻人启事。“xx，18岁，于……”后面的日期被撕掉了……照片也不见了。"),
                ("我", "这张寻人启事……是谁贴的？")));

// 在 GenerateAlley 中处理 Clue（GenerateAlley 后续步骤）
        Clue("alley_bin", "垃圾桶里的破损钥匙",
            "像油桶的垃圾桶里有一把破损的钥匙。", "有点像之前坏掉的那把家门钥匙。", "一把让你感到熟悉的破损钥匙。",
// 在 GenerateAlley 中处理 Dlg（GenerateAlley 后续步骤）
            Dlg("Dlg_alley_bin", false, null,
                ("", "（有一个垃圾桶倒在了地上，另一个依旧在那屹立着。）"),
                ("我", "这个垃圾桶……有点像油桶"),
// 推进 GenerateAlley 的当前步骤（GenerateAlley）（Clue）
                ("", "（里面好像有什么东西。）"),
                ("", "（你发现了一把破损的钥匙。）"),
// 在 GenerateAlley 中继续当前处理（GenerateAlley 后续步骤）（427）
                ("我", "为什么……有点像我之前坏掉的那把家门钥匙？")));
        // 建立当前运行关联
        Clue("alley_notice_board", "破旧的告示板",
// 在 GenerateAlley 中继续当前处理（GenerateAlley 后续步骤）（431）
            "贴满了破旧的广告和公告。", "好像没有有用的消息。", "日常广告层层覆盖着告示板。",
            Dlg("Dlg_alley_notice_board", false, null,
// 在 GenerateAlley 中继续当前处理（GenerateAlley 后续步骤）（434）
                ("", "（贴满了破旧的广告和公告。）"),
                ("我", "好像没有有用的消息。"),
// 在 GenerateAlley 中继续当前处理（GenerateAlley 后续步骤）（437）
                ("我", "专业疏通下水道  电话……"),
                ("我", "打孔、开锁、通马桶  电话……"),
// 在 GenerateAlley 中继续当前处理（GenerateAlley 后续步骤）（440）
                ("我", "办证 身份证 驾照  电话……"),
                ("我", "高价回收空调、冰箱、旧手机  电话……"),
// 在 GenerateAlley 中继续当前处理（GenerateAlley 后续步骤）（443）
                ("我", "房屋出租 一房一厅  电话……"),
                ("我", "嗯，确实没有有用的东西。")));
// 在 GenerateAlley 中处理 Clue（GenerateAlley 后续步骤）（后续处理 2）
        Clue("alley_drain", "下水道口",
            "下水道口居然没什么垃圾堵住。", "没有堵塞，却散发着臭味。", "小巷里的一处下水道口。",
            // 解除当前运行关联
            Dlg("Dlg_alley_drain", false, null,
                ("", "（上面居然没什么垃圾堵住，这是最令你意外的点。）"),
// 推进 GenerateAlley 的当前步骤（GenerateAlley）（private）
                ("我", "嗯……"),
                ("我", "好臭。")));
    }

    // 生成 GeneratePlayground 对应资源
    private static void GeneratePlayground()
    {
        Clue("pg_carousel", "旋转木马",
// 推进 GeneratePlayground 的当前步骤
            "游乐场中央，其中一匹木马的背上刻着一行字。",
            "“许愿：永远在一起”。",
// 推进 GeneratePlayground 的当前步骤（GeneratePlayground）
            "一个人对自己许下的愿望。",
            Dlg("Dlg_pg_carousel", false, null,
                ("", "（旋转木马还在原地，木马都斑驳掉漆了。其中一匹的背上刻着一行小字。）"),
// 推进 GeneratePlayground 的当前步骤（GeneratePlayground）（sibling）
                ("我", "这匹木马背上刻着……“许愿：永远在一起”，我也希望和{sibling}永远在一起。")));

        Clue("pg_ferris", "摩天轮",
// 推进 GeneratePlayground 的当前步骤（GeneratePlayground）（Dlg）
            "游乐场边缘的摩天轮停着，5号座舱的门是打开的。",
            "一个熟悉的座舱。",
// 在 GeneratePlayground 中继续当前处理
            "你坐过很多次，而且每一次都是一个人。",
            Dlg("Dlg_pg_ferris", false, null,
// 推进 GeneratePlayground 的当前步骤（GeneratePlayground）（Clue）
                ("", "（摩天轮停在半空，5号座舱微微晃动，门敞开着。）"),
                ("我", "我好像在这里坐过很多次。……一个人。")));

// 推进 GeneratePlayground 中的必要步骤
        Clue("pg_bench", "长椅",
            "旋转木马旁边的长椅，椅背上刻着两个名字，其中一个被划掉了。",
// 在 GeneratePlayground 中继续当前处理（GeneratePlayground 后续步骤）
            "很旧的划痕，和之前墙上的刮痕一样。",
            "两个名字都与你有关，被划掉的名字也是你留下的。",
// 推进 GeneratePlayground 中的必要步骤（GeneratePlayground）
            Dlg("Dlg_pg_bench", false, null,
                ("", "（小型过山车旁边的长椅。椅背上刻着两个名字，其中一个被划掉了。）"),
// 在 GeneratePlayground 中继续当前处理（GeneratePlayground 后续步骤）（491）
                ("我", "划痕很旧……和之前墙上的刮痕一样。")));

        Clue("pg_booth", "售票亭",
// 在 GeneratePlayground 中继续当前处理（GeneratePlayground 后续步骤）（495）
            "入口处的铁皮亭子，窗户碎了一半，里面有一卷旧票根。",
            "同一场次的旧票根，全是单人票。",
// 在 GeneratePlayground 中继续当前处理（GeneratePlayground 后续步骤）（498）
            "回忆里的“两个人”，每一次都只有一张票。",
            Dlg("Dlg_pg_booth", false, null,
// 在 GeneratePlayground 中继续当前处理（GeneratePlayground 后续步骤）（501）
                ("", "（入口处的铁皮亭子窗户碎了一半，里面落着一卷旧票根。）"),
                ("我", "这些票根……都是同一场次的。……单人票。每一次都是单人票。")));

// 在 GeneratePlayground 中处理 Clue
        Clue("pg_sign", "路标",
            "前方的路标写着：摩天轮；左转：过山车；右转：许愿湖。",
// 在 GeneratePlayground 中继续当前处理（GeneratePlayground 后续步骤）（508）
            "通往三个游乐设施的路标。",
            "你对这里的路线很熟悉。",
            Dlg("Dlg_pg_sign", false, null,
// 在 GeneratePlayground 中继续当前处理（GeneratePlayground 后续步骤）（512）
                ("", "（路标歪在风里，箭头分别指向摩天轮、过山车和许愿湖。）"),
                ("我", "摩天轮……")));

// 在 GeneratePlayground 中处理 Clue（GeneratePlayground 后续步骤）
        Clue("pg_wishing_lake", "许愿湖",
            "旋转木马背后的普通湖泊，被游乐场染上了奇幻的色彩。",
// 在 GeneratePlayground 中继续当前处理（GeneratePlayground 后续步骤）（519）
            "小时候曾和{sibling}在这里许愿。",
            "你曾把寻找{sibling}的愿望也交给过这片湖。",
// 在 GeneratePlayground 中处理 Dlg
            Dlg("Dlg_pg_wishing_lake", false, null,
                ("", "（旋转木马背后有一片普通的湖。因为这里是游乐场，湖面也像被染上了奇幻的色彩。）"),
// 在 GeneratePlayground 中继续当前处理（GeneratePlayground 后续步骤）（525）
                ("我", "以前会跟{sibling}在这里许愿。无非是希望成绩好一些，零花钱多一些之类的愿望。"),
                ("我", "许愿湖啊，如果我希望我能找到消失的{sibling}，你能实现我的愿望吗…")));

// 在 GeneratePlayground 中处理 Clue（GeneratePlayground 后续步骤）（后续处理 2）
        Clue("pg_rollercoaster", "过山车",
            "游乐场最深处最高的过山车登车平台，再往前已经没有必要了。",
// 在 GeneratePlayground 中继续当前处理（GeneratePlayground 后续步骤）（532）
            "你曾和{sibling}一起登上过这里。",
            "即使害怕，{sibling}还是每次都陪你上去。",
// 在 GeneratePlayground 中处理 Dlg（GeneratePlayground 后续步骤）
            Dlg("Dlg_pg_rollercoaster", false, null,
                ("", "（再往里走，就是这个游乐场最大最高的过山车登车平台。不过，没必要再往前了。）"),
// 在 GeneratePlayground 中继续当前处理（GeneratePlayground 后续步骤）（538）
                ("我", "以前{sibling}很害怕坐过山车，但是都会陪我上去。")));

        Clue("pg_kids_coaster", "小型过山车",
// 在 GeneratePlayground 中继续当前处理（GeneratePlayground 后续步骤）（542）
            "相对较小的过山车，更适合年纪小的孩子玩。",
            "你曾经玩过一次，却觉得还不够。",
// 在 GeneratePlayground 中继续当前处理（GeneratePlayground 后续步骤）（545）
            "你拉着害怕的{sibling}去了另一座过山车。",
            Dlg("Dlg_pg_kids_coaster", false, null,
// 在 GeneratePlayground 中继续当前处理（GeneratePlayground 后续步骤）（548）
                ("", "（这座过山车更小，轨道也更低，像是专门给年纪小的孩子准备的。）"),
                ("我", "印象中，我玩了一次这个不过瘾，就拉着{sibling}去另一个过山车了。"),
// 在 GeneratePlayground 中继续当前处理（GeneratePlayground 后续步骤）（551）
                ("我", "当时{sibling}口头上疯狂拒绝，但还是陪我去了。")));
    }

    // 生成 GenerateRooftop 对应资源
    private static void GenerateRooftop()
    {
// 推进 GenerateRooftop 中的必要步骤
        Clue("roof_chair", "椅子",
            "天台正中央的一把旧木椅，椅面磨损严重。",
            "像是有人常年坐在这里。",
// 推进 GenerateRooftop 的当前步骤
            "你曾经常年坐在这里。18岁那年，也是坐在这里做出了那个决定。",
            Dlg("Dlg_roof_chair", false, null,
// 推进 GenerateRooftop 的当前步骤（GenerateRooftop）
                ("", "（天台正中央放着一把旧木椅，椅面磨得发亮。风很大。）"),
                ("我", "这把椅子放在这里多久了？……像是有人常年坐在这里。")));

// 推进 GenerateRooftop 中的必要步骤（GenerateRooftop）
        Clue("roof_diary", "日记",
            "椅子上翻开的皮质日记，纸张泛黄。笔迹和自己的一模一样。",
// 推进 GenerateRooftop 的当前步骤（GenerateRooftop）（Dlg）
            "翻开在最后一页的日记。这笔迹……",
            "日记是你写的。从头到尾都只有一个人。",
// 在 GenerateRooftop 中处理 Dlg
            Dlg("Dlg_roof_diary", false, null,
                ("", "（椅子上放着一本翻开的日记。皮质封面，纸张泛黄。笔迹和你的一模一样。）"),
// 推进 GenerateRooftop 的当前步骤（GenerateRooftop）（Clue）
                ("", "“如果你在读这行字，说明你已经走过了很远的路……从头到尾都只有一个人……”")));

        Clue("roof_diary_page", "日记里的自画像",
// 推进 GenerateRooftop 的当前步骤（GenerateRooftop）（sibling）
            "日记里夹着一张泛黄的自画像：一个人坐在天台的椅子上。",
            "右下角写着：“画于18岁生日前一天。”",
// 在 GenerateRooftop 中继续当前处理
            "自画像画的是你。18岁生日前一天——“{sibling}”消失的前一天。",
            Dlg("Dlg_roof_diary_page", false, null,
// 在 GenerateRooftop 中继续当前处理（GenerateRooftop 后续步骤）
                ("", "（你翻过一页。里面夹着一张泛黄的画——一个人坐在天台的椅子上。）"),
                ("我", "右下角写着一行小字：“画于18岁生日前一天。”")));

// 在 GenerateRooftop 中处理 Clue
        Clue("roof_footprints", "地上的脚印",
            "灰尘上从楼梯口到椅子、从椅子到围栏来回多次的脚印。",
// 在 GenerateRooftop 中继续当前处理（GenerateRooftop 后续步骤）（595）
            "来回走了很多次的脚印。",
            "脚印全是你的。来回多次——你比记忆中更常回到这里。",
// 在 GenerateRooftop 中处理 Dlg（GenerateRooftop 后续步骤）
            Dlg("Dlg_roof_footprints", false, null,
                ("", "（地面的薄灰上印着杂乱的脚印。从楼梯口到椅子，从椅子到围栏……来回，来回。）"),
// 在 GenerateRooftop 中继续当前处理（GenerateRooftop 后续步骤）（601）
                ("我", "全是同一个人的脚印。")));

        Clue("roof_reflection", "玻璃门上的倒影",
            "夕阳照在脏玻璃门上，变成一面镜子。",
// 在 GenerateRooftop 中继续当前处理（GenerateRooftop 后续步骤）（606）
            "倒影里的自己……在微笑。",
            "倒影里微笑的人，就是你要找的人。",
// 在 GenerateRooftop 中处理 Dlg（GenerateRooftop 后续步骤）（后续处理 2）
            Dlg("Dlg_roof_reflection", false, null,
                ("", "（夕阳斜照在脏玻璃门上。玻璃变成了一面昏黄的镜子。）"),
// 在 GenerateRooftop 中继续当前处理（GenerateRooftop 后续步骤）（612）
                ("", "（倒影里的人——长着“{sibling}”的脸。那张脸，在微笑。）"),
                ("", "（那是你的脸。）"),
// 在 GenerateRooftop 中继续当前处理（GenerateRooftop 后续步骤）（615）
                ("我", "…………")));

        Clue("roof_diary_final", "日记的最后一页",
// 在 GenerateRooftop 中继续当前处理（GenerateRooftop 后续步骤）（619）
            "再次翻开日记，最后一页的文字变了。",
            "“你终于认出我了。”",
// 在 GenerateRooftop 中继续当前处理（GenerateRooftop 后续步骤）（622）
            "你终于认出我了。",
            Dlg("Dlg_roof_diary_final", false, null,
// 推进 GenerateRooftop 的当前步骤（GenerateRooftop）（private）
                ("", "（你回到椅子边，再次翻开日记。最后一页的文字——变了。）"),
                ("", "“你终于认出我了。”")));
    }

// 生成 GenerateSceneIntrosAndClears 对应资源
    private static void GenerateSceneIntrosAndClears()
    {
        // 开场白：勾选计数（6 个开场 + 28 个物品；补充线索首次调查也只计数一次）
        Dlg("Dlg_intro_home", true, null,
// 推进 GenerateSceneIntrosAndClears 的当前步骤
            ("", "（黄昏的光从窗帘缝里挤进来，灰尘在光柱里漂浮。你从沙发上醒来。）"),
            ("我", "……我又睡着了吗。"),
// 推进 GenerateSceneIntrosAndClears 的当前步骤（GenerateSceneIntrosAndClears）
            ("我", "{sibling}失踪已经很久了。所有人都说没有这个人……但我记得。我一定要找到{ta}。"),
            ("", "（餐桌上，好像放着什么东西。）"));

// 推进 GenerateSceneIntrosAndClears 中的必要步骤
        Dlg("Dlg_intro_school", true, null,
            ("", "（走廊的日光灯管在闪。墙上的钟停在12:30。课桌椅东倒西歪。）"),
// 推进 GenerateSceneIntrosAndClears 的当前步骤（GenerateSceneIntrosAndClears）（sibling）
            ("我", "沿着记忆里的路走，就到了学校。{sibling}在这里上过学……我记得的。"));

        Dlg("Dlg_intro_store", true, null,
// 在 GenerateSceneIntrosAndClears 中继续当前处理
            ("", "（整条街只有这家便利店还亮着半块灯牌。收银台上的计算器亮着：0.00。）"),
            ("我", "{sibling}以前放学后总来这里买东西。……我怎么会记得这么清楚？"));

        Dlg("Dlg_intro_alley", true, null,
// 推进 GenerateSceneIntrosAndClears 的当前步骤（GenerateSceneIntrosAndClears）（Dlg）
            ("", "（小巷很窄，只够两个人并排走。墙上的海报被雨水泡烂了，露出下面一层旧广告。）"),
            ("我", "只够两个人并排走的巷子。……或者，一个人。"));

// 推进 GenerateSceneIntrosAndClears 中的必要步骤（GenerateSceneIntrosAndClears）
        Dlg("Dlg_intro_playground", true, null,
            ("", "（穿过小巷后，视野忽然开阔。眼前是一个废弃的小型游乐场。黄昏里，旋转木马还在原地，木马都斑驳掉漆了；摩天轮停着，座舱在半空中微微晃动，落叶被风吹得打转。）"),
// 在 GenerateSceneIntrosAndClears 中继续当前处理（GenerateSceneIntrosAndClears 后续步骤）
            ("我", "这个游乐场……小时候我经常来。和{sibling}一起。……和{sibling}一起？"));

        Dlg("Dlg_intro_rooftop", true, null,
// 在 GenerateSceneIntrosAndClears 中继续当前处理（GenerateSceneIntrosAndClears 后续步骤）（665）
            ("", "（风越来越大。推开锈住的铁门——天台。深蓝色的天空，介于黄昏和夜晚之间。）"),
            ("我", "正中央……放着一把椅子。椅子上有一本翻开的日记。"));

        // 通关独白：不计数
        Dlg("Dlg_clear_home", false, null,
            ("", "（门打开了。客厅的窗帘被风吹起——门口，一个模糊的影子站了一秒，然后消失了。）"),
// 在 GenerateSceneIntrosAndClears 中继续当前处理（GenerateSceneIntrosAndClears 后续步骤）（672）
            ("我", "……你是在给我带路吗？"));

        Dlg("Dlg_clear_school", false, null,
// 在 GenerateSceneIntrosAndClears 中继续当前处理（GenerateSceneIntrosAndClears 后续步骤）（676）
            ("", "（讲台旁边出现一个模糊的侧影，坐在课桌前写字。写了几笔，就消失了。）"),
            ("", "黑板上多了一行字：“往前走，别回头。”"));

// 在 GenerateSceneIntrosAndClears 中处理 Dlg
        Dlg("Dlg_clear_store", false, null,
            ("", "（推开后门的瞬间，便利店的灯全部暗了。）"),
// 在 GenerateSceneIntrosAndClears 中继续当前处理（GenerateSceneIntrosAndClears 后续步骤）（683）
            ("", "（门口站着一个模糊的影子。影子抬起手，指了指前方的路。然后消失了。）"));

        Dlg("Dlg_clear_alley", false, null,
// 在 GenerateSceneIntrosAndClears 中继续当前处理（GenerateSceneIntrosAndClears 后续步骤）（687）
            ("", "（巷子两侧墙上的涂鸦突然全部亮了起来——一瞬间，全是同一句话。）"),
            ("", "“往前走。”"),
// 在 GenerateSceneIntrosAndClears 中继续当前处理（GenerateSceneIntrosAndClears 后续步骤）（690）
            ("", "“别停。”"),
            ("", "“我在前面等你。”"));

// 在 GenerateSceneIntrosAndClears 中处理 Dlg（GenerateSceneIntrosAndClears 后续步骤）
        Dlg("Dlg_clear_playground", false, null,
            ("", "（你朝出口走去。身后，旋转木马上出现了一个坐着的影子。）"),
// 在 GenerateSceneIntrosAndClears 中继续当前处理（GenerateSceneIntrosAndClears 后续步骤）（697）
            ("", "（影子朝你挥了挥手。）"),
            ("", "（但你没有回头。）"));

        // 天台结局（黑幕后播放）
        Dlg("Dlg_clear_rooftop", false, null,
// 推进 GenerateSceneIntrosAndClears 的当前步骤（GenerateSceneIntrosAndClears）（private）
            ("", "（你合上日记，走到天台楼梯口，回头看了一眼那把椅子。）"),
            ("", "（然后推开门，走了回去。）"),
// 在 GenerateSceneIntrosAndClears 中继续当前处理（GenerateSceneIntrosAndClears 后续步骤）（706）
            ("", "……"),
            ("我", "后来我再也没有上过这个天台。"),
// 在 GenerateSceneIntrosAndClears 中继续当前处理（GenerateSceneIntrosAndClears 后续步骤）（709）
            ("我", "因为已经不需要了。"));
    }

    private static void GenerateSystemDialogues()
    {
        // 回溯闪回（暂以文字演出，美术闪回图就绪后填入事件表的 images 即可叠加）
        Dlg("Dlg_inv_5", false, null,
            ("", "（视野突然闪烁——一瞬间，你看到一个背影站在路的尽头。还没看清，画面就恢复了。）"),
// 推进 GenerateSystemDialogues 的当前步骤
            ("我", "刚才那是……{sibling}？"));

        Dlg("Dlg_inv_10", false, null,
// 推进 GenerateSystemDialogues 的当前步骤（GenerateSystemDialogues）
            ("我", "为什么这些线索……都像是我自己留下的？"),
            ("我", "……不。不会的。我只是太累了。"));

// 推进 GenerateSystemDialogues 中的必要步骤
        Dlg("Dlg_inv_15", false, null,
            ("", "（又是那种感觉。你看到“{sibling}”俯身在写一张便条——上面的字，和你刚才捡到的那张，一模一样。）"),
// 推进 GenerateSystemDialogues 的当前步骤（GenerateSystemDialogues）（Dlg）
            ("我", "……为什么？"));

        Dlg("Dlg_inv_25", false, null,
// 推进 GenerateSystemDialogues 的当前步骤（GenerateSystemDialogues）（sibling）
            ("", "（这次更清晰了。“{sibling}”站在一面镜子前。）"),
            ("", "（镜子里反射出来的脸——是你自己的脸。）"),
// 在 GenerateSystemDialogues 中继续当前处理
            ("我", "…………不对。不对不对不对。"));

        Dlg("Dlg_inv_28", false, null,
// 在 GenerateSystemDialogues 中继续当前处理（GenerateSystemDialogues 后续步骤）
            ("", "（身后传来什么东西合上的声音。回去的路，好像已经不在了。）"),
            ("我", "……只能往前走了。"));

        // 封锁台词
        Dlg("Dlg_locked_home", false, null,
            ("我", "这地方我翻遍了……没有更多线索了。"));

        // 门的通用台词
        Dlg("Dlg_door_locked", false, null,
            ("我", "还不能走。……总觉得这里还有没找到的东西。"));

        Dlg("Dlg_door_noreturn", false, null,
// 在 GenerateSystemDialogues 中继续当前处理（GenerateSystemDialogues 后续步骤）（753）
            ("", "（门推不开。像是有什么在告诉你——过去回不去了。）"));

        // 坏结局：前五关未全部查过时，翻开日记最后一页触发（EndingGate 播完回主菜单）
        Dlg("Dlg_bad_ending", false, null,
// 在 GenerateSystemDialogues 中继续当前处理（GenerateSystemDialogues 后续步骤）（758）
            ("", "（你翻开最后一页。上面……什么都没有。）"),
            ("我", "……"),
// 在 GenerateSystemDialogues 中继续当前处理（GenerateSystemDialogues 后续步骤）（761）
            ("我", "这一切都是假的吧。{sibling}的下落，根本就查不出来。"),
            ("我", "我被那封信骗了……"));
    }

// 生成 GenerateEventTable 对应资源
    private static void GenerateEventTable()
    {
        var table = GetOrCreate<InvestigationEventTable>(Root + "/InvestigationEventTable.asset");

// 同步 GenerateEventTable 的状态
        table.events = new[]
        {
            Evt(5,  "回溯1·背影",   null,                          "Dlg_inv_5",  "……{sibling}？"),
// 推进 GenerateEventTable 中的必要步骤
            Evt(8,  "封锁·家",     new[] { "lock_home_items" },   null,         null),
            Evt(10, "动摇独白",     null,                          "Dlg_inv_10", null),
// 推进 GenerateEventTable 中的必要步骤（GenerateEventTable）
            Evt(15, "回溯2·便条",   null,                          "Dlg_inv_15", null),
            Evt(18, "封锁·路人",   new[] { "lock_npc_talk" },     null,         null),
// 在 GenerateEventTable 中处理 Evt
            Evt(25, "回溯3·镜子",   null,                          "Dlg_inv_25", null),
            Evt(28, "封锁·回头路", new[] { "lock_early_scenes" }, "Dlg_inv_28", null),
// 推进 GenerateEventTable 的当前步骤
        };

        EditorUtility.SetDirty(table);
    }

// 完成 GenerateEventTable 的主要职责
    private static InvestigationEventTable.ThresholdEvent Evt(
        int threshold, string label, string[] flags, string dialogueName, string caption)
    {
// 返回 GenerateEventTable 的处理结果
        return new InvestigationEventTable.ThresholdEvent
        {
            threshold = threshold,
// 同步 GenerateEventTable 的内部状态
            editorLabel = label,
            setFlags = flags ?? new string[0],
// 同步 GenerateEventTable 的内部状态（GenerateEventTable）
            monologue = dialogueName != null
                ? AssetDatabase.LoadAssetAtPath<DialogueData>($"{DlgDir}/{dialogueName}.asset")
// 推进 GenerateEventTable 的当前步骤（GenerateEventTable）
                : null,
            flashback = new FlashbackSequence { caption = caption ?? string.Empty, secondsPerImage = 1.2f },
        };
    }

    // 生成 GenerateClueDatabase 对应资源
    private static void GenerateClueDatabase()
    {
        var db = GetOrCreate<ClueDatabase>(Root + "/ClueDatabase.asset");

// 同步 GenerateClueDatabase 的相关数据
        var so = new SerializedObject(db);
        var list = so.FindProperty("allClues");
// 同步 GenerateClueDatabase 的内部状态
        list.arraySize = allClues.Count;
        for (int i = 0; i < allClues.Count; i++)
        {
// 使用 GenerateClueDatabase 所需功能
            list.GetArrayElementAtIndex(i).objectReferenceValue = allClues[i];
        }

        var requiredList = so.FindProperty("trueEndingRequiredClues");
// 同步 GenerateClueDatabase 的内部状态（GenerateClueDatabase）
        requiredList.arraySize = trueEndingRequiredClues.Count;
        for (int i = 0; i < trueEndingRequiredClues.Count; i++)
        {
// 使用 GenerateClueDatabase 所需功能（GenerateClueDatabase）
            requiredList.GetArrayElementAtIndex(i).objectReferenceValue = trueEndingRequiredClues[i];
        }

        so.ApplyModifiedPropertiesWithoutUndo();
    }

// 处理 Clue 对应逻辑
    private static ClueData Clue(string id, string title, string description,
        string surface, string truth, DialogueData inspectDialogue)
    {
// 保存 clue 引用
        var clue = GetOrCreate<ClueData>($"{ClueDir}/Clue_{id}.asset");

        var so = new SerializedObject(clue);
// 使用 Clue 所需功能
        so.FindProperty("clueId").stringValue = id;
        so.FindProperty("title").stringValue = title;
// 使用 Clue 所需功能（Clue）
        so.FindProperty("description").stringValue = description;
        so.FindProperty("surfaceMeaning").stringValue = surface;
// 在 Clue 中处理 FindProperty
        so.FindProperty("trueMeaning").stringValue = truth;
        so.ApplyModifiedPropertiesWithoutUndo();

        // 调查对话结束时发放本线索
        if (inspectDialogue != null)
        {
            inspectDialogue.grantCluesOnComplete = new[] { clue };
// 在 Clue 中处理 SetDirty
            EditorUtility.SetDirty(inspectDialogue);
        }

        allClues.Add(clue);
        return clue;
    }

    // 处理 Dlg 对应逻辑
    private static DialogueData Dlg(string assetName, bool countsAsInvestigation,
        string[] setFlags, params (string speaker, string text)[] lines)
    {
// 保存 dlg 引用
        var dlg = GetOrCreate<DialogueData>($"{DlgDir}/{assetName}.asset");

        dlg.lines = new DialogueData.Line[lines.Length];
// 循环处理当前集合
        for (int i = 0; i < lines.Length; i++)
        {
            dlg.lines[i] = new DialogueData.Line
            {
// 同步 Dlg 的内部状态
                speakerName = lines[i].speaker,
                text = lines[i].text,
// 推进 Dlg 的当前步骤
            };
        }

        dlg.countsAsInvestigation = countsAsInvestigation;
// 同步 Dlg 的内部状态（Dlg）
        dlg.setFlagsOnComplete = setFlags ?? new string[0];
        dlg.grantCluesOnComplete = new ClueData[0];
// 同步 Dlg 的内部状态（Dlg）（dlg）
        dlg.advanceTimeOnComplete = 0;

        EditorUtility.SetDirty(dlg);
// 返回 Dlg 的处理结果
        return dlg;
    }

    private static T GetOrCreate<T>(string path) where T : ScriptableObject
    {
// 同步 Dlg 的相关数据
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        // CreateAsset 只能用于尚未存在的目标路径
        if (asset == null)
        {
            // 新建资产会由 Unity 同时创建对应的 .meta 文件
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
        }

// 返回 Dlg 的处理结果（Dlg）
        return asset;
    }

    // 处理 EnsureFolder 对应逻辑
    private static void EnsureFolder(string path)
    {
        // 已存在时不操作，保证可重复执行
        if (!AssetDatabase.IsValidFolder(path))
        {
            string parent = path.Substring(0, path.LastIndexOf('/'));
// 完成 EnsureFolder 的主要职责
            string leaf = path.Substring(path.LastIndexOf('/') + 1);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
#endif
