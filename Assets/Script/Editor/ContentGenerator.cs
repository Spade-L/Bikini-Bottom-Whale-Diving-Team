#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 一键生成《Trace Me（寻己）》全部内容资产（线索、对话、事件表、线索数据库）。
/// 菜单：Trace Me > 生成全部内容资产
/// 可重复执行：已存在的资产会被更新（GUID 不变，场景引用不丢）。
/// 文本中的 {sibling}/{ta}/{kin} 在运行时按玩家性别替换为 哥哥/他/好兄弟 或 姐姐/她/好姐妹。
/// </summary>
public static class ContentGenerator
{
    // 编辑器专用生成流程说明：
    // 菜单命令只在 UNITY_EDITOR 条件下编译。
    // 运行时程序集不会引用 UnityEditor API。
    // 生成目标必须位于项目的 Assets 目录内。
    // 目录由 AssetDatabase 以项目相对路径识别。
    // 每次生成先确保所有目标目录存在。
    // 已存在目录不会被删除或重建。
    // 已存在资产会按相同路径重新加载。
    // 重新加载不会替换该资产的 .meta 文件。
    // 因此 Unity 会保持原有资产 GUID。
    // 场景、预制体和其他资产的 GUID 引用可继续有效。
    // 同路径重复生成只更新可写入的序列化字段。
    // 生成器不会自动保留这些字段的手动改动。
    // 内容调整应修改本文件后重新执行菜单。
    // 删除 .asset 会使下次生成创建新资产。
    // 删除或重建 .meta 也会改变资产 GUID。
    // GUID 变化后，现有序列化引用需要重新关联。
    // allClues 仅保存本轮生成期间的内存引用。
    // Clear 不会删除任何已创建的线索资产。
    // 场景方法按固定顺序填充该内存列表。
    // 数据库在全部线索生成后一次性写入。
    // 该顺序也会成为数据库的默认线索顺序。
    // 对话必须先创建，线索才能引用其完成奖励。
    // 事件表也必须在对应对话创建后生成。
    // SerializedObject 用于访问私有序列化字段。
    // ApplyModifiedPropertiesWithoutUndo 不创建编辑器撤销记录。
    // EditorUtility.SetDirty 只标记资产待保存。
    // SaveAssets 才会统一写入磁盘。
    // Refresh 让导入管线和 Project 窗口识别新增资产。
    // 本工具不会保存或打开场景。
    // 本工具不会执行运行时对白、拾取或事件逻辑。
    // 本工具只生成由运行时系统消费的数据资产。
    // 线索 ID 是运行时查找和存档使用的稳定键。
    // 线索标题是面向界面显示的文本。
    // 描述、表层含义和真相分别保存到数据字段。
    // Dlg_ 前缀资产是可复用的对白数据。
    // 空说话人名称表示旁白或环境文本。
    // 对白行数组顺序就是运行时显示顺序。
    // countsAsInvestigation 影响调查进度统计。
    // setFlagsOnComplete 在对白结束时由运行时设置。
    // grantCluesOnComplete 在对白结束时由运行时发放。
    // 生成时会重置对白的这些可生成配置。
    // 如需手动扩展，应在生成源中声明而非直接改资产。
    // 文本占位符保留给运行时的玩家性别替换。
    // 生成器不负责验证占位符是否被运行时支持。
    // 事件阈值按调查数量触发，具体执行由运行时负责。
    // 封锁 flag 的含义由对应运行时逻辑解释。
    // FlashbackSequence 的图片配置可在事件数据中补充。
    // 新建资产时 Unity 会创建对应的 .meta 文件。
    // CreateAsset 仅应在目标路径没有资产时调用。
    // LoadAssetAtPath 失败时才进入新建分支。
    // 目录创建会拆分父路径和最后一级目录名。
    // 目标父目录应已由前序 EnsureFolder 建立。
    // 菜单可重复执行，但应在版本控制中提交生成结果。
    // 合并资产改动时需注意同一数据字段的覆盖。
    // 仅修改注释不会改变上述生成行为。
    // 资产路径是 GUID 引用之外的加载定位依据。
    // 保持路径稳定可避免生成器创建重复内容。
    // 同名路径上的资产会在原对象上更新字段。
    // AssetDatabase 调用只能在编辑器环境执行。
    // 条件编译范围覆盖整个生成器类。
    // 构建时该文件不会向玩家代码提供菜单功能。
    // 生成前建议确认版本控制工作区状态。
    // 生成后应检查新增资产及其 .meta 一并提交。
    // 资源引用异常时先确认 .meta 未被重建。
    // 手动移动资产前应同步更新这里的目录常量。
    // 手动重命名对白资产会影响事件表加载路径。
    // 线索资产名称由 Clue_ 加线索 ID 组成。
    // 对话资产名称由调用时提供的名称组成。
    // 同一线索 ID 不应在不同调用中重复使用。
    // 同一资产路径不应被不同数据类型复用。
    // 生成目录不应存放需要手工维护的同名资产。
    // 生成器以代码内容为最终数据来源。
    // 运行时读取的是生成后的 ScriptableObject 数据。
    // 修改代码后未重新生成不会更新已有数据资产。
    // Refresh 不会替代 SaveAssets 的持久化职责。
    // SaveAssets 不会自动修复失效的场景引用。
    // GUID 稳定性依赖保留既有 .asset 与 .meta 配对。
    // 仅修改注释不会改变上述生成行为。
    // 目录常量统一定义，避免各生成方法拼接出不一致的路径。
    // Root 是全部游戏数据资产的共同父目录。
    // ClueDir 专门保存可调查线索的 ScriptableObject 资产。
    // DlgDir 专门保存对白数据资产。
    // 路径均使用 Unity 识别的项目相对路径格式。
    // 不要改为操作系统绝对路径，否则 AssetDatabase 无法定位资产。
    // 目录名称变化时，旧资产需先在版本控制中完成迁移。
    // 常量之间的拼接关系保持生成目标集中且易于维护。
    // 这些路径仅用于编辑器生成流程。
    // 运行时通过资产引用读取数据，不直接使用本组常量。
    // 创建目录的责任由 GenerateAll 调用 EnsureFolder 承担。
    // 同一目录下的资产名应保持唯一。
    // 线索与对白分目录存放可避免同名资产冲突。
    // 后续新增数据类别应声明独立目录常量。
    // 保持目录结构稳定有助于减少资源迁移成本。
    // 以下三个常量不承载可变运行时状态。
    private const string Root = "Assets/GameData";
    private const string ClueDir = Root + "/Clues";
    private const string DlgDir = Root + "/Dialogues";

