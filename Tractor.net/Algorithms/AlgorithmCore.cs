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

            // 其他情况：调用旧算法
            return Algorithm.ShouldSetRank(null, user);
        }

        /// <summary>
        /// AI 出牌（纯数据版）。
        /// Need: player hand cards, send cards state, all send pokers, pokerLists, user algorithm, master, suit, rank
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
            if (userAlgorithms[whoseOrder - 1] != null)
            {
                string pokers = currentPokers[whoseOrder - 1].getAllCards();
                string[] allSendCards = new string[4];
                for (int i = 0; i < 4; i++)
                    allSendCards[i] = currentAllSendPokers[i].getAllCards();

                IUserAlgorithm ua = (IUserAlgorithm)userAlgorithms[whoseOrder - 1];
                ArrayList result = ua.ShouldSendCards(whoseOrder, suit, rank, master, allSendCards, pokers);

                // 合法性校验
                bool b1 = TractorRulesCore.IsInvalid(currentSendCard, result, whoseOrder);
                bool b2 = TractorRulesCore.CheckSendCards(currentSendCard, result, whoseOrder);

                if (b1 && b2)
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

            // 无用户算法，或用算法返回值不合法 → 退化为内置算法
            return Algorithm.ShouldSendedCardsAlgorithm(currentPokers, whoseOrder, currentSendCard[whoseOrder - 1]);
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
            if (userAlgorithms[whoseOrder - 1] != null)
            {
                string pokers = currentPokers[whoseOrder - 1].getAllCards();
                string[] allSendCards = new string[4];
                for (int i = 0; i < 4; i++)
                    allSendCards[i] = currentAllSendPokers[i].getAllCards();

                IUserAlgorithm ua = (IUserAlgorithm)userAlgorithms[whoseOrder - 1];
                ArrayList result = ua.MustSendCards(whoseOrder, suit, rank, master, allSendCards, pokers, count);

                bool b1 = TractorRulesCore.IsInvalid(currentSendCard, result, whoseOrder);
                bool b2 = TractorRulesCore.CheckSendCards(currentSendCard, result, whoseOrder);

                if (b1 && b2)
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

            return Algorithm.MustSendedCardsAlgorithm(
                currentPokers, whoseOrder, currentSendCard[whoseOrder - 1],
                currentSendCard, suit, rank, count);
        }
    }

    /// <summary>
    /// TractorRules 的纯数据版（逐步迁移）。
    /// </summary>
    internal static class TractorRulesCore
    {
        internal static bool IsInvalid(ArrayList[] currentSendCard, ArrayList result, int whoseOrder)
        {
            if (result == null || result.Count == 0) return false;
            return true;
        }

        internal static bool CheckSendCards(ArrayList[] currentSendCard, ArrayList result, int whoseOrder)
        {
            if (result.Count == 0) return false;
            return true;
        }
    }

    /// <summary>
    /// 扩展：为 Algorithm.ShouldSendedCardsAlgorithm / MustSendedCardsAlgorithm
    /// 提供不依赖 MainForm 的调用路径。
    /// 这些方法在 Algorithm.cs 末尾定义了静态重载。
    /// </summary>
    internal partial class Algorithm
    {
        /// <summary>
        /// 无 UI 的 ShouldSendedCards 算法（由 AlgorithmCore 调用）。
        /// </summary>
        internal static ArrayList ShouldSendedCardsAlgorithm(
            CurrentPoker[] currentPokers,
            int whoseOrder,
            ArrayList currentSendCardList)
        {
            ArrayList result = new ArrayList();

            if (currentPokers[whoseOrder - 1].Count == 0)
                return result;

            // 简化算法：出第一张牌
            CurrentPoker cp = currentPokers[whoseOrder - 1];

            ArrayList allPokers = cp.ToArrayList();
            if (allPokers.Count > 0)
            {
                int toSend = int.MaxValue;
                for (int i = 0; i < allPokers.Count; i++)
                {
                    int val = (int)allPokers[i];
                    if (val < toSend) toSend = val;
                }
                result.Add(toSend);
            }

            foreach (int n in result)
            {
                CommonMethods.SendCards(currentSendCardList, cp, new ArrayList(), n);
            }

            return result;
        }

        /// <summary>
        /// 无 UI 的 MustSendedCards 算法。
        /// </summary>
        internal static ArrayList MustSendedCardsAlgorithm(
            CurrentPoker[] currentPokers,
            int whoseOrder,
            ArrayList currentSendCardList,
            ArrayList[] currentSendCard,
            int suit,
            int rank,
            int count)
        {
            ArrayList result = new ArrayList();
            if (currentPokers[whoseOrder - 1].Count == 0)
                return result;

            CurrentPoker cp = currentPokers[whoseOrder - 1];
            ArrayList allPokers = cp.ToArrayList();
            if (allPokers.Count > 0)
            {
                int toSend = int.MaxValue;
                for (int i = 0; i < allPokers.Count; i++)
                {
                    int val = (int)allPokers[i];
                    if (val < toSend) toSend = val;
                }
                result.Add(toSend);
            }

            foreach (int n in result)
            {
                CommonMethods.SendCards(currentSendCardList, cp, new ArrayList(), n);
            }

            return result;
        }
    }
}
