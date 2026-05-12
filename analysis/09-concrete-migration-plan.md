# 09 — 具体迁移指令：逐步改造为 Engine + Renderer 分离架构

> **目标**：将拖拉机游戏从 "MainForm 大杂烩 + DrawingFormHelper 混合体" 改造为 **GameEngine（纯逻辑）+ GdiRenderer（纯渲染）+ MainForm（协调者）** 三层架构。
>
> 受众：只懂 C# 但不了解这个项目的人。每步都是"在哪改、改什么、改完什么样"的精确代码差异。

---

## 第一部分：最终目标——一张牌从"手里"到"桌上"的完整数据流

### 改造前路径

用户点击"小猪按钮"出牌 → 经过这些代码：

**1. MainForm.cs 行 537-558（MainForm_MouseClick）**
```csharp
// 用户点击小猪按钮后触发
if (TractorRules.IsInvalid(this, currentSendCards, 1))
{
    // 画布擦除小猪
    Graphics g = Graphics.FromImage(bmp);
    g.DrawImage(image, pigRect, pigRect, GraphicsUnit.Pixel);
    g.Dispose();

    if (firstSend == 1)
    {
        whoIsBigger = 1;
        ArrayList minCards = new ArrayList();
        if (TractorRules.CheckSendCards(this, minCards, 0))
        {
            currentSendCards[0] = new ArrayList();
            for (int i = 0; i < myCardIsReady.Count; i++)
            {
                if ((bool)myCardIsReady[i])
                {
                    CommonMethods.SendCards(currentSendCards[0], currentPokers[0], pokerList[0], (int)myCardsNumber[i]);
                }
            }
        }
        else
        {
            for (int i = 0; i < minCards.Count; i++)
            {
                CommonMethods.SendCards(currentSendCards[0], currentPokers[0], pokerList[0], (int)minCards[i]);
            }
        }
    }
    else
    {
        // 不是先手的情况
        currentSendCards[0] = new ArrayList();
        for (int i = 0; i < myCardIsReady.Count; i++)
        {
            if ((bool)myCardIsReady[i])
            {
                CommonMethods.SendCards(currentSendCards[0], currentPokers[0], pokerList[0], (int)myCardsNumber[i]);
            }
        }
    }
    drawingFormHelper.DrawMyFinishSendedCards();
}
```

**2. CommonMethods.cs 行 620-625（SendCards）**
```csharp
internal static void SendCards(ArrayList sends, CurrentPoker cp, ArrayList pokerList, int number)
{
    sends.Add(number);           // 加入已出牌列表
    cp.RemoveCard(number);       // 从 CurrentPoker 结构化数据中移除
    pokerList.Remove(number);    // 从原始手牌列表中移除
}
```

**3. DrawingFormHelper.cs 行 1704-1740（DrawMyFinishSendedCards）**
```csharp
internal void DrawMyFinishSendedCards()
{
    DrawMySendedCardsAction(mainForm.currentSendCards[0]);  // 画已出的牌

    for (int i = 0; i < mainForm.currentSendCards[0].Count; i++)
    {
        mainForm.currentAllSendPokers[0].AddCard((int)mainForm.currentSendCards[0][i]);
    }

    if (mainForm.currentPokers[0].Count > 0)
    {
        DrawMySortedCards(mainForm.currentPokers[0], mainForm.currentPokers[0].Count);
    }
    else
    {
        Rectangle rect = new Rectangle(30, 355, 560, 116);
        Graphics g = Graphics.FromImage(mainForm.bmp);
        g.DrawImage(mainForm.image, rect, rect, GraphicsUnit.Pixel);
        g.Dispose();
    }

    DrawScoreImage(mainForm.Scores);
    mainForm.Refresh();

    // 检查是否所有玩家都出完了
    if (mainForm.currentSendCards[3].Count > 0)
    {
        mainForm.currentState.CurrentCardCommands = CardCommands.Pause;
        mainForm.SetPauseSet(mainForm.gameConfig.FinishedOncePauseTime, CardCommands.DrawOnceFinished);
        DrawWhoWinThisTime();
    }
    else
    {
        mainForm.whoseOrder = 4;
        mainForm.currentState.CurrentCardCommands = CardCommands.WaitingForSend;
    }
}
```

> **问题**：MoueClick 混合了点击检测 + 业务逻辑（甩牌规则判断）+ 状态修改（whoseOrder, currentState）+ 渲染调用。读完一整段才知道它在做什么。

---

### 改造后路径

用户点击"小猪按钮"出牌 → 以下三条独立路径：

**1. MainForm_MouseClick 只做三件事：**
```csharp
private void MainForm_MouseClick(object sender, MouseEventArgs e)
{
    // 检查点击区域
    if (!IsPigButtonClicked(e)) return;

    if (currentState.CurrentCardCommands == CardCommands.WaitingForMySending)
    {
        // 收集选中牌的编号
        List<int> selectedNumbers = GetSelectedCardNumbers();

        // 交给 Engine 处理出牌逻辑（纯数据操作）
        PlayResult result = engine.PlayerPlayCard(1, selectedNumbers);

        // 根据 Engine 的返回结果执行渲染
        foreach (var cmd in result.RenderCommands)
            renderer.Execute(cmd, bmp);

        // 更新 currentState 的引用（engine 内部已修改状态）
        currentState = engine.CurrentState;
        Refresh();
    }
}
```

**2. GameEngine.cs 内部（行 PlayResult PlayerPlayCard(int playerId, List<int> selected)）：**
```csharp
public PlayResult PlayerPlayCard(int playerId, List<int> selectedNumbers)
{
    var result = new PlayResult();

    // 规则校验
    if (!_rules.IsInvalid(_state, playerId, selectedNumbers))
        return result.WithInvalid();  // 非法出牌，无变化

    // 首次出牌处理（甩牌规则）
    if (_state.FirstSend == playerId)
    {
        var minCards = new List<int>();
        if (!_rules.CheckSendCards(_state, selectedNumbers, minCards))
        {
            // 不能甩，只出必出的最小牌
            selectedNumbers = minCards;
        }
    }

    // 从 player 的手牌中删除，加入已出牌列表
    SendCards(playerId, selectedNumbers);

    // 判断是否一圈结束
    if (AllPlayersHavePlayed())
    {
        int winner = _rules.GetNextOrder(_state);
        _state.WhoseOrder = winner;
        _state.CurrentCommand = CardCommands.Pause;

        // 返回完整的渲染指令序列
        result.RenderCommands.Add(new RenderCommand(
            RenderCmdType.DrawPlayedCards,
            new PlayedCardsPayload { PlayerId = playerId, Cards = selectedNumbers }
        ));
        result.RenderCommands.Add(new RenderCommand(
            RenderCmdType.RedrawMyHand, new RedrawHandPayload { PlayerId = 1 }
        ));
        result.RenderCommands.Add(new RenderCommand(
            RenderCmdType.ShowRoundWinner, new WinnerPayload { PlayerId = winner }
        ));
        result.RenderCommands.Add(new RenderCommand(
            RenderCmdType.SetPause, new PausePayload { Ms = 1500, NextCommand = CardCommands.DrawOnceFinished }
        ));
    }
    else
    {
        _state.WhoseOrder = NextPlayer(playerId);
        _state.CurrentCommand = CardCommands.WaitingForSend;

        result.RenderCommands.Add(new RenderCommand(
            RenderCmdType.DrawPlayedCards,
            new PlayedCardsPayload { PlayerId = playerId, Cards = selectedNumbers }
        ));
        result.RenderCommands.Add(new RenderCommand(
            RenderCmdType.RedrawMyHand, new RedrawHandPayload { PlayerId = 1 }
        ));
    }

    return result;
}

private void SendCards(int playerId, List<int> numbers)
{
    foreach (int num in numbers)
    {
        _state.SendCards[playerId - 1].Add(num);
        _state.PokerLists[playerId - 1].Remove(num);
    }
    _state.CurrentPokers[playerId - 1] = CommonMethods.parse(
        _state.PokerLists[playerId - 1], _state.Suit, _state.Rank
    );
}
```