    // 本次生成临时收集线索，用于随后重建数据库的排序列表。
    // 列表按各场景生成方法的调用顺序追加。
    // 该顺序决定数据库中默认展示和处理的线索顺序。
    // 此集合只在本次编辑器命令执行期间有效。
    // GenerateAll 开始时会清空旧的内存引用。
    // 列表不直接序列化到任何资产文件。
    // 每个元素均为本轮加载或创建的 ClueData 实例。
    // 数据库写入阶段会逐项复制这些对象引用。
    // 生成流程结束后无需手动释放此静态集合。
    // 重复执行菜单时会以新的完整顺序重新填充。
    // 不应在场景生成方法之外随意插入临时线索。
    // 线索数量日志也以该集合的数量为准。
    // 保持此列表集中收集可避免数据库遗漏资产。
    // 该集合不负责去重，调用方必须保证线索 ID 唯一。
    // 清空集合不会影响已经写入磁盘的任何线索资产。
    // 其职责仅是衔接线索生成和数据库生成两个阶段。
    // 静态只读限定符保证集合实例本身不会被替换。
    // 其中的元素仍会按生成逻辑被新增。
    // 该设计避免每次添加线索时重复读取数据库资产。
    // 数据库最终写入使用 SerializedObject 保持私有字段封装。
    // 这里不保存对白引用，相关引用由 Clue 方法单独配置。
    // 线索资产可被其他编辑器或运行时系统通过 GUID 引用。
    // 因此列表中保存对象引用而非资产路径字符串。
    // 本列表不承担资源生命周期或卸载职责。
    // 编辑器域重载后列表会按下一次菜单执行重新建立。
    // 执行结束后的下一次生成会自然覆盖其内存内容。
    // 下面字段是本生成器唯一的线索汇总缓存。
    private static readonly List<ClueData> allClues = new List<ClueData>();
    private static readonly List<ClueData> trueEndingRequiredClues = new List<ClueData>();

    [MenuItem("Trace Me/生成全部内容资产")]
    // 仅能在编辑器菜单中运行；构建产物不会包含此脚本。
    public static void GenerateAll()
    {
        // 先确保父目录存在，避免 CreateAsset 因路径缺失失败。
        EnsureFolder(Root);
        EnsureFolder(ClueDir);
        EnsureFolder(DlgDir);
        // 清空的是内存列表，不会删除磁盘上的资产。
        allClues.Clear();
        trueEndingRequiredClues.Clear();

        // 按场景顺序生成，数据库也保留这一稳定顺序。
        GenerateHome();
        trueEndingRequiredClues.AddRange(allClues);
        GenerateSchool();

        int beforeStore = allClues.Count;
        GenerateStore();
        trueEndingRequiredClues.AddRange(allClues.GetRange(beforeStore, allClues.Count - beforeStore));

        int beforeAlley = allClues.Count;
        GenerateAlley();
        trueEndingRequiredClues.AddRange(allClues.GetRange(beforeAlley, allClues.Count - beforeAlley));

        int beforePlayground = allClues.Count;
        GeneratePlayground();
        trueEndingRequiredClues.AddRange(allClues.GetRange(beforePlayground, allClues.Count - beforePlayground));

        GenerateRooftop();
        // 开场与通关对白不属于单个可调查物品。
        GenerateSceneIntrosAndClears();
        // 阈值、封锁和门等运行时系统使用独立对白资产。
        GenerateSystemDialogues();
        // 事件表依赖前面已创建的对白资产。
        GenerateEventTable();
        // 数据库最后写入，确保其引用的是本轮生成的线索。
        GenerateClueDatabase();

        // SetDirty 仅标记变更；此处统一持久化到磁盘。
        AssetDatabase.SaveAssets();
        // 刷新后 Project 窗口和导入管线才能立即识别新资产。
        AssetDatabase.Refresh();
        Debug.Log($"[ContentGenerator] 完成：{allClues.Count} 条线索及全部对话/事件表已生成到 {Root}");
    }

