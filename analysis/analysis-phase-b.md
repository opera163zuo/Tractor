# Phase B GdiRenderer 全覆盖 — 影响分析

**文件**: Tractor.net/Renderers/GdiRenderer.cs, Tractor.net/Helpers/DrawingFormHelper.cs, Tractor.net/Helpers/LayoutManager.cs
**改造目标**: 让 GdiRenderer 独立处理所有渲染指令，不再需要 FallbackRender，逐步迁移 DrawingFormHelper 方法到 GdiRenderer

---

## 1. GdiRenderer.Execute 当前覆盖情况

### handled=true（GdiRenderer 直接支持的 RenderCmdType）

| RenderCmdType | 对应方法 | 状态 |
|--------------|---------|------|
| RedrawAll | DrawBackground | ✅ 正常工作 |
| DealCard | DrawDealCard | ✅ 正常工作 |
| RedrawMyHand | DrawMySortedCards | ⚠️ 函数体为空（只画了 if-判断，实际绘制走 Fallback） |
| ShowToolbar | DrawToolbar | ✅ 正常工作 |
| ShowPassImage | DrawPassImage | ✅ 正常工作 |
| ShowBottomCards | DrawBottomCards | ✅ 正常工作 |

### handled=false（通过 FallbackRender 委托回 DrawingFormHelper）

| RenderCmdType | 回退到 DrawingFormHelper 的方法 |
|--------------|----------------------------------|
| DrawCenter8 | drawingFormHelper.DrawCenter8Cards() |
| DrawPlayedCards | 根据 PlayerId 分发到 DrawMyFinishSendedCards / DrawFrieldUserSendedCards / DrawPreviousUserSendedCards / DrawNextUserSendedCards |
| ShowRoundWinner | drawingFormHelper.DrawFinishedOnceSendedCards() |
| ShowRankResult | drawingFormHelper.DrawFinishedScoreImage() |

### 完全不处理的 RenderCmdType

| RenderCmdType | 说明 |
|--------------|------|
| None | 无操作 |
| SetPause | 无操作（暂停逻辑在 Engine 里处理） |
| AiPlayCard | 无操作（AI 出牌由 Engine 决策，MainForm 处理绘制） |
| WaitingForPlayerAction | 无操作（等待玩家操作的 UI 反馈，由 MainForm 处理） |

---

## 2. DrawingFormHelper 方法分组

DrawingFormHelper.cs 共 2,427 行，可拆为 6 组：

### 第 1 组：手牌渲染（约 320 行）

| 方法 | 行数 | 依赖的 MainForm 字段 |
|------|------|---------------------|
| DrawMySortedCards | ~150 | currentPokers[0], pokerList[0], myCardsLocation, myCardsNumber, currentRank, currentState.Suit/Master |
| DrawMyFinishSendedCards | ~80 | currentSendCards[0], myCardsNumber, myCardsLocation, currentState.Suit, currentRank |
| DrawMyOneOrTwoCards | ~50 | currentPokers[0], showSuits, whoShowRank, currentState.Suit, currentRank |
| DrawMeMaster | ~40 | currentState.Suit/Master, currentRank |

**MainForm 依赖**: currentPokers[0] → GameState.CurrentPokers 已有 ✅
myCardsLocation/myCardsNumber → 未在 GameState 中，这些是 UI 坐标缓存，不应进 GameState

### 第 2 组：对手出牌渲染（约 200 行）

| 方法 | 行数 | 依赖的 MainForm 字段 |
|------|------|---------------------|
| DrawFrieldUserSendedCards | ~50 | currentSendCards[1], currentState.Suit, currentRank |
| DrawPreviousUserSendedCards | ~50 | currentSendCards[2], currentState.Suit, currentRank |
| DrawNextUserSendedCards | ~50 | currentSendCards[3], currentState.Suit, currentRank |
| DrawOtherMaster | ~50 | currentState.Suit/Master |

**MainForm 依赖**: currentSendCards → GameState.CurrentSendCards 已有 ✅

### 第 3 组：亮主/叫主（约 500 行）

| 方法 | 行数 | 依赖的 MainForm 字段 |
|------|------|---------------------|
| DoRankOrNot(🔴) | ~200 | currentPokers, showSuits, whoShowRank, currentState.Suit/Rank, currentRank, timer |
| DrawRankCards | ~80 | showSuits, whoShowRank, currentState.Suit |
| DrawSuitCards | ~80 | currentState.Suit, showSuits |
| IsClickedRanked | ~70 | showSuits, whoShowRank, currentState.Suit, mouse coords |
| CallDoRankOrNot | ~70 | currentPokers, showSuits, currentState.Suit, currentRank |

(🔴) **DoRankOrNot 同时修改状态和渲染**：它不仅绘图，还修改 currentState.Suit、showSuits、whoShowRank 等字段，并调用 engine.SyncRank。这是 Phase B 最大的障碍。

### 第 4 组：牌局结果（约 250 行）

