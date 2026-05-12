# Phase C 核心牌规逻辑沉入 Engine — 影响分析

**文件**: Tractor.net/GameEngine.cs, Tractor.net/Algorithms/AlgorithmCore.cs, Tractor.net/Algorithms/TractorRules.cs, Tractor.net/CommonMethods.cs
**改造目标**: Engine 能独立完成一圈胜负判定（GetNextOrder 不再是简化占位符）和本墩得分计算

---

## 1. TractorRules.cs 全貌

文件路径: `/home/zxyzxy/文档/tractor/Tractor.net/Algorithms/TractorRules.cs`

### 所有 public 方法

| 方法 | 签名 | 行数 | 功能 | 依赖 MainForm? |
|------|------|------|------|---------------|
| **IsInvalid** (重载1) | `IsInvalid(MainForm, ArrayList[], int who) → bool` | ~70 | 玩家出牌合法性检查（读 myCardIsReady） | (🔴) 是 — 读 mainForm.myCardIsReady |
| **IsInvalid** (重载2) | `IsInvalid(MainForm, ArrayList[], ArrayList, int who) → bool` | ~70 | Engine.ts.ai出牌合法性检查 | (🔴) 是 — 读 mainForm.currentSendCards |
| **GetNextRank** | `GetNextRank(MainForm, bool success) → void` | ~120 | 计算升级跳级数 | (🔴) 是 — 读/写 mainForm.currentState.Our/OpposedCurrentRank |
| **IsMasterOK** | `IsMasterOK(MainForm, int who) → bool` | ~40 | 最后一把是否护住底 | (🔴) 是 — 读 mainForm.currentState.Master |
| **CalculateNextMaster** | `CalculateNextMaster(MainForm, bool) → int` | ~50 | 计算下任庄家 | (🔴) 是 — 读 mainForm.currentState.Master |
| **GetNextMasterUser** | `GetNextMasterUser(MainForm) → void` | ~110 | **主入口**: 计算一局结束后的一切 | (🔴) 是 — 大量读写 MainForm 字段 |
| **GetNextOrder** | `GetNextOrder(MainForm) → int` | ~130 | **一圈谁最大**: 比较各家出牌 | (🔴) 是 — 读 mainForm.currentSendCards |
| **CalculateScore** | `CalculateScore(MainForm) → void` | ~15 | 累加本局得分 | (🔴) 是 — 写 mainForm.Scores |
| **Calculate8CardsScore** | `Calculate8CardsScore(MainForm, int) → void` | ~10 | 底牌翻倍计算 | (🔴) 是 — 写 mainForm.Scores |
| **CheckSendCards** (重载1) | `CheckSendCards(MainForm, ArrayList minCards, int) → bool` | ~130 | 甩牌检查（读 myCardIsReady） | (🔴) 是 — 读 mainForm.myCardIsReady |
| **CheckSendCards** (重载2) | `CheckSendCards(MainForm, ArrayList, ArrayList, int) → bool` | ~130 | 甩牌检查（传参版） | (🔴) 是 — 读 mainForm.currentPokers |

**结论**: TractorRules 的**所有 public 方法**都依赖 MainForm 引用。没有一个是纯函数。

### 内部 private 方法

```csharp
private static int GetScores(ArrayList list) → int  // 计算 ArrayList 中的得分，纯函数 ✅
```

这是整个 TractorRules 中**唯一**的纯函数方法。`GetScores` 只读 `ArrayList` 不碰 MainForm。

---

## 2. 与出牌合法性 + 一圈胜负判定相关的方法

### 直接相关的 4 个方法

| 方法 | 圈判定相关 | 合法性判定 |
|------|-----------|-----------|
| **GetNextOrder** | ✅ 核心：比较 4 家出牌，出最大者赢 | ❌ 不检查合法性 |
| **IsInvalid** (2个) | ❌ | ✅ 核心：检查跟牌规则 |
| **CheckSendCards** (2个) | ❌ 检查甩牌 | ✅ 核心：首出/甩牌检查 |
| **CalculateScore** | ✅ 算分 | ❌ |

### GetNextOrder 的完整内部逻辑

它并不是简单比单张！它涵盖了拖拉机**完整牌型比较**：

```csharp
// 伪代码
GetNextOrder(MainForm):
    1. 解析 4 家 currentSendCards 为 CurrentPoker
    2. 确定 firstSuit = 首出牌的花色
    3. 根据首出的牌型分类比较:
       if (首出是混合牌 - 拖拉机+单/对+单):
          只比较各家的拖拉机部分
       elif (首出是拖拉机):
          各家的拖拉机部分比大小
       elif (首出是 1 个对 + 单张):
          只比较各家的对子部分
       elif (首出是对子):
          只比较各家的对子部分
       elif (首出是单张):
          各自的第一张牌 compareTo
    4. 返回最大的玩家编号
```

