using Spotify2.Class;
using Spotify2.InputLogic;
using System.Windows.Threading;

namespace Spotify2.Other
{
    public class AntiRecoilManager
    {
        public DispatcherTimer HoldDownTimer = new();
        public int IndependentMousePress = 0;

        public void HoldDownLoad()
        {
            if (HoldDownTimer != null)
            {
                HoldDownTimer.Tick += new EventHandler(HoldDownTimerTicker!);
                HoldDownTimer.Interval = TimeSpan.FromMilliseconds(1);
            }
        }

        private void HoldDownTimerTicker(object sender, EventArgs e)
        {
            IndependentMousePress += 1;
            if (IndependentMousePress >= Dictionary.AntiRecoilSettings["Hold Time"])
            {
                MouseManager.DoAntiRecoil();
            }
        }
    }
}