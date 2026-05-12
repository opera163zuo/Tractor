# 拖拉机游戏状态机分析文档

> 分析目标：MainForm.cs + DrawingFormHelper.cs 中的游戏状态机
> 项目：四人拖拉机（升级/80分）Windows桌面游戏（C# WinForms .NET 4.6.1）
> 源码位置：/tmp/tractor-analysis/Tractor.net/

---

## 1. CardCommands 枚举定义（DefinedConstant.cs）

| 枚举值 | 数值 | 含义 |
|--------|------|------|
| `ReadyCards` | 0 | 发牌阶段 |
| `DrawCenter8Cards` | 1 | 绘制8张底牌到庄家 |
| `WaitingForSending8Cards` | 2 | 等待玩家扣底牌 |
| `DrawMySortedCards` | 3 | 绘制我方排序后的手牌 |
| `Pause` | 4 | 通用暂停状态（异步延时转换） |
| `WaitingShowPass` | 5 | 显示"无人叫主/过牌"信息 |
| `WaitingShowBottom` | 6 | 显示底牌 |
| `WaitingForSend` | 7 | 等待出牌（AI玩家自动出牌） |
| `WaitingForMySending` | 8 | 等待我方玩家点击出牌 |
| `DrawOnceFinished` | 9 | 绘制一轮出牌结果（四人出完） |
| `DrawOnceRank` | 10 | 绘制一局结束结果（全手牌出完） |
| `Undefined` | 11 | 未定义状态 |

---

## 2. 状态转换图（文本形式）

```
                           ┌─────────────────────────────────────────────┐
                           │                                             │
                           ▼                                             │
                    ┌──────────────┐                                     │
                    │  ReadyCards  │ ◄────────────────────────────────────┘
                    │  (发牌阶段)   │
                    └──────┬───────┘
                           │ currentCount == 25
                           ▼
                 ┌─────────────────────┐
                 │  DrawCenter8Cards    │
                 │  (画8张底牌到庄家)     │
                 └──┬────────────┬──────┘
                    │            │
         ┌──────────┘            └──────────┐
         ▼                                   ▼
  ┌────────────────┐              ┌──────────────────────┐
  │ NoRank（没人叫主）│              │ WaitingShowBottom    │
  │      ↓          │              │ (显示底牌)            │
  │ WaitingShowPass │              └──────────┬───────────┘
  │ (显示过牌信息)    │                         │ pause结束
  └────────┬───────┘                          ▼
           │ pause结束              ┌─────────────────────┐
           ▼                        │  DrawCenter8Cards    │
    ┌──────────────┐                │ (取8张给庄家 + 排序) │
    │  ReadyCards  │                └──────────┬───────────┘
    │ (重发新牌局)  │                           │
    └──────────────┘                            ▼
                                     ┌──────────────────────────┐
                                     │  WaitingForSending8Cards  │
                                     │  (等待扣底牌)              │
                                     └──────────────┬───────────┘
                                                    │ 玩家扣8张完成 / AI自动扣
                                                    ▼
                                          ┌─────────────────────┐
                                          │  DrawMySortedCards   │
                                          │  (整理并显示手牌)     │
                                          └──────────┬──────────┘
                                                     │ pause结束
                                                     ▼
                                           ┌───────────────────┐
                                      ┌────│  WaitingForSend    │
                                      │    │  (AI自动出牌)       │
                                      │    └───────────────────┘
                                      │            │
                                      │            │
                                      │            ▼
                                      │     ┌─────────────────────┐
                                      │     │ 等待出牌分支：        │
                                      │     │ whoseOrder==2 →     │
                                      │     │   DrawFrield...Cards │
                                      │     │ whoseOrder==3 →     │
                                      │     │   DrawPrev...Cards  │
                                      │     │ whoseOrder==4 →     │
                                      │     │   DrawNext...Cards  │
                                      │     │ whoseOrder==1(玩家)→ │
                                      │     │   WaitingForMySend  │
                                      │     └─────────────────────┘
                                      │                  │
                                      │                  ▼
                                      │     ┌─────────────────────┐
                                      │     │  WaitingForMySending │
                                      │     │  (玩家点击出牌)       │
                                      │     └──────────┬──────────┘
                                      │                │ 玩家点击小猪确认
                                      │                ▼
                                      │     ┌─────────────────────┐
                                      │     │  DrawMyFinishCards  │
                                      │     │  (画我出的牌)         │
                                      │     └──────────┬──────────┘
                                      │                │
                                      │                ▼
                                      │          Pause(短暂)
                                      │          sleepMaxTime 后
                                      │                │
                                      │                ▼
                                      │     ┌─────────────────────┐
                                      └─────│  DrawOnceFinished    │
                                            │ (四人出完一轮)        │
                                            └──────────┬──────────┘
                                                       │
                                                       ▼
                                              ┌──────────────────┐
                                              │ 计算本轮谁赢      │
                                              │ 清除currentSend  │
                                              │ 计算得分         │
                                              └────────┬─────────┘
                                                       │
                                          ┌────────────┴────────────┐
                                          │                         │
                                  手牌还有剩余              手牌出完
                                          │                         │
                                          ▼                         ▼
                                 ┌────────────────┐      ┌──────────────────┐
                                 │ WaitingForSend │      │ DrawFinished...  │
                                 │ (继续出牌)      │      │ (一局结束)        │
                                 └────────────────┘      └────────┬─────────┘
                                                                  │
                                                                  ▼
                                                          Pause(暂停)
                                                          │
                                                          ▼
                                                  ┌──────────────────┐
                                                  │  DrawOnceRank    │
                                                  │ (进入下一局)      │
                                                  └────────┬─────────┘
                                                           │
                                                           ▼
                                                   Undefined → init() → ReadyCards
```

