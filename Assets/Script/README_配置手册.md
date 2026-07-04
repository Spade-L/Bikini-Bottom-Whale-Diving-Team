# Trace Me（寻己）— 全脚本配置手册

> 本手册覆盖 `Assets/Script/` 下全部脚本。
> 分四类阅读：**全局唯一**（每个场景放一个/常驻）、**数据资产**（Project 窗口创建，不挂物体）、
> **场景物件**（挂在场景中的交互对象上）、**辅助/旧脚本**。
> **第七节起是本作全部剧情内容的配置对照表（按策划案生成）。**
>
> 通用约定：
> - 所有交互都是「玩家走近 → 显示提示 UI → 按 E」。玩家物体的 Tag 必须是 `Player`。
> - 「Flag」是一个字符串开关，由 GameManager 记录，存档保存。谁都能设置、谁都能引用。
> - 「StoryCondition（剧情条件）」是所有脚本共用的条件配置块，见第 2 节，先看懂它。
> - 文本占位符：`{sibling}`=哥哥/姐姐、`{ta}`=他/她、`{kin}`=好兄弟/好姐妹。
>   开局选女性时设置 flag `gender_female`，全部文本自动切换（TextTokens.cs 实现）。

---

## 一、全局唯一脚本

### 1. GameManager.cs（Core/）— 游戏状态中枢

**作用**：记录四类核心数据——剧情 Flag、时间段、已收集线索、**调查次数**。
所有其他系统都从它读状态、向它写状态。跨场景不销毁（DontDestroyOnLoad）。

**配置**：

1. 在**第一个游戏场景**建空物体 `GameManager`，挂上脚本
2. `Clue Database`：拖入线索数据库资产（见下文 ClueDatabase）
3. `Log State Changes`：勾选后每次 Flag/计数变化都打 Console 日志，调试期建议开着

**注意**：它跨场景存活，不要在每个场景都放一个（第二个会自毁，但没必要）。

---

### 2. InvestigationDirector.cs（Investigation/）— 调查事件导演
**作用**：监听调查次数，达到阈值时演出「回溯闪回」（全屏图片淡入淡出）、
播放独白、设置封锁 Flag。阈值内容全部来自事件表资产。

**配置**（每个游戏场景的 Canvas 上放一个）：
1. Canvas 下建全屏覆盖层：`FlashbackOverlay`（加 **CanvasGroup**，全屏拉伸，
   下挂一个全屏 **Image**（黑底）和一个 **TMP_Text**（居中说明文字））
2. 空物体或 Canvas 上挂 InvestigationDirector：
   - `Event Table`：拖入调查事件表资产
   - `Flashback Overlay` / `Flashback Image` / `Flashback Caption`：拖入上面三个
   - `Fade Duration`：闪回淡入淡出秒数（默认 0.35）

**机制细节**：
- 每个阈值只触发一次，触发时自动设置 `inv_reached_<次数>` flag
- 若正在对话，会等对话关闭后再演出，不会叠 UI
- 一次跳过多个阈值（理论上）会按顺序排队演出

---

### 3. DialogueUIManager.cs（根目录）— 对话框
**作用**：播放 DialogueData 对话——多行推进、打字机效果、说话人名字、
**左侧立绘 + 表情差分**。按 E：打字中→立刻显示全文；显示完→下一行；最后一行→关闭并结算对话效果。

**配置**（每个游戏场景的 Canvas 上）：
1. Canvas 下建 `DialoguePanel`（对话框背景 Image，锚定屏幕下方）
2. 面板内建：
   - `DialogueText`（TMP_Text，正文）
   - `SpeakerName`（TMP_Text，名字栏，通常在框左上）
   - `Portrait`（Image，**锚定面板左侧**，立绘用；建议 Preserve Aspect 勾上）
   - `ContinueIndicator`（小箭头图标，一行显示完才出现）
3. 挂 DialogueUIManager 到 Canvas（或面板父物体），依次拖入上面引用
4. `Chars Per Second`：打字速度（默认 30）；`Advance Key`：推进键（默认 E）