    // ================== 场景内容 ==================

    // 家庭场景的四条可调查线索。
    private static void GenerateHome()
    {
        Clue("home_photo", "旧照片",
            "钢琴上倒扣着相框。两个人站在家门口，其中一个的脸被划掉了。",
            "和{sibling}在家门口的合影。{ta}的脸怎么都看不清。",
            "脸被划掉的那个人，就是你。这张照片里的“两个人”，从来都是同一个人。",
            Dlg("Dlg_home_photo", false, null,
                ("", "（钢琴上倒扣着一个相框。你把它翻过来——两个人站在家门口。）"),
                ("我", "这张照片……是在家门口拍的。我和{sibling}。但{ta}的脸……怎么都看不清？")));

        Clue("home_bowls", "两副碗筷",
            "餐桌左右两端各摆着一个杯子。一个干净，一个积满灰尘，杯子仍放在盘子上。",
            "{sibling}走之前用过的杯子，一直没有收。",
            "那副积灰的碗筷是你摆的。你一直在等一个不会回来的人——等的其实是过去的自己。",
            Dlg("Dlg_home_bowls", false, null,
                ("", "（餐桌两端各有一个杯子。一个干净，另一个积满了灰，杯子还放在盘子上。）"),
                ("我", "{sibling}走之前还在这里吃饭吗？怎么感觉像是放了很久很久……")));

        Clue("home_marks", "身高刻痕",
            "门框侧面从低到高的十几条刻痕，最高的那条标着“18岁”。",
            "{sibling}每年量身高留下的记号，停在18岁。",
            "刻痕只有一列。18岁那年，“{sibling}”消失了——那是你把自己封存起来的年纪。",
            Dlg("Dlg_home_marks", false, null,
                ("", "（门框侧面有十几条刻痕，从低到高。最高的一条旁边写着——18岁。）"),
                ("我", "这些刻痕……最高的那条标着18岁。{sibling}走的时候就是18岁。")));

        Clue("home_tinbox", "铁盒子",
            "玩偶背后藏着一个生锈的铁盒，里面是一条旧手环。",
            "{sibling}藏起来的手环，内侧刻着名字缩写。",
            "手环是你自己的。缩写相同，因为那本来就是同一个名字。",
            Dlg("Dlg_home_tinbox", false, null,
                ("", "（你搬开玩偶，后面藏着一个生了锈的铁盒。撬开——里面是一条旧手环。）"),
                ("我", "手环内侧有刻字……我的名字缩写，和{sibling}的名字缩写是一样的……不愧是{kin}。")));
    }

