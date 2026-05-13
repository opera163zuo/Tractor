# 渲染层拆解分析 — DrawingFormHelper (2355行)

**项目**: 四人拖拉机 (升级/80分) Windows 桌面游戏 (C# WinForms .NET 4.6.1)
**分析范围**: DrawingFormHelper.cs (`/tmp/tractor-analysis/Tractor.net/Helpers/DrawingFormHelper.cs`)
**分析日期**: 2026-05-12

---

## 概述

DrawingFormHelper 当前集成了三件事：
1. **渲染** — 画背景、画手牌、画出牌、画分数、画动画
2. **AI/规则调用** — 调用 Algorithm / TractorRules / MustSendCardsAlgorithm 驱动 AI 出牌
3. **游戏状态修改** — 直接修改 MainForm 的 whoseOrder, showSuits, currentState, Scores 等

本报告对 10 处 AI/规则调用 + 8 处游戏状态修改做逐行拆解，给出"渲染→GdiRenderer、逻辑→GameEngine"的完整拆分方案。

---

## A. 10 处 AI/规则调用 — 逐行拆解分析

---

### 调用 #1: DoRankOrNot — ShouldSetRank (行 572-608)

#### 位置
`DoRankOrNot(CurrentPoker currentPoker, int user)` 方法，起始行 ~560。

#### 调用细节
- **行 574**: `int suit = Algorithm.ShouldSetRank(mainForm, user);`
- **行 578-585**: 根据返回值修改状态（见 B1）

#### 依赖分析
- **读取**: `mainForm.currentState.Suit`, `mainForm.currentRank`, `mainForm.isNew`
- **修改**: `mainForm.showSuits`, `mainForm.whoShowRank`, `mainForm.currentState.Suit`, `mainForm.currentState.Master`
- **渲染**: 调用 DrawSuit, DrawRank, DrawMaster, DrawOtherMaster

#### 这一调用产生了什么结果
`Algorithm.ShouldSetRank` 返回一个花色 (1-5)，表示 AI 玩家（user=2/3/4）在当前发牌过程中应该亮出的花色。返回 0 表示不亮。

#### 这个结果现在被用于什么
1. 作为游戏状态写入 — `mainForm.showSuits = 1; mainForm.whoShowRank = user; mainForm.currentState.Suit = suit;`
2. 如果是新局且无人确定庄家 — `mainForm.currentState.Master = user`
3. 作为渲染数据 — 根据结果绘制亮牌标记和庄家标识

#### 拆分方案
```csharp
// Engine 提供
public class GameEngine {
    // 返回值封装亮牌决策+状态变更
    public SuitDecision? TryPlayerSetRank(int playerId);
}

public class SuitDecision {
    public int Suit;       // 1-5
    public bool IsNewMaster;  // 是否成为庄家
    public int NewMaster;     // 如果是，庄家是谁
}

// Renderer 监听事件
engine.OnRankSet += (playerId, suit, isNewMaster, master) => {
    renderer.DrawSuitDisplay(suit, master, showSuitsCount);
    if (isNewMaster) renderer.DrawDealerMark(master);
};
```

#### 风险点
- Engine 需要知道"当前是否应该自动亮牌"的逻辑（isNew / currentRank == 0 等条件），这些条件目前散落在 DrawingFormHelper 和 MainForm 中
- Renderer 不再触发亮牌逻辑，必须由 Engine 在发牌过程中自动调用 TryPlayerSetRank
- 渲染"秀牌"的卡片位置依赖于 whoShowRank，这个信息需要 Engine 通过事件传回

---

### 调用 #2: DoRankOrNot — ShouldSetRankAgain (行 618-675)

#### 位置
`DoRankOrNot` 方法的 `else` 分支（已有花色时），行 614-676。

#### 调用细节
- **行 618**: `int suit = Algorithm.ShouldSetRankAgain(mainForm, currentPoker);`

#### 依赖分析
- **读取**: `mainForm.currentState.Suit`, `mainForm.whoShowRank`, `mainForm.gameConfig.CanMyStrengthen`, `mainForm.gameConfig.CanMyRankAgain`
- **修改**: `mainForm.showSuits`, `mainForm.whoShowRank`, `mainForm.currentState.Suit`, `mainForm.currentState.Master`
- **渲染**: DrawSuit, DrawRank, DrawMaster, DrawOtherMaster

#### 这一调用产生了什么结果
返回花色 (1-5) 表示 AI 玩家在已有亮牌的情况下，决定再次亮牌（对抗/加强）。返回 0 表示不亮。

#### 这个结果现在被用于什么
1. 配置检查：如果花色相同但 `!CanMyStrengthen` 则跳过；如果花色不同但 `!CanMyRankAgain` 则跳过
2. 状态修改：showSuits=2, whoShowRank=user, currentState.Suit=suit
3. 如果是新局且无人确定庄家 — 设置庄家（老亮了直接被换庄）

#### 拆分方案
```csharp
// Engine 提供
public SuitDecision? TryPlayerSetRankAgain(int playerId);

// 事件相同，但区分首次亮牌和再次亮牌
engine.OnRankSetAgain += (playerId, suit, showSuitsCount, master) => {
    renderer.DrawSuitDisplay(suit, master, showSuitsCount);
};
```

#### 风险点
- `gameConfig.CanMyStrengthen` 和 `gameConfig.CanMyRankAgain` 的判定逻辑目前在 DrawingFormHelper（调用前做配置检查），应移入 Engine
- 保存 oldWhoShowRank / oldMaster 的逻辑需要被 Engine 接管
- showSuits 的计数器递增由 Engine 管理，渲染器只读

---

### 调用 #3: MyRankOrNot — CanSetRank (行 692-696)

#### 位置
`MyRankOrNot(CurrentPoker currentPoker)` 方法，行 690-696。

#### 调用细节
- **行 692**: `bool[] suits = Algorithm.CanSetRank(mainForm, currentPoker);`
- **行 694**: `ReDrawToolbar(suits);`

#### 依赖分析
- **读取**: `mainForm.currentRank`, `mainForm.currentPokers[0]`, `mainForm.showSuits`, `mainForm.currentState.Suit`, `mainForm.whoShowRank`, `mainForm.gameConfig`
- **修改**: 无直接修改（只用于渲染）
- **渲染**: ReDrawToolbar（画亮牌工具栏按钮的可用/不可用状态）

#### 这一调用产生了什么结果
返回一个 5 元素的 bool 数组，表示"玩家是否能亮/反亮每种花色"（第5个是无主）。

#### 这个结果现在被用于什么
只用于渲染 — 用来绘制 toolbar 上 5 个花色按钮的可用/不可用状态。

#### 拆分方案
```csharp
// Engine 提供
public bool[] GetAvailableSuits(int playerId);
// 这个方法是纯查询，没有副作用

// Renderer
var availableSuits = engine.GetAvailableSuits(0); // 0 = 玩家自己的下标
renderer.DrawToolbar(availableSuits);
```

#### 风险点
- 最低风险调用之一，因为它不写状态，只做查询
- 但 `Algorithm.CanSetRank` 内部读取了 `mainForm.currentPokers` 等字段，迁移后需从 Engine 获取
- 渲染调用了 Refresh，这个调用链需要保持

---

### 调用 #4: IsClickedRanked — CanSetRank (行 747)

#### 位置
`IsClickedRanked(MouseEventArgs e)` 方法，行 740-925。

#### 调用细节
- **行 747**: `bool[] suits = Algorithm.CanSetRank(mainForm, mainForm.currentPokers[0]);`
- **行 753-915**: 根据点击区域判定玩家选择了哪个花色，然后修改状态

#### 依赖分析
- **读取**: `mainForm.currentRank`, `mainForm.isNew`, `mainForm.currentState.Suit`, `mainForm.gameConfig`
- **修改**: `mainForm.showSuits`, `mainForm.whoShowRank`, `mainForm.currentState.Suit`, `mainForm.currentState.Master`
- **渲染**: DrawSuit, DrawRank, DrawMaster, DrawOtherMaster, ClearSuitCards

#### 这一调用产生了什么结果
返回可用花色的 bool 数组，供点击检测使用（哪个按钮可点击）。

#### 这个结果现在被用于什么
1. 筛选可用花色（只处理 suits[i]==true 的区域）
2. 点击后直接修改游戏状态（showSuits++, whoShowRank=1, currentState.Suit=i, currentState.Master=1）
3. 点击后立即渲染亮牌结果

#### 拆分方案
```csharp
// 用户点击事件在 Form 层处理
void MainForm_MouseClick(...) {
    var clickResult = renderer.HitTestToolbar(e.X, e.Y);
    if (clickResult != null) {
        // 通知 Engine 玩家尝试亮牌
        var decision = engine.TryPlayerSetSuit(0, clickResult.SuitIndex);
        // Engine 通过 OnRankSet 事件通知 Renderer 更新
    }
}
```

#### 风险点
- 用户的鼠标点击目前同时在 DrawingFormHelper 中处理"点击检测 + 状态修改 + 渲染"，需拆成三层
- 点击检测应该由 CalculateRegionHelper 或 Renderer 的 HitTest 方法负责
- Engine 不能直接接收鼠标坐标，需要 Form 层转换

---

### 调用 #5: DrawNextUserSendedCards — MustSendedCards + ShouldSendedCards (行 1740-1799)

#### 位置
`DrawNextUserSendedCards()` 方法，行 1738-1799。

#### 调用细节
- **行 1760**: `DrawNextUserSendedCardsAction(Algorithm.MustSendedCards(mainForm, 4, ...))`
- **行 1764**: `DrawNextUserSendedCardsAction(Algorithm.ShouldSendedCards(mainForm, 4, ...))`
- **行 1743**: `mainForm.whoseOrder = 4;`
- **行 1744**: `mainForm.currentState.CurrentCardCommands = CardCommands.WaitingForSend;`
- **行 1765**: `mainForm.whoseOrder = 2;`

#### 依赖分析
- **读取**: `mainForm.currentSendCards`, `mainForm.currentPokers`, `mainForm.currentState`, `mainForm.firstSend`
- **修改**: `mainForm.currentState.CurrentCardCommands`, `mainForm.whoseOrder`
- **渲染**: DrawNextUserSendedCardsAction（画下家出的牌）, DrawMyPlayingCards（重画自己手牌区域）, DrawScoreImage

#### 这一调用产生了什么结果
- `Algorithm.MustSendedCards` — 下家（player 4）是首出者，必须出牌。返回 ArrayList 表示出的牌
- `Algorithm.ShouldSendedCards` — 不是首出者，推荐出牌。返回 ArrayList 表示出的牌
- 一次调用完成"AI 决策→状态修改→渲染"全过程

#### 这个结果现在被用于什么
1. 传给 `DrawNextUserSendedCardsAction` 渲染下家出的牌卡片
2. 决定是否触发"一圈结束"流程（`currentSendCards[1].Count > 0` → Pause + DrawWhoWinThisTime）
3. 设置下一个出牌者（`whoseOrder = 2` 或 `whoseOrder = 2` 后等待）

#### 拆分方案
```csharp
// Engine 出牌
public class GameEngine {
    public AIPlayResult ProcessAITurn(int playerId);
}

public class AIPlayResult {
    public List<int> Cards;           // AI 出的牌
    public int NextWhoseOrder;       // 下一个出牌者
    public bool IsRoundFinished;     // 这一圈是否结束
    public int? RoundWinner;         // 如果结束，谁赢了
}

// Renderer 只画
engine.OnCardPlayed += (playerId, cards) => {
    renderer.DrawPlayedCards(playerId, cards);
    renderer.DrawPlayerHand(playerId, engine.GetPlayerCards(playerId));
};
```

#### 风险点
- **时序关键**: 目前 DrawingFormHelper 调用 Algorithm 后立即画牌。拆开后 Engine 返回后，Renderer 必须紧跟着画
- `currentSendCards[1].Count > 0` 的检查需要由 Engine 判断（是否所有玩家都出完了一轮）
- 暂停逻辑（SetPauseSet + Pause）现在在 DrawingFormHelper 中，应移到 Engine 的流程控制
- 暂停后通过 timer_Tick 恢复时，Engine 需要恢复正确的 next 动作

---

### 调用 #6: DrawFrieldUserSendedCards — MustSendedCards + ShouldSendedCards (行 1810-1863)

#### 位置
`DrawFrieldUserSendedCards()` 方法，行 1808-1863。

#### 调用细节
- **行 1814**: `DrawFrieldUserSendedCardsAction(Algorithm.MustSendedCards(mainForm, 2, ...))`
- **行 1818**: `DrawFrieldUserSendedCardsAction(Algorithm.ShouldSendedCards(mainForm, 2, ...))`
- **行 1798**: `mainForm.whoseOrder = 2;`
- **行 1799**: `mainForm.currentState.CurrentCardCommands = CardCommands.WaitingForSend;`

#### 依赖分析
- **读取**: 同上（对家 = player 2）
- **修改**: `currentState.CurrentCardCommands`, `whoseOrder`
- **渲染**: DrawFrieldUserSendedCardsAction, RedrawFrieldUserCardsAction, 重画已出牌区域

#### 拆分方案
与 #5 相同模式。`ProcessAITurn(2)` 替代 `Algorithm.MustSendedCards(mainForm, 2, ...)`。

#### 风险点
- 该方法额外重画了自己（player 1）和对家（player 2）的已出牌区域。Renderer 需要在这些玩家的区域变化时触发重绘
- 对家手牌区域用 `RedrawFrieldUserCardsAction` 重新绘制手牌张数，因为 AI 出牌后手牌减少了

---

### 调用 #7: DrawPreviousUserSendedCards — MustSendedCards + ShouldSendedCards (行 1874-1918)

#### 位置
`DrawPreviousUserSendedCards()` 方法，行 1872-1918。

#### 调用细节
- **行 1878**: `DrawPreviousUserSendedCardsAction(Algorithm.MustSendedCards(mainForm, 3, ...))`
- **行 1882**: `DrawPreviousUserSendedCardsAction(Algorithm.ShouldSendedCards(mainForm, 3, ...))`
- **行 1862**: `mainForm.whoseOrder = 3;`

#### 依赖分析
结构与前两个完全相同，只是角色是上家（player 3）。

#### 拆分方案
`ProcessAITurn(3)` — 同样模式。三处 AI 出牌调用是同一个流程的不同分支。

#### 风险点
- 上家手牌是竖直排列，渲染位置不同。Renderer 需要根据 playerId 计算不同位置
- 重绘自己（player 1）的已出牌区域的逻辑和三处一致，可抽取为 `DrawPlayerSendedCards(playerId, currentSendCards)`

---

### 调用 #8: DrawFinishedOnceSendedCards — MustSendCardsAlgorithm.WhoseOrderIs2/3/4 (行 1944-1991)

#### 位置
`DrawFinishedOnceSendedCards()` 方法，行 1928-2130。

#### 调用细节（Debug 模式修正逻辑）
- **行 1957**: `MustSendCardsAlgorithm.WhoseOrderIs2(mainForm, mainForm.currentPokers, users[0], mainForm.currentSendCards[users[0] - 1], 1, ...)`
- **行 1968**: `MustSendCardsAlgorithm.WhoseOrderIs3(mainForm, mainForm.currentPokers, users[1], mainForm.currentSendCards[users[1] - 1], 1, ...)`
- **行 1980**: `MustSendCardsAlgorithm.WhoseOrderIs4(mainForm, mainForm.currentPokers, users[2], mainForm.currentSendCards[users[2] - 1], 1, ...)`
- **行 1991**: `mainForm.whoseOrder = TractorRules.GetNextOrder(mainForm);`

#### 依赖分析
- **读取**: `mainForm.currentSendCards`, `mainForm.currentPokers`, `mainForm.firstSend`, `mainForm.currentState`
- **修改**: `pokerList[]`（恢复手牌）, `currentPokers[]`（恢复手牌）, `currentSendCards[]`（清空偏移玩家）, `whoseOrder`
- **此部分在 **`if (mainForm.gameConfig.IsDebug)` 条件内**，仅在 Debug 模式下执行修正逻辑

#### 这一调用产生了什么结果
`MustSendCardsAlgorithm.WhoseOrderIs2/3/4` 在 Debug 模式下检查是否所有玩家都正常出牌了相同数量的牌。如果某玩家少出了，算法会重新让该 AI 出牌。

#### 拆分方案
```csharp
// Engine 集成这个验证逻辑
public class GameEngine {
    // 在每圈结束时自动验证出牌数量一致性
    public void ValidateRoundCards();
}
```
Debug 模式下的修正逻辑是 Engine 的职责，Renderer 不应参与。

#### 风险点
- 这个调试修正逻辑修改了 `pokerList` 和 `currentPokers`，直接干扰了游戏状态。移到 Engine 后需要保证修正后的事件正确通知 Renderer
- 低风险：这只在 Debug 模式下运行，生产环境不触发

---

### 调用 #9: DrawFinishedOnceSendedCards — TractorRules.GetNextOrder (行 1991) + CalculateScore (行 2112)

#### 位置
`DrawFinishedOnceSendedCards()` 方法。

#### 调用细节
- **行 1991**: `mainForm.whoseOrder = TractorRules.GetNextOrder(mainForm);`
- **行 2112**: `TractorRules.CalculateScore(mainForm);`
- **行 2095**: `mainForm.whoIsBigger = 0;`
- **行 2098**: `mainForm.firstSend = mainForm.whoseOrder;`
- **行 2118-2122**: 清空 currentSendCards、绘制分数

#### 依赖分析
- **读取**: `mainForm.currentSendCards`, `mainForm.firstSend`, `mainForm.currentState`
- **修改**: `mainForm.whoseOrder`, `mainForm.whoIsBigger`, `mainForm.firstSend`, `mainForm.currentSendCards[]`, `mainForm.Scores`（通过 CalculateScore）
- **渲染**: DrawCenterImage, DrawScoreImage

#### 这一调用产生了什么结果
1. `TractorRules.GetNextOrder` — 计算这一圈谁赢了（谁最大）
2. `TractorRules.CalculateScore` — 如果赢的不是庄家方，计算分数

#### 这个结果现在被用于什么
1. `whoseOrder` 被重新赋值（下一圈的首出者 = 本圈赢家）
2. `whoIsBigger` 被清零
3. `firstSend` 被重新赋值
4. 分数被更新（Scores += 新分数）
5. 当前圈出牌被清空

#### 拆分方案
```csharp
// Engine 提供
public class GameEngine {
    public RoundResult FinishRound();
}

public class RoundResult {
    public int NextWhoseOrder;        // 下一圈谁先出
    public int NextFirstSend;         // 下圈首出者
    public int NewScore;              // 更新后的总分
    public bool IsGameFinished;       // 是否一局结束
}
// 调用方
var result = engine.FinishRound();
renderer.ClearCenter();
renderer.DrawScore(result.NewScore);
```

#### 风险点
- **最大风险点之一**: `TractorRules.GetNextOrder` 内部逻辑非常复杂，涉及花色比较、将吃判断、拖拉机大小判断等。迁移时必须保证行为完全一致
- 清空 currentSendCards 的时机：渲染清空画面和 Engine 清空数据必须同步
- `whoIsBigger` 的零清在 Engine 中完成，Renderer 不再关心

---

### 调用 #10: DrawFinishedSendedCards — TractorRules.GetNextMasterUser (行 2234)

#### 位置
`DrawFinishedSendedCards()` 方法，行 2230-2248。

#### 调用细节
- **行 2231**: `mainForm.isNew = false;`
- **行 2234**: `TractorRules.GetNextMasterUser(mainForm);`
- **行 2236-2239**: 清空 currentSendCards

#### 依赖分析
- **读取**: `mainForm.currentState`, `mainForm.Scores`, `mainForm.send8Cards`, `mainForm.gameConfig`
- **修改**: `mainForm.isNew`, `mainForm.currentState.Master`, `mainForm.currentState.OurCurrentRank`, `mainForm.currentState.OpposedCurrentRank`, `mainForm.currentRank`, `mainForm.Scores`
- **渲染**: DrawCenterImage, DrawFinishedScoreImage, SetPauseSet

#### 这一调用产生了什么结果
`TractorRules.GetNextMasterUser` 是升/降级+换庄的核心规则：
1. 计算底牌分数
2. 计算升级/降级规则（包括倒庄、升级级数）
3. 设置下一局的主牌花色、庄家、等级
4. 更新 Scores（底牌分数归入总分）

#### 这个结果现在被用于什么
1. 重绘中央区域
2. 绘制底牌（8张底牌展示）+ 总分显示
3. 设置 Pause，等待下一局开始

#### 拆分方案
```csharp
// Engine 提供
public class GameEngine {
    public GameEndResult FinishGame();
}

public class GameEndResult {
    public int NextMaster;
    public int NextRank;
    public int OurRank;
    public int OpposedRank;
    public int FinalScore;
    public List<int> BottomCards;      // 8张底牌
    public string Summary;            // "升2级"、"倒庄"等
}

// Renderer
var result = engine.FinishGame();
renderer.DrawBottomCards(result.BottomCards);
renderer.DrawScore(result.FinalScore);
// pause 计时器由 Engine 管理
```

#### 风险点
- `TractorRules.GetNextMasterUser` 是多字段写入的核心方法（写入 Master, OurCurrentRank, OpposedCurrentRank, currentRank, Scores）
- 如果事件顺序不对，Renderer 可能在错误的状态下绘制（例如 Master 已更新但 Scores 未同步）
- SetPauseSet 的暂停动画需要 Renderer 知道暂停时长和唤醒后的动作

---

### 额外调用: DrawMySortedCards — TractorRules.IsInvalid (行 1233)

#### 位置
`DrawMySortedCards` 方法，行 ~1233。

#### 调用细节
- **行 1233**: `if (TractorRules.IsInvalid(mainForm, mainForm.currentSendCards, 1) && (mainForm.currentState.CurrentCardCommands == CardCommands.WaitingForMySending))`

#### 依赖分析
- **读取**: `mainForm.currentSendCards`, `mainForm.currentState.CurrentCardCommands`, `mainForm.currentPokers`, `mainForm.firstSend`, `mainForm.myCardIsReady`, `mainForm.myCardsNumber`
- **修改**: 无（只读，用于渲染判断）
- **渲染**: 绘制 Ready 小图标（猪）

#### 这一调用产生了什么结果
返回 true/false，表示当前玩家选中的牌是否符合出牌规则。

#### 这个结果现在被用于什么
画一个"Ready"小图标表示出牌有效性。

#### 拆分方案
```csharp
// Engine 提供
public bool AreSelectedCardsValid(int playerId);
// 纯查询方法

// Renderer
if (engine.AreSelectedCardsValid(0)) {
    renderer.DrawReadyIndicator();
}
```

#### 风险点
- 最低风险。纯查询，无副作用
- `TractorRules.IsInvalid` 内部读取了 UI 交互数据 (`myCardIsReady`, `myCardsNumber`)，这些字段留在 Form 层

---

### 额外调用: DrawWhoWinThisTime — TractorRules.GetNextOrder (行 2132)

#### 位置
`DrawWhoWinThisTime()` 方法，行 2128-2160。

#### 调用细节
- **行 2132**: `int whoWin = TractorRules.GetNextOrder(mainForm);`

#### 依赖分析
- **读取**: `mainForm.currentSendCards`, `mainForm.currentState`, `mainForm.firstSend`
- **修改**: 无
- **渲染**: 画 Winner 图标

#### 这一调用产生了什么结果
返回当前圈的最大玩家 ID。

#### 这个结果现在被用于什么
画 Winner 标志（粉红色圆圈）在赢家旁边。

#### 拆分方案
```csharp
// Engine 的事件
engine.OnRoundWinnerChanged += (roundWinner) => {
    renderer.DrawWinnerIndicator(roundWinner);
};
```

#### 风险点
- DrawWhoWinThisTime 被 4 处调用（在 4 个 DrawXxxSendedCards 方法的 Pause 分支中）
- `TractorRules.GetNextOrder` 是一个重复调用，Engine 应该缓存此结果，避免每圈结束时重复计算

---

## B. 8 处游戏状态修改 — 逐行分析

---

### B1: DoRankOrNot — 亮牌状态 (行 578-585)

#### 位置
`DoRankOrNot` 方法，首亮分支。

#### 修改的字段
```csharp
mainForm.showSuits = 1;                    // 行 578
mainForm.whoShowRank = user;               // 行 579
mainForm.currentState.Suit = suit;         // 行 581
mainForm.currentState.Master = user;       // 行 585（条件：currentRank==0 && isNew）
```

#### 当前调用场景
Renderer 在发牌过程中调用算法判断 AI 是否亮牌，然后直接修改游戏全局状态。

#### 分离方案
```csharp
// Engine 在 ProcessDealCard 中自动调用
void GameEngine.ProcessDealCard(int cardIndex) {
    // 自动对每个 AI 玩家尝试亮牌
    for each player {
        var decision = TryPlayerSetRank(player);
        if (decision != null) {
            ApplySuitDecision(decision);
            OnRankSet?.Invoke(decision);  // 通知 Renderer
        }
    }
}
```

#### 修改前 vs 修改后

| 字段 | 修改前 | 修改后 |
|---|---|---|
| mainForm.showSuits | 0 | 1 |
| mainForm.whoShowRank | 0 | user (2/3/4) |
| currentState.Suit | 0 | suit (1-5) |
| currentState.Master | 0 | user (首次亮) |

---

### B2: DoRankOrNot — 对抗亮牌状态 (行 641-649)

#### 位置
`DoRankOrNot` 方法，反亮分支（ShouldSetRankAgain）。

#### 修改的字段
```csharp
mainForm.showSuits = 2;                    // 行 641
mainForm.whoShowRank = user;               // 行 642
mainForm.currentState.Suit = suit;         // 行 645
mainForm.currentState.Master = user;       // 行 649（条件：currentRank==0 && isNew）
```

#### 分离方案
Engine 接管反亮逻辑。Engine 必须保存 oldWhoShowRank / oldMaster（行 638-639）。

---

### B3: IsClickedRanked — 玩家亮牌 (行 754-897)

#### 位置
`IsClickedRanked` 方法中 5 个花色的分支（行 753、788、822、857、891）。

#### 修改的字段
```csharp
mainForm.showSuits++;                      // 每个分支递增
mainForm.whoShowRank = 1;                  // 玩家自己亮牌
mainForm.currentState.Suit = suitIndex;    // 选中的花色
mainForm.currentState.Master = 1;          // 玩家成为庄家
```

#### 分离方案
```csharp
// Form 层
void MainForm_MouseClick(...) {
    var hitSuit = renderer.HitTestToolbar(x, y);
    if (hitSuit.HasValue) {
        engine.TryPlayerSetSuit(0, hitSuit.Value);
        // engine 内部调用 ApplySuitDecision + 触发事件
    }
}
```

#### 风险点
- player 4 的亮牌也是通过 IsClickedRanked（`mainForm.currentPokers[0]` 是 player 1 的手牌信息）。但点击事件只来自 player 1（人类玩家）

---

### B4: DrawMyFinishSendedCards — 状态流转 (行 1716-1744)

#### 位置
`DrawMyFinishSendedCards()` 方法，行 ~1710-1744。

#### 修改的字段
```csharp
// 行 1735:
mainForm.currentState.CurrentCardCommands = CardCommands.Pause;
// 行 1743-1744:
mainForm.whoseOrder = 4;
mainForm.currentState.CurrentCardCommands = CardCommands.WaitingForSend;
```

#### 当前调用场景
玩家（Player 1）出牌后，Renderer 负责：
1. 画玩家出的牌（DrawMySendedCardsAction）
2. 更新 currentAllSendPokers
3. 如果一圈结束（`currentSendCards[3].Count > 0`），暂停
4. 如果一圈未结束，设置 whoseOrder=4, CurrentCardCommands=WaitingForSend

#### 分离方案
```csharp
// 玩家出牌 → Engine 处理
public class GameEngine {
    public PlayResult PlayerPlayCards(int playerId, List<int> cards);
}

// Engine 在 PlayerPlayCards 内部:
// 1. 验证出牌合法性
// 2. 更新 currentSendCards, currentAllSendPokers
// 3. 判断一圈是否结束
// 4. 设置或暂停
// 5. 触发 OnCardsPlayed 事件

// Renderer 只渲染
engine.OnCardsPlayed += (playerId, cards) => renderer.DrawPlayedCards(playerId, cards);
engine.OnHandUpdated += (playerId, cards) => renderer.DrawPlayerHand(playerId, cards);
engine.OnScoreChanged += (score) => renderer.DrawScore(score);
engine.OnRoundFinished += (winner) => renderer.DrawWinnerIndicator(winner);
```

---

### B5-B7: 三个 AI 出牌方法的相同模式 (行 1750-1918)

`DrawNextUserSendedCards`、`DrawFrieldUserSendedCards`、`DrawPreviousUserSendedCards` 共享完全相同的模式：

#### 修改的字段
```csharp
// 每个方法开始时:
mainForm.currentState.CurrentCardCommands = CardCommands.Undefined;

// AI 出牌后，如果一圈未结束:
mainForm.whoseOrder = <nextPlayer>;
mainForm.currentState.CurrentCardCommands = CardCommands.WaitingForSend;

// AI 出牌后，如果一圈结束:
mainForm.currentState.CurrentCardCommands = CardCommands.Pause;
mainForm.SetPauseSet(...);
// (Pause 结束后会调用 DrawFinishedOnceSendedCards)
```

#### 分离方案
三合一。Engine 处理所有 AI 出牌后的状态变更，Renderer 只渲染。

---

### B8: DrawFinishedOnceSendedCards — 圈结算状态 (行 1991-2122)

#### 位置
`DrawFinishedOnceSendedCards()` 方法。

#### 修改的字段
```csharp
mainForm.whoseOrder = TractorRules.GetNextOrder(mainForm);     // 行 1991
mainForm.whoIsBigger = 0;                                       // 行 2095
mainForm.firstSend = mainForm.whoseOrder;                       // 行 2098
// 条件:
TractorRules.CalculateScore(mainForm);                          // 行 2112
// 清空:
mainForm.currentSendCards[0] = new ArrayList();                  // 行 2118
mainForm.currentSendCards[1] = new ArrayList();
mainForm.currentSendCards[2] = new ArrayList();
mainForm.currentSendCards[3] = new ArrayList();
Scores (通过 CalculateScore 间接修改)
```

#### 分离方案
```csharp
var result = engine.FinishRound();
// Engine 内部:
// 1. 计算下一圈赢家 (GetNextOrder)
// 2. 计算分数 (CalculateScore) if needed
// 3. 清空 currentSendCards
// 4. 设置 whoseOrder, firstSend, whoIsBigger
// 5. 触发 OnRoundFinished 事件
```

#### 额外B9: DrawFinishedSendedCards — 一局结束状态 (行 2231-2248)

这不是要求的 8 处之一，但同等重要。

```csharp
mainForm.isNew = false;                                         // 行 2231
TractorRules.GetNextMasterUser(mainForm);                       // 行 2234
// 内部修改: currentState.Master, currentState.OurCurrentRank,
//           currentState.OpposedCurrentRank, currentRank, Scores
mainForm.currentSendCards[0..3] = new ArrayList();              // 行 2236-2239
```

---

## C. GdiRenderer 接口设计

重构后的 GdiRenderer 只做渲染，不再调用算法或修改游戏状态。

### 方法列表

```csharp
namespace Kuaff.Tractor.Rendering {

/// <summary>
/// 纯渲染接口。不调用任何游戏逻辑。
/// 所有状态数据通过参数/事件传入。
/// </summary>
class GdiRenderer {
    // 构造函数: 传入 Bitmap bmp, Image bgImage
    public GdiRenderer(Bitmap canvas, Image backgroundImage);

    // ===== 生命周期 =====
    public void ClearAll();
    public void DrawBackground();

    // ===== 中央区域 =====
    public void DrawCenterDeck(int cardCount);        // 画中央牌堆
    public void ClearCenter();                         // 清空中央区域
    public void DrawBottomCards(List<int> cards);     // 画底牌

    // ===== 玩家手牌 =====
    /// <summary>
    /// 绘制某玩家的手牌
    /// </summary>
    public void DrawPlayerHand(int playerId, CurrentPoker pokerData, 
                                bool showRankedEffect);

    /// <summary>
    /// 绘制某玩家已出的牌
    /// </summary>
    public void DrawPlayedCards(int playerId, List<int> cards);

    // ===== 亮牌/庄家 =====
    public void DrawSuitDisplay(int suit, bool isOurSide, bool colored);
    public void DrawRankDisplay(int rank, bool isOurSide, bool colored);
    public void DrawDealerMark(int masterPlayer);
    public void DrawOtherDealerMark(int masterPlayer);

    // ===== 工具栏 =====
    public void DrawToolbar(bool[] availableSuits);

    // ===== 分数与状态 =====
    public void DrawScore(int score);
    public void DrawWinnerIndicator(int playerId);
    public void DrawReadyIndicator(bool isValid);

    // ===== 动画 =====
    public void AnimateDealCard(int playerId, int cardNumber, 
                                int totalDealt);
    public void AnimateTakeBottomCards();

    // ===== 信息 =====
    public void DrawTestInfo(string playerName, int count, ...);

    // ===== 命中测试 (唯一与 UI 交互的方法) =====
    public int? HitTestToolbar(int x, int y);  // returns suit index
    public int HitTestCards(int x, int y);     // returns card index
}
```

### 渲染数据源 —— 不再是 MainForm 字段，而是参数

```csharp
// Renderer 不从 MainForm 读取任何数据。
// 所有数据通过方法参数或事件参数传入。
// 
// 示例: 不再有 mainForm.currentPokers[0] 内部读取
//       改为: renderer.DrawPlayerHand(0, playerPokerData, ...)
```

### 事件到绘制的映射

| Engine 事件 | Renderer 方法 | 参数 |
|---|---|---|
| OnDealProgress | AnimateDealCard | playerId, cardNumber, totalDealt |
| OnRankSet | DrawSuitDisplay + DrawDealerMark | suit, whoShowRank, master |
| OnCardsPlayed | DrawPlayedCards | playerId, cards |
| OnHandUpdated | DrawPlayerHand | playerId, pokerData |
| OnScoreChanged | DrawScore | newScore |
| OnRoundFinished | DrawWinnerIndicator + ClearCenter | winner, firstSend |
| OnGameFinished | DrawBottomCards + DrawScore | bottomCards, finalScore |
| OnHandInvalid | DrawReadyIndicator | isValid |

---

## 总结: 渲染与逻辑分离的执行建议

### 立即可以做的（低风险）
1. 创建 `GdiRenderer` 类，将 DrawingFormHelper 中**纯绘制方法**（DrawCenterAllCards, DrawMyCards, DrawSuitCards, DrawScoreImage, DrawMaster 等）迁移过去
2. 将纯查询（无副作用）的调用改为 Engine 查询方法：`AreSelectedCardsValid`, `GetAvailableSuits`
3. 将 SetCardsInformation（写 myCardsLocation/myCardsNumber/myCardIsReady）仍留在 Form 层

### 中风险（需并行测试）
1. `DoRankOrNot` 中的亮牌逻辑移到 Engine。绘制部分调用 Renderer
2. `IsClickedRanked` 拆分：HitTest → Engine.TryPlayerSetSuit → Renderer
3. `DrawWhoWinThisTime` 改为事件响应

### 高风险（需要完整状态机重构）
1. 四个 DrawXxxSendedCards 方法的彻底拆分：Engine 处理 AI 出牌和状态流转，Renderer 只画
2. `DrawFinishedOnceSendedCards` 的圈结算逻辑
3. `DrawFinishedSendedCards` 的局结算逻辑（GetNextMasterUser）

### 执行顺序（按代码行号）
```
Step 1: Renderer 抽取 [纯绘制] → 行 30-550 迁移
Step 2: Renderer 抽取 [手牌排序绘制] → 行 950-1700 迁移
Step 3: Engine 接入 [亮牌判断] → 行 550-930 拆分
Step 4: Engine 接入 [AI 出牌] → 行 1738-1918 拆分 (4个方法)
Step 5: Engine 接入 [圈结算] → 行 1928-2130 拆分
Step 6: Engine 接入 [局结算] → 行 2132-2248 拆分
Step 7: 清理 DrawingFormHelper 中不再需要的字段写入
```
