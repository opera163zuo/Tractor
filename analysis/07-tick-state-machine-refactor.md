# CardCommands 状态机重构分析：timer_Tick 职责拆分方案

**分析目标**: MainForm.timer_Tick（~250行, 行680-935），将混合的游戏逻辑/渲染/UI代码拆分为 Engine.Tick() + Renderer.Render() + Form 协调三层。

**状态流转图**:

```
ReadyCards (发28张) ──┐
    ↓ 25次后          │
DrawCenter8Cards ◄────┤ (no rank / pass)
    │                  │
    ├─有人叫主 → WaitingForSending8Cards → WaitingForSend → (循环) → DrawOnceFinished → DrawOnceFinished → ...
    ├─无人叫主+pass → WaitingShowPass ──────────┐
    └─无人叫主+!pass → WaitingShowBottom ────────┘
         ↑                                            │
         └─────── SetPauseSet ────────────────────────┘
```

---

## 1. 状态: ReadyCards (行697-718)

### 当前代码
```
if currentCount == 0:
    if (!IsDebug): drawingFormHelper.DrawToolbar()

if currentCount < 25:
    drawingFormHelper.ReadyCards(currentCount)
    currentCount++
else:
    currentState.CurrentCardCommands = CardCommands.DrawCenter8Cards
```

### ReadyCards 内部做的事情（在 DrawingFormHelper.ReadyCards 方法中，行38-99）
1. 擦除中间区域，重画58-count*2张背面牌
2. 将当前轮发的4张牌（每人各一张）写入 `currentPokers[0..3]`
3. 为自己画正面牌（正面 -> 牌桌底部区域）
4. 为其他三人画背面牌（各自区域）
5. 自动叫主逻辑（如果 AI 算法决定叫主，则修改 `currentState.Suit`, `currentState.Master` 等）
6. 玩家叫主 UI（如果没开启调试模式，绘制可点击的花色选择工具栏）

### 涉及字段
- **读取**: `currentCount`, `gameConfig.IsDebug`, `pokerList[0..3][count]`, `currentState.Suit/Master/OurCurrentRank/OpposedCurrentRank`
- **写入**: `currentCount++`, `currentPokers[0..3]`（添加新牌）, `currentState.Suit`（可能被自动叫主修改）, `currentState.Master`（可能被自动叫主修改）, `showSuits`, `whoShowRank`, `bmp`（绘制）
- **渲染**: `bmp` 上的全部绘制（DrawToolbar, DrawCenterAllCards, DrawAnimatedCard, DrawMyCards 等）

### 渲染/逻辑分离方案

**Engine.Tick() 应该**:
1. 维护 `_dealCount`（当前发到第几张，0..24）
2. 第0次tick时，如果 `!IsDebug`，返回 `RenderCmd.ShowToolbar`
3. 每次 tick:
   - 分配牌给四个玩家: `pokerList[i][_dealCount]` → `currentPokers[i]`
   - 运行自动叫主逻辑（判断是否需要叫主，更新 Suit/Master）
   - 增加 `_dealCount`
4. 如果 `_dealCount >= 25`: 设置 `nextCommand = DrawCenter8Cards`
5. 返回 `RenderCmd.DealCard(round: _dealCount-1, cards: [list of 4 cards])`

**Renderer 应该**:
1. 接到 `RenderCmd.ShowToolbar` → 绘制花色选择工具栏
2. 接到 `RenderCmd.DealCard(round, cards)` → 
   - 绘制背面牌堆（58 - round*2 张背面）
   - 玩家0: 正面牌动画 + 已收牌整理
   - 玩家1/2/3: 背面牌动画 + 各自区域
   - 如果叫主状态已更新 → 绘制叫主指示

**Form 应该**:
1. `timer_Tick`: 仅 `var cmd = engine.Tick(); renderer.Render(cmd);`
2. MouseClick（叫主按钮）改为调用 Engine 的叫主方法

