# 06 — 算法迁移方案：Algorithm 系列类到 GameEngine

> **目标**：将 Algorithm.cs、TractorRules.cs、MustSendCardsAlgorithm.cs、ShouldSendedCardsAlgorithm.cs 四类中的 `mainForm` 依赖全部解除，改为 GameEngine 实例方法。

---

## 1. 全局扫描总览

| 文件 | 行数 | 方法数 | mainForm/form 引用数 | 涉及 mainForm 字段数 |
|------|------|--------|---------------------|---------------------|
| Algorithm.cs | 1109 | 15（8种包装+7种底层） | 37（mainForm 27 + form 16 但去重后26） | 11 |
| TractorRules.cs | 1300 | 12 | 108 | 10 |
| MustSendCardsAlgorithm.cs | 2554 | 13（3公开+10私有） | 56 | 5 |
| ShouldSendedCardsAlgorithm.cs | 555 | 1（单方法） | 66 | 3 |
| **合计** | **5518** | **41** | **~267** | **~15** |

### 1.1 被引用的 mainForm 字段汇总

| 字段 | 引用文件 | 引用总次数 | 归属判断 |
|------|---------|-----------|---------|
| `currentPokers` | Algo + Rules | 44（Algo 9 + Rules 35） | **GameEngine 状态** |
| `currentSendCards` | Rules + MustSend | 37（Rules 20 + MustSend 17） | **GameEngine 状态** |
| `currentState` | 全部4个文件 | ~40 | **GameEngine 状态** |
| `currentRank` | 全部4个文件 | ~30 | **GameEngine 状态** |
| `pokerList` | Algo + MustSend + ShouldSend | ~55 | **GameEngine 状态** |
| `whoIsBigger` | MustSend + ShouldSend | 48 | **GameEngine 状态**（游戏进行中的临时状态） |
| `firstSend` | Rules + MustSend + Algo | 12 | **GameEngine 状态** |
| `Scores` | Rules | 13 | **GameEngine 状态** |
| `UserAlgorithms` | Algo | 6 | **留给 MainForm 的扩展点**（插件接口） |
| `currentAllSendPokers` | Algo | 10 | **GameEngine 状态** |
| `myCardIsReady` | Rules（IsInvalid） | 4 | **UI 临时状态** → B 类 |
| `myCardsNumber` | Rules（IsInvalid） | 4 | **UI 临时状态** → B 类 |
| `gameConfig` | Rules + Algo | 12 | **GameEngine 状态**（配置对象） |
| `showSuits` | Algo（叫主） | 4 | **GameEngine 状态** |
| `send8Cards` | Rules + Algo | 2 | **GameEngine 状态** |
| `Text` | Rules（注释掉） | 1 | **UI 残留** → 删除 |
| `whoShowRank` | Algo（IsInvalidRank） | 3 | **GameEngine 状态** |
| `initSendedCards()` | Algo（Send8Cards） | 1 | **UI 刷新方法** → C 类通知 |

---

## 2. 文件级方法统计

### 2.1 Algorithm.cs（1109行）

#### 方法清单

| # | 方法 | 行号 | 可见性 | 参数 | mainRefs | formRefs | 引用的 mainForm/form 字段 |
|---|------|------|--------|------|----------|----------|--------------------------|
| 1 | ShouldSetRank | 18 | internal static | MainForm mainForm, int user | 9 | 0 | currentPokers |
| 2 | ShouldSetRankAgain | 96 | internal static | MainForm mainForm, CurrentPoker currentPoker | 1 | 0 | showSuits |
| 3 | CanSetRank | 149 | internal static | MainForm mainForm, CurrentPoker currentPoker | 5 | 0 | currentRank, showSuits |
| 4 | IsInvalidRank2 | 221 | private static | MainForm form, int suit | 0 | 4 | form.currentState.Suit, form.whoShowRank, form.gameConfig |
| 5 | IsInvalidRank | 237 | private static | MainForm form, int suit | 0 | 4 | form.currentState.Suit, form.whoShowRank, form.gameConfig |
| 6 | ShouldSendedCards(包装) | 260 | internal static | MainForm mainForm, int whoseOrder, ... | 10 | 0 | UserAlgorithms, currentAllSendPokers, currentState.Master, pokerList |
| 7 | MustSendedCards(包装) | 313 | internal static | MainForm mainForm, int whoseOrder, ... | 11 | 0 | currentAllSendPokers, UserAlgorithms, currentState.Master, firstSend, pokerList |
| 8 | Send8Cards | 366 | internal static | MainForm form, int user | 0 | 15 | form.currentState.Suit, form.currentRank, form.pokerList, form.gameConfig, form.send8Cards, form.initSendedCards() |
| 9 | Send8Cards1 | 451 | internal static | MainForm form, ... | 0 | 0 | 无（仅转发调用） |
| 10 | Send8Cards3 | 476 | internal static | MainForm form, ... | 0 | 0 | 无 |
| 11 | Send8Cards2 | 796 | internal static | MainForm form, ... | 0 | 0 | 无 |
| 12 | GetShouldSend8Cards | 848 | private static | 无 MainForm | 0 | 0 | 无 |
| 13 | GetShouldSend8CardsNoScores | 913 | private static | 无 MainForm | 0 | 0 | 无 |
| 14 | IsCanSend | 979 | private static | 无 MainForm | 0 | 0 | 无 |
| 15 | GetOrder | 1022 | private static | 无 MainForm | 0 | 0 | 无 |

