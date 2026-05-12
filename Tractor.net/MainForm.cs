using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Resources;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Configuration;



using Kuaff.CardResouces;
using Kuaff.ModelResources;
using Kuaff.OperaResources;

using Kuaff.TractorFere;


namespace Kuaff.Tractor
{
    internal partial class MainForm : Form
    {
        #region ��������
        //������ͼ��
        internal Bitmap bmp = null;
        //ԭʼ����ͼƬ
        internal Bitmap image = null;
      

        //*״̬
        //��ǰ��״̬
        internal CurrentState currentState ;
        //��ǰ��Rank,������ǰ�ƾֵ�Rank,0����ʵ�ʵ��ƾ�2.....11����K,12����A,53��������
        internal int currentRank = 0;
        //�Ƿ����¿�ʼ����Ϸ
        internal bool isNew = true;

        //���ƵĴ���
        internal int showSuits = 0;
        //˭������
        internal int whoShowRank = 0;


        //*��������
        //�õ�һ�η��Ƶ�����,dpokerʱ���Ƶİ����࣬pokerList��ÿ�������е��Ƶ��б�
        internal DistributePokerHelper dpoker = null;
        internal ArrayList[] pokerList = null;

        //ÿ�������н����õ���
        internal CurrentPoker[] currentPokers = { new CurrentPoker(), new CurrentPoker(), new CurrentPoker(), new CurrentPoker() };
        //��ͼ�Ĵ��������ڷ���ʱʹ�ã�
        internal int currentCount = 0;
        //��ǰһ�ָ��ҵĳ������
        internal ArrayList[] currentSendCards = new ArrayList[4];
        //Ӧ��˭����
        internal int whoseOrder = 0;//0δ��,1�ң�2�Լң�3����,4����
        //һ�γ�����˭���ȿ�ʼ������
        internal int firstSend = 0;

        //*��������
        //��ǰ�����Ƶ�����
        internal ArrayList myCardsLocation = new ArrayList();
        //��ǰ�����Ƶ���ֵ
        internal ArrayList myCardsNumber = new ArrayList();
        //��ǰ�����Ƶ��Ƿ񱻵��
        internal ArrayList myCardIsReady = new ArrayList();
        //��ǰ�۵׵���
        internal ArrayList send8Cards = new ArrayList();

        //*���ҵ��Ƶĸ�������
        //����˳��
        internal int cardsOrderNumber = 0;

        //ȷ���������ߵ��ʱ��
        internal long sleepTime;
        internal long sleepMaxTime = 2000;
        internal CardCommands wakeupCardCommands;

        // ====== 步骤4：Engine + Renderer 实例 ======
        internal GameEngine engine = new GameEngine();
        internal GdiRenderer renderer;

        //*�滭������
        //DrawingForm����
        internal DrawingFormHelper drawingFormHelper = null;
        internal��CalculateRegionHelper calculateRegionHelper = null;

        //��¼���ε÷�
        internal int Scores = 0;

       
        //��Ϸ����
        internal GameConfig gameConfig = new GameConfig();

        //����ʱĿǰ��������һ��
        internal int whoIsBigger = 0;


        //�����ļ�
        private string musicFile = "";
        //����ͼ��
        internal Bitmap[] cardsImages = new Bitmap[54];

        //�����㷨
        internal object[] UserAlgorithms = { null, null, null, null };

        //��ǰһ���Ѿ�������
        internal CurrentPoker[] currentAllSendPokers = { new CurrentPoker(), new CurrentPoker(), new CurrentPoker(), new CurrentPoker() };

        #endregion // ��������

    
        internal MainForm()
        {
            InitializeComponent();
            SetStyle(ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.StandardDoubleClick, true);

           
            //��ȡ��������
            InitAppSetting();
            
            notifyIcon.Text = Text;
            BackgroundImage = image;
        
            //������ʼ��
            bmp = new Bitmap(ClientRectangle.Width, ClientRectangle.Height);
            
            
            drawingFormHelper = new DrawingFormHelper(this);
            calculateRegionHelper = new CalculateRegionHelper(this);
            renderer = new GdiRenderer(gameConfig);

            renderer.FallbackRender = (cmdType, payload, bmp) =>
            {
                switch (cmdType)
                {
                    case RenderCmdType.DrawCenter8:
                        drawingFormHelper.DrawCenter8Cards();
                        break;
                    case RenderCmdType.RedrawMyHand:
                        drawingFormHelper.DrawMySortedCards(currentPokers[0], currentPokers[0].Count);
                        break;
                    case RenderCmdType.DrawPlayedCards:
                        var playedP = (PlayedCardsPayload)payload;
                        if (playedP.PlayerId == 1) drawingFormHelper.DrawMyFinishSendedCards();
                        else if (playedP.PlayerId == 2) drawingFormHelper.DrawFrieldUserSendedCards();
                        else if (playedP.PlayerId == 3) drawingFormHelper.DrawPreviousUserSendedCards();
                        else if (playedP.PlayerId == 4) drawingFormHelper.DrawNextUserSendedCards();
                        break;
                    case RenderCmdType.ShowRoundWinner:
                        drawingFormHelper.DrawFinishedOnceSendedCards();
                        break;
                    case RenderCmdType.ShowRankResult:
                        drawingFormHelper.DrawFinishedScoreImage();
                        break;
                    case RenderCmdType.ShowBottomCards:
                        drawingFormHelper.DrawBottomCards(send8Cards);
                        break;
                }
            };
            renderer.BackgroundImage = image;


            for (int i = 0; i < 54; i++)
            {
                cardsImages[i] = null; //��ʼ��
            }
        }

