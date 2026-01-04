using Spotify2.Class;
using Spotify2.MouseMovementLibraries.GHubSupport;
using MouseMovementLibraries.ArduinoSupport;
using Spotify2.MouseMovementLibraries.RazerSupport;
using Spotify2.MouseMovementLibraries.SendInputSupport;
using Spotify2.WinformsReplacement;
using System.Drawing;
using System.Runtime.InteropServices;

namespace Spotify2.InputLogic
{
    internal class MouseManager
    {
        private static readonly double ScreenWidth = WinAPICaller.ScreenWidth;
        private static readonly double ScreenHeight = WinAPICaller.ScreenHeight;

        private static DateTime LastClickTime = DateTime.MinValue;
        private static int LastAntiRecoilClickTime = 0;

        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;
        private const uint MOUSEEVENTF_MOVE = 0x0001;
        
        // Make these thread-safe too since they're shared between UI and AI threads
        private static long _previousXBits = 0;
        private static long _previousYBits = 0;
        
        private static double previousX
        {
            get => BitConverter.Int64BitsToDouble(Interlocked.Read(ref _previousXBits));
            set => Interlocked.Exchange(ref _previousXBits, BitConverter.DoubleToInt64Bits(value));
        }
        
        private static double previousY
        {
            get => BitConverter.Int64BitsToDouble(Interlocked.Read(ref _previousYBits));
            set => Interlocked.Exchange(ref _previousYBits, BitConverter.DoubleToInt64Bits(value));
        }
        
        // Use Interlocked with long conversion for lock-free thread safety
        private static long _smoothingFactorBits = BitConverter.DoubleToInt64Bits(0.65);
        private static int _isEMASmoothingEnabled = 0; // 0 = false, 1 = true
        
        public static double smoothingFactor
        {
            get => BitConverter.Int64BitsToDouble(Interlocked.Read(ref _smoothingFactorBits));
            set => Interlocked.Exchange(ref _smoothingFactorBits, BitConverter.DoubleToInt64Bits(value));
        }
        
        public static bool IsEMASmoothingEnabled
        {
            get => Interlocked.CompareExchange(ref _isEMASmoothingEnabled, 0, 0) == 1;
            set
            {
                int newValue = value ? 1 : 0;
                int oldValue = Interlocked.Exchange(ref _isEMASmoothingEnabled, newValue);
                
                // Reset EMA state when toggling from enabled to disabled
                if (oldValue == 1 && newValue == 0)
                {
                    previousX = 0;
                    previousY = 0;
                }
            }
        }

        [DllImport("user32.dll")]
        private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, int dwExtraInfo);

        private static readonly Random MouseRandom = new();
        public static ArduinoInput arduinoMouse = new();
        
        private static double EmaSmoothing(double previousValue, double currentValue, double smoothingFactor) 
            => currentValue * smoothingFactor + previousValue * (1 - smoothingFactor);

