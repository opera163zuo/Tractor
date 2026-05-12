# 渲染管线分析 — DrawingFormHelper.cs

**文件**: `/tmp/tractor-analysis/Tractor.net/Helpers/DrawingFormHelper.cs`  
**总行数**: 2355  
**框架**: C# WinForms .NET 4.6.1  
**核心机制**: 通过 `Graphics.FromImage(mainForm.bmp)` 在离屏 Bitmap 上手动绘制，再通过 `mainForm.Refresh()` 推送到窗口。

---

## 1. 方法清单（完整）

> 行号范围基于文件逐行标注。`internal` = 类内部/同命名空间可访问；`private` = 仅本类。

### 1.1 构造函数

| # | 方法 | 行号 | 说明 |
|---|------|------|------|
| 1 | `internal DrawingFormHelper(MainForm mainForm)` | 23–27 | 注入 MainForm 引用，后续所有渲染都通过 `this.mainForm` 读取/修改 |

### 1.2 发牌/底牌相关（#region 发牌动画）

| # | 方法 | 行号 | 参数 | 返回值 | 绘制内容 |
|---|------|------|------|--------|----------|
| 2 | `internal void ReadyCards(int count)` | 36–85 | `count`: 当前发牌轮次 (0–24) | void | 中心发牌区(58-2*count张)、自己、对家、上家、下家手牌 + 亮牌判定 |
| 3 | `internal void DrawCenterAllCards(Graphics g, int num)` | 105–120 | `g`: Graphics; `num`: 要画的牌张数 | void | 桌面中央叠牌区（58张→逐渐减少） |
| 4 | `internal void DrawCenterImage()` | 126–132 | 无 | void | 清空/重置中央区域 (77,124 476×244) |
| 5 | `internal void DrawPassImage()` | 138–144 | 无 | void | 显示"过牌"提示图片 (110,150 400×199) |
| 6 | `internal void DrawCenter8Cards()` | 162–196 | 无 | void | 从桌底摸8张底牌 → 动画飞向庄家 |
| 7 | `private void Get8Cards(ArrayList list0, ...)` | 198–210 | 四个玩家的牌列表 | void | **逻辑方法**：将其他三人第25-26张牌移给庄家 |
| 8 | `internal void DrawBottomCards(ArrayList bottom)` | 212–229 | `bottom`: 底牌列表(8张) | void | 在底部区域(230,186/146)绘制8张底牌（第3张偏移到y=146） |

### 1.3 Sidebar / Toolbar（#region 显示Sidebar和toolbar）

| # | 方法 | 行号 | 参数 | 返回值 | 绘制内容 |
|---|------|------|------|--------|----------|
| 9 | `internal void DrawSidebar(Graphics g)` | 239–243 | `g` | void | 左右两侧的等级/花色栏 (20,30 / 540,30) |
| 10 | `internal void DrawMaster(Graphics g, int who, int start)` | 249–270 | `who`: 玩家1-4; `start`: 花式偏移 | void | 庄家标记图标（X=30/60/548/580, Y=45, 20×20） |
| 11 | `internal void DrawOtherMaster(Graphics g, int who, int start)` | 275–296 | `who`: 庄家玩家号 | void | 非庄家的其他三人标记置灰 |
| 12 | `internal void DrawRank(Graphics g, int number, bool me, bool b)` | 302–329 | `number`; `me`: 我方/敌方; `b`: 彩色/灰色 | void | 等级数字（左侧46/右侧566, Y=68, 20×20） |
| 13 | `private Rectangle getCardNumberImage(int number, bool b)` | 333–349 | `number`: 0-53; `b`: 彩色 | Rectangle | **辅助计算**：返回等级数字在资源图中的src矩形 |
| 14 | `internal void DrawSuit(Graphics g, int suit, bool me, bool b)` | 356–470 | `suit`: 1-5(红桃/黑桃/方块/梅花/王); `me/b`同上 | void | 花色图标（左侧43或右侧563, Y=88, 25×25） |
| 15 | `internal void DrawToolbar()` | 476–481 | 无 | void | 主工具栏 (415,325 129×29) + 五色按钮 (417,327 各25×25) |
| 16 | `internal void RemoveToolbar()` | 486–490 | 无 | void | 从背景图恢复，擦除工具栏 |

