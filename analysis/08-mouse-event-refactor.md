# 08 · 鼠标事件重构方案：逻辑与渲染分离

> 项目：拖拉机游戏 C# WinForms .NET 4.6.1  
> 范围：MainForm_MouseClick（~150行，409–558）和 MainForm_MouseDoubleClick（~65行，562–624）  
> 目标：逻辑与渲染分离，让 MouseClick 只负责「捕获鼠标位置 → 传递给 Engine → 按 Engine 结果调用 Renderer」

---

## 1. MainForm_MouseClick 完整调用图

```
MainForm_MouseClick(e)
 │
 ├─ [外层守卫] ─ 状态检查 ───────────────────────────────
 │   if (state==WaitingForMySending||WaitingForSending8Cards) && whoseOrder==1
 │
 ├─ 1.1 [左键] ─ 手牌区域点击 ───────────────────────────
 │   ├─ Y边界检查：e.Y ∈ [355, 472)
 │   ├─ X边界检查：e.X ∈ [myCardsLocation[0], myCardsLocation[-1]+71]
 │   └─ CalculateClickedRegion(e, clicks=1)
 │       ├─ Region.IsVisible 判定点击到了哪张牌
 │       ├─ 切换 myCardIsReady[i] = !myCardIsReady[i]  ← 纯UI状态
 │       └─ DrawMyPlayingCards(currentPokers[0])        ← 刷新
 │           └─ Refresh()
 │
 ├─ 1.2 [右键] ─ 连锁选中 ───────────────────────────────
 │   ├─ CalculateRightClickedRegion(e)
 │   │   └─ 返回被点击牌的索引 i
 │   ├─ 向左遍历连续牌（间隔13px），同步开关
 │   ├─ DrawMyPlayingCards(currentPokers[0])             ← 刷新
 │   └─ Refresh()
 │
 ├─ 1.3 [小猪按钮] ─ 出牌/扣牌确认 ─────────────────────
 │   Region pigRect = (296, 300, 53, 46).IsVisible(e.X, e.Y)
 │   │
 │   ├─ 1.3.1 [扣牌阶段] WaitingForSending8Cards ────────
 │   │   ├─ 遍历 myCardIsReady，收集已选中牌的 myCardsNumber
 │   │   ├─ readyCards.Count == 8？
 │   │   │   ├─ [是] for i=0..7: CommonMethods.SendCards() ×8
 │   │   │   │       SendCards 做的事：
 │   │   │   │         sends.Add(number)
 │   │   │   │         cp.RemoveCard(number)     ← 从 CurrentPoker 删牌
 │   │   │   │         pokerList.Remove(number)  ← 从原始列表删牌
 │   │   │   ├─ initSendedCards()
 │   │   │   │         ← 重新解析4个玩家手牌（CommonMethods.parse ×4）
 │   │   │   └─ currentState.CurrentCardCommands = DrawMySortedCards
 │   │   └─ [否] 无任何操作
 │   │
 │   ├─ 1.3.2 [出牌阶段] WaitingForMySending ───────────
 │   │   ├─ TractorRules.IsInvalid(this, currentSendCards, 1)
 │   │   │   └─ 检查选中的牌是否合法（花色对、拖拉机规则、混牌等）
 │   │   │   └─ 返回 false=不合法（不往下走）, true=合法
 │   │   │
 │   │   ├─ [首次出牌] firstSend == 1 ──────────
 │   │   │   ├─ whoIsBigger = 1
 │   │   │   ├─ TractorRules.CheckSendCards(this, minCards, 0)
 │   │   │   │   └─ 甩牌检查：判断是否能全部一次性打出
 │   │   │   │   ├─ [返回 true] → 遍历选中牌，SendCards 全部
 │   │   │   │   └─ [返回 false] → 只打 minCards（系统决定的必须出的最小牌）
 │   │   │   └─ currentSendCards[0] = new ArrayList()  (如果 true)
 │   │   │
 │   │   ├─ [非首次出牌] firstSend != 1 ───────
 │   │   │   ├─ currentSendCards[0] = new ArrayList()
 │   │   │   └─ 遍历选中牌，全部 SendCards
 │   │   │
 │   │   ├─ 擦除小猪按钮图像（Graphics.FromImage(bmp) x2）
 │   │   └─ DrawMyFinishSendedCards()
 │   │       ├─ 将自己打出的牌画在屏幕中央
 │   │       ├─ 追加到 currentAllSendPokers[0]
 │   │       ├─ 重新画手牌（DrawMySortedCards）
 │   │       ├─ 刷新得分
 │   │       ├─ 判断是否4人都打完
 │   │       │   ├─ [是] → state = Pause，回合计分
 │   │       │   └─ [否] → whoseOrder = 4，state = WaitingForSend
 │   │       └─ Refresh()
 │   │
 │   └─ [擦除小猪] Graphics.FromImage(bmp).DrawImage(background)
 │
 └─ 1.4 [ReadyCards 阶段] ──────────────────────────────
     ├─ currentState.CurrentCardCommands == ReadyCards
     └─ IsClickedRanked(e)
         └─ 点击工具栏上的5个花色按钮 → 设置 Suit、Master
```