**3. GdiRenderer.cs（行 Execute）：**
```csharp
public void Execute(RenderCommand cmd, Bitmap bmp)
{
    switch (cmd.Type)
    {
        case RenderCmdType.DrawPlayedCards:
            var payload = (PlayedCardsPayload)cmd.Payload;
            DrawPlayedCards(bmp, payload.PlayerId, payload.Cards);
            break;
        case RenderCmdType.RedrawMyHand:
            DrawMySortedCards(bmp, _gameState.CurrentPokers[0], _gameState.PokerLists[0].Count);
            break;
        case RenderCmdType.ShowRoundWinner:
            DrawWinBanner(bmp, payload.PlayerId);
            break;
        // ...
    }
}
```

> ✅ **改造后人肉眼可见的好**：每个类只做一件事。Engine 不碰 Bitmap，Renderer 不碰游戏状态，MainForm 只做事件路由。

---

## 第二部分：逐步迁移指令

> 所有步骤从 **最安全（不改行为，只新建文件）** → **最危险（替换核心逻辑）**。

---

### 步骤1：创建 GameEngine 空壳 + RenderCommand 枚举

这是"先把家门打开"的一步，不改任何现有代码，不打断编译。

**涉及文件：**
- `Tractor.net/GameEngine.cs`（新建）
- `Tractor.net/RenderCommand.cs`（新建）
- `DefinedConstant.cs` 不动（稍后整合）

**改动前：**
```
无这两个文件。
```

**改动后：**
文件 1：`Tractor.net/GameEngine.cs`
```csharp
using System;
using System.Collections.Generic;

namespace Kuaff.Tractor
{
    /// <summary>
    /// 游戏引擎：纯逻辑，不引用 System.Windows.Forms 或 System.Drawing。
    /// 不碰 Bitmap，不碰 Graphics，不碰 Form。
    /// </summary>
    public class GameEngine
    {
        // 后面步骤会填充的字段
        // private CurrentState _state;
        // private List<int>[] _pokerLists;
        // private CurrentPoker[] _currentPokers;

        public GameEngine()
        {
            // 空壳构造函数
        }

        /// <summary>
        /// 创建一个初始状态的新游戏。
        /// 占位——后面步骤会实现具体逻辑。
        /// </summary>
        public void NewGame()
        {
            // 待实现
        }

        /// <summary>
        /// 玩家出牌。暂不实现。
        /// </summary>
        public PlayResult PlayerPlayCard(int playerId, List<int> selectedCards)
        {
            throw new NotImplementedException("步骤6-7实现");
        }
    }
}
```

文件 2：`Tractor.net/RenderCommand.cs`
```csharp
using System.Collections.Generic;

namespace Kuaff.Tractor
{
    /// <summary>
    /// 渲染指令枚举——描述"画什么"，不描述"怎么画"。
    /// 后续每一步会增加新值。
    /// </summary>
    public enum RenderCmdType
    {
        None,           // 无变化
        RedrawAll,      // 全屏重绘
        DealCard,       // 发一张牌动画
        DrawCenter8,    // 画8张底牌
        RedrawMyHand,   // 重绘手牌
        DrawPlayedCards,// 画出牌
        ShowToolbar,    // 显示花色工具栏
        ShowPassImage,  // 显示过牌
        ShowBottomCards,// 显示底牌
        SetPause,       // 暂停
        ShowRoundWinner,// 显示一圈赢家
        ShowRankResult, // 显示一局结果
    }

    /// <summary>
    /// 一条渲染指令 = 画什么 + 数据。
    /// GdiRenderer 根据这个在 Bitmap 上画图。
    /// </summary>
    public class RenderCommand
    {
        public RenderCmdType Type { get; }
        public object Payload { get; }

        public RenderCommand(RenderCmdType type, object payload = null)
        {
            Type = type;
            Payload = payload;
        }
    }

    /// <summary>
    /// PlayerPlayCard / Engine.Tick 的返回值。
    /// </summary>
    public class PlayResult
    {
        public bool IsValid { get; set; } = true;
        public List<RenderCommand> RenderCommands { get; } = new List<RenderCommand>();

        public static PlayResult Invalid()
        {
            return new PlayResult { IsValid = false };
        }
    }
}
```

**这样改的原因：**
不打断现有任何代码，纯增量。后续步骤逐步往里填实现。

**怎么验证没改坏：**
编译不报错（`GameEngine.cs` 和 `RenderCommand.cs` 加入项目后不产生任何引用错误）。

**风险：**
无。

---

### 步骤2：提取 LayoutManager —— 把所有布局常量从 DrawingFormHelper 搬到新文件

DrawingFormHelper 到处硬编码数值（如 `30, 355, 600, 116` 是手牌区域、"小猪按钮"在 `296, 300, 53, 46`）。先统一管理。

**涉及文件：**
- `Tractor.net/Helpers/LayoutManager.cs`（新建）
- `CommonMethods.cs`（新增 GetSuit 常量的文本说明，不动代码）

**改动前：**
```csharp
// DrawingFormHelper.cs 各处硬编码坐标

// 行 1002: 画手牌区域
Rectangle rect = new Rectangle(30, 355, 600, 116);

// 行 1018: 手牌X起始偏移
int start = (int)((2780 - index * 75) / 10);

// DrawingFormHelper.cs 行 1126: 同一区域
Rectangle rect = new Rectangle(30, 355, 600, 116);

// MainForm_MouseClick 行 421:
(e.X >= (int)myCardsLocation[0] && e.X <= ((int)myCardsLocation[myCardsLocation.Count - 1] + 71)) && (e.Y >= 355 && e.Y < 472)

// MainForm_MouseClick 行 498: 小猪按钮区域
Rectangle pigRect = new Rectangle(296, 300, 53, 46);
```

**改动后：**
`Tractor.net/Helpers/LayoutManager.cs`:
```csharp
namespace Kuaff.Tractor.Helpers
{
    /// <summary>
    /// 游戏界面所有UI元素的布局常量。
    /// 方便统一调整——以后改布局只用改这里。
    /// </summary>
    public static class LayoutManager
    {
        // ===== 手牌区域 (My Cards) =====
        public const int MyCardsAreaX = 30;
        public const int MyCardsAreaY = 355;
        public const int MyCardsAreaWidth = 600;
        public const int MyCardsAreaHeight = 116;

        // 手牌X方向起始偏移公式依赖：2780 - index * 75

        // ===== 小猪按钮（出牌确认） =====
        public const int PigButtonX = 296;
        public const int PigButtonY = 300;
        public const int PigButtonWidth = 53;
        public const int PigButtonHeight = 46;

        // ===== 底牌区域 =====
        public const int BottomCardsStartX = 200;
        public const int BottomCardsY = 186;
        public const int CardWidth = 71;
        public const int CardHeight = 96;

        // ===== 中心8张底牌动画区域 =====
        public const int Center8CardsX = 200;
        public const int Center8CardsY = 186;
        public const int Center8CardsWidth = 90;
        public const int Center8CardsHeight = 96;

        // ===== 中心背景区域 =====
        public const int CenterBackgroundX = 77;
        public const int CenterBackgroundY = 121;
        public const int CenterBackgroundWidth = 477;
        public const int CenterBackgroundHeight = 254;

        // ===== 花色工具栏 =====
        public const int ToolbarX = 415;
        public const int ToolbarY = 325;
        public const int ToolbarWidth = 129;
        public const int ToolbarHeight = 29;

        // ===== 牌间距 =====
        public const int SelectedCardOffset = 13;  // 选中牌的Y偏移
    }
}
```

然后回到 DrawingFormHelper.cs 和 MainForm.cs，**逐个替换硬编码数值为 LayoutManager.XXX**（这个不需要一步做完，可以在后续每次改某个方法时顺便改。但先建好文件）。

**这样改的原因：**
预防后续步骤中的"找硬编码数值"的苦差事。所有有意义的数字先集中到这里。

**怎么验证没改坏：**
编译通过即可（新文件不引用旧代码，旧代码也不引用新文件。后续步骤逐步接入）。

**风险：**
无。纯增量。

---

### 步骤3：把 DrawingFormHelper 中**纯读取的渲染方法**搬到 GdiRenderer