**注意**：面板初始激活与否无所谓，Awake 会强制隐藏。名字/立绘/箭头引用都可留空（对应功能跳过）。

---

### 4. ClueJournalUI.cs（Clue/）— 线索日志
**作用**：按 Tab 打开的线索一览界面。左列表右详情。
结局设置 `truth_revealed` flag 后，所有线索详情自动替换为「真相」文本。

**配置**：
1. Canvas 下建 `JournalPanel`：
   - 左侧：**Scroll View**，其 Content 作为列表容器
   - 右侧：`Title`（TMP_Text）、`Description`（TMP_Text）、`Meaning`（TMP_Text）、`Icon`（Image）
2. 做一个列表项预制体：**Button** + 子物体 TMP_Text，存为 Prefab
3. 挂 ClueJournalUI，拖入：`Journal Panel`、`List Content`（ScrollView 的 Content）、
   `List Item Prefab`（上面的预制体）、四个详情引用
4. `Toggle Key`：开关键（默认 Tab）

**机制**：对话进行中按 Tab 无效（防止 UI 冲突）。

---

### 5. SceneClueTracker.cs（Scene/）— 场景寻踪进度 & 通关演出
**作用**：登记本场景的 3-4 个关键线索；角落 UI 显示「寻踪进度：2/4」；
集齐后自动演出：**影子出现 → 黑幕 → （黑幕中设置通关 Flag）→ 淡出 → 可选独白**。

**配置**（每个游戏场景一个）：
1. `Scene Id`：场景标识（`home` / `school` / `store` / `alley` / `playground` / `rooftop`），
   通关后自动设置 flag `scene_cleared_<sceneId>`
2. `Key Clues`：拖入本场景的 3-4 个 ClueData 资产
3. `Progress Text`：Canvas 角落的 TMP_Text；`Progress Format` 默认「寻踪进度：{0}/{1}」
4. `Brother Shadow`：场景里预先摆好的影子物体（SpriteRenderer，摆在显眼位置），**默认设为隐藏**——脚本会在演出时激活
5. `Blackout`：Canvas 下的全屏黑 Image + **CanvasGroup**（和闪回覆盖层分开建，避免冲突）
6. `Shadow Duration` / `Blackout Fade Duration` / `Blackout Hold Duration`：演出节奏
7. `Clear Monologue`：黑幕结束后的独白 DialogueData（可空；**不要勾**计数）

**机制**：演出会等对话和闪回都结束才开始；场景变化（门开、物件改变）靠通关 Flag 驱动，在黑幕遮挡期间完成切换。

---

### 6. SaveSystem.cs（Core/）— 存档（静态类，不挂物体）
**作用**：把 SaveData 序列化为 JSON 写到 `persistentDataPath/save_槽位.json`。

**无需配置**。代码调用方式：
```csharp
// 存档
SaveData data = GameManager.Instance.CaptureSaveData();
data.sceneName = SceneManager.GetActiveScene().name;
SaveSystem.Save(data);

// 读档
SaveData data = SaveSystem.Load();
if (data != null) GameManager.Instance.RestoreSaveData(data);

// 判断有无存档（主菜单"继续"按钮用）
SaveSystem.HasSave();
```
目前还没有接「存档点/自动存档」的触发器——需要时告诉我在哪存（过门时？手动菜单？）。

---

## 二、数据资产（Project 窗口右键 Create > 游戏数据 > …）

### 7. StoryCondition.cs（Core/）— 剧情条件（共用配置块，非资产）
**作用**：不是独立资产，而是嵌在其他配置里的「条件」区块。哪里有它，哪里就能配「什么时候出现/可用」。

**字段**：
| 字段 | 含义 | 留默认的效果 |
|---|---|---|
| `Min/Max Time Period` | 时间段范围（-1 = 不限） | 不限时间 |
| `Required Flags` | 这些 Flag **全部**已设置才满足 | 无要求 |
| `Forbidden Flags` | 这些 Flag **任一**已设置就不满足 | 无排除 |
| `Required Clues` | 这些线索（填 ClueId）全部已收集才满足 | 无要求 |