### 风险
- **时序耦合**: `ReadyCards` 同时写 `currentPokers` 和画图。拆分后 Engine 先写入 game state，Renderer 后读取渲染。必须确保 Renderer 在 Engine 完成后再访问 game state。
- **动画触发**: 当前的 `DrawAnimatedCard` 和 `mainForm.Refresh()` 混在 ReadyCards 中。拆分后需要 Renderer 自己管理动画调度（或者 Engine 返回动画指令）。
- **自动叫主的绘图**: 自动叫主的逻辑在 ReadyCards 内部，且同时进行绘图（DrawSuit/DrawRank/DrawMaster）。拆分后 Engine 只返回 "玩家N叫了花色M", Renderer 据此绘图。

---

## 2. 状态: WaitingShowBottom (行720-733)

### 当前代码
```
drawingFormHelper.DrawCenterImage()  // 清空中央区域

Graphics g = Graphics.FromImage(bmp)
for i in 0..7:
    g.DrawImage(gameConfig.BackImage, 200+i*2, 186, 71, 96)  // 画8张底牌背面
g.Dispose()

SetPauseSet(gameConfig.Get8CardsTime, CardCommands.DrawCenter8Cards)
```

### 涉及字段
- **读取**: `bmp`, `gameConfig.BackImage`, `gameConfig.Get8CardsTime`
- **写入**: `bmp`（绘制）, `sleepMaxTime`, `sleepTime`, `wakeupCardCommands`, `currentState.CurrentCardCommands` → `Pause`

### 渲染/逻辑分离方案

这是一个纯视觉效果过渡的状态。玩家叫完主后，显示8张底牌背面，短暂停顿后进入 DrawCenter8Cards。

**Engine.Tick() 应该**:
1. 什么也不做（这是纯渲染状态）
2. 返回 `RenderCmd.ShowBottomCardsBack`（无逻辑变化）

**Renderer 应该**:
1. 接到 `RenderCmd.ShowBottomCardsBack` → 清空中央、画8张背面牌

### 风险
- 低风险。此状态不涉及 game state 修改，只做视觉展示。

---

## 3. 状态: DrawCenter8Cards (行735-799)

**最复杂的状态，3条子路径：**

### 子路径 A: 无人叫主 (DoRankNot() == true)

#### 当前代码（行739-749，pass场景）
```
if (gameConfig.IsPass):
    init()
    isNew = false
    drawingFormHelper.DrawPassImage()
    SetPauseSet(gameConfig.NoRankPauseTime, CardCommands.WaitingShowPass)
    return
```

**DoRankNot() 的真实含义**: `currentState.Suit == 0`（尚未确定主花色）

#### 当前代码（行752-785，非pass场景）
```
// 构建 bottom（8张底牌）
ArrayList bottom = new ArrayList()
bottom.Add(pokerList[0][0..1])
bottom.Add(pokerList[1][0..1])
bottom.Add(pokerList[2][0..1])
bottom.Add(pokerList[3][0..1])

int suit = CommonMethods.GetSuit(bottom[2])
currentState.Suit = suit

// 画主花色标记
Graphics g = Graphics.FromImage(bmp)
if Master == 1 or 2: drawingFormHelper.DrawSuit(g, suit, true, true)
else: drawingFormHelper.DrawSuit(g, suit, false, true)

drawingFormHelper.DrawCenterImage()
drawingFormHelper.DrawBottomCards(bottom)

SetPauseSet(gameConfig.NoRankPauseTime, CardCommands.WaitingShowBottom)
return
```

### 子路径 B: 有人叫主 (DoRankNot() == false)

#### 当前代码（行789-799）
```
whoseOrder = currentState.Master
firstSend = whoseOrder

SetPauseSet(gameConfig.Get8CardsTime, CardCommands.DrawMySortedCards)

drawingFormHelper.DrawCenter8Cards()  // 动画：将中间8张底牌移入庄家手牌
initSendedCards()                      // 重新解析各玩家手牌（按主花色排序）
drawingFormHelper.DrawMySortedCards(currentPokers[0], currentPokers[0].Count)

currentState.CurrentCardCommands = CardCommands.WaitingForSending8Cards

drawingFormHelper.DrawScoreImage(0)   // 绘制计分板
```