        private void InitAppSetting()
        {
            //û�������ļ������config�ļ��ж�ȡ
            if (!File.Exists("gameConfig"))
            {
                AppSettingsReader reader = new AppSettingsReader();
                try
                {
                    Text = (String)reader.GetValue("title", typeof(String));
                }
                catch (Exception ex)
                {
                    Text = "��������ս";
                }

                try
                {
                    gameConfig.MustRank = (String)reader.GetValue("mustRank", typeof(String));
                }
                catch (Exception ex)
                {
                    gameConfig.MustRank = ",3,8,11,12,13,";
                }

                try
                {
                    gameConfig.IsDebug = (bool)reader.GetValue("debug", typeof(bool));
                }
                catch (Exception ex)
                {
                    gameConfig.IsDebug = false;
                }

                try
                {
                    gameConfig.BottomAlgorithm = (int)reader.GetValue("bottomAlgorithm", typeof(int));
                }
                catch (Exception ex)
                {
                    gameConfig.BottomAlgorithm = 1;
                }
            }
            else
            {
                //ʵ�ʴ�gameConfig�ļ��ж�ȡ
                Stream stream = null;
                try
                {
                    IFormatter formatter = new BinaryFormatter();
                    stream = new FileStream("gameConfig", FileMode.Open, FileAccess.Read, FileShare.Read);
                    gameConfig = (GameConfig)formatter.Deserialize(stream);
                    
                }
                catch (Exception ex)
                {
                    
                }
                finally
                {
                    if (stream != null)
                    {
                        stream.Close();
                    }
                }
            }

            //δ���л���ֵ
            AppSettingsReader myreader = new AppSettingsReader();
            gameConfig.CardsResourceManager = Kuaff_Cards.ResourceManager;
            try
            {
                String bkImage = (String)myreader.GetValue("backImage", typeof(String));
                image = new Bitmap(bkImage);
                KuaffToolStripMenuItem.CheckState = CheckState.Unchecked;

            }
            catch (Exception ex)
            {
                image = global::Kuaff.Tractor.Properties.Resources.Backgroud;
            }

            try
            {
                Text = (String)myreader.GetValue("title", typeof(String));
            }
            catch (Exception ex)
            {

            }

            gameConfig.CardImageName = "";

            if (gameConfig.IsDebug)
            {
                RobotToolStripMenuItem.CheckState = CheckState.Checked;
            }

        }



        #region �����¼���������

        internal void MenuItem_Click(object sender, EventArgs e)
        {

            ToolStripMenuItem menuItem = (ToolStripMenuItem)sender;
            if (menuItem.Text.Equals("�˳�"))
            {
                this.Close();
            }

            if (menuItem.Text.Equals("��ʼ����Ϸ"))
            {
                PauseGametoolStripMenuItem.Text = "��ͣ��Ϸ";


                //����Ϸ��ʼ״̬���ҼҺ͵з�����2��ʼ������Ϊ��ʼ����
                currentState = new CurrentState(0, 0, 0, 0,0,0,CardCommands.ReadyCards);
                currentRank = 0;

                isNew = true;
                whoIsBigger = 0;

                //��ʼ��
                init();

                //��ʼ��ʱ�������з���
                timer.Start();
            }

        }


        //��ʼ��
        internal void init()
        {
            //ÿ�γ�ʼ�����ػ汳��
            Graphics g = Graphics.FromImage(bmp);
            drawingFormHelper.DrawBackground(g);


            //��һ����
            dpoker = new DistributePokerHelper();
            pokerList = dpoker.Distribute();

            //ÿ�������е������,׼������
            currentPokers[0].Clear();
            currentPokers[1].Clear(); 
            currentPokers[2].Clear();
            currentPokers[3].Clear();

            //����ѷ��͵���
            currentAllSendPokers[0].Clear();
            currentAllSendPokers[1].Clear();
            currentAllSendPokers[2].Clear();
            currentAllSendPokers[3].Clear();


            //Ϊÿ���˵�currentPokers����Rank
            currentPokers[0].Rank = currentRank;
            currentPokers[1].Rank = currentRank;
            currentPokers[2].Rank = currentRank;
            currentPokers[3].Rank = currentRank;
            currentPokers[0].Suit = 0;
            currentPokers[1].Suit = 0;
            currentPokers[2].Suit = 0;
            currentPokers[3].Suit = 0;


            currentSendCards[0] = new ArrayList();
            currentSendCards[1] = new ArrayList();
            currentSendCards[2] = new ArrayList();
            currentSendCards[3] = new ArrayList();

            //
            myCardsLocation= new ArrayList();
            myCardsNumber= new ArrayList();
            myCardIsReady= new ArrayList();
            send8Cards= new ArrayList();


            //��������
            currentState.CurrentCardCommands = CardCommands.ReadyCards;
            currentState.Suit = 0;
        

            //���û�δ����,ѭ��25�ν��Ʒ���
            currentCount = 0;

            // 同步 Engine 数据
            engine.NewGame();
            engine.SetGameData(pokerList, currentPokers, currentSendCards, send8Cards, gameConfig, gameConfig.IsDebug);

            //
            showSuits = 0;showSuits = 0;
            whoShowRank = 0;

            //�÷�����
            Scores = 0;
            

            //����Sidebar
            drawingFormHelper.DrawSidebar(g);
            //���ƶ�������
            drawingFormHelper.DrawOtherMaster(g, 0, 0);
            
            if (currentState.Master != 0)
            {
                drawingFormHelper.DrawMaster(g, currentState.Master, 1);
                drawingFormHelper.DrawOtherMaster(g, currentState.Master, 1);
            }

            //����Rank
            drawingFormHelper.DrawRank(g,currentState.OurCurrentRank,true,false);
            drawingFormHelper.DrawRank(g, currentState.OpposedCurrentRank, false, false);

            //���ƻ�ɫ
            drawingFormHelper.DrawSuit(g, 0, true, false);
            drawingFormHelper.DrawSuit(g, 0, false, false);

            send8Cards = new ArrayList();
            //������ɫ
            if (currentRank == 53)
            {
                currentState.Suit = 5;
            }

            whoIsBigger = 0;

            //�����������Ϸ��ֹ����ֹͣ��Ϸ
            if (gameConfig.WhenFinished > 0)
            {
                
                bool b = false;

                if ((currentState.OurTotalRound + 1) > gameConfig.WhenFinished) 
                {
                    b = true;
                }
                if ((currentState.OpposedTotalRound + 1) > gameConfig.WhenFinished)
                {
                    b = true;
                }
                if (b)
                {
                    timer.Stop();
                    PauseGametoolStripMenuItem.Text = "������Ϸ";
                    PauseGametoolStripMenuItem.Image = Properties.Resources.MenuResume;
                }
            }
        }

       