**全部留默认 = 永远满足（一直出现）。**

### 8. DialogueData.cs（Dialogue/）— 一段对话
**作用**：一段完整对话的内容和后果。游戏里所有文字（NPC 对话、调查描述、独白、门的提示语）都是它。

**创建**：Create > 游戏数据 > 对话，命名建议 `Dlg_场景_内容`（如 `Dlg_home_photo`）

**Lines（每行）**：
- `Character`：说话人立绘资产（**留空 = 旁白**，不显示立绘）
- `Expression`：表情差分名（留空 = 默认立绘）
- `Speaker Name`：覆盖显示名（留空则用立绘资产的 displayName）
- `Text`：正文

**播放完毕后的效果**：
- `Counts As Investigation`：**勾选 = 独白计数 +1**。只给「主角主动的思考独白」勾；
  阈值事件触发的独白、封锁台词、门的提示语**都不要勾**（会连锁/刷计数）
- `Set Flags On Complete`：结束时设置的 Flag（干涉 NPC 成功、结局 `truth_revealed` 都在这配）
- `Advance Time On Complete`：推进时间段数（推动时限 NPC 变化用）
- `Grant Clues On Complete`：结束时给的线索

### 9. ClueData.cs（Clue/）— 一条线索
**创建**：Create > 游戏数据 > 线索

- `Clue Id`：**全局唯一**，存档和条件判断都用它。建议 `场景_物品`（如 `home_photo`）。**上线后不要改**
- `Title` / `Description` / `Icon`：线索日志里的展示
- `Surface Meaning`：表层解读（"哥哥留下的日记"）——玩家全程看到的
- `True Meaning`：真相（"这是你自己的笔迹"）——`truth_revealed` 后日志自动替换

### 10. ClueDatabase.cs（Clue/）— 线索登记表
**创建**：Create > 游戏数据 > 线索数据库，**全项目只要一个**。
把**所有** ClueData 拖进 `All Clues` 列表（漏登记的线索读档后会丢失显示），然后把资产拖给 GameManager。

### 11. CharacterData.cs（Dialogue/）— 角色立绘
**创建**：Create > 游戏数据 > 角色立绘

- `Display Name`：名字栏显示（主角可以叫"我"）
- `Default Portrait`：默认立绘
- `Expressions`：表情差分表，每项 = 表情名 + Sprite。
  **全项目统一命名**：`normal / worried / shocked / sad / doubt / smile`

**需要的资产**：主角（男/女各一个，性别自选功能待接）+ 会说话的 NPC 各一个。
找不到差分名会用默认立绘并打警告，不会报错。

### 12. InvestigationEventTable.cs（Investigation/）— 调查阈值事件表

**创建**：Create > 游戏数据 > 调查事件表，**全项目一个**，拖给每个场景的 InvestigationDirector。

每个事件：`Threshold`（次数）、`Set Flags`（附带设置的 Flag）、
`Flashback`（闪回图组 + 每张秒数 + 说明文字）、`Monologue`（独白对话，不勾计数）。

**按策划案填 8 行**：
| 阈值 | 内容 | Set Flags |
|---|---|---|
| 5 | 闪回·哥哥背影 | — |
| 8 | 封锁家中物品 | `lock_home_items` |
| 10 | 动摇独白 | — |
| 15 | 闪回·写便条 | — |
| 18 | 路人消失 | `lock_npc_talk` |
| 25 | 闪回·镜子 | — |
| 28 | 封锁回头路 | `lock_early_scenes` |
| 30 | 终门解锁 | `final_door_open` |

（第 20 次「NPC 困惑」不在表里配——给 NPC 加个状态，条件 `inv_reached_20`，见 TimedNPC。）

---

## 三、场景物件脚本（挂在场景交互对象上）

### 13. CluePickup2D.cs（Clue/）— 可调查物品 ⭐ 用得最多
**作用**：可调查的物品/痕迹。按 E → 调查计数 +1 → 播调查对话 → 给线索 → 消失（可选）。