### 涉及字段
- **读取**: `currentState.Suit/Master`, `pokerList[*][*]`, `gameConfig.IsPass/NoRankPauseTime/Get8CardsTime/BackImage`
- **写入**: `currentState.Suit`（无人叫主时自动设置）, `currentState.CurrentCardCommands`, `whoseOrder`（路径B）, `firstSend`（路径B）, `currentPokers[0..3]`（路径B庄家获得8张底牌）, `isNew`（路径A）, `bmp`

### 渲染/逻辑分离方案

**Engine.Tick() 应该（选择子路径后决定返回）**:

**子路径 A（无人叫主）:**
- 如果 pass: 设置 `currentState = new CurrentState(..., ReadyCards)`，返回 `RenderCmd.PassGame`
- 如果 !pass: 
  - 构建 `bottom` 数组（取每家的前2张作为底牌）
  - 计算主花色: `suit = GetSuit(bottom[2])`
  - 设置 `currentState.Suit = suit`
  - 返回 `RenderCmd.NoRankAutoAssign { Suit: suit, Bottom: bottom }`

**子路径 B（有人叫主）:**
- 设置 `whoseOrder = currentState.Master`, `firstSend = whoseOrder`
- 将8张底牌移入庄家的 `currentPokers[master-1]`（调用 Get8Cards）
- 执行 `initSendedCards()`（重新解析手牌）
- 设置 `nextCommand = WaitingForSending8Cards`
- 返回 `RenderCmd.CollectCenterCards { Master: master }`

**Renderer 应该**:
- `RenderCmd.PassGame` → 显示 Pass 图片
- `RenderCmd.NoRankAutoAssign { Suit, Bottom }` → 
  1. 画主花色标记
  2. 清空中央
  3. 展开8张底牌正面（DrawBottomCards）
- `RenderCmd.CollectCenterCards { Master }` →
  1. 底牌移入庄家动画（DrawCenter8Cards）
  2. 绘制庄家已排序手牌（DrawMySortedCards）
  3. 绘制计分板（DrawScoreImage）

### 风险
- **数据结构耦合**: `DrawCenter8Cards` 内部的 `Get8Cards()` 方法 **同时修改了所有4个玩家的 pokerList**（将底牌添加到庄家并移除其他三家）。这不是纯渲染。Engine 必须完全接管这个数据变更。
- **逻辑依赖**: `initSendedCards()` 被调用后才能正确显示排序后的手牌。Engine 必须确保在返回渲染指令之前完成 `initSendedCards()`。
- **WhoShowRank 一致性**: 自动叫主场景下，`DoRankOrNot` 可能在 ReadyCards 阶段就已经修改了 `whoShowRank` 和 `showSuits`。Engine 和 Renderer 需要共享这些状态。

---

## 4. 状态: WaitingShowPass (行806-812)

### 当前代码
```
drawingFormHelper.DrawCenterImage()  // 清空中央
Refresh()
currentState.CurrentCardCommands = CardCommands.ReadyCards
```

### 涉及字段
- **读取**: `bmp`
- **写入**: `bmp`（清空绘图）, `currentState.CurrentCardCommands` → `ReadyCards`

### 渲染/逻辑分离方案

这是一个纯过渡状态。显示完 Pass 画面后清空，回到发牌状态。

**Engine.Tick() 应该**:
1. 什么也不做
2. 返回 `RenderCmd.ClearCenter` + 设置 `nextCommand = ReadyCards`

**Renderer 应该**:
1. 接到 `RenderCmd.ClearCenter` → 清空中央区域

### 风险
- 无风险。纯过渡状态。

---

## 5. 状态: WaitingForSending8Cards (行847-866)

