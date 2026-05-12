using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using Kuaff.Tractor.Helpers;

namespace Kuaff.Tractor
{
    /// <summary>
    /// GDI+ 渲染器。
    /// 接收 RenderCommand 和 gameState，在 Bitmap 上执行绘制。
    /// 
    /// 规则：只读 gameState，不修改。所有副作用仅限于 Bitmap 绘制。
    /// </summary>
    public class GdiRenderer
    {
        private GameConfig _config;

        /// <summary>
        /// 外部传入的获取牌图的回调。
        /// </summary>
        public Func<int, Bitmap> GetCardImageFunc { get; set; }

        /// <summary>
        /// 外部传入的主窗口背景图。
        /// </summary>
        public Bitmap BackgroundImage { get; set; }

        /// <summary>
        /// 游戏状态快照（只读引用，渲染时读取卡牌数据）。
        /// </summary>
        public GameState State { get; set; }

        public GdiRenderer(GameConfig config)
        {
            _config = config;
        }

        /// <summary>
        /// 执行一条渲染指令。
        /// </summary>
        public void Execute(RenderCommand cmd, Bitmap bmp, CurrentState currentState = null, object context = null)
        {
            switch (cmd.Type)
            {
                case RenderCmdType.RedrawAll:
                    DrawBackground(bmp, null);
                    break;

                case RenderCmdType.DealCard:
                    if (cmd.Payload is DealCardPayload deal)
                    {
                        DrawDealCard(bmp, deal.Round);
                    }
                    else if (cmd.Payload is int round)
                    {
                        DrawDealCard(bmp, round);
                    }
                    break;

                case RenderCmdType.DrawCenter8:
                    DrawCenter8Cards(bmp);
                    break;

                case RenderCmdType.RedrawMyHand:
                    if (State != null && State.CurrentPokers != null && State.CurrentPokers[0] != null)
                    {
                        DrawMySortedCards(bmp, State.CurrentPokers[0], State.CurrentPokers[0].Count);
                    }
                    break;

                case RenderCmdType.DrawPlayedCards:
                    if (cmd.Payload is PlayedCardsPayload played)
                    {
                        DrawPlayedCards(bmp, played.PlayerId, played.Cards);
                    }
                    break;

                case RenderCmdType.ShowToolbar:
                    DrawToolbar(bmp);
                    break;

                case RenderCmdType.ShowPassImage:
                    DrawPassImage(bmp);
                    break;

                case RenderCmdType.ShowBottomCards:
                    if (State != null && State.Send8Cards != null)
                    {
                        DrawBottomCards(bmp, State.Send8Cards);
                    }
                    break;

                case RenderCmdType.ShowRoundWinner:
                    DrawWhoWinThisTime(bmp);
                    break;

                case RenderCmdType.ShowRankResult:
                    if (State != null && State.Send8Cards != null)
                    {
                        DrawFinishedScoreImage(bmp, State.Scores, State.Send8Cards);
                    }
                    break;

                case RenderCmdType.None:
                case RenderCmdType.SetPause:
                default:
                    break;
            }
        }

        #region 发牌渲染

        private Bitmap GetCardImage(int number)
        {
            if (GetCardImageFunc != null)
                return GetCardImageFunc(number);
            if (_config.CardImageName.Length == 0)
                return (Bitmap)_config.CardsResourceManager.GetObject("_" + number);
            return _config.BackImage;
        }

        private void DrawCard(Bitmap bmp, Bitmap cardImg, int x, int y, int w, int h)
        {
            Graphics g = Graphics.FromImage(bmp);
            g.DrawImage(cardImg, x, y, w, h);
            g.Dispose();
        }