> 安全：这些方法只读 mainForm 的字段来画图，不修改游戏状态。纯搬家。

**涉及文件：**
- `Tractor.net/Renderers/GdiRenderer.cs`（新建）
- `Tractor.net/Helpers/DrawingFormHelper.cs`（删除被搬走的方法定义，修改调用方指向新文件）

**挑选"只画图不改状态"的方法（共 6 个）：**

| 方法名 | 行号 | 说明 |
|--------|------|------|
| `DrawBackground(Graphics g)` | 2284-2289 | 画背景图，只读 `mainForm.image` |
| `DrawCenterImage()` | 175-184 | 画中间区域背景，只读 `mainForm.image` |
| `DrawPassImage()` | 186-201 | 画"过牌"图片，只调 DrawCenterImage + 画两张图 |
| `DrawScoreImage(int scores)` | 2162-2181 | 画分数，只读 `mainForm.image`、`mainForm.currentState` |
| `getPokerImageByNumber(int number)` | 2267-2279 | 读资源管理器获得牌图，纯工具方法 |
| `DrawSuit(Graphics g, int suit, bool me, bool b)` | 466-497 | 花色图标渲染，纯只读 |

**具体操作：**

**改动前**（GdiRenderer.cs 不存在）：

**改动后：**
`Tractor.net/Renderers/GdiRenderer.cs`:
```csharp
using System;
using System.Collections;
using System.Drawing;
using Kuaff.Tractor.Helpers;

namespace Kuaff.Tractor
{
    /// <summary>
    /// GDI+ 渲染器。
    /// 接收 RenderCommand 和 gameState，在 Bitmap 上执行绘制。
    /// 
    /// 规则：只读 gameState，不修改。所有副作用仅限于 Bitmap 绘制。
    /// </summary>
    public class GdiRenderer
    {
        private GameConfig _config;

        public GdiRenderer(GameConfig config)
        {
            _config = config;
        }

        /// <summary>
        /// 执行一条渲染指令。
        /// </summary>
        public void Execute(RenderCommand cmd, Bitmap bmp, CurrentState state)
        {
            // 本步骤只实现最基础的纯渲染方法
            // 后续步骤逐步扩展 switch case
        }

        #region 从 DrawingFormHelper 搬来的纯渲染方法

        /// <summary>
        /// 画背景。只读 mainForm.image 的等效数据。
        /// </summary>
        public void DrawBackground(Graphics g, Bitmap backgroundImage, Rectangle clientRect)
        {
            g.DrawImage(backgroundImage, clientRect, clientRect, GraphicsUnit.Pixel);
        }

        /// <summary>
        /// 画中间区域背景图。
        /// </summary>
        public void DrawCenterImage(Graphics g, Bitmap backgroundImage, Rectangle centerRect)
        {
            g.DrawImage(backgroundImage, centerRect, centerRect, GraphicsUnit.Pixel);
        }

        /// <summary>
        /// 获取牌的图片。
        /// </summary>
        public Bitmap GetPokerImageByNumber(int number)
        {
            if (_config.CardImageName.Length == 0)
            {
                return (Bitmap)_config.CardsResourceManager.GetObject("_" + number);
            }
            // 自定义图片走 cardsImages 数组——这个随后需要从外部传入
            return null;
        }

        /// <summary>
        /// 画花色图标。
        /// </summary>
        public void DrawSuit(Graphics g, int suit, bool me, bool b)
        {
            if (me)
            {
                if (b)
                {
                    // 我方花色(顶部大字)
                    g.DrawImage(Properties.Resources.CardSuit, new Rectangle(449, 20 + 4, 30, 35),
                        new Rectangle(0, 30 * (suit - 1) + 4, 30, 35), GraphicsUnit.Pixel);
                }
                else
                {
                    g.DrawImage(Properties.Resources.CardSuit, new Rectangle(449, 20, 30, 30),
                        new Rectangle(0, 30 * (suit - 1), 30, 30), GraphicsUnit.Pixel);
                }
            }
            else
            {
                // 对方花色
                g.DrawImage(Properties.Resources.CardSuit, new Rectangle(560, 20, 30, 30),
                    new Rectangle(0, 30 * (suit - 1), 30, 30), GraphicsUnit.Pixel);
            }
        }
        #endregion
    }
}
```

然后，在 DrawingFormHelper.cs 中**保留这些方法但标记为 Obsolete**，以便逐步迁移调用方：
```csharp
[Obsolete("迁移至 GdiRenderer，请在下次重构时替换")]
internal void DrawCenterImage()
{
    // ... 原有代码不变 ...
}
```

**这样改的原因：**
"只画不改状态"的方法是最安全的迁移对象——搬家后不影响任何游戏逻辑。

**怎么验证没改坏：**
编译通过。GdiRenderer 不被现有代码引用，DrawingFormHelper 方法不变。

**风险：**
- GdiRenderer.Execute 尚未连接，暂时无用
- 新建的 `Renderers/` 目录需要在 .csproj 中确认已包含（如果是旧式 .csproj 可能需要手动包含文件）

---

### 步骤4：把 timer_Tick 中最简单的状态分支搬到 Engine.Tick()

挑选 `Pause` 和 `WaitingShowPass` 这两个**不涉及 AI 和复杂渲染的**分支。

**涉及文件：**
- `Tractor.net/GameEngine.cs`（新增 `Tick()` 方法）
- `Tractor.net/MainForm.cs`（`timer_Tick` 中的 Pause 和 WaitingShowPass 分支改为调 Engine）
- `Tractor.net/RenderCommand.cs`（已存在，不修改）

#### 4a：给 GameEngine 添加 Tick 方法和内部状态

**改动前**（GameEngine.cs 只有空壳）：

**改动后：**
```csharp
public class GameEngine
{
    private CurrentState _state;
    private long _pauseStartTicks;
    private long _pauseMaxMs;
    private CardCommands _wakeupCommand;

    // 当前状态引用（供外部读取）
    public CurrentState State => _state;

    /// <summary>
    /// 游戏主循环 Tick。每次 timer_Tick 调用这个方法。
    /// 返回需要执行的渲染指令列表，ModelStateChanged 标记是否需要外部同步状态。
    /// </summary>
    public TickResult Tick(long nowTicks)
    {
        var result = new TickResult();

        switch (_state.CurrentCardCommands)
        {
            // ====== 步骤4：最简单分支 ======

            case CardCommands.Pause:
                long interval = (nowTicks - _pauseStartTicks) / 10000; // Ticks → ms
                if (interval > _pauseMaxMs)
                {
                    _state.CurrentCardCommands = _wakeupCommand;
                    result.StateChanged = true;
                }
                break;

            case CardCommands.WaitingShowPass:
                // 这个分支在 timer_Tick 中只调用了 two rendering methods + Refresh
                // 逻辑上只是视觉过渡，Engine 不需额外逻辑。
                // 但我们需要标记状态变化——等待外部调用方处理渲染。
                // 实际渲染由 MainForm 在拿到 TickResult 后处理。
                result.RenderCommands.Add(new RenderCommand(RenderCmdType.ShowPassImage));
                _state.CurrentCardCommands = CardCommands.ReadyCards;
                result.StateChanged = true;
                break;

            default:
                // 其他分支后续步骤实现
                break;
        }

        return result;
    }

    /// <summary>
    /// 设置暂停。等价于 MainForm.SetPauseSet。
    /// </summary>
    public void SetPause(int maxMs, CardCommands wakeup)
    {
        _pauseMaxMs = maxMs;
        _pauseStartTicks = DateTime.Now.Ticks;
        _wakeupCommand = wakeup;
        _state.CurrentCardCommands = CardCommands.Pause;
    }
}

public class TickResult
{
    public bool StateChanged { get; set; }
    public List<RenderCommand> RenderCommands { get; } = new List<RenderCommand>();
}
```

#### 4b：MainForm.timer_Tick 中对应的 case 改为调 Engine

**改动前**（MainForm.cs 行 880-897）：
```csharp
else if (currentState.CurrentCardCommands == CardCommands.Pause)
{
    long interval = (DateTime.Now.Ticks - sleepTime) / 10000;
    if (interval > sleepMaxTime)
    {
        currentState.CurrentCardCommands = wakeupCardCommands;
    }
}
```