### 当前代码
```
switch (currentState.Master):
    case 1 (我):
        if (IsDebug):
            Algorithm.Send8Cards(this, 1)
        else:
            drawingFormHelper.DrawMyPlayingCards(currentPokers[0])
            Refresh()
            return  // 等待鼠标签
    case 2, 3, 4 (AI):
        Algorithm.Send8Cards(this, N)
```

**注意**: 玩家的扣牌在 MouseClick 事件里处理（MainForm.cs 行415-609），不是在 timer_Tick 里。

### 涉及字段
- **读取**: `currentState.Master`, `gameConfig.IsDebug`
- **写入**: `currentPokers[0..3]`（通过 Algorithm.Send8Cards 移除扣掉的8张）, `send8Cards`, `myCardsLocation`, `myCardsNumber`, `myCardIsReady`, `bmp`

### 渲染/逻辑分离方案

**Engine.Tick() 应该**:
- 如果 `Master == 1 && !IsDebug`（人类玩家扣牌）:
  1. 返回 `RenderCmd.WaitForPlayerDiscard`（不做逻辑，等 MouseClick）
  2. 不改变状态
- 如果 `Master != 1 || IsDebug`（AI扣牌）:
  1. 调用 `Algorithm.Send8Cards()` — **这修改了 `currentPokers` 和 `send8Cards`**
  2. 如果需要，在其他 DrawMySortedCards 分支后自动转换
  
**注意**: `Algorithm.Send8Cards` 内部会调用 `form.currentState.CurrentCardCommands = CardCommands.DrawMySortedCards`，所以这里 Engine 需要把这个逻辑和状态流转也纳入。

### 风险
- **算法强耦合**: `Algorithm.Send8Cards` 直接写 `currentPokers` 并且修改 `currentState.CurrentCardCommands`。拆分后算法应该返回决策指令而不是直接修改状态。
- **玩家交互**: 扣牌状态的切换核心在 MouseClick 事件（人类玩家选完8张牌后，MouseClick 调用 `initSendedCards()` + 设置 `currentState.CurrentCardCommands = DrawMySortedCards`）。Engine 需要提供接口让 MouseClick 通知 Engine "玩家已完成扣牌"。

---

## 6. 状态: DrawMySortedCards (行849-859)

### 当前代码
```
SetPauseSet(gameConfig.SortCardsTime, CardCommands.DrawMySortedCards)
drawingFormHelper.DrawMySortedCards(currentPokers[0], currentPokers[0].Count)
Refresh()
currentState.CurrentCardCommands = CardCommands.WaitingForSend
```

### 涉及字段
- **读取**: `gameConfig.SortCardsTime`, `currentPokers[0]`
- **写入**: `bmp`（绘制）, `sleepMaxTime`, `sleepTime`, `wakeupCardCommands`, `currentState.CurrentCardCommands` → `Pause`

### 渲染/逻辑分离方案

**注意**: 这里有个奇怪的设计 — `SetPauseSet` 设置了唤醒命令为 `DrawMySortedCards`，但实际上在调用 SetPauseSet 后，下面又覆盖了 `currentState.CurrentCardCommands = WaitingForSend`。所以 SetPauseSet 实际上会立即让状态进入 Pause，然后在 Pause 结束后进入 DrawMySortedCards，但下面的赋值又不生效了... 需要确认一下。

实际执行流程：`SetPauseSet` 将状态改为 Pause，设置 `wakeupCardCommands = DrawMySortedCards`。然后下面的 `currentState.CurrentCardCommands = WaitingForSend` 实际上被覆盖了不会执行。所以这是 bug 还是有意为之？

但从行号803-805可以看到，SetPauseSet 会设置 Pause，然后下一个 tick 进入 Pause 分支。所以 `SortCardsTime` 结束后回到 DrawMySortedCards，然后再次 SetPauseSet + WaitingForSend... 看起来是一个无限循环。或者是故意的多次排序刷新动画？

考虑到这个设计可能是有问题的，改造时建议统一语义。