    // 学校场景的四条核心线索与补充调查线索。
    private static void GenerateSchool()
    {
        int schoolCoreStart = allClues.Count;
        Clue("school_desk", "课桌刻字",
            "靠窗第三排的课桌，桌面上用小刀刻着一个日期。",
            "{sibling}的课桌上刻着{ta}消失那天的日期。",
            "日期是你亲手刻下的。那一天，你决定忘记。",
            Dlg("Dlg_school_desk", false, null,
                ("", "（靠窗第三排的课桌。桌面上有用小刀刻出来的痕迹——是一个日期。）"),
                ("我", "下面刻着日期……是{sibling}消失的那天。")));

        Clue("school_report", "成绩单",
            "讲台抽屉里泛黄的成绩单，名字被墨水涂掉了。",
            "应该是{sibling}的成绩单，科目成绩都很高。",
            "名字是你涂掉的。那是你的成绩单。",
            Dlg("Dlg_school_report", false, null,
                ("", "（讲台的抽屉没有锁。里面有一张泛黄的纸——成绩单。名字的位置被墨水涂掉了。）"),
                ("我", "名字被涂掉了……但科目成绩都很高。是{sibling}的吧？")));

        Clue("school_locker", "储物柜",
            "教室边上的铁皮柜。锁是坏的。里面有一幅画：两个人站在操场边，其中一个被撕掉了。",
            "{sibling}的储物柜里留着一幅两个人的画，被撕掉了一半。",
            "画里被撕掉的那个人从来不存在。完整的那个人，就是你。",
            Dlg("Dlg_school_locker", false, null,
                ("", "（教室边上的铁皮柜。锁已经坏了，轻轻一拉就开。里面放着一幅画。）"),
                ("我", "画的是两个人站在操场边上……但其中一个被撕掉了。只剩下右边那个。"),
                ("", "（右边那个人的脸是完整的——和你一模一样。）")));

        Clue("school_board", "黑板上的字",
            "黑板右下角有粉笔写的一行不完整的字。",
            "笔迹很眼熟，像是自己的。什么时候写的？",
            "确实是你写的。你比自己以为的记得更多。",
            Dlg("Dlg_school_board", false, null,
                ("", "（黑板右下角，有一行没写完的粉笔字。日光灯在头顶闪了一下。）"),
                ("我", "这粉笔字……是我写的。我记得这笔迹。但我什么时候写的？")));

        trueEndingRequiredClues.AddRange(allClues.GetRange(schoolCoreStart, allClues.Count - schoolCoreStart));

        Clue("school_water_dispenser", "饮水机",
            "一台放在墙角的饮水机，水桶早就干了。",
            "一个饮水机，竟然暗藏玄机？",
            "你把它移开后，才看见后面藏着的房间。",
            Dlg("Dlg_school_water_dispenser", false, null,
                ("", "（一台放在墙角的饮水机 水桶早就干了）"),
                ("我", "饮水机不太稳……好像可以移动？")));

        Dlg("Dlg_school_water_dispenser_reveal", false, null,
            ("我", "后面好像有个房间……"));
        Dlg("Dlg_school_water_dispenser_repeat", false, null,
            ("我", "一个饮水机，竟然暗藏玄机？"));

        Clue("school_paper_rank", "成绩排名纸",
            "饮水机不远处的地上掉了一张纸。",
            "是某次考试的成绩排名，虽然看不太清上面的具体字迹了，不过我们家姓氏很少见，当然也很明显，哥哥（姐姐）的名字在第一。",
            "那张成绩排名记录的，是你曾经取得的成绩。",
            Dlg("Dlg_school_paper_rank", false, null,
                ("", "（饮水机不远处的地上掉了一张纸）"),
                ("我", "是某次考试的成绩排名，虽然看不太清上面的具体字迹了，不过我们家姓氏很少见，当然也很明显，哥哥（姐姐）的名字在第一。")));

        Clue("school_paper_counseling", "破碎的纸张",
            "公告栏前面掉了一张破碎的纸张。",
            "勉强看得清上面残留的部分字迹，是学校的心理咨询活动吗？",
            "被撕碎的记录，仍然留下了你不愿面对的求助痕迹。",
            Dlg("Dlg_school_paper_counseling", false, null,
                ("", "（公告栏前面掉了一张破碎的纸张）"),
                ("我", "勉强看得清上面残留的部分字迹，是学校的心理咨询活动吗？")));

        Clue("school_hidden_room_files", "墙上的纸张",
            "地上墙上充满了凌乱的纸张，无一例外的都是关于哥哥（姐姐）的信息。",
            "家庭住址、盗摄的照片、每次考试的成绩、作业的分数……还有病历？",
            "这里记录的不是另一个人，而是你被切割出来的记忆。",
            Dlg("Dlg_school_hidden_room_files", false, null,
                ("我", "这…这里是怎么回事……"),
                ("", "（地上墙上充满了凌乱的纸张，无一例外的都是关于哥哥（姐姐）的信息）"),
                ("我", "家庭住址、盗摄的照片、每次考试的成绩、作业的分数……还有病历？"),
                ("我", "哥哥（姐姐）是生病了吗……"),
                ("我", "不对，这里到底是怎么回事，为什么哥哥（姐姐）会被人盯上。"),
                ("我", "但是，为什么只写了姓氏？")));

        Clue("school_notice_board", "公告栏",
            "上面贴了学校的各种活动，以及通知，包括国家规定的节假日，以及寒暑假的调休。",
            "哥哥（姐姐）真的好辛苦，要参加的东西好多。",
            "你把自己的日程和身份，写成了另一个人的生活。",
            Dlg("Dlg_school_notice_board", false, null,
                ("", "（上面贴了学校的各种活动，以及通知，包括国家规定的节假日，以及寒暑假的调休）"),
                ("我", "哥哥（姐姐）真的好辛苦，要参加的东西好多。")));

        Clue("school_cleaning_tools", "清洁工具堆",
            "扫帚和拖把之类的东西被一股脑地堆在这个角落中。",
            "好乱。",
            "你一直把不愿整理的东西堆在角落里。",
            Dlg("Dlg_school_cleaning_tools", false, null,
                ("", "（扫帚和拖把之类的东西被一股脑地堆在这个角落中）"),
                ("我", "好乱。")));
    }