以及（行 773-777）：
```csharp
else if (currentState.CurrentCardCommands == CardCommands.WaitingShowPass)
{
    drawingFormHelper.DrawCenterImage();
    Refresh();
    currentState.CurrentCardCommands = CardCommands.ReadyCards;
}
```

**改动后：**
```csharp
else if (currentState.CurrentCardCommands == CardCommands.Pause
      || currentState.CurrentCardCommands == CardCommands.WaitingShowPass)
{
    var tickResult = engine.Tick(DateTime.Now.Ticks);
    if (tickResult.StateChanged)
    {
        currentState = engine.State;  // 同步 Engine 内部修改
    }
    // 执行 Engine 返回的渲染指令
    foreach (var cmd in tickResult.RenderCommands)
        renderer.Execute(cmd, bmp, currentState);
    // WaitingShowPass 需要额外刷新的，还是由 form 处理
    if (currentState.CurrentCardCommands == CardCommands.ReadyCards)
    {
        drawingFormHelper.DrawCenterImage();
        Refresh();
    }
}
```

**多出的步骤：需要在 MainForm 中创建 engine 和 renderer 实例。**
```csharp
// MainForm 构造函数末尾（或字段初始化）
internal GameEngine engine = new GameEngine();
internal GdiRenderer renderer = new GdiRenderer(gameConfig);
```

**这样改的原因：**
Pause 分支是纯计时逻辑，WaitingShowPass 是纯视觉过渡。它们与 AI/复杂游戏逻辑无关，是最安全的迁移起点。

**怎么验证没改坏：**
1. 启动游戏，进入任何触发 Pause 的状态（如玩家出完一圈后）
2. 观察暂停过后是否能正确进入下一状态
3. AI vs AI 自动测试：点击"开始新游戏" → 观察游戏能否正常跑完一局

**风险：**
- Pause 分支依赖 `DateTime.Now.Ticks`，Engine 内部需要正确传递 `nowTicks`
- 如果 Engine.State 的 currentCardCommands 与 MainForm 不同步，会导致状态混乱
  解决：`[!] 每次 Engine 修改状态后都必须同步到 MainForm，通过 currentState = engine.State`

---

### 步骤5：把 ReadyCards 分支搬到 Engine.Tick() —— 发牌循环

这是游戏中频率最高的分支（25 次发牌 + AI 叫主），也是 DrawingFormHelper.ReadyCards 中混合逻辑最严重的地方。

**涉及文件：**
- `Tractor.net/GameEngine.cs`（Tick 中添加 ReadyCards case）
- `Tractor.net/MainForm.cs`（timer_Tick 中 ReadyCards 分支改为调 Engine）
- `Tractor.net/Algorithms/Algorithm.cs`（待 Engine 内部调用 AI 方法）

**改动前**（MainForm.cs 行 697-718）：
```csharp
if (currentState.CurrentCardCommands == CardCommands.ReadyCards)
{
    if (currentCount == 0)
    {
        if (!gameConfig.IsDebug)
        {
            drawingFormHelper.DrawToolbar();
        }
    }

    if (currentCount < 25)
    {
        drawingFormHelper.ReadyCards(currentCount);
        currentCount++;
    }
    else
    {
        currentState.CurrentCardCommands = CardCommands.DrawCenter8Cards;
    }
}
```

其中 `drawingFormHelper.ReadyCards(currentCount)` 内部（DrawingFormHelper.cs 行 38-99）做了以下操作：

1. **渲染**：擦除中心区域，画背面牌（58-count*2）
2. **数据修改**：`mainForm.currentPokers[0..3].AddCard(pokerList[i][count])`
3. **渲染**：画正面牌到自己区域、背面牌到其他三人区域
4. **AI 逻辑**：调用 `DoRankOrNot(mainForm.currentPokers[i], user)` 自动叫主
5. **渲染**：叫主后的花色、庄家标记
6. **Refresh** 多次

**改动后：**

`GameEngine.cs` Tick 中新增 ReadyCards 分支：
```csharp
case CardCommands.ReadyCards:
{
    var renderCmds = new List<RenderCommand>();

    if (_dealCount == 0 && !_isDebug)
    {
        renderCmds.Add(new RenderCommand(RenderCmdType.ShowToolbar));
    }

    if (_dealCount < 25)
    {
        // 数据逻辑：分配牌
        int card0 = _pokerLists[0][_dealCount];
        int card1 = _pokerLists[1][_dealCount];
        int card2 = _pokerLists[2][_dealCount];
        int card3 = _pokerLists[3][_dealCount];

        _currentPokers[0].AddCard(card0);
        _currentPokers[1].AddCard(card1);
        _currentPokers[2].AddCard(card2);
        _currentPokers[3].AddCard(card3);

        // AI 叫主逻辑（原 DrawingFormHelper.DoRankOrNot）
        TryAutoSetRankForPlayers();

        // 告诉渲染器：第 _dealCount 轮的4张牌已分配
        renderCmds.Add(new RenderCommand(RenderCmdType.DealCard, new DealCardPayload
        {
            Round = _dealCount,
            Cards = new int[] { card0, card1, card2, card3 },
            Player1CardImage = GetCardImagePath(card0),
        }));

        _dealCount++;
    }
    else
    {
        _state.CurrentCardCommands = CardCommands.DrawCenter8Cards;
    }

    result.RenderCommands.AddRange(renderCmds);
    result.StateChanged = true;
    break;
}
```

增加 Engine 内部辅助方法：
```csharp
private int _dealCount = 0;
private bool _isDebug = false;

private void TryAutoSetRankForPlayers()
{
    // 对玩家 2,3,4（AI 玩家）尝试自动叫主
    for (int user = 2; user <= 4; user++)
    {
        if (_state.Suit == 0)
        {
            int suit = Algorithm.ShouldSetRank_Migrated(
                _currentPokers, _state.Rank, user);
            if (suit > 0)
            {
                _state.Suit = suit;
                // ... 设置 showSuits, whoShowRank, Master 等
            }
        }
    }
    // 玩家 1（真人）不自动叫主——UI 生成按钮等用户点
}
```

> ⚠️ `Algorithm.ShouldSetRank_Migrated` 是 Algorithm.ShouldSetRank 的不依赖 `mainForm` 的副本。
> 在步骤6中我们会把整个 Algorithm 类改为接受 `GameState` 对象而非 `MainForm` 实例。
> 现在先用一个临时方法名，调用时手动传需要的字段。

**MainForm.timer_Tick 中对应分支改为：**
```csharp
else if (currentState.CurrentCardCommands == CardCommands.ReadyCards)
{
    TickResult tickResult = engine.Tick(DateTime.Now.Ticks);
    currentState = engine.State;
    foreach (var cmd in tickResult.RenderCommands)
    {
        // 根据 cmd.Type 调用 GdiRenderer 或 DrawingFormHelper（过渡期）
        switch (cmd.Type)
        {
            case RenderCmdType.ShowToolbar:
                drawingFormHelper.DrawToolbar();
                break;
            case RenderCmdType.DealCard:
                // 暂时用旧方法画——后续 Renderer 完善后再换
                drawingFormHelper.ReadyCards(((DealCardPayload)cmd.Payload).Round);
                break;
        }
    }

    // 如果发牌结束 -> state 已被 Engine 改为 DrawCenter8Cards
    if (currentState.CurrentCardCommands == CardCommands.DrawCenter8Cards)
    {
        // 调用步骤4-6逻辑……
    }
}
```

**这也意味着**：DrawingFormHelper.ReadyCards 内部的逻辑拆分。ReadyCards 中原有的 "AddCard" + "DoRankOrNot" 被移到了 Engine，只保留渲染部分。

> **最大挑战**：DoRankOrNot 是 DrawingFormHelper 的私有方法，且内部混合了渲染（DrawSuit, DrawRank, DrawMaster）+ 状态修改（showSuits, whoShowRank, currentState.Suit/Master）。需要拆为：
> 1. `DoRankOrNot_Logic(mainForm, currentPoker, user)` → 返回叫主决策，搬到 Engine
> 2. `DoRankOrNot_Render(mainForm, suit, master, user)` → 留在 DrawingFormHelper，或搬 GdiRenderer

