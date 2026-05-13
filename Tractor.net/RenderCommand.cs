using System.Collections.Generic;

namespace Kuaff.Tractor
{
    /// <summary>
    /// 渲染指令枚举：描述"画什么"，不关心"怎么画"。
    /// </summary>
    public enum RenderCmdType
    {
        None,
        RedrawAll,
        DealCard,
        DrawCenter8,
        RedrawMyHand,
        DrawPlayedCards,
        AiPlayCard,
        WaitingForPlayerAction,
        ShowToolbar,
        ShowPassImage,
        ShowBottomCards,
        SetPause,
        ShowRoundWinner,
        ShowRankResult,
    }

    /// <summary>
    /// 一条渲染指令 = 画什么 + 数据载荷。
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
    /// Engine.Tick / PlayerPlayCard / PlayerSend8Cards 的通用返回值。
    /// </summary>
    public class PlayResult
    {
        public bool IsValid { get; set; } = true;
        public GameState NewState { get; set; }
        public List<RenderCommand> RenderCommands { get; } = new List<RenderCommand>();

        public static PlayResult Invalid()
        {
            return new PlayResult { IsValid = false };
        }
    }

    /// <summary>
    /// Engine.Tick 的返回值。包含渲染指令和新的 GameState。
    /// </summary>
    public class TickResult
    {
        public bool StateChanged { get; set; }
        public GameState NewState { get; set; }
        public List<RenderCommand> RenderCommands { get; } = new List<RenderCommand>();
    }

    // ====== 指令载荷类型 ======

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