---

## 2. 每个逻辑节点的拆分方案

| 节点 | 当前谁管 | 改后谁管 | 接口设计 |
|------|---------|---------|---------|
| **外层状态守卫** (`if (state==XX) && whoseOrder==1`) | Form | **Form** | 留在 UI 层，是事件路由，不进 Engine |
| **Y/X 边界检查** | Form | **Form** | 纯 UI 坐标检查，不进 Engine |
| **CalculateClickedRegion** (命中检测 → 切换选中) | Form | **Form** | 纯 UI 操作，修改 `myCardIsReady` 数组，不进 Engine |
| **CalculateRightClickedRegion** (右键连锁) | Form | **Form** | 同上，纯 UI |
| **myCardIsReady 切换** | Form | **Form** | 纯 UI 状态，不进 Engine |
| **小猪按钮 Region.IsVisible** | Form | **Form** | 纯 UI 区域检查，不进 Engine |
| **收集 readyCards** (从 myCardIsReady 筛选) | Form | **Form** | 纯数据收集——但需转为标准化格式传递给 Engine |
| **TractorRules.IsInvalid** (出牌合法性) | Form 直接调用 | **Engine** | `Engine.CheckPlayValidity(playerId, selectedCards)` → 返回 `PlayResult { IsValid, ErrorMessage, MinCards[], ... }` |
| **TractorRules.CheckSendCards** (甩牌检查) | Form 直接调用 | **Engine** | `Engine.CheckFirstPlay(playerId, selectedCards)` → 返回 `FirstPlayResult { CanPlayAll, MinCards[] }` |
| **CommonMethods.SendCards** (从手牌移除) | Form 直接调用 | **Engine** | `Engine.ExecutePlay(playerId, selectedCards)` → 内部执行 SendCards 逻辑，返回 `PlayExecResult { UpdatedHands[], NewState, RenderCommands[] }` |
| **currentSendCards[0] = new ArrayList()** | Form | **Engine**（内部管理） | Engine 内部维护回合牌局状态 |
| **initSendedCards()** (重新解析4家手牌) | Form 直接调用 | **Engine** | `Engine.Reinitialize()` 或由 `ExecutePlay` 内部自动同步 |
| **whoIsBigger = 1** (标志首出) | Form | **Engine** | Engine 内部管理首出状态 |
| **DrawMyPlayingCards** (刷新手牌) | Form → DrawingFormHelper | **Renderer** | `renderer.DrawHand(hand, selectedFlags)` |
| **擦除小猪按钮** (Graphics.FromImage(bmp)) | Form | **Renderer** | `renderer.ClearActionButton()` |
| **DrawMyFinishSendedCards** (打完牌动画+刷新手牌+更新状态) | Form → DrawingFormHelper | **Renderer** | `renderer.RenderPlayResult(result)` — 根据 Engine 返回渲染所需全部画面 |
| **设置 currentState.CurrentCardCommands** | Form | **Engine**（返回新状态） | Engine 返回的 `NewCardCommand` 或 `NewState`，Form 读取后 `currentState.CurrentCardCommands = result.NewCommand` |
| **IsClickedRanked** (叫主) | Form → DrawingFormHelper | **Renderer** | `renderer.HandleRankClick(e)` — 实际上叫主包含 UI 和逻辑，需拆：逻辑部分进 Engine（`Engine.SetSuit`），渲染部分进 Renderer |
| **Refresh()** | Form | **Form** | 留在 Form，Refresh 是 WinForms 框架调用 OnPaint |

### 核心原则总结

```
click event → Form: 点击检测 → Form: 收集参数 → Engine: 处理逻辑
    → Engine: 返回结果(新状态+渲染指令) → Form: 更新状态 → Renderer: 执行渲染
```

