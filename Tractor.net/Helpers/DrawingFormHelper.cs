using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;
using System.Windows.Forms;
using System.Threading;

using Kuaff.CardResouces;

namespace Kuaff.Tractor
{
    /// <summary>
    /// ʵ�ִ󲿷ֵĻ滭����
    /// </summary>
    class DrawingFormHelper
    {
        MainForm mainForm;
        internal DrawingFormHelper(MainForm mainForm)
        {
            this.mainForm = mainForm;
        }

      
        #region ���ƶ���

        /// <summary>
        /// ׼������.
        /// �����ڳ������뻭��58-i*2����(ʵ��25+8�Ϳ����ˣ�Ϊ����ʾ�ƶ࣬������50+8),
        /// ÿ��һ���ƣ��������š�
        /// 
        /// Ȼ��ÿ�������з�һ���ƣ�Ȼ����ҿ�ʼ�����λ��õ��ƺ�Ľ��档
        /// ���������˻����ƺ�Ӧ�õ����㷨�еķ������ж��Ƿ�Ӧ��������
        /// </summary>
        /// <param name="count">���ƴ�����һ����25���ƣ�ÿ��25�ţ����ׯ���յ�</param>
        internal void ReadyCards(int count)【已迁移到 GdiRenderer.DrawDealCard，请用 RenderDealRound】
        
        {

            //�õ�������ͼ���Graphics
            Graphics g = Graphics.FromImage(mainForm.bmp);
            //���ƾֵ����룬ϴ�õ��ƣ�ʵ�ʻ�58��,ÿ��һ�ּ�������
            DrawCenterAllCards(g, 58 - count * 2);

            //��ǰÿ�������е���
            mainForm.currentPokers[0].AddCard((int)mainForm.pokerList[0][count]);
            mainForm.currentPokers[1].AddCard((int)mainForm.pokerList[1][count]);
            mainForm.currentPokers[2].AddCard((int)mainForm.pokerList[2][count]);
            mainForm.currentPokers[3].AddCard((int)mainForm.pokerList[3][count]);

            //���Լ���λ��
            DrawAnimatedCard(getPokerImageByNumber((int)mainForm.pokerList[0][count]), 260, 280, 71, 96);
            DrawMyCards(g, mainForm.currentPokers[0], count);
            //�ж����Ƿ��������
            if (mainForm.gameConfig.IsDebug)
            {
                DoRankOrNot(mainForm.currentPokers[0], 1);
            }
            else
            {

                MyRankOrNot(mainForm.currentPokers[0]);
            }
            mainForm.Refresh();

            //���Լҵ�λ��
            DrawAnimatedCard(mainForm.gameConfig.BackImage, 400 - count * 13, 60, 71, 96);
            DrawMyImage(g, mainForm.gameConfig.BackImage, 437 - count * 13, 25, 71, 96);
            mainForm.Refresh();

            //�Ƿ�����
            DoRankOrNot(mainForm.currentPokers[1], 2);

            //�����ҵ�λ��
            DrawAnimatedCard(mainForm.gameConfig.BackImage, 50, 160 + count * 4, 71, 96);
            DrawMyImage(g, mainForm.gameConfig.BackImage, 6, 145 + count * 4, 71, 96);
            mainForm.Refresh();

            //�Ƿ�����
            DoRankOrNot(mainForm.currentPokers[2], 3);

            //�����ҵ�λ��
            DrawAnimatedCard(mainForm.gameConfig.BackImage, 520, 220 - count * 4, 71, 96);
            DrawMyImage(g, mainForm.gameConfig.BackImage, 554, 241 - count * 4, 71, 96);
            mainForm.Refresh();


            //��������
            DrawSuitCards(g);
            //�Ƿ�����
            DoRankOrNot(mainForm.currentPokers[3], 4);

            mainForm.Refresh();

            g.Dispose();
        }

        /// <summary>
        /// 纯渲染方法：画第 count 轮的牌（步骤5新增）。
        /// 不做 AddCard / DoRankOrNot（这些由 Engine 处理）。
        /// </summary>
        internal void RenderDealRound(int count)
        {
            Graphics g = Graphics.FromImage(mainForm.bmp);
            DrawCenterAllCards(g, 58 - count * 2);

            DrawAnimatedCard(getPokerImageByNumber((int)mainForm.pokerList[0][count]), 260, 280, 71, 96);
            DrawMyCards(g, mainForm.currentPokers[0], count);
            mainForm.Refresh();

            DrawAnimatedCard(mainForm.gameConfig.BackImage, 400 - count * 13, 60, 71, 96);
            DrawMyImage(g, mainForm.gameConfig.BackImage, 437 - count * 13, 25, 71, 96);
            mainForm.Refresh();

            DrawAnimatedCard(mainForm.gameConfig.BackImage, 50, 160 + count * 4, 71, 96);
            DrawMyImage(g, mainForm.gameConfig.BackImage, 6, 145 + count * 4, 71, 96);
            mainForm.Refresh();

            DrawAnimatedCard(mainForm.gameConfig.BackImage, 520, 220 - count * 4, 71, 96);
            DrawMyImage(g, mainForm.gameConfig.BackImage, 554, 241 - count * 4, 71, 96);
            mainForm.Refresh();

            g.Dispose();
        }

        private void DrawSuitCards(Graphics g)
        {


            if (mainForm.showSuits == 1)
            {
                if (mainForm.whoShowRank == 2)
                {
                    g.DrawImage(getPokerImageByNumber(mainForm.currentState.Suit * 13 - 13 + mainForm.currentRank), 437, 124, 71, 96);
                }
                else if (mainForm.whoShowRank == 3)
                {

                    g.DrawImage(getPokerImageByNumber(mainForm.currentState.Suit * 13 - 13 + mainForm.currentRank), 80, 158, 71, 96);
                }
                else if (mainForm.whoShowRank == 4)
                {
                    g.DrawImage(getPokerImageByNumber(mainForm.currentState.Suit * 13 - 13 + mainForm.currentRank), 480, 200, 71, 96);
                }
            }
            else if (mainForm.showSuits == 2)
            {
                if (mainForm.whoShowRank == 2)
                {
                    ClearSuitCards(g);
                    g.DrawImage(getPokerImageByNumber(mainForm.currentState.Suit * 13 - 13 + mainForm.currentRank), 423, 124, 71, 96);
                    g.DrawImage(getPokerImageByNumber(mainForm.currentState.Suit * 13 - 13 + mainForm.currentRank), 437, 124, 71, 96);
                }
                else if (mainForm.whoShowRank == 3)
                {
                    ClearSuitCards(g);
                    g.DrawImage(getPokerImageByNumber(mainForm.currentState.Suit * 13 - 13 + mainForm.currentRank), 80, 158, 71, 96);
                    g.DrawImage(getPokerImageByNumber(mainForm.currentState.Suit * 13 - 13 + mainForm.currentRank), 80, 178, 71, 96);

                }
                else if (mainForm.whoShowRank == 4)
                {
                    ClearSuitCards(g);
                    g.DrawImage(getPokerImageByNumber(mainForm.currentState.Suit * 13 - 13 + mainForm.currentRank), 480, 200, 71, 96);
                    g.DrawImage(getPokerImageByNumber(mainForm.currentState.Suit * 13 - 13 + mainForm.currentRank), 480, 220, 71, 96);
                }
            }
        }
        //���������
        private void ClearSuitCards(Graphics g)
        {
            g.DrawImage(mainForm.image, new Rectangle(80, 158, 71, 96), new Rectangle(80, 158, 71, 96), GraphicsUnit.Pixel);
            g.DrawImage(mainForm.image, new Rectangle(480, 200, 71, 96), new Rectangle(480, 200, 71, 96), GraphicsUnit.Pixel);
            g.DrawImage(mainForm.image, new Rectangle(437, 124, 71, 96), new Rectangle(437, 124, 71, 96), GraphicsUnit.Pixel);
        }

        #endregion // ���ƶ���

        #region ������λ�õ���
        /// <summary>
        /// ����ʱ���������.
        /// ���ȴӵ�ͼ��ȡ��Ӧ��λ�ã��ػ���鱳����
        /// Ȼ�����Ƶı��滭58-count*2���ơ�
        /// 
        /// </summary>
        /// <param name="g">������ͼƬ��Graphics</param>
        /// <param name="num">�Ƶ�����=58-���ƴ���*2</param>
        internal void DrawCenterAllCards(Graphics g, int num)
        {
            Rectangle rect = new Rectangle(200, 186, (num + 1) * 2 + 71, 96);
            g.DrawImage(mainForm.image, rect, rect, GraphicsUnit.Pixel);

            for (int i = 0; i < num; i++)
            {
                g.DrawImage(mainForm.gameConfig.BackImage, 200 + i * 2, 186, 71, 96);
            }
        }

        /// <summary>
        /// ����һ���ƣ���Ҫ������������
        /// </summary>
        internal void DrawCenterImage()【已迁移到 GdiRenderer.DrawCenterImage】
        
        {
            Graphics g = Graphics.FromImage(mainForm.bmp);
            Rectangle rect = new Rectangle(77, 124, 476, 244);
            g.DrawImage(mainForm.image, rect, rect, GraphicsUnit.Pixel);
            g.Dispose();
        }

        /// <summary>
        /// ������ͼƬ
        /// </summary>
        internal void DrawPassImage()【已迁移到 GdiRenderer.DrawPassImage】
        
        {
            Graphics g = Graphics.FromImage(mainForm.bmp);
            Rectangle rect = new Rectangle(110, 150, 400, 199);
            g.DrawImage(Properties.Resources.Pass, rect);
            g.Dispose();
            mainForm.Refresh();
        }
        #endregion // ������λ�õ���

        #region ���ƴ���
        //�յ��ƵĶ���
        /// <summary>
        /// ����25�κ����ʣ��8����.
        /// ��ʱ�Ѿ�ȷ����ׯ�ң���8���ƽ���ׯ��,
        /// ͬʱ�Զ����ķ�ʽ��ʾ��
        /// </summary>
        internal void DrawCenter8Cards()
        {
            Graphics g = Graphics.FromImage(mainForm.bmp);
            Rectangle rect = new Rectangle(200, 186, 90, 96);
            Rectangle backRect = new Rectangle(77, 121, 477, 254);
            //���8�ŵ�ͼ��ȡ����
            Bitmap backup = mainForm.bmp.Clone(rect, PixelFormat.DontCare);
            //����λ���ñ�������
            //g.DrawImage(mainForm.image, rect, rect, GraphicsUnit.Pixel);
            g.DrawImage(mainForm.image, backRect, backRect, GraphicsUnit.Pixel);

            //������8�Ž���ׯ�ң�������ʽ��
            if (mainForm.currentState.Master == 1)
            {
                DrawAnimatedCard(backup, 300, 330, 90, 96);
                Get8Cards(mainForm.pokerList[0], mainForm.pokerList[1], mainForm.pokerList[2], mainForm.pokerList[3]);
            }
            else if (mainForm.currentState.Master == 2)
            {
                DrawAnimatedCard(backup, 200, 80, 90, 96);
                Get8Cards(mainForm.pokerList[1], mainForm.pokerList[0], mainForm.pokerList[2], mainForm.pokerList[3]);
            }
            else if (mainForm.currentState.Master == 3)
            {
                DrawAnimatedCard(backup, 70, 186, 90, 96);
                Get8Cards(mainForm.pokerList[2], mainForm.pokerList[1], mainForm.pokerList[0], mainForm.pokerList[3]);
            }
            else if (mainForm.currentState.Master == 4)
            {
                DrawAnimatedCard(backup, 400, 186, 90, 96);
                Get8Cards(mainForm.pokerList[3], mainForm.pokerList[1], mainForm.pokerList[2], mainForm.pokerList[0]);
            }
            mainForm.Refresh();

            g.Dispose();
        }
        //�����8�Ž���ׯ��
        private void Get8Cards(ArrayList list0, ArrayList list1, ArrayList list2, ArrayList list3)
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

