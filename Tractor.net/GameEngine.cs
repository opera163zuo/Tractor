using System;
using System.Collections.Generic;

namespace Kuaff.Tractor
{
    /// <summary>
    /// 游戏引擎：纯逻辑，不引用 System.Windows.Forms 或 System.Drawing。
    /// 不碰 Bitmap，不碰 Graphics，不碰 Form。
    /// </summary>
    public class GameEngine
    {
        // 后面步骤会填充的字段
        // private CurrentState _state;
        // private List<int>[] _pokerLists;
        // private CurrentPoker[] _currentPokers;

        public GameEngine()
        {
            // 空壳构造函数
        }

        /// <summary>
        /// 创建一个初始状态的新游戏。
        /// 占位——后面步骤会实现具体逻辑。
        /// </summary>
        public void NewGame()
        {
            // 待实现
        }

        /// <summary>
        /// 玩家出牌。暂不实现。
        /// </summary>
        public PlayResult PlayerPlayCard(int playerId, List<int> selectedCards)
        {
            throw new NotImplementedException("步骤6-7实现");
        }
    }
}
