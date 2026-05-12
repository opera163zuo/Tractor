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
        /// 外部传入的获取牌图的回调，解决 GdiRenderer 不直接持有 resource manager 的问题。
        /// 参数：牌号(int)，返回牌图(Bitmap)。
        /// </summary>
        public Func<int, Bitmap> GetCardImageFunc { get; set; }

        /// <summary>
        /// 外部传入的主窗口背景图。
        /// </summary>
        public Bitmap BackgroundImage { get; set; }

        public GdiRenderer(GameConfig config)
        {
            _config = config;
        }

        /// <summary>
        /// 执行一条渲染指令。
        /// </summary>
        public void Execute(RenderCommand cmd, Bitmap bmp, CurrentState state, object context = null)
        {
            // 本步骤只实现最基础的纯渲染方法
            // 后续步骤逐步扩展 switch case
            switch (cmd.Type)
            {
                case RenderCmdType.RedrawAll:
                    DrawBackground(bmp, null);
                    break;
                case RenderCmdType.ShowPassImage:
                    DrawPassImage(bmp);
                    break;
                case RenderCmdType.None:
                default:
                    break;
            }
        }

        #region 从 DrawingFormHelper 搬来的纯渲染方法

        /// <summary>
        /// 画背景。只读 mainForm.image 的等效数据。
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
        public void DrawCenterImage(Bitmap bmp, Bitmap backgroundImage)
        {
            if (backgroundImage == null) return;
            Graphics g = Graphics.FromImage(bmp);
            Rectangle rect = new Rectangle(LayoutManager.CenterBackgroundX,
                                           LayoutManager.CenterBackgroundY,
                                           LayoutManager.CenterBackgroundWidth,
                                           LayoutManager.CenterBackgroundHeight);
            g.DrawImage(backgroundImage, rect, rect, GraphicsUnit.Pixel);
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
            if (GetCardImageFunc != null)
            {
                return GetCardImageFunc(number);
            }

            if (_config.CardImageName.Length == 0)
            {
                return (Bitmap)_config.CardsResourceManager.GetObject("_" + number);
            }
            return null;
        }

        /// <summary>
        /// 画花色图标。
        /// </summary>
        public void DrawSuit(Graphics g, int suit, bool me, bool b)
        {
            int X = 0;
            int X2 = 0;
            if (me)
            {
                X = 563;
                X2 = 43;
            }
            else
            {
                X = 43;
                X2 = 563;
            }

            Rectangle destRect = new Rectangle(X, 88, 25, 25);
            Rectangle redrawRect = new Rectangle(X2, 88, 25, 25);

            if (!b)
            {
                Rectangle srcRect = new Rectangle(250, 0, 25, 25);
                g.DrawImage(Properties.Resources.Suit, destRect, srcRect, GraphicsUnit.Pixel);
                return;
            }

            if (suit == 1)
            {
                Rectangle srcRect = new Rectangle(0, 0, 25, 25);
                g.DrawImage(Properties.Resources.Sidebar, destRect, new Rectangle(23, 58, 25, 25), GraphicsUnit.Pixel);
                g.DrawImage(Properties.Resources.Suit, destRect, srcRect, GraphicsUnit.Pixel);
                g.DrawImage(Properties.Resources.Sidebar, redrawRect, new Rectangle(23, 58, 25, 25), GraphicsUnit.Pixel);
                DrawSuit(g, 0, !me, false);
            }
            else if (suit == 2)
            {
                Rectangle srcRect = new Rectangle(25, 0, 25, 25);
                g.DrawImage(Properties.Resources.Sidebar, destRect, new Rectangle(23, 58, 25, 25), GraphicsUnit.Pixel);
                g.DrawImage(Properties.Resources.Suit, destRect, srcRect, GraphicsUnit.Pixel);
                g.DrawImage(Properties.Resources.Sidebar, redrawRect, new Rectangle(23, 58, 25, 25), GraphicsUnit.Pixel);
                DrawSuit(g, 0, !me, false);
            }
            else if (suit == 3)
            {
                Rectangle srcRect = new Rectangle(50, 0, 25, 25);
                g.DrawImage(Properties.Resources.Sidebar, destRect, new Rectangle(23, 58, 25, 25), GraphicsUnit.Pixel);
                g.DrawImage(Properties.Resources.Suit, destRect, srcRect, GraphicsUnit.Pixel);
                g.DrawImage(Properties.Resources.Sidebar, redrawRect, new Rectangle(23, 58, 25, 25), GraphicsUnit.Pixel);
                DrawSuit(g, 0, !me, false);
            }
            else if (suit == 4)
            {
                Rectangle srcRect = new Rectangle(75, 0, 25, 25);
                g.DrawImage(Properties.Resources.Sidebar, destRect, new Rectangle(23, 58, 25, 25), GraphicsUnit.Pixel);
                g.DrawImage(Properties.Resources.Suit, destRect, srcRect, GraphicsUnit.Pixel);
                g.DrawImage(Properties.Resources.Sidebar, redrawRect, new Rectangle(23, 58, 25, 25), GraphicsUnit.Pixel);
                DrawSuit(g, 0, !me, false);
            }
            else if (suit == 5)
            {
                Rectangle srcRect = new Rectangle(100, 0, 25, 25);
                g.DrawImage(Properties.Resources.Sidebar, destRect, new Rectangle(23, 58, 25, 25), GraphicsUnit.Pixel);
                g.DrawImage(Properties.Resources.Suit, destRect, srcRect, GraphicsUnit.Pixel);
                g.DrawImage(Properties.Resources.Sidebar, redrawRect, new Rectangle(23, 58, 25, 25), GraphicsUnit.Pixel);
                DrawSuit(g, 0, !me, false);
            }
        }

        /// <summary>
        /// 画分数。
        /// </summary>
        public void DrawScoreImage(Bitmap bmp, Bitmap backgroundImage, int scores, int master)
        {
            Graphics g = Graphics.FromImage(bmp);
            Bitmap bmpScore = global::Kuaff.Tractor.Properties.Resources.scores;
            Font font = new Font("宋体", 12, FontStyle.Bold);

            if (master == 2 || master == 4)
            {
                Rectangle rect = new Rectangle(490, 128, 56, 56);
                g.DrawImage(backgroundImage, rect, rect, GraphicsUnit.Pixel);
                g.DrawImage(bmpScore, rect);
                int x = 506;
                if (scores.ToString().Length == 2) x -= 4;
                else if (scores.ToString().Length == 3) x -= 8;
                g.DrawString(scores + "", font, Brushes.White, x, 138);
            }
            else
            {
                Rectangle rect = new Rectangle(85, 300, 56, 56);
                g.DrawImage(backgroundImage, rect, rect, GraphicsUnit.Pixel);
                g.DrawImage(bmpScore, rect);
                int x = 100;
                if (scores.ToString().Length == 2) x -= 4;
                else if (scores.ToString().Length == 3) x -= 8;
                g.DrawString(scores + "", font, Brushes.White, x, 310);
            }

            g.Dispose();
        }

        #endregion
    }
}