        internal void DrawBottomCards(ArrayList bottom)
        {
            Graphics g = Graphics.FromImage(mainForm.bmp);

            //������,��169��ʼ��
            for (int i = 0; i < 8; i++)
            {
                if (i ==2)
                {
                    g.DrawImage(getPokerImageByNumber((int)bottom[i]), 230 + i * 14, 146, 71, 96);
                }
                else
                {
                    g.DrawImage(getPokerImageByNumber((int)bottom[i]), 230 + i * 14, 186, 71, 96);
                }
            }
           
            mainForm.Refresh();

            g.Dispose();
        }
        #endregion // ���ƴ���


        #region ����Sidebar��toolbar
        /// <summary>
        /// ����Sidebar
        /// </summary>
        /// <param name="g"></param>
        internal void DrawSidebar(Graphics g)
        {
            DrawMyImage(g, Properties.Resources.Sidebar, 20, 30, 70, 89);
            DrawMyImage(g, Properties.Resources.Sidebar, 540, 30, 70, 89);
        }
        /// <summary>
        /// �������ϱ�
        /// </summary>
        /// <param name="g">������ͼ���Graphics</param>
        /// <param name="who">��˭</param>
        /// <param name="b">�Ƿ���ɫ</param>
        internal void DrawMaster(Graphics g, int who, int start)
        {
            if (who < 1 || who > 4)
            {
                return;
            }

            start = start * 80;

            int X = 0;

            if (who == 1)
            {
                start += 40;
                X = 548;
            }
            else if (who == 2)
            {
                start += 60;
                X = 580;
            }
            else if (who == 3)
            {
                start += 0;
                X = 30;
            }
            else if (who == 4)
            {
                start += 20;
                X = 60;
            }

            Rectangle destRect = new Rectangle(X, 45, 20, 20);
            Rectangle srcRect = new Rectangle(start, 0, 20, 20);

            g.DrawImage(Properties.Resources.Master, destRect, srcRect, GraphicsUnit.Pixel);

        }

        /// <summary>
        /// ��������ɫ
        /// </summary>
        /// <param name="g"></param>
        /// <param name="who"></param>
        /// <param name="start"></param>
        internal void DrawOtherMaster(Graphics g, int who, int start)
        {
            

            if (who != 1)
            {
                Rectangle destRect = new Rectangle(548, 45, 20, 20);
                Rectangle srcRect = new Rectangle(40, 0, 20, 20);
                g.DrawImage(Properties.Resources.Master, destRect, srcRect, GraphicsUnit.Pixel);
            }
            if (who != 2)
            {
                Rectangle destRect = new Rectangle(580, 45, 20, 20);
                Rectangle srcRect = new Rectangle(60, 0, 20, 20);
                g.DrawImage(Properties.Resources.Master, destRect, srcRect, GraphicsUnit.Pixel);
            }
            if (who != 3)
            {
                Rectangle destRect = new Rectangle(31, 45, 20, 20);
                Rectangle srcRect = new Rectangle(0, 0, 20, 20);
                g.DrawImage(Properties.Resources.Master, destRect, srcRect, GraphicsUnit.Pixel);
            }
            if (who != 4)
            {
                Rectangle destRect = new Rectangle(61, 45, 20, 20);
                Rectangle srcRect = new Rectangle(20, 0, 20, 20);
                g.DrawImage(Properties.Resources.Master, destRect, srcRect, GraphicsUnit.Pixel);
            }

        }


        /// <summary>
        /// ����Rank
        /// </summary>
        /// <param name="g">������ͼ���Graphics</param>
        /// <param name="me">���һ��ǻ��Է�</param>
        /// <param name="b">��ɫ���ǰ�ɫ</param>
        internal void DrawRank(Graphics g, int number, bool me, bool b)
        {
            int X = 0;
            int X2 = 0;
            if (me)
            {
                X = 566;
                X2 = 46;
            }
            else
            {
                X = 46;
                X2 = 566;
            }

            Rectangle destRect = new Rectangle(X, 68, 20, 20);
            Rectangle destRect2 = new Rectangle(X2, 68, 20, 20);



            //Ȼ��������д��
            if (!b)
            {
                g.DrawImage(Properties.Resources.Sidebar, destRect, new Rectangle(26, 38, 20, 20), GraphicsUnit.Pixel);
                if (me)
                {
                    g.DrawImage(Properties.Resources.CardNumber, destRect, getCardNumberImage(mainForm.currentState.OurCurrentRank, b), GraphicsUnit.Pixel);
                }
                else
                {
                    g.DrawImage(Properties.Resources.CardNumber, destRect, getCardNumberImage(mainForm.currentState.OpposedCurrentRank, b), GraphicsUnit.Pixel);
                }

            }
            else
            {
                g.DrawImage(Properties.Resources.Sidebar, destRect, new Rectangle(26, 38, 20, 20), GraphicsUnit.Pixel);
                g.DrawImage(Properties.Resources.Sidebar, destRect2, new Rectangle(26, 38, 20, 20), GraphicsUnit.Pixel);

                if (me)
                {
                    g.DrawImage(Properties.Resources.CardNumber, destRect, getCardNumberImage(mainForm.currentState.OurCurrentRank, b), GraphicsUnit.Pixel);
                    g.DrawImage(Properties.Resources.CardNumber, destRect2, getCardNumberImage(mainForm.currentState.OpposedCurrentRank, !b), GraphicsUnit.Pixel);
                }
                else
                {
                    g.DrawImage(Properties.Resources.CardNumber, destRect, getCardNumberImage(mainForm.currentState.OpposedCurrentRank, b), GraphicsUnit.Pixel);
                    g.DrawImage(Properties.Resources.CardNumber, destRect2, getCardNumberImage(mainForm.currentState.OurCurrentRank, !b), GraphicsUnit.Pixel);
                }
            }

        }

        private Rectangle getCardNumberImage(int number, bool b)
        {
            Rectangle result = new Rectangle(0, 0, 0, 0);

            if (number >= 0 && number <= 12)
            {
                if (b)
                {
                    number += 14;
                }
                result = new Rectangle(number * 20, 0, 20, 20);
            }


            if ((number == 53) && (b))
            {
                result = new Rectangle(540, 0, 20, 20);
            }
            if ((number == 53) && (!b))
            {
                result = new Rectangle(260, 0, 20, 20);
            }

            return result;
        }


        /// <summary>
        /// ����ɫ
        /// </summary>
        /// <param name="g"></param>
        /// <param name="suit">��ɫ</param>
        /// <param name="me">���ҷ����ǶԷ�</param>
        /// <param name="b">�Ƿ���ɫ</param>
        internal void DrawSuit(【已迁移到 GdiRenderer.DrawSuit】
        Graphics g, int suit, bool me, bool b)
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

            //�������ɫ
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
            else if (suit == 3) //����
            {
                Rectangle srcRect = new Rectangle(50, 0, 25, 25);
                g.DrawImage(Properties.Resources.Sidebar, destRect, new Rectangle(23, 58, 25, 25), GraphicsUnit.Pixel);
                g.DrawImage(Properties.Resources.Suit, destRect, srcRect, GraphicsUnit.Pixel);
                g.DrawImage(Properties.Resources.Sidebar, redrawRect, new Rectangle(23, 58, 25, 25), GraphicsUnit.Pixel);
                DrawSuit(g, 0, !me, false);
            }
            else if (suit == 4)//÷��club
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
        /// ��������
        /// </summary>
        internal void DrawToolbar()【已迁移到 GdiRenderer.DrawToolbar】
        
        {
            Graphics g = Graphics.FromImage(mainForm.bmp);
            g.DrawImage(Properties.Resources.Toolbar, new Rectangle(415, 325, 129, 29), new Rectangle(0, 0, 129, 29), GraphicsUnit.Pixel);
            //�����ְ���ɫ
            g.DrawImage(Properties.Resources.Suit, new Rectangle(417, 327, 125, 25), new Rectangle(125, 0, 125, 25), GraphicsUnit.Pixel);
            g.Dispose();
        }

        /// <summary>
        /// ��ȥ������
        /// </summary>
        internal void RemoveToolbar()
        {
            Graphics g = Graphics.FromImage(mainForm.bmp);
            g.DrawImage(mainForm.image, new Rectangle(415, 325, 129, 29), new Rectangle(415, 325, 129, 29), GraphicsUnit.Pixel);
            g.Dispose();
        }


        #endregion // ����Sidebar��toolbar


