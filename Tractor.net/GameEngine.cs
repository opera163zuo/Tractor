using System;
using System.Collections;
using System.Collections.Generic;

namespace Kuaff.Tractor
{
    /// <summary>
    /// 游戏引擎：纯逻辑，不引用 System.Windows.Forms 或 System.Drawing。
    /// 不碰 Bitmap，不碰 Graphics，不碰 Form。
    /// 所有输入通过 GameState 参数传入，输出通过 TickResult/PlayResult + NewState 返回。
    /// </summary>
    public class GameEngine
    {
        // ====== Engine 内部状态（不暴露到 GameState） ======
        private long _pauseStartTicks;
        private long _pauseMaxMs;
        private CardCommands _wakeupCommand;

        public GameEngine()
        {
        }

        /// <summary>
        /// 重置引擎内部状态。
        /// </summary>
        public void NewGame()
        {
            _pauseStartTicks = 0;
            _pauseMaxMs = 0;
            _wakeupCommand = CardCommands.ReadyCards;
        }

        /// <summary>
        /// 游戏主循环 Tick。每次 timer_Tick 调用这个方法。
        /// </summary>
        public TickResult Tick(GameState state, long nowTicks)
        {
            var result = new TickResult();
            var gs = state.Clone();  // 浅拷贝，修改 gs 不影响 MainForm 原数据

            switch (gs.State.CurrentCardCommands)
            {
                // ====== Pause ======
                case CardCommands.Pause:
                {
                    long interval = (nowTicks - _pauseStartTicks) / 10000;
                    if (interval > _pauseMaxMs)
                    {
                        gs.State = new CurrentState {
                OurCurrentRank = gs.State.OurCurrentRank,
                OpposedCurrentRank = gs.State.OpposedCurrentRank,
                Suit = gs.State.Suit,
                Rank = gs.State.Rank,
                Master = gs.State.Master,
                OurTotalRound = gs.State.OurTotalRound,
                OpposedTotalRound = gs.State.OpposedTotalRound,
                CurrentCardCommands = _wakeupCommand,
            };
                        result.StateChanged = true;
                    }
                    break;
                }

                // ====== WaitingShowPass ======
                case CardCommands.WaitingShowPass:
                {
                    result.RenderCommands.Add(new RenderCommand(RenderCmdType.ShowPassImage));
                    gs.State = new CurrentState {
                OurCurrentRank = gs.State.OurCurrentRank,
                OpposedCurrentRank = gs.State.OpposedCurrentRank,
                Suit = gs.State.Suit,
                Rank = gs.State.Rank,
                Master = gs.State.Master,
                OurTotalRound = gs.State.OurTotalRound,
                OpposedTotalRound = gs.State.OpposedTotalRound,
                CurrentCardCommands = CardCommands.ReadyCards,
            };
                    result.StateChanged = true;
                    break;
                }

                // ====== 发牌 ReadyCards ======
                case CardCommands.ReadyCards:
                {
                    bool isDebug = gs.Config?.IsDebug ?? false;

                    if (gs.DealCount == 0 && !isDebug)
                    {
                        result.RenderCommands.Add(new RenderCommand(RenderCmdType.ShowToolbar));
                    }

                    if (gs.DealCount < 25)
                    {
                        if (gs.PokerLists != null && gs.CurrentPokers != null)
                        {
                            int card0 = (int)gs.PokerLists[0][gs.DealCount];
                            int card1 = (int)gs.PokerLists[1][gs.DealCount];
                            int card2 = (int)gs.PokerLists[2][gs.DealCount];
                            int card3 = (int)gs.PokerLists[3][gs.DealCount];

                            gs.CurrentPokers[0].AddCard(card0);
                            gs.CurrentPokers[1].AddCard(card1);
                            gs.CurrentPokers[2].AddCard(card2);
                            gs.CurrentPokers[3].AddCard(card3);
                        }

                        result.RenderCommands.Add(new RenderCommand(
                            RenderCmdType.DealCard,
                            new DealCardPayload { Round = gs.DealCount }));

                        gs.DealCount++;
                        result.StateChanged = true;
                    }
                    else
                    {
                        gs.State = new CurrentState {
                        OurCurrentRank = gs.State.OurCurrentRank,
                        OpposedCurrentRank = gs.State.OpposedCurrentRank,
                        Suit = gs.State.Suit,
                        Rank = gs.State.Rank,
                        Master = gs.State.Master,
                        OurTotalRound = gs.State.OurTotalRound,
                        OpposedTotalRound = gs.State.OpposedTotalRound,
                        CurrentCardCommands = CardCommands.DrawCenter8Cards,
                    };
                        result.StateChanged = true;
                    }
                    break;
                }

                // ====== DrawCenter8Cards ======
                case CardCommands.DrawCenter8Cards:
                {
                    result.RenderCommands.Add(new RenderCommand(RenderCmdType.DrawCenter8));
                    // 复杂的叫主逻辑暂时留在 MainForm，Engine 只发指令
                    result.StateChanged = true;
                    break;
                }

                // ====== WaitingForSending8Cards（AI 扣底） ======
                case CardCommands.WaitingForSending8Cards:
                {
                    int master = gs.State.Master;
                    bool isDebug = gs.Config?.IsDebug ?? false;
                    bool hasValidMaster = master >= 1 && master <= 4;
                    bool shouldAutoSend = hasValidMaster && (master != 1 || isDebug);

                    if (shouldAutoSend)
                    {
                        result.RenderCommands.Add(new RenderCommand(
                            RenderCmdType.WaitingForPlayerAction,
                            new WaitPayload { WaitingMode = "AutoSend8Cards", PlayerId = master }));
                    }
                    else
                    {
                        result.RenderCommands.Add(new RenderCommand(
                            RenderCmdType.WaitingForPlayerAction,
                            new WaitPayload { WaitingMode = "Send8Cards", PlayerId = 1 }));
                    }
                    result.StateChanged = true;
                    break;
                }

                // ====== WaitingForSend（AI 出牌） ======
                case CardCommands.WaitingForSend:
                {
                    if (gs.WhoseOrder >= 2 && gs.WhoseOrder <= 4)
                    {
                        result.RenderCommands.Add(new RenderCommand(
                            RenderCmdType.AiPlayCard,
                            new AiPlayPayload { PlayerId = gs.WhoseOrder }));
                    }
                    else if (gs.WhoseOrder == 1)
                    {
                        bool isDebug = gs.Config?.IsDebug ?? false;
                        if (isDebug)
                        {
                            result.RenderCommands.Add(new RenderCommand(
                                RenderCmdType.AiPlayCard,
                                new AiPlayPayload { PlayerId = 1 }));
                        }
                        else
                        {
                            gs.State = new CurrentState {
                                OurCurrentRank = gs.State.OurCurrentRank,
                                OpposedCurrentRank = gs.State.OpposedCurrentRank,
                                Suit = gs.State.Suit,
                                Rank = gs.State.Rank,
                                Master = gs.State.Master,
                                OurTotalRound = gs.State.OurTotalRound,
                                OpposedTotalRound = gs.State.OpposedTotalRound,
                                CurrentCardCommands = CardCommands.WaitingForMySending,
                            };
                            result.StateChanged = true;
                        }
                    }
                    else
                    {
                        gs.State = new CurrentState {
                OurCurrentRank = gs.State.OurCurrentRank,
                OpposedCurrentRank = gs.State.OpposedCurrentRank,
                Suit = gs.State.Suit,
                Rank = gs.State.Rank,
                Master = gs.State.Master,
                OurTotalRound = gs.State.OurTotalRound,
                OpposedTotalRound = gs.State.OpposedTotalRound,
                CurrentCardCommands = CardCommands.WaitingForMySending,
            };
                        result.StateChanged = true;
                    }
                    break;
                }

                default:
                    break;
            }

            result.NewState = gs;
            return result;
        }