#### 字段引用统计

| mainForm 字段 | 引用方法数 | 引用总次数 | 分类 |
|--------------|-----------|-----------|------|
| currentPokers | 1（ShouldSetRank） | 9 | A |
| currentAllSendPokers | 2（Should/MustSendedCards） | 10 | A |
| UserAlgorithms | 2（Should/MustSendedCards） | 6 | B（插件扩展点）|
| showSuits | 2（Should/CanSetRank） | 4 | A |
| currentState.Suit | 3（IsInvalidRank/IsInvalidRank2/Send8Cards） | 3 | A |
| currentRank | 2（CanSetRank/Send8Cards） | 3 | A |
| pokerList | 3（Should/MustSendedCards/Send8Cards） | 4 | A |
| firstSend | 1（MustSendedCards） | 1 | A |
| gameConfig | 3（IsInvalidRank/IsInvalidRank2/Send8Cards） | 7 | A |
| whoShowRank | 2（IsInvalidRank/IsInvalidRank2） | 3 | A |
| send8Cards | 1（Send8Cards） | 1 | A |
| initSendedCards() | 1（Send8Cards） | 1 | C（UI通知）|

#### 是否存在不通过 mainForm 参数的静态资源访问

- 所有 private 底层方法（GetShouldSend8Cards、IsCanSend、GetOrder等）完全不引用 mainForm，**它们是纯函数**，直接迁移。
- `ShouldSendedCards` / `MustSendedCards` 调用 `TractorRules.IsInvalid()` 和 `ShouldSendedCardsAlgorithm.ShouldSendCards()` 等——这些调用本身包含 mainForm 引用，因此是**转发依赖**。

---

### 2.2 TractorRules.cs（1300行）

#### 方法清单

| # | 方法 | 行号 | 可见性 | mainRefs | 引用的 mainForm 字段 |
|---|------|------|--------|----------|---------------------|
| 1 | IsInvalid(玩家出牌时) | 22 | internal static | 13 | myCardsNumber(3), currentSendCards(3), myCardIsReady(2), currentPokers(2), currentState.Suit(1), firstSend(1), currentRank(1) |
| 2 | IsInvalid(AI出牌时,重载) | 169 | internal static | 8 | currentSendCards(3), currentPokers(2), currentState.Suit(1), firstSend(1), currentRank(1) |
| 3 | GetNextRank | 313 | internal static | 14 | currentState(7), Scores(4), currentRank(2), gameConfig.MustRank(1) |
| 4 | IsMasterOK | 508 | internal static | 4 | currentState.Master(4) |
| 5 | CalculateNextMaster | 548 | internal static | 5 | currentState.Master(5) |
| 6 | GetNextMasterUser | 601 | internal static | 27 | currentSendCards(13), currentState(9), gameConfig(3), currentRank(1), Scores(1) |
| 7 | GetNextOrder | 699 | internal static | 17 | currentSendCards(14), currentState.Suit(1), currentRank(1), firstSend(1) |
| 8 | CalculateScore | 915 | internal static | 7 | currentSendCards(4), Scores(2), Text(1,已注释) |
| 9 | Calculate8CardsScore | 930 | internal static | 2 | send8Cards(1), Scores(1) |
| 10 | GetScores | 939 | private static | 0 | 无（纯函数） |
| 11 | CheckSendCards(玩家) | 964 | internal static | 14 | currentPokers(9), myCardIsReady(2), currentState.Suit(1), currentRank(1), myCardsNumber(1) |
| 12 | CheckSendCards(AI,重载) | 1133 | internal static | 11 | currentPokers(9), currentState.Suit(1), currentRank(1) |

#### 字段引用统计