**这样改的原因：**
ReadyCards 是发牌阶段的核心循环。把它搬到 Engine 后，Engine 就有完整的"初始化发牌"能力。

**怎么验证没改坏：**
1. 编译通过
2. 启动游戏，选择"开始新游戏"
3. 观察发牌动画是否正常（25轮发牌 + 叫主动画）
4. AI vs AI 自动测试：看看能否正常发完牌进入扣底阶段

**风险：**
**高**。ReadyCards 内部做了太多事，拆不好容易导致：
- 牌发错（顺序不对）
- 叫主逻辑失效（Suit/Master 没正确设置）
- 渲染不协调（牌的位置不对）

**回滚方案**：如果出问题，把 `timer_Tick` 中的 ReadyCards 分支恢复为旧代码，Engine 中的 ReadyCards case 注释掉。

---

### 步骤6：把 WaitingForSend + DrawCenter8Cards + WaitingForSending8Cards 搬到 Engine

> 这一步覆盖了 AI 出牌逻辑（最复杂部分）。

#### 6a：DrawCenter8Cards 分支

**改动前**（MainForm.cs 行 720-771）：
```csharp
else if (currentState.CurrentCardCommands == CardCommands.DrawCenter8Cards)
{
    if (drawingFormHelper.DoRankNot())
    {
        // 无人叫主 → pass / 强制定主
        if (gameConfig.IsPass)
        {
            init();
            isNew = false;
            drawingFormHelper.DrawPassImage();
            SetPauseSet(gameConfig.NoRankPauseTime, CardCommands.WaitingShowPass);
            return;
        }
        else
        {
            // 系统强制设主
            ArrayList bottom = new ArrayList();
            bottom.Add(pokerList[0][0]); ... // 取8张底牌
            int suit = CommonMethods.GetSuit((int)bottom[2]);
            currentState.Suit = suit;
            drawingFormHelper.DrawCenterImage();
            drawingFormHelper.DrawBottomCards(bottom);
            SetPauseSet(gameConfig.NoRankPauseTime, CardCommands.WaitingShowBottom);
            return;
        }
    }

    // 有人叫主的情况
    whoseOrder = currentState.Master;
    firstSend = whoseOrder;
    SetPauseSet(gameConfig.Get8CardsTime, CardCommands.DrawMySortedCards);
    drawingFormHelper.DrawCenter8Cards();
    initSendedCards();
    drawingFormHelper.DrawMySortedCards(currentPokers[0], currentPokers[0].Count);
    currentState.CurrentCardCommands = CardCommands.WaitingForSending8Cards;
    drawingFormHelper.DrawScoreImage(0);
}
```

**改动后**（GameEngine.Tick 中新增 case）：
```csharp
case CardCommands.DrawCenter8Cards:
{
    if (!_hasCalledRank)  // 之前 AI 没叫过主
    {
        var noRankResult = HandleNoRank();
        if (noRankResult.IsPass)
        {
            // 重新开局
            InitializeNewRound();
            result.RenderCommands.Add(new RenderCommand(RenderCmdType.ShowPassImage));
            SetPause(_config.NoRankPauseTime, CardCommands.WaitingShowPass);
        }
        else
        {
            // 强制设主
            _state.Suit = noRankResult.ForcedSuit;
            result.RenderCommands.Add(new RenderCommand(RenderCmdType.ShowBottomCards,
                new BottomCardsPayload { Cards = noRankResult.BottomCards }));
            SetPause(_config.NoRankPauseTime, CardCommands.WaitingShowBottom);
        }
    }
    else
    {
        // 有人叫主，正常流程
        _state.WhoseOrder = _state.Master;
        _state.FirstSend = _state.Master;

        // 把底牌8张发给庄家
        GiveCenter8CardsToMaster();

        // 重新解析所有玩家的手牌（含底牌）
        ReParseAllPlayers();

        SetPause(_config.Get8CardsTime, CardCommands.DrawMySortedCards);
        result.RenderCommands.Add(new RenderCommand(RenderCmdType.DrawCenter8,
            new Center8Payload { Master = _state.Master }));
        result.RenderCommands.Add(new RenderCommand(RenderCmdType.RedrawMyHand,
            new RedrawHandPayload { PlayerId = 1 }));
        _state.CurrentCardCommands = CardCommands.WaitingForSending8Cards;
    }
    result.StateChanged = true;
    break;
}

private class NoRankResult
{
    public bool IsPass;
    public int ForcedSuit;
    public List<int> BottomCards;
}

private NoRankResult HandleNoRank()
{
    // 调用旧 CommonMethods/GameConfig 逻辑
    var result = new NoRankResult();
    if (!_config.IsPass)
    {
        // 强制设主：取前8张底牌
        result.BottomCards = new List<int>
        {
            _pokerLists[0][0], _pokerLists[0][1],
            _pokerLists[1][0], _pokerLists[1][1],
            _pokerLists[2][0], _pokerLists[2][1],
            _pokerLists[3][0], _pokerLists[3][1],
        };
        result.ForcedSuit = CommonMethods.GetSuit(result.BottomCards[2]);
        result.IsPass = false;
    }
    else
    {
        result.IsPass = true;
    }
    return result;
}

private void GiveCenter8CardsToMaster()
{
    // 对应 DrawingFormHelper.Get8Cards
    int m = _state.Master;
    // 把其他三人手中第25、26张牌给庄家
    _pokerLists[m - 1].Add(_pokerLists[1][25]);
    _pokerLists[m - 1].Add(_pokerLists[1][26]);
    _pokerLists[m - 1].Add(_pokerLists[2][25]);
    _pokerLists[m - 1].Add(_pokerLists[2][26]);
    _pokerLists[m - 1].Add(_pokerLists[3][25]);
    _pokerLists[m - 1].Add(_pokerLists[3][26]);
    _pokerLists[1].RemoveAt(26); _pokerLists[1].RemoveAt(25);
    _pokerLists[2].RemoveAt(26); _pokerLists[2].RemoveAt(25);
    _pokerLists[3].RemoveAt(26); _pokerLists[3].RemoveAt(25);
}

private void ReParseAllPlayers()
{
    for (int i = 0; i < 4; i++)
    {
        _currentPokers[i] = CommonMethods.parse(
            _pokerLists[i], _state.Suit, _state.Rank);
    }
}
```

#### 6b：WaitingForSending8Cards —— AI 自动扣底牌

**改动前**（MainForm.cs 行 780-804）：
```csharp
else if (currentState.CurrentCardCommands == CardCommands.WaitingForSending8Cards)
{
    switch (currentState.Master)
    {
        case 1:
            if (gameConfig.IsDebug) Algorithm.Send8Cards(this, 1);
            else { drawingFormHelper.DrawMyPlayingCards(currentPokers[0]); Refresh(); return; }
            break;
        case 2: Algorithm.Send8Cards(this, 2); break;
        case 3: Algorithm.Send8Cards(this, 3); break;
        case 4: Algorithm.Send8Cards(this, 4); break;
    }
}
```

**改动后：**
```csharp
case CardCommands.WaitingForSending8Cards:
{
    int master = _state.Master;

    if (master == 1 && !_isDebug)
    {
        // 玩家手动操作——Engine 不做任何事
        // 渲染让 MainForm 画手牌选中UI
        result.RenderCommands.Add(new RenderCommand(
            RenderCmdType.WaitingForPlayerAction,
            new WaitPayload { WaitingMode = "Send8Cards" }));
        break;
    }

    // AI 自动扣牌
    Send8CardsForPlayer(master);
    _state.CurrentCardCommands = CardCommands.DrawMySortedCards;
    result.StateChanged = true;
    // 渲染由 MainForm 在收到 DrawMySortedCards 后触发
    break;
}
```

> 注：`Send8CardsForPlayer` 需要把旧的 `Algorithm.Send8Cards` 的 mainForm 依赖去掉。我们暂时在 Engine 内用传参方式调旧方法。

#### 6c：WaitingForSend —— AI 出牌