        /// <summary>
        /// 设置暂停。
        /// </summary>
        public void SetPause(int maxMs, CardCommands wakeup)
        {
            _pauseMaxMs = maxMs;
            _pauseStartTicks = DateTime.Now.Ticks;
            _wakeupCommand = wakeup;
        }

        /// <summary>
        /// 玩家扣底牌。
        /// </summary>
        public PlayResult PlayerSend8Cards(GameState state, List<int> selectedNumbers)
        {
            var result = new PlayResult();
            var gs = state.Clone();

            if (selectedNumbers.Count != 8)
            {
                result.IsValid = false;
                result.NewState = gs;
                return result;
            }

            foreach (int number in selectedNumbers)
            {
                gs.Send8Cards.Add(number);
                gs.CurrentPokers[0].RemoveCard(number);
                gs.PokerLists[0].Remove(number);
            }

            gs.CurrentPokers[0] = CommonMethods.parse(gs.PokerLists[0], gs.State.Suit, gs.State.Rank);
            gs.CurrentPokers[1] = CommonMethods.parse(gs.PokerLists[1], gs.State.Suit, gs.State.Rank);
            gs.CurrentPokers[2] = CommonMethods.parse(gs.PokerLists[2], gs.State.Suit, gs.State.Rank);
            gs.CurrentPokers[3] = CommonMethods.parse(gs.PokerLists[3], gs.State.Suit, gs.State.Rank);

            gs.State = new CurrentState {
                OurCurrentRank = gs.State.OurCurrentRank,
                OpposedCurrentRank = gs.State.OpposedCurrentRank,
                Suit = gs.State.Suit,
                Rank = gs.State.Rank,
                Master = gs.State.Master,
                OurTotalRound = gs.State.OurTotalRound,
                OpposedTotalRound = gs.State.OpposedTotalRound,
                CurrentCardCommands = CardCommands.DrawMySortedCards,
            };

            result.RenderCommands.Add(new RenderCommand(RenderCmdType.RedrawMyHand,
                new RedrawHandPayload { PlayerId = 1 }));
            result.NewState = gs;
            return result;
        }