---

## 3. 每个状态的详细分析

### 3.1 ReadyCards —— 发牌阶段

**进入条件：**
- 新游戏开始时（`MenuItem_Click` → `timer.Start()`）
- 初始化时（`init()` → 设置 `currentState.CurrentCardCommands = CardCommands.ReadyCards`）
- 无人叫主重发时（`WaitingShowPass` → 回到 `ReadyCards`）

**timer_Tick 逻辑（MainForm.cs 第697-718行）：**
- `currentCount == 0` 时：非调试模式显示工具栏（`DrawToolbar()`）
- `currentCount < 25` 时：每 tick 调用 `ReadyCards(currentCount)`，然后 `currentCount++`
  - ReadyCards 为每位玩家发一张牌（4位同时发），画出背面图
  - 其中0号玩家（自己）显示正面，其他显示背面
  - 过程中实时判断是否该叫主
- `currentCount == 25` 时：发完 → 切换到 `DrawCenter8Cards`

**数据读写：**
- 读：`pokerList[0..3]`（原始牌堆）、`currentCount`
- 写：`currentPokers[0..3]`（每人手中的牌）、`currentCount`、`currentState.CurrentCardCommands`
- 外部调用：`drawingFormHelper.DrawToolbar()`、`drawingFormHelper.ReadyCards(count)`
- 叫主判断：`MyRankOrNot(currentPokers[0])`、`DoRankOrNot(currentPokers[1..3])`

**出口转换：**
- `currentCount == 25` → `CardCommands.DrawCenter8Cards`

---

### 3.2 DrawCenter8Cards —— 画8张底牌给庄家

**进入条件：**
- ReadyCards 发完25轮，共104张

**timer_Tick 逻辑（MainForm.cs 第720-799行）：**

有两个独立入口：

**入口A：刚从ReadyCards过来（currentCount == 25）**
- 先判断 `DoRankNot()`（是否有人叫主）：
  - **无人叫主（NoRank）：**
    - 如果 `gameConfig.IsPass` = true（设置为"过"模式）
      - 调用 `init()` 重新洗牌
      - 显示过牌图片，`SetPauseSet(NoRankPauseTime, WaitingShowPass)` → Pause
      - 返回
    - 否则（强制模式）
      - 取4位玩家第0/1张牌各一张共8张做底牌
      - 从第三张花色强制确定主花色
      - 绘制底牌背面，`SetPauseSet(NoRankPauseTime, WaitingShowBottom)` → Pause
      - 返回
  - **有人叫主（正常流程）：**
    - `whoseOrder = currentState.Master`，`firstSend = whoseOrder`
    - `SetPauseSet(Get8CardsTime, DrawMySortedCards)`
    - `DrawCenter8Cards()` — 把8张剩余牌给庄家
    - `initSendedCards()` — 重新解析各玩家的挂牌
    - `DrawMySortedCards(...)` — 画自己排序后的手牌
    - 设置状态为 `WaitingForSending8Cards`

