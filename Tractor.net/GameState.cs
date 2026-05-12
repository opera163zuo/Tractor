using System;
using System.Collections;

namespace Kuaff.Tractor
{
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
        public ArrayList[] PokerLists { get; set; }        // 4个玩家的原始手牌列表
        public CurrentPoker[] CurrentPokers { get; set; }  // 4个玩家的结构化统计
        public CurrentPoker[] CurrentAllSendPokers { get; set; }  // 所有已出的牌
        public ArrayList[] CurrentSendCards { get; set; }  // 当前圈各玩家出牌
        public ArrayList Send8Cards { get; set; }          // 底牌8张

        // ===== 游戏状态 =====
        public CurrentState State { get; set; }
        public int CurrentRank { get; set; }              // 当前 Rank
        public bool IsNew { get; set; }                   // 是否新局
        public int ShowSuits { get; set; }                // 叫主次数
        public int WhoShowRank { get; set; }              // 谁叫的主
        public int WhoseOrder { get; set; }               // 轮到谁
        public int FirstSend { get; set; }                // 一圈中先出者
        public int WhoIsBigger { get; set; }              // 当前圈谁最大
        public int Scores { get; set; }                   // 累计得分
        public int DealCount { get; set; }                // 发牌到第几张

        // ===== 暂停计时器（Engine 内部用） =====
        public long PauseStartTicks { get; set; }
        public long PauseMaxMs { get; set; }
        public CardCommands WakeupCommand { get; set; }
    }
}
