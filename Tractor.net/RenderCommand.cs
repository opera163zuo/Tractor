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
        DrawPlayedCards, AiPlayCard, WaitingForPlayerAction,// 画出牌
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