**入口B：从WaitingShowBottom回来（pause结束后）**
- 绘制底牌背面 → `SetPauseSet(Get8CardsTime, DrawCenter8Cards)` → Pause
- 等待下次 tick 进入入口A的正常流程

**数据读写：**
- 读：`currentState.Suit`、`currentState.Master`、`gameConfig.IsPass`、`pokerList[0..3]`
- 写：`whoseOrder`、`firstSend`、`currentState.CurrentCardCommands`、`currentState.Suit`、`currentState.Master`
- 写：`send8Cards`（重置）、`currentSendCards[0..3]`（通过 initSendedCards）
- 外部调用：`DrawCenter8Cards()`（DrawingFormHelper）、`DrawMySortedCards()`

**出口转换：**
- 无叫主+IsPass → `WaitingShowPass`（通过 Pause 桥接）
- 无叫主+!IsPass → `WaitingShowBottom`（通过 Pause 桥接）
- 有叫主 → `WaitingForSending8Cards`

---

### 3.3 WaitingShowPass —— 显示过牌信息

**进入条件：**
- 无人叫主+游戏设置为"过"

**timer_Tick 逻辑（MainForm.cs 第806-812行）：**
- 调用 `DrawCenterImage()` 清除中间区域
- `Refresh()`
- 直接设置状态为 `ReadyCards`

**出口转换：**
- 直接 → `ReadyCards`（重发新牌局）

---

### 3.4 WaitingShowBottom —— 显示底牌

**进入条件：**
- 无人叫主+强制模式

**timer_Tick 逻辑（MainForm.cs 第720-733行）：**
- `DrawCenterImage()` — 清中间
- 绘制8张底牌背面
- `SetPauseSet(Get8CardsTime, DrawCenter8Cards)` → Pause

**出口转换：**
- Pause → `DrawCenter8Cards`

---

### 3.5 WaitingForSending8Cards —— 等待扣底牌

**进入条件：**
- 正常叫主流程的 DrawCenter8Cards 分支

**timer_Tick 逻辑（MainForm.cs 第847-866行）：**
- 根据 `currentState.Master` 判断谁是庄家：
  - **Master == 1（自己）：**
    - 调试模式（AI）：调用 `Algorithm.Send8Cards(this, 1)` → 转到 `DrawMySortedCards`
    - 玩家模式：`DrawMyPlayingCards(...)` 显示可点击的牌，`Refresh(); return;`（暂停timer分发）
  - **Master == 2/3/4（AI）：**
    - 调用 `Algorithm.Send8Cards(this, 2/3/4)` → 自动扣牌

**扣牌完成的触发：**
- 玩家模式（Master=1）：在 `MainForm_MouseClick` 或 `MainForm_MouseDoubleClick` 中
  - 点击小猪区域 → 检查选了8张牌 → `send8Cards` 记下所选牌 → 从手牌移除 → `initSendedCards()` → 设置 `currentState.CurrentCardCommands = DrawMySortedCards`

**出口转换：**
- 扣牌完成 → `DrawMySortedCards`

---

### 3.6 DrawMySortedCards —— 整理并显示手牌

**进入条件：**
- 扣牌完成后，或者AI扣完底牌

**timer_Tick 逻辑（MainForm.cs 第849-859行）：**
- `SetPauseSet(SortCardsTime, DrawMySortedCards)` — 先暂停（给玩家看一眼动画）
- `DrawMySortedCards(currentPokers[0], count)` — 按花色排序显示
- `Refresh()`
- 设置状态为 `WaitingForSend`

**数据读写：**
- 读：`currentPokers[0]`
- 写：`myCardsLocation`、`myCardsNumber`、`myCardIsReady`（在DrawMySortedCards内部填充）
- 外部调用：`drawingFormHelper.DrawMySortedCards()`

**出口转换：**
- Pause → `WaitingForSend`

---

### 3.7 WaitingForSend —— 等待出牌（AI流程 / 自动出牌）

**进入条件：**
- 手牌排序完成

**timer_Tick 逻辑（MainForm.cs 第861-902行）：**
根据 `whoseOrder` 分发：