| mainForm 字段 | 方法数 | 总次数 | 分类 |
|--------------|--------|--------|------|
| currentSendCards | 8 | 37 | A |
| currentState | 9 | 30 | A |
| currentPokers | 5 | 22 | A |
| Scores | 3 | 8 | A |
| currentRank | 5 | 8 | A |
| gameConfig | 2 | 4 | A |
| myCardIsReady | 2 | 4 | B（UI状态） |
| myCardsNumber | 2 | 4 | B（UI状态） |
| firstSend | 3 | 3 | A |
| send8Cards | 1 | 1 | A |
| Text | 1 | 1 | 删除 |

#### 关键发现

- `IsInvalid(玩家)` 和 `CheckSendCards(玩家)` 中引用的 `myCardIsReady` 和 `myCardsNumber` 是 **MainForm 独有的 UI 就绪状态**。这两个方法是为**玩家手动出牌**而设计的版本。
- AI专用的重载 `IsInvalid(AI)` 和 `CheckSendCards(AI)` 没有这两个引用，只依赖 `currentSendCards` 和 `currentPokers`。

---

### 2.3 MustSendCardsAlgorithm.cs（2554行）

#### 方法清单

| # | 方法 | 行号 | 可见性 | mainRefs | 引用的 mainForm 字段 |
|---|------|------|--------|----------|---------------------|
| 1 | MustSendCards(入口) | 20 | internal static | 8 | currentSendCards(5), currentState.Suit(1), currentRank(1), firstSend(1) |
| 2 | WhoseOrderIs2 | 49 | internal static | 15 | whoIsBigger(9), currentSendCards(4), firstSend(1), pokerList(1) |
| 3 | SendOtherSuitNoScores | 330 | private static | 0 | 无 |
| 4 | SendOtherSuitOrScores | 389 | private static | 0 | 无 |
| 5 | SendOtherSuit | 523 | private static | 0 | 无 |
| 6 | SendThisSuitNoScores | 574 | private static | 0 | 无 |
| 7 | SendThisSuitOrScores | 621 | private static | 0 | 无 |
| 8 | SendThisSuit | 749 | private static | 0 | 无 |
| 9 | SendMasterSuitNoScores | 793 | private static | 0 | 无 |
| 10 | SendMasterSuitOrScores | 978 | private static | 0 | 无 |
| 11 | SendMasterSuit | 1278 | private static | 0 | 无 |
| 12 | WhoseOrderIs3 | 1461 | internal static | 30 | whoIsBigger(16), currentSendCards(7), firstSend(4), currentPokers(2), pokerList(1) |
| 13 | WhoseOrderIs4 | 1959 | internal static | 33 | whoIsBigger(23), currentSendCards(7), firstSend(2), pokerList(1) |

#### 字段引用统计

| mainForm 字段 | 方法数 | 总次数 | 分类 |
|--------------|--------|--------|------|
| whoIsBigger | 3+1(ShouldSend) | 48 | A（游戏运行时状态）|
| currentSendCards | 4 | 23 | A |
| firstSend | 4 | 8 | A |
| pokerList | 3 | 3 | A |
| currentPokers | 1 | 2 | A |
| currentState.Suit | 1 | 1 | A |
| currentRank | 1 | 1 | A |

#### 关键发现

- 10个 private 方法（SendOtherSuit/MasterSuit/ThisSuit系列）**完全不引用 mainForm**，它们通过方法参数接受所有输入。
- 所有 mainForm 引用集中在 **WhoseOrderIs2/3/4 + 入口方法** 这4个方法中。
- `whoIsBigger` 是读写最频繁的字段——这是**当前轮谁最大**的临时游戏状态，完全适合进 GameEngine。

---

### 2.4 ShouldSendedCardsAlgorithm.cs（555行）

#### 方法清单

| # | 方法 | 行号 | 可见性 | mainRefs | 引用的 mainForm 字段 |
|---|------|------|--------|----------|---------------------|
| 1 | ShouldSendCards | 12 | internal static | 66 | pokerList(50), currentRank(15), whoIsBigger(1) |

#### 关键发现

- 555行的文件只有 **1个方法**。
- `pokerList` 引用 50 次——都是 `mainForm.pokerList[whoseOrder-1]` 传入 `CommonMethods.SendCards()` 作为第三个参数。实际上这些引用都可以直接用 `currentPokers[whoseOrder-1]` 替代（因为 SendCards 需要从实际牌列表中移除牌，而 currentPokers 就是数据副本）。
- `currentRank` 引用 15 次——多在 `mainForm.currentRank` 的比较分支中。

---

## 3. 改造方案分层

### A 类：纯读且有同级替代（可直接替换）

这类引用直接改成 `this.xxx`（GameEngine 的实例字段或属性）。

#### 迁移方式

