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
        /// 深度浅拷贝（引用类型字段创建新容器但复用内部元素）。
        /// </summary>
        public GameState Clone()
        {
            var gs = new GameState
            {
                Config = this.Config,

                // ArrayList 浅拷贝：创建新 ArrayList 但复 int 元素（int 是值类型，安全）
                PokerLists = this.PokerLists?.Select(arr => new ArrayList(arr)).ToArray(),
                CurrentSendCards = this.CurrentSendCards?.Select(arr => new ArrayList(arr)).ToArray(),
                Send8Cards = this.Send8Cards != null ? new ArrayList(this.Send8Cards) : null,

                // CurrentPoker 重建：通过 PokerLists 重新 parse 生成新实例
                CurrentPokers = new CurrentPoker[4],
                CurrentAllSendPokers = new CurrentPoker[4],

                // 值类型直接拷贝
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

            // 重建 CurrentPokers 和 CurrentAllSendPokers
            int suit = this.State.Suit;
            int rank = this.State.Rank;
            for (int i = 0; i < 4; i++)
            {
                if (this.PokerLists?[i] != null)
                    gs.CurrentPokers[i] = CommonMethods.parse(this.PokerLists[i], suit, rank);
                else
                    gs.CurrentPokers[i] = new CurrentPoker { Rank = rank, Suit = suit };

                if (this.CurrentAllSendPokers?[i] != null)
                    gs.CurrentAllSendPokers[i] = new CurrentPoker
                    {
                        Rank = this.CurrentAllSendPokers[i].Rank,
                        Suit = this.CurrentAllSendPokers[i].Suit,
                        Hearts = (int[])this.CurrentAllSendPokers[i].Hearts.Clone(),
                        Peachs = (int[])this.CurrentAllSendPokers[i].Peachs.Clone(),
                        Diamonds = (int[])this.CurrentAllSendPokers[i].Diamonds.Clone(),
                        Clubs = (int[])this.CurrentAllSendPokers[i].Clubs.Clone(),
                        // 其他字段手动拷贝...
                        BigJack = this.CurrentAllSendPokers[i].BigJack,
                        SmallJack = this.CurrentAllSendPokers[i].SmallJack,
                        MasterRank = this.CurrentAllSendPokers[i].MasterRank,
                        SubRank = this.CurrentAllSendPokers[i].SubRank,
                    };
                else
                    gs.CurrentAllSendPokers[i] = new CurrentPoker { Rank = rank, Suit = suit };
            }

            return gs;
        }
    }
}