**配置**（物品是带 SpriteRenderer 的物体，脚本自动要求 BoxCollider2D 并设为 Trigger，把碰撞框调大一圈当交互范围）：
| 字段 | 说明 |
|---|---|
| `Appear Condition` | 出现条件（默认一直出现） |
| `Inspect Dialogue` | 调查时播的对话 |
| `Clue To Grant` | 给的线索（可空 = 纯调查点，只计数不给线索） |
| `Disappear After Pickup` | 勾 = 拾取后消失（跨存档记住）；不勾 = 可反复调查但线索只给一次 |
| `Counts As Investigation` | 默认勾。装饰性小物件想不计数就取消 |
| `Locked By Flag` | 封锁 Flag。家中物品填 `lock_home_items` |
| `Locked Dialogue` | 封锁后按 E 的台词（"这地方我翻遍了……"，**不勾计数**） |
| `Interaction UI` | 头顶"按 E"提示（物品子物体，默认隐藏） |

**典型配法——家里的旧照片**：
Inspect Dialogue=`Dlg_home_photo`，Clue To Grant=`home_photo`，
Locked By Flag=`lock_home_items`，Locked Dialogue=`Dlg_home_locked`

### 14. TimedNPC.cs（NPC/）— NPC（时限 + 多状态）
**作用**：所有 NPC 都用它。三层机制：
1. **整体出现条件**（appearCondition）：不满足直接隐藏 → 实现"18 次后路人消失"
2. **状态列表**（states）：每个状态 = 条件 + 对话，**列表靠后的优先**。同一 NPC 在不同时间/Flag 下说不同的话
3. **离开设定**：时间段到 `departTimePeriod` 且 `rescueFlag` 未设置 → 永久消失，自动设置 `departed_<npcId>`

**配置**：
| 字段 | 说明 |
|---|---|
| `Npc Id` | 唯一标识（`gardener`、`shopkeeper`…） |
| `Appear Condition` | 路人填 Forbidden Flags: `lock_npc_talk` |
| `States` | 状态数组，见下例 |
| `Depart Time Period` | -1 = 永不离开 |
| `Rescue Flag` | 干涉成功 Flag（由某段对话的 setFlags 设置） |
| `Fallback Dialogue` | 没有状态满足时的兜底对话 |
| `Interaction UI` | 头顶提示 |

**典型配法——会离开的老花匠**：
- Npc Id=`gardener`，Depart Time Period=`3`，Rescue Flag=`helped_gardener`
- States[0]：条件 maxTimePeriod=1 → 日常寒暄
- States[1]：条件 minTimePeriod=2 → 暗示要走了（提示玩家干涉）
- States[2]：条件 requiredFlags=[`helped_gardener`] → 感谢（最后 = 优先级最高）
- States[3]：条件 requiredFlags=[`inv_reached_20`] → "你在找谁？这里从没有别人住过。"

### 15. SceneDoor.cs（Scene/）— 场景门
**作用**：按 E 切换场景。条件不满足播「锁着」台词。

**配置**：
| 字段 | 典型值 |
|---|---|
| `Target Scene Name` | 目标场景名（**必须加进 Build Settings**） |
| `Open Condition` | 普通门：Required Flags=[`scene_cleared_home`]；终门：[`final_door_open`]；回头门：Forbidden Flags=[`lock_early_scenes`] |
| `Locked Dialogue` | "门锁着……好像还缺少什么线索。"（不勾计数） |
| `Enter Dialogue` | 进门前的过场独白（可空，播完才切场景） |

### 16. PlayerMovement2D.cs（根目录）— 玩家移动（原有，未改）
**配置**：玩家物体挂 Rigidbody2D（脚本自动设零重力）+ 本脚本 + Tag 设 `Player`。
`Move Speed` 移速；`Allow Diagonal Movement` 是否允许斜走；`Animator` 可空（有走路动画再接）。

---

## 四、辅助/旧脚本

### MainMenu.cs（Menu/）— 主菜单（原有）
按钮 OnClick 绑 `StartGame()`（按 Build 序号加载场景）/`QuitGame()`。
**待升级**：还没有"继续游戏"读档按钮和性别选择界面（女性 = 设置 `gender_female` flag），需要时我来加。

