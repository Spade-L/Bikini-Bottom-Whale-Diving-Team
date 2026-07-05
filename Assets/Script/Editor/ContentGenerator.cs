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
    private const string Root = "Assets/GameData";
    private const string ClueDir = Root + "/Clues";
    private const string DlgDir = Root + "/Dialogues";

    private static readonly List<ClueData> allClues = new List<ClueData>();

    [MenuItem("Trace Me/生成全部内容资产")]
    public static void GenerateAll()
    {
        EnsureFolder(Root);
        EnsureFolder(ClueDir);
        EnsureFolder(DlgDir);
        allClues.Clear();

        GenerateHome();
        GenerateSchool();
        GenerateStore();
        GenerateAlley();
        GeneratePlayground();
        GenerateRooftop();
        GenerateSceneIntrosAndClears();
        GenerateSystemDialogues();
        GenerateEventTable();
        GenerateClueDatabase();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[ContentGenerator] 完成：{allClues.Count} 条线索及全部对话/事件表已生成到 {Root}");
    }

    // ================== 场景内容 ==================

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

    private static void GenerateSchool()
    {
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
            "走廊尽头的铁皮柜。锁是坏的。里面有一幅画：两个人站在操场边，其中一个被撕掉了。",
            "{sibling}的储物柜里留着一幅两个人的画，被撕掉了一半。",
            "画里被撕掉的那个人从来不存在。完整的那个人，就是你。",
            Dlg("Dlg_school_locker", false, null,
                ("", "（走廊尽头的铁皮柜。锁已经坏了，轻轻一拉就开。里面放着一幅画。）"),
                ("我", "画的是两个人站在操场边上……但其中一个被撕掉了。只剩下右边那个。"),
                ("", "（右边那个人的脸是完整的——和你一模一样。）")));

        Clue("school_board", "黑板上的字",
            "黑板右下角有粉笔写的一行不完整的字。",
            "笔迹很眼熟，像是自己的。什么时候写的？",
            "确实是你写的。你比自己以为的记得更多。",
            Dlg("Dlg_school_board", false, null,
                ("", "（黑板右下角，有一行没写完的粉笔字。日光灯在头顶闪了一下。）"),
                ("我", "这粉笔字……是我写的。我记得这笔迹。但我什么时候写的？")));
    }

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
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ================== 工具方法 ==================

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

    private static T GetOrCreate<T>(string path) where T : ScriptableObject
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
        }

        return asset;
    }

    private static void EnsureFolder(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            string parent = path.Substring(0, path.LastIndexOf('/'));
            string leaf = path.Substring(path.LastIndexOf('/') + 1);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
#endif