**假设预期行为**: 显示排序后的手牌，暂停 sortCardsTime 毫秒，然后进入 WaitingForSend。

**Engine.Tick() 应该**:
1. 什么也不做（纯渲染暂停）
2. 返回 `RenderCmd.ShowSortedCards { PlayerCards: currentPokers[0] }` 并设置 Pause 定时器

**Renderer 应该**:
1. 接到 `RenderCmd.ShowSortedCards` → 调用 `DrawMySortedCards`

### 风险
- **原始代码可能有无限循环 bug**: 上面分析到 SetPauseSet 和下面的状态赋值冲突。改造时建议清理这个逻辑。

---

## 7. 状态: WaitingForSend (行861-902)

**有 5 个子路径：**

### 子路径 whoseOrder == 2 (对家)
```
drawingFormHelper.DrawFrieldUserSendedCards()
```
**注意**: DrawFrieldUserSendedCards 内部调用 `Algorithm.ShouldSendedCards` 或 `Algorithm.MustSendedCards`，**并且**修改 `currentState.CurrentCardCommands`。

### 子路径 whoseOrder == 3 (上家)
```
drawingFormHelper.DrawPreviousUserSendedCards()
```
同上，内部调用算法并修改状态。

### 子路径 whoseOrder == 4 (下家)
```
drawingFormHelper.DrawNextUserSendedCards()
```
同上。

### 子路径 whoseOrder == 1 + IsDebug
```
Algorithm.ShouldSendedCards() 或 Algorithm.MustSendedCards()
drawingFormHelper.DrawMyFinishSendedCards()
```
**注意**: DrawMyFinishSendedCards 内部会修改 `currentState.CurrentCardCommands` 和 `whoseOrder`。

### 子路径 whoseOrder == 1 + !IsDebug
```
currentState.CurrentCardCommands = CardCommands.WaitingForMySending  // 等待鼠标事件
```

### 深层问题: 副作用隐藏

`DrawFrieldUserSendedCards()`, `DrawPreviousUserSendedCards()`, `DrawNextUserSendedCards()`, `DrawMyFinishSendedCards()` 这四个方法**每个都同时做了**：
1. 调用算法决定出牌
2. 修改 `currentPokers`（从手牌中移除出的牌）
3. 将牌添加到 `currentAllSendPokers`
4. 修改 `currentState.CurrentCardCommands`
5. 修改 `whoseOrder`
6. 绘制所有渲染

### 涉及字段
- **读取**: `whoseOrder`, `firstSend`, `currentPokers[*]`, `currentSendCards[*]`, `gameConfig.IsDebug`
- **写入**: `currentSendCards[*]`（通过算法）, `currentPokers[*]`（移除出的牌）, `currentAllSendPokers[*]`（记录出过的牌）, `currentState.CurrentCardCommands`, `whoseOrder`, `bmp`

### 渲染/逻辑分离方案

**Engine.Tick() 应该**:
1. 根据 `whoseOrder` + 是否是 AI 决定行为
2. 对 AI 玩家（whoseOrder != 1）: 
   - 调用 `Algorithm.ShouldSendedCards/MustSendedCards` 
   - 更新 `currentPokers[user-1]`, `currentSendCards[user-1]`, `currentAllSendPokers[user-1]`
   - 更新 `whoseOrder`（到下一个玩家）
   - 判断是否一轮结束（`currentSendCards[3].Count > 0` 这种检查）
   - 返回渲染指令
3. 对调试模式的玩家1: 同上
4. 对非调试模式的玩家1: 返回 `RenderCmd.WaitForPlayerAction`

**关键问题**: 当前 AI 出牌的副作用（修改 `currentPokers`, `whoseOrder`, 判断胜负）全部嵌在 DrawingFormHelper 的 "Draw" 命名方法中。**这些方法名称全是误导，它们执行的是游戏逻辑而非纯渲染。**