        public static async Task DoTriggerClick()
        {
            int timeSinceLastClick = (int)(DateTime.UtcNow - LastClickTime).TotalMilliseconds;
            int triggerDelayMilliseconds = (int)(Dictionary.sliderSettings["Auto Trigger Delay"] * 1000);
            const int clickDelayMilliseconds = 20;

            if (timeSinceLastClick < triggerDelayMilliseconds && LastClickTime != DateTime.MinValue)
            {
                return;
            }

            string mouseMovementMethod = Dictionary.dropdownState["Mouse Movement Method"];
            Action mouseDownAction, mouseUpAction;

            (mouseDownAction, mouseUpAction) = GetMouseActions(mouseMovementMethod);

            mouseDownAction.Invoke();
            await Task.Delay(clickDelayMilliseconds);
            mouseUpAction.Invoke();

            LastClickTime = DateTime.UtcNow;

            static (Action, Action) GetMouseActions(string method)
            {
                return method switch
                {
                    "SendInput" => (
                        () => SendInputMouse.SendMouseCommand(MOUSEEVENTF_LEFTDOWN),
                        () => SendInputMouse.SendMouseCommand(MOUSEEVENTF_LEFTUP)
                    ),
                    "Arduino" => (
                        () => arduinoMouse.SendMouseCommand(0, 0, 1),
                        () => arduinoMouse.SendMouseCommand(0, 0, 0)
                    ),
                    "LG HUB" => (
                        () => LGMouse.Move(1, 0, 0, 0),
                        () => LGMouse.Move(0, 0, 0, 0)
                    ),
                    "Razer Synapse (Require Razer Peripheral)" => (
                        () => RZMouse.mouse_click(1),
                        () => RZMouse.mouse_click(0)
                    ),
                    _ => (
                        () => mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0),
                        () => mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0)
                    )
                };
            }
        }

        public static void DoAntiRecoil()
        {
            int timeSinceLastClick = Math.Abs(DateTime.UtcNow.Millisecond - LastAntiRecoilClickTime);
            int fireRate = Dictionary.AntiRecoilSettings["Fire Rate"];

            if (timeSinceLastClick < fireRate)
            {
                return;
            }

            int xRecoil = (int)Dictionary.AntiRecoilSettings["X Recoil (Left/Right)"];
            int yRecoil = (int)Dictionary.AntiRecoilSettings["Y Recoil (Up/Down)"];
            string mouseMovementMethod = Dictionary.dropdownState["Mouse Movement Method"];

            switch (mouseMovementMethod)
            {
                case "SendInput":
                    SendInputMouse.SendMouseCommand(MOUSEEVENTF_MOVE, xRecoil, yRecoil);
                    break;
                case "Arduino":
                    arduinoMouse.SendMouseCommand(xRecoil, yRecoil, 0);
                    break;
                case "LG HUB":
                    LGMouse.Move(0, xRecoil, yRecoil, 0);
                    break;
                case "Razer Synapse (Require Razer Peripheral)":
                    RZMouse.mouse_move(xRecoil, yRecoil, true);
                    break;
                default:
                    mouse_event(MOUSEEVENTF_MOVE, (uint)xRecoil, (uint)yRecoil, 0, 0);
                    break;
            }

            LastAntiRecoilClickTime = DateTime.UtcNow.Millisecond;
        }

        public static void MoveCrosshair(int detectedX, int detectedY)
        {
            // Cache dictionary lookups at the start
            double mouseSensitivity = Dictionary.sliderSettings["Mouse Sensitivity (+/-)"];
            int mouseJitter = (int)Dictionary.sliderSettings["Mouse Jitter"];
            string movementPath = Dictionary.dropdownState["Movement Path"];
            string mouseMovementMethod = Dictionary.dropdownState["Mouse Movement Method"];
            bool autoTrigger = Dictionary.toggleState["Auto Trigger"];
            
            bool emaEnabled = IsEMASmoothingEnabled;
            double cachedSmoothingFactor = smoothingFactor;
            double cachedPreviousX = previousX;
            double cachedPreviousY = previousY;

            int halfScreenWidth = (int)ScreenWidth / 2;
            int halfScreenHeight = (int)ScreenHeight / 2;

            int targetX = detectedX - halfScreenWidth;
            int targetY = detectedY - halfScreenHeight;

            double aspectRatioCorrection = ScreenWidth / ScreenHeight;

            int jitterX = MouseRandom.Next(-mouseJitter, mouseJitter);
            int jitterY = MouseRandom.Next(-mouseJitter, mouseJitter);

            Point start = new(0, 0);
            Point end = new(targetX, targetY);
            Point newPosition;

            switch (movementPath)
            {
                case "Cubic Bezier":
                    Point control1 = new(start.X + (end.X - start.X) / 3, start.Y + (end.Y - start.Y) / 3);
                    Point control2 = new(start.X + 2 * (end.X - start.X) / 3, start.Y + 2 * (end.Y - start.Y) / 3);
                    newPosition = MovementPaths.CubicBezier(start, end, control1, control2, 1 - mouseSensitivity);
                    break;
                case "Exponential":
                    newPosition = MovementPaths.Exponential(start, end, 1 - (mouseSensitivity - 0.2), 2.7);
                    break;
                case "Adaptive":
                    newPosition = MovementPaths.Adaptive(start, end, 1 - mouseSensitivity);
                    break;
                case "Smoothstep":
                    newPosition = MovementPaths.Smoothstep(start, end, 1 - mouseSensitivity);
                    break;
                default:
                    newPosition = MovementPaths.Lerp(start, end, 1 - mouseSensitivity);
                    break;
            }

            if (emaEnabled && cachedSmoothingFactor > 0 && cachedSmoothingFactor <= 1)
            {
                double smoothedX = EmaSmoothing(cachedPreviousX, newPosition.X, cachedSmoothingFactor);
                double smoothedY = EmaSmoothing(cachedPreviousY, newPosition.Y, cachedSmoothingFactor);
                
                // Make sure smoothed values aren't null or Infinity
                if (!double.IsNaN(smoothedX) && !double.IsInfinity(smoothedX) &&
                    !double.IsNaN(smoothedY) && !double.IsInfinity(smoothedY))
                {
                    newPosition.X = (int)smoothedX;
                    newPosition.Y = (int)smoothedY;
                    
                    previousX = smoothedX;
                    previousY = smoothedY;
                }
            }

            // Clamp and apply aspect ratio correction
            targetX = Math.Clamp(newPosition.X, -150, 150);
            targetY = Math.Clamp(newPosition.Y, -150, 150);
            
            targetY = (int)(targetY * aspectRatioCorrection);

            targetX += jitterX;
            targetY += jitterY;

            switch (mouseMovementMethod)
            {
                case "SendInput":
                    SendInputMouse.SendMouseCommand(MOUSEEVENTF_MOVE, targetX, targetY);
                    break;

                case "Arduino":
                    arduinoMouse.SendMouseCommand(targetX, targetY, 0);
                    break;

                case "LG HUB":
                    LGMouse.Move(0, targetX, targetY, 0);
                    break;

                case "Razer Synapse (Require Razer Peripheral)":
                    RZMouse.mouse_move(targetX, targetY, true);
                    break;

                default:
                    mouse_event(MOUSEEVENTF_MOVE, (uint)targetX, (uint)targetY, 0, 0);
                    break;
            }

            if (autoTrigger)
            {
                _ = DoTriggerClick();
            }
        }
    }
}