| 原引用 | 替换为 | 涉及方法 |
|--------|--------|---------|
| `mainForm.currentPokers[i]` | `this.currentPokers[i]` | ShouldSetRank, IsInvalid ×2, GetNextOrder, CheckSendCards ×2, WhoseOrderIs3 |
| `mainForm.currentRank` | `this.currentRank` | CanSetRank, Send8Cards, IsInvalid ×2, GetNextRank, GetNextOrder, GetNextMasterUser, CheckSendCards ×2, ShouldSendCards |
| `mainForm.currentState.Suit` | `this.currentState.Suit` | IsInvalid ×2, IsInvalidRank ×2, Send8Cards, GetNextOrder, MustSendCards, GetNextMasterUser, GetNextRank，WhoseOrderIs2/3/4 |
| `mainForm.currentState.Master` | `this.currentState.Master` | ShouldSendedCards, MustSendedCards, IsMasterOK, CalculateNextMaster, GetNextMasterUser, GetNextRank |
| `mainForm.currentState.OurCurrentRank` | `this.currentState.OurCurrentRank` | GetNextRank, GetNextMasterUser |
| `mainForm.currentState.OpposedCurrentRank` | `this.currentState.OpposedCurrentRank` | GetNextRank, GetNextMasterUser |
| `mainForm.currentState.OurTotalRound` | `this.currentState.OurTotalRound` | GetNextRank |
| `mainForm.currentState.OpposedTotalRound` | `this.currentState.OpposedTotalRound` | GetNextRank |
| `mainForm.currentSendCards[i]` | `this.currentSendCards[i]` | IsInvalid ×2, GetNextOrder, GetNextMasterUser, CalculateScore, MustSendCards, WhoseOrderIs2/3/4 |
| `mainForm.firstSend` | `this.firstSend` | IsInvalid ×2, GetNextOrder, MustSendedCards, MustSendCards, WhoseOrderIs2/3/4 |
| `mainForm.whoIsBigger` | `this.whoIsBigger` | WhoseOrderIs2/3/4, ShouldSendCards |
| `mainForm.Scores` | `this.Scores` | CalculateScore, Calculate8CardsScore, GetNextRank, GetNextMasterUser |
| `mainForm.showSuits` | `this.showSuits`（叫主状态）| ShouldSetRankAgain, CanSetRank |
| `mainForm.whoShowRank` | `this.whoShowRank` | IsInvalidRank, IsInvalidRank2 |
| `mainForm.gameConfig.*` | `this.gameConfig.*` | IsInvalidRank ×2, Send8Cards, GetNextRank, GetNextMasterUser |
| `mainForm.currentAllSendPokers[i]` | `this.currentAllSendPokers[i]` | ShouldSendedCards, MustSendedCards |
| `mainForm.send8Cards` | `this.send8Cards` | Send8Cards, Calculate8CardsScore |
| `mainForm.pokerList[i]` | `this.pokerList[i]` | ShouldSendedCards, MustSendedCards, Send8Cards, WhoseOrderIs2/3/4, ShouldSendCards(50x!) |

**复杂度**：低。纯文本替换，通过编译检查保证完整性。

---

### B 类：依赖 UI 临时状态（需由调用者传入）

这类引用读取的是**用户当前在 UI 上选中的牌状态**，MainForm 保留这些数据，GameEngine 不接管。

| 原引用 | 替换策略 | 涉及方法 |
|--------|---------|---------|
| `mainForm.myCardIsReady[i]` | 改为**参数 `bool[] myCardIsReady` 传入** | `IsInvalid(玩家)` at L22, `CheckSendCards(玩家)` at L964 |
| `mainForm.myCardsNumber[i]` | 改为**参数 `int[] myCardsNumber` 传入** | `IsInvalid(玩家)` at L22, `CheckSendCards(玩家)` at L964 |

**复杂度**：中。

**改造方案**：为这两个方法保留两个版本：
- 版本1（玩家）：`IsInvalid(bool[] myCardIsReady, int[] myCardsNumber, ...)`——从 MainForm 传入选中状态
- 版本2（AI）：`IsInvalid(...)`——不依赖 UI 状态，AI 的待出牌数据已体现在 `currentSendedCards` 参数中

`CheckSendCards(玩家)` 也一样，但更简单——实际上它的 `myCardIsReady` 和 `myCardsNumber` 读取只用于构建当前要出的牌的 ArrayList `list`。而 **`list` 的内容可以从参数推断**：调用方可把 `ArrayList minCards` 反向构造，或者直接传入 `list`。

---

### B+ 类：插件扩展点（接口回调，MainForm 保留）

