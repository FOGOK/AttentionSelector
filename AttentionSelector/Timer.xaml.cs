using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

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
                Left = int.Parse(Properties.Settings.Default["LeftOffset"].ToString());
                Top = int.Parse(Properties.Settings.Default["TopOffset"].ToString());
            } catch (Exception e) { }

            settings[Settings.StartTime] = DateTime.Now;
            settings[Settings.RelaxTime] = new TimeSpan(0, int.Parse(GetValueFromConfig("relaxMinutes")), 0);
            settings[Settings.WorkTime] = new TimeSpan(0, int.Parse(GetValueFromConfig("workMinutes")), 0);

            settings[Settings.CheckTime] = new TimeSpan(0, int.Parse(GetValueFromConfig("waitForCheckMinutes")), 0);
            WorkState = State.Work;

            AttentSelector.getInstance().Init();

            new Thread(MainLogic).Start();
        }

        private void MainLogic()
        {
            TimeSpan offsetTime = DateTime.Now.TimeOfDay;
            TimeSpan startWaitToCheckStamp = DateTime.Now.TimeOfDay;
            while (true)
            {
                Thread.Sleep(1000);
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
                            SwitchStateTo(State.WaitToCheck);
                            Check.Dispatcher.BeginInvoke((Action) (() => Check.IsChecked = false));
                            continue;
                        }

                        if (isPause)
                        {
                            settings[Settings.PauseInRelax] = WorkState == State.Relax;
                            SwitchStateTo(State.Pause);
                            settings[Settings.CurrentOffsetToPause] = offsetTime;
                            continue;
                        }

                        Check.Dispatcher.BeginInvoke((Action)(() => Check.IsChecked = true));

                        Label.Dispatcher.BeginInvoke((Action)(() =>
                        {
                            Label.Content = (WorkState == State.Work ? "Work: " : "Relax: ") + offsetTime.ToString("g").Split('.')[0];
                            Label.Foreground = WorkState == State.Work ? Brushes.CornflowerBlue : Brushes.GreenYellow;
                        }));
                        break;
                    case State.WaitToCheck:
                        Label.Dispatcher.BeginInvoke((Action)(() =>
                        {
                            Label.Content = "Check";
                            Label.Foreground = Brushes.DarkOrchid;
                        }));

                        if (checkedToggle)
                        {
                            SwitchStateTo(!(bool)settings[Settings.PauseInRelax] ? State.Relax : State.Work);                      
                        }
                        else
                        {
                            if (DateTime.Now.TimeOfDay - startWaitToCheckStamp > (TimeSpan) settings[Settings.CheckTime])
                            {
                                startWaitToCheckStamp = DateTime.Now.TimeOfDay;
                                if (switchState)
                                {
                                    switchState = false;
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
                            Label.Content = "IS Pause";
                            Label.Foreground = Brushes.Brown;
                        }));
                        

                        if (!isPause)
                        {
                            SwitchStateTo((bool)settings[Settings.PauseInRelax] ? State.Relax : State.Work);
                            settings[Settings.StartTime] = DateTime.Now - (TimeSpan)settings[Settings.CurrentOffsetToPause];
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
        }

        private void PauseResume_Click(object sender, RoutedEventArgs e)
        {
            if (WorkState == State.WaitToCheck)
                return;

            isPause = !isPause;
            PauseResume.Header = isPause ? "Resume" : "Pause";
        }

        private void SwitchWorkRelax_OnClick(object sender, RoutedEventArgs e)
        {
            if (WorkState == State.WaitToCheck || WorkState == State.Pause || switchState)
                return;

            switchState = true;
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Environment.Exit(Environment.ExitCode);
        }

    }
}
