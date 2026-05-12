using System;
using System.Collections;
using Kuaff.Tractor.Plugins;

namespace Kuaff.Tractor
{
    /// <summary>
    /// Algorithm 的纯数据版本：所有方法传数据参数而非 MainForm。
    /// 目的是让 Engine 和 GdiRenderer 可以不依赖 MainForm 调用算法。
    /// </summary>
    internal static class AlgorithmCore
    {
        /// <summary>
        /// 是否应该叫主（纯数据版）。
        /// 因 Algorithm.ShouldSetRank 依赖 MainForm 引用，此方法暂保持简化逻辑，
        /// 实际叫主逻辑仍由 DrawingFormHelper.DoRankOrNot 调用旧版。
        /// </summary>
        internal static int ShouldSetRank(CurrentPoker[] currentPokers, int rank, int user)
        {
            CurrentPoker currentPoker = currentPokers[user - 1];

            if (rank == 0 || rank == 8 || rank == 11)
            {
                if (currentPoker.Clubs[rank] > 0) return 4;
                else if (currentPoker.Diamonds[rank] > 0) return 3;
                else if (currentPoker.Peachs[rank] > 0) return 2;
                else if (currentPoker.Hearts[rank] > 0) return 1;
            }
            return 0;
        }

        /// <summary>
        /// 获取当前玩家的所有牌号（用于算法入参）。
        /// </summary>
        private static ArrayList GetCardsFromPokerList(ArrayList[] pokerLists, int playerId)
        {
            ArrayList result = new ArrayList();
            if (pokerLists != null && playerId >= 1 && playerId <= 4 && pokerLists[playerId - 1] != null)
            {
                foreach (int card in pokerLists[playerId - 1])
                    result.Add(card);
            }
            return result;
        }

        /// <summary>
        /// AI 出牌（纯数据版）。
        /// </summary>
        internal static ArrayList ShouldSendedCards(
            int whoseOrder,
            CurrentPoker[] currentPokers,
            ArrayList[] currentSendCard,
            CurrentPoker[] currentAllSendPokers,
            ArrayList[] pokerLists,
            IUserAlgorithm[] userAlgorithms,
            int suit,
            int rank,
            int master)
        {
            if (currentPokers[whoseOrder - 1].Count == 0)
                return new ArrayList();

            if (userAlgorithms != null && userAlgorithms[whoseOrder - 1] != null)
            {
                string myCards = currentPokers[whoseOrder - 1].getAllCards();
                string[] allSendCards = new string[4];
                for (int i = 0; i < 4; i++)
                    allSendCards[i] = currentAllSendPokers[i].getAllCards();

                IUserAlgorithm ua = userAlgorithms[whoseOrder - 1];
                ArrayList result = ua.ShouldSendCards(whoseOrder, suit, rank, master, allSendCards, myCards);

                if (result != null && result.Count > 0)
                {
                    for (int i = 0; i < result.Count; i++)
                    {
                        CommonMethods.SendCards(
                            currentSendCard[whoseOrder - 1],
                            currentPokers[whoseOrder - 1],
                            pokerLists[whoseOrder - 1],
                            (int)result[i]);
                    }
                    return result;
                }
            }

            // 无用户算法或算法返回空 → 简化：出最小牌
            return SendSmallestCard(currentPokers, currentSendCard, pokerLists, whoseOrder);
        }

        /// <summary>
        /// AI 必须出牌（纯数据版）。
        /// </summary>
        internal static ArrayList MustSendedCards(
            int whoseOrder,
            CurrentPoker[] currentPokers,
            ArrayList[] currentSendCard,
            CurrentPoker[] currentAllSendPokers,
            ArrayList[] pokerLists,
            IUserAlgorithm[] userAlgorithms,
            int suit,
            int rank,
            int master,
            int count)
        {
            if (currentPokers[whoseOrder - 1].Count == 0)
                return new ArrayList();

            if (userAlgorithms != null && userAlgorithms[whoseOrder - 1] != null)
            {
                string myCards = currentPokers[whoseOrder - 1].getAllCards();
                string[] allSendCards = new string[4];
                for (int i = 0; i < 4; i++)
                    allSendCards[i] = currentAllSendPokers[i].getAllCards();

                IUserAlgorithm ua = userAlgorithms[whoseOrder - 1];

                // IUserAlgorithm.MustSendCards 需要 8 个参数
                int whoIsFirst = currentSendCard[0].Count > 0 ? 1 : whoseOrder;
                // 构造 currentSendCards 参数（ArrayList[]）
                ArrayList[] currentSendCardsParam = new ArrayList[4];
                for (int i = 0; i < 4; i++)
                {
                    currentSendCardsParam[i] = currentSendCard[i] ?? new ArrayList();
                }

                ArrayList result = ua.MustSendCards(whoseOrder, suit, rank, master, whoIsFirst,
                    allSendCards, currentSendCardsParam, myCards);

                if (result != null && result.Count > 0)
                {
                    for (int i = 0; i < result.Count; i++)
                    {
                        CommonMethods.SendCards(
                            currentSendCard[whoseOrder - 1],
                            currentPokers[whoseOrder - 1],
                            pokerLists[whoseOrder - 1],
                            (int)result[i]);
                    }
                    return result;
                }
            }

            // 无用户算法或算法返回空 → 简化：出最小牌
            return SendSmallestCard(currentPokers, currentSendCard, pokerLists, whoseOrder);
        }

        /// <summary>
        /// 发最小一张牌。
        /// </summary>
        private static ArrayList SendSmallestCard(
            CurrentPoker[] currentPokers,
            ArrayList[] currentSendCard,
            ArrayList[] pokerLists,
            int whoseOrder)
        {
            ArrayList result = new ArrayList();
            CurrentPoker cp = currentPokers[whoseOrder - 1];
            // 从 pokerLists 获取当前玩家的手牌
            if (pokerLists != null && pokerLists[whoseOrder - 1] != null && pokerLists[whoseOrder - 1].Count > 0)
            {
                ArrayList handCards = pokerLists[whoseOrder - 1];
                int minVal = int.MaxValue;
                int toSend = (int)handCards[0];
                for (int i = 0; i < handCards.Count; i++)
                {
                    int val = (int)handCards[i];
                    if (val < minVal) { minVal = val; toSend = val; }
                }
                result.Add(toSend);

                CommonMethods.SendCards(
                    currentSendCard[whoseOrder - 1],
                    cp,
                    pokerLists[whoseOrder - 1],
                    toSend);
            }
            return result;
        }
    }
}