| 原引用 | 替换策略 | 涉及方法 |
|--------|---------|---------|
| `mainForm.UserAlgorithms[i]` | GameEngine 不持有。改造为：`IUserAlgorithm[] userAlgorithms` 作为构造函数参数传入，或事件回调 | `ShouldSendedCards(包装)`, `MustSendedCards(包装)` |

**复杂度**：中。

**方案**：把 `UserAlgorithms` 作为 GameEngine 构造时的配置项（`IUserAlgorithm[]`），引擎执行到需要调用的位置直接调用，不需要 MainForm 做中间人。

---

### C 类：读写 UI / 刷新触发（通过事件通知）

| 原引用 | 替换策略 | 涉及方法 |
|--------|---------|---------|
| `form.initSendedCards()` | GameEngine 触发**事件** `OnCardsSent` 或 `AfterBottomCardsSet`，MainForm 监听并刷新 UI | `Send8Cards` |
| `form.currentState.CurrentCardCommands = ...` | 直接改 GameEngine 的 `this.currentState.CurrentCardCommands`，MainForm 监听状态变更 | `Send8Cards` |
| `mainForm.Text = ...` | 已注释——**直接删除** | `CalculateScore` |

**复杂度**：高（需要事件架构设计）。

---

## 4. 方法级改造复杂度排序

### 4.1 Algorithm.cs

| 方法 | 改造复杂度 | 建议优先级 | 理由 |
|------|-----------|-----------|------|
| ShouldSetRank | **低** | 1 | 只读 currentPokers，纯逻辑 |
| ShouldSetRankAgain | **低** | 1 | 只读 showSuits，纯逻辑 |
| CanSetRank | **低** | 1 | 只读 currentRank/showSuits，纯逻辑 |
| IsInvalidRank / IsInvalidRank2 | **低** | 1 | 参数是 form，读 currentState.Suit/whoShowRank/gameConfig，全进Engine |
| ShouldSendedCards(包装) | **中** | 2 | 涉及 UserAlgorithms 插件，需事件架构 |
| MustSendedCards(包装) | **中** | 2 | 同上 |
| Send8Cards | **中** | 2 | 含 gameConfig.BottomAlgorithm 分发 + UI通知 |
| Send8Cards1/2/3 | **低** | 1 | 纯逻辑，参数化完整 |
| GetShouldSend8Cards/NoScores | **低** | 1 | 纯函数 |
| IsCanSend | **低** | 1 | 纯函数 |
| GetOrder | **低** | 1 | 纯函数 |

### 4.2 TractorRules.cs

| 方法 | 改造复杂度 | 建议优先级 | 理由 |
|------|-----------|-----------|------|
| IsInvalid(玩家) | **中** | 2 | 依赖 UI 状态 myCardIsReady/myCardsNumber→需参数传入 |
| IsInvalid(AI重载) | **低** | 1 | 不依赖 UI 状态 |
| GetNextRank | **低** | 1 | 纯逻辑，读 currentState/Scores/gameConfig |
| IsMasterOK | **低** | 1 | 纯读 currentState |
| CalculateNextMaster | **低** | 1 | 纯读 currentState |
| GetNextMasterUser | **中** | 2 | 大量读 currentSendCards/currentState，但都是A类替换 |
| GetNextOrder | **低** | 1 | 读 currentSendCards(大量)但都是A类 |
| CalculateScore | **低** | 1 | 读 currentSendCards + 写 Scores |
| Calculate8CardsScore | **低** | 1 | 读 send8Cards + 写 Scores |
| GetScores | **低** | 1 | 纯函数 |
| CheckSendCards(玩家) | **中** | 2 | 依赖 UI 状态 |
| CheckSendCards(AI) | **低** | 1 | 不依赖 UI 状态 |

### 4.3 MustSendCardsAlgorithm.cs

| 方法 | 改造复杂度 | 建议优先级 | 理由 |
|------|-----------|-----------|------|
| MustSendCards(入口) | **低** | 1 | 只读 currentSendCards/currentState/currentRank/firstSend |
| WhoseOrderIs2 | **低** | 1 | 读 whoIsBigger/currentSendCards/firstSend/pokerList——全部A类 |
| WhoseOrderIs3 | **低** | 1 | 同上 + 少量 currentPokers |
| WhoseOrderIs4 | **低** | 1 | 同上 |
| 10个Send*私有方法 | **低** | 1 | 纯函数，不引用 mainForm |

### 4.4 ShouldSendedCardsAlgorithm.cs

| 方法 | 改造复杂度 | 建议优先级 | 理由 |
|------|-----------|-----------|------|
| ShouldSendCards | **低~中** | 2 | 50次 pokerList 引用都是 `commonMethods.SendCards` 的数据源——可用 `currentPokers` 替代；15次 currentRank 全部A类；1次 whoIsBigger A类 |

