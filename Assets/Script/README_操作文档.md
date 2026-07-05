# Trace Me（寻己）— 从零搭建操作文档

> 这份文档是**操作顺序指南**：按第 0 步到第 9 步依次做，做完即可在 Unity 里跑通游戏。
> 每个字段具体什么含义查阅《README_配置手册.md》；本文档只告诉你**先做什么、点哪里、拖什么**。
> 预计首次搭建时间：UI 部分约 1 小时，之后每个场景约 20 分钟。

---

## 第 0 步：生成内容资产（1 分钟）

1. 打开 Unity 项目，等待编译完成（左下角转圈结束）
2. 菜单栏点击 **Trace Me > 生成全部内容资产**
3. 确认 Console 输出：`[ContentGenerator] 完成：24 条线索及全部对话/事件表已生成到 Assets/GameData`
4. 检查 Project 窗口出现：
   - `Assets/GameData/Clues/`（24 个 Clue_ 资产）
   - `Assets/GameData/Dialogues/`（45 个 Dlg_ 资产）
   - `Assets/GameData/ClueDatabase.asset`
   - `Assets/GameData/InvestigationEventTable.asset`

> 以后想改台词：直接点开对应 Dlg_ 资产在 Inspector 里改；或改 ContentGenerator.cs 里的文本后重新执行菜单（推荐后者，文本有源可查）。

---

## 第 1 步：项目级设置（5 分钟）

### 1.1 Tag
玩家物体必须用 `Player` 标签（Unity 自带，无需新建）。

### 1.2 场景文件
在 `Assets/Scenes/` 下创建 6 个场景（File > New Scene，2D 模板，Ctrl+S 保存）：
`Home`、`School`、`Store`、`Alley`、`Playground`、`Rooftop`
（已有的 `level1.unity` 可以改名为 `Home` 复用，里面已有玩家和对话 UI 的雏形）

### 1.3 Build Settings
File > Build Settings > 把场景按此顺序拖入：
```
0  Menu
1  Home
2  School
3  Store
4  Alley
5  Playground
6  Rooftop
```
（MainMenu 的 StartGame 按序号 1 加载，所以 Home 必须排第 1）

---

## 第 2 步：搭玩家（10 分钟，做一次存为 Prefab）

1. Hierarchy 右键 > 2D Object > Sprites > Square（临时用方块当主角，有立绘素材后替换）
2. 重命名为 `Player`，**Tag 设为 Player**
3. Add Component：
   - `Rigidbody2D`（脚本会自动把重力设 0、锁旋转，无需手动调）
   - `Box Collider 2D`（**不勾 Is Trigger**，这是身体碰撞）
   - `PlayerMovement2D`（Move Speed 默认 4 即可）
4. 把 Player 拖到 Project 窗口存为 Prefab（建议放 `Assets/Prefabs/`，没有该文件夹就新建）
5. 摄像机：小场景直接把 Main Camera 摆中间不动即可；场景比屏幕大时，最简单做法是把 Main Camera 拖成 Player 的子物体（Z 保持 -10）

---

## 第 3 步：搭 UI Canvas（30-40 分钟，做一次存为 Prefab，全场景复用）

> 这是工作量最大的一步。结构做完后**整个 Canvas 存为 Prefab**，六个场景直接拖入。

### 3.1 基础 Canvas
1. Hierarchy 右键 > UI > Canvas（会自动带 EventSystem，保留）
2. Canvas Scaler 组件：UI Scale Mode 改为 **Scale With Screen Size**，Reference Resolution 填 1920×1080

### 3.2 对话面板（DialoguePanel）
在 Canvas 下建（右键 Canvas > UI > …）：

```
Canvas
└── DialoguePanel          （UI > Image；锚点选 bottom-stretch，高约 280，半透明深色底）
    ├── Portrait           （UI > Image；锚点左下，约 220×260，勾 Preserve Aspect）
    ├── SpeakerName        （UI > Text - TextMeshPro；框左上方，字号约 32）
    ├── DialogueText       （UI > Text - TextMeshPro；主体区域，留出左侧立绘位置，字号约 28）
    └── ContinueIndicator  （UI > Image；框右下角小三角/小图标，约 32×32）
```

3. 在 **Canvas 上** Add Component > `DialogueUIManager`，拖引用：
   - Dialogue Panel → DialoguePanel
   - Dialogue Text → DialogueText
   - Speaker Name Text → SpeakerName
   - Continue Indicator → ContinueIndicator
   - Portrait Image → Portrait
   - Chars Per Second：30 / Advance Key：E（默认即可）