### SceneIntro.cs（Scene/）— 场景开场白
进入场景自动播一次「前提开头」独白（flag `intro_<sceneId>` 记录，只播一次）。
每个场景放一个空物体挂上，填 `Scene Id` + 拖入对应 `Dlg_intro_*` 对话。
**开场白勾了计数**（6 个开场 + 24 个物品 = 恰好 30 次，正好对齐终门解锁阈值）。

### TextTokens.cs（Core/）— 性别文本替换（静态类，不挂物体）
所有对话/线索/闪回文本显示前自动把 `{sibling}`/`{ta}`/`{kin}` 替换为对应性别用词。
无需配置；写文本时用占位符即可。

### ContentGenerator.cs（Editor/）— 内容资产生成器（编辑器专用）
菜单栏 **Trace Me > 生成全部内容资产**：按策划案一键生成/更新全部线索（24 条）、
对话（约 50 段）、事件表、线索数据库到 `Assets/GameData/`。
**可重复执行**——资产已存在时原地更新（GUID 不变，场景引用不丢），改了策划文本就改脚本重新生成。

---

## 五、从零搭一个场景的完整清单（以「家」为例）

1. **场景物体**：Tilemap/背景、玩家（Rigidbody2D + PlayerMovement2D + Tag=Player）、主摄像机
2. **常驻物体**（首场景才放 GameManager，其余场景不放）：
   - `GameManager`（拖 ClueDatabase）
3. **Canvas 一套**（每个场景都要，建议做成 Prefab 复用）：
   - 对话面板 → DialogueUIManager（含立绘 Image）
   - 闪回覆盖层 → InvestigationDirector（拖事件表）
   - 黑幕 CanvasGroup + 角落进度文本 → 给 SceneClueTracker 用
   - 线索日志面板 → ClueJournalUI
4. **场景逻辑**：
   - 空物体挂 SceneClueTracker（sceneId=`home`，拖 4 个线索、影子物体、黑幕、进度文本）
   - 影子物体（SpriteRenderer，摆好位置，**取消激活**）
5. **交互物**：
   - 4 个 CluePickup2D（旧照片、两副碗筷、日记残页、墙上刻字）
   - 若干 TimedNPC
   - SceneDoor → 学校（Required Flags: `scene_cleared_home`）
6. **资产**：4 个 ClueData（登记进 ClueDatabase！）、若干 DialogueData、主角 CharacterData[]
7. Build Settings 添加所有场景

## 六、易错点备忘

- **新线索必须登记进 ClueDatabase**，否则读档后日志找不到它（生成器已自动登记全部 24 条）
- **ClueId 和 Flag 是裸字符串**，拼写错误不报错只失效——从本手册的表复制粘贴
- 阈值独白、封锁台词、门提示**不要勾 Counts As Investigation**，只有"主角主动调查的独白"才勾
- 交互物的 BoxCollider2D 是 Trigger（脚本自动设置），大小 = 交互范围，比 Sprite 大一圈手感更好
- Canvas 那套 UI 做成 **Prefab**，六个场景复用，改一处全生效

---

# 内容配置对照表（按策划案，资产由生成器创建）

> 先执行菜单 **Trace Me > 生成全部内容资产**，以下资产会出现在 `Assets/GameData/`。
> 之后按表把资产拖到场景物件上即可。所有台词已写入对话资产，无需手填。

## 七、调查次数总账（设计校验）

固定计数来源：**5 段开场白**（家→游乐场）+ **18 个关键物品**（4+4+3+3+4）= 到达天台门前**23 次**。
终门需要 **30 次** → 前五个场景还需布置**至少 7 个「装饰性调查点」**——
CluePickup2D，`Clue To Grant` 留空、`Disappear After Pickup` 不勾、计数保持勾选，
配一段简短氛围独白（如便利店闪烁的灯、学校停在12:30的钟、游乐场打转的落叶）。
建议每场景放 2 个（共 10 个），给玩家留探索余量；只放 7 个则必须全查才开终门。

进入天台后：开场白 +1、6 条线索 +6（若之前不足 30，天台内也会补满）。阈值节奏：