```
whoseOrder == 1: 我出牌
  - 调试模式(AI): 自动出牌 + DrawMyFinishSendedCards
  - 玩家模式: → WaitingForMySending（等待玩家点击）

whoseOrder == 2: 对家出牌
  → DrawFrieldUserSendedCards()  // 自动AI出牌
  → 判断是否4人都出完 → Pause/DrawOnceFinished 或 WaitingForSend

whoseOrder == 3: 上家出牌
  → DrawPreviousUserSendedCards()  // 自动AI出牌
  → 同上

whoseOrder == 4: 下家出牌
  → DrawNextUserSendedCards()  // 自动AI出牌
  → 同上
```

**AI出牌逻辑（DrawingFormHelper）：**
- `Draw[Role]SendedCards()` 中调用 `Algorithm.ShouldSendedCards()` 或 `Algorithm.MustSendedCards()` 决定出的牌
- 画牌到对应位置
- 从 `currentPokers[N]` 中移除出的牌
- 判断是否4人都出完 → Pause → DrawOnceFinished

**出口转换：**
- whoseOrder == 1（调试）/2/3/4 且未收齐4人 → `WaitingForSend`（继续下一个）
- whoseOrder == 1（玩家模式） → `WaitingForMySending`
- 4人出完 → Pause → `DrawOnceFinished`

---

### 3.8 WaitingForMySending —— 等待玩家点击出牌

**进入条件：**
- whoseOrder == 1 + 非调试模式

**timer_Tick 逻辑：**
- **不在此状态停留**（不进入 `timer_Tick` 的 `WaitingForSend` 分支中的 `whoseOrder==1` 玩家模式切换到此状态后，timer_Tick不处理此状态，靠事件驱动）

**事件驱动（MainForm_MouseClick / MainForm_MouseDoubleClick）：**
- 鼠标左键点击手牌区域：
  - `CalculateClickedRegion(e, 1)` → 切换选中/未选中
  - `DrawMyPlayingCards(...)` → 刷新显示（选中牌上移20px）
- 鼠标右键点击：
  - `CalculateRightClickedRegion(e)` → 连锁选中
  - 刷新显示
- 双击手牌：
  - `CalculateDoubleClickedRegion(e)` → 选中该牌
- 点击小猪（Ready区）：
  - 检查 `TractorRules.IsInvalid()` → 无效则提示/自动出对子
  - 有效则执行出牌：
    - 如果是首出：调用 `TractorRules.CheckSendCards()` 决定最小牌
    - 调用 `CommonMethods.SendCards()` 从手牌中移除
    - 调用 `DrawMyFinishSendedCards()`

**DrawMyFinishSendedCards（DrawingFormHelper 第608行起）：**
- 画我出的牌在桌面
- 记录到 `currentAllSendPokers[0]`
- 更新手牌区域（剩余手牌重绘）
- 判断是否4人都出完：
  - 是 → Pause → `DrawOnceFinished`
  - 否 → `whoseOrder = 4`（下家），状态设为 `WaitingForSend`

---

### 3.9 DrawOnceFinished —— 一轮出牌结束

**进入条件：**
- 4位玩家都出完一张/多张牌（Pause 到期后）

**timer_Tick 逻辑（MainForm.cs 第920-922行）：**
- 调用 `DrawFinishedOnceSendedCards()`（DrawingFormHelper）

**DrawFinishedOnceSendedCards 详细流程（DrawingFormHelper 第970行起）：**

```
1. 检查 currentPokers[0].Count == 0 → 全部出完 → DrawFinishedSendedCards() 并 return

2. 计算本轮谁赢：
   mainForm.whoseOrder = TractorRules.GetNextOrder(mainForm)
   int newFirst = mainForm.whoseOrder

3. 计算得分（如果赢家与庄家不同侧）：
   if (庄家是己方 但 赢家是对手方) 或 反之:
     TractorRules.CalculateScore(mainForm)  // 给对方加分

4. 清零 currentSendCards[0..3]（准备下一轮）

5. 更新 firstSend = newFirst

6. 清空桌面中央区域
```

**数据读写：**
- 读：`currentSendCards[0..3]`、`currentPokers[0..3]`、`currentState.Master`、`firstSend`
- 写：`whoseOrder`（下一轮先出者）、`firstSend`、`Scores`（通过 CalculateScore）
- 写：`currentSendCards[0..3]` 重置为新的 ArrayList