### 1.4 亮牌判定（#region 判断是否亮牌）

| # | 方法 | 行号 | 参数 | 返回值 | 绘制内容 |
|---|------|------|------|--------|----------|
| 17 | `private void DoRankOrNot(CurrentPoker currentPoker, int user)` | 540–681 | `currentPoker`; `user:1-4` | void | 根据算法判定是否亮牌，更新sidebar花色/等级/庄家标记 |
| 18 | `private void MyRankOrNot(CurrentPoker currentPoker)` | 685–693 | `currentPoker` | void | 仅对玩家(1)：调用Algorithm判断→刷新toolbar按钮 |
| 19 | `internal void ReDrawToolbar(bool[] suits)` | 696–713 | `suits`: 五个花色的可用性 | void | 重绘亮牌工具栏：可用花色高亮，不可用灰色 |
| 20 | `internal bool DoRankNot()` | 717–728 | 无 | bool | **纯逻辑**：判断是否尚未亮牌（`Suit==0`） |
| 21 | `internal void IsClickedRanked(MouseEventArgs e)` | 740–900 | `e`: 鼠标事件 | void | 判断点击落在哪个花色按钮上(417/443/468/493/518) → 更新状态 + 重绘 |

### 1.5 手牌绘制（#region 在各种情况下画自己的牌）

| # | 方法 | 行号 | 参数 | 返回值 | 绘制内容 |
|---|------|------|------|--------|----------|
| 22 | `internal void DrawMyCards(Graphics g, CurrentPoker currentPoker, int index)` | 939–976 | `g`; `currentPoker`; `index` | void | 发牌时：按花色顺序绘制手牌(底部 30,360 560×96) |
| 23 | `internal void DrawMySortedCards(CurrentPoker currentPoker, int index)` | 984–1106 | `currentPoker`; `index` | void | 收牌后：按牌局花色规则排序绘制（主牌花色优先） |
| 24 | `internal void DrawMyPlayingCards(CurrentPoker currentPoker)` | 1121–1260 | `currentPoker` | void | 出牌阶段：支持点击选择（上移20px），显示"Ready"/"无效"标记 |
| 25 | `private void My8CardsIsReady(Graphics g)` | 1267–1285 | `g` | void | 扣底阶段：如果选择了8张，显示"Ready"按钮 |
| 26 | `private static void IsSuitLost(ref int j, ref int k)` | 1112–1118 | `j/k`：位置计数器 | void | **辅助计算**：当花色缺失时减少间隔 |

### 1.6 出牌动作绘制（#region 画给各个玩家的牌）

| # | 方法 | 行号 | 参数 | 返回值 | 绘制内容 |
|---|------|------|------|--------|----------|
| 27 | `internal void DrawMySendedCardsAction(ArrayList readys)` | 1299–1311 | `readys`: 出的牌列表 | void | 我出的牌显示在中间偏下 (start=285-n*7, Y=244) |
| 28 | `private void DrawFrieldUserSendedCardsAction(ArrayList readys)` | 1318–1333 | `readys` | void | 对家出的牌显示在中间偏上 (Y=130) |
| 29 | `private void RedrawFrieldUserCardsAction(Graphics g, CurrentPoker cp)` | 1338–1348 | `g`; `cp` | void | 刷新对家手牌的背面显示 |
| 30 | `private void DrawPreviousUserSendedCardsAction(ArrayList readys)` | 1352–1363 | `readys` | void | 上家出的牌在左侧 (X=245+偏移, Y=192) |
| 31 | `private void RedrawPreviousUserCardsAction(Graphics g, CurrentPoker cp)` | 1367–1378 | `g`; `cp` | void | 刷新上家手牌的背面显示（纵向排列） |
| 32 | `private void DrawNextUserSendedCardsAction(ArrayList readys)` | 1384–1395 | `readys` | void | 下家出的牌在右侧 (X=326+偏移, Y=192) |
| 33 | `private void RedrawNextUserCardsAction(Graphics g, CurrentPoker cp)` | 1399–1410 | `g`; `cp` | void | 刷新下家手牌的背面显示 |

### 1.7 我手牌的按花色绘制（#region 画自己的牌 — 普通/发牌阶段）

