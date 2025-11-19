using System.Timers;

namespace AutoGenCrudLib.Views;

public enum TimeDir
{
    Uptime,
    Downtime
}

public class TimerView : ContentView
{
    public Label Display = new();
    public System.Timers.Timer Timer;
    public TimeSpan Current;
    public event EventHandler<TimeSpan> Tick; 
    public TimeDir Dir { get; set; } = TimeDir.Uptime;


    public TimerView()
    { 
        Content = Display;
        Timer = new System.Timers.Timer(1000);
        Timer.Elapsed += (s,e) =>
        {
            if (Dir == TimeDir.Uptime) Current = Current.Add(TimeSpan.FromSeconds(1));
            else Current = Current.Subtract(TimeSpan.FromSeconds(1));
            MainThread.BeginInvokeOnMainThread(() => UpdateLabel());
            Tick?.Invoke(this, Current);
        };
    }

    private void UpdateLabel()
    {
        Display.Text = Current.ToString(@"hh\:mm\:ss");
    }
}