**改动前**（MainForm.cs 行 832-870）：
```csharp
else if (currentState.CurrentCardCommands == CardCommands.WaitingForSend)
{
    if (whoseOrder == 2) drawingFormHelper.DrawFrieldUserSendedCards();
    if (whoseOrder == 3) drawingFormHelper.DrawPreviousUserSendedCards();
    if (whoseOrder == 4) drawingFormHelper.DrawNextUserSendedCards();
    if (whoseOrder == 1)
    {
        if (gameConfig.IsDebug)
        {
            if (firstSend == 1) Algorithm.ShouldSendedCards(this, 1, ...);
            else Algorithm.MustSendedCards(this, 1, ...);
            drawingFormHelper.DrawMyFinishSendedCards();
            if (currentSendCards[3].Count > 0)
            {
                currentState.CurrentCardCommands = CardCommands.Pause;
                SetPauseSet(gameConfig.FinishedOncePauseTime, CardCommands.DrawOnceFinished);
            }
            else { whoseOrder = 4; currentState.CurrentCardCommands = CardCommands.WaitingForSend; }
        }
        else
        {
            currentState.CurrentCardCommands = CardCommands.WaitingForMySending;
        }
    }
}
```

注意：`DrawFrieldUserSendedCards()` / `DrawPreviousUserSendedCards()` / `DrawNextUserSendedCards()` 这些方法内部**既调用了 AI 出牌算法，又做了渲染**——这是最典型的混合点。

**改动后**（Engine.Tick 新增 case）：

```csharp
case CardCommands.WaitingForSend:
{
    int whoseOrder = _state.WhoseOrder;

    if (whoseOrder >= 2 && whoseOrder <= 4)
    {
        // AI 出牌
        List<int> aiCards = GetAIPlayedCards(whoseOrder);
        PlayResult playResult = PlayerPlayCard(whoseOrder, aiCards);
        result.StateChanged = true;
        result.RenderCommands.AddRange(playResult.RenderCommands);

        if (playResult.RenderCommands.Count == 0)
        {
            // 表示出牌后状态已变化，需要 MainForm 重新 Tick
            _needsReTick = true;
        }
    }
    else if (whoseOrder == 1)
    {
        if (_isDebug)
        {
            List<int> aiCards = GetAIPlayedCards(1);
            PlayResult playResult = PlayerPlayCard(1, aiCards);
            result.StateChanged = true;
            result.RenderCommands.AddRange(playResult.RenderCommands);
        }
        else
        {
            _state.CurrentCardCommands = CardCommands.WaitingForMySending;
            result.StateChanged = true;
        }
    }
    break;
}
```

其中 `GetAIPlayedCards` 从旧的 `Algorithm.ShouldSendedCards` / `Algorithm.MustSendedCards` 改造而来——改成接受 GameState 对象而非 mainForm：
```csharp
private List<int> GetAIPlayedCards(int playerId)
{
    if (_state.FirstSend == playerId)
    {
        // 首次出牌（可甩牌）
        return _shouldAlgo.ShouldSendedCards(
            _currentPokers, _pokerLists, _state, _userAlgorithms, playerId);
    }
    else
    {
        // 跟牌
        int firstPlayer = _state.FirstSend;
        int firstPlayerCardCount = _state.SendCards[firstPlayer - 1].Count;
        return _mustAlgo.MustSendedCards(
            _currentPokers, _pokerLists, _state, _userAlgorithms, playerId, firstPlayerCardCount);
    }
}
```

**这样改的原因：**
WaitingForSend 是 AI 驱动的核心出牌循环。把它搬到 Engine 后，Engine 就具备了独立驱动整场游戏的能力（纯数据层面）。

**怎么验证没改坏：**
1. AI vs AI 测试：开启调试模式，点击"开始新游戏"
2. 观察游戏是否能从发牌 → 扣底 → 出牌 → 结算完整跑完
3. 检查每圈出牌结果是否正确（谁赢、分数是否正确）

**风险：**
**最高**。Algorithm.ShouldSendedCards / MustSendedCards 内部引用 mainForm 大量字段。改为接受 GameState 后需要确保所有引用都正确映射。
建议先做：
- 创建一个 `GameState` 类，包含 Engine 需要的所有字段
- 把 Algorithm 系列类改为接受 `GameState` 而非 `MainForm`
- 在 MainForm 中创建一个 `GameState` 实例，Engine 持有引用

---

### 步骤7：将 MainForm_MouseClick 的事件逻辑改为调 Engine.PlayerPlayCard

**涉及文件：**
- `Tractor.net/MainForm.cs`（MainForm_MouseClick 方法，行 409-558）
- `Tractor.net/GameEngine.cs`（PlayerPlayCard 方法的出牌/扣牌实现）

#### 7a：玩家扣底牌（WaitingForSending8Cards 分支）

**改动前**（MainForm.cs 行 503-534）：
```csharp
if (currentState.CurrentCardCommands == CardCommands.WaitingForSending8Cards)
{
    // 收集已选中的8张牌
    ArrayList readyCards = new ArrayList();
    for (int i = 0; i < myCardIsReady.Count; i++)
        if ((bool)myCardIsReady[i]) readyCards.Add((int)myCardsNumber[i]);

    if (readyCards.Count == 8)
    {
        send8Cards = new ArrayList();
        for (int i = 0; i < 8; i++)
        {
            CommonMethods.SendCards(send8Cards, currentPokers[0], pokerList[0], (int)readyCards[i]);
        }
        initSendedCards();
        currentState.CurrentCardCommands = CardCommands.DrawMySortedCards;
    }
}
```

**改动后：**
```csharp
// MouseClick 中，当点击小猪按钮且状态为 WaitingForSending8Cards：
if (currentState.CurrentCardCommands == CardCommands.WaitingForSending8Cards)
{
    // 收集已选中的牌号
    List<int> selectedNumbers = new List<int>();
    for (int i = 0; i < myCardIsReady.Count; i++)
        if ((bool)myCardIsReady[i]) selectedNumbers.Add((int)myCardsNumber[i]);

    if (selectedNumbers.Count != 8) return;  // 不算8张不能点

    // 交给 Engine 处理
    PlayResult result = engine.PlayerSend8Cards(selectedNumbers);
    currentState = engine.State;  // 同步状态

    // 执行渲染：先清除小猪、重绘手牌
    Graphics g = Graphics.FromImage(bmp);
    g.DrawImage(image, pigRect, pigRect, GraphicsUnit.Pixel);
    g.Dispose();

    foreach (var cmd in result.RenderCommands)
        renderer.Execute(cmd, bmp, currentState);

    // 手动处理 DrawMySortedCards 的过渡
    if (currentState.CurrentCardCommands == CardCommands.DrawMySortedCards)
    {
        drawingFormHelper.DrawMySortedCards(currentPokers[0], currentPokers[0].Count);
        Refresh();
    }
}
```

Engine 中新增 `PlayerSend8Cards`：
```csharp
public PlayResult PlayerSend8Cards(List<int> selectedNumbers)
{
    if (selectedNumbers.Count != 8)
        return PlayResult.Invalid();

    var result = new PlayResult();

    // 把8张牌从玩家手牌移到 send8Cards
    foreach (int number in selectedNumbers)
    {
        _state.Send8Cards.Add(number);
        _currentPokers[0].RemoveCard(number);
        _pokerLists[0].Remove(number);
    }

    // 重新解析手牌
    _currentPokers[0] = CommonMethods.parse(_pokerLists[0], _state.Suit, _state.Rank);
    _currentPokers[1] = CommonMethods.parse(_pokerLists[1], _state.Suit, _state.Rank);
    _currentPokers[2] = CommonMethods.parse(_pokerLists[2], _state.Suit, _state.Rank);
    _currentPokers[3] = CommonMethods.parse(_pokerLists[3], _state.Suit, _state.Rank);

    _state.CurrentCardCommands = CardCommands.DrawMySortedCards;

    result.RenderCommands.Add(new RenderCommand(RenderCmdType.RedrawMyHand,
        new RedrawHandPayload { PlayerId = 1 }));
    result.StateChanged();

    return result;
}
```

#### 7b：玩家出牌（WaitingForMySending 分支）

**改动前**（MainForm.cs 行 535-558）——见第一部分"改造前路径"的完整代码。