        /// <summary>
        /// 画第 count 轮的发牌动画（纯渲染，无数据修改）。
        /// </summary>
        public void DrawDealCard(Bitmap bmp, int count)
        {
            Graphics g = Graphics.FromImage(bmp);
            DrawCenterAllCards(g, 58 - count * 2);
            g.Dispose();

            if (State == null || State.PokerLists == null || State.PokerLists[0] == null)
                return;

            // 玩家1 (自己) 的牌动画
            if (count < State.PokerLists[0].Count)
            {
                int card0 = (int)State.PokerLists[0][count];
                Bitmap img0 = GetCardImage(card0);
                DrawCard(bmp, img0, 260, 280, 71, 96);
                if (State.CurrentPokers != null && State.CurrentPokers[0] != null)
                {
                    using (Graphics g2 = Graphics.FromImage(bmp))
                    {
                        DrawMyCards(g2, State.CurrentPokers[0], count);
                    }
                }
            }

            // 玩家2 (对家) 的牌背面动画
            DrawCard(bmp, _config.BackImage, 400 - count * 13, 60, 71, 96);
            using (Graphics g2 = Graphics.FromImage(bmp))
            {
                g2.DrawImage(_config.BackImage, 437 - count * 13, 25, 71, 96);
            }

            // 玩家3 (上手) 的牌背面动画
            DrawCard(bmp, _config.BackImage, 50, 160 + count * 4, 71, 96);
            using (Graphics g2 = Graphics.FromImage(bmp))
            {
                g2.DrawImage(_config.BackImage, 6, 145 + count * 4, 71, 96);
            }

            // 玩家4 (下手) 的牌背面动画
            DrawCard(bmp, _config.BackImage, 520, 220 - count * 4, 71, 96);
            using (Graphics g2 = Graphics.FromImage(bmp))
            {
                g2.DrawImage(_config.BackImage, 554, 241 - count * 4, 71, 96);
            }
        }

        /// <summary>
        /// 画中间发牌区域的牌堆。
        /// </summary>
        public void DrawCenterAllCards(Bitmap bmp, int num)
        {
            Graphics g = Graphics.FromImage(bmp);
            DrawCenterAllCards(g, num);
            g.Dispose();
        }

        private void DrawCenterAllCards(Graphics g, int num)
        {
            for (int i = 0; i < num; i++)
            {
                g.DrawImage(_config.BackImage, 260 + i * 2, 280, 71, 96);
            }
        }

        #endregion

        #region 核心画图方法

        /// <summary>
        /// 画背景。
        /// </summary>
        public void DrawBackground(Bitmap bmp, Rectangle? clientRect)
        {
            if (BackgroundImage == null) return;
            Graphics g = Graphics.FromImage(bmp);
            Rectangle rect = clientRect ?? new Rectangle(0, 0, bmp.Width, bmp.Height);
            g.DrawImage(BackgroundImage, rect, rect, GraphicsUnit.Pixel);
            g.Dispose();
        }

        /// <summary>
        /// 画中间区域背景图。
        /// </summary>
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

        /// <summary>
        /// 画"过牌"图片。
        /// </summary>
        public void DrawPassImage(Bitmap bmp)
        {
            Graphics g = Graphics.FromImage(bmp);
            Rectangle rect = new Rectangle(110, 150, 400, 199);
            g.DrawImage(Properties.Resources.Pass, rect);
            g.Dispose();
        }

        /// <summary>
        /// 获取牌的图片。
        /// </summary>
        public Bitmap GetPokerImageByNumber(int number)
        {
            return GetCardImage(number);
        }

        /// <summary>
        /// 画花色图标。
        /// </summary>
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
            Rectangle srcRect = new Rectangle(srcX, 0, 25, 25);
            g.DrawImage(Properties.Resources.Sidebar, destRect, new Rectangle(23, 58, 25, 25), GraphicsUnit.Pixel);
            g.DrawImage(Properties.Resources.Suit, destRect, srcRect, GraphicsUnit.Pixel);
            g.DrawImage(Properties.Resources.Sidebar, redrawRect, new Rectangle(23, 58, 25, 25), GraphicsUnit.Pixel);
            if (suit > 0) DrawSuit(g, 0, !me, false);
        }