> 首次导入 TMP 组件时若弹窗 "Import TMP Essentials"，点导入。
> 中文显示为方块 = 缺中文字体：Window > TextMeshPro > Font Asset Creator，
> 用一个中文 ttf（如思源黑体）生成 Font Asset，设置给所有 TMP 文本。

### 3.3 闪回覆盖层（FlashbackOverlay）
```
Canvas
└── FlashbackOverlay       （UI > Image；锚点 stretch-stretch 全屏，颜色纯黑）
    │                       Add Component > Canvas Group
    ├── FlashbackImage     （UI > Image；全屏拉伸，勾 Preserve Aspect，无图时保持纯黑也可）
    └── FlashbackCaption   （UI > Text - TextMeshPro；居中，字号约 36，白色）
```
4. Canvas 上 Add Component > `InvestigationDirector`，拖引用：
   - Event Table → `Assets/GameData/InvestigationEventTable.asset`
   - Flashback Overlay → FlashbackOverlay（的 CanvasGroup）
   - Flashback Image → FlashbackImage
   - Flashback Caption → FlashbackCaption

### 3.4 黑幕与进度文本
```
Canvas
├── Blackout               （UI > Image；全屏纯黑；Add Component > Canvas Group）
└── ProgressText           （UI > Text - TextMeshPro；锚点右上角，字号约 26，先随便填字）
```
（这两个不挂脚本，等第 5 步给 SceneClueTracker 拖引用用）

### 3.5 线索日志（JournalPanel）
```
Canvas
└── JournalPanel           （UI > Image；居中大面板，约 1400×800）
    ├── ListScroll         （UI > Scroll View；靠左，约 400 宽；删掉横向滚动条）
    │   └── Viewport/Content   ← 这个 Content 就是 List Content
    │        （给 Content 加 Vertical Layout Group + Content Size Fitter[Vertical=Preferred]）
    └── Detail             （空物体，靠右）
        ├── DetailTitle        （TMP，字号 34）
        ├── DetailIcon         （UI > Image，约 128×128）
        ├── DetailDescription  （TMP，字号 24）
        └── DetailMeaning      （TMP，字号 24，建议斜体/另一颜色区分"解读"）
```
5. **列表项预制体**：Canvas 下临时建一个 UI > Button - TextMeshPro，重命名 `ClueListItem`，
   高度约 60，把它拖到 `Assets/Prefabs/` 存为 Prefab，然后**删掉场景里这个临时按钮**
6. Canvas 上 Add Component > `ClueJournalUI`，拖引用：
   - Journal Panel → JournalPanel
   - List Content → ListScroll/Viewport/Content
   - List Item Prefab → ClueListItem 预制体
   - Detail Title / Description / Meaning / Icon → 对应四个
   - Toggle Key：Tab（默认）

### 3.6 交互提示（"按 E"气泡）
这个不在 Canvas 里——做一个**世界空间**的小提示，跟着物品走：
1. 场景里建 2D Object > Sprites > Square，改小（如 0.5×0.5），换成"E"字样的 Sprite 或加个世界空间 TMP 子文本
2. 存为 Prefab `InteractPrompt`，删掉场景里的
3. 之后每个交互物把它拖为**子物体**，摆在头顶，**默认取消勾选（隐藏）**，再拖给脚本的 Interaction UI 字段

### 3.7 存 Prefab
把整个 **Canvas 拖到 Assets/Prefabs/ 存为 Prefab**（EventSystem 不用进 Prefab，每个场景保留一个即可）。

---

## 第 4 步：GameManager（2 分钟）

1. Hierarchy 建空物体，命名 `GameManager`
2. Add Component > `GameManager`，把 `Assets/GameData/ClueDatabase.asset` 拖到 Clue Database
3. 存为 Prefab
4. **每个场景都放一个**：它自带跨场景不销毁 + 重复自毁逻辑，每个场景都放的好处是——你可以在编辑器里直接从任意场景按 Play 测试，不必每次从 Home 开始

---

## 第 5 步：搭第一个场景【家 Home】（20 分钟）

打开 Home 场景，放入：**Player Prefab、Canvas Prefab、GameManager Prefab、EventSystem**（若无）。

### 5.1 场景逻辑物体
建空物体 `SceneLogic`，加两个组件：

**SceneIntro**：
- Scene Id：`home`
- Intro Dialogue → `Dlg_intro_home`

**SceneClueTracker**：
- Scene Id：`home`
- Key Clues → 拖 4 个：`Clue_home_photo`、`Clue_home_bowls`、`Clue_home_marks`、`Clue_home_tinbox`
- Progress Text → Canvas 里的 ProgressText
- Brother Shadow → 见 5.3
- Blackout → Canvas 里的 Blackout（CanvasGroup）
- Clear Monologue → `Dlg_clear_home`

