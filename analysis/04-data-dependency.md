# 数据流与依赖分析报告 — MainForm 与外部类的数据依赖关系

**项目**: 四人拖拉机 (升级/80分) Windows 桌面游戏 (C# WinForms .NET 4.6.1)
**分析范围**: MainForm、DrawingFormHelper、CalculateRegionHelper、CurrentPoker、Algorithm、TractorRules、MustSendCardsAlgorithm、ShouldSendedCardsAlgorithm、GameConfig、DefinedConstant、CommonMethods
**分析日期**: 2026-05-12

---

## 1. MainForm 字段全分类

> 字段声明在 `MainForm.cs` 的 `#region 字段定义` 区域 (第12-77行)，以及 `MainForm.Designer.cs` 中的 WinForms 控件。

### 1.1 游戏状态 (Game State)

| 字段 | 类型 | 说明 | 被哪些类读取/写入 |
|---|---|---|---|
| `currentState` | `CurrentState` | 当前游戏完整状态结构体 (OurCurrentRank, OpposedCurrentRank, Suit, Master, CurrentCardCommands 等) | **读取**: DrawingFormHelper (几乎所有绘制方法)、Algorithm (ShouldSetRank, CanSetRank)、TractorRules (几乎所有方法)、MustSendCardsAlgorithm (所有方法)、ShouldSendedCardsAlgorithm<br>**写入**: MainForm.timer_Tick、MainForm.MenuItem_Click、MainForm.init、MainForm.MouseClick/DoubleClick、DrawingFormHelper (DrawFinishedSendedCards, DrawNextUserSendedCards 等)、TractorRules.GetNextMasterUser |
| `currentRank` | `int` | 当前牌局的 Rank (0=2, ..., 12=A, 53=无主) | **读取**: DrawingFormHelper (DrawRank, DrawSuit, DrawMySortedCards, 绘制排名的各方法)、CommonMethods.parse、Algorithm.ShouldSetRank、TractorRules (所有方法)、MustSendCardsAlgorithm (所有方法)<br>**写入**: MainForm.MenuItem_Click、MainForm.init、TractorRules.GetNextRank |
| `isNew` | `bool` | 是否新开局 | **读取**: DrawingFormHelper.DrawFinishedSendedCards、MainForm.SelectImage_Click<br>**写入**: MainForm.MenuItem_Click、MainForm.init、DrawingFormHelper.DrawFinishedSendedCards |
| `showSuits` | `int` | 显示花色次数 (0/1/2/3) | **读取**: DrawingFormHelper.DrawSuitCards、DrawingFormHelper.DoRankOrNot、DrawingFormHelper.DrawMyOneOrTwoCards、Algorithm.CanSetRank<br>**写入**: DrawingFormHelper.DoRankOrNot、DrawingFormHelper.IsClickedRanked |
| `whoShowRank` | `int` | 谁亮的牌 (0=无人, 1=我, 2=对家, 3=上家, 4=下家) | **读取**: DrawingFormHelper (DrawSuitCards, DoRankOrNot, DrawMyOneOrTwoCards)、Algorithm.IsInvalidRank<br>**写入**: DrawingFormHelper.DoRankOrNot、DrawingFormHelper.IsClickedRanked |
| `Scores` | `int` | 累计得分 | **读取**: DrawingFormHelper (DrawScoreImage, DrawFinishedScoreImage)、TractorRules.GetNextRank、Algorithm.ShouldSendedCards<br>**写入**: MainForm.init、TractorRules.CalculateScore、TractorRules.Calculate8CardsScore、TractorRules.GetNextMasterUser |
| `whoIsBigger` | `int` | 当前圈谁最大 (0=未定, 1=我, 2=对家, 3=上家, 4=下家) | **读取**: DrawingFormHelper.DrawFinishedOnceSendedCards、MustSendCardsAlgorithm (所有 WhoseOrderIs2/3/4 方法)<br>**写入**: MainForm.MenuItem_Click、MainForm.init、DrawingFormHelper.DrawFinishedOnceSendedCards、MustSendCardsAlgorithm (各方法) |
| `firstSend` | `int` | 一圈中谁先出 (1=我, 2=对家, 3=上家, 4=下家) | **读取**: DrawingFormHelper (DrawFinishedOnceSendedCards, DrawNextUserSendedCards 等)、TractorRules (GetNextOrder, IsInvalid, GetNextMasterUser)、Algorithm.MustSendedCards<br>**写入**: MainForm.timer_Tick、DrawingFormHelper.DrawFinishedOnceSendedCards |

### 1.2 卡牌数据 (Card Data)

| 字段 | 类型 | 说明 |
|---|---|---|
| `dpoker` | `DistributePokerHelper` | 发牌器 |
| `pokerList` | `ArrayList[]` | 4个玩家的手牌原始数据 (int 列表) |
| `currentPokers` | `CurrentPoker[]` | 4个玩家的 CurrentPoker 对象 (结构化统计) |
| `currentCount` | `int` | 发牌计数器 (UI动画用) |
| `currentSendCards` | `ArrayList[]` | 当前圈各玩家已出的牌 |
| `whoseOrder` | `int` | 当前轮到谁出牌 (0=未定, 1=我, 2=对家, 3=上家, 4=下家) |
| `currentAllSendPokers` | `CurrentPoker[]` | 所有已出牌的结构化统计 (用于 AI 插件) |
| `send8Cards` | `ArrayList` | 底牌的8张牌 |

**数据流分析**:

| 字段 | DrawingFormHelper | CalculateRegionHelper | Algorithm 系列 | TractorRules | MustSendCards | ShouldSendedCards |
|---|---|---|---|---|---|---|
| `dpoker` | — | — | — | — | — | — |
| `pokerList` | **R**: ReadyCards, DrawCenter8Cards | — | **R/W**: Send8Cards, ShouldSendedCards, MustSendedCards | — | **R/W**: WhoseOrderIs2/3/4 | **R/W**: ShouldSendCards |
| `currentPokers` | **R**: ReadyCards, DrawMyCards, DrawMySortedCards, DoRankOrNot | — | **R**: ShouldSetRank, CanSetRank, ShouldSendedCards, MustSendedCards, Send8Cards | **R**: GetNextOrder, IsInvalid, CheckSendCards, GetNextMasterUser | **R/W**: WhoseOrderIs2/3/4 | **R/W**: ShouldSendCards |
| `currentCount` | **R**: ReadyCards | — | — | — | — | — |
| `currentSendCards` | **R/W**: DrawMyFinishSendedCards, DrawNextUserSendedCards, DrawFinishedOnceSendedCards, DrawFinishedSendedCards | — | **R/W**: ShouldSendedCards, MustSendedCards | **R**: GetNextOrder, IsInvalid, CalculateScore, Calculate8CardsScore, GetNextMasterUser | **R**: WhoseOrderIs2/3/4 | — |
| `whoseOrder` | **R/W**: DrawMyFinishSendedCards, DrawNextUserSendedCards, DrawFinishedOnceSendedCards, DrawFinishedSendedCards | — | — | — | — | — |
| `currentAllSendPokers` | — | — | **R/W**: ShouldSendedCards, MustSendedCards | — | — | — |
| `send8Cards` | **R**: DrawFinishedScoreImage | — | **R/W**: Send8Cards1/2/3 | **R**: Calculate8CardsScore, GetNextMasterUser | — | — |

### 1.3 UI/交互状态 (UI State)

| 字段 | 类型 | 说明 | 依赖 |
|---|---|---|---|
| `myCardsLocation` | `ArrayList` | 我方手牌每张牌的 X 坐标 (用于点击检测和渲染) | **写入**: DrawingFormHelper (DrawMySortedCards, SetCardsInformation)<br>**读取**: CalculateRegionHelper (CalculateClickedRegion, CalculateDoubleClickedRegion, CalculateRightClickedRegion), DrawingFormHelper (DrawMyPlayingCards, DrawMyOneOrTwoCards2) |
| `myCardsNumber` | `ArrayList` | 我方手牌每张牌的牌号 | **写入**: DrawingFormHelper.SetCardsInformation<br>**读取**: CalculateRegionHelper (间接通过 myCardIsReady), MainForm.MouseClick, TractorRules.IsInvalid |
| `myCardIsReady` | `ArrayList` | 我方手牌每张牌是否被选中 | **写入**: CalculateRegionHelper (CalculateClickedRegion, CalculateRightClickedRegion), MainForm.MouseClick<br>**读取**: MainForm.MouseClick, MainForm.MouseDoubleClick, DrawingFormHelper (DrawMyPlayingCards, DrawMyOneOrTwoCards2, My8CardsIsReady), TractorRules.IsInvalid |
| `cardsOrderNumber` | `int` | 绘制手牌时的顺序计数器 | **写入/读取**: DrawingFormHelper.DrawMyPlayingCards → DrawMyOneOrTwoCards2 |
| `sleepTime` | `long` | 暂停开始时间 (Tick) | **写入**: MainForm.SetPauseSet<br>**读取**: MainForm.timer_Tick (Pause 分支) |
| `sleepMaxTime` | `long` | 暂停最大时长 (ms) | **写入**: MainForm.SetPauseSet<br>**读取**: MainForm.timer_Tick (Pause 分支) |
| `wakeupCardCommands` | `CardCommands` | 暂停结束后的命令 | **写入**: MainForm.SetPauseSet<br>**读取**: MainForm.timer_Tick (Pause 分支) |

### 1.4 渲染相关 (Rendering)

| 字段 | 类型 | 说明 | 依赖 |
|---|---|---|---|
| `bmp` | `Bitmap` | 双缓冲位图，所有绘制操作的画布 | **写入/读取**: DrawingFormHelper (所有方法), MainForm.Paint, MainForm.SelectImage_Click |
| `image` | `Bitmap` | 背景图片 | **读取**: DrawingFormHelper (DrawBackground, DrawCenterImage, DrawMyPlayingCards 等所有清屏操作), MainForm.SelectImage_Click |
| `drawingFormHelper` | `DrawingFormHelper` | 绘图助手 | 只在 MainForm 内部使用 |
| `calculateRegionHelper` | `CalculateRegionHelper` | 区域计算助手 | 只在 MainForm 内部使用 |
| `cardsImages` | `Bitmap[]` | 自定义卡牌图像缓存 (54张) | **读取**: DrawingFormHelper.getPokerImageByNumber<br>**写入**: MainForm 构造函数 (初始化 null) |

### 1.5 配置 (Configuration)

| 字段 | 类型 | 说明 | 依赖 |
|---|---|---|---|
| `gameConfig` | `GameConfig` | 游戏配置 (速度、规则、资源管理器等) | **读取**: DrawingFormHelper (几乎所有方法), Algorithm (Send8Cards), TractorRules (GetNextRank, GetNextMasterUser)<br>**写入**: MainForm (InitAppSetting, MenuItem_Click 等菜单事件) |

### 1.6 AI相关 (AI/Algorithms)

| 字段 | 类型 | 说明 | 依赖 |
|---|---|---|---|
| `UserAlgorithms` | `object[]` | 4个玩家的自定义 AI 算法插件 | **读取**: Algorithm.ShouldSendedCards, Algorithm.MustSendedCards<br>**写入**: MainForm.SelectAlgorithmToolStripMenuItem_Click |

### 1.7 其他

| 字段 | 类型 | 说明 |
|---|---|---|
| `musicFile` | `string` | 音乐文件路径 (只在 MainForm 内使用) |

---

## 2. 数据流关系图 (文本形式)

### 2.1 DrawingFormHelper → MainForm 数据依赖

DrawingFormHelper 持有 `MainForm` 引用 (构造函数注入)，所有绘制方法通过 `mainForm.xxx` 直接访问字段。

```
DrawingFormHelper
  │
  ├─ mainForm.bmp          ← R/W (画布)
  ├─ mainForm.image        ← R (背景擦除)
  ├─ mainForm.gameConfig   ← R (BackImage, CardsResourceManager, CardImageName, 速度常量)
  ├─ mainForm.pokerList    ← R (ReadyCards, DrawCenter8Cards)
  ├─ mainForm.currentPokers ← R (所有绘制手牌方法, DoRankOrNot)
  ├─ mainForm.currentState  ← R (Suit, Rank, Master, CurrentCardCommands)
  ├─ mainForm.currentRank   ← R (绘制Rank用)
  ├─ mainForm.currentCount  ← R (ReadyCards)
  ├─ mainForm.currentSendCards ← R (DrawMyFinishSendedCards, 显示各家出牌)
  ├─ mainForm.currentAllSendPokers ← R (DrawFinishedOnceSendedCards)
  ├─ mainForm.cardsImages   ← R (getPokerImageByNumber)
  ├─ mainForm.myCardsLocation ← W (SetCardsInformation)
  ├─ mainForm.myCardsNumber  ← W (SetCardsInformation)
  ├─ mainForm.myCardIsReady  ← W (SetCardsInformation)
  ├─ mainForm.cardsOrderNumber ← R/W (DrawMyPlayingCards → DrawMyOneOrTwoCards2)
  ├─ mainForm.whoseOrder    ← R/W (出牌流程控制)
  ├─ mainForm.whoIsBigger   ← W (tractorRule回调后间接)
  ├─ mainForm.firstSend     ← R (DrawNext/Frield/Previous UserSendedCards)
  ├─ mainForm.showSuits     ← R/W (DoRankOrNot)
  ├─ mainForm.whoShowRank   ← R/W (DoRankOrNot)
  ├─ mainForm.Scores        ← R/W (DrawScoreImage, DrawFinishedScoreImage)
  ├─ mainForm.currentRank   ← R
  ├─ mainForm.send8Cards    ← R (DrawFinishedScoreImage)
  ├─ mainForm.isNew         ← W (DrawFinishedSendedCards)
  ├─ mainForm.Refresh()     ← 调用 (强制重绘)
  ├─ mainForm.SetPauseSet() ← 调用 (流程控制)
  └─ mainForm.timer         ← 间接控制
```

**关键**: DrawingFormHelper 调用了 **Algorithm.ShouldSendedCards / MustSendedCards** (静态方法，传入 mainForm) 以及 **TractorRules.GetNextOrder / CalculateScore / GetNextMasterUser**，所以 DrawingFormHelper 实际上间接驱动了整个游戏逻辑。

### 2.2 CalculateRegionHelper → MainForm 数据依赖

```
CalculateRegionHelper
  │
  ├─ mainForm.myCardsLocation  ← R (所有方法)
  ├─ mainForm.myCardsNumber    ← R (间接)
  ├─ mainForm.myCardIsReady    ← R/W (点击后切换选中状态)
  └─ 纯数学计算: Region.Intersect/Exclude/IsVisible
```

**特点**: CalculateRegionHelper 只读 UI 状态，只写 `myCardIsReady`。不涉及游戏逻辑。

### 2.3 Algorithm 静态类 → MainForm 数据依赖

所有方法都是 `internal static`，接受 `MainForm mainForm` 参数。

```
Algorithm
  │
  ├─ [ShouldSetRank]
  │   ├─ mainForm.currentPokers[user-1] ← R (判断能否亮牌)
  │   └─ mainForm.currentShowSuits  ← R (间接通过 DrawingFormHelper 调用时的上下文)
  │
  ├─ [CanSetRank]
  │   ├─ mainForm.currentRank        ← R
  │   ├─ mainForm.currentPokers[0]   ← R
  │   ├─ mainForm.showSuits          ← R
  │   ├─ mainForm.currentState.Suit  ← R
  │   ├─ mainForm.whoShowRank        ← R
  │   └─ mainForm.gameConfig         ← R (CanMyRankAgain, CanMyStrengthen, CanRankJack)
  │
  ├─ [Send8Cards]
  │   ├─ mainForm.pokerList[user-1]  ← R/W (删除8张底牌)
  │   ├─ mainForm.send8Cards         ← W (写入底牌)
  │   ├─ mainForm.currentState.Suit  ← R
  │   ├─ mainForm.currentRank        ← R
  │   ├─ mainForm.gameConfig.BottomAlgorithm ← R
  │   └─ mainForm.initSendedCards()  ← 调用
  │
  ├─ [ShouldSendedCards / MustSendedCards]
  │   ├─ mainForm.currentPokers      ← R
  │   ├─ mainForm.pokerList          ← R/W
  │   ├─ mainForm.currentAllSendPokers ← R/W
  │   ├─ mainForm.currentState       ← R
  │   ├─ mainForm.firstSend          ← R
  │   ├─ mainForm.currentSendCards   ← R
  │   ├─ mainForm.UserAlgorithms     ← R (AI插件接口)
  │   └─ mainForm.whoIsBigger        ← W (在 ShouldSendedCardsAlgorithm 中设置)
  │
  └─ [ShouldSetRankAgain]
      ├─ mainForm.showSuits          ← R
      └─ mainForm.currentPokers      ← R (caller 传入)
```

### 2.4 TractorRules 静态类 → MainForm 数据依赖

```
TractorRules
  │
  ├─ [IsInvalid]
  │   ├─ mainForm.currentState.Suit   ← R
  │   ├─ mainForm.currentRank         ← R
  │   ├─ mainForm.firstSend           ← R
  │   ├─ mainForm.myCardIsReady       ← R
  │   ├─ mainForm.myCardsNumber       ← R
  │   ├─ mainForm.currentSendCards    ← R
  │   └─ mainForm.currentPokers       ← R (用于检查实际手牌)
  │
  ├─ [GetNextOrder]
  │   ├─ mainForm.currentState.Suit   ← R
  │   ├─ mainForm.currentRank         ← R
  │   ├─ mainForm.currentSendCards    ← R
  │   ├─ mainForm.firstSend           ← R
  │   └─ mainForm.currentPokers       ← R (CheckSendCards 也读)
  │
  ├─ [CalculateScore / Calculate8CardsScore]
  │   ├─ mainForm.currentSendCards    ← R
  │   ├─ mainForm.send8Cards          ← R
  │   └─ mainForm.Scores              ← W
  │
  ├─ [GetNextMasterUser]
  │   ├─ mainForm.currentState        ← R/W (Master, OurCurrentRank, OpposedCurrentRank)
  │   ├─ mainForm.currentRank         ← W
  │   ├─ mainForm.Scores              ← R/W
  │   ├─ mainForm.currentSendCards    ← R
  │   ├─ mainForm.send8Cards          ← R
  │   ├─ mainForm.gameConfig          ← R (MustRank, JToBottom, QToHalf, AToJ)
  │   └─ mainForm.currentRank         ← W
  │
  ├─ [CheckSendCards]
  │   ├─ mainForm.currentState.Suit   ← R
  │   ├─ mainForm.currentRank         ← R
  │   ├─ mainForm.myCardIsReady       ← R
  │   ├─ mainForm.myCardsNumber       ← R
  │   └─ mainForm.currentPokers       ← R
  │
  └─ [GetNextRank]
      ├─ mainForm.currentState        ← R/W
      ├─ mainForm.Scores              ← R
      ├─ mainForm.currentRank         ← W
      └─ mainForm.gameConfig          ← R
```

### 2.5 MustSendCardsAlgorithm / ShouldSendedCardsAlgorithm → MainForm

这两个类同样通过静态方法接受 `MainForm mainForm` 参数，访问模式与 Algorithm 类似。

**MustSendCardsAlgorithm** 核心依赖:
- `mainForm.currentState.Suit / currentRank` — 游戏状态
- `mainForm.currentSendCards[firstSend-1]` — 首出牌
- `mainForm.currentPokers[whoseOrder-1]` — 当前玩家手牌
- `mainForm.pokerList[whoseOrder-1]` — 修改手牌列表
- `mainForm.whoIsBigger` — R/W (控制谁最大)
- `mainForm.firstSend` — R

**ShouldSendedCardsAlgorithm** 核心依赖:
- `mainForm.currentPokers` — R/W
- `mainForm.pokerList` — R/W
- `mainForm.currentRank` — R
- `mainForm.whoIsBigger` — W (在方法开头设置)
- `mainForm.currentAllSendPokers` — 间接 (通过 Algorithm.ShouldSendedCards)

### 2.6 只在 MainForm 内部使用的字段

| 字段 | 说明 |
|---|---|
| `dpoker` | 只在 init() 中使用，创建后即被抛弃 |
| `bmp` | MainForm_Paint 绘制到屏幕，DrawingFormHelper 在上面作画 |
| `image` | 读取背景图 |
| `drawingFormHelper` | mainForm 创建，传入 this 给 DrawingFormHelper |
| `calculateRegionHelper` | mainForm 创建，传入 this 给 CalculateRegionHelper |
| `musicFile` | 只读/写在菜单事件中 |
| `cardsImages` | 自定义卡牌图像缓存，被 DrawingFormHelper.getPokerImageByNumber 读取 |
| `sleepTime / sleepMaxTime / wakeupCardCommands` | 仅在 timer_Tick 的 Pause 分支使用 |
| `currentCount` | 仅在 timer_Tick 的 ReadyCards 分支使用 (发牌计数) |
| `cardsOrderNumber` | 仅被 DrawingFormHelper.DrawMyOneOrTwoCards2 读写 |

---

## 3. GameEngine 抽取方案

### 3.1 核心问题分析

当前架构的核心问题: **所有算法类 (Algorithm, TractorRules, MustSendCardsAlgorithm, ShouldSendedCardsAlgorithm) 都是 MainForm 的静态方法扩展**。它们接受 `MainForm` 引用，直接读取/改写 MainForm 的字段。DrawingFormHelper 也持有 MainForm 引用并直接访问其字段。

抽取 GameEngine 的难度等级: **高** — 因为耦合极为紧密。

### 3.2 应该进入 GameEngine 的字段

```
GameEngine {
    // 游戏状态
    currentState: CurrentState
    currentRank: int
    isNew: bool
    showSuits: int
    whoShowRank: int
    Scores: int
    whoIsBigger: int
    firstSend: int
    whoseOrder: int

    // 卡牌数据
    pokerList: ArrayList[]          // 原始手牌 (所有玩家)
    currentPokers: CurrentPoker[]   // 结构化手牌
    currentSendCards: ArrayList[]   // 当前圈出牌
    currentAllSendPokers: CurrentPoker[]  // 已出牌统计
    send8Cards: ArrayList           // 底牌

    // 配置
    gameConfig: GameConfig

    // AI
    UserAlgorithms: object[]

    // 构造函数注入
    dpoker: DistributePokerHelper   // 或移入 GameEngine 创建
}
```

### 3.3 应该留在 Form 层的字段

```
MainForm (Form) {
    // 渲染
    bmp: Bitmap
    image: Bitmap
    cardsImages: Bitmap[]

    // 绘图助手
    drawingFormHelper: DrawingFormHelper
    calculateRegionHelper: CalculateRegionHelper

    // UI 交互状态
    myCardsLocation: ArrayList
    myCardsNumber: ArrayList
    myCardIsReady: ArrayList
    cardsOrderNumber: int

    // 动画控制
    currentCount: int           // 发牌计数器 (纯UI)
    sleepTime: long
    sleepMaxTime: long
    wakeupCardCommands: CardCommands

    // 音频
    musicFile: string
}
```

### 3.4 应该进入 GameEngine 的方法

```
GameEngine {
    // 核心游戏流程
    init()                              // 初始化游戏
    initSendedCards()                   // 重新解析各玩家手牌
    timer_Tick()                        // 游戏主循环 (改为 tick() 方法)
    SetPauseSet(max, wakeup)            // 暂停控制

    // 状态变更
    MenuItem_Click()                    // "开始新游戏" 等菜单 → 改为 StartNewGame()
    RestoreToolStripMenuItem_Click()    // 读取存档 → 改为 RestoreFromSave()

    // 算法/规则调用 (交由 GameEngine 调度)
    // Algorithm.ShouldSetRank → engine.ShouldSetRank(user)
    // Algorithm.CanSetRank → engine.CanSetRank(user)
    // Algorithm.Send8Cards → engine.Send8Cards(user)
    // Algorithm.ShouldSendedCards → engine.ShouldSendedCards(whoseOrder)
    // Algorithm.MustSendedCards → engine.MustSendedCards(whoseOrder, count)
    // TractorRules.GetNextOrder → engine.GetNextOrder()
    // TractorRules.IsInvalid → engine.IsInvalid(who)
    // TractorRules.CalculateScore → engine.CalculateScore()
    // TractorRules.Calculate8CardsScore → engine.Calculate8CardsScore(howmany)
    // TractorRules.GetNextMasterUser → engine.GetNextMasterUser()
    // TractorRules.GetNextRank → engine.GetNextRank(success)
    // TractorRules.CheckSendCards → engine.CheckSendCards(minCards, who)
}
```

### 3.5 应该留在 Form 层的方法

```
MainForm (Form) {
    MainForm_Paint()                    // Paint 事件
    MainForm_MouseClick()               // 鼠标点击 → 调用 engine 的方法
    MainForm_MouseDoubleClick()         // 双击
    MenuItem_Click() 的 UI 部分         // 菜单事件 (选图、音乐、设置)
    timer_Tick() 的 UI 部分             // 调用 engine.tick()，根据结果做 UI 更新
    // DrawingFormHelper 的绘制方法全在 Form 层
    // CalculateRegionHelper 全在 Form 层
}
```

### 3.6 GameEngine ↔ Form 通信接口设计

建议使用 **事件 (Events)** 机制，而不是回调或直接方法调用。

```csharp
// GameEngine 事件定义
class GameEngine {
    // 状态变化事件
    event Action<CurrentState> StateChanged;          // 游戏状态变化
    event Action<int, int> CardsAdded;                // (playerId, count) 发牌
    event Action<int, ArrayList> CardsSended;         // (playerId, cards) 出牌
    event Action<int> WhoseOrderChanged;              // 轮到谁了
    event Action<int> ShowSuitChanged;                // 花色/亮牌变化
    event Action<int> ScoreChanged;                   // 得分更新
    event Action<int, int> RankChanged;               // (side, rank) 升级
    event Action<CardCommands> CommandChanged;         // 游戏命令变化
    event Action<string> Error;                       // 错误报告

    // 时长暂停 (Form 层的 Timer 需要等待)
    // Form 层调用 engine.IsPaused() / engine.PauseRemaining()
}
```

Form 层订阅这些事件并更新 UI:

```csharp
// Form 层
engine.StateChanged += (state) => {
    drawingFormHelper.UpdateMaster(state.Master);
    drawingFormHelper.UpdateSuit(state.Suit);
    // ...
};

engine.CardsSended += (player, cards) => {
    if (player == 0) drawingFormHelper.DrawMyFinishSendedCards();
    else if (player == 1) drawingFormHelper.DrawFrieldUserSendedCards();
    // ...
};
```

### 3.7 抽取依赖顺序 (分步策略)

#### 阶段 1: 接口定义与数据迁移 (不改变行为)

1. **创建 `GameEngine` 类**，将 MainForm 的游戏状态字段复制为 GameEngine 的字段
2. **定义事件接口** (StateChanged, CommandChanged 等)
3. **将 `GameConfig` 注入到 GameEngine** (目前 GameConfig 已独立)

#### 阶段 2: 算法类迁移 (核心风险阶段)

4. **将 Algorithm 静态方法改为 GameEngine 实例方法**
   - `Algorithm.ShouldSetRank(mainForm, user)` → `GameEngine.ShouldSetRank(user)`
   - 内部访问 `this.currentPokers` 替代 `mainForm.currentPokers`
   - 这一步要求所有静态方法不再引用 MainForm，而是引用 GameEngine

5. **将 TractorRules 静态方法改为 GameEngine 实例方法**
   - 同样地，`mainForm.xxx` → `this.xxx`

6. **将 MustSendCardsAlgorithm / ShouldSendedCardsAlgorithm 合并到 GameEngine**
   - 这些是出牌算法逻辑，本应属于引擎

#### 阶段 3: 事件化改造 (解耦)

7. **DrawingFormHelper 改造**: 
   - 不再持有 `MainForm` 引用，改为持有 `GameEngine` 引用
   - 所有 `mainForm.xxx` → `engine.xxx`
   - 但绘制方法仍保留在 DrawingFormHelper 中

8. **MainForm.timer_Tick 分离**:
   - 纯游戏逻辑部分 → `engine.Tick()`
   - UI 更新部分 → 留在 MainForm，通过返回值和事件驱动

9. **MainForm.MouseClick 分离**:
   - 调用 `engine.TrySendCards(player1, selectedCards)` 替代直接操作字段

#### 阶段 4: 验证与清理

10. **逐步替换调用点**:
    - 每次换一个方法后运行游戏验证
    - 先迁移"亮牌"逻辑 (ShouldSetRank, CanSetRank)
    - 再迁移"底牌"逻辑 (Send8Cards)
    - 再迁移"出牌"逻辑 (ShouldSendCards, MustSendCards)
    - 最后迁移"结算"逻辑 (TractorRules)

11. **删除 MainForm 中已迁移的字段声明**

---

## 4. 风险分析

### 4.1 最大风险点

| # | 风险 | 严重程度 | 说明 |
|---|---|---|---|
| R1 | **static 方法的 MainForm 参数** | 🔴 **致命** | Algorithm、TractorRules 等 4 个类共 20+ 个静态方法都接受 `MainForm` 参数，内部通过 `mainForm.xxx` 直接访问字段。这些方法分布在 4 个文件中，共约 3000+ 行代码 |
| R2 | **DrawingFormHelper 对游戏状态的间歇写入** | 🟠 **高** | DrawingFormHelper 既负责绘制，又负责修改游戏状态 (如 `whoseOrder`, `whoIsBigger`, `currentState.CurrentCardCommands`)。在 Architecture 分离时，需要区分"绘制方法"和"状态变更" |
| R3 | **Algorithm.ShouldSendedCards 同时访问 mainForm.currentAllSendPokers 和 mainForm.pokerList** | 🟠 **高** | 这两个字段在游戏循环中被同时读写。先调用 Algorithm 出牌，再更新 currentAllSendPokers。如果抽取出 GameEngine 后顺序不对，会导致 AI 插件拿到错误状态 |
| R4 | **TractorRules.GetNextMasterUser 多字段写入** | 🟠 **高** | 这个方法同时写入 currentState.Master、currentState.OurCurrentRank、currentState.OpposedCurrentRank、currentRank、Scores。一次调用改变 5 个字段，任何遗漏都是 bug |
| R5 | **MainForm.timer_Tick 中的游戏状态机** | 🟠 **高** | timer_Tick 通过 `currentState.CurrentCardCommands` 的 switch 驱动整个游戏流程。该方法引用 MainForm 的 20+ 个字段。拆分时不能破坏状态机顺序 |
| R6 | **DrawingFormHelper 中的出牌流程 (DrawMyFinishSendedCards / DrawNextUserSendedCards / DrawFrieldUserSendedCards / DrawPreviousUserSendedCards)** | 🟡 **中** | 这 4 个方法既渲染又调用 Algorithm 出牌算法、修改游戏状态、设置 `whoseOrder` 等。它们是绘制逻辑和游戏逻辑最深度的耦合点 |
| R7 | **卡牌校验与出牌规则 (TractorRules.IsInvalid)** | 🟡 **中** | 读取 `myCardIsReady` 和 `myCardsNumber` (UI 交互状态)，同时也读 `currentPokers` (游戏状态)。需要在 Form 层和 Engine 层之间传递选中状态 |
| R8 | **游戏存档 (RestoreToolStripMenuItem_Click)** | 🟡 **中** | 当前存档只序列化 `CurrentState`，恢复时通过 `init()` 重建整个游戏。如果引入 GameEngine，需要确保 init() 也能正确重建 Engine 状态 |

### 4.2 逐类依赖关系风险矩阵

| 类 | 大小 | MainForm 引用方式 | 耦合度 | 迁移难度 |
|---|---|---|---|---|
| DrawingFormHelper | ~2355 行 | 构造函数注入 `mainForm` | 🔴 **极高** — 读/写 25+ 字段 | 🟠 高 |
| CalculateRegionHelper | ~155 行 | 构造函数注入 `mainForm` | 🟢 **低** — 只读 3 个 UI 字段 | 🟢 低 |
| Algorithm | ~500 行 | 静态方法参数 `MainForm` | 🔴 **极高** — 5 个静态方法需改为实例方法 | 🔴 高 |
| TractorRules | ~500 行 | 静态方法参数 `MainForm` | 🔴 **极高** — 8 个静态方法涉及 15+ 字段 | 🔴 高 |
| MustSendCardsAlgorithm | ~2000 行 | 静态方法参数 `MainForm` | 🔴 **极高** — 大量访问 currentState/currentPokers | 🔴 高 |
| ShouldSendedCardsAlgorithm | ~500 行 | 静态方法参数 `MainForm` | 🟠 **高** — 较多读字段 | 🟠 中 |
| CommonMethods | ~300 行 | **无 MainForm 依赖** | 🟢 **无** → 可保持静态 | 🟢 可直接使用 |
| CurrentPoker | ~1000 行 | **无 MainForm 依赖** | 🟢 **无** → 纯数据模型 | 🟢 可直接使用 |
| GameConfig | ~120 行 | **无 MainForm 依赖** | 🟢 **无** → 纯配置类 | 🟢 可直接使用 |
| DistributePokerHelper | ~50 行 | **无 MainForm 依赖** | 🟢 **无** → 独立发牌器 | 🟢 可直接使用 |

### 4.3 分批验证策略

#### 每步验证的方法

每个迁移步骤完成后，按以下顺序验证:

1. **编译验证**: `dotnet build` 通过
2. **启动验证**: 程序能正常启动
3. **发牌验证**: 开始新游戏，25 轮发牌动画正常
4. **亮牌验证**: AI 自动亮牌逻辑正常 (切换 debug/非 debug 模式)
5. **底牌验证**: 底牌处理逻辑正常 (3 种底牌算法)
6. **出牌验证**: 一局游戏能正常打完
7. **结算验证**: 积分计算、升级逻辑正确

#### 推荐的迁移顺序 (最小化风险)

```
Step 1: 抽取 CommonMethods(不改) + CurrentPoker(不改) + GameConfig(不改)
        → 验证: 编译通过

Step 2: 创建 GameEngine 骨架，复制字段，建立事件
        → 验证: 编译通过，GameEngine 可以实例化

Step 3: 迁移 "亮牌" 逻辑
        → Algorithm.ShouldSetRank, ShouldSetRankAgain, CanSetRank
        → TractorRules 中亮牌相关
        → 验证: AI 亮牌、手动亮牌正常

Step 4: 迁移 "底牌" 逻辑
        → Algorithm.Send8Cards 系列
        → 验证: 3 种底牌算法都正常

Step 5: 迁移 "出牌" 逻辑 (最大风险!)
        → Algorithm.ShouldSendedCards, MustSendedCards
        → MustSendCardsAlgorithm (全部)
        → ShouldSendedCardsAlgorithm (全部)
        → 验证: 完整一局 AI vs AI 正常打完

Step 6: 迁移 "结算" 逻辑
        → TractorRules 全部
        → 验证: 积分计算、升级、换庄 逻辑正确

Step 7: 迁移 MainForm 的游戏状态机
        → timer_Tick 核心逻辑
        → 验证: 完整多局游戏正常

Step 8: 事件化 DrawingFormHelper
        → DrawingFormHelper 不再持有 MainForm，改为持有 GameEngine + 事件
        → 验证: 画面显示正常
```

---

## 5. 数据流汇总图 (文本形式)

```
+--MainForm(Form)------------------------------+
|                                              |
|  [Game State Fields]     [UI State Fields]   |
|  currentState            myCardsLocation     |
|  currentRank             myCardsNumber        |
|  isNew                   myCardIsReady         |
|  showSuits               cardsOrderNumber     |
|  whoShowRank             currentCount         |
|  Scores                  sleepTime            |
|  whoIsBigger             sleepMaxTime         |
|  firstSend               wakeupCardCommands   |
|  whoseOrder                                    |
|                         [Rendering]           |
|  [Card Data]             bmp                  |
|  dpoker                  image                |
|  pokerList               cardsImages          |
|  currentPokers[]                              |
|  currentSendCards[]      [Helper 引用]         |
|  currentAllSendPokers[]  drawingFormHelper    |
|  send8Cards              calculateRegionHelper |
|                                              |
|  [Config]  gameConfig      [AI] UserAlgorithms |
+--------+----------+----------------+---------+
         |          |                |
         ▼          ▼                ▼
+------------------+   +-----------------------+
| DrawingFormHelper|   | CalculateRegionHelper  |
| (构造注入MainForm)|   | (构造注入MainForm)      |
|                  |   |                       |
| 读20+字段         |   | 读: myCardsLocation    |
| 写 ~8字段         |   |     myCardIsReady      |
| 调用算法类         |   | 写: myCardIsReady      |
| 调用TractorRules  |   |                       |
+------------------+   +-----------------------+

  ▲                                 ▲
  │                                 │
  │    Algorithm 系列                │   TractorRules
  │    (静态方法，参数MainForm)       │   (静态方法，参数MainForm)
  │                                 │
  │  读15+字段                       │  读15+字段
  │  写: pokerList, send8Cards,     │  写: Scores,
  │      currentAllSendPokers,      │      currentState.Master,
  │      whoIsBigger                │      currentState.Rank,
  │                                 │      currentRank
  └─────────────────────────────────┘

               ┌──────────────────────┐
               │    CurrentPoker       │
               │    (纯数据模型)        │
               │    无 MainForm 依赖    │
               └──────────┬───────────┘
                          │
               ┌──────────▼───────────┐
               │   CommonMethods       │
               │    (无 MainForm 依赖)  │
               │   parse(), SendCards()│
               │   CompareTo(), 等等    │
               └──────────────────────┘
```

---

## 6. 核心结论

1. **耦合本质**: 整个游戏逻辑 (`Algorithm`, `TractorRules`, `MustSendCardsAlgorithm`, `ShouldSendedCardsAlgorithm`) 通过 `static` 方法 + `MainForm` 参数与 UI 紧耦合。这不是"MainForm 太大"，而是"游戏逻辑寄生在 MainForm 上"。

2. **重构难度**: 极高。涉及约 3000 行静态方法、4 个大类的改写。建议分 8 步走，每步完成后充分验证。

3. **最安全的第一步**: 先分离 `DrawingFormHelper` 对游戏状态的写入，将其改为通过事件/方法调用通知 GameEngine。因为 DrawingFormHelper 是单向依赖中最容易被事件化的部分。

4. **最快见效的一步**: 创建 `GameEngine` 骨架，将所有 MainForm 的状态字段引用过去。字段级别用 `GameEngine.State` 替代 MainForm.xxx。这不会改变行为，但为后续步骤建立基础。

5. **不要试图一步到位**。GameEngine 的抽取建议使用 "并行存在" 策略: 在 MainForm 中同时持有 GameEngine 引用，逐步将方法调用从 MainForm 迁移到 GameEngine，最终 MainForm 只剩下 UI 处理代码。