**出口转换：**
- 手牌还有剩余 → `WaitingForSend`（继续下一轮出牌）
- 手牌出完（currentPokers[0].Count == 0）→ 进入 DrawFinishedSendedCards

---

### 3.10 一局结束：DrawFinishedSendedCards → DrawOnceRank

**DrawFinishedSendedCards（DrawingFormHelper 第857行起）：**
```
1. isNew = false

2. TractorRules.GetNextMasterUser(mainForm)
   - 计算本局总分
   - 确定下一局庄家（升几级）
   - 更新 currentRank、OurCurrentRank/OpposedCurrentRank、Master

3. 清零 currentSendCards

4. 绘制：清中央 → DrawFinishedScoreImage()（显示底牌+得分+Logo）
   → SetPauseSet(FinishedThisTime, DrawOnceRank) → Pause

5. Pause 到期 → DrawOnceRank（timer_Tick）
```

**DrawOnceRank（MainForm.cs 第924行）：**
```
currentState.CurrentCardCommands = CardCommands.Undefined;
init();  // 重新洗牌、重置所有状态、以新Rank开始
```
`init()` 会设置 `currentState.CurrentCardCommands = CardCommands.ReadyCards`
然后 timer tick 会进入 `ReadyCards` 分支，开始新一局。

**出口转换：**
- Pause → `DrawOnceRank` → `Undefined` → `init()` → `ReadyCards`

---

### 3.11 Pause —— 通用暂停/异步转换桥接

**进入条件：**
- 任何需要延时的地方调用 `SetPauseSet(max, wakeupCommands)`

**SetPauseSet 方法（MainForm.cs 第932行）：**
```
sleepMaxTime = max;          // 暂停时长（毫秒）
sleepTime = DateTime.Now.Ticks;  // 记录开始时间
wakeupCardCommands = wakeup; // 暂停结束后进入的状态
currentState.CurrentCardCommands = CardCommands.Pause;
```

**timer_Tick 逻辑（MainForm.cs 第904-910行）：**
```
long interval = (DateTime.Now.Ticks - sleepTime) / 10000;  // 已过毫秒数
if (interval > sleepMaxTime)
{
    currentState.CurrentCardCommands = wakeupCardCommands;
}
```

**所有 Pause 的使用场景：**

| 调用位置 | 暂停时长 | 唤醒状态 |
|----------|----------|----------|
| WaitingShowBottom → 显示底牌 | `gameConfig.Get8CardsTime` (默认1000ms) | DrawCenter8Cards |
| DrawCenter8Cards (无人叫主) | `gameConfig.NoRankPauseTime` (默认5000ms) | WaitingShowPass |
| DrawCenter8Cards (无叫主强制) | `gameConfig.NoRankPauseTime` (默认5000ms) | WaitingShowBottom |
| DrawCenter8Cards (取8张) | `gameConfig.Get8CardsTime` (默认1000ms) | DrawMySortedCards |
| DrawMySortedCards | `gameConfig.SortCardsTime` (默认1000ms) | DrawMySortedCards（去抖） |
| 一轮出完 → DrawOnceFinished | `gameConfig.FinishedOncePauseTime` (默认1500ms) | DrawOnceFinished |
| DrawFinishedSendedCards | `gameConfig.FinishedThisTime` (默认2500ms) | DrawOnceRank |

---

## 4. 数据依赖总表：每个状态读写哪些 MainForm 成员

