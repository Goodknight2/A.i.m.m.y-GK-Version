using System;
using System.Threading;
using Vector2.Class;
using InputLogic;

namespace Other
{
    public class AntiRecoilManager : IDisposable
    {
        private Thread? _workerThread;
        private volatile bool _isActive = false;
        private volatile bool _shouldStop = false;
        private int _holdTimeMs = 0;
        private readonly int holdThreshold = 15;

        public void Start()
        {
            if (_isActive) return;
            
            _isActive = true;
            _shouldStop = false;
            _holdTimeMs = 0;
            
            _workerThread = new Thread(WorkerLoop)
            {
                IsBackground = true,
                Priority = ThreadPriority.Highest
            };
            _workerThread.Start();
        }

        public void Stop()
        {
            if (!_isActive) return;
            
            _shouldStop = true;
            _isActive = false;
            
            _workerThread?.Join(TimeSpan.FromMilliseconds(100));
            _workerThread = null;
        }

        private void WorkerLoop()
        {
            var nextTick = Environment.TickCount64;
            
            while (!_shouldStop)
            {
                int moveDelay = (int)Dictionary.sliderSettings["Move Delay"];
                nextTick += moveDelay;
                long now = Environment.TickCount64;
                long sleepTime = nextTick - now;
                
                if (sleepTime > 0)
                    Thread.Sleep((int)sleepTime);
                else
                    nextTick = Environment.TickCount64; // Catch up if we fell behind
                
                if (_shouldStop) break;
                
                _holdTimeMs += moveDelay;

                if (_holdTimeMs >= holdThreshold)
                {
                    int xRecoil = (int)Dictionary.sliderSettings["X Recoil (Left/Right)"];
                    int yRecoil = (int)Dictionary.sliderSettings["Y Recoil (Up/Down)"];
                    MouseManager.DoAntiRecoil(xRecoil, yRecoil);
                }
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}