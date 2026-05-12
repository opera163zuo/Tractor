using System;
using System.Collections;
using System.Linq;

namespace Kuaff.Tractor
{
    /// <summary>
    /// 纯数据容器：保存游戏全部状态，不含任何方法逻辑。
    /// Engine 读取它、修改副本后返回新实例；Renderer 读取它进行绘制。
    /// </summary>
    public class GameState
    {
        // ===== 游戏配置（只读） =====
        public GameConfig Config { get; set; }

        // ===== 卡牌数据 =====
        public ArrayList[] PokerLists { get; set; }          // 4 个玩家的原始手牌列表
        public CurrentPoker[] CurrentPokers { get; set; }    // 4 个玩家的结构化统计
        public CurrentPoker[] CurrentAllSendPokers { get; set; }  // 全局已出牌统计
        public ArrayList[] CurrentSendCards { get; set; }    // 当前圈各家出牌
        public ArrayList Send8Cards { get; set; }            // 底牌 8 张

        // ===== 游戏状态 =====
        public CurrentState State { get; set; }
        public int CurrentRank { get; set; }
        public bool IsNew { get; set; }

        // ===== 亮主/叫主 =====
        public int ShowSuits { get; set; }
        public int WhoShowRank { get; set; }

        // ===== 出牌轮转 =====
        public int WhoseOrder { get; set; }
        public int FirstSend { get; set; }
        public int WhoIsBigger { get; set; }

        // ===== 得分 =====
        public int Scores { get; set; }

        // ===== 发牌进度 =====
        public int DealCount { get; set; }

        /// <summary>
        /// 深拷贝当前状态，确保 Engine 在副本上修改时不会污染 UI 持有的原对象。
        /// </summary>
        public GameState Clone()
        {
            int suit = this.State.Suit;
            int rank = this.State.Rank;

            var gs = new GameState
            {
                Config = this.Config,
                PokerLists = this.PokerLists?.Select(arr => arr != null ? new ArrayList(arr) : new ArrayList()).ToArray(),
                CurrentPokers = this.CurrentPokers?.Select(CloneCurrentPoker).ToArray(),
                CurrentAllSendPokers = this.CurrentAllSendPokers?.Select(CloneCurrentPoker).ToArray(),
                CurrentSendCards = this.CurrentSendCards?.Select(arr => arr != null ? new ArrayList(arr) : new ArrayList()).ToArray(),
                Send8Cards = this.Send8Cards != null ? new ArrayList(this.Send8Cards) : new ArrayList(),
                State = this.State,
                CurrentRank = this.CurrentRank,
                IsNew = this.IsNew,
                ShowSuits = this.ShowSuits,
                WhoShowRank = this.WhoShowRank,
                WhoseOrder = this.WhoseOrder,
                FirstSend = this.FirstSend,
                WhoIsBigger = this.WhoIsBigger,
                Scores = this.Scores,
                DealCount = this.DealCount,
            };

            if (gs.CurrentPokers == null)
            {
                gs.CurrentPokers = new CurrentPoker[4];
            }
            if (gs.CurrentAllSendPokers == null)
            {
                gs.CurrentAllSendPokers = new CurrentPoker[4];
            }

            for (int i = 0; i < 4; i++)
            {
                if (gs.CurrentPokers[i] == null)
                {
                    gs.CurrentPokers[i] = new CurrentPoker { Rank = rank, Suit = suit };
                }
                if (gs.CurrentAllSendPokers[i] == null)
                {
                    gs.CurrentAllSendPokers[i] = new CurrentPoker { Rank = rank, Suit = suit };
                }
            }

            return gs;
        }

        private static CurrentPoker CloneCurrentPoker(CurrentPoker source)
        {
            if (source == null)
            {
                return null;
            }

            return new CurrentPoker
            {
                Rank = source.Rank,
                Suit = source.Suit,
                Diamonds = source.Diamonds != null ? (int[])source.Diamonds.Clone() : null,
                DiamondsNoRank = source.DiamondsNoRank != null ? (int[])source.DiamondsNoRank.Clone() : null,
                DiamondsRankTotal = source.DiamondsRankTotal,
                DiamondsNoRankTotal = source.DiamondsNoRankTotal,
                SortCards = source.SortCards != null ? (int[])source.SortCards.Clone() : null,
                Clubs = source.Clubs != null ? (int[])source.Clubs.Clone() : null,
                ClubsNoRank = source.ClubsNoRank != null ? (int[])source.ClubsNoRank.Clone() : null,
                ClubsRankTotal = source.ClubsRankTotal,
                ClubsNoRankTotal = source.ClubsNoRankTotal,
                Hearts = source.Hearts != null ? (int[])source.Hearts.Clone() : null,
                HeartsNoRank = source.HeartsNoRank != null ? (int[])source.HeartsNoRank.Clone() : null,
                HeartsRankTotal = source.HeartsRankTotal,
                HeartsNoRankTotal = source.HeartsNoRankTotal,
                Peachs = source.Peachs != null ? (int[])source.Peachs.Clone() : null,
                PeachsNoRank = source.PeachsNoRank != null ? (int[])source.PeachsNoRank.Clone() : null,
                PeachsRankTotal = source.PeachsRankTotal,
                PeachsNoRankTotal = source.PeachsNoRankTotal,
                BigJack = source.BigJack,
                SmallJack = source.SmallJack,
                MasterRank = source.MasterRank,
                SubRank = source.SubRank,
            };
        }
    }
}
