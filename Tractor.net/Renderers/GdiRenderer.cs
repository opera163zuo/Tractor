using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using Kuaff.Tractor.Helpers;

namespace Kuaff.Tractor
{
    /// <summary>
    /// GDI+ 渲染器。
    /// 接收 RenderCommand，在 Bitmap 上执行绘制。
    /// 
    /// 规则：只读 state，不修改。所有副作用仅限于 Bitmap 绘制。
    /// 复杂渲染通过 FallbackRenderDelegate 回调 DrawingFormHelper。
    /// </summary>
    public class GdiRenderer
    {
        private GameConfig _config;

        /// <summary>
        /// 获取牌图的回调。
        /// </summary>
        public Func<int, Bitmap> GetCardImageFunc { get; set; }

        /// <summary>
        /// 主窗口背景图。
        /// </summary>
        public Bitmap BackgroundImage { get; set; }

        /// <summary>
        /// 复杂渲染的 fallback 回调（参数：RenderCmdType、payload、Bitmap）。
        /// 由 MainForm 注入 DrawingFormHelper 的相应方法。
        /// </summary>
        public Action<RenderCmdType, object, Bitmap> FallbackRender { get; set; }

        /// <summary>
        /// 游戏状态快照（只读引用）。
        /// </summary>
        public GameState State { get; set; }

        public GdiRenderer(GameConfig config)
        {
            _config = config;
        }

        /// <summary>
        /// 执行渲染指令。
        /// </summary>
        public void Execute(RenderCommand cmd, Bitmap bmp, CurrentState currentState = default(CurrentState), object context = null)
        {
            bool handled = false;

            switch (cmd.Type)
            {
                case RenderCmdType.RedrawAll:
                    DrawBackground(bmp, null);
                    handled = true;
                    break;

                case RenderCmdType.DealCard:
                    if (cmd.Payload is DealCardPayload deal)
                    {
                        DrawDealCard(bmp, deal.Round);
                        handled = true;
                    }
                    break;

                case RenderCmdType.DrawCenter8:
                    if (State != null)
                    {
                        DrawCenter8Cards(bmp, State);
                        handled = true;
                    }
                    break;

                case RenderCmdType.RedrawMyHand:
                    if (State?.CurrentPokers?[0] != null)
                    {
                        DrawMySortedCards(bmp, State.CurrentPokers[0], State.CurrentPokers[0].Count);
                        handled = true;
                    }
                    break;

                case RenderCmdType.DrawPlayedCards:
                    if (State != null && cmd.Payload is PlayedCardsPayload played)
                    {
                        if (played.PlayerId == 1)
                        {
                            DrawMyFinishSendedCards(bmp, State);
                        }
                        else
                        {
                            DrawOtherPlayerPlayedCards(bmp, State, played.PlayerId);
                        }
                        handled = true;
                    }
                    break;

                case RenderCmdType.ShowToolbar:
                    DrawToolbar(bmp);
                    handled = true;
                    break;

                case RenderCmdType.ShowPassImage:
                    DrawPassImage(bmp);
                    handled = true;
                    break;

                case RenderCmdType.ShowBottomCards:
                    if (State?.Send8Cards != null)
                    {
                        DrawBottomCards(bmp, State.Send8Cards);
                        handled = true;
                    }
                    break;

                case RenderCmdType.ShowRoundWinner:
                    if (State != null)
                    {
                        DrawFinishedOnceSendedCards(bmp, State);
                        handled = true;
                    }
                    break;

                case RenderCmdType.ShowRankResult:
                    if (State != null)
                    {
                        DrawFinishedScoreImage(bmp, State);
                        handled = true;
                    }
                    break;

                case RenderCmdType.None:
                case RenderCmdType.SetPause:
                default:
                    handled = false;
                    break;
            }

            if (!handled && FallbackRender != null)
            {
                FallbackRender(cmd.Type, cmd.Payload, bmp);
            }
        }

        #region GdiRenderer 直接支持的渲染方法

        public void DrawBackground(Bitmap bmp, Rectangle? clientRect)
        {
            if (BackgroundImage == null) return;
            Graphics g = Graphics.FromImage(bmp);
            Rectangle rect = clientRect ?? new Rectangle(0, 0, bmp.Width, bmp.Height);
            g.DrawImage(BackgroundImage, rect, rect, GraphicsUnit.Pixel);
            g.Dispose();
        }