| 状态 | 读 | 写 |
|------|----|----|
| **ReadyCards** | `pokerList`, `currentCount`, `currentPokers`, `currentState`, `showSuits`, `whoShowRank`, `gameConfig.IsDebug` | `currentPokers[N]`, `currentCount++`, `currentState.CurrentCardCommands`, `showSuits`, `whoShowRank`, `currentState.Suit`, `currentState.Master` |
| **DrawCenter8Cards** | `DoRankNot()`, `gameConfig.IsPass`, `currentState.Master`, `pokerList`, `currentState.Suit` | `whoseOrder`, `firstSend`, `currentState.CurrentCardCommands`, `currentState.Suit`, `send8Cards`, `currentSendCards[0..3]`（通过initSendedCards） |
| **WaitingShowPass** | 无 | `currentState.CurrentCardCommands` |
| **WaitingShowBottom** | 无 | `currentState.CurrentCardCommands`（通过SetPauseSet） |
| **WaitingForSending8Cards** | `currentState.Master`, `gameConfig.IsDebug`, `currentPokers[0]`, `myCardIsReady` | `send8Cards`, `currentState.CurrentCardCommands`, `currentSendCards[0..3]`（initSendedCards） |
| **DrawMySortedCards** | `currentPokers[0]`, `currentState.Suit`, `currentRank` | `myCardsLocation`, `myCardsNumber`, `myCardIsReady`, `currentState.CurrentCardCommands` |
| **WaitingForSend** | `whoseOrder`, `currentSendCards[0..3]`, `currentPokers[N]`, `gameConfig.IsDebug` | `currentState.CurrentCardCommands`, `whoseOrder`, `currentSendCards[N]`, `currentAllSendPokers[N]` |
| **WaitingForMySending** | `myCardIsReady`, `myCardsNumber`, `TractorRules.IsInvalid()`, `firstSend` | `currentSendCards[0]`, `currentPokers[0]`, `pokerList[0]`, `currentState.CurrentCardCommands`, `whoseOrder`, `whoIsBigger` |
| **DrawOnceFinished** | `currentPokers[0]`, `currentSendCards[0..3]`, `firstSend`, `whoIsBigger` | `whoseOrder`（GetNextOrder）, `firstSend`, `Scores`（CalculateScore）, `currentSendCards[0..3]`（清零）, `currentState.CurrentCardCommands` |
| **一局结束** | `currentPokers[0]`, `Scores` | `isNew`, `currentRank`, `currentState.OurCurrentRank`, `currentState.OpposedCurrentRank`, `currentState.Master` |
| **DrawOnceRank** | 无 | `currentState.CurrentCardCommands = Undefined` → 调 `init()` |
| **Pause** | `sleepTime`, `sleepMaxTime`, `DateTime.Now` | `currentState.CurrentCardCommands = wakeupCardCommands`（超时后） |

---

## 5. 游戏逻辑 vs 渲染逻辑的切割建议

### 5.1 应抽取到 GameEngine 的核心字段

```csharp
// 状态管理（GameEngine）
- CurrentState currentState          // 当前状态机状态
- int currentRank                    // 当前级数
- bool isNew                         // 是否新牌局
- int showSuits                      // 亮主次数
- int whoShowRank                    // 谁亮的
- DistributePokerHelper dpoker       // 洗牌发牌器
- ArrayList[] pokerList              // 4人原始牌堆
- CurrentPoker[] currentPokers       // 4人当前手牌
- int currentCount                   // 发牌计数器
- ArrayList[] currentSendCards       // 4人当前出的牌
- int whoseOrder                     // 该谁出牌（0-4）
- int firstSend                      // 第一个出牌的人
- ArrayList send8Cards               // 扣的8张底牌
- int whoIsBigger                    // 本轮谁大
- int Scores                         // 本局得分
- CurrentPoker[] currentAllSendPokers // 所有已出过的牌
- GameConfig gameConfig              // 游戏配置
- object[] UserAlgorithms            // 4人AI算法

// 玩家交互
- ArrayList myCardsLocation          // 手牌X坐标列表
- ArrayList myCardsNumber            // 手牌编号列表
- ArrayList myCardIsReady            // 手牌选中标记
- int cardsOrderNumber               // 绘牌顺序计数器（属于渲染逻辑，但被排序驱动）

// 暂停系统
- long sleepTime
- long sleepMaxTime
- CardCommands wakeupCardCommands
```

### 5.2 应留在 Renderer (DrawingFormHelper) 的逻辑

```
所有 Draw* 方法：
- DrawBackground, DrawSidebar, DrawToolbar, RemoveToolbar
- ReadyCards（发牌绘制）
- DrawCenterAllCards/DrawCenterImage/DrawCenter8Cards
- DrawMyCards/DrawMySortedCards/DrawMyPlayingCards
- DrawMySendedCardsAction/Draw[Role]SendedCardsAction
- DrawFinishedOnceSendedCards/DrawFinishedSendedCards
- DrawWhoWinThisTime/DrawScoreImage/DrawFinishedScoreImage
- DrawBottomCards/DrawPassImage
- DrawMaster/DrawOtherMaster/DrawRank/DrawSuit/DrawSuits
- ReDrawToolbar/IsClickedRanked
- DrawAnimatedCard/DrawMyImage

以及 CalculateRegionHelper 的所有区域计算方法
```

