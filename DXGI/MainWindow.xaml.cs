﻿using Providers.Nova.Modules;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Managers.Nova.Server;
using System.Diagnostics;
using System.Threading.Tasks;
using Managers.LiveControl.Client;

using Providers.LiveControl.Client;
using Network;
using Network.Messages.Nova;
using System.IO;
using System.Drawing.Imaging;
using RamGecTools;
using HookerClient;
using System.Windows.Input;
using System.ComponentModel;
using System.Collections.Generic;
using System.Windows.Media;
using WindowsInput;
using WindowsInput.Native;
using HookerServer;



namespace DXGI_DesktopDuplication
{
    /// <summary>
    ///     MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public static UpdateUI RefreshUI;
        private Thread duplicateThread = null;

        public Managers.Nova.Client.NovaManager NovaManagerClient;
        public Managers.LiveControl.Client.LiveControlManager LiveControlManagerClient;

        public NovaManager NovaManagerServer;
        public Managers.LiveControl.Server.LiveControlManager LiveControlManagerServer;

        //Hook Servers
        InputSimulator inputSimulator;

        //Hook Client
        RamGecTools.MouseHook mouseHook = new RamGecTools.MouseHook();
        RamGecTools.KeyboardHook keyboardHook = new RamGecTools.KeyboardHook();
        private LayoutManager layout;
        private ServerManager serverManger;

        private double screenImagePositionX;
        private double screenImagePositionY;

        public MainWindow()
        {
            InitializeComponent();
            RefreshUI = UpdateImage;

            //test code here
            screenImagePositionX = SystemParameters.WorkArea.Width;
            screenImagePositionY = SystemParameters.WorkArea.Height;

           

            Console.WriteLine("{0}, {1}", SystemParameters.WorkArea.Width, SystemParameters.WorkArea.Height);
            Console.WriteLine("{0}, {1}", SystemParameters.PrimaryScreenWidth, SystemParameters.PrimaryScreenHeight);
            Console.WriteLine(Marshal.SizeOf(typeof(Vertex)));
        }

      
        
        public async Task InitNetworkManagerClient()
        {
            NovaManagerClient=Managers.NovaClient.Instance.NovaManager;
            LiveControlManagerClient=  Managers.NovaClient.Instance.LiveControlManager;

            LiveControlManagerClient.OnScreenshotReceived += new EventHandler<ScreenshotMessageEventArgs>(LiveControlManager_OnScreenshotReceived);

            NovaManagerClient.OnIntroductionCompleted += new EventHandler<IntroducerIntroductionCompletedEventArgs>(ClientManager_OnIntroductionCompleted);
            NovaManagerClient.OnNatTraversalSucceeded += new EventHandler<Network.NatTraversedEventArgs>(ClientManager_OnNatTraversalSucceeded);
            NovaManagerClient.OnConnected += new EventHandler<ConnectedEventArgs>(ClientManager_OnConnected);
        }

        public async Task InitNetworkManagerServer()
        {

             NovaManagerServer =  Managers.NovaServer.Instance.NovaManager; 
             LiveControlManagerServer =  Managers.NovaServer.Instance.LiveControlManager;
             inputSimulator = new InputSimulator();
            NovaManagerServer.OnIntroducerRegistrationResponded += NovaManager_OnIntroducerRegistrationResponded;
            NovaManagerServer.OnNewPasswordGenerated += new EventHandler<PasswordGeneratedEventArgs>(ServerManager_OnNewPasswordGenerated);
            NovaManagerServer.Network.OnConnected += new EventHandler<Network.ConnectedEventArgs>(Network_OnConnected);

            PasswordGeneratedEventArgs passArgs = await NovaManagerServer.GeneratePassword();
            //LabelPassword.Content = passArgs.NewPassword;
            IntroducerRegistrationResponsedEventArgs regArgs = await NovaManagerServer.RegisterWithIntroducerAsTask();
            //LabelNovaId.Content = regArgs.NovaId;
            Status.Content = "Host is live";


        }

