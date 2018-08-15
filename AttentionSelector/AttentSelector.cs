using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace AttentionSelector
{
    public class AttentSelector
    {
        private static AttentSelector instance;

        private AttentSelector()
        { }
        

        public static AttentSelector getInstance()
        {
            if (instance == null)
                instance = new AttentSelector();
            return instance;
        }
        
        private List<MainWindow> windows;

        public void Init()
        {
            LinkedList<ScreenInformation.WpfScreen> screens = ScreenInformation.GetAllScreens();
            windows = new List<MainWindow>();
            foreach (var screen in screens)
            {
                var window = new MainWindow();

                Console.WriteLine("Metrics {0} {1}", screen.metrics.top, screen.metrics.left);

                window.Top = screen.metrics.top;
                window.Left = screen.metrics.left;
                window.WindowStyle = WindowStyle.None;
                windows.Add(window);
            }
        }

        public void ShowWindow(Dispatcher dispatcher)
        {

            foreach (var w in windows)
                dispatcher.Invoke(() =>
                {
                    w.Topmost = true;
                    w.Show();
                    w.WindowState = WindowState.Maximized;
                });

            Thread.Sleep(1000);

            foreach (var w in windows)
            {
                dispatcher.Invoke(() => w.Hide());
                
            }

            GC.Collect();
            
            //thread = new Thread(() =>
            //{
            //    Thread.CurrentThread.IsBackground = true;
            //    int max = timeout / 100;
            //    int secs = max;
            //    while (!stopThread)
            //    {
            //        Thread.Sleep(1000);
            //        if (!Pause)
            //            secs++;
            //        if (secs < max)
            //            continue;
            //        secs = 0;


            //    }
            //});
            //thread.Start();
        }
    }
}