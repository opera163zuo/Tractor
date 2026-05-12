# Phase A 数据流纯化 — 影响分析

**文件**: Tractor.net/GameEngine.cs, Tractor.net/GameState.cs, Tractor.net/MainForm.cs
**改造目标**: Engine 不再持有外部数据引用，所有输入通过参数传入，输出通过返回值返回

---

## 1. GameEngine.cs 内部字段和方法签名总览

### 内部字段

| 字段 | 类型 | 设置处 | 读取/修改处 |
|------|------|--------|------------|
| `_state` | `CurrentState` | NewGame() 设初始值 | Tick 各处读/写，SetPause 写，PlayerSend8Cards/PlayerPlayCard 读/写 |
| `_pauseStartTicks` | `long` | SetPause() | Tick(Pause 分支) |
| `_pauseMaxMs` | `long` | SetPause() | Tick(Pause 分支) |
| `_wakeupCommand` | `CardCommands` | SetPause() | Tick(Pause 分支) |
| `_dealCount` | `int` | NewGame(), Tick(ReadyCards) | Tick(ReadyCards) |
| `_isDebug` | `bool` | SetGameData() | Tick(WaitingForSending8Cards/WaitingForSend) |
| `_whoseOrder` | `int` | NewGame(), SyncOrder(), PlayerPlayCard() | Tick(WaitingForSend) |
| `_firstSend` | `int` | NewGame(), SyncOrder(), GetNextOrder() | GetNextOrder() |
| `_whoIsBigger` | `int` | NewGame(), SyncOrder(), PlayerPlayCard() | - (仅存储) |
| `_hasCalledRank` | `bool` | NewGame() | - (目前已无代码读它) |
| `_pokerLists`(🔴) | `ArrayList[]` | SetGameData() | Tick(ReadyCards 分支), PlayerSend8Cards, PlayerPlayCard |
| `_currentPokers`(🔴) | `CurrentPoker[]` | SetGameData() | Tick(ReadyCards), PlayerSend8Cards, PlayerPlayCard |
| `_currentSendCards`(🔴) | `ArrayList[]` | SetGameData() | PlayerPlayCard |
| `_send8Cards`(🔴) | `ArrayList` | SetGameData() | PlayerSend8Cards |
| `_config` | `GameConfig` | SetGameData() | Tick(WaitingForSending8Cards/Pause), PlayerPlayCard |

### 方法签名

```csharp
public GameEngine()
public void SetGameData(ArrayList[], CurrentPoker[], ArrayList[], ArrayList, GameConfig, bool)
public void NewGame()
public TickResult Tick(long nowTicks)
public void SetPause(int maxMs, CardCommands wakeup)
public PlayResult PlayerSend8Cards(List<int> selectedNumbers)
public PlayResult PlayerPlayCard(int playerId, List<int> selectedNumbers)
public void SyncOrder(int whoseOrder, int firstSend, int whoIsBigger)
public void SyncRank(int suit, int master)
public void SyncState(CardCommands command)
public void SyncTeamRanks(int ourRank, int opposedRank)
public void SetCurrentRank(int rank)
public CurrentState State => _state
public int DealCount => _dealCount
public int WhoseOrder => _whoseOrder
public int FirstSend => _firstSend
public int WhoIsBigger => _whoIsBigger
public int Scores { get; set; }
```

---

## 2. 每个字段的流向分析

### (🔴) _pokerLists, _currentPokers, _currentSendCards, _send8Cards

这四个字段是核心问题所在。SetGameData 把 MainForm 的 ArrayList 数组引用直接塞给 Engine。之后 Engine 在以下位置直接修改它们：

1. Tick(ReadyCards) 分支—发牌：`int card0 = (int)_pokerLists[0][_dealCount]`(读)  → `_currentPokers[0].AddCard(card0)` (改 MainForm.currentPokers[0])
2. PlayerSend8Cards 扣底：`_send8Cards.Add(number)` 改 MainForm.send8Cards；`_currentPokers[0].RemoveCard(number)` 改 MainForm.currentPokers[0]；`_pokerLists[0].Remove(number)` 改 MainForm.pokerList[0]
3. PlayerPlayCard 出牌：`_currentSendCards[playerId-1].Add(number)` 改 MainForm.currentSendCards；`_pokerLists[playerId-1].Remove(number)` 改 MainForm.pokerList；`_currentPokers[playerId-1] = CommonMethods.parse(...)` 改 MainForm.currentPokers