---

## 5. 跨文件调用依赖图

```
                     Algorithm.ShouldSendedCards(MustSendedCards)
                     ┌──────────────────────────────────────────┐
                     │  调用 TractorRules.IsInvalid()          │
                     │  调用 TractorRules.CheckSendCards()      │
                     │  调用 ShouldSendedCardsAlgorithm.        │
                     │    ShouldSendCards()                     │
                     │  调用 MustSendCardsAlgorithm.            │
                     │    MustSendCards()                       │
                     └──────────────────────────────────────────┘
                                       │
                                       ▼
                     TractorRules.IsInvalid() → TractorRules.IsInvalid(重载)
                     TractorRules.GetNextMasterUser()
                       → GetNextOrder() → CalculateScore() → Calculate8CardsScore()
                       → IsMasterOK() → CalculateNextMaster() → GetNextRank()
                     TractorRules.CheckSendCards()

                     MustSendCardsAlgorithm.MustSendCards()
                       → WhoseOrderIs2()
                       → WhoseOrderIs3()
                       → WhoseOrderIs4()
                         (后3者调用: SendOtherSuitNoScores, SendThisSuit, SendMasterSuit 等10个私有方法)

                     ShouldSendedCardsAlgorithm.ShouldSendCards()
                       → CommonMethods.SendCards()
```

**所有跨类调用都是静态方法调用**，且所有被调方法也都接收 `mainForm` 参数。转换为 GameEngine 实例方法后，所有 `MyClass.Method(mainForm, ...)` 调用变成 `this.Method(...)`。

---

## 6. 改造顺序建议

### 阶段 1：纯函数优先（优先级1，无风险）

1. **Algorithm.cs** 私有方法：`GetShouldSend8Cards`, `GetShouldSend8CardsNoScores`, `IsCanSend`, `GetOrder`
2. **MustSendCardsAlgorithm.cs** 10个私有 Send* 方法
3. **TractorRules.cs** `GetScores`

这些方法已经是纯函数，移到 GameEngine 作为 private 实例方法即完成。

### 阶段 2：A 类替换（优先级1，大量替换但无风险）

按类逐一完成：

1. **MustSendCardsAlgorithm.cs**（最容易，98% A类）
   - WhoseOrderIs2 — 替换 whoIsBigger/currentSendCards/firstSend → this.
   - WhoseOrderIs3 — 同上
   - WhoseOrderIs4 — 同上
   - MustSendCards(入口) — 同上

2. **Algorithm.cs**（80% A类）
   - ShouldSetRank — 替换 currentPokers → this.currentPokers
   - ShouldSetRankAgain — 替换 showSuits → this.showSuits
   - CanSetRank — 替换 currentRank/showSuits → this.
   - IsInvalidRank/IsInvalidRank2 — 参数名从 form 改为 this
   - Send8Cards/Send8Cards1/2/3 — 全部 A 类替换

3. **TractorRules.cs**（70% A类）
   - IsInvalid(AI) — 替换 currentSendCards/currentPokers/currentState/currentRank → this.
   - GetNextRank — 替换 currentState/Scores/currentRank → this.
   - IsMasterOK/CalculateNextMaster — 替换 currentState → this.
   - GetNextOrder — 替换 currentSendCards/currentState/currentRank/firstSend → this.
   - CalculateScore/Calculate8CardsScore — 替换 currentSendCards/send8Cards/Scores → this.
   - CheckSendCards(AI) — 替换 currentPokers/currentState/currentRank → this.
   - GetNextMasterUser — 替换 currentSendCards/currentState/Scores/currentRank/gameConfig → this.

4. **ShouldSendedCardsAlgorithm.cs**
   - ShouldSendCards — 关键：把 50 次 `mainForm.pokerList[whoseOrder-1]` 替换为 `this.pokerList[whoseOrder-1]`

### 阶段 3：B 类改造（优先级2，需设计参数）

1. **TractorRules.IsInvalid(玩家)** 和 **CheckSendCards(玩家)**
   - 增加参数 `bool[] myCardIsReady, int[] myCardsNumber`
   - 或：MainForm 在调用前自行组装出 `ArrayList readyCards`，传入替代

### 阶段 4：B+ 类改造（优先级2）

1. **Algorithm.ShouldSendedCards(包装)** 和 **MustSendedCards(包装)**
   - `UserAlgorithms` 以 `IUserAlgorithm[]` 注入 GameEngine 构造函数
   - 如果某个算法为 null，直接 fallback 到算法类的默认方法