Form 层始终只做三件事：
1. **路由**：判断点击位置、状态，决定走哪条路径
2. **参数收集**：整理需要传递给 Engine 的结构化数据（不传递 Form 内部对象如 `myCardIsReady`，而是传 `List<int> selectedCardIds`）
3. **结果转发**：接收 Engine 返回的结果，更新状态，调用 Renderer

---

## 3. 事务性问题

### 3.1 当前代码中是否有"半修改"风险？

**有。** 当前代码存在明显的原子性缺口：

#### 风险点 1：SendCards 循环中途失败
```csharp
// MouseClick 扣牌阶段 (467-490)
ArrayList readyCards = new ArrayList();
for (int i = 0; i < myCardIsReady.Count; i++)
{
    if ((bool)myCardIsReady[i])
        readyCards.Add((int)myCardsNumber[i]);
}

if (readyCards.Count == 8)
{
    send8Cards = new ArrayList();
    for (int i = 0; i < 8; i++)
    {
        // ⚠️ 如果第4次调用 SendCards 时 hand 不一致，cp.RemoveCard 抛异常
        CommonMethods.SendCards(send8Cards, currentPokers[0], pokerList[0], (int)readyCards[i]);
    }
    initSendedCards();
    currentState.CurrentCardCommands = CardCommands.DrawMySortedCards;
}
```

`SendCards` 做了三个操作：`sends.Add` + `cp.RemoveCard` + `pokerList.Remove`。如果 `readyCards[i]` 不在 `currentPokers[0]` 或 `pokerList[0]` 中，`RemoveCard` 或 `pokerList.Remove` 可能：
- 不执行删除（静默失败）
- 抛 IndexOutOfRangeException / ArgumentException

**如果抛异常**：程序崩溃，部分牌已移出但部分未移出，`initSendedCards()` 不执行，状态不更新 → 游戏状态损坏。

#### 风险点 2：出牌阶段多层条件嵌套
```csharp
// MouseClick 出牌阶段 (497-551)
if (TractorRules.IsInvalid(this, currentSendCards, 1))   // ← 只检查，不修改
{
    if (firstSend == 1)
    {
        whoIsBigger = 1;
        if (TractorRules.CheckSendCards(this, minCards,0)) // ← 只检查，不修改
        {
            currentSendCards[0] = new ArrayList();
            for (...) { CommonMethods.SendCards(...); }      // ← 实际修改
        }
        else
        {
            for (...) { CommonMethods.SendCards(...); }      // ← 实际修改
        }
    }
    else
    {
        currentSendCards[0] = new ArrayList();
        for (...) { CommonMethods.SendCards(...); }
    }
    drawingFormHelper.DrawMyFinishSendedCards();            // ← 渲染+状态更新
}
```

流程是：**校验 → 出牌 → 渲染**。校验通过后，出牌过程中如果失败，游戏状态已修改一半。

#### 风险点 3：`currentSendCards[0]` 清理
出牌前统一执行 `currentSendCards[0] = new ArrayList()`，覆盖了上次出牌数据。如果在执行 `SendCards` 之前崩了，`currentSendCards[0]` 已经被清空。

### 3.2 现在是怎么办的？

**没有任何事务保护。** 当前代码依赖：
- `SendCards` 通常不会因"牌不存在"而抛异常（因为 `myCardsNumber` 和 `currentPokers[0]` 是同步的）
- 但没有任何 try-catch / 事务回滚机制
- 程序崩溃时，状态就永远坏了（无持久化，无法恢复）

### 3.3 拆分后如何保证一致性

#### 方案：Engine.PlayerPlayCard 单方法原子性

建议将鼠标点击触发业务逻辑的全部操作合并为 **一个 Engine 方法**：