**关键点**：它依赖 `CurrentPoker.GetTractor()`, `CurrentPoker.GetPairs()`, `CurrentPoker.HasTractors()`, `CommonMethods.CompareTo()` 这些方法。其中 GetTractor / GetPairs / HasTractors 都在 CurrentPoker 类中实现，不依赖 MainForm。`CommonMethods.CompareTo` 也不依赖 MainForm（纯数据函数）。

---

## 3. GetNextOrder 改造方案

### 所需数据

- 4 家的出牌列表（ArrayList[] currentSendCards 或等价的 int list）
- 主花色（int suit）
- 级牌（int rank）
- 首出方（int firstSend）
- 首出牌的花色（从 firstSender 出牌列表第一张获得）

所有这些数据都可以从 **GameState** 拿到：`state.CurrentSendCards`, `state.State.Suit/Rank`, `state.FirstSend`。

### 改造方式

在 Engine (Phase A 改造后) 中添加新方法：

```csharp
// 不需要 MainForm引用，只用传入数据
public int ResolveTrickWinner(
    ArrayList[] currentSendCards,  // 或 int[][]
    int suit,
    int rank,
    int firstSend
)
```

逻辑可以直接复制 TractorRules.GetNextOrder 的方法体，把 `mainForm.xxx` 全部替换为参数引用。TractorRules.GetNextOrder 依赖的 CurrentPoker 方法和 CommonMethods.CompareTo 都不需要 MainForm 引用，所以可以**直接移植**。

### (✅) 工作量评估

**GetNextOrder 移植：约 2 小时，修改 ~150 行**
1. 从 TractorRules 复制 GetNextOrder 方法体
2. 改为接收数据参数，去除 MainForm 引用
3. 放进 Engine 或 AlgorithmCore
4. Engine 原 GetNextOrder 改为调用这个方法

---

## 4. 得分计算迁移

### 当前状态

Engine 没有自行计算得分。得分计算分布在：
- `TractorRules.CalculateScore(MainForm)` — 遍历 4 家出牌，累加 5/10/K 分，写入 mainForm.Scores
- `TractorRules.Calculate8CardsScore(MainForm, int howmany)` — 底牌翻倍，写入 mainForm.Scores
- `CommonMethods.GetScores(ArrayList)` — 纯函数计算一个 ArrayList 的得分 ✅

### 改造方案

Engine 中新增纯函数：

```csharp
// 计算本墩得分
public int CalculateTrickScore(ArrayList[] trickCards)
{
    int score = 0;
    for (int i = 0; i < 4; i++)
        score += GetScoreFromArrayList(trickCards[i]);  // 直接调用 CommonMethods.GetScores
    return score;
}

// 或接受 int[][] 纯数据版
public int CalculateTrickScore(int[][] trickCards)
{
    int score = 0;
    foreach (var cards in trickCards)
        foreach (int c in cards)
        {
            int r = c % 13;
            if (r == 4)  score += 5;   // 5
            if (r == 9 || r == 12) score += 10;  // 10/K
        }
    return score;
}
```

**5/10/K 的牌值映射**:
- 5: rank 4 (0-based 索引 4)
- 10: rank 9 (0-based 索引 9)
- K: rank 12 (0-based 索引 12)

### 扣底翻倍规则

TractorRules.GetNextMasterUser 中的规则：
- 无对无拖拉机：×2
- 有对无拖拉机：×4
- 有拖拉机：×8

当前简化版本（engine 的旧代码）仅 `return _firstSend`，没有扣底计分。Phase C 可以不移植扣底（留到后续），**只做每墩得分累加**。

---

## 5. 出牌合法性检查是否搬入 Engine

### 当前调用路径

玩家出牌时：
```
MainForm_MouseClick → 用户点"小猪"按钮
  → TractorRules.IsInvalid(this, currentSendCards, 1)  → 合法性校验
  → TractorRules.CheckSendCards(this, minCards, 0)      → 甩牌检查
  → engine.PlayerPlayCard(1, selectedCards)              → 出牌
```

### 搬入 Engine 的利与弊

**利**:
1. Engine 完全可自验证——给定手牌+出牌+规则，Engine 自己判定是否合法
2. 合法性检查和出牌逻辑在同一处，减少跨类调用
3. AI 出牌也可复用同一套校验