    // 便利店场景的三条可调查线索。
    private static void GenerateStore()
    {
        Clue("store_note", "收银台便条",
            "掉在地上的手写购物清单。最后一行写着“别忘了买糖”。",
            "{sibling}留下的购物清单。",
            "购物清单是你写的。“别忘了买糖”——是你留给自己的话。",
            Dlg("Dlg_store_note", false, null,
                ("", "（收银机旁边，计算器还亮着：0.00。地上掉着一张撕下来的纸。）"),
                ("我", "一张购物清单……牛奶、面包。最后一行写着“别忘了买糖”。")));

        Clue("store_toy", "货架上的玩具",
            "箱子堆上一个落满灰的塑料玩具。",
            "小时候好像也有一个一模一样的。",
            "不是“好像也有一个”。就是这一个。",
            Dlg("Dlg_store_toy", false, null,
                ("", "（一堆纸箱上，孤零零摆着一个塑料玩具，落满了灰。）"),
                ("我", "这个玩具……小时候我好像也有一个。")));

        Clue("store_handprint", "门上的手印",
            "便利店后门把手附近一个清晰的手印，指纹方向是往外推门的。",
            "有人从这里推门出去过。手印不太大。",
            "手印当然是你的大小。是你自己推开了这扇门。",
            Dlg("Dlg_store_handprint", false, null,
                ("", "（便利店后门。接近把手的位置有一个清晰的手印。你下意识把手覆了上去。）"),
                ("我", "……刚好是我的手掌大小。指纹方向……是往外推门的。")));
    }

    // 小巷场景的三条可调查线索。
    private static void GenerateAlley()
    {
        Clue("alley_graffiti", "墙上的涂鸦",
            "墙面一米五高的位置，黑色马克笔写着：“x…对不起”，前面的字被刮掉了。",
            "有人写下的道歉，对象不明。",
            "被刮掉的是你的名字。对不起——是你想对自己说的话。",
            Dlg("Dlg_alley_graffiti", false, null,
                ("", "（墙面上有一行黑色马克笔字：“x…对不起”。前面的字母被人用力刮掉了。）"),
                ("我", "什么……对不起？前面被刮掉了……")));

        Clue("alley_cigs", "地上的烟头",
            "杂草周围散落着五六根发黄的烟头。",
            "有人在这里待了很久。是在等谁吗？",
            "在这里等待的人是你。你一直在等自己回来。",
            Dlg("Dlg_alley_cigs", false, null,
                ("", "（杂草周围散落着五六根烟头，已经发黄了。有人曾在这里站了很久很久。）"),
                ("我", "感觉有人在这里待了很久。“{sibling}”……你是在等我吗？")));

        Clue("alley_poster", "旧海报",
            "只留下了残骸的旧寻人启事，日期被撕掉，照片不见了。",
            "一张残缺的寻人启事：“xx，18岁，于……”。是谁贴的？",
            "寻人启事是你贴的。你在寻找的，从一开始就是你自己。",
            Dlg("Dlg_alley_poster", false, null,
                ("", "（墙上贴过的海报早已剥落，只剩残骸。你拨开一角——底下露出一张旧寻人启事。）"),
                ("我", "寻人启事。“xx，18岁，于……”后面的日期被撕掉了……照片也不见了。"),
                ("我", "这张寻人启事……是谁贴的？")));
    }

    // 游乐场场景的四条可调查线索。
    private static void GeneratePlayground()
    {
        Clue("pg_carousel", "旋转木马",
            "游乐场中央的旋转木马，其中一匹的背上刻着“许愿：永远在一起”。",
            "谁刻下的愿望：永远在一起。",
            "“永远在一起”——一个人对自己许下的愿望。",
            Dlg("Dlg_pg_carousel", false, null,
                ("", "（旋转木马还在原地，马身上的漆斑驳脱落。其中一匹的背上刻着一行小字。）"),
                ("我", "这匹木马背上刻着……“许愿：永远在一起”。我也希望和{sibling}永远在一起。")));

        Clue("pg_ferris", "摩天轮",
            "游乐场边缘停着的摩天轮，5号座舱的门开着。",
            "很熟悉的座舱。在这里坐过很多次。",
            "你确实一个人坐过很多次。从来都是一个人。",
            Dlg("Dlg_pg_ferris", false, null,
                ("", "（摩天轮静静停着，5号座舱的门开着，在风里微微晃动。）"),
                ("我", "我好像在这里坐过很多次。……一个人。")));

        Clue("pg_bench", "长椅",
            "旋转木马旁的柱子残骸，残骸上刻着两个名字，其中一个被划掉了。",
            "两个名字，一个被划掉了。划痕很旧。",
            "两个名字都是你刻的。划掉其中一个的，也是你。",
            Dlg("Dlg_pg_bench", false, null,
                ("", "（旋转木马旁边立着一截柱子残骸。上面刻着两个名字——其中一个被反复划掉了。）"),
                ("我", "划痕很旧……和之前墙上的刮痕一样。")));

        Clue("pg_booth", "售票亭",
            "入口不远处的神秘废墟，碎了一半，里面有一卷旧票根。",
            "一卷同一场次的旧票根。全是单人票。",
            "每一张都是单人票。回忆里的“两个人”，从来只有一个人。",
            Dlg("Dlg_pg_booth", false, null,
                ("", "（入口不远处的一片废墟，塌了一半。你从瓦砾里摸出一卷旧票根。）"),
                ("我", "这些票根……都是同一场次的。……单人票。每一次都是单人票。")));
    }

