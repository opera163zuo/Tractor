﻿using System;
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
        #region 变量声明
        //缓冲区图像
        internal Bitmap bmp = null;
        //原始背景图片
        internal Bitmap image = null;


        //*状态
        //当前的状态
        internal CurrentState currentState ;
        //当前的Rank,代表当前牌局的Rank,0代表实际的牌局2.....11代表K,12代表A,53代表打王
        internal int currentRank = 0;
        //是否是新开始的游戏
        internal bool isNew = true;

        //亮牌的次数
        internal int showSuits = 0;
        //璋佷寒鐨勭墝
        internal int whoShowRank = 0;


        //*发牌序列
        //得到一次发牌的序列,dpoker时发牌的帮助类，pokerList是每个人手中的牌的列表
        internal DistributePokerHelper dpoker = null;
        internal ArrayList[] pokerList = null;

        //每个人手中解析好的牌
        internal CurrentPoker[] currentPokers = { new CurrentPoker(), new CurrentPoker(), new CurrentPoker(), new CurrentPoker() };
        //画图的次数（仅在发牌时使用）
        internal int currentCount = 0;
        //当前一轮各家的出牌情况
        internal ArrayList[] currentSendCards = new ArrayList[4];
        //应该谁出牌
        internal int whoseOrder = 0;//0未定,1我，2对家，3西家,4东家
        //一次出来中谁最先开始出的牌
        internal int firstSend = 0;

        //*辅助变量
        //褰撳墠鎵嬩腑鐗岀殑鍧愭爣
        internal ArrayList myCardsLocation = new ArrayList();
        //当前手中牌的数值
        internal ArrayList myCardsNumber = new ArrayList();
        //当前手中牌的是否被点出
        internal ArrayList myCardIsReady = new ArrayList();
        //褰撳墠鎵ｅ簳鐨勭墝
        internal ArrayList send8Cards = new ArrayList();

        //*画我的牌的辅助变量
        //鐢荤墝椤哄簭
        internal int cardsOrderNumber = 0;

        //确定程序休眠的最长时间
        internal long sleepTime;
        internal long sleepMaxTime = 2000;
        internal CardCommands wakeupCardCommands;

        internal GameEngine engine = new GameEngine();

        internal GameState _gameState;

        private void SyncFromGameState(GameState newState)
        {
            if (newState == null) return;
            _gameState = newState;
            currentState = newState.State;
            pokerList = newState.PokerLists;
            currentPokers = newState.CurrentPokers;
            currentSendCards = newState.CurrentSendCards;
            currentAllSendPokers = newState.CurrentAllSendPokers;
            send8Cards = newState.Send8Cards;
            whoseOrder = newState.WhoseOrder;
            firstSend = newState.FirstSend;
            whoIsBigger = newState.WhoIsBigger;
            currentRank = newState.CurrentRank;
            Scores = newState.Scores;
            currentCount = newState.DealCount;
            showSuits = newState.ShowSuits;
            whoShowRank = newState.WhoShowRank;
            if (renderer != null)
            {
                renderer.State = _gameState;
            }
        }

        internal void SyncLocalStateToGameState()
        {
            if (_gameState == null) return;
            _gameState.Config = gameConfig;
            _gameState.State = currentState;
            _gameState.PokerLists = pokerList;
            _gameState.CurrentPokers = currentPokers;
            _gameState.CurrentSendCards = currentSendCards;
            _gameState.CurrentAllSendPokers = currentAllSendPokers;
            _gameState.Send8Cards = send8Cards;
            _gameState.CurrentRank = currentRank;
            _gameState.IsNew = isNew;
            _gameState.ShowSuits = showSuits;
            _gameState.WhoShowRank = whoShowRank;
            _gameState.WhoseOrder = whoseOrder;
            _gameState.FirstSend = firstSend;
            _gameState.WhoIsBigger = whoIsBigger;
            _gameState.Scores = Scores;
            _gameState.DealCount = currentCount;
            if (renderer != null)
            {
                renderer.State = _gameState;
            }
        }
        internal GdiRenderer renderer;

        //*绘画辅助类
        //DrawingForm变量
        internal DrawingFormHelper drawingFormHelper = null;
        internal CalculateRegionHelper calculateRegionHelper = null;

        //记录本次得分
        internal int Scores = 0;


        //娓告垙璁剧疆
        internal GameConfig gameConfig = new GameConfig();

        //出牌时目前牌最大的那一家
        internal int whoIsBigger = 0;


        //闊充箰鏂囦欢
        private string musicFile = "";
        //牌面图案
        internal Bitmap[] cardsImages = new Bitmap[54];

        //鍑虹墝绠楁硶
        internal object[] UserAlgorithms = { null, null, null, null };

        //当前一局已经出的牌
        internal CurrentPoker[] currentAllSendPokers = { new CurrentPoker(), new CurrentPoker(), new CurrentPoker(), new CurrentPoker() };

        #endregion // 变量声明


        internal MainForm()
        {
            InitializeComponent();
            SetStyle(ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.StandardDoubleClick, true);


            //璇诲彇绋嬪簭閰嶇疆
            InitAppSetting();

            notifyIcon.Text = Text;
            BackgroundImage = image;

            //变量初始化
            bmp = new Bitmap(ClientRectangle.Width, ClientRectangle.Height);


            drawingFormHelper = new DrawingFormHelper(this);
            calculateRegionHelper = new CalculateRegionHelper(this);
            renderer = new GdiRenderer(gameConfig);
            renderer.BackgroundImage = image;



            for (int i = 0; i < 54; i++)
            {
                cardsImages[i] = null; //初始化
            }
        }

        private void InitAppSetting()
        {
            //没有配置文件，则从config文件中读取
            if (!File.Exists("gameConfig"))
            {
                AppSettingsReader reader = new AppSettingsReader();
                try
                {
                    Text = (String)reader.GetValue("title", typeof(String));
                }
                catch (Exception ex)
                {
                    Text = "拖拉机大战";
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
                //实际从gameConfig文件中读取
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

            //未序列化的值
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



        #region 绐楀彛浜嬩欢澶勭悊绋嬪簭

        internal void MenuItem_Click(object sender, EventArgs e)
        {

            ToolStripMenuItem menuItem = (ToolStripMenuItem)sender;
            if (menuItem.Text.Equals("退出"))
            {
                this.Close();
            }

            if (menuItem.Text.Equals("开始新游戏"))
            {
                PauseGametoolStripMenuItem.Text = "鏆傚仠娓告垙";


                //新游戏初始状态，我家和敌方都从2开始，令牌为开始发牌
                currentState = new CurrentState(0, 0, 0, 0,0,0,CardCommands.ReadyCards);
                currentRank = 0;

                isNew = true;
                whoIsBigger = 0;

                //初始化
                init();

                //开始定时器，进行发牌
                timer.Start();
            }

        }


        //初始化
        internal void init()
        {
            //每次初始化都重绘背景
            Graphics g = Graphics.FromImage(bmp);
            drawingFormHelper.DrawBackground(g);


            //发一张牌
            dpoker = new DistributePokerHelper();
            pokerList = dpoker.Distribute();

            //每个人手中的牌清空,准备摸牌
            currentPokers[0].Clear();
            currentPokers[1].Clear();
            currentPokers[2].Clear();
            currentPokers[3].Clear();

            //清空已发送的牌
            currentAllSendPokers[0].Clear();
            currentAllSendPokers[1].Clear();
            currentAllSendPokers[2].Clear();
            currentAllSendPokers[3].Clear();


            //为每个人的currentPokers设置Rank
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
            showSuits = 0;
            whoShowRank = 0;
            Scores = 0;
            whoseOrder = 0;
            firstSend = 0;

            //璁剧疆鍛戒护
            currentState.CurrentCardCommands = CardCommands.ReadyCards;
            currentState.Suit = 0;


            //设置还未发牌，循环25次将牌发完
            currentCount = 0;


            engine.NewGame();
            _gameState = new Kuaff.Tractor.GameState {
                Config = gameConfig,
                PokerLists = pokerList,
                CurrentPokers = currentPokers,
                CurrentSendCards = currentSendCards,
                CurrentAllSendPokers = currentAllSendPokers,
                Send8Cards = send8Cards,
                State = currentState,
                CurrentRank = currentRank,
                IsNew = isNew,
                ShowSuits = showSuits,
                WhoShowRank = whoShowRank,
                WhoseOrder = whoseOrder,
                FirstSend = firstSend,
                WhoIsBigger = whoIsBigger,
                Scores = Scores,
                DealCount = currentCount,
            };
            SyncLocalStateToGameState();

            //缁樺埗Sidebar
            drawingFormHelper.DrawSidebar(g);
            //缁樺埗涓滃崡瑗垮寳
            drawingFormHelper.DrawOtherMaster(g, 0, 0);

            if (currentState.Master != 0)
            {
                drawingFormHelper.DrawMaster(g, currentState.Master, 1);
                drawingFormHelper.DrawOtherMaster(g, currentState.Master, 1);
            }

            //缁樺埗Rank
            drawingFormHelper.DrawRank(g,currentState.OurCurrentRank,true,false);
            drawingFormHelper.DrawRank(g, currentState.OpposedCurrentRank, false, false);

            //缁樺埗鑺辫壊
            drawingFormHelper.DrawSuit(g, 0, true, false);
            drawingFormHelper.DrawSuit(g, 0, false, false);

            send8Cards = new ArrayList();
            //璋冩暣鑺辫壊
            if (currentRank == 53)
            {
                currentState.Suit = 5;
            }

            whoIsBigger = 0;

            //如果设置了游戏截止，则停止游戏
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
                    PauseGametoolStripMenuItem.Text = "缁х画娓告垙";
                    PauseGametoolStripMenuItem.Image = Properties.Resources.MenuResume;
                }
            }
        }




        //窗口绘画处理,将缓冲区图像画到窗口上
        private void MainForm_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            //将bmp画到窗口上
            g.DrawImage(bmp, 0, 0);
        }




        private void MainForm_MouseClick_New(object sender, MouseEventArgs e)
        {
            if (((currentState.CurrentCardCommands == CardCommands.WaitingForMySending) ||
                 (currentState.CurrentCardCommands == CardCommands.WaitingForSending8Cards)) &&
                 (whoseOrder == 1))
            {
                HandleCardSelection(e);

                Rectangle pigRect = new Rectangle(296, 300, 53, 46);
                if (pigRect.Contains(e.Location))
                {
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        g.DrawImage(image, pigRect, pigRect, GraphicsUnit.Pixel);
                    }
                    TrySubmitSelectedCards();
                }
            }
            else if (currentState.CurrentCardCommands == CardCommands.ReadyCards)
            {
                TickResult tickResult = engine.Tick(_gameState, DateTime.Now.Ticks);
                if (tickResult.StateChanged && tickResult.NewState != null)
                {
                    SyncFromGameState(tickResult.NewState);
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
                        drawingFormHelper.RenderDealRound(payload.Round);
                    }
                }
                if (currentState.Suit == 0 && currentPokers[0].Count > 0)
                {
                    drawingFormHelper.CallDoRankOrNot();
                    renderer.DrawRankOrNotUI(bmp, _gameState);
                }
            }
        }

        private void TrySubmitSelectedCards()
        {
            List<int> selected = new List<int>();
            for (int i = 0; i < myCardIsReady.Count; i++)
            {
                if ((bool)myCardIsReady[i])
                {
                    selected.Add((int)myCardsNumber[i]);
                }
            }

            PlayResult result = null;
            if (currentState.CurrentCardCommands == CardCommands.WaitingForSending8Cards)
            {
                if (selected.Count == 8)
                {
                    result = engine.PlayerSend8Cards(_gameState, selected);
                }
            }
            else if (currentState.CurrentCardCommands == CardCommands.WaitingForMySending)
            {
                if (selected.Count > 0)
                {
                    result = engine.PlayerPlayCard(_gameState, 1, selected);
                }
            }

            if (result == null)
            {
                return;
            }

            if (result.NewState != null)
            {
                SyncFromGameState(result.NewState);
            }

            foreach (var cmd in result.RenderCommands)
            {
                renderer.Execute(cmd, bmp, currentState);
            }

            if (currentState.CurrentCardCommands == CardCommands.DrawMySortedCards)
            {
                renderer.DrawMySortedCards(bmp, currentPokers[0], currentPokers[0].Count);
            }

            Refresh();
        }

        private void HandleCardSelection(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && myCardsLocation.Count > 0 &&
                e.X >= (int)myCardsLocation[0] &&
                e.X <= ((int)myCardsLocation[myCardsLocation.Count - 1] + 71) &&
                e.Y >= 355 && e.Y < 472)
            {
                if (calculateRegionHelper.CalculateClickedRegion(e, 1))
                {
                    drawingFormHelper.DrawMyPlayingCards(currentPokers[0]);
                    Refresh();
                }
            }
            else if (e.Button == MouseButtons.Right && myCardsLocation.Count > 0)
            {
                int i = calculateRegionHelper.CalculateRightClickedRegion(e);
                if (i > -1 && i < myCardIsReady.Count)
                {
                    bool b = (bool)myCardIsReady[i];
                    int x = (int)myCardsLocation[i];
                    for (int j = 1; j <= i; j++)
                    {
                        if ((int)myCardsLocation[i - j] == (x - 13))
                        {
                            myCardIsReady[i - j] = b;
                            x = x - 13;
                        }
                        else
                        {
                            break;
                        }
                    }
                    drawingFormHelper.DrawMyPlayingCards(currentPokers[0]);
                    Refresh();
                }
            }
        }

        private void MainForm_MouseClick(object sender, MouseEventArgs e)
        {
            MainForm_MouseClick_New(sender, e);
        }


        private void MainForm_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (currentPokers[0].Count == 0)
            {
                return;
            }

            bool b = calculateRegionHelper.CalculateDoubleClickedRegion(e);
            if (!b)
            {
                return;
            }

            Rectangle pigRect = new Rectangle(296, 300, 53, 46);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.DrawImage(image, pigRect, pigRect, GraphicsUnit.Pixel);
            }

            TrySubmitSelectedCards();
        }

        //初始化每个人出的牌
        internal void initSendedCards()
        {
            //重新解析每个人手中的牌
            currentPokers[0] = CommonMethods.parse(pokerList[0], currentState.Suit, currentRank);
            currentPokers[1] = CommonMethods.parse(pokerList[1], currentState.Suit, currentRank);
            currentPokers[2] = CommonMethods.parse(pokerList[2], currentState.Suit, currentRank);
            currentPokers[3] = CommonMethods.parse(pokerList[3], currentState.Suit, currentRank);
        }


        #endregion // 绐楀彛浜嬩欢澶勭悊绋嬪簭


        //定时器,用来显示发牌时的动画
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
            //1.鍒嗙墝
            if (currentState.CurrentCardCommands == CardCommands.ReadyCards) //鍒嗙墝
            {
                if (currentCount ==0)
                {
                    //鐢诲伐鍏锋爮
                    if (!gameConfig.IsDebug)
                    {
                        drawingFormHelper.DrawToolbar();
                    }

                }

                if (currentCount < 25)
                {
                    renderer.DrawDealRound(bmp, _gameState, currentCount);
                    currentCount++;
                    SyncLocalStateToGameState();
                }
                else
                {
                    currentState.CurrentCardCommands = CardCommands.DrawCenter8Cards;
                    SyncLocalStateToGameState();
                }
            }
            else if (currentState.CurrentCardCommands == CardCommands.WaitingShowBottom) //翻底牌完毕后
            {
                drawingFormHelper.DrawCenterImage();
                //画8张牌的背面
                Graphics g = Graphics.FromImage(bmp);

                for (int i = 0; i < 8; i++)
                {
                    g.DrawImage(gameConfig.BackImage, 200 + i * 2, 186, 71, 96);
                }

                SetPauseSet(gameConfig.Get8CardsTime, CardCommands.DrawCenter8Cards);

            }
                        else if (currentState.CurrentCardCommands == CardCommands.DrawCenter8Cards)
            {
                                // save rank state (set by CallDoRankOrNot during dealing)
                int savedSuit = currentState.Suit;
                int savedMaster = currentState.Master;

                TickResult tickResult = engine.Tick(_gameState, DateTime.Now.Ticks);
                if (tickResult.StateChanged && tickResult.NewState != null)
                {
                    SyncFromGameState(tickResult.NewState);
                }
                // restore rank/master if AI called it; master may already be known while suit is still 0
                if (savedSuit != 0 || (savedMaster >= 1 && savedMaster <= 4))
                {
                    if (savedSuit != 0)
                    {
                        currentState.Suit = savedSuit;
                    }
                    if (savedMaster >= 1 && savedMaster <= 4)
                    {
                        currentState.Master = savedMaster;
                    }
                    SyncLocalStateToGameState();
                }
                                foreach (var cmd in tickResult.RenderCommands)
                {
                    if (cmd.Type == RenderCmdType.DrawCenter8)
                    {
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
                                bottom.Add(pokerList[0][0]); bottom.Add(pokerList[0][1]);
                                bottom.Add(pokerList[1][0]); bottom.Add(pokerList[1][1]);
                                bottom.Add(pokerList[2][0]); bottom.Add(pokerList[2][1]);
                                bottom.Add(pokerList[3][0]); bottom.Add(pokerList[3][1]);
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
                        if (currentState.Master < 1 || currentState.Master > 4)
                        {
                            currentState.Master = savedMaster;
                        }
                        whoseOrder = currentState.Master;
                        firstSend = whoseOrder;
                        renderer.DrawCenter8Cards(bmp, _gameState);
                        initSendedCards();
                        renderer.DrawMySortedCards(bmp, currentPokers[0], currentPokers[0].Count);
                        currentState.CurrentCardCommands = CardCommands.WaitingForSending8Cards;
                        SyncLocalStateToGameState();
                        drawingFormHelper.DrawScoreImage(0);
                    }
                }
            }
            else if (currentState.CurrentCardCommands == CardCommands.WaitingShowPass) //鏄剧ず娴佸眬淇℃伅
            {
                //将流局图片清理掉
                drawingFormHelper.DrawCenterImage();
                //drawingFormHelper.DrawScoreImage(0);
                Refresh();
                currentState.CurrentCardCommands = CardCommands.ReadyCards;
                SyncLocalStateToGameState();
            }
                        else if (currentState.CurrentCardCommands == CardCommands.WaitingForSending8Cards)
            {
                TickResult tickResult = engine.Tick(_gameState, DateTime.Now.Ticks);
                if (tickResult.StateChanged && tickResult.NewState != null)
                {
                    SyncFromGameState(tickResult.NewState);
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
            }

                        else if (currentState.CurrentCardCommands == CardCommands.WaitingForSend)
            {
                /* SyncOrder removed - state managed via GameState */
                TickResult tickResult = engine.Tick(_gameState, DateTime.Now.Ticks);
                if (tickResult.StateChanged && tickResult.NewState != null)
                {
                    SyncFromGameState(tickResult.NewState);
                }
                // ensure whoIsBigger is valid before AI play dispatch
                if (whoIsBigger < 1 || whoIsBigger > 4) whoIsBigger = whoseOrder;
                bool needsRender = true;
                foreach (var cmd in tickResult.RenderCommands)
                {
                    if (cmd.Type == RenderCmdType.AiPlayCard)
                    {
                        var payload = (AiPlayPayload)cmd.Payload;
                        int pid = payload.PlayerId;
                        if (pid == 2) drawingFormHelper.DrawFrieldUserSendedCards();
                        else if (pid == 3) drawingFormHelper.DrawPreviousUserSendedCards();
                        else if (pid == 4) drawingFormHelper.DrawNextUserSendedCards();
                        else if (pid == 1)
                        {
                            if (firstSend == 1)
                                Algorithm.ShouldSendedCards(this, 1, currentPokers, currentSendCards, currentState.Suit, currentRank);
                            else
                                Algorithm.MustSendedCards(this, 1, currentPokers, currentSendCards, currentState.Suit, currentRank, currentSendCards[firstSend - 1].Count);
                            renderer.DrawMyFinishSendedCards(bmp, _gameState);
                        }
                        needsRender = false;
                    }
                }
                if (needsRender)
                {
                    Refresh();
                }
            }
                        else if (currentState.CurrentCardCommands == CardCommands.DrawMySortedCards)
            {
                // sort my cards and wait for play
                renderer.DrawMySortedCards(bmp, currentPokers[0], currentPokers[0].Count);
                Refresh();
                currentState.CurrentCardCommands = CardCommands.WaitingForSend;
                SyncLocalStateToGameState();
            }
            else if (currentState.CurrentCardCommands == CardCommands.Pause)
            {
                TickResult tickResult = engine.Tick(_gameState, DateTime.Now.Ticks);
                if (tickResult.StateChanged && tickResult.NewState != null)
                {
                    SyncFromGameState(tickResult.NewState);
                }
            }
            else if (currentState.CurrentCardCommands == CardCommands.DrawOnceFinished) //如果是大家都出完牌
            {
                drawingFormHelper.DrawFinishedOnceSendedCards(); //瀹屾垚娓呯悊宸ヤ綔
                if (currentPokers[0].Count > 0)
                {
                    // reset whoIsBigger for new round, will be updated by play
                    if (whoIsBigger < 1 || whoIsBigger > 4) whoIsBigger = firstSend;
                    currentState.CurrentCardCommands = CardCommands.WaitingForSend;
                    SyncLocalStateToGameState();
                }
            }
            else if (currentState.CurrentCardCommands == CardCommands.DrawOnceRank) //如果这轮大家都出完牌
            {
                currentState.CurrentCardCommands = CardCommands.Undefined;
                /* SyncState removed - state managed via GameState */
                init();
            }
        }

        //设置暂停的最大时间，以及暂停结束后的执行命令
        internal void SetPauseSet(int max, CardCommands wakeup)
        {
            engine.SetPause(max, wakeup);
            sleepMaxTime = max;
            sleepTime = DateTime.Now.Ticks;
            wakeupCardCommands = wakeup;
            currentState.CurrentCardCommands = CardCommands.Pause;
            SyncLocalStateToGameState();
        }


        #region 鑿滃崟浜嬩欢澶勭悊
        //牌面图像
        private void SelectCardImage_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem menuItem = (ToolStripMenuItem)sender;

            if (menuItem.Text.Equals("普通图案"))
            {
                gameConfig.CardsResourceManager = Kuaff_Cards.ResourceManager;
                CommonToolStripMenuItem.CheckState = CheckState.Checked;
                ModelToolStripMenuItem.CheckState = CheckState.Unchecked;
                OperaToolStripMenuItem.CheckState = CheckState.Unchecked;
                CustomCardImageToolStripMenuItem.CheckState = CheckState.Unchecked;

                CustomCardImageToolStripMenuItem.Text = "自定义";
                gameConfig.CardImageName = "";

            }
            else if (menuItem.Text.Equals("棣欒溅缇庡コ"))
            {
                gameConfig.CardsResourceManager = Kuaff_Model.ResourceManager;
                CommonToolStripMenuItem.CheckState = CheckState.Unchecked;
                ModelToolStripMenuItem.CheckState = CheckState.Checked;
                OperaToolStripMenuItem.CheckState = CheckState.Unchecked;
                CustomCardImageToolStripMenuItem.CheckState = CheckState.Unchecked;

                CustomCardImageToolStripMenuItem.Text = "自定义";
                gameConfig.CardImageName = "";
            }
            else if (menuItem.Text.Equals("京剧脸谱"))
            {
                gameConfig.CardsResourceManager = Kuaff_Opera.ResourceManager;
                CommonToolStripMenuItem.CheckState = CheckState.Unchecked;
                ModelToolStripMenuItem.CheckState = CheckState.Unchecked;
                OperaToolStripMenuItem.CheckState = CheckState.Checked;
                CustomCardImageToolStripMenuItem.CheckState = CheckState.Unchecked;

                CustomCardImageToolStripMenuItem.Text = "自定义";
                gameConfig.CardImageName = "";
            }
            else if (menuItem.Text.StartsWith("自定义"))
            {
                SelectCardsImage sci = new SelectCardsImage(this);
                if (sci.ShowDialog(this) == DialogResult.OK)
                {
                    gameConfig.CardImageName = sci.CardsName;
                    menuItem.Text = "自定义--" + gameConfig.CardImageName;

                    CommonToolStripMenuItem.CheckState = CheckState.Unchecked;
                    ModelToolStripMenuItem.CheckState = CheckState.Unchecked;
                    OperaToolStripMenuItem.CheckState = CheckState.Unchecked;
                    CustomCardImageToolStripMenuItem.CheckState = CheckState.Checked;
                }
            }
        }
        //鐗岃儗鍥剧墖
        private void SelectBackImage_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem menuItem = (ToolStripMenuItem)sender;

            if (menuItem.Text.Equals("钄氳摑涓栫晫"))
            {
                gameConfig.BackImage = Kuaff_Cards.back;
                BlueWorldToolStripMenuItem.CheckState = CheckState.Checked;
                GreenAgeToolStripMenuItem.CheckState = CheckState.Unchecked;
                AntelopeToolStripMenuItem.CheckState = CheckState.Unchecked;

                CustomBackImageToolStripMenuItem.CheckState = CheckState.Unchecked;
                CustomBackImageToolStripMenuItem.Text = "自定义";
            }
            else if (menuItem.Text.Equals("闈掓订骞村崕"))
            {
                gameConfig.BackImage = Kuaff_Cards.back2;
                BlueWorldToolStripMenuItem.CheckState = CheckState.Unchecked;
                GreenAgeToolStripMenuItem.CheckState = CheckState.Checked;
                AntelopeToolStripMenuItem.CheckState = CheckState.Unchecked;

                CustomBackImageToolStripMenuItem.CheckState = CheckState.Unchecked;
                CustomBackImageToolStripMenuItem.Text = "自定义";
            }
            else if (menuItem.Text.Equals("鑽夊師缇氱緤"))
            {
                gameConfig.BackImage = Kuaff_Cards.back3;
                BlueWorldToolStripMenuItem.CheckState = CheckState.Unchecked;
                GreenAgeToolStripMenuItem.CheckState = CheckState.Unchecked;
                AntelopeToolStripMenuItem.CheckState = CheckState.Checked;

                CustomBackImageToolStripMenuItem.CheckState = CheckState.Unchecked;
                CustomBackImageToolStripMenuItem.Text = "自定义";
            }
            else if (menuItem.Text.StartsWith("自定义"))
            {
                SelectCardbackImage sci = new SelectCardbackImage(this);
                if (sci.ShowDialog(this) == DialogResult.OK)
                {
                    menuItem.Text = "自定义--" + sci.CardBackImageName;

                    BlueWorldToolStripMenuItem.CheckState = CheckState.Unchecked;
                    GreenAgeToolStripMenuItem.CheckState = CheckState.Unchecked;
                    AntelopeToolStripMenuItem.CheckState = CheckState.Unchecked;
                    CustomBackImageToolStripMenuItem.CheckState = CheckState.Checked;
                }

            }
        }

        //閫夋嫨鑳屾櫙鍥剧墖
        private void SelectImage_Click(object sender, EventArgs e)
        {
            PauseGametoolStripMenuItem.Text = "鏆傚仠娓告垙";

            ToolStripMenuItem menuItem = (ToolStripMenuItem)sender;
            if (menuItem.Text.Equals("澶哥埗绉戞妧"))
            {
                KuaffToolStripMenuItem.CheckState = CheckState.Checked;
                image = global::Kuaff.Tractor.Properties.Resources.Backgroud;
                BackgroundImage = image;

                Graphics g = Graphics.FromImage(bmp);
                g.DrawImage(image, ClientRectangle, ClientRectangle,GraphicsUnit.Pixel);

                init();
                //缁樺埗涓滃崡瑗垮寳

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
            else if (menuItem.Text.Equals("自定义图片"))
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
                    //缁樺埗涓滃崡瑗垮寳

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

        //鎵樼洏浜嬩欢澶勭悊
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

        //璁剧疆娓告垙閫熷害
        private void GameSpeedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetSpeedDialog dialog = new SetSpeedDialog(this);
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                //璋冩暣閫熷害
                gameConfig.FinishedOncePauseTime = (int)(150 * Math.Pow(10, dialog.trackBar1.Value / 25.0));
                gameConfig.NoRankPauseTime = (int)(500 * Math.Pow(10, dialog.trackBar2.Value / 25.0));
                gameConfig.Get8CardsTime = (int)(100 * Math.Pow(10, dialog.trackBar3.Value / 25.0));
                gameConfig.SortCardsTime = (int)(100 * Math.Pow(10, dialog.trackBar4.Value / 25.0));
                gameConfig.FinishedThisTime = (int)(250 * Math.Pow(10, dialog.trackBar5.Value / 25.0));
                gameConfig.TimerDiDa = (int)(10 * Math.Pow(10, dialog.trackBar6.Value / 25.0));
                timer.Interval = gameConfig.TimerDiDa;
            }
        }

        //淇濆瓨鐗屽眬
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

        //璇诲彇鐗屽眬
        private void RestoreToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PauseGametoolStripMenuItem.Text = "鏆傚仠娓告垙";

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
                    /* SetCurrentRank removed */
                    /* SyncTeamRanks removed */
                }
                else if(currentState.Master == 3 || currentState.Master == 4)
                {
                    currentRank = currentState.OpposedCurrentRank;
                    /* SetCurrentRank removed */
                    /* SyncTeamRanks removed */
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

        //显示帮助
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
            if (menuItem.Text.Equals("鏆傚仠娓告垙"))
            {
                timer.Stop();
                menuItem.Text = "缁х画娓告垙";
                menuItem.Image = Properties.Resources.MenuResume;
            }
            else
            {
                timer.Start();
                menuItem.Text = "鏆傚仠娓告垙";
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
            //弹出内置音乐选择对话框
            SelectMusic sem = new SelectMusic();
            if (sem.ShowDialog(this) == DialogResult.OK)
            {
                NoBackMusicToolStripMenuItem.CheckState = CheckState.Unchecked;

                //如果选择了一首曲子，则播放
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

        //随机播放音乐
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

        private void FereToolStripMenuItem_Click(object sender, EventArgs e) //拖拉机伴侣
        {
            Fere fere = new Fere();
            fere.Show(this);
        }

        private void SeeTotalScoresToolStripMenuItem_Click(object sender, EventArgs e) //得分统计
        {
            TotalScores ts = new TotalScores(this);
            ts.Show(this);
        }

        private void SelectAlgorithmToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SelectUserAlgorithm sua = new SelectUserAlgorithm(this);
            sua.ShowDialog(this);
        }

        #endregion // 鑿滃崟浜嬩欢澶勭悊

        private void SetGameFinishedtoolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetGameFinished sgf = new SetGameFinished(this);
            sgf.ShowDialog(this);
        }




    }
}
        // ====== GameState锛圥hase A 鏁版嵁娴佺函鍖栵級 ======