        void ClientManager_OnConnected(object sender, ConnectedEventArgs e)
        {
            //  ButtonConnect.Set(() => ButtonConnect.Text, "Connected.");
            remoteConnection.Content = "Connected to remote";
            LiveControlManagerClient.RequestScreenshot();

            // this.Dispose();
        }

        private void ClientManager_OnIntroductionCompleted(object sender, IntroducerIntroductionCompletedEventArgs e)
        {
            switch (e.Result)
            {
                case Network.Messages.Nova.ResponseIntroducerIntroductionCompletedMessage.Result.Allowed:
                    // Do nothing, expect OnNatTraversalSucceeded() to be raised shortly
                    break;

                case Network.Messages.Nova.ResponseIntroducerIntroductionCompletedMessage.Result.Denied:
                    switch (e.DenyReason)
                    {
                        case ResponseIntroducerIntroductionCompletedMessage.Reason.WrongPassword:
                            //TextBox_Password.Set(() => TextBox_Password.Text, String.Empty); // clear the password box for re-entry
                            remoteConnection.Content = "Enter correct password/login";
                            MessageBox.Show("Please enter the correct password.");
                            break;
                        case ResponseIntroducerIntroductionCompletedMessage.Reason.Banned:
                            MessageBox.Show("You have been banned for trying to connect too many times.");
                            break;
                    }
                    break;
            }
        }

        private void ClientManager_OnNatTraversalSucceeded(object sender, NatTraversedEventArgs e)
        {
            // ButtonConnect.Set(() => ButtonConnect.Text, "Connecting to " + TextBox_Id.Text + " ...");
            remoteConnection.Content = "Connecting to machine..";

        }


