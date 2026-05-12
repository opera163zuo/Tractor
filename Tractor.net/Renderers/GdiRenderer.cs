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
        public void Execute(RenderCommand cmd, Bitmap bmp, CurrentState currentState = null, object context = null)
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
                    // 复杂渲染→fallback
                    break;

                case RenderCmdType.RedrawMyHand:
                    if (State?.CurrentPokers?[0] != null)
                    {
                        DrawMySortedCards(bmp, State.CurrentPokers[0], State.CurrentPokers[0].Count);
                        handled = true;
                    }
                    break;

                case RenderCmdType.DrawPlayedCards:
                    // 复杂渲染→fallback
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
                    // 复杂渲染→fallback
                    break;

                case RenderCmdType.ShowRankResult:
                    // 复杂渲染→fallback
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
                Bitmap img0 = GetCardImage(card0);
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
            if (cp == null || State?.PokerLists == null) return;
            // 复杂手牌排序渲染通过 FallbackRender 转到 DrawingFormHelper
            // GdiRenderer 暂时只支持简化渲染
        }

        public void DrawBottomCards(Bitmap bmp, ArrayList cards)
        {
            if (cards == null) return;
            using (Graphics g = Graphics.FromImage(bmp))
            {
                for (int i = 0; i < cards.Count; i++)
                {
                    g.DrawImage(GetCardImage((int)cards[i]), 230 + i * 14, 186, 71, 96);
                }
            }
        }

        #endregion
    }
}
