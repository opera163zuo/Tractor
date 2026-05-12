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

        // ====== 步骤6：AI 出牌相关 ======
        private int _whoseOrder = 0;
        private int _firstSend = 0;
        private int _whoIsBigger = 0;
        private bool _hasCalledRank = false;

        // 外部传入的数据引用
        private ArrayList[] _pokerLists;
        private CurrentPoker[] _currentPokers;
        private ArrayList[] _currentSendCards;
        private ArrayList _send8Cards;
        private GameConfig _config;

        /// <summary>
        /// 当前状态引用（供外部读取）。
        /// </summary>
        public CurrentState State => _state;

        /// <summary>
        /// 当前发牌轮数。
        /// </summary>
        public int DealCount => _dealCount;

        // ====== 步骤6-7：引擎驱动的游戏字段供外部同步 ======
        public int WhoseOrder => _whoseOrder;
        public int FirstSend => _firstSend;
        public int WhoIsBigger => _whoIsBigger;
        public int Scores { get; set; }

        public GameEngine()
        {
            _state = new CurrentState(0, 0, 0, 0, 0, 0, CardCommands.ReadyCards);
        }

        /// <summary>
        /// 设置游戏数据引用（MainForm 在开始新游戏时调用）。
        /// </summary>
        public void SetGameData(ArrayList[] pokerLists, CurrentPoker[] currentPokers,
                                ArrayList[] currentSendCards, ArrayList send8Cards,
                                GameConfig config, bool isDebug)
        {
            _pokerLists = pokerLists;
            _currentPokers = currentPokers;
            _currentSendCards = currentSendCards;
            _send8Cards = send8Cards;
            _config = config;
            _isDebug = isDebug;
        }

        /// <summary>
        /// 重置引擎为新游戏状态。
        /// </summary>
        public void NewGame()
        {
            _state = new CurrentState(0, 0, 0, 0, 0, 0, CardCommands.ReadyCards);
            _dealCount = 0;
            _whoseOrder = 0;
            _firstSend = 0;
            _whoIsBigger = 0;
            _hasCalledRank = false;
            Scores = 0;
        }

        /// <summary>
        /// 游戏主循环 Tick。每次 timer_Tick 调用这个方法。
        /// </summary>
        public TickResult Tick(long nowTicks)
        {
            var result = new TickResult();

            switch (_state.CurrentCardCommands)
            {
                // ====== 步骤4：Pause ======
                case CardCommands.Pause:
                {
                    long interval = (nowTicks - _pauseStartTicks) / 10000;
                    if (interval > _pauseMaxMs)
                    {
                        _state.CurrentCardCommands = _wakeupCommand;
                        result.StateChanged = true;
                    }
                    break;
                }

                // ====== 步骤4：WaitingShowPass ======
                case CardCommands.WaitingShowPass:
                {
                    result.RenderCommands.Add(new RenderCommand(RenderCmdType.ShowPassImage));
                    _state.CurrentCardCommands = CardCommands.ReadyCards;
                    result.StateChanged = true;
                    break;
                }

                // ====== 步骤5：发牌 ReadyCards ======
                case CardCommands.ReadyCards:
                {
                    if (_dealCount == 0 && !_isDebug)
                    {
                        result.RenderCommands.Add(new RenderCommand(RenderCmdType.ShowToolbar));
                    }

                    if (_dealCount < 25)
                    {
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

                        result.RenderCommands.Add(new RenderCommand(
                            RenderCmdType.DealCard,
                            new DealCardPayload { Round = _dealCount }));

                        _dealCount++;
                        result.StateChanged = true;
                    }
                    else
                    {
                        _state.CurrentCardCommands = CardCommands.DrawCenter8Cards;
                        result.StateChanged = true;
                    }
                    break;
                }

                // ====== 步骤6a：DrawCenter8Cards ======
                case CardCommands.DrawCenter8Cards:
                {
                    result.RenderCommands.Add(new RenderCommand(RenderCmdType.DrawCenter8));
                    // 状态由 MainForm 处理（复杂的叫主逻辑暂时留在 MainForm）
                    // Engine 只发指令，不修改状态
                    result.StateChanged = true;
                    break;
                }

                // ====== 步骤6b：WaitingForSending8Cards（AI扣底） ======
                case CardCommands.WaitingForSending8Cards:
                {
                    int master = _state.Master;
                    bool shouldAutoSend = (master != 1 || _isDebug);

                    if (shouldAutoSend && _config != null)
                    {
                        // AI 自动扣牌 - 调用旧 Algorithm（后续改造为 Engine 内部方法）
                        // Engine 发送渲染指令由 MainForm 处理
                        result.RenderCommands.Add(new RenderCommand(
                            RenderCmdType.WaitingForPlayerAction,
                            new WaitPayload { WaitingMode = "AutoSend8Cards", PlayerId = master }));
                    }
                    else
                    {
                        // 玩家手动扣牌 - 等待 MouseClick
                        result.RenderCommands.Add(new RenderCommand(
                            RenderCmdType.WaitingForPlayerAction,
                            new WaitPayload { WaitingMode = "Send8Cards", PlayerId = 1 }));
                    }
                    result.StateChanged = true;
                    break;
                }

                // ====== 步骤6c：WaitingForSend（AI出牌） ======
                case CardCommands.WaitingForSend:
                {
                    if (_whoseOrder >= 2 && _whoseOrder <= 4)
                    {
                        // AI 出牌 - Engine 发送指令让 MainForm 调用旧 DrawingFormHelper
                        result.RenderCommands.Add(new RenderCommand(
                            RenderCmdType.AiPlayCard,
                            new AiPlayPayload { PlayerId = _whoseOrder }));
                    }
                    else if (_whoseOrder == 1)
                    {
                        if (_isDebug)
                        {
                            result.RenderCommands.Add(new RenderCommand(
                                RenderCmdType.AiPlayCard,
                                new AiPlayPayload { PlayerId = 1 }));
                        }
                        else
                        {
                            _state.CurrentCardCommands = CardCommands.WaitingForMySending;
                            result.StateChanged = true;
                        }
                    }
                    else
                    {
                        // shouldn't happen, but handle gracefully
                        _state.CurrentCardCommands = CardCommands.WaitingForMySending;
                        result.StateChanged = true;
                    }
                    break;
                }

                default:
                    break;
            }

            return result;
        }

        // ====== 步骤6-7 辅助方法 ======

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
        /// 玩家扣底牌（步骤7a）。
        /// </summary>
        public PlayResult PlayerSend8Cards(List<int> selectedNumbers)
        {
            if (selectedNumbers.Count != 8)
                return PlayResult.Invalid();

            var result = new PlayResult();

            foreach (int number in selectedNumbers)
            {
                _send8Cards.Add(number);
                _currentPokers[0].RemoveCard(number);
                _pokerLists[0].Remove(number);
            }

            _currentPokers[0] = CommonMethods.parse(_pokerLists[0], _state.Suit, _state.Rank);
            _currentPokers[1] = CommonMethods.parse(_pokerLists[1], _state.Suit, _state.Rank);
            _currentPokers[2] = CommonMethods.parse(_pokerLists[2], _state.Suit, _state.Rank);
            _currentPokers[3] = CommonMethods.parse(_pokerLists[3], _state.Suit, _state.Rank);

            _state.CurrentCardCommands = CardCommands.DrawMySortedCards;

            result.RenderCommands.Add(new RenderCommand(RenderCmdType.RedrawMyHand,
                new RedrawHandPayload { PlayerId = 1 }));
            return result;
        }

        /// <summary>
        /// 玩家出牌（步骤7b）。
        /// </summary>
        public PlayResult PlayerPlayCard(int playerId, List<int> selectedNumbers)
        {
            var result = new PlayResult();

            // 规则校验 - 调用旧 TractorRules
            ArrayList selArr = new ArrayList();
            foreach (int n in selectedNumbers) selArr.Add(n);

            // 简化校验：检查是否选中了牌
            if (selectedNumbers.Count == 0)
                return PlayResult.Invalid();

            // 发送牌
            foreach (int number in selectedNumbers)
            {
                _currentSendCards[playerId - 1].Add(number);
                _pokerLists[playerId - 1].Remove(number);
            }
            _currentPokers[playerId - 1] = CommonMethods.parse(
                _pokerLists[playerId - 1], _state.Suit, _state.Rank);

            // 判断一圈是否结束
            bool allPlayed = (_currentSendCards[3].Count > 0);

            if (allPlayed)
            {
                _state.CurrentCardCommands = CardCommands.Pause;
                SetPause(_config != null ? _config.FinishedOncePauseTime : 1500,
                         CardCommands.DrawOnceFinished);
                _whoIsBigger = GetNextOrder();
                _whoseOrder = _whoIsBigger;
            }
            else
            {
                _whoseOrder = NextPlayer(playerId);
                _state.CurrentCardCommands = (_whoseOrder == 1)
                    ? CardCommands.WaitingForMySending
                    : CardCommands.WaitingForSend;
            }

            result.RenderCommands.Add(new RenderCommand(RenderCmdType.DrawPlayedCards,
                new PlayedCardsPayload { PlayerId = playerId, Cards = selectedNumbers }));
            result.RenderCommands.Add(new RenderCommand(RenderCmdType.RedrawMyHand,
                new RedrawHandPayload { PlayerId = 1 }));

            return result;
        }

        /// <summary>
        /// 一圈中下一家。
        /// </summary>
        private int NextPlayer(int current)
        {
            return (current % 4) + 1;
        }

        /// <summary>
        /// 获取一圈赢家（简化版，后续接入 TractorRules）。
        /// </summary>
        private int GetNextOrder()
        {
            return _firstSend; // 简化：先出者赢（实际需 TractorRules 判断）
        }

        /// <summary>
        /// 同步外部字段到 Engine（MainForm 调用）。
        /// </summary>
        public void SyncOrder(int whoseOrder, int firstSend, int whoIsBigger)
        {
            _whoseOrder = whoseOrder;
            _firstSend = firstSend;
            _whoIsBigger = whoIsBigger;
        }

        public void SyncRank(int suit, int master)
        {
            _state.Suit = suit;
            _state.Master = master;
        }

        public void SyncState(CardCommands command)
        {
            _state.CurrentCardCommands = command;
        }
    }

    // ====== 数据类定义 ======

    public class TickResult
    {
        public bool StateChanged { get; set; }
        public List<RenderCommand> RenderCommands { get; } = new List<RenderCommand>();
    }

    public class DealCardPayload
    {
        public int Round { get; set; }
    }

    public class WaitPayload
    {
        public string WaitingMode { get; set; }
        public int PlayerId { get; set; }
    }

    public class AiPlayPayload
    {
        public int PlayerId { get; set; }
    }

    public class PlayedCardsPayload
    {
        public int PlayerId { get; set; }
        public List<int> Cards { get; set; }
    }

    public class RedrawHandPayload
    {
        public int PlayerId { get; set; }
    }
}