        //���ڻ滭����,��������ͼ�񻭵�������
        private void MainForm_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            //��bmp����������
            g.DrawImage(bmp, 0, 0);
        }


       

            internal void MainForm_MouseClick_New(object sender, MouseEventArgs e)
        {
            if (((currentState.CurrentCardCommands == CardCommands.WaitingForMySending) || 
                 (currentState.CurrentCardCommands == CardCommands.WaitingForSending8Cards)) && 
                 (whoseOrder == 1))
            {
                // Handle card selection (select/deselect cards on click)
                HandleCardSelection(e);
                
                // Handle pig button click (confirm play)
                Rectangle pigRect = new Rectangle(296, 300, 53, 46);
                if (pigRect.Contains(e.Location))
                {
                    Graphics g = Graphics.FromImage(bmp);
                    g.DrawImage(image, pigRect, pigRect, GraphicsUnit.Pixel);
                    g.Dispose();
                    
                    // Collect selected card numbers
                    List<int> selected = new List<int>();
                    for (int i = 0; i < myCardIsReady.Count; i++)
                        if ((bool)myCardIsReady[i]) selected.Add((int)myCardsNumber[i]);
                    
                    bool hasValidSelection = false;
                    PlayResult result = null;
                    
                    if (currentState.CurrentCardCommands == CardCommands.WaitingForSending8Cards)
                    {
                        // 扣底牌 - 必须选8张
                        if (selected.Count == 8)
                        {
                            result = engine.PlayerSend8Cards(selected);
                            hasValidSelection = true;
                        }
                    }
                    else if (currentState.CurrentCardCommands == CardCommands.WaitingForMySending)
                    {
                        // 出牌 - 至少选1张
                        if (selected.Count > 0)
                        {
                            result = engine.PlayerPlayCard(1, selected);
                            hasValidSelection = true;
                        }
                    }
                    
                    if (hasValidSelection && result != null)
                    {
                        currentState = engine.State;
                        whoseOrder = engine.WhoseOrder;
                        firstSend = engine.FirstSend;
                        whoIsBigger = engine.WhoIsBigger;
                        
                        foreach (var cmd in result.RenderCommands)
                            renderer.Execute(cmd, bmp, currentState);
                        
                        if (currentState.CurrentCardCommands == CardCommands.DrawMySortedCards)
                        {
                            drawingFormHelper.DrawMySortedCards(currentPokers[0], currentPokers[0].Count);
                        }
                        Refresh();
                    }
                }
            }
            else if (currentState.CurrentCardCommands == CardCommands.ReadyCards)
            {
                // Do nothing during dealing
            }
        }
        
        private void HandleCardSelection(MouseEventArgs e)
        {
            // Original click handling logic for selecting/deselecting cards
            // Check if click is in player's card area
            if (myCardsLocation.Count > 0 &&
                e.X >= (int)myCardsLocation[0] && 
                e.X <= ((int)myCardsLocation[myCardsLocation.Count - 1] + 71) && 
                e.Y >= 355 && e.Y < 472)
            {
                drawingFormHelper.DrawMyPlayingCards(currentPokers[0]);
                Refresh();
            }
        }
        
        private void MainForm_MouseClick(object sender, MouseEventArgs e)
        {
            MainForm_MouseClick_New(sender, e);
        }

       
        private void MainForm_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            //if (e.Button == MouseButtons.Right)
            //    return;

            //�����ǰû���ƿɳ� 
            if (currentPokers[0].Count == 0)
            {
                return;
            }

            bool  b = calculateRegionHelper.CalculateDoubleClickedRegion(e);
            if (!b)
            {
                return;
            }

            currentSendCards[0]= new ArrayList();


            //���ƣ����Բ�ȥС��
            Rectangle pigRect = new Rectangle(296, 300, 53, 46);
            Graphics g = Graphics.FromImage(bmp);
            g.DrawImage(image, pigRect, pigRect, GraphicsUnit.Pixel);
           
           