        public void DrawCenterImage(Bitmap bmp, Bitmap bgImage)
        {
            if (bgImage == null) return;
            Graphics g = Graphics.FromImage(bmp);
            Rectangle rect = new Rectangle(LayoutManager.CenterBackgroundX,
                                           LayoutManager.CenterBackgroundY,
                                           LayoutManager.CenterBackgroundWidth,
                                           LayoutManager.CenterBackgroundHeight);
            g.DrawImage(bgImage, rect, rect, GraphicsUnit.Pixel);
            g.Dispose();
        }

        public void DrawPassImage(Bitmap bmp)
        {
            Graphics g = Graphics.FromImage(bmp);
            g.DrawImage(Properties.Resources.Pass, new Rectangle(110, 150, 400, 199));
            g.Dispose();
        }

        public void DrawToolbar(Bitmap bmp)
        {
            Graphics g = Graphics.FromImage(bmp);
            g.DrawImage(Properties.Resources.Toolbar,
                new Rectangle(415, 325, 129, 29),
                new Rectangle(0, 0, 129, 29), GraphicsUnit.Pixel);
            g.DrawImage(Properties.Resources.Suit,
                new Rectangle(417, 327, 125, 25),
                new Rectangle(125, 0, 125, 25), GraphicsUnit.Pixel);
            g.Dispose();
        }

        public void DrawSuit(Graphics g, int suit, bool me, bool b)
        {
            int X = 0, X2 = 0;
            if (me) { X = 563; X2 = 43; }
            else { X = 43; X2 = 563; }

            Rectangle destRect = new Rectangle(X, 88, 25, 25);
            Rectangle redrawRect = new Rectangle(X2, 88, 25, 25);

            if (!b)
            {
                g.DrawImage(Properties.Resources.Suit, destRect, new Rectangle(250, 0, 25, 25), GraphicsUnit.Pixel);
                return;
            }

            int srcX = (suit - 1) * 25;
            g.DrawImage(Properties.Resources.Sidebar, destRect, new Rectangle(23, 58, 25, 25), GraphicsUnit.Pixel);
            g.DrawImage(Properties.Resources.Suit, destRect, new Rectangle(srcX, 0, 25, 25), GraphicsUnit.Pixel);
            g.DrawImage(Properties.Resources.Sidebar, redrawRect, new Rectangle(23, 58, 25, 25), GraphicsUnit.Pixel);
            if (suit > 0) DrawSuit(g, 0, !me, false);
        }

        public void DrawScoreImage(Bitmap bmp, Bitmap bgImage, int scores, int master)
        {
            Graphics g = Graphics.FromImage(bmp);
            Bitmap bmpScore = Properties.Resources.scores;
            using (Font font = new Font("宋体", 12, FontStyle.Bold))
            {
                int x, y;
                Rectangle rect;
                if (master == 2 || master == 4)
                {
                    rect = new Rectangle(490, 128, 56, 56);
                    x = 506; y = 138;
                }
                else
                {
                    rect = new Rectangle(85, 300, 56, 56);
                    x = 100; y = 310;
                }
                g.DrawImage(bgImage, rect, rect, GraphicsUnit.Pixel);
                g.DrawImage(bmpScore, rect);
                string txt = scores.ToString();
                if (txt.Length == 2) x -= 4;
                else if (txt.Length == 3) x -= 8;
                g.DrawString(txt, font, Brushes.White, x, y);
            }
            g.Dispose();
        }

        public Bitmap GetPokerImageByNumber(int number)
        {
            if (GetCardImageFunc != null)
                return GetCardImageFunc(number);
            if (_config.CardImageName.Length == 0)
                return (Bitmap)_config.CardsResourceManager.GetObject("_" + number);
            return _config.BackImage;
        }

        private void ClearSuitCards(Graphics g)
        {
            if (BackgroundImage == null) return;
            g.DrawImage(BackgroundImage, new Rectangle(80, 158, 71, 96), new Rectangle(80, 158, 71, 96), GraphicsUnit.Pixel);
            g.DrawImage(BackgroundImage, new Rectangle(480, 200, 71, 96), new Rectangle(480, 200, 71, 96), GraphicsUnit.Pixel);
            g.DrawImage(BackgroundImage, new Rectangle(437, 124, 71, 96), new Rectangle(437, 124, 71, 96), GraphicsUnit.Pixel);
        }