改造步骤建议:
1. 将 `DrawFrieldUserSendedCards`, `DrawPreviousUserSendedCards`, `DrawNextUserSendedCards`, `DrawMyFinishSendedCards` 中的**逻辑部分提取到 Engine**
2. `DrawMyFinishSendedCards` 的逻辑: 记录出牌、判断是否一轮结束、赋值 `whoseOrder`
3. 胜负判断逻辑也应归 Engine

**Renderer 应该**:
1. 纯按 `whoseOrder` 和 `currentSendCards` 渲染各玩家打出的牌
2. 渲染计分板
3. 渲染胜负指示

### 风险
- **最高风险状态**: 游戏逻辑、AI决策、状态流转、渲染全部深度耦合。每个 "Draw" 方法都是函数内联的游戏引擎。
- **胜负判断藏在渲染层**: `DrawFrieldUserSendedCards` 等内部通过检查 `currentSendCards` 大小来判断一轮是否结束，这个逻辑必须提到 Engine。
- **状态机流转被 Draw 方法内部覆盖**: 当前代码在 Draw 方法内部修改 `currentState.CurrentCardCommands` 后立即 return。拆分后 Engine 必须接管所有状态变更，然后 Renderer 只读渲染。

---

## 8. 状态: Pause (行904-910)

### 当前代码
```
long interval = (DateTime.Now.Ticks - sleepTime) / 10000
if (interval > sleepMaxTime):
    currentState.CurrentCardCommands = wakeupCardCommands
```

### 涉及字段
- **读取**: `sleepTime`, `sleepMaxTime`
- **写入**: `currentState.CurrentCardCommands` → `wakeupCardCommands`

### 渲染/逻辑分离方案

Pause 是一个忙等轮询，每100ms检查一次是否到达设定的暂停时间。

**改造建议: 替换为基于 Timer 的事件**:
- 在 Engine（或 Form）中设置一个 `System.Threading.Timer` 或使用 `Task.Delay` 的事件
- 或者直接使用 Windows.Forms.Timer 把定时到期当作一个事件处理
- 这样就不需要每100ms轮询了

如果必须保留轮询模式:
- Engine 检查间隔 → 返回 `RenderCmd.None`（不渲染）
- 或者 Engine 返回空指令，表示无需渲染

**更好的方案**:
- 在 Form 层用 `Task.Delay(sleepMaxTime).ContinueWith(...)` 触发下一个状态
- Pause 状态直接变成 `sleepMaxTime` 后被自动调入下一个状态
- 避免每100ms浪费一次Tick

### 风险
- **性能浪费**: 每100ms做一次无意义的 tick。虽然不影响游戏体验，但无端增加CPU使用。
- **时间精度足够**: 100ms 的 timer 配合 ms 级检查足够精确了。

---

## 9. 状态: DrawOnceFinished (行916-922)

### 当前代码
```
drawingFormHelper.DrawFinishedOnceSendedCards()  // 判断谁赢得本轮

if (currentPokers[0].Count > 0):
    currentState.CurrentCardCommands = CardCommands.WaitingForSend
```

**DrawFinishedOnceSendedCards 内部的复杂逻辑**（行1925-...）:
1. 如果自己的手牌没了（`currentPokers[0].Count == 0`），调用 `DrawFinishedSendedCards()` — 这是整盘游戏结束逻辑
2. 计算本轮谁赢了（`whoIsBigger`）
3. 将打出的牌记录到胜者的 `currentAllSendPokers`
4. 更新 `Scores`
5. 将胜者设为下一轮的 `whoseOrder` 和 `firstSend`
6. 重新绘制各玩家手牌、输赢标记、计分板
7. 设置下一状态为 Pause（短暂暂停后回 WaitingForSend）或 Undefined（结束）

### 涉及字段
- **读取**: `currentPokers[0..3].Count`, `currentSendCards[*]`, `currentAllSendPokers[*]`, `firstSend`, `currentState.Suit/Rank/Master`
- **写入**: `whoIsBigger`, `whoseOrder`, `firstSend`, `Scores`, `currentAllSendPokers[winner-1]`, `currentState.CurrentCardCommands`, `currentState.OurCurrentRank`, `currentState.OpposedCurrentRank`（可能）, `bmp`

