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
        // ===== 对手出牌区域 =====
        public const int OtherPlayer2CardX = 320;
        public const int OtherPlayer2CardY = 55;
        public const int OtherPlayer2Spacing = 20;
        public const int OtherPlayer3CardX = 50;
        public const int OtherPlayer3CardY = 290;
        public const int OtherPlayer3Spacing = 10;
        public const int OtherPlayer4CardX = 530;
        public const int OtherPlayer4CardY = 290;
        public const int OtherPlayer4Spacing = 10;

        // ===== 侧栏 =====
        public const int SidebarCardX1 = 20;
        public const int SidebarCardX2 = 540;
        public const int SidebarCardY = 30;
        public const int SidebarCardWidth = 70;
        public const int SidebarCardHeight = 89;

        // ===== 庄家标识 =====
        public const int MasterIconX1 = 548;
        public const int MasterIconX2 = 580;
        public const int MasterIconX3 = 30;
        public const int MasterIconX4 = 60;
        public const int MasterIconY = 45;
        public const int MasterIconSize = 20;

        // ===== Rank 显示 =====
        public const int RankIconXMe = 566;
        public const int RankIconXOther = 46;
        public const int RankIconY = 88;
        public const int RankIconSize = 25;

        // ===== 花色显示 =====
        public const int SuitIconXMe = 563;
        public const int SuitIconXOther = 43;
        public const int SuitIconY = 88;
        public const int SuitIconSize = 25;

    }
}