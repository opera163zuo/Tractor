# 布局系统分析文档

> 分析日期：2026-05-12
> 分析文件：
>   - `Tractor.net/Helpers/CalculateRegionHelper.cs`（202行）
>   - `Tractor.net/Helpers/DrawingFormHelper.cs`（2355行）
>   - `Tractor.net/MainForm.cs`（1398行）

---

## 目录

1. [CalculateRegionHelper 方法分析](#1-calculateregionhelper-方法分析)
2. [所有布局常量清单](#2-所有布局常量清单)
3. [鼠标事件处理流程](#3-鼠标事件处理流程)
4. [渲染与命中检测的边界分析](#4-渲染与命中检测的边界分析)
5. [LayoutManager 职责范围建议](#5-layoutmanager-职责范围建议)
6. [发现的 Bug/不一致性](#6-发现的-bug不一致性)

---

## 1. CalculateRegionHelper 方法分析

### 1.1 `CalculateClickedRegion(MouseEventArgs e, int clicks)` [CAL-REG-L42-79]

**功能：** 判断鼠标点击是否落在某张牌的可见区域，并切换该牌的选中状态。

**算法步骤：**

1. **为每张牌创建初始 Region** — 遍历 `myCardsLocation`，对每张牌创建一个 71×96 的矩形 Region。
   - 选中（ready）状态：Y=355
   - 未选中状态：Y=375
   - Region 的 X 坐标来自 `myCardsLocation` 列表
   - 见第28-37行

2. **可见区域排除算法** — 由于牌与牌之间只有 13px 间距（新牌的左边缘），而牌宽 71px，后面的牌会遮挡前面牌的大部分区域。每张牌实际**可见**的部分只有最左侧的 `71 - 5×13 = 6px` 窄条。排除逻辑分两段：
   - **前 N-5 张牌**（第45-51行）：对第 i 张牌，排除其右侧 5 张牌（i+1 到 i+5）的 Region。因为需要检查的后续牌 >= 5，可以批量排除。
   - **最后 5 张牌**（第55-65行）：使用嵌套循环两两排除，因为最后 5 张牌没有足够多的后续牌（不足 5 张）。

3. **命中检测**（第68-77行）：遍历所有 Region，调用 `region.IsVisible(e.X, e.Y)`。
   - clicks=1：切换选中状态
   - clicks=2：强制设为选中

**关键公式**：每张牌可见宽度 ≈ `71 - 5*13 = 6px`，这是区域排除的物理依据。

### 1.2 `CalculateDoubleClickedRegion(MouseEventArgs e)` [CAL-REG-L88-142]

**功能：** 双击检测，功能与 `CalculateClickedRegion` 几乎相同，区别是：
- 双击始终将牌设为选中状态（`myCardIsReady[i] = true`，第136行）。
- 返回 `bool`，不切换状态。

**代码问题：** 实质是 `CalculateClickedRegion` 在 clicks=2 时的行为副本。**三次重复同一段逻辑**，仅在第68行（click=1）vs 第136行（toggle vs force true）和返回类型上有微小差异。

### 1.3 `CalculateRightClickedRegion(MouseEventArgs e)` [CAL-REG-L146-200]

**功能：** 右键点击检测，返回被点击牌的**索引**。

**与左键的不同：**
- 切换选中状态（第195行）
- 返回牌索引 `i`（而非 `bool`），失败返回 -1

**调用方处理**（MainForm.cs 第436-451行）：
右键点击一张牌后，还会向**左连续**找到同 x 间距的相邻牌，将它们的状态统一为相同值。这是为了在点击一张牌时，同时选中/取消它前面的连锁牌（拖拉机连牌场景）。

---

## 2. 所有布局常量清单

### 2.1 卡牌基础尺寸

| 常量 | 值 | 出现位置 |
|------|-----|---------|
| CardWidth | **71** | 所有三个文件中几乎所有 `DrawImage` 和 `Region` 构造 |
| CardHeight | **96** | 同上 |
| CardSpacing (水平) | **13** | 玩家手牌每张牌之间的 X 间距 |
| CardSelectedOffset | **-20** | 选中牌比未选中牌在 Y 方向高 20px（未选中=375，选中=355） |

### 2.2 四个玩家手牌区域

#### 玩家1（自己，底部）

| 渲染阶段 | 清理矩形 | 命中检测 Y 范围 |
|----------|---------|----------------|
| DrawMyCards（发牌中） | (30, 360, 560, 96) — DWC-L934 | — |
| DrawMySortedCards（整理后） | (30, 355, 600, 116) — DWC-L1004 | — |
| DrawMyPlayingCards（出牌阶段） | (30, 355, 600, 116) — DWC-L1132 | — |
| MouseClick 命中检测 | — | Y ∈ [355, 472) — MFC-L420 |

**起始位置公式**（三处一致：DWC-L944/L1008/L1136）：
```
int start = (int)((2780 - index * 75) / 10);
```
其中 `index` = `currentPokers[0].Count`。

**选中/未选中 Y 偏移：**

| 状态 | CalculateRegionHelper (Region Y) | DrawMyOneOrTwoCards2 (渲染 Y) | DrawMyOneOrTwoCards (渲染 Y) |
|------|------|------|------|
| 未选中 | 375 | 375 (y+20) | 375 |
| 选中 (ready) | 355 | 355 (传参 y) | **360**（叫牌阶段） |
| 偏移量 | 20px | 20px | 15px |

> ⚠️ **不一致**：`DrawMyOneOrTwoCards` 中选中牌 Y=360 不是 355，偏移 15px 而非 20px。

#### 玩家2（对家，上方）

| 属性 | 值 |
|------|-----|
| 清理矩形 | (105, 25, 420, 96) — DWC-L1308 |
| 初始位置 | X = 437 - count×13, Y=25（发牌时） |
| 牌背间距 | X 方向 -13（从右到左排列） |
| 动画起始位 | (400 - count×13, 60) — DWC-L68 |

#### 玩家3（上家，左侧）

| 属性 | 值 |
|------|-----|
| 清理矩形 | (6, 140, 71, 202) — DWC-L1342 |
| 初始位置 | X=6, Y = 145 + count×4（发牌时） |
| 牌背间距 | Y 方向 +4（从上到下排列） |
| 动画起始位 | (50, 160 + count×4) — DWC-L76 |

#### 玩家4（下家，右侧）

| 属性 | 值 |
|------|-----|
| 清理矩形 | (554, 136, 71, 210) — DWC-L1370 |
| 初始位置 | X=554, Y = 241 - count×4（发牌时） |
| 牌背间距 | Y 方向 -4（从下到上排列） |
| 动画起始位 | (520, 220 - count×4) — DWC-L84 |

### 2.3 桌面中央区域

| 区域 | 矩形 | 用途 |
|------|------|------|
| 牌堆区 | (200, 186, 动态宽, 96) — DWC-L163 | 发牌时中央牌堆，宽度 = (count+1)×2 + 71 |
| 中央清理区 | (77, 124, 476, 244) — DWC-L178 | 清除中央场景，用于出牌/底牌/记分覆盖 |
| 发牌底图 | (77, 121, 477, 254) — DWC-L206 | 清除「拿8张底牌」动画覆盖区域 |
| Pass 图片位 | (110, 150, 400, 199) — DWC-L191 | 显示"过"图片 |
| 出牌完毕半透明遮罩 | (77, 124, 476, 244) — DWC-L2209-2210 | 半透明白色 + 白色边框 |

### 2.4 底牌（8张牌，266行起）

```
X = 230 + i×14，i ∈ [0, 7]
Y = 146（第3张牌单独抬高）／186（其余7张）
```

- 8张底牌展示时，第3张牌（索引2）被抬高到 Y=146（DWC-L265），其余在 Y=186（DWC-L269）
- 底牌清理时，X方向覆盖到 `200 + 8×14 + 71 = 383` 左右

### 2.5 工具栏 / 花色按钮区域

| 元素 | 矩形 | 文件-行号 |
|------|------|----------|
| 工具栏背景 | (415, 325, 129, 29) | DWC-L542 |
| 工具栏擦除 | (415, 325, 129, 29) | DWC-L554 |
| 花色按钮 (5个) | (417 + i×25, 327, 25, 25), i∈[0,4] | DWC-L711-716 |
| 花色按钮命中检测 (红桃) | (417, 327, 25, 25) | DWC-L751 |
| 花色按钮命中检测 (黑桃) | (443, 327, 25, 25) | DWC-L785 |
| 花色按钮命中检测 (方块) | (468, 327, 25, 25) | DWC-L819 |
| 花色按钮命中检测 (草花) | (493, 327, 25, 25) | DWC-L854 |
| 花色按钮命中检测 (王牌) | (518, 327, 25, 25) | DWC-L888 |
| 花色按钮渲染常量 | 417 + i×25, 327 | DWC-L711-716 |

> 注意：渲染和命中检测的坐标值完全一致，没有偏差。

### 2.6 侧边栏

| 元素 | 矩形 | 位置 |
|------|------|------|
| 左侧Sidebar | (20, 30, 70, 89) — DWC-L287 | 上家/对家信息区 |
| 右侧Sidebar | (540, 30, 70, 89) — DWC-L288 | 自己/下家信息区 |

### 2.7 庄家标识

| 玩家 | 矩形 | 
|------|------|
| 自家 (1) | (548, 45, 20, 20) |
| 对家 (2) | (580, 45, 20, 20) |
| 上家 (3) | (31, 45, 20, 20) 或 (30, 45, 20, 20) |
| 下家 (4) | (61, 45, 20, 20) 或 (60, 45, 20, 20) |

> 注意：DrawMaster 和 DrawOtherMaster 中玩家3/4的 X 值有 ±1px 的微小不一致。

### 2.8 等级显示区域

| 角色 | 矩形 |
|------|------|
| 自家等级 | (566, 68, 20, 20) — DWC-L383 |
| 对家等级 | (46, 68, 20, 20) — DWC-L384 |
| Sidebar 等级图标源 | (26, 38, 20, 20) — DWC-L394 |

### 2.9 花色显示区域

| 角色 | 矩形 |
|------|------|
| 自家花色 | (563, 88, 25, 25) — DWC-L479 |
| 对家花色 | (43, 88, 25, 25) — DWC-L480 |

> 注意：花色显示中 side=0 时采用占位图 (250, 0, 25, 25)，side=1~5 各自有具体的源矩形。

### 2.10 分数显示区域

| 庄家阵营 | 矩形 | 文字X | 文字Y |
|---------|------|-------|-------|
| 已方为庄（Master=1/3） | (85, 300, 56, 56) | 100（根据位数微调） | 310 |
| 对方为庄（Master=2/4） | (490, 128, 56, 56) | 506（根据位数微调） | 138 |

> 分数文字位置会根据位数动态偏移（DWC-L2177/L2193）。

### 2.11 出牌区域中央

| 元素 | 位置/矩形 |
|------|----------|
| 自家出牌 | 起始 X = 285 - 手数×7，Y=244，牌间距=14 — DWC-L1281 |
| 对家出牌 | 起始 X = 285 - 手数×7，Y=130，牌间距=14 — DWC-L1299 |
| 上家出牌 | 起始 X = 245 - 手数×13，Y=192，牌间距=13 — DWC-L1330 |
| 下家出牌 | X=326 + i×13（i从0开始），Y=192 — DWC-L1360 |

### 2.12 胜利指示器

| 位置 | 矩形 |
|------|------|
| 自家（玩家1）上 | (437, 310, 33, 53) — DWC-L2137 |
| 对家（玩家2）下 | (437, 120, 33, 53) — DWC-L2143 |
| 上家（玩家3）左 | (90, 218, 33, 53) — DWC-L2149 |
| 下家（玩家4）右 | (516, 218, 33, 53) — DWC-L2155 |

### 2.13 各玩家区域边界汇总

| 玩家 | 方向 | X范围 | Y范围 | 显示方式 |
|------|------|-------|-------|---------|
| 1 (自己) | 底部 | 30~590 | 355~471 | 正面朝上，水平排列 |
| 2 (对家) | 顶部 | 105~525 | 25~121 | 牌背，水平排列 |
| 3 (上家) | 左侧 | 6~77 | 140~342 | 牌背，垂直排列 |
| 4 (下家) | 右侧 | 554~625 | 136~346 | 牌背，垂直排列 |

---

## 3. 鼠标事件处理流程

### 3.1 事件注册

MainForm 标准 Windows Forms 事件处理器：
- `MainForm_MouseClick` — 行409
- `MainForm_MouseDoubleClick` — 行562

### 3.2 鼠标有效状态

**MouseClick 生效条件**（MFC-L416-418）：
```
(currentState.CurrentCardCommands == WaitingForMySending 
 || currentState.CurrentCardCommands == WaitingForSending8Cards)
 && whoseOrder == 1
```
即：只有**轮到玩家自己出牌**时，鼠标事件才会被处理。其他状态下（发牌、AI出牌等）不响应。

**MouseDoubleClick 生效条件**（MFC-L566-568）：
```
currentPokers[0].Count > 0
```
仅检查手牌非空，没有状态检查！但实际上会接替 MouseClick 的逻辑。

### 3.3 左键点击处理流程（MFC-L419-455）

```
鼠标事件触发
  ↓
判断状态是否有效（WaitingForMySending / WaitingForSending8Cards）
  ↓
[左键] Y边界检查：e.Y ∈ [355, 472)
  ↓
X边界检查：e.X ∈ [myCardsLocation[0], myCardsLocation[last] + 71]
  ↓
CalculateClickedRegion(e, 1):
  → 创建所有牌的 Region
  → 可见区域排除
  → 逐一 IsVisible 检测
  → 命中后切换 myCardIsReady[i]
  ↓
调用 DrawMyPlayingCards 重绘
  ↓
[继续] 检查小猪按钮 (296, 300, 53, 46)
  → 如果是抠底阶段（WaitingForSending8Cards）：
    检查是否恰好选8张 → 执行抠底
  → 如果是出牌阶段（WaitingForMySending）：
    检查 TractorRules.IsInvalid → 执行出牌
```

### 3.4 右键点击处理流程（MFC-L429-451）

```
鼠标事件触发（状态检查同上）
  ↓
[右键] 直接调用 CalculateRightClickedRegion(e)
  ↓
返回牌索引 i（或 -1）
  ↓
[连锁选中] 从 i 向左遍历同间距牌，统一其选中状态
  ↓
调用 DrawMyPlayingCards 重绘
```

**右键的连锁选中逻辑**（MFC-L438-449）：
```csharp
bool b = (bool)myCardIsReady[i];
int x = (int)myCardsLocation[i];
for (int j = 1; j <= i; j++)
{
    if ((int)myCardsLocation[i - j] == (x - 13))
    {
        myCardIsReady[i - j] = b;
        x -= 13;
    }
    else break;
}
```
从被点击的牌开始向左，只要相邻牌的间距恰好为 13px，就将它们的选中状态统一。这只在右键点击时触发，用于快速选中一组连牌（拖拉机）。

### 3.5 双击处理流程（MFC-L562-624）

```
鼠标双击触发
  ↓
快速检查手牌非空
  ↓
调用 CalculateDoubleClickedRegion(e) → 确保点击牌的精选状态为 true
  ↓
创建出牌列表 currentSendCards[0]
  ↓
[同样检查小猪按钮]
  → 抠底或出牌判断（与左击后逻辑相同）
```

> ⚠️ 注意：双击事件**没有**先检查事件状态（WaitingForMySending / WaitingForSending8Cards），如果手牌不为空且处于非出牌阶段，双击可能导致意外行为。

### 3.6 花色按钮点击（叫牌期间的命中检测）

`IsClickedRanked(MouseEventArgs e)` — DWC-L745，在 `MouseClick` 事件中当 `currentState.CurrentCardCommands == ReadyCards` 时调用（MFC-L547）。

为每个可用的花色创建 `Region`（矩形大小 25×25，位置与渲染位置完全一致），检测 `IsVisible`。命中后更新游戏状态（`currentState.Suit`、`whoShowRank`、`showSuits`）并重绘。

### 3.7 小猪按钮命中检测（出牌/抠底确认）

猪按钮位置：`(296, 300, 53, 46)` — MFC-L459/L583。

检测方式：
```csharp
Region region = new Region(pigRect);
if (region.IsVisible(e.X, e.Y)) { ... }
```

在出牌阶段（WaitingForMySending）和抠底阶段（WaitingForSending8Cards）都做此检测。出牌时先检查 `TractorRules.IsInvalid()` 确认牌型合法。

---

## 4. 渲染与命中检测的边界分析

### 4.1 两者的共享布局值

| 布局值 | 用于渲染 | 用于命中检测 | 是否一致 |
|--------|---------|-------------|---------|
| 牌宽 71 | ✅ 所有 DrawImage | ✅ Region 宽度 | ✅ 一致 |
| 牌高 96 | ✅ 所有 DrawImage | ✅ Region 高度 | ✅ 一致 |
| 手牌X坐标 myCardsLocation | ✅ SetCardsInformation | ✅ Region X | ✅ 通过 SetCardsInformation 同步 |
| 选中Y=355/未选中Y=375 | ✅ DrawMyOneOrTwoCards2 | ✅ CalculateClickedRegion | ✅ 一致（出牌阶段） |
| 水平间距 13 | ✅ 渲染时 j++ 后的 X | ✅ 排除算法依赖 | ✅ 存在隐式依赖 |
| 花色按钮 417+i×25, 327 | ✅ ReDrawToolbar | ✅ IsClickedRanked | ✅ 完全一致 |
| 小猪按钮 296, 300, 53, 46 | ✅ DrawMyPlayingCards | ✅ MouseClick/DoubleClick | ✅ 完全一致 |

> ⚠️ **关键发现：y=360 不一致** — 「渲染牌的选中 Y 偏移」在发牌阶段（DrawMyOneOrTwoCards）使用 Y=360（下移 15px），但命中检测（CalculateRegionHelper）使用 Y=355（下移 20px）。不过发牌阶段没有鼠标事件处理，所以目前不会导致 Bug，但重构时需要注意。

### 4.2 清理矩形的边界差异

| 渲染阶段 | 清理矩形 | 备注 |
|---------|---------|------|
| DrawMyCards（发牌） | (30, 360, **560**, 96) | 覆盖区域更小 |
| DrawMySortedCards（整理后） | (30, 355, **600**, 116) | 覆盖区域更大 |
| DrawMyPlayingCards（出牌） | (30, 355, **600**, 116) | 同上 |

在发牌阶段的清理矩形高度只有 360→456（96px），但牌宽不够（560 vs 600），这在极端情况下可能造成残影。

### 4.3 Region 排除算法的精度

排除算法假设每张牌的前一张牌在 X 方向偏移 13px，由于 `myCardsLocation` 正是由 `SetCardsInformation(start + j * 13, ...)` 填充的（DWC-L1479），每张牌的 X 间距就是 13px，保证排除算法与渲染精确对应。

### 4.4 隐式耦合的布局值

以下值跨多个文件和渲染/命中检测使用，是强耦合点：

1. **13px（手牌间距）**：在 DrawingFormHelper 中用于渲染的 `start + j * 13`，用于 `SetCardsInformation` 记录坐标，在 CalculateRegionHelper 中 5 张牌的排除（5×13=65 < 71）。如果改变间距值，排除算法中的 `Exclude(regions[i+5])` 逻辑可能失效。

2. **71px（牌宽）**：在命中检测中，HitTest Y 范围的上限是 `myCardsLocation[last] + 71`（MFC-L420）。如果改变牌宽，这个边界必须同步更新。

3. **355/375（Y偏移）**：渲染的 Y 值和命中检测的 Region Y 值必须保持一致。目前出牌阶段一致（355/375），但发牌阶段 DrawMyOneOrTwoCards 用 360/375 可能存在隐含问题。

---

## 5. LayoutManager 职责范围建议

如果重构为具有专用 `LayoutManager` 的架构，建议职责边界如下：

### 5.1 LayoutManager 负责

| 职责 | 包含 | 不包含 |
|------|------|--------|
| 定义布局常量 | CardWidth, CardHeight, CardSpacing, 玩家区域矩形边界 | 这些常量的派生计算 |
| 计算手牌起始位置 | 根据手牌数计算 `start = (2780 - index * 75) / 10` | 渲染调用 |
| 计算每张牌渲染坐标 | 给定牌序 → (x, y, width, height) | 判断选中/未选中 Y 偏移 |
| 提供玩家区域边界 | 4 个玩家的清理矩形 | 命中检测时的边界检查 |
| 提供牌桌中央区域 | 牌堆、出牌区、底牌区、分数区、工具栏等 | 以上区域的绘制细节 |

### 5.2 建议的接口方法

```csharp
class LayoutManager
{
    // 基础常量
    const int CardWidth = 71;
    const int CardHeight = 96;
    const int CardSpacing = 13;
    const int CardSelectedOffset = 20;  // 选中牌上移量

    // 玩家手牌区域
    Rectangle GetPlayerHandRect(int playerId, GamePhase phase);
    Point GetCardPosition(int cardIndex, int handCount, bool isSelected);
    int GetHandStartX(int handCount);

    // 服务器区域
    Rectangle GetCenterDeckRect();
    Rectangle GetBottomCardsRect();
    Rectangle GetScoreRect(bool isOurTurn);
    Rectangle GetToolbarRect();
    Rectangle GetSuitButtonRect(int suitIndex);
    Rectangle GetPigButtonRect();
    Rectangle GetWinnerIndicatorRect(int playerId);

    // 命中检测辅助
    Region GetCardHitRegion(int x, bool isSelected);
    Region[] GetCardHitRegions(List<int> cardLocations, List<bool> readyStates);
}
```

### 5.3 建议不允许 LayoutManager 做的事

- 不调用 Graphics.DrawImage
- 不修改游戏状态（currentState、myCardIsReady 等）
- 不处理鼠标事件
- 不涉及动画（DrawAnimatedCard 应留在 DrawingFormHelper）

---

## 6. 发现的 Bug/不一致性

### 6.1 BUG: DrawMyOneOrTwoCards 选中 Y 偏移不一致

- **渲染（发牌阶段）**：DrawMyOneOrTwoCards，选中牌 Y=360 — DWC-L1488/L1504/L1515
- **渲染（出牌阶段）**：DrawMyOneOrTwoCards2，选中牌 Y=355（传参）— DWC-L1686
- **命中检测**：CalculateRegionHelper，选中牌 Region Y=355 — CAL-L32/L96/L154
- **差异**：发牌阶段的选中 Y 为 360，而命中检测使用 355，差 5px
- **影响**：发牌阶段没有鼠标事件处理，不产生实际 Bug。但如果未来扩展发牌阶段的交互，将导致点击偏差

### 6.2 BUG: MouseDoubleClick 缺少状态检查

- MouseClick（MFC-L416）：检查 `currentState.CurrentCardCommands == WaitingForMySending || WaitingForSending8Cards`
- MouseDoubleClick（MFC-L566）：**只检查** `currentPokers[0].Count > 0`
- **影响**：在非出牌阶段双击手牌区域，会本应不做响应，但实际上进入了下半段出牌/抠底逻辑

### 6.3 BUG: DrawMyCards 清理矩形小于 DrawMySortedCards

- DrawMyCards（DWC-L934）：`(30, 360, 560, 96)` — Y=360，宽 560，高 96
- DrawMySortedCards（DWC-L1004）：`(30, 355, 600, 116)` — Y=355，宽 600，高 116
- DrawMyPlayingCards（DWC-L1132）：`(30, 355, 600, 116)`
- **影响**：DrawMyCards 的清理区域少覆盖了顶部 5px（355→360）和侧面 40px（560 vs 600），在特定情况下可能留下残影

### 6.4 BUG: CalculateClickedRegion/CalculateDoubleClickedRegion 代码重复

- `CalculateClickedRegion`（CAL-L24-79）和 `CalculateDoubleClickedRegion`（CAL-L88-142）的 90% 代码完全相同
- 仅在第69行 vs 第134行的 `myCardIsReady[i]` 赋值逻辑有差异（toggle vs force true）
- 本应为单一方法 + 参数的重构

### 6.5 BUG: CalculateRegionHelper 三个方法代码三重复制

- `CalculateClickedRegion`、`CalculateDoubleClickedRegion`、`CalculateRightClickedRegion` 的 Region 创建和排除算法三段完全相同的代码
- 仅返回值类型和最终 `myCardIsReady[i]` 的赋值有差异
- **本应为单一方法的三个重载/参数化调用**

---

## 附录：文件引用规则

| 缩写 | 文件路径 |
|------|---------|
| CAL | `CalculateRegionHelper.cs` |
| DWC | `DrawingFormHelper.cs` |
| MFC | `MainForm.cs` |