| 累计 | 大约位置 | 触发 |
|---|---|---|
| 5 | 家中段 | 回溯1·背影 |
| 8 | 家收尾/学校开场 | 封锁家中物品 `lock_home_items` |
| 10 | 学校中段 | 动摇独白 |
| 15 | 便利店中段 | 回溯2·便条 |
| 18 | 小巷中段 | 路人消失 `lock_npc_talk` |
| 25 | 游乐场收尾 | 回溯3·镜子 |
| 28 | 游乐场收尾~天台门前 | 封锁回头路 `lock_early_scenes` |
| 30 | 天台门前 | 终门解锁 `final_door_open` |

> **校验规则：增删任何调查点后，确保「天台门之前可达到的总数 ≥ 30」，否则终局无法解锁。**

## 八、各场景接线表

每个场景固定三件套：
- `SceneIntro`（sceneId + `Dlg_intro_场景`）
- `SceneClueTracker`（sceneId + 本场景全部线索 + `Dlg_clear_场景` 作 Clear Monologue + 影子物体 + 黑幕）
- `SceneDoor` 若干（见每场景表）

CluePickup2D 只需要拖 **Inspect Dialogue**（`Dlg_场景_物品`）——线索发放已配置在对话的
Grant Clues On Complete 里，**Clue To Grant 留空**，避免重复发放。

### 场景1【家】sceneId=`home`（场景文件建议名 Home）

| 物件 | 摆放位置 | Inspect Dialogue | 备注 |
|---|---|---|---|
| 旧照片 | 餐桌上（相框倒扣） | `Dlg_home_photo` | 开场醒来后第一个引导目标 |
| 两副碗筷 | 餐桌左右两端 | `Dlg_home_bowls` | 一副干净一副积灰 |
| 身高刻痕 | 门框侧面 | `Dlg_home_marks` | 最高一条标"18岁" |
| 铁盒子 | 床底最里面 | `Dlg_home_tinbox` | 交互范围可稍大（床底不好点） |

- 家中全部 4 个物品的 `Locked By Flag` = `lock_home_items`，`Locked Dialogue` = `Dlg_locked_home`
- 出口门 → School：Open Condition.requiredFlags = [`scene_cleared_home`]，Locked Dialogue = `Dlg_door_locked`
- 通关演出：影子=门口窗帘处的模糊人影，Clear Monologue = `Dlg_clear_home`（"……你是在给我带路吗？"）

### 场景2【学校/教室】sceneId=`school`

| 物件 | 摆放位置 | Inspect Dialogue |
|---|---|---|
| 课桌刻字 | 靠窗第三排课桌 | `Dlg_school_desk` |
| 成绩单 | 讲台抽屉 | `Dlg_school_report` |
| 储物柜 | 走廊尽头铁皮柜 | `Dlg_school_locker` |
| 黑板上的字 | 黑板右下角 | `Dlg_school_board` |

- 回家的门：Open Condition.forbiddenFlags = [`lock_early_scenes`]，Locked Dialogue = `Dlg_door_noreturn`
- 出口门（后门）→ Store：requiredFlags = [`scene_cleared_school`]
- 影子=讲台旁坐着写字的侧影，Clear Monologue = `Dlg_clear_school`（"往前走，别回头。"）

### 场景3【废弃便利店】sceneId=`store`（3 个线索）

| 物件 | 摆放位置 | Inspect Dialogue |
|---|---|---|
| 收银台便条 | 计算器下面 | `Dlg_store_note` |
| 货架上的玩具 | 货架第三层 | `Dlg_store_toy` |
| 门上的手印 | 后门把手附近 | `Dlg_store_handprint` |

- 出口=后门 → Alley：requiredFlags = [`scene_cleared_store`]；影子在门口指路，Clear Monologue = `Dlg_clear_store`
- 氛围建议：通关演出时把场景里的灯光物件全关掉（灯全暗）——黑幕期间 flag 驱动即可

### 场景4【小巷】sceneId=`alley`（3 个线索）