    // 天台场景承载最终线索与结局前置条件。
    private static void GenerateRooftop()
    {
        Clue("roof_chair", "椅子",
            "天台正中央的一把旧木椅，椅面磨损严重。",
            "像是有人常年坐在这里。",
            "你曾经常年坐在这里。18岁那年，也是坐在这里做出了那个决定。",
            Dlg("Dlg_roof_chair", false, null,
                ("", "（天台正中央放着一把旧木椅，椅面磨得发亮。风很大。）"),
                ("我", "这把椅子放在这里多久了？……像是有人常年坐在这里。")));

        Clue("roof_diary", "日记",
            "椅子上翻开的皮质日记，纸张泛黄。笔迹和自己的一模一样。",
            "翻开在最后一页的日记。这笔迹……",
            "日记是你写的。从头到尾都只有一个人。",
            Dlg("Dlg_roof_diary", false, null,
                ("", "（椅子上放着一本翻开的日记。皮质封面，纸张泛黄。笔迹和你的一模一样。）"),
                ("", "“如果你在读这行字，说明你已经走过了很远的路……从头到尾都只有一个人……”")));

        Clue("roof_diary_page", "日记里的自画像",
            "日记里夹着一张泛黄的自画像：一个人坐在天台的椅子上。",
            "右下角写着：“画于18岁生日前一天。”",
            "自画像画的是你。18岁生日前一天——“{sibling}”消失的前一天。",
            Dlg("Dlg_roof_diary_page", false, null,
                ("", "（你翻过一页。里面夹着一张泛黄的画——一个人坐在天台的椅子上。）"),
                ("我", "右下角写着一行小字：“画于18岁生日前一天。”")));

        Clue("roof_footprints", "地上的脚印",
            "灰尘上从楼梯口到椅子、从椅子到围栏来回多次的脚印。",
            "来回走了很多次的脚印。",
            "脚印全是你的。来回多次——你比记忆中更常回到这里。",
            Dlg("Dlg_roof_footprints", false, null,
                ("", "（地面的薄灰上印着杂乱的脚印。从楼梯口到椅子，从椅子到围栏……来回，来回。）"),
                ("我", "全是同一个人的脚印。")));

        Clue("roof_reflection", "玻璃门上的倒影",
            "夕阳照在脏玻璃门上，变成一面镜子。",
            "倒影里的自己……在微笑。",
            "倒影里微笑的人，就是你要找的人。",
            Dlg("Dlg_roof_reflection", false, null,
                ("", "（夕阳斜照在脏玻璃门上。玻璃变成了一面昏黄的镜子。）"),
                ("", "（倒影里的人——长着“{sibling}”的脸。那张脸，在微笑。）"),
                ("", "（那是你的脸。）"),
                ("我", "…………")));

        // 最后一条线索：真相揭示交由天台的 EndingGate 判定后处理，此对话不直接设 flag。
        // 全查过 → EndingGate 设 truth_revealed（真结局）；有遗漏 → 播 Dlg_bad_ending 回主菜单。
        Clue("roof_diary_final", "日记的最后一页",
            "再次翻开日记，最后一页的文字变了。",
            "“你终于认出我了。”",
            "你终于认出我了。",
            Dlg("Dlg_roof_diary_final", false, null,
                ("", "（你回到椅子边，再次翻开日记。最后一页的文字——变了。）"),
                ("", "“你终于认出我了。”")));
    }

    // ================== 开场白 & 通关独白 ==================