        public void DrawSuitUI(Bitmap bmp, GameState state)
        {
            if (bmp == null || state == null || state.State == null) return;

            using (Graphics g = Graphics.FromImage(bmp))
            {
                if (state.ShowSuits == 1)
                {
                    if (state.WhoShowRank == 2)
                    {
                        g.DrawImage(GetPokerImageByNumber(state.State.Suit * 13 - 13 + state.CurrentRank), 437, 124, 71, 96);
                    }
                    else if (state.WhoShowRank == 3)
                    {
                        g.DrawImage(GetPokerImageByNumber(state.State.Suit * 13 - 13 + state.CurrentRank), 80, 158, 71, 96);
                    }
                    else if (state.WhoShowRank == 4)
                    {
                        g.DrawImage(GetPokerImageByNumber(state.State.Suit * 13 - 13 + state.CurrentRank), 480, 200, 71, 96);
                    }
                }
                else if (state.ShowSuits == 2)
                {
                    if (state.WhoShowRank == 2)
                    {
                        ClearSuitCards(g);
                        g.DrawImage(GetPokerImageByNumber(state.State.Suit * 13 - 13 + state.CurrentRank), 423, 124, 71, 96);
                        g.DrawImage(GetPokerImageByNumber(state.State.Suit * 13 - 13 + state.CurrentRank), 437, 124, 71, 96);
                    }
                    else if (state.WhoShowRank == 3)
                    {
                        ClearSuitCards(g);
                        g.DrawImage(GetPokerImageByNumber(state.State.Suit * 13 - 13 + state.CurrentRank), 80, 158, 71, 96);
                        g.DrawImage(GetPokerImageByNumber(state.State.Suit * 13 - 13 + state.CurrentRank), 80, 178, 71, 96);
                    }
                    else if (state.WhoShowRank == 4)
                    {
                        ClearSuitCards(g);
                        g.DrawImage(GetPokerImageByNumber(state.State.Suit * 13 - 13 + state.CurrentRank), 480, 200, 71, 96);
                        g.DrawImage(GetPokerImageByNumber(state.State.Suit * 13 - 13 + state.CurrentRank), 480, 220, 71, 96);
                    }
                }
            }
        }

        public void DrawRankCardsUI(Bitmap bmp, GameState state)
        {
            if (bmp == null || state == null || state.State == null) return;
            int suit = state.State.Suit;
            int master = state.State.Master;
            if (suit <= 0) return;

            using (Graphics g = Graphics.FromImage(bmp))
            {
                if ((master == 1) || (master == 2))
                {
                    DrawSuit(g, suit, true, true);
                }
                else
                {
                    DrawSuit(g, suit, false, true);
                }
            }

            if ((master == 1) || (master == 2))
            {
                DrawRank(bmp, state.State.OurCurrentRank, true, true);
            }
            else
            {
                DrawRank(bmp, state.State.OpposedCurrentRank, false, true);
            }

            DrawMaster(bmp, master, 1);
            DrawOtherMaster(bmp, master, 1);
        }

        public void DrawRankOrNotUI(Bitmap bmp, GameState state)
        {
            DrawRankCardsUI(bmp, state);
            DrawSuitUI(bmp, state);
        }

        #endregion

        #region 发牌动画

        public void DrawDealCard(Bitmap bmp, int count)
        {
            Graphics g = Graphics.FromImage(bmp);
            for (int i = 0; i < 58 - count * 2; i++)
                g.DrawImage(_config.BackImage, 260 + i * 2, 280, 71, 96);
            g.Dispose();

            if (State == null || State.PokerLists == null) return;

            // 玩家1正面的牌
            if (count < State.PokerLists[0].Count)
            {
                int card0 = (int)State.PokerLists[0][count];
                Bitmap img0 = GetCardImageFunc(card0);
                using (Graphics g2 = Graphics.FromImage(bmp))
                    g2.DrawImage(img0, 260, 280, 71, 96);
            }

            // 其他玩家背面
            Action<int, int, int> drawBack = (x, y, x2) =>
            {
                using (Graphics g2 = Graphics.FromImage(bmp))
                {
                    g2.DrawImage(_config.BackImage, x - 13 * count, y, 71, 96);
                    g2.DrawImage(_config.BackImage, x2 - 13 * count, y - 35, 71, 96);
                }
            };

            drawBack(400, 60, 437);    // 玩家2（对家）
            drawBack(50, 160 + count * 4, 6); // 玩家3（上手）
            drawBack(520, 220 - count * 4, 554); // 玩家4（下手）
        }