```csharp
class PlayCardResult
{
    public bool Success { get; set; }
    public string ErrorMessage { get; set; }
    public CardCommands NewCommand { get; set; }
    public int NewWhoseOrder { get; set; }
    public Dictionary<int, CurrentPoker> UpdatedHands { get; set; }
    // 渲染指令
    public List<RenderCommand> RenderCommands { get; set; }
}

class Engine
{
    public PlayCardResult PlayerPlayCard(int playerId, List<int> selectedCardIds)
    {
        // 1. 快照当前状态（用于回滚）
        var snapshot = TakeSnapshot();
        
        try
        {
            // 2. 合法性检查
            if (!CheckPlayValidity(playerId, selectedCardIds, out var errorMsg))
            {
                return new PlayCardResult { Success = false, ErrorMessage = errorMsg };
            }

            // 3. 甩牌检查（仅首出）
            if (_firstSend == playerId)
            {
                if (!CheckFirstPlay(playerId, selectedCardIds, out var minCards))
                {
                    selectedCardIds = minCards;  // 系统选定最小牌
                }
            }

            // 4. 单次原子执行出牌
            ExecutePlay(playerId, selectedCardIds);

            // 5. 判断该谁出、是否该回合结束
            var nextState = ComputeNextState(playerId, selectedCardIds);
            
            return new PlayCardResult
            {
                Success = true,
                NewCommand = nextState.Command,
                NewWhoseOrder = nextState.WhoseOrder,
                UpdatedHands = GetCurrentHands(),
                RenderCommands = ComputeRenderCommands(playerId, selectedCardIds, nextState)
            };
        }
        catch (Exception)
        {
            // 6. 回滚到快照
            Rollback(snapshot);
            throw;  // 或返回失败结果
        }
    }
}
```

#### 为什么单方法优于多调用？

| 方案 | 好 | 不好 |
|------|----|------|
| **多调用**：`CheckValidity → ExecutePlay → ...` | 灵活，可组合 | 非事务性：调用之间 Form 可能修改状态、崩在半路 |
| **单方法**：`PlayerPlayCard` 返回所有信息 | **原子性保证**：内部事务（失败回滚） | 方法签名可能变长 |

**推荐单方案**：Engine 内部维护完整副本 (`pokerList`, `currentPokers`, `currentSendCards`, 等)，`PlayerPlayCard` 内部执行所有操作并在失败时回滚。

### 3.4 initSendedCards 的事务问题

`initSendedCards()` 用 `CommonMethods.parse` 重新从原始 `pokerList` 解析 4 个玩家的 `CurrentPoker`。如果 SendCards 时序不正确（比如已经 RemoveCard 但还没 Add 到正确玩家），`parse` 的结果可能不一致。

**改造后**：Engine 内部维护 `pokerList` 数组，在 `ExecutePlay` 执行过程中保持一致性。Engine 不再暴露 `initSendedCards` 给外部调用，而是内部自动在每次出牌后同步。

---

## 4. 建议的新 MouseClick 伪代码

```csharp
// ====== 改造后的 MouseClick ======
private void MainForm_MouseClick(object sender, MouseEventArgs e)
{
    // ---------- 阶段 0：守卫 ----------
    var currentPhase = currentState.CurrentCardCommands;
    if (!IsMyPlayPhase(currentPhase) || whoseOrder != 1)
        return;

    // ---------- 阶段 1：UI 交互（不上 Engine）----------
    
    // 1.1 左键 → 选中/取消选中
    if (e.Button == MouseButtons.Left && IsInHandArea(e))
    {
        if (calculateRegionHelper.CalculateClickedRegion(e, 1))
        {
            renderer.DrawHand(currentPokers[0], myCardIsReady);
            Refresh();
        }
        return;
    }

    // 1.2 右键 → 连锁选中
    if (e.Button == MouseButtons.Right)
    {
        int i = calculateRegionHelper.CalculateRightClickedRegion(e);
        if (i >= 0)
        {
            PropagateSelectionLeft(i);
            renderer.DrawHand(currentPokers[0], myCardIsReady);
            Refresh();
        }
        return;
    }

    // ---------- 阶段 2：业务操作（上 Engine）----------
    if (!IsPigButtonArea(e))
        return;

    // 阶段 2.1：叫主阶段 → 走 Renderer
    if (currentPhase == CardCommands.ReadyCards)
    {
        renderer.HandleRankClick(e);
        return;
    }

    // 阶段 2.2：扣牌/出牌 → 走 Engine
    List<int> selectedCardIds = CollectSelectedCardIds(myCardIsReady, myCardsNumber);

    if (currentPhase == CardCommands.WaitingForSending8Cards)
    {
        // 扣牌
        if (selectedCardIds.Count != 8)
        {
            renderer.ShowMessage("请选择8张底牌");
            return;
        }
        var result = engine.PlayerHoldBottomCards(/*playerId=*/1, selectedCardIds);
        if (result.Success)
        {
            currentState.CurrentCardCommands = result.NewCommand;
            renderer.ClearActionButton();
            renderer.DrawSortedHand(result.UpdatedHands[0]);
        }
        else
        {
            renderer.ShowMessage(result.ErrorMessage);
        }
    }
    else if (currentPhase == CardCommands.WaitingForMySending)
    {
        // 出牌
        var result = engine.PlayerPlayCard(/*playerId=*/1, selectedCardIds);
        if (result.Success)
        {
            // 更新 Form 状态（只从 result 同步 Form 关心的元数据）
            currentState.CurrentCardCommands = result.NewCommand;
            whoseOrder = result.NewWhoseOrder;
            whoIsBigger = result.WhoIsBigger;
            // firstSend 由 Engine 内部管理

            // 引擎内部状态已更新，Form 不再维护 cardList/pokerList 副本
            // 交由 Renderer 统一渲染
            renderer.ClearActionButton();
            foreach (var cmd in result.RenderCommands)
            {
                renderer.Execute(cmd);
            }
            Refresh();
        }
        else
        {
            renderer.ShowMessage(result.ErrorMessage);
        }
    }
}
```

