using System;
using System.Collections;
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

        // ====== 步骤5：发牌相关 ======
        private int _dealCount = 0;
        private bool _isDebug = false;

        // 外部传入的数据引用
        private ArrayList[] _pokerLists;
        private CurrentPoker[] _currentPokers;

        /// <summary>
        /// 当前状态引用（供外部读取）。
        /// </summary>
        public CurrentState State => _state;

        /// <summary>
        /// 当前发牌轮数。
        /// </summary>
        public int DealCount => _dealCount;

        public GameEngine()
        {
            _state = new CurrentState(0, 0, 0, 0, 0, 0, CardCommands.ReadyCards);
        }

        /// <summary>
        /// 设置牌数据引用（MainForm 在开始新游戏时调用）。
        /// </summary>
        public void SetGameData(ArrayList[] pokerLists, CurrentPoker[] currentPokers, bool isDebug)
        {
            _pokerLists = pokerLists;
            _currentPokers = currentPokers;
            _isDebug = isDebug;
        }

        /// <summary>
        /// 重置引擎为新游戏状态。
        /// </summary>
        public void NewGame()
        {
            _state = new CurrentState(0, 0, 0, 0, 0, 0, CardCommands.ReadyCards);
            _dealCount = 0;
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
                    long interval = (nowTicks - _pauseStartTicks) / 10000;
                    if (interval > _pauseMaxMs)
                    {
                        _state.CurrentCardCommands = _wakeupCommand;
                        result.StateChanged = true;
                    }
                    break;

                case CardCommands.WaitingShowPass:
                    result.RenderCommands.Add(new RenderCommand(RenderCmdType.ShowPassImage));
                    _state.CurrentCardCommands = CardCommands.ReadyCards;
                    result.StateChanged = true;
                    break;

                // ====== 步骤5：发牌循环 ======
                case CardCommands.ReadyCards:
                {
                    if (_dealCount == 0 && !_isDebug)
                    {
                        result.RenderCommands.Add(new RenderCommand(RenderCmdType.ShowToolbar));
                    }

                    if (_dealCount < 25)
                    {
                        // 数据逻辑：分配牌
                        if (_pokerLists != null && _currentPokers != null)
                        {
                            int card0 = (int)_pokerLists[0][_dealCount];
                            int card1 = (int)_pokerLists[1][_dealCount];
                            int card2 = (int)_pokerLists[2][_dealCount];
                            int card3 = (int)_pokerLists[3][_dealCount];

                            _currentPokers[0].AddCard(card0);
                            _currentPokers[1].AddCard(card1);
                            _currentPokers[2].AddCard(card2);
                            _currentPokers[3].AddCard(card3);
                        }

                        // 通知渲染器：第 _dealCount 轮的4张牌已分配
                        result.RenderCommands.Add(new RenderCommand(
                            RenderCmdType.DealCard,
                            new DealCardPayload { Round = _dealCount }));

                        _dealCount++;
                        result.StateChanged = true;
                    }
                    else
                    {
                        // 25 轮发牌完毕
                        _state.CurrentCardCommands = CardCommands.DrawCenter8Cards;
                        result.StateChanged = true;
                    }
                    break;
                }

                default:
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
        public bool StateChanged { get; set; }
        public List<RenderCommand> RenderCommands { get; } = new List<RenderCommand>();
    }

    /// <summary>
    /// 发牌指令的数据负载。
    /// </summary>
    public class DealCardPayload
    {
        public int Round { get; set; }
    }
}
