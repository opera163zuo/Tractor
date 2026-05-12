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
        private CurrentState _state;
        private long _pauseStartTicks;
        private long _pauseMaxMs;
        private CardCommands _wakeupCommand;

        /// <summary>
        /// 当前状态引用（供外部读取）。
        /// </summary>
        public CurrentState State => _state;

        public GameEngine()
        {
            _state = new CurrentState(0, 0, 0, 0, 0, 0, CardCommands.ReadyCards);
        }

        /// <summary>
        /// 重置引擎为新游戏状态。
        /// </summary>
        public void NewGame()
        {
            _state = new CurrentState(0, 0, 0, 0, 0, 0, CardCommands.ReadyCards);
        }

        /// <summary>
        /// 游戏主循环 Tick。每次 timer_Tick 调用这个方法。
        /// 返回需要执行的渲染指令列表和状态变更标记。
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
                    // 这个分支在 timer_Tick 中只调用了 DrawCenterImage + Refresh
                    // 逻辑上只是视觉过渡，Engine 只需标记状态变化。
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

        /// <summary>
        /// 玩家出牌。暂不实现。
        /// </summary>
        public PlayResult PlayerPlayCard(int playerId, List<int> selectedCards)
        {
            throw new NotImplementedException("步骤6-7实现");
        }
    }

    /// <summary>
    /// Engine.Tick 的返回结果。
    /// </summary>
    public class TickResult
    {
        /// <summary>
        /// Engine 内部状态是否发生变化（需要外部同步）。
        /// </summary>
        public bool StateChanged { get; set; }

        /// <summary>
        /// 需要外部执行的渲染指令列表。
        /// </summary>
        public List<RenderCommand> RenderCommands { get; } = new List<RenderCommand>();
    }
}