### 阶段 5：C 类改造（优先级3，事件架构）

1. **Send8Cards** 末尾的 UI 调用
   - `form.initSendedCards()` → 触发 `event Action OnBottomCardsSet`
   - `form.currentState.CurrentCardCommands = ...` → 直接写 `this.currentState.CurrentCardCommands = ...`，MainForm 订阅状态变更

---

## 7. 测试验证建议

### 7.1 能通过"AI vs AI 闷声跑一局"验证的方法（无需人工介入）

这些方法的正确性可以通过让两个 AI 用新旧两套引擎对打来验证——如果所有出牌选择完全一致则无误：

- **TractorRules.cs**: `IsInvalid(AI)`, `CheckSendCards(AI)`, `GetNextOrder`, `CalculateNextMaster`, `IsMasterOK`
- **MustSendCardsAlgorithm.cs**: `MustSendCards`, `WhoseOrderIs2/3/4` 及其所有私有 Send* 方法
- **Algorithm.cs**: `ShouldSendedCards(包装)`, `MustSendedCards(包装)`（排除 UserAlgorithms null 路径）
- **ShouldSendedCardsAlgorithm.cs**: `ShouldSendCards`

### 7.2 需要在特定游戏状态下验证的方法

这些方法在 AI vs AI 测试中可能不会被每个分支都覆盖到，需要人工构造特定牌局：

- **Algorithm.cs**: `ShouldSetRank`, `ShouldSetRankAgain`, `CanSetRank`, `IsInvalidRank`——只在**叫主阶段**触发
- **Algorithm.cs**: `Send8Cards/Send8Cards1/2/3`——只在**扣底阶段**触发
- **TractorRules.cs**: `GetNextRank`——只在**每局结束**触发
- **TractorRules.cs**: `GetNextMasterUser`——涉及 JToBottom/QToHalf/AToJ 特殊规则，需构造对应等级
- **TractorRules.cs**: `CheckSendCards(玩家)`——需要玩家手动出牌触发

### 7.3 验证策略建议

1. **检查清单**：对每个方法，在改造后编译通过，然后**打印每个 `this.xxx` 引用的新旧值对比日志**，跑 100 局 AI vs AI，确认完全一致
2. **边界测试专项**：分别测试叫主、扣底、甩牌检查、拖拉机计算边界
3. **分阶段验证**：每完成一个阶段的改造，先单独验证该阶段改动的正确性，不要等全部改完再测试
4. **回归基准**：在改造前跑 100 局 AI vs AI，记录每局的出牌序列（序列化到文件），改造后跑相同牌局的种子序列对比

---

## 8. GameEngine 实例方法改造总结

### 8.1 新方法签名

| 类 | 旧签名 | 新签名 |
|---|--------|--------|
| Algorithm.ShouldSetRank | `static int(MainForm, int user)` | `int ShouldSetRank(int user)` |
| Algorithm.CanSetRank | `static bool[](MainForm, CurrentPoker)` | `bool[] CanSetRank(CurrentPoker)` |
| Algorithm.ShouldSendedCards | `static ArrayList(MainForm, int, CurrentPoker[], ArrayList[], int, int, int)` | `ArrayList ShouldSendedCards(...同上减去MainForm...)` |
| Algorithm.MustSendedCards | `static ArrayList(MainForm, int, CurrentPoker[], ArrayList[], int, int, int)` | `ArrayList MustSendedCards(...同上减去MainForm...)` |
| Algorithm.Send8Cards | `static void(MainForm, int user)` | `void Send8Cards(int user)` |
| TractorRules.IsInvalid(AI) | `static bool(MainForm, ArrayList[], int)` | `bool IsInvalid(ArrayList[], int)` |
| TractorRules.GetNextRank | `static void(MainForm, bool)` | `void GetNextRank(bool success)` |
| TractorRules.GetNextMasterUser | `static void(MainForm)` | `void GetNextMasterUser()` |
| TractorRules.CheckSendCards(AI) | `static bool(MainForm, ArrayList, ArrayList, int)` | `bool CheckSendCards(ArrayList, ArrayList, int)` |
| MustSendCardsAlgorithm.MustSendCards | `static void(MainForm, CurrentPoker[], int, ArrayList, int)` | `void MustSendCards(CurrentPoker[], int, ArrayList, int)` |
| ShouldSendedCardsAlgorithm.ShouldSendCards | `static void(MainForm, CurrentPoker[], int, ArrayList)` | `void ShouldSendCards(CurrentPoker[], int, ArrayList)` |

### 8.2 GameEngine 新增实例字段

GameEngine 需要包含以下状态字段，取代原先从 MainForm 读取的数据：