| 方法 | 行数 | 依赖的 MainForm 字段 |
|------|------|---------------------|
| DrawFinishedOnceSendedCards | ~120 | currentSendCards, firstSend, currentState.Suit/Master, whoIsBigger, currentRank |
| DrawFinishedScoreImage | ~80 | Scores, currentState.Suit/OurCurrentRank/OpposedCurrentRank/Master |
| DrawScoreImage | ~50 | Scores, currentState.Master |

**MainForm 依赖**: currentSendCards → GameState 有；whoIsBigger/firstSend → GameState 目前没有这些字段，需要加

### 第 5 组：底牌/工具栏（约 150 行）

| 方法 | 行数 | 依赖的 MainForm 字段 |
|------|------|---------------------|
| DrawBottomCards | ~40 | send8Cards |
| DrawToolbar | ~40 | currentState.Suit |
| DrawSuit | ~40 | currentState.Suit |
| WriteMessage | ~30 | (纯参数) |

**MainForm 依赖**: send8Cards → GameState.Send8Cards 已有 ✅

### 第 6 组：辅助函数（约 180 行）

| 方法 | 行数 | 依赖的 MainForm 字段 |
|------|------|---------------------|
| DrawBackground | ~40 | bmp, image, currentState.Master |
| DrawSidebar | ~50 | bmp, image |
| DrawMaster | ~40 | currentState.Master/Suit |
| DrawRank | ~30 | currentState.OurCurrentRank/OpposedCurrentRank |
| GetCardImage | ~20 | cardsImages[] |

**MainForm 依赖**: 读位图类资源，大部分已是纯函数 ✅

### 其他（约 700 行）

包括发牌动画 ReadyCards(~150)、DrawCenterAllCards(~50)、DrawCenterImage(~40)、DrawCenter8Cards(~150)、Get8Cards(~80)、DrawPassImage(~50)、各种杂项 Draw 方法(~180)。这些涉及发牌阶段的渲染与动画逻辑。

---

## 3. 每组方法依赖的 MainForm 字段能否从 GameState 拿到

| 组 | 需要的字段 | GameState 已有? | 需要扩展? |
|----|-----------|----------------|-----------|
| 1(手牌) | CurrentPokers[0], CurrentRank, State.Suit/Master | CurrentPokers ✅ | 不需要扩展 |
| 2(对手) | CurrentSendCards[1-3], State.Suit/Master | CurrentSendCards ✅ | 不需要扩展 |
| 3(亮主🔴) | showSuits, whoShowRank, State.Suit/Master | State.Suit/Master ✅ | 需要加 ShowSuits, WhoShowRank |
| 4(牌局) | Scores, whoIsBigger, firstSend | Scores ✅ | 需要加 WhoIsBigger, FirstSend |
| 5(底牌) | Send8Cards | Send8Cards ✅ | 不需要扩展 |
| 6(辅助) | CurrentRank, State.Suit/Master | ✅ | 不需要扩展 |

### 需要扩展 GameState 的字段

```csharp
// 第 3 组亮主/叫主需要
public int ShowSuits { get; set; }       // 亮牌次数 0/1/2/3
public int WhoShowRank { get; set; }     // 谁亮的牌 0=无人, 1=我...

// 第 4 组牌局结果需要
public int WhoIsBigger { get; set; }     // 当前圈谁最大
public int FirstSend { get; set; }       // 一圈中谁先出
```

这些字段在 Engine 的 Phase A 改造后也从 GameState 走。

---

## 4. LayoutManager 覆盖检查

### LayoutManager 已有常量（共 24 个）

手牌区坐标、小猪按钮、底牌区、中心牌动画区、中心背景区、花色工具栏、牌间距

### 缺少的常量（需要从 DrawingFormHelper 提取）

| 常量 | DrawingFormHelper 中的硬编码值 | 说明 |
|------|-------------------------------|------|
| OtherPlayerCardsY | (各家手牌的 Y 轴偏移) | 对家/上家/下家牌的位置 |
| SidebarX/Y | 43, 88 | 侧栏显示花色的大图标 |
| ScoreAreaX/Y | 485, 128 或 85, 300 | 得分图标位置（分庄家非庄家） |
| PassImageRect | 110, 150, 400, 199 | Pass 图片位置 |
| CenterAllCardsX/Y | 220, 240 | 发牌阶段的叠牌区 |
| RankImageX/Y | 多个位置 | 级牌标识显示位置 |
| MasterLabelX/Y | 多个位置 | 庄家标识显示位置 |

**建议**：迁移每组方法时，顺便把对应的坐标常量从 DrawingFormHelper 的硬编码搬到 LayoutManager。

---

## 5. 分步迁移方案

### 推荐顺序（从易到难，从独立到耦合）

#### 第 1 批 — 纯渲染迁移（约 400 行，2-3 小时）
迁移完全不涉及状态修改、只读 GameState 的方法。

- DrawBottomCards → GdiRenderer.DrawBottomCards（40 行）✅ 已有但需要验证
- DrawBackground → GdiRenderer.DrawBackground（40 行）✅ 已有
- DrawSidebar, DrawMaster, DrawRank → GdiRenderer（120 行）新建
- WriteMessage → GdiRenderer（30 行）新建
- GetCardImage → GdiRenderer.GetCardImageFunc（20 行）已委托
- DrawToolbar, DrawSuit, DrawScoreImage → 从 Fallback 提到直接处理（130 行）