### 5.2 四个调查物品
每个物品：2D Sprite（临时方块也行）+ Add Component > `CluePickup2D`（碰撞框自动加，把 Size 调大一圈当交互范围）+ 子物体 InteractPrompt（隐藏，拖给 Interaction UI）。

| 物品 | Inspect Dialogue | Locked By Flag | Locked Dialogue | 其他 |
|---|---|---|---|---|
| 旧照片（餐桌） | `Dlg_home_photo` | `lock_home_items` | `Dlg_locked_home` | 默认值即可 |
| 两副碗筷（餐桌两端） | `Dlg_home_bowls` | `lock_home_items` | `Dlg_locked_home` | 同上 |
| 身高刻痕（门框） | `Dlg_home_marks` | `lock_home_items` | `Dlg_locked_home` | Disappear After Pickup **不勾**（刻痕不会消失） |
| 铁盒子（床底） | `Dlg_home_tinbox` | `lock_home_items` | `Dlg_locked_home` | 默认值即可 |

> ⚠️ 三个易错点：
> - **Clue To Grant 全部留空**！线索发放已配置在对话资产里，填了会重复
> - Appear Condition 全部保持默认（一直出现）
> - Counts As Investigation 保持勾选

### 5.3 影子
1. 建 2D Sprite，命名 `BrotherShadow`，用半透明深色人形（临时可用拉长的黑色方块，透明度调到 0.5）
2. 摆在门口位置
3. **取消勾选（隐藏）**——SceneClueTracker 会在通关演出时激活它
4. 拖给 SceneClueTracker 的 Brother Shadow

### 5.4 出口门
建 2D Sprite（门），Add Component > `SceneDoor`：
- Target Scene Name：`School`
- Open Condition > Required Flags：填 1 个元素 `scene_cleared_home`
- Locked Dialogue → `Dlg_door_locked`
- Interaction UI → 子物体 InteractPrompt

### 5.5 测试！
按 Play，验证这条流程：
1. 开场白自动播放（第一次调查计数 +1，看 Console 日志）
2. 走近照片出现提示 → 按 E 对话 → 右上角进度变 1/4
3. Tab 打开日志能看到"旧照片"，有描述和表层解读
4. 集齐 4 个 → 影子出现 2 秒 → 黑幕 → "……你是在给我带路吗？"
5. 走到门按 E → 切到 School 场景（School 还没搭好会白屏，正常）
6. Console 全程无红色报错

---

## 第 6 步：复制流程搭其余 4 个场景（每个约 20 分钟）

School / Store / Alley / Playground 与 Home 完全同构，只有内容不同。
每个场景：拖入四件套（Player、Canvas、GameManager、EventSystem）→ SceneLogic（SceneIntro + SceneClueTracker）→ 调查物品 → 影子 → 门。

**每场景参数速查**（对话和线索资产名详见《配置手册》第八节）：

| 场景 | sceneId | Intro / Clear 对话 | 线索数 | 出口门 Required Flags | 目标场景 |
|---|---|---|---|---|---|
| School | `school` | `Dlg_intro_school` / `Dlg_clear_school` | 4 | `scene_cleared_school` | Store |
| Store | `store` | `Dlg_intro_store` / `Dlg_clear_store` | 3 | `scene_cleared_store` | Alley |
| Alley | `alley` | `Dlg_intro_alley` / `Dlg_clear_alley` | 3 | `scene_cleared_alley` | Playground |
| Playground | `playground` | `Dlg_intro_playground` / `Dlg_clear_playground` | 4 | `scene_cleared_playground` | Rooftop |

**额外两件事：**

1. **回头门**：School 起每个场景加一扇往回走的门（SceneDoor）：
   - Target Scene Name：上一个场景名
   - Open Condition > Forbidden Flags：`lock_early_scenes`
   - Locked Dialogue → `Dlg_door_noreturn`
2. **装饰性调查点**（可选）：每个场景可放几个纯氛围调查物——
   CluePickup2D，Inspect Dialogue 随意配一段氛围独白（可在 GameData/Dialogues 手动
   Create > 游戏数据 > 对话 新建），**Clue To Grant 留空、Disappear After Pickup 不勾、计数保持勾选**。
   例：学校停在 12:30 的钟、便利店闪烁的灯牌、小巷的易拉罐、游乐场打转的落叶。
   > 注意：结局已改为「探索完整度」判定，**装饰性调查点不再影响结局**，纯氛围，放不放都行。

---

## 第 7 步：天台 Rooftop（30 分钟，有特殊配置）