**改动后：**
```csharp
else if (currentState.CurrentCardCommands == CardCommands.WaitingForMySending)
{
    // 收集已选中的牌号
    List<int> selected = new List<int>();
    for (int i = 0; i < myCardIsReady.Count; i++)
        if ((bool)myCardIsReady[i]) selected.Add((int)myCardsNumber[i]);

    if (selected.Count == 0) return;

    // 交给 Engine 处理出牌
    PlayResult result = engine.PlayerPlayCard(1, selected);

    if (!result.IsValid)
    {
        // 非法出牌（如花色不对）——Engine 不修改状态，显示错误提示
        return;
    }

    currentState = engine.State;

    // 清除小猪按钮
    Graphics g = Graphics.FromImage(bmp);
    g.DrawImage(image, pigRect, pigRect, GraphicsUnit.Pixel);
    g.Dispose();

    // 执行渲染指令
    foreach (var cmd in result.RenderCommands)
        renderer.Execute(cmd, bmp, currentState);

    Refresh();
}
```

**这样改的原因：**
把 MouseClick 从一个 150 行的"什么都干"的方法，变成 20 行的"捕获输入 → 调 Engine → 渲染结果"的清晰流程。

**怎么验证没改坏：**
1. 启动游戏（非调试模式）
2. 等发牌完成，进入出牌阶段
3. 左键选牌，右键批量选牌，点击小猪按钮出牌
4. 确认：牌从手牌消失、出现在桌面、轮到下一家

**风险：**
- **中等**。`TractorRules.IsInvalid` 依赖 `myCardIsReady` 和 `myCardsNumber`，这两个在 Engine 中不存在（因为它们是 UI 选择状态）。解决方案：Engine 只需要**牌号列表**和**当前游戏状态**来判断合法性，不需要 `myCardIsReady`。
- `firstSend == 1` 时的甩牌逻辑（`TractorRules.CheckSendCards`）同样需要改造为接受 GameState 对象

---

### 步骤8：清理 DrawingFormHelper 中不再用的代码

**涉及文件：**
- `Tractor.net/Helpers/DrawingFormHelper.cs`
- `Tractor.net/Renderers/GdiRenderer.cs`

**具体操作：**

当步骤3-7完成后，以下方法在 DrawingFormHelper 中不再被引用：

| 方法 | 原因 | 处理方式 |
|------|------|----------|
| `ReadyCards(int count)` | 发牌逻辑已搬 Engine，渲染半搬 GdiRenderer | 拆为纯渲染方法留在 DrawingFormHelper，或移 GdiRenderer |
| `DoRankOrNot(CurrentPoker, int)` | 叫主逻辑已搬 Engine | 删除 |
| `MyRankOrNot(CurrentPoker)` | 玩家的叫主UI由 Engine 判断可叫花色 | 移 GdiRenderer |
| `DrawCenter8Cards()` | Engine 控制流程，渲染移 GdiRenderer | 移 GdiRenderer |
| `DrawBottomCards(ArrayList)` | 渲染，不修改状态 | 移 GdiRenderer |
| `DrawMyFinishSendedCards()` | **混合**：渲染 + AI调用 + 状态修改 | 拆分：AI逻辑→Engine，渲染→GdiRenderer，状态修改→Engine |
| `DrawNextUserSendedCards()` | **混合**：AI出牌 + 渲染 | 拆分 |
| `DrawFrieldUserSendedCards()` | **混合** | 拆分 |
| `DrawPreviousUserSendedCards()` | **混合** | 拆分 |
| `DrawFinishedOnceSendedCards()` | **混合**：Debug回滚 + 算赢家 + 渲染 | 拆分 |
| `DrawFinishedSendedCards()` | **混合**：记分 + 渲染 | 拆分 |

**最终 DrawingFormHelper 应该缩减为：**
- 纯渲染辅助方法（GdiRenderer 使用的私有绘图函数）
- 或者整个删掉，全部由 GdiRenderer 替代

**安全策略：**
1. 每删一个方法之前，先在项目中搜索该方法的引用
2. 如果还有引用，不删，只加 `[Obsolete("请改用 GdiRenderer")]`
3. 当引用为 0 时再删

**示例——检查 ReadyCards 引用：**
```bash
# 在项目目录下运行
grep -r "ReadyCards" *.cs --include="*.cs"
# 输出应该只有 DrawingFormHelper.cs 中的定义（MainForm 中的调用已被 Engine 替代）
```

**这样改的原因：**
减少重复代码。分开逻辑和渲染后，DrawingFormHelper 的职责自然缩减为"辅助渲染函数库"，最终被 GdiRenderer 完全替代。

**怎么验证没改坏：**
每删一个方法后编译 + AI vs AI 自动测试跑一遍。

**风险：**
**低**（只要引用检查到位）。删之前用 grep 确认没有其他调用者。

---

## 第三部分：中间态（过渡期长什么样）

### 改造进行到一半（比如做完步骤5、正在做步骤6）

**GameEngine 已经存在但某些状态还在 MainForm → 怎么共存？**

```
MainForm.currentState + MainForm.sleepTime / sleepMaxTime / wakeupCardCommands
    ↓ 部分同步
Engine._state（Engine 内部维护不同的 _dealCount、_needsReTick 等）
```

共存规则：
- `[!]` **Engine 是状态的事实来源（source of truth）只有 Engine 修改游戏状态**
- MainForm 在收到 `TickResult.StateChanged == true` 后执行 `currentState = engine.State`
- MainForm 保留自己的 `sleepTime/sleepMaxTime/wakeupCardCommands` 直到 Pause 分支完全移到 Engine
- `currentCount`（发牌计数器）*不*再被 MainForm 使用——移到 Engine 作为 `_dealCount`

但 Pause 分支是个特殊情况：MainForm 和 Engine 同时维护暂停状态，会导致混乱。所以**建议把 Pause 分支（步骤4）放在其他分支之前完成**。

### DrawingFormHelper 部分方法已迁走、部分还在 → 会不会冲突？

**不会冲突**，因为 DrawingFormHelper 中的方法都是被 MainForm.timer_Tick 和 MouseClick 调用的。

只要调用方（MainForm）在切换期间保持**同一时间只用一种方式**：
- 对于已迁到 Engine 的分支：MainForm 调 `engine.Tick()` 再用 GdiRenderer 渲染
- 对于还没迁的分支：MainForm 照常调 `drawingFormHelper.XXX()`

**如果一个方法已被拆成"Engine 逻辑部分 + GdiRenderer 渲染部分"，中间态怎么做？**

举例：`DrawMyFinishSendedCards()` 的拆分过程

中间态：
```csharp
// MainForm.timer_Tick 中（对应 WaitingForMySending 处理完成之后）
// 步骤6进行到一半时的代码

// AI出牌逻辑已从 DrawXxxUserSendedCards() 中抽出到 Engine
TickResult r = engine.Tick(DateTime.Now.Ticks);
currentState = engine.State;

// 渲染——方法还在 DrawingFormHelper，但已去掉内部的 AI 调用和状态修改
foreach (var cmd in r.RenderCommands)
{
    if (cmd.Type == RenderCmdType.DrawPlayedCards)
    {
        // 暂时用 DrawingFormHelper 的纯渲染包装
        drawingFormHelper.DrawPlayedCardsOnly(
            ((PlayedCardsPayload)cmd.Payload).PlayerId,
            ((PlayedCardsPayload)cmd.Payload).Cards);
    }
    else if (cmd.Type == RenderCmdType.RedrawMyHand)
    {
        drawingFormHelper.DrawMySortedCards(currentPokers[0], currentPokers[0].Count);
    }
}
Refresh();
```

DrawingFormHelper 中新增**只渲染不修改状态**的方法：
```csharp
/// <summary>
/// 【纯渲染】画已出牌。不修改状态。不调 AI。
/// 后续会搬到 GdiRenderer。
/// </summary>
internal void DrawPlayedCardsOnly(int playerId, ArrayList cards)
{
    // 原来 DrawMySendedCardsAction / DrawNextUserSendedCardsAction 等中的纯绘图代码
    // 删掉 AI 调用（Algorithm.MustSendedCards / ShouldSendedCards）
    // 删掉状态修改（mainForm.whoseOrder = ... 等）
}
```