| 物件 | 摆放位置 | Inspect Dialogue |
|---|---|---|
| 墙上的涂鸦 | 左侧墙面 1.5m 高 | `Dlg_alley_graffiti` |
| 地上的烟头 | 墙角一平米内 | `Dlg_alley_cigs` |
| 旧海报 | 右侧墙面（多层） | `Dlg_alley_poster` |

- 出口 → Playground：requiredFlags = [`scene_cleared_alley`]
- 通关演出特殊：策划案是"满墙涂鸦同时亮起"——影子物体可换成一组发光涂鸦 Sprite 的父物体，Clear Monologue = `Dlg_clear_alley`（三连句）

### 场景5【游乐场】sceneId=`playground`

| 物件 | 摆放位置 | Inspect Dialogue |
|---|---|---|
| 旋转木马 | 场地中央 | `Dlg_pg_carousel` |
| 摩天轮 | 场地边缘（5号座舱） | `Dlg_pg_ferris` |
| 长椅 | 旋转木马旁 | `Dlg_pg_bench` |
| 售票亭 | 入口处 | `Dlg_pg_booth` |

- 出口 → Rooftop：requiredFlags = [`scene_cleared_playground`]
- 影子=旋转木马上坐着挥手的影子，Clear Monologue = `Dlg_clear_playground`（"但你没有回头。"）

### 场景6【天台】sceneId=`rooftop`（6 个线索，**顺序解锁**）

天台线索有依赖链（日记→自画像→……→最后一页），用 CluePickup2D 的
**Appear Condition.Required Clues** 配置：

| 物件 | Inspect Dialogue | Appear Condition（Required Clues） | 说明 |
|---|---|---|---|
| 椅子 | `Dlg_roof_chair` | —（一直可查） | Disappear After Pickup **不勾**（椅子不消失） |
| 日记 | `Dlg_roof_diary` | — | 同上不消失 |
| 日记里的自画像 | `Dlg_roof_diary_page` | `roof_diary` | 与日记同位置再放一个触发器 |
| 地上的脚印 | `Dlg_roof_footprints` | — | 楼梯口到椅子之间 |
| 玻璃门上的倒影 | `Dlg_roof_reflection` | `roof_chair`, `roof_diary`, `roof_diary_page`, `roof_footprints` | 前四条集齐才出现 |
| 日记的最后一页 | `Dlg_roof_diary_final` | `roof_reflection` | **对话会设置 `truth_revealed`**（日志切换真相文本） |

- SceneClueTracker.Key Clues 填全部 6 条；集齐 → 影子演出 → 黑幕 → Clear Monologue = `Dlg_clear_rooftop`（结局独白+黑屏两句）
- 终门（天台入口，在游乐场→天台之间或天台楼梯口）：requiredFlags = [`final_door_open`]
- 结局后处理（回主菜单/制作名单）暂未实现，告诉我你想要的形式

## 九、系统对话速查

| 资产 | 用途 | 配到哪 |
|---|---|---|
| `Dlg_inv_5/10/15/25/28` | 阈值回溯/动摇/封路独白 | 已配进事件表，无需手动 |
| `Dlg_locked_home` | 家中物品封锁台词 | 家 4 个物品的 Locked Dialogue |
| `Dlg_door_locked` | 线索未集齐门锁着 | 各场景出口门 Locked Dialogue |
| `Dlg_door_noreturn` | 回头路被封 | 各场景"往回走"的门 Locked Dialogue |
| `Dlg_intro_*` × 6 | 场景开场白（计数） | 各场景 SceneIntro |
| `Dlg_clear_*` × 6 | 通关演出独白 | 各场景 SceneClueTracker |

## 十、待办清单（内容完成后）

1. 主菜单：性别选择（女性 → `SetFlag("gender_female")`）+ 继续游戏按钮
2. 闪回美术图就绪后填入事件表各行的 Flashback.Images（文字版演出已可用）
3. 结局黑屏后的收尾（回主菜单/制作名单）
4. NPC：策划案暂未写具体 NPC，第 20 次困惑台词的 NPC 待内容确定后配置
5. 存档触发点（建议过门自动存档）