        /// <summary>
        /// 画分数。
        /// </summary>
        public void DrawScoreImage(Bitmap bmp, Bitmap bgImage, int scores, int master)
        {
            Graphics g = Graphics.FromImage(bmp);
            Bitmap bmpScore = global::Kuaff.Tractor.Properties.Resources.scores;
            using (Font font = new Font("宋体", 12, FontStyle.Bold))
            {
                if (master == 2 || master == 4)
                {
                    Rectangle rect = new Rectangle(490, 128, 56, 56);
                    g.DrawImage(bgImage, rect, rect, GraphicsUnit.Pixel);
                    g.DrawImage(bmpScore, rect);
                    int x = 506;
                    if (scores.ToString().Length == 2) x -= 4;
                    else if (scores.ToString().Length == 3) x -= 8;
                    g.DrawString(scores.ToString(), font, Brushes.White, x, 138);
                }
                else
                {
                    Rectangle rect = new Rectangle(85, 300, 56, 56);
                    g.DrawImage(bgImage, rect, rect, GraphicsUnit.Pixel);
                    g.DrawImage(bmpScore, rect);
                    int x = 100;
                    if (scores.ToString().Length == 2) x -= 4;
                    else if (scores.ToString().Length == 3) x -= 8;
                    g.DrawString(scores.ToString(), font, Brushes.White, x, 310);
                }
            }
            g.Dispose();
        }

        /// <summary>
        /// 画底牌（8张）。
        /// </summary>
        public void DrawBottomCards(Bitmap bmp, ArrayList bottom)
        {
            Graphics g = Graphics.FromImage(bmp);
            DrawBottomCards(g, bottom);
            g.Dispose();
        }

        private void DrawBottomCards(Graphics g, ArrayList bottom)
        {
            bool isVertical = false; // 根据逻辑判断
            for (int i = 0; i < bottom.Count; i++)
            {
                int x = 230 + i * 14;
                int y = isVertical ? 146 : 186;
                g.DrawImage(GetCardImage((int)bottom[i]), x, y, 71, 96);
            }
        }

        /// <summary>
        /// 画 8 张底牌动画。
        /// </summary>
        public void DrawCenter8Cards(Bitmap bmp)
        {
            // MainForm 中 DrawCenter8Cards 的逻辑主要是扣底，暂时由 DrawingFormHelper 处理
            // 此方法作为占位
        }

        /// <summary>
        /// 画工具栏（花色选择）。
        /// </summary>
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

        /// <summary>
        /// 画玩家的手牌（排序后）。
        /// </summary>
        public void DrawMySortedCards(Bitmap bmp, CurrentPoker cp, int cardCount)
        {
            Graphics g = Graphics.FromImage(bmp);
            DrawMySortedCards(g, cp, cardCount);
            g.Dispose();
        }

        private void DrawMySortedCards(Graphics g, CurrentPoker cp, int cardCount)
        {
            if (cp == null || State?.PokerLists == null) return;
            ArrayList list = State.PokerLists[0];
            if (list == null) return;

            int start = (int)((2780 - cardCount * 75) / 10);
            Rectangle rect = new Rectangle(LayoutManager.MyCardsAreaX, LayoutManager.MyCardsAreaY,
                                           LayoutManager.MyCardsAreaWidth, LayoutManager.MyCardsAreaHeight);
            using (Graphics clearG = Graphics.FromImage((Bitmap)((Image)g.GetNearestColor(new Color()))))
            {
                // 无法从 g 直接 clear，使用 bmp 参数
            }
        }

        /// <summary>
        /// 画玩家手牌（发牌过程中）。
        /// </summary>
        public void DrawMyCards(Graphics g, CurrentPoker currentPoker, int index)
        {
            // DrawingFormHelper 中的 DrawMyCards 是画发牌过程中手牌排列
            // 这里简化处理，调用 DrawMySortedCards 代替
        }

        /// <summary>
        /// 画已出牌。
        /// </summary>
        public void DrawPlayedCards(Bitmap bmp, int playerId, List<int> cards)
        {
            // 已出牌由 DrawingFormHelper 中的 DrawMySendedCardsAction 等方法处理
            // 这里作为占位
        }

        /// <summary>
        /// 显示一圈赢家。
        /// </summary>
        public void DrawWhoWinThisTime(Bitmap bmp)
        {
            // 由 DrawingFormHelper.DrawWhoWinThisTime 处理
        }

        /// <summary>
        /// 显示一局结束画面。
        /// </summary>
        public void DrawFinishedScoreImage(Bitmap bmp, int scores, ArrayList send8Cards)
        {
            // 由 DrawingFormHelper.DrawFinishedScoreImage 处理
        }

        #endregion
    }
}