**弊**:
1. Engine 需要知道玩家的完整手牌（当前 Engine 只有出牌列表，没有未出牌列表）
2. IsInvalid 的重载1读 `mainForm.myCardIsReady`（UI 交互状态），这属于 UI 层数据，不应进入 Engine
3. IsInvalid 中用 `mainForm.currentPokers[who-1]` 检查对子数量（"你是否有更多对子"），Engine 需要玩家手牌 CurrentPoker

### (⚠️) 建议：部分搬入

不做全搬。当前 MainForm 中合法性检查调用 2 次 TractorRules 方法后调用 engine.PlayerPlayCard 的模式是合理的。**Phase C 只搬 GetNextOrder + CalculateScore**，合法性检查保留在 MainForm。

---

## 6. AlgorithmCore 能否扩展

### 当前 AlgorithmCore 能力

- `ShouldSetRank(CurrentPoker[], int rank, int user) → int` — 是否应该叫主
- `ShouldSendedCards(params)` — AI 出牌策略
- `MustSendedCards(params)` — AI 必须出牌策略

### 可扩展的

`AlgorithmCore` 当前已经是纯数据模式（不依赖 MainForm）。可以把以下函数加进去：

1. **CompareCards**(int a, int b, int suit, int rank, int firstSuit) → bool
   — 直接调用 `CommonMethods.CompareTo`，它已是纯函数
   — 这是 GetNextOrder 的核心依赖

2. **GetScoreFromArrayList**(ArrayList list) → int
   — 直接调用 `CommonMethods.GetScores` 或 `TractorRules.GetScores`

3. **IsTractorTrick**(int[] cards, int suit, int rank) → bool
   — 纯数据版判断牌型

### 建议

把 CompareCards 和 GetScore 的纯数据入口放进 AlgorithmCore。这样 Engine 调用的依赖均在 core 层，不涉及 MainForm。

---

## 7. 三种策略对比

| 策略 | 改动范围 | 涉及文件 | 预计耗时 | 效果 |
|------|---------|---------|---------|------|
| **最小**：只搬 GetNextOrder | Engine + AlgorithmCore 各 ~150 行 | 2 | 1 天 | Engine 正确判定一圈谁赢 |
| **中等**：+ 得分计算 + 底牌计分 | Engine + AlgorithmCore ~300 行 | 2 | 2 天 | Engine 可独立完成墩结算 |
| **大重构**：合法性检查也搬入 | Engine + TractorRules 重构 ~500 行 | 3-4 | 3-4 天 | Engine 完全自验证 |

### 推荐策略：中等

理由：
1. GetNextOrder 是 Engine 当前最大的简化缺陷（`return _firstSend` 导致输赢不对）
2. 得分计算也很简单（就是加 5/10/K），不搬的话 Engine 还不如不改
3. 合法性检查涉及 UI 层数据（`myCardIsReady`），强行搬入 Engine 反而增加耦合
4. TractorRules.GetNextOrder 已经是完整实现，直接移植无设计风险

---

## 8. 依赖关系：是否需要 Phase A 先完成

### 依赖关系

- **GetNextOrder 移植** → 不依赖 Phase A。可以直接复制 TractorRules.GetNextOrder 的代码，改入参从 MainForm 变为数据参数。这个新函数不访问任何 Engine 内部字段。
- **Engine 内调用 GetNextOrder** → 依赖 Phase A。Phase A 完成后 Engine 的 PlayerPlayCard/GetNextOrder 调用从 `_state.xxx` 改为 `state.xxx`。
- **得分计算** → 部分依赖 Phase A。得分写入新 GameState，而不是 Engine.Scores 属性。

### 执行顺序建议

**Phase A 先做，Phase C 后做**。

但 Phase C 的分析工作（即本文件）可以与 Phase A 并行。等 Phase A 的代码落地后，Phase C 的开发者可以立即开始移植 GetNextOrder。

---

## 选择建议

**推荐采用"中等策略"**：
1. 从 TractorRules.GetNextOrder 复制完整一圈胜负判定逻辑到 Engine（含拖拉机/对子/单张的全类型比较）
2. Engine 新增 CalculateTrickScore 纯函数
3. 把 CompareCards 和 GetScore 的函数入口加到 AlgorithmCore
4. 合法性检查（IsInvalid/CheckSendCards）保留在原处不动
5. 扣底翻倍计算暂不移植（与 GetNextMasterUser 的完整实现绑定，留到后续）

这样 Engine 可以正确判定每一圈的赢家和得分，但出牌合法性校验仍由 MainForm 调用 TractorRules 完成。