        /// <summary>
        /// 玩家出牌。
        /// </summary>
        public PlayResult PlayerPlayCard(GameState state, int playerId, List<int> selectedNumbers)
        {
            var result = new PlayResult();
            var gs = state.Clone();

            if (selectedNumbers.Count == 0)
            {
                result.IsValid = false;
                result.NewState = gs;
                return result;
            }

            // 出牌
            foreach (int number in selectedNumbers)
            {
                gs.CurrentSendCards[playerId - 1].Add(number);
                gs.PokerLists[playerId - 1].Remove(number);
            }
            gs.CurrentPokers[playerId - 1] = CommonMethods.parse(
                gs.PokerLists[playerId - 1], gs.State.Suit, gs.State.Rank);

            // 判断一圈是否结束
            bool allPlayed = true;
            int expectedCount = (gs.FirstSend >= 1 && gs.FirstSend <= 4)
                ? gs.CurrentSendCards[gs.FirstSend - 1].Count : 1;
            for (int i = 0; i < 4; i++)
            {
                if (gs.CurrentSendCards[i].Count != expectedCount)
                {
                    allPlayed = false;
                    break;
                }
            }

            int nextOrder;
            CardCommands nextCmd;
            int newWhoIsBigger = gs.WhoIsBigger;

            if (allPlayed)
            {
                nextCmd = CardCommands.Pause;
                _pauseMaxMs = gs.Config?.FinishedOncePauseTime ?? 1500;
                _pauseStartTicks = DateTime.Now.Ticks;
                _wakeupCommand = CardCommands.DrawOnceFinished;
                newWhoIsBigger = ResolveTrickWinner(
                    gs.CurrentSendCards, gs.State.Suit, gs.State.Rank, gs.FirstSend);
                nextOrder = newWhoIsBigger;
                // 累加本墩得分
                gs.Scores += CalculateTrickScore(gs.CurrentSendCards);
            }
            else
            {
                nextOrder = (playerId % 4) + 1;
                nextCmd = (nextOrder == 1)
                    ? CardCommands.WaitingForMySending
                    : CardCommands.WaitingForSend;
            }

            gs.State = new CurrentState {
                OurCurrentRank = gs.State.OurCurrentRank,
                OpposedCurrentRank = gs.State.OpposedCurrentRank,
                Suit = gs.State.Suit,
                Rank = gs.State.Rank,
                Master = gs.State.Master,
                OurTotalRound = gs.State.OurTotalRound,
                OpposedTotalRound = gs.State.OpposedTotalRound,
                CurrentCardCommands = nextCmd,
            };
            gs.WhoseOrder = nextOrder;
            gs.WhoIsBigger = newWhoIsBigger;

            result.RenderCommands.Add(new RenderCommand(RenderCmdType.DrawPlayedCards,
                new PlayedCardsPayload { PlayerId = playerId, Cards = selectedNumbers }));
            result.RenderCommands.Add(new RenderCommand(RenderCmdType.RedrawMyHand,
                new RedrawHandPayload { PlayerId = 1 }));
            result.NewState = gs;
            return result;
        }

        /// <summary>
        /// 读取 Engine 内部暂停状态（供外部判断）。
        /// </summary>
        public long PauseStartTicks => _pauseStartTicks;
        public long PauseMaxMs => _pauseMaxMs;
        public CardCommands WakeupCommand => _wakeupCommand;
    }
}