**验证**：编译通过，渲染结果与原版肉眼一致。

#### 第 2 批 — 对手出牌 + 牌局结果迁移（约 450 行，3-4 小时）
不需要等待用户操作，纯根据 currentSendCards 绘制。

- DrawFrieldUserSendedCards, DrawPreviousUserSendedCards, DrawNextUserSendedCards → GdiRenderer（150 行）
- DrawFinishedOnceSendedCards → GdiRenderer（120 行）
- DrawFinishedScoreImage → GdiRenderer（80 行）
- DrawScoreImage → GdiRenderer（50 行）
- 配套坐标常量搬到 LayoutManager

**验证**：走一局 AI vs AI 对局，截图对比前后渲染一致。

#### 第 3 批 — 手牌渲染 + 发牌动画（约 500 行，3-4 小时）
依赖部分 GameState 字段，但逻辑简单（只画，不改）。

- DrawMySortedCards → GdiRenderer（150 行）⚠️ 需要额外数据：card X 坐标列表
- DrawMyFinishSendedCards → GdiRenderer（80 行）
- DrawMyOneOrTwoCards → GdiRenderer（50 行）
- ReadyCards 发牌动画 → GdiRenderer（150 行）
- DrawCenterAllCards, DrawCenterImage → GdiRenderer（90 行）
- DrawCenter8Cards → GdiRenderer（150 行）

**验证**：整局流程的手牌渲染、发牌动画对齐。

#### 第 4 批 — 亮主/叫主（约 500 行，4-5 小时）
最难，因为 DoRankOrNot 混合了状态修改（叫主）和渲染。

**改造方案**：
1. 将 DoRankOrNot 拆为两个方法：
   - `RankOrNotLogic(GameState, mouseX, mouseY) → (bool suitChanged, int newSuit)` — 纯逻辑，决定是否叫主
   - `DrawRankOrNotUI(GameState, Bitmap)` — 纯渲染，画叫主界面
2. 将 IsClickedRanked 改为纯坐标判定函数，移入 CalculateRegionHelper
3. CallDoRankOrNot（AI 叫主）搬到 AlgorithmCore 或保留在 MainForm

**验证**：亮主/叫主的全部 4 种情况（玩家亮/AI亮/翻底牌/流局）都能走通。

---

## 6. 风险点

(🔴) **DoRankOrNot 混合状态修改和渲染**：这是最大的风险点。它同时做三大类事：
   1. 修改 showSuits / whoShowRank / currentState.Suit（状态修改）
   2. 调用 engine.SyncRank （引擎同步）
   3. 画图（渲染到 bmp）
   
   改造时必须将 1+2 与 3 分离，否则 GdiRenderer 就变成了"既渲染又修改状态"的混合体，违背分离原则。

(🔴) **FallbackRender 删除后 DrawingFormHelper 能否独立编译**：DrawingFormHelper 的构造函数现在被 MainForm 调用。所有 FallbackRender 依赖的方法被搬走后，DrawingFormHelper 类可以保留或删除。建议保留为 @Obsolete 方法直到所有调用删除。

(⚠️) **GdiRenderer 当前通过 State 字段访问数据**：GdiRenderer 有 public GameState State 属性，在 Execute 前由外部设置。如果多个调用之间 state 变化，Renderer 可能拿到旧数据。建议改为参数传递模式。

(⚠️) **双缓冲 Bitmap 的绘制顺序依赖**：DrawingFormHelper 方法之间有隐含的绘制顺序（先画背景、再画牌、再画高亮）。需要保持这些顺序。

---

## 7. 工作量估算

| 批次 | 迁移行数 | 修改文件数 | 预计耗时 | 
|------|---------|-----------|---------|
| 第 1 批：纯渲染 | ~400 | 3 (GdiRenderer, DrawingFormHelper, LayoutManager) | 2-3 小时 |
| 第 2 批：对手出牌+牌局 | ~450 | 3 | 3-4 小时 |
| 第 3 批：手牌+发牌 | ~500 | 3 | 3-4 小时 |
| 第 4 批：亮主叫主 | ~500 | 4 (含 CalculateRegionHelper) | 4-5 小时 |
| **合计** | **~1,850** | **4-5** | **2-3 天** |

---

## 批次划分建议

建议按 **第1批 → 第2批 → 第3批 → 第4批** 的顺序执行。第 4 批（亮主/叫主）放在最后，因为它需要先拆逻辑/渲染，而其他三批都是纯渲染迁移。

每批独立可验证：编译通过 + 肉眼对照渲染结果。第 1-3 批不改变游戏行为，第 4 批需要完整的叫主用例测试。

与 Phase A 的关系：Phase A 需在第 3 批之前完成（因为手牌渲染依赖 GameState 中的 CurrentPokers 字段），但可以先并行做第 1-2 批。