### 5.3 混合逻辑（需要拆分）

| 方法 | 游戏逻辑部分 | 渲染部分 |
|------|-------------|----------|
| `Draw[Role]SendedCards()` | 调用 `Algorithm.ShouldSendedCards()/MustSendedCards()` 决定出什么牌 | 画牌到桌面、重绘手牌区域 |
| `DrawFinishedOnceSendedCards()` | `TractorRules.GetNextOrder()` 计算赢家、`CalculateScore()` 算分 | 清理桌面中央、画分数 |
| `DrawFinishedSendedCards()` | `TractorRules.GetNextMasterUser()` 计算新庄家/级数 | 画底牌+得分界面 |
| `ReadyCards()` (Renderer) | **无游戏逻辑**（发牌已由 dpoker 完成） | 绘制动画+卡片 |
| `MainForm_MouseClick` | `TractorRules.IsInvalid()`, `TractorRules.CheckSendCards()` | 无渲染，但触发 DrawMyPlayingCards |
| `init()` | 重置所有数据字段 | `DrawBackground()`, `DrawSidebar()`, `DrawMaster()` 等 |

### 5.4 重构建议架构

```
┌──────────────────────────────────────────────────────┐
│                    GameEngine                         │
│  - currentState (state machine)                       │
│  - pokerList, currentPokers, currentSendCards         │
│  - Timer_Tick dispatch (tick handler)                 │
│  - Sleep/Pause system                                 │
│  - Algorithm calls (AI decisions)                     │
│  - MouseClick processing (dispatch actions)           │
│  - init(), menu click handlers                        │
│                                                      │
│  Events: OnStateChanged(NewState)                     │
│          OnCardsUpdated(playerId, cards)              │
│          OnScoresUpdated(scores)                      │
│          OnAnimationNeeded(animationType, data)       │
└──────────────────┬───────────────────────────────────┘
                   │ Events
                   ▼
┌──────────────────────────────────────────────────────┐
│            DrawingFormRenderer                        │
│  (事件驱动，只负责绘制，不做游戏决策)                    │
│                                                      │
│  - OnStateChanged → 切换绘制模式                      │
│  - OnCardsUpdated → 重绘对应区域                      │
│  - OnScoresUpdated → 更新分数显示                     │
│  - OnAnimation → 播放发牌/出牌动画                    │
│  - HandleClick(x, y) → 返回点击了什么卡片              │
└──────────────────────────────────────────────────────┘
```

---

## 6. 抽取 GameEngine 时需封装的字段/方法清单

### 6.1 需封装的字段（从 MainForm 移到 GameEngine）

```csharp
// ========== 游戏状态 ==========
CurrentState currentState;
int currentRank;
bool isNew;
int showSuits;
int whoShowRank;
int currentCount;
int whoseOrder;       // 1=self, 2=frield, 3=previous, 4=next
int firstSend;
int whoIsBigger;
int Scores;

// ========== 牌数据 ==========
DistributePokerHelper dpoker;
ArrayList[] pokerList;          // [4] 原始牌堆
CurrentPoker[] currentPokers;   // [4] 当前持牌
ArrayList[] currentSendCards;   // [4] 本轮已出的牌
CurrentPoker[] currentAllSendPokers;  // [4] 所有已出牌
ArrayList send8Cards;           // 扣底的8张

// ========== 玩家交互 ==========
ArrayList myCardsLocation;      // 🔴 UI字段，渲染才需要
ArrayList myCardsNumber;        // 🔴 UI字段
ArrayList myCardIsReady;        // 🔴 UI字段
int cardsOrderNumber;           // 🔴 UI字段

// ========== 暂停系统 ==========
long sleepTime;
long sleepMaxTime;
CardCommands wakeupCardCommands;

// ========== 配置 ==========
GameConfig gameConfig;
object[] UserAlgorithms;        // [4] 玩家AI算法
```

> ⚠️ `myCardsLocation/myCardsNumber/myCardIsReady/cardsOrderNumber` 这四个是纯渲染字段，应留在 Renderer 中。