这些操作都是原地修改 MainForm 的数据，意味着 Engine 的数据"真相"和 MainForm 的数据"真相"指向同一堆内存。

### _state (CurrentState struct)

_state 是 struct 值类型。Engine 在多个 Tick 分支和 Player* 方法中直接写 `_state.CurrentCardCommands = xxx`。MainForm 的 timer_Tick 同时也有自己维护的 currentState（同名但不同变量），需要通过 engine.SyncState(CardCommands) 推回给 Engine。两者需要频繁同步，容易不同步。

### _whoseOrder, _firstSend, _whoIsBigger

这些在 PlayerPlayCard 中被 Engine 设为计算结果，MainForm 通过 engine.WhoseOrder 属性读取。同时 MainForm 也可以调用 engine.SyncOrder() 把 MainForm 侧的相同字段推回 Engine。同样的问题：两份数据需要手动同步。

---

## 3. Tick 方法改为接收 GameState 参数的影响

### 当前 Tick 内部依赖

| Tick 分支 | 依赖的数据 | 来源 |
|-----------|-----------|------|
| Pause | _pauseStartTicks, _pauseMaxMs, _wakeupCommand | 内部字段 |
| ReadyCards | _dealCount, _pokerLists[0..3][_dealCount], _currentPokers[0..3] | **核心依赖** |
| DrawCenter8Cards | 无（只发指令） | - |
| WaitingForSending8Cards | _state.Master, _isDebug, _config | 内部字段 |
| WaitingForSend | _whoseOrder, _isDebug | 内部字段 |

### 改造方案

将 `Tick(long nowTicks)` → `Tick(GameState state, long nowTicks)`

GameState 需要扩展以包含 Tick 需要的全部数据。当前 GameState 已有 PokerLists、CurrentPokers、CurrentSendCards、Send8Cards、CurrentRank、State。缺少的：
- `_dealCount` — 应在 GameState 中加一个 DealCount
- `_pauseStartTicks / _pauseMaxMs / _wakeupCommand` — Engine 内部暂停计时，建议保留为 Engine 内部字段（不暴露到 GameState），因为这是 Engine 实现细节
- `_isDebug` — 应在 GameConfig 已有，不需要额外加

### (🔴) 关键问题：发牌数据的传递

Tick(ReadyCards) 中直接读 `_pokerLists[0][_dealCount]` 来取第 n 张发的牌。如果 GameState 包含 PokerLists 引用，Engine 还是能通过 state 引用到 MainForm 的 ArrayList。

方案 A（不可变）：Tick 内将本次需要发的牌从 state 中拷贝出来，修改后的 state 通过返回值传回。但 CurrentPoker.AddCard() 是原地方法，要改为不可变需要重写 CurrentPoker（~500 行），改动太大。

**推荐方案 B（所有权转移）**：Tick 中从 state 拿牌，修改 state 的 CurrentPokers，将修改后的 state 返回给 MainForm 替换旧 state。本质还是引用的浅拷贝，但对调用方来说是单向的（Engine 产生新 state，MainForm 替换旧 state）。

---

## 4. PlayerPlayCard/PlayerSend8Cards 的数据流重构

### 改动签名

```csharp
// 当前
public PlayResult PlayerPlayCard(int playerId, List<int> selectedNumbers)
// 改为
public (PlayResult result, GameState newState) PlayerPlayCard(
    GameState state, int playerId, List<int> selectedNumbers)
```

### 数据流方案

Engine 读取 state.PokerLists, state.CurrentPokers 等，构造出新的数据，与 PlayResult 一同返回。

需要处理的问题：
1. **CurrentPoker 修改**：Engine 里已有模式 `_currentPokers[playerId-1] = CommonMethods.parse(...)`，替换为改新 state 中的副本。
2. **ArrayList.Remove**：改为构造新 ArrayList 过滤掉要移除的牌。
3. PokerLists[i] 是 ArrayList 可变类型，修改前用 `new ArrayList(oldArr)` 做浅拷贝。

