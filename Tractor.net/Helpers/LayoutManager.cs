namespace Kuaff.Tractor.Helpers
{
    /// <summary>
    /// 游戏界面所有UI元素的布局常量。
    /// 方便统一调整——以后改布局只用改这里。
    /// </summary>
    public static class LayoutManager
    {
        // ===== 手牌区域 (My Cards) =====
        public const int MyCardsAreaX = 30;
        public const int MyCardsAreaY = 355;
        public const int MyCardsAreaWidth = 600;
        public const int MyCardsAreaHeight = 116;

        // ===== 小猪按钮（出牌确认） =====
        public const int PigButtonX = 296;
        public const int PigButtonY = 300;
        public const int PigButtonWidth = 53;
        public const int PigButtonHeight = 46;

        // ===== 底牌区域 =====
        public const int BottomCardsStartX = 200;
        public const int BottomCardsY = 186;
        public const int CardWidth = 71;
        public const int CardHeight = 96;

        // ===== 中心8张底牌动画区域 =====
        public const int Center8CardsX = 200;
        public const int Center8CardsY = 186;
        public const int Center8CardsWidth = 90;
        public const int Center8CardsHeight = 96;

        // ===== 中心背景区域 =====
        public const int CenterBackgroundX = 77;
        public const int CenterBackgroundY = 121;
        public const int CenterBackgroundWidth = 477;
        public const int CenterBackgroundHeight = 254;

        // ===== 花色工具栏 =====
        public const int ToolbarX = 415;
        public const int ToolbarY = 325;
        public const int ToolbarWidth = 129;
        public const int ToolbarHeight = 29;

        // ===== 牌间距 =====
        public const int SelectedCardOffset = 13;  // 选中牌的Y偏移
    }
}