### 6.2 需封装的方法（从 MainForm 移到 GameEngine）

```csharp
// ========== 状态机核心 ==========
void timer_Tick(object sender, EventArgs e);     // 状态分发
void SetPauseSet(int max, CardCommands wakeup);  // 暂停系统
void init();                                     // 游戏初始化
void initSendedCards();                          // 重新解析手牌
void MenuItem_Click(object sender, EventArgs e);  // 新游戏菜单

// ========== 流程控制 ==========
// ReadyCards 分支（调用 Renderer 画牌，但发牌逻辑归 Engine）
// DrawCenter8Cards 分支
// WaitingShowPass/WaitingShowBottom 分支
// WaitingForSending8Cards 分支（主花色逻辑、底牌转移）
// DrawMySortedCards 分支
// WaitingForSend 分支（AI出牌调度）
// WaitingForMySending → 事件驱动
// DrawOnceFinished 分支
// DrawOnceRank 分支

// ========== 事件处理（仅决策部分） ==========
void MainForm_MouseClick(object sender, MouseEventArgs e);  // 出牌决策+叫主决策
void MainForm_MouseDoubleClick(...);                        // 简化出牌

// ========== 保存/恢复 ==========
void SaveToolStripMenuItem_Click(...);
void RestoreToolStripMenuItem_Click(...);
```

### 6.3 需暴露给 Renderer 的事件

```csharp
// GameEngine 事件
event Action<CardCommands> StateChanged;         // 状态变更
event Action<int, ArrayList> CardsDealt;         // 发牌：playerId, cards
event Action<int, ArrayList> CardsPlayed;        // 出牌：playerId, cards
event Action<int> ScoreUpdated;                  // 得分更新
event Action<int, int> RoundOver;                // 轮结束：winner, scores
event Action<GameResult> GameOver;               // 局结束
event Action<int, int, bool> MasterChanged;      // 庄家变更
event Action<int, int> SuitChanged;              // 主花色变更
event Action<bool[]> ShowRankToolbar;            // 显示叫主工具栏
event Action PauseStarted;                       // 暂停开始
event Action<CardCommands> PauseEnded;           // 暂停结束
event Action AnimationTrigger;                   // 动画需求
```

---

## 7. 状态机关键观察

### 7.1 设计特点
- **timer_Tick 作为统一调度中心**：所有状态转换最终通过 `timer_Tick` 完成
- **Pause 作为异步桥接**：通过轮询 `DateTime.Now - sleepTime` 实现非阻塞延时
- **玩家交互状态 "空转"**：`WaitingForMySending` 不处理 timer_Tick，等待鼠标事件触发
- **调试模式跳过交互**：`gameConfig.IsDebug = true` 时所有玩家的出牌都由 Algorithm 自动完成

### 7.2 设计问题
1. **状态机与渲染严重耦合**：timer_Tick 中同时处理状态转换和调用 Draw* 方法
2. **数据同时被 Engine 和 Renderer 读写**：currentPokers、currentSendCards 等被双方同时修改
3. **Pause 是忙等轮询**：每 100ms（TimerDiDa）轮询一次是否超时，不是异步回调
4. **游戏规则分散在多处**：叫主判断在 DrawingFormHelper 的 `DoRankOrNot`、`MyRankOrNot` 和 `IsClickedRanked` 中；算分在 TractorRules
5. **渲染方法内嵌游戏逻辑**：DrawFinishedOnceSendedCards 同时做计分决策和绘牌

### 7.3 重构优先级建议
1. **高优先级**：将 `currentState`、`currentPokers`、`pokerList`、`currentSendCards` 移到 GameEngine
2. **高优先级**：将 `timer_Tick` 的分发逻辑移到 GameEngine，Renderer 通过事件接收
3. **中优先级**：将 `Pause` 系统改为基于 `Task.Delay` 或 `Timer` 的回调
4. **中优先级**：将叫主判断逻辑从 DrawingFormHelper 抽出到独立的规则类
5. **低优先级**：将 `Algorithm.ShouldSendedCards`/`MustSendedCards` 的调用抽象到 Engine

---

*分析完成于：2026-05-12*
*行号基于 Tractor.net/MainForm.cs, DrawingFormHelper.cs, DefinedConstant.cs*