| # | 方法 | 行号 | 参数 | 说明 |
|---|------|------|------|------|
| 34 | `private int DrawBigJack(...)` | 1418–1420 | `g, currentPoker, j, start` | 画大王 |
| 35 | `private int DrawSmallJack(...)` | 1423–1425 | 同上 | 画小王 |
| 36–39 | `private int DrawDiamondsRank/ClubsRank/PeachsRank/HeartsRank(...)` | 1428–1445 | 同上 | 画各花色的Rank牌（主牌） |
| 40–43 | `private int DrawMyClubs/Diamonds/Peachs/Hearts(...)` | 1448–1479 | 同上 | 循环画各花色普通牌 (0-12) |
| 44 | `private int DrawMyOneOrTwoCards(Graphics g, int count, int number, int j, int start)` | 1482–1542 | `count`: 1或2; `number`: 牌号; `j/start`: 位置 | **核心绘制函数**：画1张或2张相同的牌，亮牌时上移15px |

### 1.8 我手牌的按花色绘制（#region 画自己的牌 — 出牌阶段，_2变体）

| # | 方法 | 行号 | 说明 |
|---|------|------|------|
| 45–54 | `DrawBigJack2 / DrawSmallJack2 / Draw*Rank2 / DrawMy*2 / DrawMyOneOrTwoCards2` | 1583–1707 | 出牌阶段版本：检查 `myCardIsReady[]` 标记，选中牌上移20px |

### 1.9 整轮出牌完成后（#region 画给各个玩家的牌 ...）

| # | 方法 | 行号 | 说明 |
|---|------|------|------|
| 55 | `internal void DrawMyFinishSendedCards()` | 1315–1735 | 我出完牌后：展示出的牌 + 重绘手牌 + 检查是否四人全出 |
| 56 | `internal void DrawNextUserSendedCards()` | 1740–1790 | AI下家出牌（调用Algorithm），画牌 + 检查完成 |
| 57 | `internal void DrawFrieldUserSendedCards()` | 1795–1850 | AI对家出牌 |
| 58 | `internal void DrawPreviousUserSendedCards()` | 1855–1915 | AI上家出牌 |
| 59 | `internal void DrawFinishedOnceSendedCards()` | 1920–2125 | 四人全出后：结算、Debug模式修复逻辑、计算下轮谁先出 |
| 60 | `private void DrawWhoWinThisTime()` | 2130–2155 | 显示本轮赢家箭头图标 (33×53) |
| 61 | `internal void DrawScoreImage(int scores)` | 2163–2190 | 计分器：在分数图标上DrawString（根据庄家位置选择左上或右下） |
| 62 | `internal void DrawFinishedScoreImage()` | 2194–2210 | 全局结束后：半透明白色遮罩 + 底牌 + Logo + 总分 |
| 63 | `internal void DrawFinishedSendedCards()` | 2215–2242 | 本轮打完：计算分/庄家/Rank + 清场 + 准备下一轮 |

### 1.10 工具方法（#region 用时的一些方法）

| # | 方法 | 行号 | 说明 |
|---|------|------|------|
| 64 | `private Bitmap getPokerImageByNumber(int number)` | 2252–2263 | 从嵌入资源或自定义图片中获取牌面Bitmap |
| 65 | `internal void DrawBackground(Graphics g)` | 2268–2272 | 用 `mainForm.image` 铺满客户区作为背景 |
| 66 | `private void DrawAnimatedCard(Bitmap card, int x, int y, int w, int h)` | 2277–2287 | **闪一下动画**：备份→画牌→Refresh→恢复 |
| 67 | `private void DrawMyImage(Graphics g, Bitmap bmp, int x, int y, int w, int h)` | 2292–2296 | 简单封装 `g.DrawImage` |
| 68 | `private void SetCardsInformation(int x, int number, bool ready)` | 2301–2307 | **纯数据**：记录牌的X位置/编号/是否选中 |
| 69 | `internal void TestCards()` | 2311–2355 | Debug输出：在画面上显示所有玩家手牌的编号 |

---

## 2. 渲染模式分析

### 2.1 离屏 Bitmap 渲染（double-buffering）