        #endregion

        #region 手牌渲染

        public void DrawMySortedCards(Bitmap bmp, CurrentPoker cp, int cardCount)
        {
            if (bmp == null || cp == null) return;

            using (Graphics g = Graphics.FromImage(bmp))
            {
                Rectangle rect = new Rectangle(30, 355, 600, 116);
                if (BackgroundImage != null)
                {
                    g.DrawImage(BackgroundImage, rect, rect, GraphicsUnit.Pixel);
                }
                else
                {
                    g.FillRectangle(Brushes.Transparent, rect);
                }

                int start = (int)((2780 - cardCount * 75) / 10);
                int x = start;
                foreach (int card in GetOrderedHandCards(cp))
                {
                    g.DrawImage(GetPokerImageByNumber(card), x, 355, 71, 96);
                    x += 13;
                }
            }
        }

        public void DrawCenter8Cards(Bitmap bmp, GameState state)
        {
            if (bmp == null || state == null || state.State == null || state.PokerLists == null || state.PokerLists.Length < 4) return;

            using (Graphics g = Graphics.FromImage(bmp))
            {
                Rectangle backRect = new Rectangle(77, 121, 477, 254);
                if (BackgroundImage != null)
                {
                    g.DrawImage(BackgroundImage, backRect, backRect, GraphicsUnit.Pixel);
                }
            }

            if (state.State.Master == 1)
            {
                Get8Cards(state.PokerLists[0], state.PokerLists[1], state.PokerLists[2], state.PokerLists[3]);
            }
            else if (state.State.Master == 2)
            {
                Get8Cards(state.PokerLists[1], state.PokerLists[0], state.PokerLists[2], state.PokerLists[3]);
            }
            else if (state.State.Master == 3)
            {
                Get8Cards(state.PokerLists[2], state.PokerLists[1], state.PokerLists[0], state.PokerLists[3]);
            }
            else if (state.State.Master == 4)
            {
                Get8Cards(state.PokerLists[3], state.PokerLists[1], state.PokerLists[2], state.PokerLists[0]);
            }
        }

        public void DrawDealRound(Bitmap bmp, GameState state, int count)
        {
            if (bmp == null || state == null || state.PokerLists == null || state.CurrentPokers == null) return;
            if (count < 0) return;

            using (Graphics g = Graphics.FromImage(bmp))
            {
                DrawCenterAllCards(g, 58 - count * 2);
            }

            for (int i = 0; i < 4; i++)
            {
                if (state.PokerLists.Length <= i || state.CurrentPokers.Length <= i || state.PokerLists[i] == null || state.CurrentPokers[i] == null) return;
                if (state.PokerLists[i].Count <= count) return;
                state.CurrentPokers[i].AddCard((int)state.PokerLists[i][count]);
            }

            DrawMySortedCards(bmp, state.CurrentPokers[0], state.CurrentPokers[0].Count);

            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.DrawImage(_config.BackImage, 437 - count * 13, 25, 71, 96);
                g.DrawImage(_config.BackImage, 6, 145 + count * 4, 71, 96);
                g.DrawImage(_config.BackImage, 554, 241 - count * 4, 71, 96);
            }
        }