### 渲染/逻辑分离方案

**Engine.Tick() 应该**:
1. 判断本轮胜负（计算谁赢得本轮）
2. 更新得分（`Scores` 和 `currentAllSendPokers`）
3. 更新 `whoseOrder` = 胜者
4. 更新 `firstSend` = 胜者
5. 检查升/跳级逻辑（`OurCurrentRank` 是否增加）
6. 检查游戏是否结束（某方手牌数为0 → `DrawFinishedSendedCards` → 可能晋级/获胜）
7. 返回完整渲染指令

**Renderer 应该**:
1. 根据 Engine 的胜负判定，绘制胜负标记
2. 绘制各玩家的手牌变化
3. 绘制计分板更新
4. 如果是游戏结束，绘制结束画面

### 风险
- **胜负逻辑藏在渲染层**: 和 WaitingForSend 一样，`DrawFinishedOnceSendedCards` 的"Draw"前缀严重误导。这是核心游戏逻辑。
- **游戏结束判定**: `currentPokers[0].Count == 0` 只是检查自己（玩家）的手牌。理论上只需要检查玩家自己的？但应该检查所有四人？需要确认原逻辑是否完整。
- **多局制循环**: `DrawFinishedSendedCards` 内部可能触发晋级/跳级 → 重开的流程，这个逻辑也必须移植到 Engine。

---

## 10. 状态: DrawOnceRank (行924-926)

### 当前代码
```
currentState.CurrentCardCommands = CardCommands.Undefined
init()
```

### 涉及字段
- **写入**: `currentState.CurrentCardCommands` → `Undefined`
- **调用**: `init()` 重置所有游戏状态

### 渲染/逻辑分离方案

**Engine.Tick() 应该**:
1. 返回 `RenderCmd.GameOver` 
2. 清理当前游戏状态（同 init() 做的工作）
3. 设置 `nextCommand = ReadyCards`

**Renderer 应该**:
1. 接到 `RenderCmd.GameOver` → 调用渲染层的清理（绘制背景、sidebar、rank等）

### 风险
- `init()` 同时做了数据初始化（分配牌、清空字段）和绘制初始化（DrawBackground）。拆分后 Engine 只做数据初始化，Renderer 做绘图初始化。

---

## 重构后的 timer_Tick 伪代码