        #region �ж��Ƿ�����
        //�Ƿ�Ӧ������,�����㷨
        private void DoRankOrNot(CurrentPoker currentPoker, int user)
        {
            //������������������ж�
            if (currentPoker.Rank == 53)
                return;


            //�����δ������������
            if (mainForm.currentState.Suit == 0)
            {
                int suit = Algorithm.ShouldSetRank(mainForm, user);

                if (suit > 0)
                {
                    mainForm.showSuits = 1;
                    mainForm.whoShowRank = user;

                    mainForm.currentState.Suit = suit;

                    if ((mainForm.currentRank == 0) && mainForm.isNew)
                    {
                        mainForm.currentState.Master = user; //
                    }

                    //��Ȼ�Ѿ�ȷ����˭���ģ�˭�������򼸣���ô�ͻ���

                    Graphics g = Graphics.FromImage(mainForm.bmp);

                    //������ʱ��ͬʱ����ɫ,��ɫ��ʾ��ׯ������
                    if ((mainForm.currentState.Master == 1) || (mainForm.currentState.Master == 2))
                    {
                        DrawSuit(g, suit, true, true);
                        DrawRank(g, mainForm.currentState.OurCurrentRank, true, true);
                    }
                    else
                    {
                        DrawSuit(g, suit, false, true);
                        DrawRank(g, mainForm.currentState.OpposedCurrentRank, false, true);
                    }


                    //��˭������,��ɫ��ʾ
                    //DrawMaster(g, user, 1);
                    //��ׯ�ң�����ɫ
                    DrawMaster(g, mainForm.currentState.Master, 1);
                    DrawOtherMaster(g, mainForm.currentState.Master, 1);

                    g.Dispose();


                }
            }
            else //�Ƿ���Է�
            {
                int suit = Algorithm.ShouldSetRankAgain(mainForm, currentPoker);

                

                if (suit > 0)
                {

                    //�Ƿ���Լӹ�
                    if ((suit == mainForm.currentState.Suit) && (mainForm.whoShowRank == user) && (!mainForm.gameConfig.CanMyStrengthen))  //����������ӹ�
                    {
                        return;
                    }

                    //�Ǽӹ�ʱ,�����Է�
                    if ((suit != mainForm.currentState.Suit) && (mainForm.whoShowRank == user) && (!mainForm.gameConfig.CanMyRankAgain))  //����������Է�
                    {
                        return;
                    }


                    int oldWhoShowRank = mainForm.whoShowRank;
                    int oldMaster = mainForm.currentState.Master;

                    mainForm.showSuits = 2;
                    mainForm.whoShowRank = user;


                    mainForm.currentState.Suit = suit;

                    if ((mainForm.currentRank == 0) && mainForm.isNew)
                    {
                        mainForm.currentState.Master = user;
                    }



                    Graphics g = Graphics.FromImage(mainForm.bmp);

                    //������ʱ��ͬʱ����ɫ,��ɫ��ʾ��ׯ������
                    if ((mainForm.currentState.Master == 1) || (mainForm.currentState.Master == 2))
                    {
                        DrawSuit(g, suit, true, true);
                        DrawRank(g, mainForm.currentState.OurCurrentRank, true, true);
                    }
                    else
                    {
                        DrawSuit(g, suit, false, true);
                        DrawRank(g, mainForm.currentState.OpposedCurrentRank, false, true);
                    }


                    //����ԭ��������
                    DrawOtherMaster(g, mainForm.currentState.Master, 1);

                    //��˭������,��ɫ��ʾ
                    //DrawMaster(g, user, 1);
                    //��ׯ�ң�����ɫ
                    DrawMaster(g, mainForm.currentState.Master, 1);

                    g.Dispose();



                }
            }

        }

        //�ж����Ƿ�����
        private void MyRankOrNot(CurrentPoker currentPoker)
        {
            //������������������ж�
            if (currentPoker.Rank == 53)
                return;
            bool[] suits = Algorithm.CanSetRank(mainForm, currentPoker);

            ReDrawToolbar(suits);


        }

        //���ҵĹ�����
        internal void ReDrawToolbar(bool[] suits)
        {
            Graphics g = Graphics.FromImage(mainForm.bmp);
            g.DrawImage(Properties.Resources.Toolbar, new Rectangle(415, 325, 129, 29), new Rectangle(0, 0, 129, 29), GraphicsUnit.Pixel);
            //�����ְ���ɫ
            for (int i = 0; i < 5; i++)
            {
                if (suits[i])
                {
                    g.DrawImage(Properties.Resources.Suit, new Rectangle(417 + i * 25, 327, 25, 25), new Rectangle(i * 25, 0, 25, 25), GraphicsUnit.Pixel);
                }
                else
                {
                    g.DrawImage(Properties.Resources.Suit, new Rectangle(417 + i * 25, 327, 25, 25), new Rectangle(125 + i * 25, 0, 25, 25), GraphicsUnit.Pixel);
                }
            }
            g.Dispose();
        }