    // 生成场景进入和清空后的非物品对白。
    private static void GenerateSceneIntrosAndClears()
    {
        // 开场白：勾选计数（6 个开场 + 24 个物品 = 恰好 30 次，与阈值表对齐）
        Dlg("Dlg_intro_home", true, null,
            ("", "（黄昏的光从窗帘缝里挤进来，灰尘在光柱里漂浮。你从沙发上醒来。）"),
            ("我", "……我又睡着了吗。"),
            ("我", "{sibling}失踪已经很久了。所有人都说没有这个人……但我记得。我一定要找到{ta}。"),
            ("", "（餐桌上，好像放着什么东西。）"));

        Dlg("Dlg_intro_school", true, null,
            ("", "（走廊的日光灯管在闪。墙上的钟停在12:30。课桌椅东倒西歪。）"),
            ("我", "沿着记忆里的路走，就到了学校。{sibling}在这里上过学……我记得的。"));

        Dlg("Dlg_intro_store", true, null,
            ("", "（整条街只有这家便利店还亮着半块灯牌。收银台上的计算器亮着：0.00。）"),
            ("我", "{sibling}以前放学后总来这里买东西。……我怎么会记得这么清楚？"));

        Dlg("Dlg_intro_alley", true, null,
            ("", "（小巷很窄，只够两个人并排走。墙上的海报被雨水泡烂了，露出下面一层旧广告。）"),
            ("我", "只够两个人并排走的巷子。……或者，一个人。"));

        Dlg("Dlg_intro_playground", true, null,
            ("", "（穿过小巷，视野忽然开阔。旋转木马斑驳掉漆，摩天轮的座舱在半空中微微晃动。）"),
            ("我", "这个游乐场……小时候我经常来。和{sibling}一起。……和{sibling}一起？"));

        Dlg("Dlg_intro_rooftop", true, null,
            ("", "（风越来越大。推开锈住的铁门——天台。深蓝色的天空，介于黄昏和夜晚之间。）"),
            ("我", "正中央……放着一把椅子。椅子上有一本翻开的日记。"));

        // 通关独白：不计数
        Dlg("Dlg_clear_home", false, null,
            ("", "（门打开了。客厅的窗帘被风吹起——门口，一个模糊的影子站了一秒，然后消失了。）"),
            ("我", "……你是在给我带路吗？"));

        Dlg("Dlg_clear_school", false, null,
            ("", "（讲台旁边出现一个模糊的侧影，坐在课桌前写字。写了几笔，就消失了。）"),
            ("", "黑板上多了一行字：“往前走，别回头。”"));

        Dlg("Dlg_clear_store", false, null,
            ("", "（推开后门的瞬间，便利店的灯全部暗了。）"),
            ("", "（门口站着一个模糊的影子。影子抬起手，指了指前方的路。然后消失了。）"));

        Dlg("Dlg_clear_alley", false, null,
            ("", "（巷子两侧墙上的涂鸦突然全部亮了起来——一瞬间，全是同一句话。）"),
            ("", "“往前走。”"),
            ("", "“别停。”"),
            ("", "“我在前面等你。”"));

        Dlg("Dlg_clear_playground", false, null,
            ("", "（你朝出口走去。身后，旋转木马上出现了一个坐着的影子。）"),
            ("", "（影子朝你挥了挥手。）"),
            ("", "（但你没有回头。）"));

        // 天台结局（黑幕后播放）
        Dlg("Dlg_clear_rooftop", false, null,
            ("", "（你合上日记，走到天台楼梯口，回头看了一眼那把椅子。）"),
            ("", "（然后推开门，走了回去。）"),
            ("", "……"),
            ("我", "后来我再也没有上过这个天台。"),
            ("我", "因为已经不需要了。"));
    }

    // ================== 系统对话（阈值/封锁/门） ==================

    // 生成由调查进度或关卡规则触发的对白。
    private static void GenerateSystemDialogues()
    {
        // 回溯闪回（暂以文字演出，美术闪回图就绪后填入事件表的 images 即可叠加）
        Dlg("Dlg_inv_5", false, null,
            ("", "（视野突然闪烁——一瞬间，你看到一个背影站在路的尽头。还没看清，画面就恢复了。）"),
            ("我", "刚才那是……{sibling}？"));

        Dlg("Dlg_inv_10", false, null,
            ("我", "为什么这些线索……都像是我自己留下的？"),
            ("我", "……不。不会的。我只是太累了。"));

        Dlg("Dlg_inv_15", false, null,
            ("", "（又是那种感觉。你看到“{sibling}”俯身在写一张便条——上面的字，和你刚才捡到的那张，一模一样。）"),
            ("我", "……为什么？"));

        Dlg("Dlg_inv_25", false, null,
            ("", "（这次更清晰了。“{sibling}”站在一面镜子前。）"),
            ("", "（镜子里反射出来的脸——是你自己的脸。）"),
            ("我", "…………不对。不对不对不对。"));

        Dlg("Dlg_inv_28", false, null,
            ("", "（身后传来什么东西合上的声音。回去的路，好像已经不在了。）"),
            ("我", "……只能往前走了。"));

        // 封锁台词
        Dlg("Dlg_locked_home", false, null,
            ("我", "这地方我翻遍了……没有更多线索了。"));

        // 门的通用台词
        Dlg("Dlg_door_locked", false, null,
            ("我", "还不能走。……总觉得这里还有没找到的东西。"));

        Dlg("Dlg_door_noreturn", false, null,
            ("", "（门推不开。像是有什么在告诉你——过去回不去了。）"));

        // 坏结局：前五关未全部查过时，翻开日记最后一页触发（EndingGate 播完回主菜单）
        Dlg("Dlg_bad_ending", false, null,
            ("", "（你翻开最后一页。上面……什么都没有。）"),
            ("我", "……"),
            ("我", "这一切都是假的吧。{sibling}的下落，根本就查不出来。"),
            ("我", "我被那封信骗了……"));
    }

    // ================== 事件表 & 数据库 ==================