**所有渲染都通过 `Graphics.FromImage(mainForm.bmp)` 写入离屏 Bitmap**，再调用 `mainForm.Refresh()` 触发窗口重绘（`OnPaint` → `DrawBackground`，见 MainForm.cs）。

**证据**：
- `ReadyCards` (行38): `Graphics g = Graphics.FromImage(mainForm.bmp);`
- `DrawCenterImage` (行128): 同上
- 几乎所有 `internal void` 方法都以此开头

**例外**：`DrawBackground(Graphics g)`（行2268）接收外部传入的 Graphics（来自 OnPaint 的 e.Graphics），用于将 `mainForm.bmp` 最终画到屏幕上。

### 2.2 仅辅助计算的方法（不直接绘制）

| 方法 | 所在行 | 说明 |
|------|--------|------|
| `Get8Cards` | 198 | 纯数组操作：移动牌 |
| `getCardNumberImage` | 333 | 返回资源图中 src Rectangle |
| `SetCardsInformation` | 2301 | 记录牌的位置/编号到 `mainForm` 的 ArrayList |
| `DoRankNot` | 717 | 纯布尔判断 |
| `IsSuitLost` | 1112 | 调整间隔计数器 |

### 2.3 DrawAnimatedCard 的实现机制（行2277–2287）

```csharp
private void DrawAnimatedCard(Bitmap card, int x, int y, int width, int height)
{
    Graphics g = Graphics.FromImage(mainForm.bmp);
    Bitmap backup = mainForm.bmp.Clone(new Rectangle(x, y, width, height), PixelFormat.DontCare);
    g.DrawImage(card, x, y, width, height);
    mainForm.Refresh();              // 闪现一帧
    g.DrawImage(backup, x, y, width, height);  // 立即恢复
    g.Dispose();
}
```

**机制**：
1. 备份目标区域背景
2. 在新位置画牌 → `Refresh()` 推送到屏幕（闪现一帧）
3. 立即用备份恢复原位

**效果**：牌"闪烁"了一下。没有任何补间/渐变/缓动，就是一瞬间显示再消失。每次发牌时每个玩家的新牌都会调用此方法（`ReadyCards` 中4次调用）。

### 2.4 计分器文字渲染方式（行2163–2190）

```csharp
internal void DrawScoreImage(int scores)
{
    Graphics g = Graphics.FromImage(mainForm.bmp);
    Bitmap bmp = global::Kuaff.Tractor.Properties.Resources.scores;
    Font font = new Font("宋体", 12, FontStyle.Bold);
    // ... 根据庄家位置选择区域
    g.DrawString(scores + "", font, Brushes.White, x, 138/310);
}
```

**方式**：
- 使用 GDI+ `DrawString`，系统字体"宋体"，Bold，12pt，白色
- 位置根据庄家阵营动态选择：右上(490,128) 或 左下(85,300)
- 文本中心手动算法：根据分数位数调整 x 偏移（1位→x, 2位→x-4, 3位→x-8）
- **New Font() 每次调用不Dispose** → 资源泄漏风险

---

## 3. 数据依赖分析

所有方法都通过 `this.mainForm` 读取数据。以下是按方法分组的依赖清单。

### 3.1 mainForm 读取字段