```csharp
class GameEngine {
    // 从 MainForm 迁移过来的状态
    CurrentPoker[] currentPokers;          // 4人当前手牌
    ArrayList[] currentSendCards;           // 4人当前轮已出牌
    ArrayList[] currentAllSendPokers;       // 4人已出过的所有牌
    ArrayList send8Cards;                   // 底牌
    ArrayList[] pokerList;                  // 4人原始牌列表（用于 CommonMethods.SendCards 删除操作）
    GameState currentState;                 // 游戏状态（含 Suit, Rank, Master, OurCurrentRank 等）
    int currentRank;                        // 当前级别
    int firstSend;                          // 本轮先出牌者
    int whoIsBigger;                        // 当前轮谁最大
    int Scores;                             // 得分
    int showSuits;                          // 叫主花色展示状态
    int whoShowRank;                        // 谁叫主
    GameConfig gameConfig;                  // 游戏配置
    IUserAlgorithm[] userAlgorithms;        // 插件算法（可选）
    
    // 事件
    event Action OnBottomCardsSet;          // 扣底完成
}
```

### 8.3 各字段在 GameEngine 中的赋值时机

| 字段 | 赋值时机 | 来源 |
|------|---------|------|
| currentPokers | 发牌后 / 每次出牌后 | MainForm 发牌逻辑→Engine |
| currentSendCards | 每轮出牌后 | Engine 自己管理 |
| currentAllSendPokers | 每手牌出完后 | Engine 自己管理 |
| send8Cards | 筛选底牌后 | Engine. Send8Cards 方法填充 |
| pokerList | 从 MainForm 传入 | MainForm 发牌后复制给 Engine |
| currentState | 每局开始/结束 | Engine 自己管理 |
| currentRank | 每次升级后 | Engine.GetNextRank 设置 |
| firstSend | 每轮出牌开始时 | Engine 自己管理 |
| whoIsBigger | 每轮比较大小后 | Engine 自己管理（WhoseOrderIs2/3/4 中赋值）|
| Scores | 每轮结算后 | Engine.CalculateScore 设置 |
| showSuits | 叫主阶段 | Engine 叫主逻辑设置 |
| whoShowRank | 叫主阶段 | Engine 叫主逻辑设置 |
| gameConfig | 构造函数参数 | MainForm 创建 Engine 时传入 |

---

## 附录：MainForm 字段归属完整性核对

### 迁移到 GameEngine 的字段（15个）

| 字段 | 类型 | 用途 | 迁移方式 |
|------|------|------|---------|
| currentPokers | CurrentPoker[4] | 4人当前手牌对象 | 直接迁移 |
| currentSendCards | ArrayList[4] | 当前轮每人出了什么牌 | 直接迁移 |
| currentAllSendPokers | ArrayList[4] | 每人已出的所有牌历史 | 直接迁移 |
| send8Cards | ArrayList | 底牌 | 直接迁移 |
| pokerList | ArrayList[4] | 原始牌列表 | 直接迁移 |
| currentState | GameState | 游戏状态对象 | 直接迁移 |
| currentRank | int | 当前级别 | 直接迁移 |
| firstSend | int | 先出牌者 | 直接迁移 |
| whoIsBigger | int | 当前轮谁最大 | 直接迁移 |
| Scores | int | 得分 | 直接迁移 |
| showSuits | int | 显示花色状态 | 直接迁移 |
| whoShowRank | int | 谁叫了主 | 直接迁移 |
| gameConfig | GameConfig | 游戏配置 | 直接迁移 |
| UserAlgorithms | IUserAlgorithm[4] | 插件接口(可选) | 构造函数注入 |
| Text | string | 窗口标题(已注释) | **删除** |

### 保留在 MainForm 的字段（2个）

| 字段 | 类型 | 用途 | 保留理由 |
|------|------|------|---------|
| myCardIsReady | ArrayList (bool) | 用户选中了哪些牌 | 纯 UI 状态 |
| myCardsNumber | ArrayList (int) | 用户选中牌的编号 | 纯 UI 状态，与 myCardIsReady 一一对应 |

### 迁移策略

```
改造后调用链：

MainForm
  ├─ 创建 GameEngine(gameConfig, userAlgorithms)
  ├─ gameEngine.DealCards(pokerList)        // 发牌完成后调用
  ├─ gameEngine.PlayRound(...)              // 每轮出牌
  ├─ gameEngine.CalculateRoundResult()      // 每轮结束
  │
  └─ gameEngine 触发事件 → MainForm 刷新 UI
       ├─ OnStateChanged
       ├─ OnCardsSent  
       ├─ OnRoundEnded
       └─ OnGameEnded
```