            //���ƻ��ǳ���
            if ((currentState.CurrentCardCommands == CardCommands.WaitingForSending8Cards) && (whoseOrder == 1)) //������ҿ���
            {
                ArrayList readyCards = new ArrayList();
                for (int i = 0; i < myCardIsReady.Count; i++)
                {
                    if ((bool)myCardIsReady[i])
                    {
                        readyCards.Add((int)myCardsNumber[i]);
                    }
                }

                if (readyCards.Count == 8)
                {
                    send8Cards = new ArrayList();
                    for (int i = 0; i < 8; i++)
                    {
                        CommonMethods.SendCards(send8Cards, currentPokers[0], pokerList[0], (int)readyCards[i]);
                    }
                    initSendedCards();
                    currentState.CurrentCardCommands = CardCommands.DrawMySortedCards;
                }


            }
            else if (currentState.CurrentCardCommands == CardCommands.WaitingForMySending) //������ҷ���
            {
               

                if (TractorRules.IsInvalid(this, currentSendCards, 1))
                {
                    if (firstSend == 1)
                    {
                        whoIsBigger = 1;

                        ArrayList minCards = new ArrayList();
                        if (TractorRules.CheckSendCards(this, minCards,0))
                        {
                            currentSendCards[0] = new ArrayList(); 
                            for (int i = 0; i < myCardIsReady.Count; i++)
                            {
                                if ((bool)myCardIsReady[i])
                                {
                                    CommonMethods.SendCards(currentSendCards[0], currentPokers[0], pokerList[0], (int)myCardsNumber[i]);
                                }
                            }

                        }
                        else
                        {
                            for (int i = 0; i < minCards.Count; i++)
                            {
                                CommonMethods.SendCards(currentSendCards[0], currentPokers[0], pokerList[0], (int)minCards[i]);
                            }
                           
                        }


                    }
                    else
                    {
                        currentSendCards[0] = new ArrayList(); 
                        for (int i = 0; i < myCardIsReady.Count; i++)
                        {
                            if ((bool)myCardIsReady[i])
                            {
                                CommonMethods.SendCards(currentSendCards[0], currentPokers[0], pokerList[0], (int)myCardsNumber[i]);
                            }
                        }
                    }


                    drawingFormHelper.DrawMyFinishSendedCards();
                }
            }


        }

        //��ʼ��ÿ���˳�����
        internal void initSendedCards()
        {
            //���½���ÿ�������е���
            currentPokers[0] = CommonMethods.parse(pokerList[0], currentState.Suit, currentRank);
            currentPokers[1] = CommonMethods.parse(pokerList[1], currentState.Suit, currentRank);
            currentPokers[2] = CommonMethods.parse(pokerList[2], currentState.Suit, currentRank);
            currentPokers[3] = CommonMethods.parse(pokerList[3], currentState.Suit, currentRank);
        }


        #endregion // �����¼���������


        //��ʱ��,������ʾ����ʱ�Ķ���
        internal void timer_Tick(object sender, EventArgs e)
        {

            if (musicFile.Length > 0 && (!MciSoundPlayer.IsPlaying()) && PlayMusicToolStripMenuItem.Checked)
            {
                MciSoundPlayer.Stop();
                MciSoundPlayer.Close();
                MciSoundPlayer.Play(musicFile,"song");
            }
            else if (musicFile.Length > 0 && (!MciSoundPlayer.IsPlaying()) && RandomPlayToolStripMenuItem.Checked)
            {
                PlayRandomSongs();
            }
            //1.���� — 通过 Engine 处理
            if (currentState.CurrentCardCommands == CardCommands.ReadyCards)
            {
                TickResult tickResult = engine.Tick(DateTime.Now.Ticks);
                if (tickResult.StateChanged)
                {
                    currentState = engine.State;
                }
                foreach (var cmd in tickResult.RenderCommands)
                {
                    if (cmd.Type == RenderCmdType.ShowToolbar)
                    {
                        drawingFormHelper.DrawToolbar();
                    }
                    else if (cmd.Type == RenderCmdType.DealCard)
                    {
                        var payload = (DealCardPayload)cmd.Payload;
                        // Engine 已完成 card distribution data work
                        // DrawingFormHelper 只做渲染（无 AddCard / DoRankOrNot）
                        drawingFormHelper.RenderDealRound(payload.Round);
                    }
                }
                // 同步 currentCount（虽然 Engine 已管理，保留兼容）
                currentCount = engine.DealCount;
            }
            else if (currentState.CurrentCardCommands == CardCommands.WaitingShowBottom) //��������Ϻ����������
            {
                drawingFormHelper.DrawCenterImage();
                //��8���Ƶı���
                Graphics g = Graphics.FromImage(bmp);

                for (int i = 0; i < 8; i++)
                {
                    g.DrawImage(gameConfig.BackImage, 200 + i * 2, 186, 71, 96);
                }

                SetPauseSet(gameConfig.Get8CardsTime, CardCommands.DrawCenter8Cards);

            }
                        else if (currentState.CurrentCardCommands == CardCommands.DrawCenter8Cards) //通过 Engine 处理
            {
                TickResult tickResult = engine.Tick(DateTime.Now.Ticks);
                if (tickResult.StateChanged)
                {
                    currentState = engine.State;
                }
                foreach (var cmd in tickResult.RenderCommands)
                {
                    if (cmd.Type == RenderCmdType.DrawCenter8)
                    {
                        // 复杂的叫主/定主逻辑暂时留在 MainForm
                        if (drawingFormHelper.DoRankNot())
                        {
                            if (gameConfig.IsPass)
                            {
                                init();
                                isNew = false;
                                drawingFormHelper.DrawPassImage();
                                SetPauseSet(gameConfig.NoRankPauseTime, CardCommands.WaitingShowPass);
                                return;
                            }
                            else
                            {
                                ArrayList bottom = new ArrayList();
                                bottom.Add(pokerList[0][0]);
                                bottom.Add(pokerList[0][1]);
                                bottom.Add(pokerList[1][0]);
                                bottom.Add(pokerList[1][1]);
                                bottom.Add(pokerList[2][0]);
                                bottom.Add(pokerList[2][1]);
                                bottom.Add(pokerList[3][0]);
                                bottom.Add(pokerList[3][1]);
                                int suit = CommonMethods.GetSuit((int)bottom[2]);
                                currentState.Suit = suit;

                                Graphics g = Graphics.FromImage(bmp);
                                if (currentState.Master == 1 || currentState.Master == 2)
                                    drawingFormHelper.DrawSuit(g, suit, true, true);
                                else if (currentState.Master == 3 || currentState.Master == 4)
                                    drawingFormHelper.DrawSuit(g, suit, false, true);
                                g.Dispose();

                                drawingFormHelper.DrawCenterImage();
                                drawingFormHelper.DrawBottomCards(bottom);
                                SetPauseSet(gameConfig.NoRankPauseTime, CardCommands.WaitingShowBottom);
                                return;
                            }
                        }

                        whoseOrder = currentState.Master;
                        firstSend = whoseOrder;
                        SetPauseSet(gameConfig.Get8CardsTime, CardCommands.DrawMySortedCards);
                        drawingFormHelper.DrawCenter8Cards();
                        initSendedCards();
                        drawingFormHelper.DrawMySortedCards(currentPokers[0], currentPokers[0].Count);
                        currentState.CurrentCardCommands = CardCommands.WaitingForSending8Cards;
                        drawingFormHelper.DrawScoreImage(0);
                    }
                }
            }else if (currentState.CurrentCardCommands == CardCommands.WaitingShowPass) //通过 Engine 处理
            {
                TickResult tickResult = engine.Tick(DateTime.Now.Ticks);
                if (tickResult.StateChanged)
                {
                    currentState = engine.State;
                }
                foreach (var cmd in tickResult.RenderCommands)
                {
                    renderer.Execute(cmd, bmp, currentState);
                }
                if (currentState.CurrentCardCommands == CardCommands.ReadyCards)
                {
                    drawingFormHelper.DrawCenterImage();
                    Refresh();
                }
            }
                        else if (currentState.CurrentCardCommands == CardCommands.WaitingForSending8Cards) //通过 Engine 处理
            {
                TickResult tickResult = engine.Tick(DateTime.Now.Ticks);
                if (tickResult.StateChanged)
                {
                    currentState = engine.State;
                    engine.SyncOrder(whoseOrder, firstSend, whoIsBigger);
                }
                foreach (var cmd in tickResult.RenderCommands)
                {
                    if (cmd.Type == RenderCmdType.WaitingForPlayerAction)
                    {
                        var payload = (WaitPayload)cmd.Payload;
                        if (payload.WaitingMode == "AutoSend8Cards")
                        {
                            Algorithm.Send8Cards(this, payload.PlayerId);
                        }
                        else if (payload.WaitingMode == "Send8Cards")
                        {
                            drawingFormHelper.DrawMyPlayingCards(currentPokers[0]);
                            Refresh();
                            return;
                        }
                    }
                }
            }else if (currentState.CurrentCardCommands == CardCommands.DrawMySortedCards) //4.���ҵ���
            {

                //������Լ����ƽ���������ʾ
                SetPauseSet(gameConfig.SortCardsTime, CardCommands.DrawMySortedCards);

                drawingFormHelper.DrawMySortedCards(currentPokers[0], currentPokers[0].Count);
                Refresh();

                currentState.CurrentCardCommands = CardCommands.WaitingForSend;

            }
                        else if (currentState.CurrentCardCommands == CardCommands.WaitingForSend) //通过 Engine 处理
            {
                engine.SyncOrder(whoseOrder, firstSend, whoIsBigger);
                TickResult tickResult = engine.Tick(DateTime.Now.Ticks);
                if (tickResult.StateChanged)
                {
                    currentState = engine.State;
                }
                bool needsRender = true;
                foreach (var cmd in tickResult.RenderCommands)
                {
                    if (cmd.Type == RenderCmdType.AiPlayCard)
                    {
                        var payload = (AiPlayPayload)cmd.Payload;
                        int pid = payload.PlayerId;
                        // 调用旧 DrawingFormHelper 的出牌方法（过渡期）
                        if (pid == 2) drawingFormHelper.DrawFrieldUserSendedCards();
                        else if (pid == 3) drawingFormHelper.DrawPreviousUserSendedCards();
                        else if (pid == 4) drawingFormHelper.DrawNextUserSendedCards();
                        else if (pid == 1) // debug AI
                        {
                            if (firstSend == 1)
                                Algorithm.ShouldSendedCards(this, 1, currentPokers, currentSendCards, currentState.Suit, currentRank);
                            else
                                Algorithm.MustSendedCards(this, 1, currentPokers, currentSendCards, currentState.Suit, currentRank, currentSendCards[firstSend - 1].Count);
                            drawingFormHelper.DrawMyFinishSendedCards();
                        }
                        needsRender = false;
                    }
                }
                if (needsRender)
                {
                    Refresh();
                }
            }else if (currentState.CurrentCardCommands == CardCommands.Pause) //通过 Engine 处理暂停
            {
                TickResult tickResult = engine.Tick(DateTime.Now.Ticks);
                if (tickResult.StateChanged)
                {
                    currentState = engine.State;
                }
            }
            else if (currentState.CurrentCardCommands == CardCommands.DrawOnceFinished) //����Ǵ�Ҷ�������
            {
                drawingFormHelper.DrawFinishedOnceSendedCards(); //�����������
                if (currentPokers[0].Count > 0)
                {
                    currentState.CurrentCardCommands = CardCommands.WaitingForSend;
                }
            }
            else if (currentState.CurrentCardCommands == CardCommands.DrawOnceRank) //������ִ�Ҷ�������
            {
                currentState.CurrentCardCommands = CardCommands.Undefined;
                init();
            }
        }

        //������ͣ�����ʱ�䣬�Լ���ͣ�������ִ������
        internal void SetPauseSet(int max, CardCommands wakeup)
        {
            // 过渡期：Engine 和 MainForm 双写暂停状态
            engine.SetPause(max, wakeup);
            sleepMaxTime = max;
            sleepTime = DateTime.Now.Ticks;
            wakeupCardCommands = wakeup;
            currentState.CurrentCardCommands = CardCommands.Pause;
        }


        #region �˵��¼�����
        //����ͼ��
        private void SelectCardImage_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem menuItem = (ToolStripMenuItem)sender;

            if (menuItem.Text.Equals("��ͨͼ��"))
            {
                gameConfig.CardsResourceManager = Kuaff_Cards.ResourceManager;
                CommonToolStripMenuItem.CheckState = CheckState.Checked;
                ModelToolStripMenuItem.CheckState = CheckState.Unchecked;
                OperaToolStripMenuItem.CheckState = CheckState.Unchecked;
                CustomCardImageToolStripMenuItem.CheckState = CheckState.Unchecked;

                CustomCardImageToolStripMenuItem.Text = "�Զ���";
                gameConfig.CardImageName = "";

            }
            else if (menuItem.Text.Equals("�㳵��Ů"))
            {
                gameConfig.CardsResourceManager = Kuaff_Model.ResourceManager;
                CommonToolStripMenuItem.CheckState = CheckState.Unchecked;
                ModelToolStripMenuItem.CheckState = CheckState.Checked;
                OperaToolStripMenuItem.CheckState = CheckState.Unchecked;
                CustomCardImageToolStripMenuItem.CheckState = CheckState.Unchecked;

                CustomCardImageToolStripMenuItem.Text = "�Զ���";
                gameConfig.CardImageName = "";
            }
            else if (menuItem.Text.Equals("��������"))
            {
                gameConfig.CardsResourceManager = Kuaff_Opera.ResourceManager;
                CommonToolStripMenuItem.CheckState = CheckState.Unchecked;
                ModelToolStripMenuItem.CheckState = CheckState.Unchecked;
                OperaToolStripMenuItem.CheckState = CheckState.Checked;
                CustomCardImageToolStripMenuItem.CheckState = CheckState.Unchecked;

                CustomCardImageToolStripMenuItem.Text = "�Զ���";
                gameConfig.CardImageName = "";
            }
            else if (menuItem.Text.StartsWith("�Զ���"))
            {
                SelectCardsImage sci = new SelectCardsImage(this);
                if (sci.ShowDialog(this) == DialogResult.OK)
                {
                    gameConfig.CardImageName = sci.CardsName;
                    menuItem.Text = "�Զ���--" + gameConfig.CardImageName;

                    CommonToolStripMenuItem.CheckState = CheckState.Unchecked;
                    ModelToolStripMenuItem.CheckState = CheckState.Unchecked;
                    OperaToolStripMenuItem.CheckState = CheckState.Unchecked;
                    CustomCardImageToolStripMenuItem.CheckState = CheckState.Checked;
                }
            }
        }
        //�Ʊ�ͼƬ
        private void SelectBackImage_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem menuItem = (ToolStripMenuItem)sender;

            if (menuItem.Text.Equals("ε������"))
            {
                gameConfig.BackImage = Kuaff_Cards.back;
                BlueWorldToolStripMenuItem.CheckState = CheckState.Checked;
                GreenAgeToolStripMenuItem.CheckState = CheckState.Unchecked;
                AntelopeToolStripMenuItem.CheckState = CheckState.Unchecked;

                CustomBackImageToolStripMenuItem.CheckState = CheckState.Unchecked;
                CustomBackImageToolStripMenuItem.Text = "�Զ���";
            }
            else if (menuItem.Text.Equals("��ɬ�껪"))
            {
                gameConfig.BackImage = Kuaff_Cards.back2;
                BlueWorldToolStripMenuItem.CheckState = CheckState.Unchecked;
                GreenAgeToolStripMenuItem.CheckState = CheckState.Checked;
                AntelopeToolStripMenuItem.CheckState = CheckState.Unchecked;

                CustomBackImageToolStripMenuItem.CheckState = CheckState.Unchecked;
                CustomBackImageToolStripMenuItem.Text = "�Զ���";
            }
            else if (menuItem.Text.Equals("��ԭ����"))
            {
                gameConfig.BackImage = Kuaff_Cards.back3;
                BlueWorldToolStripMenuItem.CheckState = CheckState.Unchecked;
                GreenAgeToolStripMenuItem.CheckState = CheckState.Unchecked;
                AntelopeToolStripMenuItem.CheckState = CheckState.Checked;

                CustomBackImageToolStripMenuItem.CheckState = CheckState.Unchecked;
                CustomBackImageToolStripMenuItem.Text = "�Զ���";
            }
            else if (menuItem.Text.StartsWith("�Զ���"))
            {
                SelectCardbackImage sci = new SelectCardbackImage(this);
                if (sci.ShowDialog(this) == DialogResult.OK)
                {
                    menuItem.Text = "�Զ���--" + sci.CardBackImageName;

                    BlueWorldToolStripMenuItem.CheckState = CheckState.Unchecked;
                    GreenAgeToolStripMenuItem.CheckState = CheckState.Unchecked;
                    AntelopeToolStripMenuItem.CheckState = CheckState.Unchecked;
                    CustomBackImageToolStripMenuItem.CheckState = CheckState.Checked;
                }

            }
        }

        //ѡ�񱳾�ͼƬ
        private void SelectImage_Click(object sender, EventArgs e)
        {
            PauseGametoolStripMenuItem.Text = "��ͣ��Ϸ";

            ToolStripMenuItem menuItem = (ToolStripMenuItem)sender;
            if (menuItem.Text.Equals("�丸�Ƽ�"))
            {
                KuaffToolStripMenuItem.CheckState = CheckState.Checked;
                image = global::Kuaff.Tractor.Properties.Resources.Backgroud;
                BackgroundImage = image;

                Graphics g = Graphics.FromImage(bmp);
                g.DrawImage(image, ClientRectangle, ClientRectangle,GraphicsUnit.Pixel);

                init();
                //���ƶ�������

                drawingFormHelper.DrawOtherMaster(g, 0, 0);
              
                if (isNew && (currentRank == 0))
                {
                }
                else
                {
                    if (currentState.Master != 0)
                    {
                        drawingFormHelper.DrawMaster(g, currentState.Master, 1);
                        drawingFormHelper.DrawOtherMaster(g, currentState.Master, 1);
                    }
                }
                g.Dispose();
                Refresh();
            }
            else if (menuItem.Text.Equals("�Զ���ͼƬ"))
            {
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    KuaffToolStripMenuItem.CheckState = CheckState.Unchecked;
                    image = new Bitmap(openFileDialog.OpenFile());
                    image = new Bitmap(image,new Size(ClientRectangle.Width,ClientRectangle.Height));
                    //BackgroundImage = image;

                    Graphics g = Graphics.FromImage(bmp);
                    g.DrawImage(image, ClientRectangle, ClientRectangle, GraphicsUnit.Pixel);
                   
                    init();
                    //���ƶ�������

                    drawingFormHelper.DrawOtherMaster(g, 0, 0);
                   
                    if (isNew && (currentRank == 0))
                    {
                    }
                    else
                    {
                        if (currentState.Master != 0)
                        {
                            drawingFormHelper.DrawMaster(g, currentState.Master, 1);
                            drawingFormHelper.DrawOtherMaster(g, currentState.Master, 1);
                        }
                    }
                    g.Dispose();
                    Refresh();
                }
                
            }

        }

        //�����¼�����
        private void notifyIcon_MouseClick(object sender, MouseEventArgs e)
        {
            this.Show();
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.WindowState = FormWindowState.Normal;
            }
            this.Activate();
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.Visible = false;
                notifyIcon.Visible = true;
            }
            else
            {
                notifyIcon.Visible = false;
            }
        }

        //������Ϸ�ٶ�
        private void GameSpeedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetSpeedDialog dialog = new SetSpeedDialog(this);
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                //�����ٶ�
                gameConfig.FinishedOncePauseTime = (int)(150 * Math.Pow(10, dialog.trackBar1.Value / 25.0));
                gameConfig.NoRankPauseTime = (int)(500 * Math.Pow(10, dialog.trackBar2.Value / 25.0));
                gameConfig.Get8CardsTime = (int)(100 * Math.Pow(10, dialog.trackBar3.Value / 25.0));
                gameConfig.SortCardsTime = (int)(100 * Math.Pow(10, dialog.trackBar4.Value / 25.0));
                gameConfig.FinishedThisTime = (int)(250 * Math.Pow(10, dialog.trackBar5.Value / 25.0));
                gameConfig.TimerDiDa = (int)(10 * Math.Pow(10, dialog.trackBar6.Value / 25.0));
                timer.Interval = gameConfig.TimerDiDa;
            }
        }

        //�����ƾ�
        private void SaveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Stream stream = null;
            try
            {
                IFormatter formatter = new BinaryFormatter();
                stream = new FileStream("backup", FileMode.Create, FileAccess.Write, FileShare.None);
                formatter.Serialize(stream, currentState);
            }
            catch (Exception ex)
            {

            }
            finally
            {
                if (stream != null)
                {
                    stream.Close();
                }
            }
        }

        //��ȡ�ƾ�
        private void RestoreToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PauseGametoolStripMenuItem.Text = "��ͣ��Ϸ";

            Stream stream = null;
            try
            {
                IFormatter formatter = new BinaryFormatter();
                stream = new FileStream("backup", FileMode.Open, FileAccess.Read, FileShare.Read);
                CurrentState cs = (CurrentState)formatter.Deserialize(stream);
                
                currentState = cs;

               
                if (currentState.Master == 1 || currentState.Master == 2)
                {
                    currentRank = currentState.OurCurrentRank;
                }
                else if(currentState.Master == 3 || currentState.Master == 4)
                {
                    currentRank = currentState.OpposedCurrentRank;
                }
                else
                {
                    isNew = true;
                    currentRank = 0;
                }

                init();

                timer.Start();
                

            }
            catch (Exception ex)
            {
            }
            finally
            {
                if (stream != null)
                {
                    stream.Close();
                }
            }
        }

        //��ʾ����
        private void GameHelpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Help.ShowHelp(this,"Tractor.CHM");
        }

        //Aboutme
        private void AboutMeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            About about = new About();
            about.Show(this);
        }

        private void PauseGametoolStripMenuItem_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem menuItem = (ToolStripMenuItem)sender;
            if (menuItem.Text.Equals("��ͣ��Ϸ"))
            {
                timer.Stop();
                menuItem.Text = "������Ϸ";
                menuItem.Image = Properties.Resources.MenuResume;
            }
            else
            {
                timer.Start();
                menuItem.Text = "��ͣ��Ϸ";
                menuItem.Image = Properties.Resources.MenuPause;
            }
        }

        private void RobotToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem menuItem = (ToolStripMenuItem)sender;

            if (menuItem.CheckState == CheckState.Checked)
            {
                gameConfig.IsDebug = true;
            }
            else
            {
                gameConfig.IsDebug = false;
            }
        }

        private void SetRulesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetRules sr = new SetRules(this);
            sr.ShowDialog(this);
        }

        private void NoBackMusicToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //
            ToolStripMenuItem menuItem = (ToolStripMenuItem)sender;
            menuItem.CheckState = CheckState.Checked;
            
        }

        private void PlayMusicToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //������������ѡ��Ի���
            SelectMusic sem = new SelectMusic();
            if (sem.ShowDialog(this) == DialogResult.OK)
            {
                NoBackMusicToolStripMenuItem.CheckState = CheckState.Unchecked;

                //���ѡ����һ�����ӣ��򲥷�
                try
                {
                    string music = (string)sem.music.SelectedItem;
                    String newMusicFile = Path.Combine(Application.StartupPath, "music\\" + music);
                    if (musicFile != newMusicFile && musicFile.Length > 0)
                    {
                        MciSoundPlayer.Stop();
                        MciSoundPlayer.Close();
                    }
                    musicFile = newMusicFile;
                    MciSoundPlayer.Play(musicFile,"song");

                    NoBackMusicToolStripMenuItem.CheckState = CheckState.Unchecked;
                    PlayMusicToolStripMenuItem.CheckState = CheckState.Checked;
                    RandomPlayToolStripMenuItem.CheckState = CheckState.Unchecked;
                }
                catch (Exception ex)
                {

                }
            }
            else
            {
                //NoBackMusicToolStripMenuItem.CheckState = CheckState.Checked;

            }
        }

        private void NoBackMusicToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            musicFile = "";
            MciSoundPlayer.Stop();
            MciSoundPlayer.Close();
            NoBackMusicToolStripMenuItem.CheckState = CheckState.Checked;
            RandomPlayToolStripMenuItem.CheckState = CheckState.Unchecked;
            PlayMusicToolStripMenuItem.CheckState = CheckState.Unchecked;
        }

        private void RandomPlayToolStripMenuItem_Click(object sender, EventArgs e)
        {

            PlayRandomSongs();

            NoBackMusicToolStripMenuItem.CheckState = CheckState.Unchecked;
            RandomPlayToolStripMenuItem.CheckState = CheckState.Checked;
            PlayMusicToolStripMenuItem.CheckState = CheckState.Unchecked;
        }

        //�����������
        private void PlayRandomSongs()
        {
            try
            {
                SelectMusic sem = new SelectMusic();
                int count = sem.music.Items.Count;
                Random random = new Random();
                string music = (string)sem.music.Items[random.Next(count)];
                sem.Dispose();
                String newMusicFile = Path.Combine(Application.StartupPath, "music\\" + music);
                if (musicFile != newMusicFile && musicFile.Length > 0)
                {
                    MciSoundPlayer.Stop();
                    MciSoundPlayer.Close();
                }
                musicFile = newMusicFile;
                MciSoundPlayer.Play(musicFile, "song");

            }
            catch (Exception ex)
            {

            }
        }

        private void FereToolStripMenuItem_Click(object sender, EventArgs e) //����������
        {
            Fere fere = new Fere();
            fere.Show(this);
        }

        private void SeeTotalScoresToolStripMenuItem_Click(object sender, EventArgs e) //�÷�ͳ��
        {
            TotalScores ts = new TotalScores(this);
            ts.Show(this);
        }

        private void SelectAlgorithmToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SelectUserAlgorithm sua = new SelectUserAlgorithm(this);
            sua.ShowDialog(this);
        }

        #endregion // �˵��¼�����

        private void SetGameFinishedtoolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetGameFinished sgf = new SetGameFinished(this);
            sgf.ShowDialog(this);
        }

        

               
    }
}