| 字段 | 类型 | 被哪些方法读取 |
|------|------|---------------|
| `mainForm.bmp` | Bitmap | 几乎所有方法（创建Graphics的目标） |
| `mainForm.image` | Bitmap | DrawBackground, DrawCenterImage, 大量清空/恢复操作 |
| `mainForm.currentState.Suit` | int | DoRankOrNot, MyRankOrNot, IsClickedRanked, DrawSuitCards, DrawMyCards/DrawMySortedCards/DrawMyPlayingCards 中的排序逻辑 |
| `mainForm.currentState.Master` | int | DoRankOrNot, IsClickedRanked, DrawScoreImage, DrawFinishedSendedCards |
| `mainForm.currentState.CurrentCardCommands` | CardCommands | DrawMyPlayingCards, My8CardsIsReady, DrawMyOneOrTwoCards, DrawFinishedOnceSendedCards 等 |
| `mainForm.currentState.OurCurrentRank` | int | DrawRank |
| `mainForm.currentState.OpposedCurrentRank` | int | DrawRank |
| `mainForm.pokerList[0..3]` | ArrayList[] | ReadyCards, DrawCenter8Cards, Get8Cards, DrawFinishedOnceSendedCards |
| `mainForm.currentPokers[0..3]` | CurrentPoker[] | ReadyCards, DrawMyCards/SortedCards/PlayingCards, 出牌Action相关 |
| `mainForm.currentSendCards[0..3]` | ArrayList[] | DrawMyFinishSendedCards, DrawNextUserSendedCards, DrawFrieldUserSendedCards, DrawPreviousUserSendedCards, DrawFinishedOnceSendedCards |
| `mainForm.currentRank` | int | 排序/绘制中判断Rank牌 |
| `mainForm.showSuits` / `whoShowRank` | int | DrawSuitCards, DrawMyOneOrTwoCards 等 |
| `mainForm.whoseOrder` | int | DrawFinishedOnceSendedCards |
| `mainForm.firstSend` | int | DrawFinishedOnceSendedCards, AI出牌相关 |
| `mainForm.Scores` | int | DrawScoreImage, DrawFinishedScoreImage |
| `mainForm.myCardsLocation` | ArrayList | SetCardsInformation (写入), 外部计算区域所用 |
| `mainForm.myCardsNumber` | ArrayList | SetCardsInformation (写入) |
| `mainForm.myCardIsReady` | ArrayList | SetCardsInformation, DrawMyOneOrTwoCards2, My8CardsIsReady |
| `mainForm.cardsOrderNumber` | int | DrawMyOneOrTwoCards2 (读取和递增) |
| `mainForm.gameConfig.BackImage` | Bitmap | DrawCenterAllCards, ReadyCards, Redraw* 等（对手牌背面） |
| `mainForm.gameConfig.CardImageName` | string | getPokerImageByNumber |
| `mainForm.gameConfig.IsDebug` | bool | ReadyCards, DrawFinishedOnceSendedCards |
| `mainForm.gameConfig.FinishedOncePauseTime` | int | DrawMyFinishSendedCards 等 |
| `mainForm.gameConfig.FinishedThisTime` | int | DrawFinishedSendedCards |
| `mainForm.gameConfig.CardsResourceManager` | ResourceManager | getPokerImageByNumber |
| `mainForm.cardsImages[54]` | Bitmap[] | getPokerImageByNumber |
| `mainForm.isNew` | bool | DoRankOrNot, IsClickedRanked, DrawFinishedSendedCards |
| `mainForm.whoIsBigger` | int | DrawFinishedOnceSendedCards |
| `mainForm.send8Cards` | ArrayList | DrawFinishedScoreImage |

### 3.2 mainForm 状态修改

| 字段 | 被谁修改 | 修改方式 |
|------|---------|---------|
| `mainForm.currentPokers[0..3]` | ReadyCards (AddCard), Get8Cards (Add/Remove) | 增减手牌 |
| `mainForm.currentRank` | (外部设置，本类只读) | — |
| `mainForm.showSuits` | DoRankOrNot++, IsClickedRanked | 亮牌次数递增 |
| `mainForm.whoShowRank` | DoRankOrNot, IsClickedRanked | 记录哪位玩家亮牌 |
| `mainForm.currentState.Suit` | DoRankOrNot, IsClickedRanked | 设置当前花色 |
| `mainForm.currentState.Master` | DoRankOrNot, IsClickedRanked | 庄家设置 |
| `mainForm.myCardsLocation` | SetCardsInformation | Add |
| `mainForm.myCardsNumber` | SetCardsInformation | Add |
| `mainForm.myCardIsReady` | SetCardsInformation | Add |
| `mainForm.cardsOrderNumber` | DrawMyOneOrTwoCards2 | 递增 |
| `mainForm.whoseOrder` | DrawFinishedOnceSendedCards, AI出牌方法 | 设置下一个出牌人 |
| `mainForm.whoIsBigger` | DrawFinishedOnceSendedCards | 归零 |
| `mainForm.firstSend` | DrawFinishedOnceSendedCards | 更新 |
| `mainForm.pokerList[...]` | DrawFinishedOnceSendedCards (Debug 修复) | Debug模式下纠错 |
| `mainForm.currentSendCards[...]` | DrawFinishedOnceSendedCards | 清空 |