### 关键变化总结

| 方面 | 改造前 | 改造后 |
|------|--------|--------|
| Engine 交互 | Form 直接调用多个函数（IsInvalid, CheckSendCards, SendCards, initSendedCards） | Form 调用一次 `engine.PlayerPlayCard()`，Engine 内部完成所有逻辑 |
| Form 数据维护 | `pokerList[]`, `currentPokers[]`, `currentSendCards[]`, `send8Cards`, `whoseOrder`, `firstSend`, `whoIsBigger` 全在 Form 维护 | Form 只持有 `currentState` 和 `renderer`；Engine 内部维护完整游戏状态 |
| 渲染 | 各处散落 `Graphics.FromImage(bmp)`、`g.DrawImage()` | 集中到 `Renderer.Execute()`，Engine 返回指令，Renderer 执行 |

---

## 5. MainForm_MouseDoubleClick

### 5.1 双击缺少 `whoseOrder` 状态检查的问题

**问题描述：**

MouseClick 的外层守卫是：
```csharp
if (/*state check*/ && (whoseOrder == 1))
```

MouseDoubleClick 的守卫却是：
```csharp
// 无 outer guard
if (currentPokers[0].Count == 0) return;
// 不检查 whoseOrder
...
if ((currentState.CurrentCardCommands == CardCommands.WaitingForSending8Cards) && (whoseOrder == 1))
// 仅扣牌阶段检查了 whoseOrder
...
else if (currentState.CurrentCardCommands == CardCommands.WaitingForMySending)
// 出牌阶段未检查 whoseOrder！
```

**具体漏洞：** 当 `whoseOrder != 1`（不是我的回合）时，双击仍然会：
1. 执行 `CalculateDoubleClickedRegion` → 修改 `myCardIsReady`（未选中状态的切换，但这里设为了 `true`）
2. 创建新的 `currentSendCards[0]` → **清空上一轮出牌数据！**
3. 进入 `WaitingForMySending` 分支 → 执行出牌逻辑

这会导致：
- 非我的回合时，双击会清空当前回合的出牌记录
- 可能把当前不该出的牌打出去
- 出牌后 `DrawMyFinishSendedCards()` 会推进游戏状态（`whoseOrder=4`），干扰正常 AI 出牌流程

### 5.2 改造后双击是否应和单击走同一个 Engine 方法

**答案是：应该。**

双击和单击在小猪按钮之外的逻辑路径上，本质都是"收集选中牌 + Engine 处理"：

| 阶段 | 单击流程 | 双击流程 |
|------|----------|----------|
| 选中手牌 | `CalculateClickedRegion` → 切换选中 | `CalculateDoubleClickedRegion` → 强行设为 `true` |
| 确认出牌 | 点击小猪按钮 → Engine 处理 | **双击立即触发 Engine 处理** |

区别仅在于：
- **选中机制**：单击切换选中状态，双击直接全选并提交
- **触发方式**：单击需额外点击小猪按钮，双击立即提交

因此改造后的双击伪代码应为：