        /// <summary>
        /// �ж�����Ƿ���������.
        /// �����㷨����������������򱾾����֣����·���
        /// </summary>
        /// <returns></returns>
        internal bool DoRankNot()
        {

            if (mainForm.currentState.Suit == 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// �ж����Ƿ�������������.
        /// �ڷ���ʱ�������¼�������ҽ����˵����
        /// ��������������Ͻ����˵����
        /// ����ҿ������������������
        /// </summary>
        /// <param name="e"></param>
        internal void IsClickedRanked(MouseEventArgs e)
        {
            bool[] suits = Algorithm.CanSetRank(mainForm, mainForm.currentPokers[0]);

            if (suits[0]) //�������
            {
                Region region = new Region(new Rectangle(417, 327, 25, 25));
                if (region.IsVisible(e.X, e.Y))
                {
                    mainForm.showSuits++;
                    mainForm.whoShowRank = 1;

                    mainForm.currentState.Suit = 1;
                    if ((mainForm.currentRank == 0) && mainForm.isNew)
                    {
                        mainForm.currentState.Master = 1;
                    }
                    Graphics g = Graphics.FromImage(mainForm.bmp);

                    if ((mainForm.currentState.Master == 1) || (mainForm.currentState.Master == 2))
                    {
                        DrawSuit(g, 1, true, true);
                        DrawRank(g, mainForm.currentState.OurCurrentRank, true, true);
                    }
                    else
                    {
                        DrawSuit(g, 1, false, true);
                        DrawRank(g, mainForm.currentState.OpposedCurrentRank, false, true);
                    }

                    
                    DrawMaster(g, mainForm.currentState.Master, 1);
                    DrawOtherMaster(g, mainForm.currentState.Master, 1);

                    ClearSuitCards(g);
                    g.Dispose();
                }
            }
            if (suits[1]) //�������
            {
                Region region = new Region(new Rectangle(443, 327, 25, 25));
                if (region.IsVisible(e.X, e.Y))
                {
                    mainForm.showSuits++;
                    mainForm.whoShowRank = 1;
                    Graphics g = Graphics.FromImage(mainForm.bmp);
                    mainForm.currentState.Suit = 2;
                    if ((mainForm.currentRank == 0) && mainForm.isNew)
                    {
                        mainForm.currentState.Master = 1;
                    
                    }


                    if ((mainForm.currentState.Master == 1) || (mainForm.currentState.Master == 2))
                    {
                        DrawSuit(g, 2, true, true);
                        DrawRank(g, mainForm.currentState.OurCurrentRank, true, true);
                    }
                    else
                    {
                        DrawSuit(g, 2, false, true);
                        DrawRank(g, mainForm.currentState.OpposedCurrentRank, false, true);
                    }
                    DrawMaster(g, mainForm.currentState.Master, 1);
                    DrawOtherMaster(g, mainForm.currentState.Master, 1);


                    ClearSuitCards(g);
                    g.Dispose();
                }
            }
            if (suits[2]) //�������
            {
                Region region = new Region(new Rectangle(468, 327, 25, 25));
                if (region.IsVisible(e.X, e.Y))
                {
                    mainForm.showSuits++;
                    mainForm.whoShowRank = 1;
                    Graphics g = Graphics.FromImage(mainForm.bmp);
                    mainForm.currentState.Suit = 3;
                    if ((mainForm.currentRank == 0) && mainForm.isNew)
                    {
                        mainForm.currentState.Master = 1;
                        
                    }


                    if ((mainForm.currentState.Master == 1) || (mainForm.currentState.Master == 2))
                    {
                        DrawSuit(g, 3, true, true);
                        DrawRank(g, mainForm.currentState.OurCurrentRank, true, true);
                    }
                    else
                    {
                        DrawSuit(g, 3, false, true);
                        DrawRank(g, mainForm.currentState.OpposedCurrentRank, false, true);
                    }
                    DrawMaster(g, mainForm.currentState.Master, 1);
                    DrawOtherMaster(g, mainForm.currentState.Master, 1);


                    
                    ClearSuitCards(g);
                    g.Dispose();
                }
            }
            if (suits[3]) //���÷��
            {
                Region region = new Region(new Rectangle(493, 327, 25, 25));
                if (region.IsVisible(e.X, e.Y))
                {
                    mainForm.showSuits++;
                    mainForm.whoShowRank = 1;
                    Graphics g = Graphics.FromImage(mainForm.bmp);
                    mainForm.currentState.Suit = 4;
                    if ((mainForm.currentRank == 0) && mainForm.isNew)
                    {
                        mainForm.currentState.Master = 1;
                        
                    }


                    if ((mainForm.currentState.Master == 1) || (mainForm.currentState.Master == 2))
                    {
                        DrawSuit(g, 4, true, true);
                        DrawRank(g, mainForm.currentState.OurCurrentRank, true, true);
                    }
                    else
                    {
                        DrawSuit(g, 4, false, true);
                        DrawRank(g, mainForm.currentState.OpposedCurrentRank, false, true);
                    }
                    DrawMaster(g, mainForm.currentState.Master, 1);
                    DrawOtherMaster(g, mainForm.currentState.Master, 1);

                    
                    ClearSuitCards(g);
                    g.Dispose();
                }
            }
            if (suits[4]) //�����
            {
                Region region = new Region(new Rectangle(518, 327, 25, 25));
                if (region.IsVisible(e.X, e.Y))
                {
                    mainForm.showSuits = 3;
                    mainForm.whoShowRank = 1;
                    Graphics g = Graphics.FromImage(mainForm.bmp);
                    mainForm.currentState.Suit = 5;
                    if ((mainForm.currentRank == 0) && mainForm.isNew)
                    {
                        mainForm.currentState.Master = 1;
                        
                    }



                    if ((mainForm.currentState.Master == 1) || (mainForm.currentState.Master == 2))
                    {
                        DrawSuit(g, 5, true, true);
                        DrawRank(g, mainForm.currentState.OurCurrentRank, true, true);
                    }
                    else
                    {
                        DrawSuit(g, 5, false, true);
                        DrawRank(g, mainForm.currentState.OpposedCurrentRank, false, true);
                    }

                    DrawMaster(g, mainForm.currentState.Master, 1);
                    DrawOtherMaster(g, mainForm.currentState.Master, 1);


                    ClearSuitCards(g);
                    g.Dispose();
                }
            }
        }
        #endregion // �ж��Ƿ�����


        #region �ڸ�������»��Լ�����

        /// <summary>
        /// �����ڼ���л����ҵ�����.
        /// ���ջ�ɫ�����ƽ������֡�
        /// </summary>
        /// <param name="g">������ͼƬ��Graphics</param>
        /// <param name="currentPoker">�ҵ�ǰ�õ�����</param>
        /// <param name="index">�����Ƶ�����</param>
        internal void DrawMyCards(Graphics g, CurrentPoker currentPoker, int index)
        {
            int j = 0;

            //���������Ļ
            Rectangle rect = new Rectangle(30, 360, 560, 96);
            g.DrawImage(mainForm.image, rect, rect, GraphicsUnit.Pixel);

            //ȷ���滭��ʼλ��
            int start = (int)((2780 - index * 75) / 10);

            //����
            j = DrawMyHearts(g, currentPoker, j, start);
            //��ɫ֮��ӿ�϶
            j++;


            //����
            j = DrawMyPeachs(g, currentPoker, j, start);
            //��ɫ֮��ӿ�϶
            j++;


            //����
            j = DrawMyDiamonds(g, currentPoker, j, start);
            //��ɫ֮��ӿ�϶
            j++;


            //÷��
            j = DrawMyClubs(g, currentPoker, j, start);
            //��ɫ֮��ӿ�϶
            j++;

            //Rank(�ݲ���������Rank)
            j = DrawHeartsRank(g, currentPoker, j, start);
            j = DrawPeachsRank(g, currentPoker, j, start);
            j = DrawClubsRank(g, currentPoker, j, start);
            j = DrawDiamondsRank(g, currentPoker, j, start);

            //С��
            j = DrawSmallJack(g, currentPoker, j, start);
            //����
            j = DrawBigJack(g, currentPoker, j, start);


        }

        //���Լ�����õ���,һ���������ƺ����,�ͳ�һ���ƺ����
        /// <summary>
        /// �ڳ���ײ������Ѿ�����õ���.
        /// ��������»�ʹ�����������
        /// 1.�����׼������ʱ
        /// 2.����һ����,��Ҫ�ػ��ײ�
        /// </summary>
        /// <param name="currentPoker"></param>
        internal void DrawMySortedCards(CurrentPoker currentPoker, int index)
        {

            //����ʱ�������
            //��������ʱ������¼�����е��Ƶ�λ�á���С���Ƿ񱻵��
            mainForm.myCardsLocation = new ArrayList();
            mainForm.myCardsNumber = new ArrayList();
            mainForm.myCardIsReady = new ArrayList();


            Graphics g = Graphics.FromImage(mainForm.bmp);

            //���������Ļ
            Rectangle rect = new Rectangle(30, 355, 600, 116);
            g.DrawImage(mainForm.image, rect, rect, GraphicsUnit.Pixel);

            //�����ʼλ��
            int start = (int)((2780 - index * 75) / 10);


            //��¼ÿ���Ƶ�Xֵ
            int j = 0;
            //��ʱ���������������ж��Ƿ�ĳ��ɫȱʧ
            int k = 0;
            if (mainForm.currentState.Suit == 1)//����
            {
                j = DrawMyPeachs(g, currentPoker, j, start) + 1;
                IsSuitLost(ref j, ref k);
                j = DrawMyDiamonds(g, currentPoker, j, start) + 1;
                IsSuitLost(ref j, ref k);
                j = DrawMyClubs(g, currentPoker, j, start) + 1;
                IsSuitLost(ref j, ref k);
                j = DrawMyHearts(g, currentPoker, j, start);

                j = DrawPeachsRank(g, currentPoker, j, start);
                j = DrawDiamondsRank(g, currentPoker, j, start);
                j = DrawClubsRank(g, currentPoker, j, start);
                j = DrawHeartsRank(g, currentPoker, j, start);
            }
            else if (mainForm.currentState.Suit == 2) //����
            {

                j = DrawMyDiamonds(g, currentPoker, j, start) + 1;
                IsSuitLost(ref j, ref k);
                j = DrawMyClubs(g, currentPoker, j, start) + 1;
                IsSuitLost(ref j, ref k);
                j = DrawMyHearts(g, currentPoker, j, start) + 1;
                IsSuitLost(ref j, ref k);
                j = DrawMyPeachs(g, currentPoker, j, start);


                j = DrawDiamondsRank(g, currentPoker, j, start);
                j = DrawClubsRank(g, currentPoker, j, start);
                j = DrawHeartsRank(g, currentPoker, j, start);
                j = DrawPeachsRank(g, currentPoker, j, start);
            }
            else if (mainForm.currentState.Suit == 3)  //��Ƭ
            {

                j = DrawMyClubs(g, currentPoker, j, start) + 1;
                IsSuitLost(ref j, ref k);
                j = DrawMyHearts(g, currentPoker, j, start) + 1;
                IsSuitLost(ref j, ref k);
                j = DrawMyPeachs(g, currentPoker, j, start) + 1;
                IsSuitLost(ref j, ref k);
                j = DrawMyDiamonds(g, currentPoker, j, start);


                j = DrawClubsRank(g, currentPoker, j, start);
                j = DrawHeartsRank(g, currentPoker, j, start);
                j = DrawPeachsRank(g, currentPoker, j, start);
                j = DrawDiamondsRank(g, currentPoker, j, start);//����
            }
            else if (mainForm.currentState.Suit == 4)
            {

                j = DrawMyHearts(g, currentPoker, j, start) + 1;
                IsSuitLost(ref j, ref k);
                j = DrawMyPeachs(g, currentPoker, j, start) + 1;
                IsSuitLost(ref j, ref k);
                j = DrawMyDiamonds(g, currentPoker, j, start) + 1;
                IsSuitLost(ref j, ref k);
                j = DrawMyClubs(g, currentPoker, j, start);


                j = DrawHeartsRank(g, currentPoker, j, start);
                j = DrawPeachsRank(g, currentPoker, j, start);
                j = DrawDiamondsRank(g, currentPoker, j, start);
                j = DrawClubsRank(g, currentPoker, j, start);//÷��
            }
            else if (mainForm.currentState.Suit == 5)
            {
                j = DrawMyHearts(g, currentPoker, j, start) + 1;
                IsSuitLost(ref j, ref k);
                j = DrawMyPeachs(g, currentPoker, j, start) + 1;
                IsSuitLost(ref j, ref k);
                j = DrawMyDiamonds(g, currentPoker, j, start) + 1;
                IsSuitLost(ref j, ref k);
                j = DrawMyClubs(g, currentPoker, j, start) + 1;
                IsSuitLost(ref j, ref k);

                j = DrawHeartsRank(g, currentPoker, j, start);
                j = DrawPeachsRank(g, currentPoker, j, start);
                j = DrawDiamondsRank(g, currentPoker, j, start);
                j = DrawClubsRank(g, currentPoker, j, start);
            }

            //С��
            j = DrawSmallJack(g, currentPoker, j, start);

            //����
            j = DrawBigJack(g, currentPoker, j, start);

            g.Dispose();
        }

        private static void IsSuitLost(ref int j, ref int k)
        {
            if ((j - k) <= 1)
            {
                j--;
            }
            k = j;
        }

        /// <summary>
        /// �ػ������е���.
        /// ���������˵��������һ�֮����л��ơ�
        /// </summary>
        /// <param name="currentPoker">��ǰ�����е���</param>
        /// <param name="index">�Ƶ�����</param>
        internal void DrawMyPlayingCards(CurrentPoker currentPoker)
        {
            int index = currentPoker.Count;


            mainForm.cardsOrderNumber = 0;

            Graphics g = Graphics.FromImage(mainForm.bmp);

            //���������Ļ
            Rectangle rect = new Rectangle(30, 355, 600, 116);
            g.DrawImage(mainForm.image, rect, rect, GraphicsUnit.Pixel);
            DrawScoreImage(mainForm.Scores);

            int start = (int)((2780 - index * 75) / 10);

            //Rank(��������Rank)
            //��¼ÿ���Ƶ�Xֵ
            int j = 0;
            //��ʱ���������������ж��Ƿ�ĳ��ɫȱʧ
            int k = 0;

            if (mainForm.currentState.Suit == 1)
            {
                j = DrawMyPeachs2(g, currentPoker, j, start) + 1;
                IsSuitLost(ref j, ref k);
                j = DrawMyDiamonds2(g, currentPoker, j, start) + 1;
                IsSuitLost(ref j, ref k);
                j = DrawMyClubs2(g, currentPoker, j, start) + 1;
                IsSuitLost(ref j, ref k);
                j = DrawMyHearts2(g, currentPoker, j, start);

                j = DrawPeachsRank2(g, currentPoker, j, start);
                j = DrawDiamondsRank2(g, currentPoker, j, start);
                j = DrawClubsRank2(g, currentPoker, j, start);
                j = DrawHeartsRank2(g, currentPoker, j, start);//����
            }
            else if (mainForm.currentState.Suit == 2)
            {

                j = DrawMyDiamonds2(g, currentPoker, j, start) + 1;
                IsSuitLost(ref j, ref k);
                j = DrawMyClubs2(g, currentPoker, j, start) + 1;
                IsSuitLost(ref j, ref k);
                j = DrawMyHearts2(g, currentPoker, j, start) + 1;
                IsSuitLost(ref j, ref k);
                j = DrawMyPeachs2(g, currentPoker, j, start);

                j = DrawDiamondsRank2(g, currentPoker, j, start);
                j = DrawClubsRank2(g, currentPoker, j, start);
                j = DrawHeartsRank2(g, currentPoker, j, start);
                j = DrawPeachsRank2(g, currentPoker, j, start);//����
            }
            else if (mainForm.currentState.Suit == 3)
            {

                j = DrawMyClubs2(g, currentPoker, j, start) + 1;
                IsSuitLost(ref j, ref k);
                j = DrawMyHearts2(g, currentPoker, j, start) + 1;
                IsSuitLost(ref j, ref k);
                j = DrawMyPeachs2(g, currentPoker, j, start) + 1;
                IsSuitLost(ref j, ref k);
                j = DrawMyDiamonds2(g, currentPoker, j, start);

                j = DrawClubsRank2(g, currentPoker, j, start);
                j = DrawHeartsRank2(g, currentPoker, j, start);
                j = DrawPeachsRank2(g, currentPoker, j, start);
                j = DrawDiamondsRank2(g, currentPoker, j, start);//����
            }
            else if (mainForm.currentState.Suit == 4)
            {

                j = DrawMyHearts2(g, currentPoker, j, start) + 1;
                IsSuitLost(ref j, ref k);
                j = DrawMyPeachs2(g, currentPoker, j, start) + 1;
                IsSuitLost(ref j, ref k);
                j = DrawMyDiamonds2(g, currentPoker, j, start) + 1;
                IsSuitLost(ref j, ref k);
                j = DrawMyClubs2(g, currentPoker, j, start);

                j = DrawHeartsRank2(g, currentPoker, j, start);
                j = DrawPeachsRank2(g, currentPoker, j, start);
                j = DrawDiamondsRank2(g, currentPoker, j, start);
                j = DrawClubsRank2(g, currentPoker, j, start);//÷��
            }
            else if (mainForm.currentState.Suit == 5)
            {
                j = DrawMyHearts2(g, currentPoker, j, start) + 1;
                IsSuitLost(ref j, ref k);
                j = DrawMyPeachs2(g, currentPoker, j, start) + 1;
                IsSuitLost(ref j, ref k);
                j = DrawMyDiamonds2(g, currentPoker, j, start) + 1;
                IsSuitLost(ref j, ref k);
                j = DrawMyClubs2(g, currentPoker, j, start) + 1;
                IsSuitLost(ref j, ref k);

                j = DrawHeartsRank2(g, currentPoker, j, start);
                j = DrawPeachsRank2(g, currentPoker, j, start);
                j = DrawDiamondsRank2(g, currentPoker, j, start);
                j = DrawClubsRank2(g, currentPoker, j, start);
            }

            //С��
            j = DrawSmallJack2(g, currentPoker, j, start);

            //����
            j = DrawBigJack2(g, currentPoker, j, start);


            //�жϵ�ǰ�ĳ������Ƿ���Ч,�����Ч����С��
            Rectangle pigRect = new Rectangle(296, 300, 53, 46);
            if (TractorRules.IsInvalid(mainForm, mainForm.currentSendCards, 1) && (mainForm.currentState.CurrentCardCommands == CardCommands.WaitingForMySending))
            {
                g.DrawImage(Properties.Resources.Ready, pigRect);
            }
            else
            {
                g.DrawImage(mainForm.image, pigRect, pigRect, GraphicsUnit.Pixel);
            }


            My8CardsIsReady(g);

            g.Dispose();
        }

        private void My8CardsIsReady(Graphics g)
        {
            //������ҿ���
            if ((mainForm.currentState.CurrentCardCommands == CardCommands.WaitingForSending8Cards))
            {
                int total = 0;
                for (int i = 0; i < mainForm.myCardIsReady.Count; i++)
                {
                    if ((bool)mainForm.myCardIsReady[i])
                    {
                        total++;
                    }
                }
                Rectangle pigRect = new Rectangle(296, 300, 53, 46);
                if (total == 8)
                {
                    g.DrawImage(Properties.Resources.Ready, pigRect);
                }
                else
                {
                    g.DrawImage(mainForm.image, pigRect, pigRect, GraphicsUnit.Pixel);

                }
            }
        }


        /// <summary>
        /// ����Ļ��������ҳ�����
        /// </summary>
        /// <param name="readys">�ҳ����Ƶ��б�</param>
        internal void DrawMySendedCardsAction(ArrayList readys)
        {
            int start = 285 - readys.Count * 7;
            Graphics g = Graphics.FromImage(mainForm.bmp);
            for (int i = 0; i < readys.Count; i++)
            {
                DrawMyImage(g, getPokerImageByNumber((int)readys[i]), start, 244, 71, 96);
                start += 14;
            }
            g.Dispose();


        }

        /// <summary>
        /// ���Լҵ���
        /// </summary>
        /// <param name="readys"></param>
        private void DrawFrieldUserSendedCardsAction(ArrayList readys)
        {
            int start = 285 - readys.Count * 7;
            Graphics g = Graphics.FromImage(mainForm.bmp);
            for (int i = 0; i < readys.Count; i++)
            {
                DrawMyImage(g, getPokerImageByNumber((int)readys[i]), start, 130, 71, 96);
                start += 14;
            }
            RedrawFrieldUserCardsAction(g, mainForm.currentPokers[1]);


            g.Dispose();
        }
        private void RedrawFrieldUserCardsAction(Graphics g, CurrentPoker cp)
        {
            Rectangle rect = new Rectangle(105, 25, 420, 96);
            g.DrawImage(mainForm.image, rect, rect, GraphicsUnit.Pixel);
            int start = (int)((2500 + cp.Count * 75) / 10);
            for (int i = 0; i < cp.Count; i++) //��໭25����
            {
                DrawMyImage(g, mainForm.gameConfig.BackImage, start, 25, 71, 96);
                start -= 13;
            }
        }


        /// <summary>
        /// ���ϼ�Ӧ�ó�����
        /// </summary>
        /// <param name="readys"></param>
        private void DrawPreviousUserSendedCardsAction(ArrayList readys)
        {
            int start = 245 - readys.Count * 13;
            Graphics g = Graphics.FromImage(mainForm.bmp);
            for (int i = 0; i < readys.Count; i++)
            {
                DrawMyImage(g, getPokerImageByNumber((int)readys[i]), start + i * 13, 192, 71, 96);
            }

            RedrawPreviousUserCardsAction(g, mainForm.currentPokers[2]);

            g.Dispose();
        }
        private void RedrawPreviousUserCardsAction(Graphics g, CurrentPoker cp)
        {
            Rectangle rect = new Rectangle(6, 140, 71, 202);
            g.DrawImage(mainForm.image, rect, rect, GraphicsUnit.Pixel);

            int start = 195 - cp.Count * 2;
            for (int i = 0; i < cp.Count; i++)  //��໭25��,��Ϊ���˲��û��ˣ���ʹ�յ�
            {
                DrawMyImage(g, mainForm.gameConfig.BackImage, 6, start, 71, 96);
                start += 4;
            }
        }


        /// <summary>
        /// ���¼�Ӧ�ó�����
        /// </summary>
        /// <param name="readys"></param>
        private void DrawNextUserSendedCardsAction(ArrayList readys)
        {
            Graphics g = Graphics.FromImage(mainForm.bmp);
            for (int i = 0; i < readys.Count; i++)
            {
                DrawMyImage(g, getPokerImageByNumber((int)readys[i]), 326 + i * 13, 192, 71, 96);
            }

            RedrawNextUserCardsAction(g, mainForm.currentPokers[3]);


            g.Dispose();
        }
        private void RedrawNextUserCardsAction(Graphics g, CurrentPoker cp)
        {
            Rectangle rect = new Rectangle(554, 136, 71, 210);
            g.DrawImage(mainForm.image, rect, rect, GraphicsUnit.Pixel);
            //554, 241 - count * 4, 71, 96
            int start = 191 + cp.Count * 2;
            for (int i = 0; i < cp.Count; i++)
            {
                DrawMyImage(g, mainForm.gameConfig.BackImage, 554, start, 71, 96);
                start -= 4;
            }
        }


        #endregion // �ڸ�������»��Լ�����


        #region ���Լ�������(���ֻ�ɫ�����ֻ�ɫRank����С��)
        private int DrawBigJack(Graphics g, CurrentPoker currentPoker, int j, int start)
        {
            j = DrawMyOneOrTwoCards(g, currentPoker.BigJack, 53, j, start);
            return j;
        }


        private int DrawSmallJack(Graphics g, CurrentPoker currentPoker, int j, int start)
        {
            j = DrawMyOneOrTwoCards(g, currentPoker.SmallJack, 52, j, start);
            return j;
        }

        private int DrawDiamondsRank(Graphics g, CurrentPoker currentPoker, int j, int start)
        {
            j = DrawMyOneOrTwoCards(g, currentPoker.DiamondsRankTotal, mainForm.currentRank + 26, j, start);
            return j;
        }

        private int DrawClubsRank(Graphics g, CurrentPoker currentPoker, int j, int start)
        {
            j = DrawMyOneOrTwoCards(g, currentPoker.ClubsRankTotal, mainForm.currentRank + 39, j, start);
            return j;
        }

        private int DrawPeachsRank(Graphics g, CurrentPoker currentPoker, int j, int start)
        {
            j = DrawMyOneOrTwoCards(g, currentPoker.PeachsRankTotal, mainForm.currentRank + 13, j, start);
            return j;
        }

        private int DrawHeartsRank(Graphics g, CurrentPoker currentPoker, int j, int start)
        {
            j = DrawMyOneOrTwoCards(g, currentPoker.HeartsRankTotal, mainForm.currentRank, j, start);
            return j;
        }

        private int DrawMyClubs(Graphics g, CurrentPoker currentPoker, int j, int start)
        {
            for (int i = 0; i < 13; i++)
            {
                j = DrawMyOneOrTwoCards(g, currentPoker.ClubsNoRank[i], i + 39, j, start);
            }
            return j;
        }

        private int DrawMyDiamonds(Graphics g, CurrentPoker currentPoker, int j, int start)
        {
            for (int i = 0; i < 13; i++)
            {
                j = DrawMyOneOrTwoCards(g, currentPoker.DiamondsNoRank[i], i + 26, j, start);
            }
            return j;
        }

        private int DrawMyPeachs(Graphics g, CurrentPoker currentPoker, int j, int start)
        {
            for (int i = 0; i < 13; i++)
            {
                j = DrawMyOneOrTwoCards(g, currentPoker.PeachsNoRank[i], i + 13, j, start);

            }
            return j;
        }

        private int DrawMyHearts(Graphics g, CurrentPoker currentPoker, int j, int start)
        {
            for (int i = 0; i < 13; i++)
            {
                j = DrawMyOneOrTwoCards(g, currentPoker.HeartsNoRank[i], i, j, start);
            }
            return j;
        }

        //��������
        private int DrawMyOneOrTwoCards(Graphics g, int count, int number, int j, int start)
        {
            //�������������������Ҫ��������������һ��
            bool b = (number == 52) || (number == 53);
            b = b & (mainForm.currentState.Suit == 5);
            if (number == (mainForm.currentState.Suit-1)*13 + mainForm.currentRank)
            {
                b = true;
            }

            b = b && (mainForm.currentState.CurrentCardCommands == CardCommands.ReadyCards);

            if (count == 1)
            {
                SetCardsInformation(start + j * 13, number, false);
                if (mainForm.whoShowRank == 1 && b)
                {
                    if (number == 52 || number == 53)
                    {
                        g.DrawImage(getPokerImageByNumber(number), start + j * 13, 375, 71, 96); //����������������
                    }
                    else
                    {
                        g.DrawImage(getPokerImageByNumber(number), start + j * 13, 360, 71, 96);
                    }
                }
                else
                {
                    g.DrawImage(getPokerImageByNumber(number), start + j * 13, 375, 71, 96);
                }

                j++;
            }
            else if (count == 2)
            {
                SetCardsInformation(start + j * 13, number, false);

                if (mainForm.whoShowRank == 1 && b && mainForm.showSuits >= 1)
                {
                    g.DrawImage(getPokerImageByNumber(number), start + j * 13, 360, 71, 96);
                }
                else
                {
                    g.DrawImage(getPokerImageByNumber(number), start + j * 13, 375, 71, 96);
                }
                
                j++;
                SetCardsInformation(start + j * 13, number, false);
                if (mainForm.whoShowRank == 1 && b && mainForm.showSuits >= 2)
                {
                    g.DrawImage(getPokerImageByNumber(number), start + j * 13, 360, 71, 96);
                }
                else
                {
                    g.DrawImage(getPokerImageByNumber(number), start + j * 13, 375, 71, 96);
                }

                j++;
            }
            return j;
        }


        #endregion // ���Լ�������

        #region ���Լ�����ķ���
        private int DrawBigJack2(Graphics g, CurrentPoker currentPoker, int j, int start)
        {
            if (currentPoker.BigJack == 1)
            {
                j = DrawMyOneOrTwoCards2(g, j, 53, start + j * 13, 355, 71, 96) + 1;
            }
            else if (currentPoker.BigJack == 2)
            {
                j = DrawMyOneOrTwoCards2(g, j, 53, start + j * 13, 355, 71, 96) + 1;
                j = DrawMyOneOrTwoCards2(g, j, 53, start + j * 13, 355, 71, 96) + 1;
            }
            return j;
        }

        private int DrawSmallJack2(Graphics g, CurrentPoker currentPoker, int j, int start)
        {
            if (currentPoker.SmallJack == 1)
            {
                j = DrawMyOneOrTwoCards2(g, j, 52, start + j * 13, 355, 71, 96) + 1;
            }
            else if (currentPoker.SmallJack == 2)
            {
                j = DrawMyOneOrTwoCards2(g, j, 52, start + j * 13, 355, 71, 96) + 1;
                j = DrawMyOneOrTwoCards2(g, j, 52, start + j * 13, 355, 71, 96) + 1;
            }
            return j;
        }

        private int DrawDiamondsRank2(Graphics g, CurrentPoker currentPoker, int j, int start)
        {
            if (currentPoker.DiamondsRankTotal == 1)
            {
                j = DrawMyOneOrTwoCards2(g, j, mainForm.currentRank + 26, start + j * 13, 355, 71, 96) + 1;
            }
            else if (currentPoker.DiamondsRankTotal == 2)
            {
                j = DrawMyOneOrTwoCards2(g, j, mainForm.currentRank + 26, start + j * 13, 355, 71, 96) + 1;
                j = DrawMyOneOrTwoCards2(g, j, mainForm.currentRank + 26, start + j * 13, 355, 71, 96) + 1;
            }
            return j;
        }

        private int DrawClubsRank2(Graphics g, CurrentPoker currentPoker, int j, int start)
        {
            if (currentPoker.ClubsRankTotal == 1)
            {
                j = DrawMyOneOrTwoCards2(g, j, mainForm.currentRank + 39, start + j * 13, 355, 71, 96) + 1;
            }
            else if (currentPoker.ClubsRankTotal == 2)
            {
                j = DrawMyOneOrTwoCards2(g, j, mainForm.currentRank + 39, start + j * 13, 355, 71, 96) + 1;
                j = DrawMyOneOrTwoCards2(g, j, mainForm.currentRank + 39, start + j * 13, 355, 71, 96) + 1;
            }
            return j;
        }

        private int DrawPeachsRank2(Graphics g, CurrentPoker currentPoker, int j, int start)
        {
            if (currentPoker.PeachsRankTotal == 1)
            {
                j = DrawMyOneOrTwoCards2(g, j, mainForm.currentRank + 13, start + j * 13, 355, 71, 96) + 1;
            }
            else if (currentPoker.PeachsRankTotal == 2)
            {
                j = DrawMyOneOrTwoCards2(g, j, mainForm.currentRank + 13, start + j * 13, 355, 71, 96) + 1;
                j = DrawMyOneOrTwoCards2(g, j, mainForm.currentRank + 13, start + j * 13, 355, 71, 96) + 1;
            }
            return j;
        }

        private int DrawHeartsRank2(Graphics g, CurrentPoker currentPoker, int j, int start)
        {
            if (currentPoker.HeartsRankTotal == 1)
            {
                j = DrawMyOneOrTwoCards2(g, j, mainForm.currentRank, start + j * 13, 355, 71, 96) + 1;
            }
            else if (currentPoker.HeartsRankTotal == 2)
            {
                j = DrawMyOneOrTwoCards2(g, j, mainForm.currentRank, start + j * 13, 355, 71, 96) + 1;
                j = DrawMyOneOrTwoCards2(g, j, mainForm.currentRank, start + j * 13, 355, 71, 96) + 1;
            }
            return j;
        }

        private int DrawMyClubs2(Graphics g, CurrentPoker currentPoker, int j, int start)
        {
            for (int i = 0; i < 13; i++)
            {
                if (currentPoker.ClubsNoRank[i] == 1)
                {
                    j = DrawMyOneOrTwoCards2(g, j, i + 39, start + j * 13, 355, 71, 96) + 1;
                }
                else if (currentPoker.ClubsNoRank[i] == 2)
                {
                    j = DrawMyOneOrTwoCards2(g, j, i + 39, start + j * 13, 355, 71, 96) + 1;
                    j = DrawMyOneOrTwoCards2(g, j, i + 39, start + j * 13, 355, 71, 96) + 1;
                }
            }
            return j;
        }

        private int DrawMyDiamonds2(Graphics g, CurrentPoker currentPoker, int j, int start)
        {
            for (int i = 0; i < 13; i++)
            {
                if (currentPoker.DiamondsNoRank[i] == 1)
                {
                    j = DrawMyOneOrTwoCards2(g, j, i + 26, start + j * 13, 355, 71, 96) + 1;
                }
                else if (currentPoker.DiamondsNoRank[i] == 2)
                {
                    j = DrawMyOneOrTwoCards2(g, j, i + 26, start + j * 13, 355, 71, 96) + 1;
                    j = DrawMyOneOrTwoCards2(g, j, i + 26, start + j * 13, 355, 71, 96) + 1;
                }
            }
            return j;
        }

        private int DrawMyPeachs2(Graphics g, CurrentPoker currentPoker, int j, int start)
        {
            for (int i = 0; i < 13; i++)
            {
                if (currentPoker.PeachsNoRank[i] == 1)
                {
                    j = DrawMyOneOrTwoCards2(g, j, i + 13, start + j * 13, 355, 71, 96) + 1;
                }
                else if (currentPoker.PeachsNoRank[i] == 2)
                {
                    j = DrawMyOneOrTwoCards2(g, j, i + 13, start + j * 13, 355, 71, 96) + 1;
                    j = DrawMyOneOrTwoCards2(g, j, i + 13, start + j * 13, 355, 71, 96) + 1;
                }
            }
            return j;
        }

        private int DrawMyHearts2(Graphics g, CurrentPoker currentPoker, int j, int start)
        {
            for (int i = 0; i < 13; i++)
            {
                if (currentPoker.HeartsNoRank[i] == 1)
                {
                    j = DrawMyOneOrTwoCards2(g, j, i, start + j * 13, 355, 71, 96) + 1;
                }
                else if (currentPoker.HeartsNoRank[i] == 2)
                {
                    j = DrawMyOneOrTwoCards2(g, j, i, start + j * 13, 355, 71, 96) + 1;
                    j = DrawMyOneOrTwoCards2(g, j, i, start + j * 13, 355, 71, 96) + 1;
                }
            }
            return j;
        }

        //��������
        private int DrawMyOneOrTwoCards2(Graphics g, int j, int number, int x, int y, int width, int height)
        {
            if ((bool)mainForm.myCardIsReady[mainForm.cardsOrderNumber])
            {
                g.DrawImage(getPokerImageByNumber(number), x, y, width, height);
            }
            else
            {
                g.DrawImage(getPokerImageByNumber(number), x, y + 20, width, height);
            }

            mainForm.cardsOrderNumber++;
            return j;
        }
        #endregion // ���ƵĻ��Լ�����ķ���

        #region ���Ƹ��ҳ����ƣ�������������֪ͨ��һ��
        /// <summary>
        /// ���Լ�������
        /// </summary>
        internal void DrawMyFinishSendedCards()
        {
            //�����뻭���������
            DrawMySendedCardsAction(mainForm.currentSendCards[0]);

            for (int i = 0; i < mainForm.currentSendCards[0].Count; i++)
            {
                mainForm.currentAllSendPokers[0].AddCard((int)mainForm.currentSendCards[0][i]);
            }


            //�ػ��Լ����е���
            if (mainForm.currentPokers[0].Count > 0)
            {
                DrawMySortedCards(mainForm.currentPokers[0], mainForm.currentPokers[0].Count);
            }
            else //�����²��ռ�
            {
                Rectangle rect = new Rectangle(30, 355, 560, 116);
                Graphics g = Graphics.FromImage(mainForm.bmp);
                g.DrawImage(mainForm.image, rect, rect, GraphicsUnit.Pixel);
                g.Dispose();
            }

            DrawScoreImage(mainForm.Scores);
            mainForm.Refresh();

            //����Ŀǰ˭�������

            if (mainForm.currentSendCards[3].Count > 0) //�Ƿ����
            {
                mainForm.currentState.CurrentCardCommands = CardCommands.Pause;
                mainForm.SetPauseSet(mainForm.gameConfig.FinishedOncePauseTime, CardCommands.DrawOnceFinished);

                DrawWhoWinThisTime();

            }
            else
            {
                mainForm.whoseOrder = 4;
                mainForm.currentState.CurrentCardCommands = CardCommands.WaitingForSend;
            }



        }

        /// <summary>
        /// �¼ҳ���
        /// </summary>
        internal void DrawNextUserSendedCards()
        {
            mainForm.currentState.CurrentCardCommands = CardCommands.Undefined;
            //��NextUser������
            if (mainForm.currentSendCards[0].Count > 0) //����
            {
                DrawNextUserSendedCardsAction(Algorithm.MustSendedCards(mainForm, 4, mainForm.currentPokers, mainForm.currentSendCards, mainForm.currentState.Suit, mainForm.currentRank, mainForm.currentSendCards[mainForm.firstSend - 1].Count));
            }
            else
            {
                DrawNextUserSendedCardsAction(Algorithm.ShouldSendedCards(mainForm, 4, mainForm.currentPokers, mainForm.currentSendCards, mainForm.currentState.Suit, mainForm.currentRank));
                mainForm.whoseOrder = 2;
            }

            //�����Ƿ��ס������
            //���Ѿ����ƣ�Ӧ�ý����ػ�
            int myCount = mainForm.currentSendCards[0].Count;
            if (myCount > 0)
            {
                int start = 285 - myCount * 7;
                Graphics g = Graphics.FromImage(mainForm.bmp);
                Rectangle rect = new Rectangle(start, 254, myCount * 14 + 57, 96);
                g.DrawImage(mainForm.image, rect, rect, GraphicsUnit.Pixel);
                for (int i = 0; i < myCount; i++)
                {
                    DrawMyImage(g, getPokerImageByNumber((int)mainForm.currentSendCards[0][i]), start, 244, 71, 96);
                    start += 14;
                }
                g.Dispose();
            }

            DrawScoreImage(mainForm.Scores);
            mainForm.Refresh();

            //
            if (mainForm.currentSendCards[1].Count > 0)
            {
                mainForm.currentState.CurrentCardCommands = CardCommands.Pause;
                mainForm.SetPauseSet(mainForm.gameConfig.FinishedOncePauseTime, CardCommands.DrawOnceFinished);

                DrawWhoWinThisTime();
            }
            else
            {
                mainForm.whoseOrder = 2;
                mainForm.currentState.CurrentCardCommands = CardCommands.WaitingForSend;
            }


        }

        /// <summary>
        /// �Լҳ���
        /// </summary>
        internal void DrawFrieldUserSendedCards()
        {
            mainForm.currentState.CurrentCardCommands = CardCommands.Undefined;
            //��FrieldUser������
            if (mainForm.currentSendCards[3].Count > 0) //����
            {
                DrawFrieldUserSendedCardsAction(Algorithm.MustSendedCards(mainForm, 2, mainForm.currentPokers, mainForm.currentSendCards, mainForm.currentState.Suit, mainForm.currentRank, mainForm.currentSendCards[mainForm.firstSend - 1].Count));
            }
            else
            {
                DrawFrieldUserSendedCardsAction(Algorithm.ShouldSendedCards(mainForm, 2, mainForm.currentPokers, mainForm.currentSendCards, mainForm.currentState.Suit, mainForm.currentRank));
            }


            //�����Ƿ��ס������
            //����¼��Ѿ����ƣ�Ӧ�ý��¼��ػ�,�ػ��¼�ʱ���п��ܸ�ס��
            int myCount = mainForm.currentSendCards[3].Count;
            if (myCount > 0)
            {
                Graphics g = Graphics.FromImage(mainForm.bmp);
                for (int i = 0; i < myCount; i++)
                {
                    DrawMyImage(g, getPokerImageByNumber((int)mainForm.currentSendCards[3][i]), 326 + i * 13, 192, 71, 96);
                }
                g.Dispose();
            }
            myCount = mainForm.currentSendCards[0].Count;
            if (myCount > 0)
            {
                int start = 285 - myCount * 7;
                Graphics g = Graphics.FromImage(mainForm.bmp);
                Rectangle rect = new Rectangle(start, 254, myCount * 14 + 57, 96);
                g.DrawImage(mainForm.image, rect, rect, GraphicsUnit.Pixel);
                for (int i = 0; i < myCount; i++)
                {
                    DrawMyImage(g, getPokerImageByNumber((int)mainForm.currentSendCards[0][i]), start, 244, 71, 96);
                    start += 14;
                }
                g.Dispose();
            }

            DrawScoreImage(mainForm.Scores);
            mainForm.Refresh();

            //
            if (mainForm.currentSendCards[2].Count > 0)
            {
                mainForm.currentState.CurrentCardCommands = CardCommands.Pause;
                mainForm.SetPauseSet(mainForm.gameConfig.FinishedOncePauseTime, CardCommands.DrawOnceFinished);

                DrawWhoWinThisTime();
            }
            else
            {
                mainForm.whoseOrder = 3;
                mainForm.currentState.CurrentCardCommands = CardCommands.WaitingForSend;
            }

            //
        }

        /// <summary>
        /// �ϼҳ���
        /// </summary>
        internal void DrawPreviousUserSendedCards()
        {
            mainForm.currentState.CurrentCardCommands = CardCommands.Undefined;
            //��PreviousUser������
            if (mainForm.currentSendCards[1].Count > 0) //����
            {
                DrawPreviousUserSendedCardsAction(Algorithm.MustSendedCards(mainForm, 3, mainForm.currentPokers, mainForm.currentSendCards, mainForm.currentState.Suit, mainForm.currentRank, mainForm.currentSendCards[mainForm.firstSend - 1].Count));
            }
            else
            {
                DrawPreviousUserSendedCardsAction(Algorithm.ShouldSendedCards(mainForm, 3, mainForm.currentPokers, mainForm.currentSendCards, mainForm.currentState.Suit, mainForm.currentRank));
            }


            //�����Ƿ��ס������
            //���Ѿ����ƣ�Ӧ�ý����ػ�
            int myCount = mainForm.currentSendCards[0].Count;
            if (myCount > 0)
            {
                int start = 285 - myCount * 7;
                Graphics g = Graphics.FromImage(mainForm.bmp);
                Rectangle rect = new Rectangle(start, 254, myCount * 14 + 57, 96);
                g.DrawImage(mainForm.image, rect, rect, GraphicsUnit.Pixel);
                for (int i = 0; i < myCount; i++)
                {
                    DrawMyImage(g, getPokerImageByNumber((int)mainForm.currentSendCards[0][i]), start, 244, 71, 96);
                    start += 14;
                }
                g.Dispose();
            }

            DrawScoreImage(mainForm.Scores);
            mainForm.Refresh();


            //
            if (mainForm.currentSendCards[0].Count > 0)
            {
                mainForm.currentState.CurrentCardCommands = CardCommands.Pause;
                mainForm.SetPauseSet(mainForm.gameConfig.FinishedOncePauseTime, CardCommands.DrawOnceFinished);

                DrawWhoWinThisTime();
            }
            else
            {
                mainForm.whoseOrder = 1;
                mainForm.currentState.CurrentCardCommands = CardCommands.WaitingForMySending;
            }


        }

        //��Ҷ�����һ���ƣ������÷ֶ��٣��´θ�˭����
        internal void DrawFinishedOnceSendedCards()
        {
            if (mainForm.currentPokers[0].Count == 0)
            {
                DrawFinishedSendedCards();
                return;
            }


            #region ����
            if (mainForm.gameConfig.IsDebug)
            {
                int f1 = mainForm.currentPokers[0].Count;
                int f2 = mainForm.currentPokers[1].Count;
                int f3 = mainForm.currentPokers[2].Count;
                int f4 = mainForm.currentPokers[3].Count;

                if (f1 != f2 || f2 != f3 || f3 != f4)
                {
                    int total = mainForm.currentSendCards[mainForm.firstSend - 1].Count;

                    int[] users = CommonMethods.OtherUsers(mainForm.firstSend);


                    if (mainForm.currentSendCards[users[0] - 1].Count != total)
                    {
                        for (int i = 0; i < mainForm.currentSendCards[users[0] - 1].Count; i++)
                        {
                            mainForm.pokerList[users[0] - 1].Add(mainForm.currentSendCards[users[0] - 1][i]);
                            mainForm.currentPokers[users[0] - 1].AddCard((int)mainForm.currentSendCards[users[0] - 1][i]);
                        }
                        mainForm.currentSendCards[users[0] - 1] = new ArrayList();
                        MustSendCardsAlgorithm.WhoseOrderIs2(mainForm, mainForm.currentPokers, users[0], mainForm.currentSendCards[users[0] - 1], 1, mainForm.currentState.Suit, mainForm.currentRank, CommonMethods.GetSuit((int)mainForm.currentSendCards[mainForm.firstSend - 1][0]));
                    }

                    if (mainForm.currentSendCards[users[1] - 1].Count != total)
                    {
                        for (int i = 0; i < mainForm.currentSendCards[users[1] - 1].Count; i++)
                        {
                            mainForm.pokerList[users[1] - 1].Add(mainForm.currentSendCards[users[1] - 1][i]);
                            mainForm.currentPokers[users[1] - 1].AddCard((int)mainForm.currentSendCards[users[1] - 1][i]);
                        }
                        mainForm.currentSendCards[users[1] - 1] = new ArrayList();
                        MustSendCardsAlgorithm.WhoseOrderIs3(mainForm, mainForm.currentPokers, users[1], mainForm.currentSendCards[users[1] - 1], 1, mainForm.currentState.Suit, mainForm.currentRank, CommonMethods.GetSuit((int)mainForm.currentSendCards[mainForm.firstSend - 1][0]));
                    }


                    if (mainForm.currentSendCards[users[2] - 1].Count != total)
                    {
                        for (int i = 0; i < mainForm.currentSendCards[users[2] - 1].Count; i++)
                        {
                            mainForm.pokerList[users[2] - 1].Add(mainForm.currentSendCards[users[2] - 1][i]);
                            mainForm.currentPokers[users[2] - 1].AddCard((int)mainForm.currentSendCards[users[2] - 1][i]);
                        }
                        mainForm.currentSendCards[users[2] - 1] = new ArrayList();
                        MustSendCardsAlgorithm.WhoseOrderIs4(mainForm, mainForm.currentPokers, users[2], mainForm.currentSendCards[users[2] - 1], 1, mainForm.currentState.Suit, mainForm.currentRank, CommonMethods.GetSuit((int)mainForm.currentSendCards[mainForm.firstSend - 1][0]));
                    }

                   
                }
            } 
            #endregion // ����
 


            //�����˭����
            mainForm.whoseOrder = TractorRules.GetNextOrder(mainForm);

            int newFirst = mainForm.whoseOrder;


            #region ����
            //if (mainForm.gameConfig.IsDebug) 
            if (1==0)
            {
                if (mainForm.whoIsBigger != newFirst && mainForm.currentSendCards[0].Count == 1)
                {
                    Console.WriteLine("*******************************************************");
                    Console.WriteLine("���ȳ���:" + mainForm.firstSend + ", ��ɫ=" + mainForm.currentState.Suit + ", Rank=" + mainForm.currentRank);
                    Console.WriteLine("�����������:" + mainForm.whoIsBigger + ", ���ռ���" + newFirst);

                    Console.WriteLine("");
                    Console.WriteLine("");
                    Console.WriteLine("�Լ�");
                    for (int i = 0; i < mainForm.pokerList[0].Count; i++)
                    {
                        Console.Write(mainForm.pokerList[0][i] + " ");
                    }
                    Console.WriteLine("");
                    for (int i = 0; i < mainForm.currentSendCards[0].Count; i++)
                    {
                        Console.Write(mainForm.currentSendCards[0][i] + " ");
                    }

                    Console.WriteLine("");
                    Console.WriteLine("�Լ�");
                    for (int i = 0; i < mainForm.pokerList[1].Count; i++)
                    {
                        Console.Write(mainForm.pokerList[1][i] + " ");
                    }
                    Console.WriteLine("");
                    for (int i = 0; i < mainForm.currentSendCards[1].Count; i++)
                    {
                        Console.Write(mainForm.currentSendCards[1][i] + " ");
                    }

                    Console.WriteLine("");
                    Console.WriteLine("����");
                    for (int i = 0; i < mainForm.pokerList[2].Count; i++)
                    {
                        Console.Write(mainForm.pokerList[2][i] + " ");
                    }
                    Console.WriteLine("");
                    for (int i = 0; i < mainForm.currentSendCards[2].Count; i++)
                    {
                        Console.Write(mainForm.currentSendCards[2][i] + " ");
                    }

                    Console.WriteLine("");
                    Console.WriteLine("����");
                    for (int i = 0; i < mainForm.pokerList[3].Count; i++)
                    {
                        Console.Write(mainForm.pokerList[3][i] + " ");
                    }
                    Console.WriteLine("");
                    for (int i = 0; i < mainForm.currentSendCards[3].Count; i++)
                    {
                        Console.Write(mainForm.currentSendCards[3][i] + " ");
                    }
                    Console.WriteLine("");
                    Console.WriteLine("*******************************************************");

                    //��ԭ
                    int[] users = CommonMethods.OtherUsers(mainForm.firstSend);

                    int tmp = mainForm.whoIsBigger;
                    if (mainForm.firstSend == tmp)
                    {
                        tmp = newFirst;
                    }

                    if (tmp == users[0])
                    {
                        mainForm.pokerList[users[0] - 1].Add(mainForm.currentSendCards[users[0] - 1][0]);
                        mainForm.currentPokers[users[0] - 1].AddCard((int)mainForm.currentSendCards[users[0] - 1][0]);
                        mainForm.currentSendCards[users[0] - 1] = new ArrayList();
                        MustSendCardsAlgorithm.WhoseOrderIs2(mainForm, mainForm.currentPokers, users[0], mainForm.currentSendCards[users[0] - 1], 1, mainForm.currentState.Suit, mainForm.currentRank, CommonMethods.GetSuit((int)mainForm.currentSendCards[mainForm.firstSend - 1][0]));
                    }
                    if (tmp == users[1])
                    {
                        mainForm.pokerList[users[1] - 1].Add(mainForm.currentSendCards[users[1] - 1][0]);
                        mainForm.currentPokers[users[1] - 1].AddCard((int)mainForm.currentSendCards[users[1] - 1][0]);
                        mainForm.currentSendCards[users[1] - 1] = new ArrayList();
                        MustSendCardsAlgorithm.WhoseOrderIs3(mainForm, mainForm.currentPokers, users[1], mainForm.currentSendCards[users[1] - 1], 1, mainForm.currentState.Suit, mainForm.currentRank, CommonMethods.GetSuit((int)mainForm.currentSendCards[mainForm.firstSend - 1][0]));
                    }
                    if (tmp == users[2])
                    {
                        mainForm.pokerList[users[2] - 1].Add(mainForm.currentSendCards[users[2] - 1][0]);
                        mainForm.currentPokers[users[2] - 1].AddCard((int)mainForm.currentSendCards[users[2] - 1][0]);
                        mainForm.currentSendCards[users[2] - 1] = new ArrayList();
                        MustSendCardsAlgorithm.WhoseOrderIs4(mainForm, mainForm.currentPokers, users[2], mainForm.currentSendCards[users[2] - 1], 1, mainForm.currentState.Suit, mainForm.currentRank, CommonMethods.GetSuit((int)mainForm.currentSendCards[mainForm.firstSend - 1][0]));
                    }
                    mainForm.timer.Stop();
                }
            }
            #endregion // ����




            mainForm.whoIsBigger = 0;


            mainForm.firstSend = mainForm.whoseOrder;
            bool success = false;
            if (((mainForm.currentState.Master == 1) || (mainForm.currentState.Master == 2)) && ((newFirst == 1) || (newFirst == 2)))
            {
                success = true;
            }
            if (((mainForm.currentState.Master == 3) || (mainForm.currentState.Master == 4)) && ((newFirst == 3) || (newFirst == 4)))
            {
                success = true;
            }


            if (!success)
            {
                TractorRules.CalculateScore(mainForm);

            }

            mainForm.currentSendCards[0] = new ArrayList(); 
            mainForm.currentSendCards[1] = new ArrayList(); 
            mainForm.currentSendCards[2] = new ArrayList(); 
            mainForm.currentSendCards[3] = new ArrayList(); 

            DrawCenterImage();
            DrawScoreImage(mainForm.Scores);
            mainForm.Refresh();



        }

        private void DrawWhoWinThisTime()
        {
            //˭Ӯ����һȦ
            int whoWin = TractorRules.GetNextOrder(mainForm);

            if (whoWin == 1) //��
            {
                Graphics g = Graphics.FromImage(mainForm.bmp);
                g.DrawImage(Properties.Resources.Winner, 437, 310, 33, 53);
                g.Dispose();
            }
            else if (whoWin == 2) //�Լ�
            {
                Graphics g = Graphics.FromImage(mainForm.bmp);
                g.DrawImage(Properties.Resources.Winner, 437, 120, 33, 53);
                g.Dispose();
            }
            else if (whoWin == 3) //����
            {
                Graphics g = Graphics.FromImage(mainForm.bmp);
                g.DrawImage(Properties.Resources.Winner, 90, 218, 33, 53);
                g.Dispose();
            }
            else if (whoWin == 4) //����
            {
                Graphics g = Graphics.FromImage(mainForm.bmp);
                g.DrawImage(Properties.Resources.Winner, 516, 218, 33, 53);
                g.Dispose();
            }

            mainForm.Refresh();
        }

        internal void DrawScoreImage【已迁移到 GdiRenderer.DrawScoreImage】
        (int scores)
        {
            Graphics g = Graphics.FromImage(mainForm.bmp);
            Bitmap bmp = global::Kuaff.Tractor.Properties.Resources.scores;
            Font font = new Font("����", 12, FontStyle.Bold);

            if (mainForm.currentState.Master == 2 || mainForm.currentState.Master == 4)
            {
                Rectangle rect = new Rectangle(490, 128, 56, 56);
                g.DrawImage(mainForm.image, rect, rect, GraphicsUnit.Pixel);
                g.DrawImage(bmp, rect);
                int x = 506;
                if (scores.ToString().Length ==2)
                {
                    x -= 4;
                }
                else if (scores.ToString().Length ==3)
                {
                    x -= 8;
                }
                g.DrawString(scores + "", font, Brushes.White, x, 138);
            }
            else
            {
                Rectangle rect = new Rectangle(85, 300, 56, 56);
                g.DrawImage(mainForm.image, rect, rect, GraphicsUnit.Pixel);
                g.DrawImage(bmp, rect);
                int x = 100;
                if (scores.ToString().Length == 2)
                {
                    x -= 4;
                }
                else if (scores.ToString().Length == 3)
                {
                    x -= 8;
                }
                g.DrawString(scores + "", font, Brushes.White, x, 310);
            }

            g.Dispose();
        }

        internal void DrawFinishedScoreImage()
        {
            Graphics g = Graphics.FromImage(mainForm.bmp);

            Pen pen = new Pen(Color.White, 2);
            g.FillRectangle(new SolidBrush(Color.FromArgb(100, Color.White)), 77, 124, 476, 244);
            g.DrawRectangle(pen, 77, 124, 476, 244);

            //������,��169��ʼ��
            for (int i = 0; i < 8; i++)
            {
                g.DrawImage(getPokerImageByNumber((int)mainForm.send8Cards[i]), 230 + i * 14, 130, 71, 96);
            }

            //��СѾ
            g.DrawImage(global::Kuaff.Tractor.Properties.Resources.Logo, 160, 237, 110, 112);

            //���÷�
            Font font = new Font("����", 16, FontStyle.Bold);
            g.DrawString("�ܵ÷� " + mainForm.Scores, font, Brushes.Blue, 310, 286);

            g.Dispose();
        }

        //��Ҷ������ƣ������÷ֶ��٣��´θ�˭����
        internal void DrawFinishedSendedCards()
        {
            mainForm.isNew = false;

           //����÷֣�ȷ����һ��ׯ�ң�ȷ����һ�ε�Rank
            TractorRules.GetNextMasterUser(mainForm);


            mainForm.currentSendCards[0] = new ArrayList(); 
            mainForm.currentSendCards[1] = new ArrayList(); 
            mainForm.currentSendCards[2] = new ArrayList(); 
            mainForm.currentSendCards[3] = new ArrayList(); 

            DrawCenterImage();
            DrawFinishedScoreImage();
            mainForm.Refresh();

            mainForm.SetPauseSet(mainForm.gameConfig.FinishedThisTime, CardCommands.DrawOnceRank);

           
        }
        #endregion // ���Ƹ��ҳ����ƣ�������������֪ͨ��һ��


        #region ����ʱ�ĸ�������

        //�����ƺŵõ���Ӧ���Ƶ�ͼƬ
        private Bitmap getPokerImageByNumber(int number)【已迁移到 GdiRenderer.GetPokerImageByNumber】
        
        {
            Bitmap bitmap = null;

            if (mainForm.gameConfig.CardImageName.Length == 0) //����Ƕ��ͼ���ж�ȡ
            {
                 bitmap = (Bitmap)mainForm.gameConfig.CardsResourceManager.GetObject("_" + number, Kuaff_Cards.Culture);
            }
            else
            {
                bitmap = mainForm.cardsImages[number]; //���Զ����ͼƬ�ж�ȡ
            }

            return bitmap;
        }

        /// <summary>
        /// �ػ����򱳾�
        /// </summary>
        /// <param name="g">������ͼ���Graphics</param>
        internal void DrawBackground【已迁移到 GdiRenderer.DrawBackground】
        (Graphics g)
        {
            //Bitmap image = global::Kuaff.Tractor.Properties.Resources.Backgroud;
            g.DrawImage(mainForm.image, mainForm.ClientRectangle, mainForm.ClientRectangle,GraphicsUnit.Pixel);
        }

        //�����ƶ��������м�֡�������ú���ȥ��
        private void DrawAnimatedCard(Bitmap card, int x, int y, int width, int height)
        {
            Graphics g = Graphics.FromImage(mainForm.bmp);
            Bitmap backup = mainForm.bmp.Clone(new Rectangle(x, y, width, height), PixelFormat.DontCare);
            g.DrawImage(card, x, y, width, height);
            mainForm.Refresh();
            g.DrawImage(backup, x, y, width, height);
            g.Dispose();
        }

        //��ͼ�ķ���
        private void DrawMyImage(Graphics g, Bitmap bmp, int x, int y, int width, int height)
        {
            g.DrawImage(bmp, x, y, width, height);
        }

        //���õ�ǰ���Ƶ���Ϣ
        private void SetCardsInformation(int x, int number, bool ready)
        {
            mainForm.myCardsLocation.Add(x);
            mainForm.myCardsNumber.Add(number);
            mainForm.myCardIsReady.Add(ready);
        }
        #endregion // ����ʱ�ĸ�������



        //���Է���
        internal void TestCards()
        {
            Graphics g = Graphics.FromImage(mainForm.bmp);

            int count = mainForm.pokerList[0].Count;
            Font font = new Font("����", 9);

            g.DrawString("�Լ���", font, Brushes.Red, 80, 130);
            g.DrawString("�Լң�", font, Brushes.Red, 80, 170);
            g.DrawString("���ң�", font, Brushes.Red, 80, 210);
            g.DrawString("���ң�", font, Brushes.Red, 80, 250);


            Console.Write("�Լ���");
            for (int i = 0; i < count; i++)
            {
                g.DrawString(mainForm.pokerList[0][i].ToString(), font, Brushes.Red, 120 + i * 15, 130);
                Console.Write(mainForm.pokerList[0][i].ToString() + ",");
            }
            Console.Write("\r\n�Լң�");
            count = mainForm.pokerList[1].Count;
            for (int i = 0; i < count; i++)
            {
                g.DrawString(mainForm.pokerList[1][i].ToString(), font, Brushes.Red, 120 + i * 15, 170);
                Console.Write(mainForm.pokerList[1][i].ToString() + ",");
            }
            Console.Write("\r\n���ң�");
            count = mainForm.pokerList[2].Count;
            for (int i = 0; i < count; i++)
            {
                g.DrawString(mainForm.pokerList[2][i].ToString(), font, Brushes.Red, 120 + i * 15, 210);
                Console.Write(mainForm.pokerList[2][i].ToString() + ",");
            }
            Console.Write("\r\n���ң�");
            count = mainForm.pokerList[3].Count;
            for (int i = 0; i < count; i++)
            {
                g.DrawString(mainForm.pokerList[3][i].ToString(), font, Brushes.Red, 120 + i * 15, 250);
                Console.Write(mainForm.pokerList[3][i].ToString() + ",");
            }

            mainForm.Refresh();
        }
    }
}