---

## 4. 与游戏逻辑的耦合点

### 4.1 直接调用 Algorithm.*（严重耦合）

| 方法 | 调用的 Algorithm 方法 | 行号 |
|------|----------------------|------|
| `DoRankOrNot` | `Algorithm.ShouldSetRank(mainForm, user)` | 574 |
| `DoRankOrNot` | `Algorithm.ShouldSetRankAgain(mainForm, currentPoker)` | 618 |
| `MyRankOrNot` | `Algorithm.CanSetRank(mainForm, currentPoker)` | 692 |
| `IsClickedRanked` | `Algorithm.CanSetRank(mainForm, mainForm.currentPokers[0])` | 747 |
| `DrawNextUserSendedCards` | `Algorithm.MustSendedCards(...)` | 1760 |
| `DrawNextUserSendedCards` | `Algorithm.ShouldSendedCards(...)` | 1764 |
| `DrawFrieldUserSendedCards` | `Algorithm.MustSendedCards(...)` | 1814 |
| `DrawFrieldUserSendedCards` | `Algorithm.ShouldSendedCards(...)` | 1818 |
| `DrawPreviousUserSendedCards` | `Algorithm.MustSendedCards(...)` | 1878 |
| `DrawPreviousUserSendedCards` | `Algorithm.ShouldSendedCards(...)` | 1882 |

此外还调用了：
- `TractorRules.IsInvalid(...)` — 行1233（出牌合法性校验）
- `TractorRules.GetNextOrder(...)` — 行1991, 2132（轮到谁）
- `TractorRules.CalculateScore(...)` — 行2112（算分）
- `TractorRules.GetNextMasterUser(...)` — 行2234（下一轮庄家）
- `CommonMethods.OtherUsers(...)` / `GetSuit(...)` — Debug 修复逻辑
- `MustSendCardsAlgorithm.WhoseOrderIs2/3/4(...)` — Debug 修复逻辑

### 4.2 重构建议：拆分点

1. **AI出牌与渲染混合** — `DrawNextUserSendedCards`、`DrawFrieldUserSendedCards`、`DrawPreviousUserSendedCards` 三个方法内部调用了 `Algorithm.MustSendedCards/ShouldSendedCards` 来决定AI出什么牌，再把牌画出来。渲染和AI决策完全绑定。

2. **结算与渲染混合** — `DrawFinishedOnceSendedCards` (行1920-2125) 同时处理：Debug纠错、谁赢了、算分、清空sendCards。大量游戏状态修改混杂在渲染方法里。

3. **亮牌判定与渲染混合** — `DoRankOrNot` (行540-681) 调用算法判定后直接发号施令改 `mainForm` 状态并重绘sidebar。

4. **Debug 模式下修复逻辑** — `DrawFinishedOnceSendedCards` 中包含 ~100行 Debug 纠错代码（行1930-2098），这些代码直接操作 `pokerList`、`currentPokers`、`MustSendCardsAlgorithm` 等——都是纯游戏逻辑。

---

## 5. 硬编码像素常量分类

> 以下仅分类，不逐条列出。引用行号为证据范围。

### 5.1 卡牌尺寸
- **71 × 96 px** — 所有卡牌绘制（行67, 70, 76, 81, 等）。这是标准牌面大小。

### 5.2 卡牌间距
- **13 px** — 手牌之间的 X 间距（行1112行公式中的 `start + j * 13`）
- **2 px** — 中央叠牌间距（行118: `200 + i * 2`）
- **14 px** — 底牌间距 / 已出牌间距（行222, 1306等）
- **4 px** — 对家/上家/下家手牌纵向间距（行1376, 1407）
- **7 px** — 我出牌的间距计算用 `285 - readys.Count * 7`（行1301）

### 5.3 各玩家手牌坐标

| 玩家 | 初始X | 初始Y | 排列方向 | 间距 |
|------|-------|-------|---------|------|
| **我（自己）** | 公式: `(2780 - count*75)/10` | 375(正常)/360(亮牌)/355(出牌) | 横向 | 13px |
| **对家（上家）** | 公式: `(2500 + cp.Count*75)/10` 或固定437-25 | 25 | 横向（背面） | -13px（反向） |
| **上家** | 6 | 公式: `195 - cp.Count*2` | 纵向 | +4px/张 |
| **下家** | 554 | 公式: `191 + cp.Count*2` | 纵向 | -4px/张 |