前置相同：四件套 + SceneLogic（sceneId=`rooftop`，Intro=`Dlg_intro_rooftop`，Clear=`Dlg_clear_rooftop`，Key Clues 拖全部 6 条 roof_ 线索）。
**SceneClueTracker 额外勾 `Require Truth Revealed`**（只有真结局才播通关演出）。

**6 个调查物的依赖链配置**（核心区别在 Appear Condition > Required Clues）：

| 物件 | Inspect Dialogue | Required Clues 填 | 特殊设置 |
|---|---|---|---|
| 椅子 | `Dlg_roof_chair` | （空） | Disappear After Pickup 不勾 |
| 日记 | `Dlg_roof_diary` | （空） | 不勾消失 |
| 日记里的自画像 | `Dlg_roof_diary_page` | `roof_diary` | 和日记放同一位置（叠放触发器）；日记本体不消失所以碰撞框错开一点 |
| 地上的脚印 | `Dlg_roof_footprints` | （空） | 楼梯口到椅子之间，不勾消失 |
| 玻璃门上的倒影 | `Dlg_roof_reflection` | `roof_chair`、`roof_diary`、`roof_diary_page`、`roof_footprints`（4 个全填） | 前四条集齐才出现 |
| 日记的最后一页 | `Dlg_roof_diary_final` | `roof_reflection` | 收集时由 EndingGate 判定真/坏结局 |

**结局判定（EndingGate）**：天台放一个空物体挂 `EndingGate`：
- Bad Ending → `Dlg_bad_ending`
- Menu Scene Index → 主菜单在 Build Settings 的序号（一般 0）
- 前五关 18 条关键线索全查过 = 真结局（设 truth_revealed，播通关演出）；
  有遗漏 = 坏结局（播「被那封信骗了」独白后回主菜单）。

**天台入口门**：放在 Playground 场景的出口处，配置：
- Open Condition > Required Flags：`scene_cleared_playground`（只需这一个，不再需要 final_door_open）
- Locked Dialogue → `Dlg_door_locked`

**结局验证**：拿到最后一页后按 Tab 打开线索日志——所有线索的"解读"应已变成真相文本。

---

## 第 8 步：主菜单（10 分钟）

打开 Menu 场景（已有 MainMenu 脚本）：
1. 确认开始按钮 OnClick 绑定 `MainMenu.StartGame`，Game Scene Index = 1（Home）
2. 性别选择/继续游戏功能暂未实现（在待办清单里）——当前默认男性（哥哥线）。
   临时测试姐姐线：任意脚本里调用 `GameManager.Instance.SetFlag("gender_female")`，
   或等我加上选择界面。

---

## 第 9 步：完整流程测试清单

从 Menu 按 Play 开始，全程走一遍，逐项打勾：

- [ ] 主菜单 → 开始游戏进入 Home
- [ ] 每个场景开场白只播一次（切出去再回来不重播）
- [ ] 调查计数：Console 里 `[GameManager] 调查次数: N` 递增
- [ ] 第 5 次：闪回演出（黑屏文字"……哥哥？"）
- [ ] 第 8 次后回 Home 调查任意物品：只出"这地方我翻遍了……"且**不再计数**
- [ ] 第 10 次：动摇独白自动播放
- [ ] 第 28 次后走回头门："过去回不去了"
- [ ] 每场景集齐线索：影子 → 黑幕 → 独白 → 门可通行
- [ ] 天台：倒影在前四条线索集齐前不出现
- [ ] 日记最后一页后：Tab 日志全部变真相文本
- [ ] 天台通关：结局独白 + "后来我再也没有上过这个天台。"
- [ ] 全程 Console 无红色报错

---

## 常见问题排查

| 现象 | 原因 |
|---|---|
| 按 E 没反应 | 玩家 Tag 不是 Player / 物品碰撞框太小 / 对话面板引用没拖 |
| 对话文字是方块 | TMP 缺中文字体，见 3.2 备注 |
| 线索拿了日志里没有 | 用了手动 Clue To Grant 且没登记数据库——记住 Clue To Grant 留空 |
| 线索给了两次 | Clue To Grant 和对话里都配了——清空 Clue To Grant |
| 影子/黑幕不播 | Brother Shadow 或 Blackout 引用没拖 / Key Clues 数量不对 |
| 天台门永远锁着 | 该门 Required Flags 应为 `scene_cleared_playground`（游乐场未通关则打不开） |
| 全查了却进坏结局 | 前五关有线索漏收——检查每关 SceneClueTracker 是否都通关（scene_cleared 全设） |
| 从中途场景 Play 报空引用 | 该场景忘放 GameManager Prefab |
| {sibling} 原样显示出来了 | 文本没走 TextTokens（自建 UI 直接 SetText 会绕过）——用 DialogueData 播放 |