        public void DrawMyFinishSendedCards(Bitmap bmp, GameState state)
        {
            if (bmp == null || state == null || state.CurrentSendCards == null || state.CurrentAllSendPokers == null || state.CurrentPokers == null) return;

            DrawMySendedCardsAction(bmp, state.CurrentSendCards[0]);

            for (int i = 0; i < state.CurrentSendCards[0].Count; i++)
            {
                state.CurrentAllSendPokers[0].AddCard((int)state.CurrentSendCards[0][i]);
            }

            if (state.CurrentPokers[0].Count > 0)
            {
                DrawMySortedCards(bmp, state.CurrentPokers[0], state.CurrentPokers[0].Count);
            }
            else
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    Rectangle rect = new Rectangle(30, 355, 560, 116);
                    if (BackgroundImage != null)
                    {
                        g.DrawImage(BackgroundImage, rect, rect, GraphicsUnit.Pixel);
                    }
                }
            }

            DrawScoreImage(bmp, BackgroundImage, state.Scores, state.State.Master);

        }

        private IEnumerable<int> GetOrderedHandCards(CurrentPoker cp)
        {
            int suit = 0;
            if (State != null)
            {
                suit = State.State.Suit;
            }
            if (suit == 0)
            {
                suit = cp.Suit;
            }

            int[] suitOrder;
            switch (suit)
            {
                case 1:
                    suitOrder = new int[] { 2, 3, 4, 1 };
                    break;
                case 2:
                    suitOrder = new int[] { 3, 4, 1, 2 };
                    break;
                case 3:
                    suitOrder = new int[] { 4, 1, 2, 3 };
                    break;
                case 4:
                    suitOrder = new int[] { 1, 2, 3, 4 };
                    break;
                case 5:
                    suitOrder = new int[] { 1, 2, 3, 4, 5 };
                    break;
                default:
                    suitOrder = new int[] { 1, 2, 3, 4 };
                    break;
            }

            List<int> cards = new List<int>();
            foreach (int s in suitOrder)
            {
                cards.AddRange(cp.GetSuitCards(s));
            }
            return cards;
        }

        private void DrawCenterAllCards(Graphics g, int num)
        {
            if (g == null) return;
            for (int i = 0; i < num; i++)
            {
                g.DrawImage(_config.BackImage, 260 + i * 2, 280, 71, 96);
            }
        }

        private void DrawMySendedCardsAction(Bitmap bmp, ArrayList readys)
        {
            if (bmp == null || readys == null) return;
            using (Graphics g = Graphics.FromImage(bmp))
            {
                int start = 296 - readys.Count * 13 / 2;
                for (int i = 0; i < readys.Count; i++)
                {
                    g.DrawImage(GetPokerImageByNumber((int)readys[i]), start + i * 13, 244, 71, 96);
                }
            }
        }

        private static void Get8Cards(ArrayList list0, ArrayList list1, ArrayList list2, ArrayList list3)
        {
            list0.Add(list1[25]);
            list0.Add(list1[26]);
            list0.Add(list2[25]);
            list0.Add(list2[26]);
            list0.Add(list3[25]);
            list0.Add(list3[26]);
            list1.RemoveAt(26);
            list1.RemoveAt(25);
            list2.RemoveAt(26);
            list2.RemoveAt(25);
            list3.RemoveAt(26);
            list3.RemoveAt(25);
        }

        public void DrawBottomCards(Bitmap bmp, ArrayList cards)
        {
            if (cards == null) return;
            using (Graphics g = Graphics.FromImage(bmp))
            {
                for (int i = 0; i < cards.Count; i++)
                {
                    g.DrawImage(GetCardImageFunc((int)cards[i]), 230 + i * 14, 186, 71, 96);
                }
            }
        }


        #region 对手出牌渲染（Phase B 批 2）

        public void DrawOtherPlayerPlayedCards(Bitmap bmp, GameState state, int playerId)
        {
            if (playerId < 2 || playerId > 4) return;
            if (state.CurrentSendCards == null) return;
            var cards = state.CurrentSendCards[playerId - 1];
            if (cards == null || cards.Count == 0) return;

            using (Graphics g = Graphics.FromImage(bmp))
            {
                int x, y, dx;
                int firstSuit = state.State.Suit;
                int rank = state.State.Rank;
                int count = cards.Count;

                switch (playerId)
                {
                    case 2: // 对家（上方横排）
                        x = 320 - count * 10;
                        y = 55;
                        dx = 20;
                        for (int i = 0; i < cards.Count; i++)
                        {
                            g.DrawImage(GetCardImageFunc((int)cards[i]), x + i * dx, y, 71, 96);
                        }
                        break;
                    case 3: // 上家（左侧竖排）
                        x = 50;
                        y = 290 - count * 10;
                        dx = 0;
                        for (int i = 0; i < cards.Count; i++)
                        {
                            g.DrawImage(GetCardImageFunc((int)cards[i]), x, y - i * 10, 71, 96);
                        }
                        break;
                    case 4: // 下家（右侧竖排）
                        x = 530;
                        y = 290 - count * 10;
                        dx = 0;
                        for (int i = 0; i < cards.Count; i++)
                        {
                            g.DrawImage(GetCardImageFunc((int)cards[i]), x, y - i * 10, 71, 96);
                        }
                        break;
                }
            }
        }

        public void DrawFinishedOnceSendedCards(Bitmap bmp, GameState state)
        {
            if (state == null || state.CurrentSendCards == null) return;
            // 仅绘制，不修改状态 — 状态修改由 MainForm 负责
        }

        public void DrawFinishedScoreImage(Bitmap bmp, GameState state)
        {
            if (state == null) return;
            DrawScoreImage(bmp, BackgroundImage, state.Scores, state.State.Master);
        }

        #endregion

        #region 界面装饰（Phase B 批 1）

        public void DrawSidebar(Bitmap bmp)
        {
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.DrawImage(Properties.Resources.Sidebar, 20, 30, 70, 89);
                g.DrawImage(Properties.Resources.Sidebar, 540, 30, 70, 89);
            }
        }

        public void DrawMaster(Bitmap bmp, int who, int start)
        {
            if (who < 1 || who > 4) return;
            using (Graphics g = Graphics.FromImage(bmp))
            {
                start = start * 80;
                int X = 0;
                if (who == 1) { start += 40; X = 548; }
                else if (who == 2) { start += 60; X = 580; }
                else if (who == 3) { start += 0; X = 30; }
                else if (who == 4) { start += 20; X = 60; }
                g.DrawImage(Properties.Resources.Master, new Rectangle(X, 45, 20, 20),
                    new Rectangle(start, 0, 20, 20), GraphicsUnit.Pixel);
            }
        }

        public void DrawOtherMaster(Bitmap bmp, int who, int start)
        {
            using (Graphics g = Graphics.FromImage(bmp))
            {
                if (who != 1)
                    g.DrawImage(Properties.Resources.Master, new Rectangle(548, 45, 20, 20),
                        new Rectangle(40, 0, 20, 20), GraphicsUnit.Pixel);
                if (who != 2)
                    g.DrawImage(Properties.Resources.Master, new Rectangle(580, 45, 20, 20),
                        new Rectangle(60, 0, 20, 20), GraphicsUnit.Pixel);
                if (who != 3)
                    g.DrawImage(Properties.Resources.Master, new Rectangle(31, 45, 20, 20),
                        new Rectangle(0, 0, 20, 20), GraphicsUnit.Pixel);
                if (who != 4)
                    g.DrawImage(Properties.Resources.Master, new Rectangle(61, 45, 20, 20),
                        new Rectangle(20, 0, 20, 20), GraphicsUnit.Pixel);
            }
        }

        public void DrawRank(Bitmap bmp, int number, bool me, bool b)
        {
            using (Graphics g = Graphics.FromImage(bmp))
            {
                int X = 0, X2 = 0;
                if (me) { X = 566; X2 = 46; }
                else { X = 46; X2 = 566; }

                Rectangle destRect = new Rectangle(X, 88, 25, 25);
                Rectangle redrawRect = new Rectangle(X2, 88, 25, 25);

                if (!b)
                {
                    g.DrawImage(Properties.Resources.Suit, destRect,
                        new Rectangle(250, 0, 25, 25), GraphicsUnit.Pixel);
                    return;
                }

                // 画对应的牌点
                int srcX = (number - 2) * 25;
                if (srcX < 0) srcX = 0;
                g.DrawImage(Properties.Resources.Sidebar, destRect,
                    new Rectangle(23, 58, 25, 25), GraphicsUnit.Pixel);
                g.DrawImage(Properties.Resources.Suit, destRect,
                    new Rectangle(srcX, 50, 25, 25), GraphicsUnit.Pixel);
                g.DrawImage(Properties.Resources.Sidebar, redrawRect,
                    new Rectangle(23, 58, 25, 25), GraphicsUnit.Pixel);
                if (number > 0)
                {
                    DrawSuit(g, 0, !me, false);
                }
            }
        }

        #endregion

        #endregion
    }
}