### 5.4 各区域固定坐标

| 区域 | X, Y | 宽×高 | 用途 |
|------|------|--------|------|
| 桌面中央 | 200, 186 | (num+1)*2+71 × 96 | 发牌时叠牌区 |
| 清空中央区域 | 77, 124 | 476 × 244 | 重置桌面 |
| 底部手牌区 | 30, 355/360 | 560/600 × 96/116 | 我的手牌 |
| Sidebar左侧 | 20/30/43/46, 30/45/68/88 | 70/20/25 × 89/20/25 | 等级/花色/庄家 |
| Sidebar右侧 | 540/548/563/566, 30/45/68/88 | 同上 | 同上 |
| 工具栏 | 415, 325 | 129 × 29 | 亮牌花式选择 |
| 花色按钮 | 417/443/468/493/518, 327 | 各25 × 25 | 五种花色 |
| 计分器（右） | 490, 128 | 56 × 56 | 庄家为2/4号时 |
| 计分器（左） | 85, 300 | 56 × 56 | 庄家为1/3号时 |
| 输家箭头 | 437/437/90/516, 310/120/218/218 | 33 × 53 | 本轮赢家 |
| Logo | 160, 237 | 110 × 112 | 局终展示 |
| 总分文字 | 310, 286 | — | `DrawString` |

### 5.5 魔法数字（特殊偏移）

| 值 | 用途 | 行号 |
|----|------|------|
| `58 - count * 2` | 发牌轮次 → 桌面中央牌数 | 41 |
| `(2780 - index * 75) / 10` | 手牌数 → 起始X | 950 |
| `(2500 + cp.Count * 75) / 10` | 对家手牌起始X | 1342 |
| `195 - cp.Count * 2` | 上家手牌起始Y | 1372 |
| `191 + cp.Count * 2` | 下家手牌起始Y | 1404 |
| `285 - readys.Count * 7` | 我出牌的起始X | 1301 |
| `245 - readys.Count * 13` | 上家出牌起始X | 1356 |
| `326 + i * 13` | 下家出牌起始X | 1390 |
| `start + j * 13` | 手牌位置公式（核心） | 1493, 1500 |
| `260/280` | 我自己发牌动画坐标 | 67 |
| `400 - count*13` / `437 - count*13` | 对家发牌坐标 | 70, 73 |
| `50` / `6` | 上家发牌X | 76, 79 |
| `160 + count*4` / `145 + count*4` | 上家发牌Y | 76, 79 |
| `520` / `554` | 下家发牌X | 82, 84 |
| `220 - count*4` / `241 - count*4` | 下家发牌Y | 82, 84 |

### 5.6 资源图src矩形

| 资源 | src Rect | 用途 |
|------|----------|------|
| Sidebar等级区 | (26,38,20,20) | 等级数字背景擦除 |
| Sidebar花色区 | (23,58,25,25) | 花色图标背景擦除 |
| Suit图标 | (0-125,0,25,25) | 五种花色+灰色变体 |
| Master图标 | (0-60,0,20,20) | 庄家标记方位 |
| CardNumber | (number*20,0,20,20) | 等级数字 |

---

## 6. 潜在问题

1. **资源泄漏** — `DrawScoreImage` (行2163/2190) 和 `TestCards` (行2311) 中 `new Font(...)` 未调用 `.Dispose()`
2. **同步问题** — `DrawAnimatedCard` 调用 `mainForm.Refresh()` 后立即恢复，可能导致画面撕裂或闪烁不可见（取决于刷新时机）
3. **重复 Clone** — `DrawAnimatedCard` 每次 `Clone` 一个 Bitmap 区域，发牌阶段用25×4次
4. **渲染与逻辑深度耦合** — 出牌判定/算分/亮牌逻辑直接嵌入渲染方法，重构需先分离
5. **硬编码坐标** — 所有位置都是像素常量，适应不同分辨率/DPI需要大量修改