    // 事件表中的对白名称必须与前面生成的资产名一致。
    private static void GenerateEventTable()
    {
        var table = GetOrCreate<InvestigationEventTable>(Root + "/InvestigationEventTable.asset");

        table.events = new[]
        {
            Evt(5,  "回溯1·背影",   null,                          "Dlg_inv_5",  "……{sibling}？"),
            Evt(8,  "封锁·家",     new[] { "lock_home_items" },   null,         null),
            Evt(10, "动摇独白",     null,                          "Dlg_inv_10", null),
            Evt(15, "回溯2·便条",   null,                          "Dlg_inv_15", null),
            Evt(18, "封锁·路人",   new[] { "lock_npc_talk" },     null,         null),
            Evt(25, "回溯3·镜子",   null,                          "Dlg_inv_25", null),
            Evt(28, "封锁·回头路", new[] { "lock_early_scenes" }, "Dlg_inv_28", null),
        };

        EditorUtility.SetDirty(table);
    }

    private static InvestigationEventTable.ThresholdEvent Evt(
        int threshold, string label, string[] flags, string dialogueName, string caption)
    {
        return new InvestigationEventTable.ThresholdEvent
        {
            threshold = threshold,
            editorLabel = label,
            setFlags = flags ?? new string[0],
            monologue = dialogueName != null
                ? AssetDatabase.LoadAssetAtPath<DialogueData>($"{DlgDir}/{dialogueName}.asset")
                : null,
            flashback = new FlashbackSequence { caption = caption ?? string.Empty, secondsPerImage = 1.2f },
        };
    }

    // 通过 SerializedObject 写入私有序列化字段，避免暴露运行时接口。
    private static void GenerateClueDatabase()
    {
        var db = GetOrCreate<ClueDatabase>(Root + "/ClueDatabase.asset");

        var so = new SerializedObject(db);
        var list = so.FindProperty("allClues");
        list.arraySize = allClues.Count;
        for (int i = 0; i < allClues.Count; i++)
        {
            list.GetArrayElementAtIndex(i).objectReferenceValue = allClues[i];
        }

        var requiredList = so.FindProperty("trueEndingRequiredClues");
        requiredList.arraySize = trueEndingRequiredClues.Count;
        for (int i = 0; i < trueEndingRequiredClues.Count; i++)
        {
            requiredList.GetArrayElementAtIndex(i).objectReferenceValue = trueEndingRequiredClues[i];
        }

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ================== 工具方法 ==================

    // 每条线索与其调查对白建立双向生成顺序中的引用。
    private static ClueData Clue(string id, string title, string description,
        string surface, string truth, DialogueData inspectDialogue)
    {
        var clue = GetOrCreate<ClueData>($"{ClueDir}/Clue_{id}.asset");

        var so = new SerializedObject(clue);
        so.FindProperty("clueId").stringValue = id;
        so.FindProperty("title").stringValue = title;
        so.FindProperty("description").stringValue = description;
        so.FindProperty("surfaceMeaning").stringValue = surface;
        so.FindProperty("trueMeaning").stringValue = truth;
        so.ApplyModifiedPropertiesWithoutUndo();

        // 调查对话结束时发放本线索
        if (inspectDialogue != null)
        {
            inspectDialogue.grantCluesOnComplete = new[] { clue };
            EditorUtility.SetDirty(inspectDialogue);
        }

        allClues.Add(clue);
        return clue;
    }

    // 每次执行都会覆盖此对白的可生成字段，手动修改应放在生成源中。
    private static DialogueData Dlg(string assetName, bool countsAsInvestigation,
        string[] setFlags, params (string speaker, string text)[] lines)
    {
        var dlg = GetOrCreate<DialogueData>($"{DlgDir}/{assetName}.asset");

        dlg.lines = new DialogueData.Line[lines.Length];
        for (int i = 0; i < lines.Length; i++)
        {
            dlg.lines[i] = new DialogueData.Line
            {
                speakerName = lines[i].speaker,
                text = lines[i].text,
            };
        }

        dlg.countsAsInvestigation = countsAsInvestigation;
        dlg.setFlagsOnComplete = setFlags ?? new string[0];
        dlg.grantCluesOnComplete = new ClueData[0];
        dlg.advanceTimeOnComplete = 0;

        EditorUtility.SetDirty(dlg);
        return dlg;
    }

    // 先按路径加载可保留既有 .meta 的 GUID；场景和其他资产引用不会因重复生成失效。
    // 仅路径不存在时创建新资产；删除 .asset 或 .meta 后才会产生新的 GUID。
    private static T GetOrCreate<T>(string path) where T : ScriptableObject
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        // CreateAsset 只能用于尚未存在的目标路径。
        if (asset == null)
        {
            // 新建资产会由 Unity 同时创建对应的 .meta 文件。
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
        }

        return asset;
    }

    // AssetDatabase 只接受项目内 Assets 下的规范路径。
    private static void EnsureFolder(string path)
    {
        // 已存在时不操作，保证可重复执行。
        if (!AssetDatabase.IsValidFolder(path))
        {
            string parent = path.Substring(0, path.LastIndexOf('/'));
            string leaf = path.Substring(path.LastIndexOf('/') + 1);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
#endif