### (⚠️) 浅拷贝方案

```csharp
var newState = new GameState {
    PokerLists = state.PokerLists.Select(arr => new ArrayList(arr)).ToArray(),
    // CurrentPokers 在 Engine 内部修改后通过 new CurrentPoker() + parse 重建
    ...
};
```

每次 Tick 拷贝 4 个 ArrayList（每个~25 张牌，共~208 个 int），内存开销 < 10KB/次，可忽略。

---

## 5. SetGameData/Sync* 删除后 MainForm 改造方案

### SetGameData 删除

MainForm.init() 最后一行删除 `engine.SetGameData(pokerList, ...)`。Engine.NewGame() 不需要知道外部数据。

### SyncRank/SyncTeamRanks (1 处: DrawingFormHelper.DoRankOrNot)

亮主/叫主后，MainForm 更新 GameState.State.Suit/Master/Rank，Engine 不再存同步数据。

### SyncState(CardCommands) (约 6 处: timer_Tick + 各种 MouseClick 分支)

MainForm 推进状态机时，改为直接构造 GameState 传给 Engine.Tick()，由 Engine 读取 GameState.State.CurrentCardCommands。

### SyncOrder(whoseOrder, firstSend, whoIsBigger) (多处)

这些数据从 GameState 读取。Engine 的计算结果写入返回值中的 GameState。

### SetCurrentRank(int) (timer_Tick)

MainForm 直接设置 GameState.CurrentRank。

---

## 6. 风险点

(🔴) **CommonMethods.parse 在 Engine 内部被调用**：PlayerSend8Cards/PlayerPlayCard 调用 parse(pokerList[i], suit, rank) 来重建 CurrentPoker。参数是 ArrayList（老类型），返回值是 CurrentPoker（老类型）。迁移到 GameState 后不改变这个依赖，风险可控。

(🔴) **ArrayList 类型不易不可变**：GameState 的 PokerLists 是 ArrayList[]，每个 ArrayList 可变。每次需要用 new ArrayList(oldArr) 拷贝。

(🔴) **MainForm 的 timer_Tick 大量状态推理逻辑**：当前 timer_Tick（~700 行）处理大量"下一步做什么"的逻辑。Phase A 建议只做"数据流重构"，不搬状态机逻辑。

(⚠️) **_state 是 struct (值类型)**：CurrentState 是 struct。Engine 需要把计算结果通过返回值写回 GameState。

---

## 7. 工作量估算

### GameEngine.cs（416 行 → 预计~450 行）

| 改动 | 行数 |
|------|------|
| 删除字段和 SetGameData | ~20 行 |
| 删除 Sync* 方法群 | ~20 行 |
| 删除属性 WhoseOrder/FirstSend/WhoIsBigger/Scores | ~11 行 |
| 修改 Tick 签名 + 内部引用 | ~15 行 |
| 修改 PlayerSend8Cards 签名+数据操作 | ~35 行 |
| 修改 PlayerPlayCard 签名+数据操作 | ~45 行 |
| **合计** | **~146 行** |

### GameState.cs（40 行 → 预计~70 行）

| 改动 | 行数 |
|------|------|
| 增加 DealCount 字段 | ~3 行 |
| 浅拷贝方法 Clone() | ~20 行 |
| **合计** | **~23 行** |

### MainForm.cs（1,390 行 → 预计~1,340 行）

| 改动 | 行数 |
|------|------|
| init() 中删除 SetGameData + Sync* | ~10 行删除 |
| timer_Tick 中 Sync* 调用改用 GameState | ~30 行修改 |
| 鼠标点击中调用改传 GameState | ~5 行 |
| **合计** | **~45 行** |

### 总计: 修改 3 个文件，约 210 行改动。

---

## 建议

✅ **建议现在实施**。Phase A 是其他两个 Phase 的依赖基础。改动范围可控（纯数据流重构，不改变算法逻辑），不影响运行时行为。预计需要半天完成。完成后，Engine 不再持有外部数据引用，Sync 方法全部消除，Engine 可独立测试。