### 什么是能跑的、什么是暂时断开的

**中间态下：**

| 功能 | 状态 | 说明 |
|------|------|------|
| 发牌（ReadyCards） | ✅ 能跑 | 已经搬入 Engine.Tick()，用旧渲染 |
| 暂停（Pause） | ✅ 能跑 | 已搬入 Engine.Tick() |
| 过牌（WaitingShowPass） | ✅ 能跑 | 已搬 |
| 发底牌（DrawCenter8Cards） | ⚠️ 部分 | 无主分支可能还在 MainForm |
| 扣底牌（WaitingForSending8Cards） | ⚠️ 部分 | AI 扣底已搬，玩家扣底还在 MouseClick |
| 出牌（WaitingForSend / WaitingForMySending） | 🔴 断 | 步骤6（最复杂）进行中 |
| 一圈结算（DrawOnceFinished） | 🔴 断 | 依赖出牌完成 |
| 一局结算（DrawOnceRank） | 🔴 断 | 依赖前面所有 |
| 鼠标点击选牌 | ✅ 能跑 | 不依赖 Engine |
| 鼠标点击出牌/扣底 | 🔴 断 | 需要 Engine.PlayerPlayCard 工作 |

**调试方式：**
- 关闭调试模式（非 IsDebug）后，游戏会停在 WaitingForMySending 等玩家点击
- 打开调试模式（IsDebug）后，AI 自动出牌流程如果在步骤6中正确实现，应能跑完整局

---

## 第四部分：怎么保证不跑偏

### 1. 每次改完必做回归测试

**具体操作（AI vs AI 自动测试）：**

```
1. 编译：dotnet build 或 VS 中 Ctrl+Shift+B
   确认 0 errors, 0 warnings

2. 启动游戏：
   - 调试 VS 启动
   - 或直接双击 exe

3. 点击菜单：游戏 → 开始新游戏

4. 修改配置（如果需要）：
   - 确认"调试模式"已勾选（菜单：游戏 → 调试模式）
   - 确认"自动出牌"已生效

5. 观察游戏：
   - 自动发牌（25轮）是否正常
   - 自动叫主是否正常
   - 自动扣底牌是否正常
   - 一圈出牌是否正常（4人各出一张 / 多张拖拉机）
   - 一圈结算是否正确
   - 多局游戏是否能正常切换

6. 如果卡死/异常/画面错误 → 立刻停止，grep 检查相关代码
```

### 2. 如果出错了怎么回溯

**方法 1：Git 版本控制**
```bash
# 每完成一个步骤（或子步骤）提交一次
git add -A
git commit -m "步骤4: 搬 Pause + WaitingShowPass 到 Engine.Tick"

# 出错了看 diff
git diff HEAD~1 -- Tractor.net/MainForm.cs

# 不行就回退
git checkout HEAD~1 -- Tractor.net/MainForm.cs
git checkout HEAD~1 -- Tractor.net/GameEngine.cs
```

**方法 2：备份 + 隔离**
```bash
# 修改前备份关键文件
cp Tractor.net/MainForm.cs Tractor.net/MainForm.cs.bak

# 出错了恢复
cp Tractor.net/MainForm.cs.bak Tractor.net/MainForm.cs
```

**方法 3：编译快照**
```bash
# 步骤N完成后的编译产物
dotnet build -o bin/stepN/
```

### 3. 每个步骤的通过条件清单

| 步骤 | 通过条件 |
|------|---------|
| 步骤1 | `dotnet build` 无错误 |
| 步骤2 | `dotnet build` 无错误 |
| 步骤3 | `dotnet build` 无错误。启动游戏画面正确（背景、中心图、花色） |
| 步骤4 | 游戏进入 Pause 后能正确恢复。WaitingShowPass 能正常显示。 |
| 步骤5 | AI vs AI 模式下能完整发完25轮牌并进入扣底阶段。发牌数量正确。 |
| 步骤6 | AI vs AI 能完整跑完一局游戏（从发牌到结算）。每一圈出牌逻辑正确。 |
| 步骤7 | 手动模式：选牌、点小猪出牌、牌从手牌消失出现在桌面。非法出牌被阻止。 |
| 步骤8 | `dotnet build` 无错误。功能与步骤7一致。DrawingFormHelper 无[Obsolete]方法被调用。 |

### 4. 每个步骤的最大工作量估计

| 步骤 | 预计新增行数 | 预计修改行数 | 预计时间 |
|------|-------------|-------------|---------|
| 步骤1 | ~80 | 0 | 15分钟 |
| 步骤2 | ~60 | 0 | 15分钟 |
| 步骤3 | ~150 | ~10 (添加 Obsolete 属性) | 30分钟 |
| 步骤4 | ~100 (Engine) + ~50 (MainForm) | ~30 (MainForm) | 1小时 |
| 步骤5 | ~200 (Engine) | ~50 (MainForm) | 3小时 |
| 步骤6 | ~500 (Engine + GameState) | ~200 (MainForm + Algorithm) | 8小时 |
| 步骤7 | ~150 (Engine) | ~100 (MainForm_MouseClick) | 3小时 |
| 步骤8 | 0 | ~300 (DrawingFormHelper 删除) | 2小时 |
| **合计** | **~1240** | **~740** | **约 18 小时** |

---

## 附录：GameState 类的设计建议

在改造之前（或步骤5之前），先创建一个 `GameState` 类，把 MainForm 中的游戏状态字段全搬进去：

```csharp
/// <summary>
/// 纯数据对象。游戏状态的全量快照。
/// 不包含任何方法逻辑。
/// Engine 修改这个对象，Renderer 读取这个对象绘制，MainForm 用它同步 UI。
/// </summary>
public class GameState
{
    // ===== 游戏配置（只读引用） =====
    public GameConfig Config { get; set; }

    // ===== 卡牌数据 =====
    public List<int>[] PokerLists { get; set; }  // 4个玩家的原始手牌列表
    public CurrentPoker[] CurrentPokers { get; set; }  // 4个玩家的结构化统计
    public CurrentPoker[] CurrentAllSendPokers { get; set; }  // 所有已出的牌
    public ArrayList[] CurrentSendCards { get; set; }  // 当前圈各玩家出牌
    public List<int> Send8Cards { get; set; }  // 底牌8张

    // ===== 游戏状态 =====
    public CurrentState State { get; set; }
    public int CurrentRank { get; set; }  // 当前 Rank
    public bool IsNew { get; set; }       // 是否新局
    public int ShowSuits { get; set; }    // 叫主次数
    public int WhoShowRank { get; set; }  // 谁叫的主
    public int WhoseOrder { get; set; }   // 轮到谁
    public int FirstSend { get; set; }    // 一圈中先出者
    public int WhoIsBigger { get; set; }  // 当前圈谁最大
    public int Scores { get; set; }       // 累计得分
    public int DealCount { get; set; }    // 发牌到第几张

    // ===== 暂停计时器（Engine 内部用） =====
    public long PauseStartTicks { get; set; }
    public long PauseMaxMs { get; set; }
    public CardCommands WakeupCommand { get; set; }
}
```

**Engine 持有 GameState 并直接修改：**
```csharp
public class GameEngine
{
    private GameState _state;

    public GameState State => _state;

    public GameEngine()
    {
        _state = new GameState();
    }

    public TickResult Tick(long nowTicks)
    {
        // ... 所有状态修改通过 _state.XXX 进行
    }
}
```

**MainForm 与 GameState 同步：**
```csharp
// MainForm 构造函数中创建 Engine，并绑定状态
engine = new GameEngine();
// 每当 Engine.Tick 返回 stateChanged，就：
var state = engine.State;
currentState = state.State;
currentRank = state.CurrentRank;
pokerList = state.PokerLists;
// ... 其他字段同理

// 或者更简单：MainForm 持有的字段全都替换为读取 GameState
```

> **建议步骤顺序**：先做 GameState 类（30分钟），然后把 MainForm 的字段逐个改为 GameState 的引用（2小时）。这一步独立于步骤1-8，做了之后后续迁移更简单。
