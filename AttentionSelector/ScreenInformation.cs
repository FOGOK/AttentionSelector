using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace AttentionSelector
{
  public class ScreenInformation
  {
    [StructLayout(LayoutKind.Sequential)]
    public struct ScreenRect
    {
      public int left;
      public int top;
      public int right;
      public int bottom;
    }

    [DllImport("user32")]
    private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lpRect, MonitorEnumProc callback, int dwData);

    private delegate bool MonitorEnumProc(IntPtr hDesktop, IntPtr hdc, ref ScreenRect pRect, int dwData);

    public class WpfScreen
    {
      public WpfScreen(ScreenRect prect)
      {
        metrics = prect;
      }

      public ScreenRect metrics;
    }

    static LinkedList<WpfScreen> allScreens = new LinkedList<WpfScreen>();

    public static LinkedList<WpfScreen> GetAllScreens()
    {
      ScreenInformation.GetMonitorCount();
      return allScreens;
    }

    public static int GetMonitorCount()
    {
      allScreens.Clear();
      int monCount = 0;
      MonitorEnumProc callback = (IntPtr hDesktop, IntPtr hdc, ref ScreenRect prect, int d) =>
      {
        Console.WriteLine("Left {0}", prect.left);
        Console.WriteLine("Right {0}", prect.right);
        Console.WriteLine("Top {0}", prect.top);
        Console.WriteLine("Bottom {0}", prect.bottom);
        allScreens.AddLast(new WpfScreen(prect));
        return ++monCount > 0;
      };

      if (EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, callback, 0))
        Console.WriteLine("You have {0} monitors", monCount);
      else
        Console.WriteLine("An error occured while enumerating monitors");

      return monCount;
    }
  }
}
