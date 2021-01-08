using System;

namespace IMMULIS
{
     public class CountdownTimer
     {
          public CountdownTimer(int Duration)
          {
               duration = Duration;
               remainingDuration = duration;
               timer.AutoReset = true;
               timer.Elapsed += new System.Timers.ElapsedEventHandler(Count_down);
               timer.Start();
          }

          public CountdownTimer(int Duration, EventHandler handler)
          {
               duration = Duration;
               remainingDuration = duration;
               timer.AutoReset = true;
               timer.Elapsed += new System.Timers.ElapsedEventHandler(Count_down);
               timer.Start();
               if (handler != null)
               {
                    Timeout += handler;
               }
          }

          public void Reset()
          {
               timer.Stop();
               remainingDuration = duration;
               timer.Start();
          }
          public void Reset(int NewDuration)
          {
               duration = NewDuration;
               remainingDuration = duration;
          }
          private int duration;
          public int remainingDuration;
          private readonly System.Timers.Timer timer = new System.Timers.Timer(1000);
          public event EventHandler Timeout;
          public void OnTimeout()
          {
               Timeout?.Invoke(this, EventArgs.Empty);
          }

          private void Count_down(object sender, EventArgs e)
          {
               /* If the countdown hits 0, trigger the Timeout event.
               *  If the timer hasn't expired, decrement remaining duration.
               *  Handling only these two conditions allows us to leave the timer running.
               *  That means we can use the Reset function whenever we want
               *  to set the timer without having to worry about starting it again.
               */
               if (remainingDuration == 0)
               {
                    remainingDuration--;
                    OnTimeout();
               }
               else if (remainingDuration > 0)
               {
                    remainingDuration--;
               }
          }
     }
}