        public void UpdateImage(Bitmap bitmap)
        {
            IntPtr pointer = bitmap.GetHbitmap();

            BGImage.Source = Imaging.CreateBitmapSourceFromHBitmap(pointer, IntPtr.Zero, Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
            DeleteObject(pointer);
        }

        [DllImport("gdi32")]
        private static extern int DeleteObject(IntPtr o);

        

        //NOVA Network handshakes for HOST
        void Network_OnConnected(object sender, Network.ConnectedEventArgs e)
        {
            Status.Content = "Connected";
           
        }

        void ServerManager_OnNewPasswordGenerated(object sender, Providers.Nova.Modules.PasswordGeneratedEventArgs e)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                LabelPassword.Content = e.NewPassword;
            }));

        }

        private void NovaManager_OnIntroducerRegistrationResponded(object sender, Providers.Nova.Modules.IntroducerRegistrationResponsedEventArgs e)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                LabelNovaId.Content = e.NovaId;
            }));

        }

        private async void startCapture_Click(object sender, RoutedEventArgs e)
        {
            // await CaptureFrame();
            //Start Server Network Registration
            await InitNetworkManagerServer();
            LiveControlManagerServer.OnMouseKeyboardEventReceived += LiveControlManagerServer_OnMouseKeyboardEventReceived;


        }

        void LiveControlManagerServer_OnMouseKeyboardEventReceived(object sender, Network.Messages.LiveControl.MouseKeyboardNotification e)
        {
            string msgRecvd=  e.data;   
            parseMessage(msgRecvd);
            //throw new NotImplementedException();
        }

     
        private async void ConnectRemote_Click(object sender, RoutedEventArgs e)
        {
            await  InitNetworkManagerClient();
            await NovaManagerClient.RequestIntroductionAsTask(RID.Text, PWD.Text);

            //if (duplicateThread.ThreadState == System.Threading.ThreadState.Unstarted)
            //{
            //    duplicateThread.Start();
            //    Console.WriteLine("Start");
            //}


            //CapturedChangedRects();
            //Console.WriteLine("Click");
        }

        private async void LiveControlManager_OnScreenshotReceived(object sender, ScreenshotMessageEventArgs e)
        {
            var screenshot = e.Screenshot;
            await UpdateImage(screenshot);
          

               //LiveControlManager.RequestScreenshot();
       }


        private async Task  UpdateImage(Model.LiveControl.Screenshot screenshot)
        {
            using (var stream = new System.IO.MemoryStream(screenshot.Image))
            {

                Image image = Image.FromStream(stream);
                var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                bitmap.BeginInit();
                MemoryStream memoryStream = new MemoryStream();
                // Save to a memory stream...
                image.Save(memoryStream, ImageFormat.Bmp);
                // Rewind the stream...
                memoryStream.Seek(0, System.IO.SeekOrigin.Begin);
                bitmap.StreamSource = memoryStream;
                bitmap.EndInit();

                BGImage.Source = bitmap;
                //Task.Run(()=>this.BGImage.Source = bitmap);
                //Dispatcher.BeginInvoke((Action)(() => BGImage.Source = bitmap));
                //if (dispatcher != null)
                // Dispatcher.BeginInvoke(MainWindow.RefreshUI,bitmap);
                //if (ShowRegionOutlines)
                //{
                //    var gfx = gdiScreen1.CreateGraphics();
                //    gfx.DrawLine(pen, new Point(e.Screenshot.Region.X, e.Screenshot.Region.Y), new Point(e.Screenshot.Region.X + e.Screenshot.Region.Width, e.Screenshot.Region.Y));
                //    gfx.DrawLine(pen, new Point(e.Screenshot.Region.X + e.Screenshot.Region.Width, e.Screenshot.Region.Y), new Point(e.Screenshot.Region.X + e.Screenshot.Region.Width, e.Screenshot.Region.Y + e.Screenshot.Region.Y));
                //    gfx.DrawLine(pen, new Point(e.Screenshot.Region.X + e.Screenshot.Region.Width, e.Screenshot.Region.Y + e.Screenshot.Region.Y), new Point(e.Screenshot.Region.X, e.Screenshot.Region.Y + e.Screenshot.Region.Y));
                //    gfx.DrawLine(pen, new Point(e.Screenshot.Region.X, e.Screenshot.Region.Y + e.Screenshot.Region.Y), new Point(e.Screenshot.Region.X, e.Screenshot.Region.Y));
                //    gfx.Dispose();
                //}
                //gdiScreen1.Draw(image, screenshot.Region);
            }
              
        }
        private void BGImage_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            //InstallMouseAndKeyboard();
            //Questo bind vale solo mentre si è connessi
            //bindHotkeyCommands();

            BGImage.MouseMove += BGImage_MouseMove;
            GoFullscreen(true);
            BGImage.Width = 1280;
            BGImage.Height = 1024;

            
                
            
        }


        
        private void GoFullscreen(bool fullscreen)
        {
            var window = MainWindow.GetWindow(this);
            window.WindowState = System.Windows.WindowState.Maximized;

            Password.Visibility = Visibility.Collapsed;
            LabelNovaId.Visibility = Visibility.Collapsed;
            LabelPassword.Visibility = Visibility.Collapsed;
            Status.Visibility = Visibility.Collapsed;
            label.Visibility = Visibility.Collapsed;
            label1.Visibility = Visibility.Collapsed;
            PWD.Visibility = Visibility.Collapsed;
            RID.Visibility = Visibility.Collapsed;
            ConnectRemote.Visibility = Visibility.Collapsed;
            startCapture.Visibility = Visibility.Collapsed;
            remoteConnection.Visibility = Visibility.Collapsed;

        }

        void BGImage_MouseMove(object sender, MouseEventArgs e)
        {

            double Xabs = e.GetPosition(BGImage).X;
            double Yabs = e.GetPosition(BGImage).Y;
            double x = Math.Round((Xabs / 1280), 4); //must send relative position REAL/RESOLUTION System.Windows.SystemParameters.PrimaryScreenHeigh
            double y = Math.Round((Yabs / 1024), 4);

            double x1 = Math.Round((Xabs / System.Windows.SystemParameters.PrimaryScreenWidth), 4); //must send relative position REAL/RESOLUTION System.Windows.SystemParameters.PrimaryScreenHeigh
            double y2 = Math.Round((Yabs / System.Windows.SystemParameters.PrimaryScreenHeight), 4);

            //this.serverManger.sendMessage
            LiveControlManagerClient.Provider.sendMouseKeyboardStateMessage("M" + " " + x.ToString() + " " + y.ToString());
            Console.WriteLine("M" + " " + x + " " + y);
        }



        private void BGImage_MouseLeave(object sender, MouseEventArgs e)
        {
            //UnistallMouseAndKeyboard();
            // unbindHotkeyCommands();
        }

        #region HooksServer
        private void parseMessage(string buffer)
        {
            //Console.WriteLine("[ [" + buffer + "]");
            String[] commands = buffer.Split(' '); //split incoming message
            if (commands.GetValue(0).Equals("M")) //mouse movement
            {
                //16 bit è più veloce di 32
                int x = Convert.ToInt16(Double.Parse(commands[1]) * System.Windows.SystemParameters.PrimaryScreenWidth);
                int y = Convert.ToInt16(Double.Parse(commands[2]) * System.Windows.SystemParameters.PrimaryScreenHeight);
                NativeMethods.SetCursorPos(x, y);
                inputSimulator.Mouse.MoveMouseToPositionOnVirtualDesktop(x, y);
            }
            else if (commands.GetValue(0).ToString().Equals("W"))
            { //scroll
                int scroll = Convert.ToInt32(commands.GetValue(1).ToString());
                inputSimulator.Mouse.VerticalScroll(scroll);
            }
            else if (commands.GetValue(0).ToString().Equals("C")) //click
            {
                if (commands.GetValue(1).ToString().Equals("WM_LBUTTONDOWN"))
                {
                    inputSimulator.Mouse.LeftButtonDown();
                }
                else if (commands.GetValue(1).ToString().Equals("WM_LBUTTONUP"))
                {
                    inputSimulator.Mouse.LeftButtonUp();

                }
                else if (commands.GetValue(1).ToString().Equals("WM_RBUTTONDOWN"))
                {
                    inputSimulator.Mouse.RightButtonDown();
                }
                else if (commands.GetValue(1).ToString().Equals("WM_RBUTTONUP"))
                {
                    inputSimulator.Mouse.RightButtonUp();
                }

            }
            else if (commands.GetValue(0).ToString().Equals("K")) //keyboard
            {
                VirtualKeyCode vk = (VirtualKeyCode)Convert.ToInt32(commands.GetValue(1).ToString());
                if (commands.GetValue(2).ToString().Equals("DOWN"))
                {
                    inputSimulator.Keyboard.KeyDown(vk); //keydown
                }
                else if (commands.GetValue(2).ToString().Equals("UP"))
                {
                    inputSimulator.Keyboard.KeyUp(vk); //keyup
                }
            }
            else if (commands.GetValue(0).ToString().Equals("G")) //used as callback for the clipboard
            {
                Console.WriteLine("Received : GIMME CLIPBOARD");
                ClipboardManager cb = new ClipboardManager();
                //Thread cbSenderThread = new Thread(() =>
                //{
                //    Thread.CurrentThread.IsBackground = true;
                //    // connect the fly 
                //    if (this.clientCB != null)
                //        this.clientCB.Client.Close();
                //    this.clientCB = new TcpClient();
                //    this.clientCB.Connect(((IPEndPoint)this.client.Client.RemoteEndPoint).Address, 9898); //questo è il client che riceve
                //    cb.sendClipBoardFaster(this.clientCB);
                //});
                //cbSenderThread.SetApartmentState(ApartmentState.STA);
                //cbSenderThread.Start();
                //cbSenderThread.Join();
                //icon.ShowBalloonTip("Clipboard", "La clipboard è stata trasferita al client!", new Hardcodet.Wpf.TaskbarNotification.BalloonIcon());
            }


        }
        #endregion

        #region Core
        public void closeCommunication(object sender, ExecutedRoutedEventArgs e)
        {
            //Piccolo stratagemma x evitare che al server arrivino solo gli eventi KEYDOWN (che causerebberro problemi)
            this.serverManger.sendMessage("K" + " " + (int)RamGecTools.KeyboardHook.VKeys.LCONTROL + " " + "UP");
            this.serverManger.sendMessage("K" + " " + (int)RamGecTools.KeyboardHook.VKeys.LMENU + " " + "UP");
            this.serverManger.sendMessage("K" + " " + (int)RamGecTools.KeyboardHook.VKeys.KEY_E + " " + "UP");
            UnistallMouseAndKeyboard();
            unbindHotkeyCommands(); //rimuovo vincoli su hotkeys
            this.serverManger.disconnect();

        }

        private void pauseCommunication(object sender, ExecutedRoutedEventArgs e)
        {
            //Piccolo stratagemma x evitare che al server arrivino solo gli eventi KEYDOWN (che causerebberro problemi)
            this.serverManger.sendMessage("K" + " " + (int)RamGecTools.KeyboardHook.VKeys.LCONTROL + " " + "UP");
            this.serverManger.sendMessage("K" + " " + (int)RamGecTools.KeyboardHook.VKeys.LMENU + " " + "UP");
            this.serverManger.sendMessage("K" + " " + (int)RamGecTools.KeyboardHook.VKeys.KEY_P + " " + "UP");
            UnistallMouseAndKeyboard();
            unbindHotkeyCommands();
        
        }

        private void continueCommunication()
        {
            InstallMouseAndKeyboard();
            bindHotkeyCommands();
            
        }

        public void closeOnException(String s)
        {
            //MessageBox.Show(s);
            UnistallMouseAndKeyboard();
            unbindHotkeyCommands(); //rimuovo vincoli su hotkeys
            this.serverManger.disconnect();
        }

        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }

        private void gimmeClipboard(object sender, ExecutedRoutedEventArgs e)
        {
            this.serverManger.sendMessage("K" + " " + (int)RamGecTools.KeyboardHook.VKeys.LCONTROL + " " + "UP");
            this.serverManger.sendMessage("K" + " " + (int)RamGecTools.KeyboardHook.VKeys.LMENU + " " + "UP");
            this.serverManger.sendMessage("K" + " " + (int)RamGecTools.KeyboardHook.VKeys.KEY_Z + " " + "UP");
            this.serverManger.sendMessage("G"); //this is the gimme message
        }

        private void switchToNextServer(object sender, ExecutedRoutedEventArgs e)
        {
            this.serverManger.sendMessage("K" + " " + (int)RamGecTools.KeyboardHook.VKeys.LCONTROL + " " + "UP");
            this.serverManger.sendMessage("K" + " " + (int)RamGecTools.KeyboardHook.VKeys.LMENU + " " + "UP");
            this.serverManger.sendMessage("K" + " " + (int)RamGecTools.KeyboardHook.VKeys.KEY_N + " " + "UP");
            //chiamo il metodo che realmente switcha il puntatore al sender udp 
            this.serverManger.nextSelectedServers();
            //aggiorno la label in base ai risultati effettivi dell'operazione
            //lblMessages.Dispatcher.Invoke(DispatcherPriority.Background,
             //new Action(() => { lblMessages.Content = "Connesso al server : " + this.serverManger.selectedServers.ElementAt(this.serverManger.serverPointer).name; }));

        }
        public void sendClipboard(object sender, ExecutedRoutedEventArgs e)
        {
            //Piccolo stratagemma x evitare che al server arrivino solo gli eventi KEYDOWN (che causerebberro problemi)
            this.serverManger.sendMessage("K" + " " + (int)RamGecTools.KeyboardHook.VKeys.LCONTROL + " " + "UP");
            this.serverManger.sendMessage("K" + " " + (int)RamGecTools.KeyboardHook.VKeys.LMENU + " " + "UP");
            this.serverManger.sendMessage("K" + " " + (int)RamGecTools.KeyboardHook.VKeys.KEY_X + " " + "UP");
            //this.serverManger.sendClipBoardFaster(this.serverManger.selectedServers.ElementAt(this.serverManger.serverPointer).CBClient);
        }

   

        #endregion

        #region HooksClient
        //TODO: passare un'oggetto al server in modo che questo possa eseguire azione
        void keyboardHook_KeyPress(int op, RamGecTools.KeyboardHook.VKeys key)
        {
            try
            {
                if (op == 0)
                {
                    //key is down

                    LiveControlManagerClient.Provider.sendMouseKeyboardStateMessage("K" + " " + (int)key + " " + "DOWN");
                    Console.WriteLine("K" + " " + (int)key + " " + "DOWN");

                }
                else
                {
                    //key is up
                    //this.serverManger.sendMessage
                       LiveControlManagerClient.Provider.sendMouseKeyboardStateMessage("K" + " " + (int)key + " " + "UP");
                       Console.WriteLine("K" + " " + (int)key + " " + "UP");
                }
            }
            catch (Exception ex)
            {
                closeOnException(ex.Message);
                MessageBox.Show("La connessione si è interrotta");

            }
        }

        /*
         Questo metodo è stato creato per il seguente motivo: il keyboard hooker fa in modo che gli hotkeys tipo alt-tab, non vadano al sistema operativo 
         * del client: in pratica è come se l'hooker si "mangiasse" gli eventi key_down, di conseguenza bisogna
         * generarli "artificialmente" in questo modo, per far sì che il server li riceva
         */
        void keyboardHook_HotKeyPress(int virtualKeyCode)
        {
           
            //this.serverManger.sendMessage
                LiveControlManagerClient.Provider.sendMouseKeyboardStateMessage("K" + " " + (int)virtualKeyCode + " " + "DOWN");
                Console.WriteLine("K" + " " + (int)virtualKeyCode + " " + "DOWN");
        }

        void mouseHook_MouseEvent(int type, RamGecTools.MouseHook.MSLLHOOKSTRUCT mouse, RamGecTools.MouseHook.MouseMessages move)
        {
            switch (type)
            {
                case 0:  //mouse click
                    LiveControlManagerClient.Provider.sendMouseKeyboardStateMessage("C" + " " + move.ToString());
                    Console.Write("C" + " " + move.ToString());

                    break;
                case 1: // Mouse movement
                    double x = Math.Round((mouse.pt.x / System.Windows.SystemParameters.PrimaryScreenWidth), 4); //must send relative position REAL/RESOLUTION
                    double y = Math.Round((mouse.pt.y / System.Windows.SystemParameters.PrimaryScreenHeight), 4);

                    //this.serverManger.sendMessage
                    LiveControlManagerClient.Provider.sendMouseKeyboardStateMessage("M" + " " + x.ToString() + " " +y.ToString());
                    Console.WriteLine("M" + " " + x + " " + y);
                    break;
                default:
                    break;
            }
        }

        private void MouseWheelEventHandler(object sender, MouseWheelEventArgs e)
        {
            LiveControlManagerClient.Provider.sendMouseKeyboardStateMessage("W" + " " + ((int)e.Delta / 120).ToString());
        }
        public KeyEventHandler wnd_KeyDown { get; set; }

        private void InstallMouseAndKeyboard()
        {
            //Insatllo keyboard 
            keyboardHook.KeyPress += new RamGecTools.KeyboardHook.myKeyboardHookCallback(keyboardHook_KeyPress);
            //Questo qui sotto era un vecchio handler che usavo per i problemi degli shortcut, momentaneamente lascio commentato
            keyboardHook.HotKeyPress += new RamGecTools.KeyboardHook.myKeyboardHotkeyCallback(keyboardHook_HotKeyPress);
            keyboardHook.Install();
            //Installo Mouse
            mouseHook.MouseEvent += new RamGecTools.MouseHook.myMouseHookCallback(mouseHook_MouseEvent);
            mouseHook.Install();
            this.MouseWheel += MouseWheelEventHandler;
        }

        private void UnistallMouseAndKeyboard()
        {
            keyboardHook.KeyPress -= new RamGecTools.KeyboardHook.myKeyboardHookCallback(keyboardHook_KeyPress);
            mouseHook.MouseEvent -= new RamGecTools.MouseHook.myMouseHookCallback(mouseHook_MouseEvent);
            keyboardHook.Uninstall();
            mouseHook.Uninstall();
            this.MouseWheel += MouseWheelEventHandler;

        }


        private void bindHotkeyCommands()
        {
            try
            {
                //aggancio CTRL+ALT+P con pauseCommunication
                RoutedCommand pauseComm = new RoutedCommand();
                pauseComm.InputGestures.Add(new KeyGesture(Key.P, ModifierKeys.Control | ModifierKeys.Alt));
                CommandBindings.Add(new CommandBinding(pauseComm, pauseCommunication));
                //aggancio CTRL+ALT+E con closeCommunication
                RoutedCommand closeComm = new RoutedCommand();
                closeComm.InputGestures.Add(new KeyGesture(Key.E, ModifierKeys.Control | ModifierKeys.Alt));
                CommandBindings.Add(new CommandBinding(closeComm, closeCommunication));
                //aggancio CTRL+ALT+N per next server
                RoutedCommand nextServer = new RoutedCommand();
                nextServer.InputGestures.Add(new KeyGesture(Key.N, ModifierKeys.Control | ModifierKeys.Alt));
                CommandBindings.Add(new CommandBinding(nextServer, switchToNextServer));
                //aggancio CTRL+ALT+X per inviare mia clipboard
                RoutedCommand sendClipboardcmd = new RoutedCommand();
                sendClipboardcmd.InputGestures.Add(new KeyGesture(Key.X, ModifierKeys.Control | ModifierKeys.Alt));
                CommandBindings.Add(new CommandBinding(sendClipboardcmd, sendClipboard));
                //aggancio CTRL+ALT+Z per ricevere la clipboard dal server
                RoutedCommand gimmeClipboardcmd = new RoutedCommand();
                gimmeClipboardcmd.InputGestures.Add(new KeyGesture(Key.Z, ModifierKeys.Control | ModifierKeys.Alt));
                CommandBindings.Add(new CommandBinding(gimmeClipboardcmd, gimmeClipboard));

            }
            catch (Exception e)
            {
                //MessageBox.Show("bindHotKeyCommands: " + e.Message);
                Application.Current.Shutdown();
            }
        }

        private void unbindHotkeyCommands()
        {
            try
            {
                CommandBindings.Clear();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Cannot unbind : " + ex.Message);
            }
        }

        private void wnd_Closing(object sender, CancelEventArgs e)
        {
            e.Cancel = true; //AVOID ALT F4
        }
        #endregion

        public enum MouseMessages
        {
            WM_LBUTTONDOWN = 0x0201,
            WM_LBUTTONUP = 0x0202,
            WM_MOUSEMOVE = 0x0200,
            WM_MOUSEWHEEL = 0x020A,
            WM_RBUTTONDOWN = 0x0204,
            WM_RBUTTONUP = 0x0205,
            WM_LBUTTONDBLCLK = 0x0203,
            WM_MBUTTONDOWN = 0x0207,
            WM_MBUTTONUP = 0x0208
        }

    
    }


    public partial class NativeMethods
    {
        /// Return Type: BOOL->int  
        ///X: int  
        ///Y: int  
        [System.Runtime.InteropServices.DllImportAttribute("user32.dll", EntryPoint = "SetCursorPos")]
        [return: System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.Bool)]
        public static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, UIntPtr dwExtraInfo);

    }
}