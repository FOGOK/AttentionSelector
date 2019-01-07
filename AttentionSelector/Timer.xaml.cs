using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace AttentionSelector
{
    /// <summary>
    /// Interaction logic for Timer.xaml
    /// </summary>
    public partial class Timer : Window
    {
        public enum State
        {
            Work,
            Relax,
            WaitToCheck,
            Pause
        }

        private volatile State workState;

        private Thread thread;
        private object monitor = new object();

        public State WorkState
        {
            get { return workState; }
            set { workState = value; }
        }

        //settings
        public enum Settings
        {
            StartTime,
            WorkTime,
            RelaxTime,
            CheckTime,
            CurrentOffsetToPause,
            PauseInRelax
        }
        private readonly ConcurrentDictionary<Settings, object> settings = new ConcurrentDictionary<Settings, object>();
        //

        private volatile bool checkedToggle;
        private volatile bool isPause;
        private volatile bool switchState;

        /// <summary>
        /// Get param from key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        private string GetValueFromConfig(string key)
        {
            try
            {
                return GetFileUTF8Content("config.txt")
                    .Split(new[] { Environment.NewLine }, StringSplitOptions.None)
                    .Where(q => !q.Equals(""))
                    .Where(q => !q.Substring(0, 2)
                        .Equals("//"))
                    .First(q => q.Contains(key))
                    .Split(new[] { ":=" }, StringSplitOptions.None)[1];
            }
            catch (Exception e)
            {
                return "";
            }
        }

        private string GetFileUTF8Content(string path)
        {
            FileStream file = new FileStream(path, FileMode.Open, FileAccess.Read);
            string fileContent;
            using (var fileReader = new StreamReader(file, Encoding.UTF8))
                fileContent = fileReader.ReadToEnd();
            file.Dispose();
            return fileContent;
        }


        public Timer()
        {
            InitializeComponent();
            try
            {
                var t = Properties.Settings.Default["LeftOffset"].ToString();
                Left = int.Parse(Properties.Settings.Default["LeftOffset"].ToString());
                Top = int.Parse(Properties.Settings.Default["TopOffset"].ToString());
            }
            catch (Exception e) { }

            settings[Settings.StartTime] = DateTime.Now;
            settings[Settings.RelaxTime] = new TimeSpan(0, int.Parse(GetValueFromConfig("relaxMinutes")), 0);
            settings[Settings.WorkTime] = new TimeSpan(0, int.Parse(GetValueFromConfig("workMinutes")), 0);

            settings[Settings.CheckTime] = new TimeSpan(0, int.Parse(GetValueFromConfig("waitForCheckMinutes")), 0);
            WorkState = State.Work;

            AttentSelector.getInstance().Init();

            thread = new Thread(MainLogic);
            thread.Start();
        }

        private void MainLogic()
        {
            checkedToggle = false;
            TimeSpan offsetTime = DateTime.Now.TimeOfDay;
            TimeSpan startWaitToCheckStamp = DateTime.Now.TimeOfDay;
            bool isContinueWait = false;
            while (true)
            {
                if (isContinueWait)
                {
                    isContinueWait = false;
                }
                else
                {
                    lock (monitor)   
                        Monitor.Wait(monitor, TimeSpan.FromSeconds(1));   
                }

                switch (WorkState)
                {
                    case State.Work:
                    case State.Relax: // 20 - 5 = 15
                        offsetTime = DateTime.Now - (DateTime)settings[Settings.StartTime];

                        if ((offsetTime > (TimeSpan)settings[Settings.RelaxTime] && WorkState == State.Relax) || 
                            (offsetTime > (TimeSpan)settings[Settings.WorkTime] && WorkState == State.Work) || switchState)
                        {
                            startWaitToCheckStamp = DateTime.Now.TimeOfDay - (TimeSpan) settings[Settings.CheckTime];
                            settings[Settings.PauseInRelax] = WorkState == State.Relax;
                            if (!checkedToggle)
                                Check.Dispatcher.BeginInvoke((Action) (() => Check.IsChecked = false));
                            SwitchStateTo(State.WaitToCheck);
                            isContinueWait = true;
                            continue;
                        }

                        if (isPause)
                        {
                            settings[Settings.PauseInRelax] = WorkState == State.Relax;
                            SwitchStateTo(State.Pause);
                            settings[Settings.CurrentOffsetToPause] = offsetTime;
                            isContinueWait = true;
                            continue;
                        }

                        Label.Dispatcher.BeginInvoke((Action)(() =>
                        {
                            Label.Content = (WorkState == State.Work ? "Work: " : "Relax: ") + offsetTime.ToString("h\\:mm\\:ss");
                            Label.Foreground = WorkState == State.Work ? Brushes.CornflowerBlue : Brushes.GreenYellow;
                        }));
                        break;
                    case State.WaitToCheck:
                        Label.Dispatcher.BeginInvoke((Action)(() =>
                        {
                            Label.Content = "WaitToCheck";
                            Label.Foreground = Brushes.DarkOrchid;
                        }));

                        if (checkedToggle)
                        {
                            SwitchStateTo(!(bool)settings[Settings.PauseInRelax] ? State.Relax : State.Work);
                            switchState = false;
                            isContinueWait = true;
                            continue;                  
                        }
                        else
                        {
                            if (DateTime.Now.TimeOfDay - startWaitToCheckStamp > (TimeSpan) settings[Settings.CheckTime])
                            {
                                startWaitToCheckStamp = DateTime.Now.TimeOfDay;
                                if (switchState)
                                {
                                    switchState = false;
                                    isContinueWait = true;
                                    continue;
                                }
                                AttentSelector.getInstance().ShowWindow(Dispatcher);
                                //Console.WriteLine("ATTENTION");
                            }
                        }

                        break;
                    case State.Pause:
                        Label.Dispatcher.BeginInvoke((Action)(() =>
                        {
                            Label.Content = "Pause";
                            Label.Foreground = Brushes.Brown;
                        }));
                        

                        if (!isPause)
                        {
                            SwitchStateTo((bool)settings[Settings.PauseInRelax] ? State.Relax : State.Work);
                            settings[Settings.StartTime] = DateTime.Now - (TimeSpan)settings[Settings.CurrentOffsetToPause];
                            isContinueWait = true;
                            continue;
                        }
                        break;

                }
                Dispatcher.BeginInvoke((Action)(() => { Topmost = false; }));
                Dispatcher.BeginInvoke((Action)(() => { Topmost = true; }));
            }
        }

        private void SwitchStateTo(State workState)
        {
            WorkState = workState;
            settings[Settings.StartTime] = DateTime.Now;
            checkedToggle = false;
        }

        protected override void OnClosed(EventArgs e)
        {

            base.OnClosed(e);
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {            
            int offset = 3;
            switch (e.Key)
            {
                case Key.Left:
                    Left -= offset;
                    break;
                case Key.Right:
                    Left += offset;
                    break;
                case Key.Up:
                    Top -= offset;
                    break;
                case Key.Down:
                    Top += offset;
                    break;
            }
            Properties.Settings.Default["LeftOffset"] = Left.ToString();
            Properties.Settings.Default["TopOffset"] = Top.ToString();
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            Properties.Settings.Default.Save();
        }

        private void Check_Checked(object sender, RoutedEventArgs e)
        {
            checkedToggle = true;
            
            lock (monitor)   
                Monitor.PulseAll(monitor);
        }

        private void PauseResume_Click(object sender, RoutedEventArgs e)
        {
        
            if (WorkState == State.WaitToCheck)
                return;

            isPause = !isPause;
            PauseResume.Header = isPause ? "Resume" : "Pause";
            
            lock (monitor)   
                Monitor.PulseAll(monitor);
            
        }

        private void SwitchWorkRelax_OnClick(object sender, RoutedEventArgs e)
        {
            if (WorkState == State.WaitToCheck || WorkState == State.Pause || switchState)
                return;

            switchState = true;
            
            lock (monitor)   
                Monitor.PulseAll(monitor);
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Environment.Exit(Environment.ExitCode);
        }

    }
}