```csharp
// 新的 timer_Tick: 只做协调
private void timer_Tick(object sender, EventArgs e) {
    // 1. 音乐播放 — 与 Engine 无关，保留在 Form
    HandleMusic();

    // 2. Pause 计时检查 — 保留下落式方案或替换为事件驱动
    if (_currentCommandType == RenderCmdType.Pause) {
        if (PauseTimer.HasElapsed()) {
            engine.ResumeAfterPause();
            _currentCommandType = RenderCmdType.None;
        } else {
            return;  // Pause 期间不做任何事
        }
    }

    // 3. 委托 Engine 做游戏逻辑
    var cmd = engine.Tick();

    // 4. 按指令让 Renderer 渲染
    if (cmd == null) return;

    switch (cmd.Type) {
        case RenderCmdType.ShowToolbar:
            renderer.DrawToolbar();
            break;

        case RenderCmdType.DealCard:
            renderer.DrawDealCard(
                round: cmd.Data.Round,
                cards: cmd.Data.Cards,            // 4张牌
                playerCounts: cmd.Data.PlayerCounts,
                suit: cmd.Data.Suit,
                master: cmd.Data.Master
            );
            break;

        case RenderCmdType.ShowPass:
            renderer.DrawPassImage();
            break;

        case RenderCmdType.ClearCenter:
            renderer.DrawCenterImage();
            break;

        case RenderCmdType.NoRankAutoAssign:
            renderer.DrawNoRankSuit(cmd.Data.Suit, cmd.Data.IsOurSide);
            renderer.DrawBottomCards(cmd.Data.Bottom);
            renderer.DrawCenterImage();
            break;

        case RenderCmdType.CollectCenterCards:
            renderer.AnimateCollectCenterCards(cmd.Data.Master);
            renderer.DrawMySortedCards(engine.MyCards);
            renderer.DrawScoreImage(engine.Scores);
            break;

        case RenderCmdType.WaitForPlayerDiscard:
            renderer.DrawMyPlayingCards(engine.MyCards);
            // Engine 进入 "等待玩家扣牌" 状态
            break;

        case RenderCmdType.ShowSortedCards:
            renderer.DrawMySortedCards(engine.MyCards, engine.MyCards.Count);
            break;

        case RenderCmdType.AiPlayCard:
            renderer.DrawAiPlayCard(
                userId: cmd.Data.UserId,
                cards: cmd.Data.Cards
            );
            break;

        case RenderCmdType.PlayerFinishedPlay:
            renderer.DrawMyFinishSendedCards(engine.MyCards);
            // 重绘手牌区域
            if (engine.MyCards.Count == 0) {
                renderer.DrawEmptyMyCardsRegion();
            }
            renderer.DrawScoreImage(engine.Scores);
            break;

        case RenderCmdType.WaitForPlayerAction:
            renderer.DrawMyPlayingCards(engine.MyCards);
            break;

        case RenderCmdType.RoundResult:
            renderer.DrawRoundWinner(cmd.Data.Winner);
            renderer.DrawScoreImage(engine.Scores);
            renderer.DrawPlayerCards(engine.PlayerCards);
            break;

        case RenderCmdType.GameOver:
            // 游戏结束 — 重置
            renderer.DrawGameOver();
            break;

        case RenderCmdType.Pause:
            // 不需要渲染，Engine 通知 Form 设置定时器
            PauseTimer.Start(cmd.Data.PauseMs);
            break;

        case RenderCmdType.None:
            break;
    }
}
```

## 需要新增/提取的类

### GameEngine 类
```
- 状态: currentState, currentRank, currentCount, whoseOrder, firstSend, 
        pokerList, currentPokers, currentSendCards, currentAllSendPokers,
        send8Cards, showSuits, whoShowRank, Scores, whoIsBigger, sleepTime/sleepMaxTime
- 方法:
    Tick() → RenderCommand
    Init()
    HandleDealComplete()
    HandleCallSuit(int suit)
    HandleAIDecision(int playerId)
    HandlePlayerDiscard(int[] cardIndices)
    HandlePlayerPlay(int[] cardIndices)
    CalculateRoundWinner()
    HandleGameComplete()
    ResumeAfterPause()
```

### GameRenderer 类（取代 DrawingFormHelper）
```
- 只读 game state，不做任何决策
- 方法全部以 Draw/Render 为前缀
- 构造函数接受 Graphics/bmp
```

### RenderCommand 结构
```
struct RenderCommand {
    RenderCmdType Type;
    RenderData Data;  // 联合体/多态类型
}

struct RenderData {
    int Round;
    int[] Cards;          // 牌数组
    int UserId;
    int PauseMs;
    int Suit;
    int Master;
    int Winner;
    int[] PlayerCounts;
    ArrayList Bottom;
    bool IsOurSide;
}
```

## 重构节奏建议

1. **第一阶段**: 提取 `GameEngine` 空壳 + RenderCommand 枚举 + 抽离逻辑部分（Algorithm 调用移出 DrawingFormHelper）
2. **第二阶段**: 逐个状态迁移。**从最简单的开始**: Pause → WaitingShowPass → ReadyCards → WaitingShowBottom → DrawOnceFinished → DrawOnceRank → DrawMySortedCards → WaitingForSending8Cards → DrawCenter8Cards → WaitingForSend
3. **第三阶段**: 干掉 DrawingFormHelper 中的逻辑副作用，重命名为 GameRenderer
4. **第四阶段**: 用事件/定时器替换 Pause 轮询