```csharp
private void MainForm_MouseDoubleClick(object sender, MouseEventArgs e)
{
    // ---- 守卫（与单击一致）----
    var currentPhase = currentState.CurrentCardCommands;
    if (!IsMyPlayPhase(currentPhase) || whoseOrder != 1)
        return;

    if (currentPokers[0].Count == 0)
        return;

    // ---- 命中检测 + 选中该牌 ----
    bool clicked = calculateRegionHelper.CalculateDoubleClickedRegion(e);
    if (!clicked)
        return;

    // ---- 收集所有选中牌 ----
    List<int> selectedCardIds = CollectSelectedCardIds(myCardIsReady, myCardsNumber);

    // ---- 走 Engine（与单击小猪按钮完全一致）----
    // 扣牌阶段
    if (currentPhase == CardCommands.WaitingForSending8Cards)
    {
        if (selectedCardIds.Count != 8)
        {
            renderer.ShowMessage("请选择8张底牌（双击选中8张触发）");
            return;
        }
        var result = engine.PlayerHoldBottomCards(1, selectedCardIds);
        if (result.Success)
        {
            currentState.CurrentCardCommands = result.NewCommand;
            renderer.ClearActionButton();
            renderer.DrawSortedHand(result.UpdatedHands[0]);
            Refresh();
        }
        // 不成功则不操作（让用户重新选）
        return;
    }

    // 出牌阶段
    var playResult = engine.PlayerPlayCard(1, selectedCardIds);
    if (playResult.Success)
    {
        currentState.CurrentCardCommands = playResult.NewCommand;
        whoseOrder = playResult.NewWhoseOrder;
        renderer.ClearActionButton();
        foreach (var cmd in playResult.RenderCommands)
        {
            renderer.Execute(cmd);
        }
        Refresh();
    }
    else
    {
        renderer.ShowMessage(playResult.ErrorMessage);
    }
}
```

### 5.3 改造方案总结

| 问题 | 改造方案 |
|------|----------|
| 缺少 `whoseOrder` 守卫 | 双击守卫直接复用单击的守卫逻辑 |
| `currentSendCards[0] = new ArrayList()` 清空上轮数据 | Form 不直接维护 `currentSendCards`，由 Engine 管理 |
| 双击与单击逻辑重复 | 双击调用同一 `Engine.PlayerPlayCard()` / `Engine.PlayerHoldBottomCards()` 方法 |
| 双击直接清空小猪按钮区域 | `renderer.ClearActionButton()` 统一清理（Engine 返回渲染指令后再执行） |

---

## 附录 A：Engine 接口建议

```csharp
/// <summary>
/// Engine 接口 —— 游戏逻辑引擎，不依赖 WinForms/GDI+。
/// 内部维护完整游戏状态（pokerList, currentPokers, currentSendCards 等）。
/// </summary>
interface ITractorEngine
{
    /// <summary>玩家出牌：检查合法性 → 执行出牌 → 计算后续状态</summary>
    PlayCardResult PlayerPlayCard(int playerId, List<int> selectedCardIds);

    /// <summary>玩家扣底牌：从手牌移除8张牌作为底牌</summary>
    PlayCardResult PlayerHoldBottomCards(int playerId, List<int> selectedCardIds);

    /// <summary>获取当前所有玩家的手牌（供 Renderer 首次绘制和同步）</summary>
    Dictionary<int, CurrentPoker> GetCurrentHands();

    /// <summary>获取当前的出牌回合数据</summary>
    RoundState GetRoundState();
}

class PlayCardResult
{
    public bool Success { get; set; }
    public string ErrorMessage { get; set; }
    public CardCommands NewCommand { get; set; }
    public int NewWhoseOrder { get; set; }
    public int WhoIsBigger { get; set; }
    public List<RenderCommand> RenderCommands { get; set; }
}

/// <summary>渲染指令：Renderer 消费，Engine 产生</summary>
class RenderCommand
{
    public RenderType Type { get; set; } // DrawHand, ClearButton, DrawPlayedCards, etc.
    public Dictionary<string, object> Params { get; set; }
}
```

## 附录 B：改造后文件结构

```
Tractor.net/
├── MainForm.cs                      # Form 层（事件路由 + UI状态管理）
├── Engine/
│   ├── ITractorEngine.cs            # Engine 接口
│   ├── TractorEngine.cs             # Engine 实现（游戏状态管理）
│   └── Models/
│       ├── PlayCardResult.cs        # 出牌结果
│       └── RenderCommand.cs         # 渲染指令
├── Renderer/
│   ├── IRenderer.cs                 # Renderer 接口
│   ├── GdiRenderer.cs               # GDI+ 实现（Graphics + Bitmap）
│   └── Helpers/
│       ├── DrawingFormHelper.cs     # 现有代码，可逐步迁入 Renderer
│       └── CalculateRegionHelper.cs # 现有代码，保留
├── Helpers/                         # 现有，保留
└── Algorithms/                      # 现有，TractorRules 等逻辑迁入 Engine
```
