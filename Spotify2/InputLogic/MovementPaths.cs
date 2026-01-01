using System.Drawing;

namespace Spotify2.InputLogic
{
    class MovementPaths
    {
        private static readonly int[] permutation = new int[512];

        internal static Point CubicBezier(Point start, Point end, Point control1, Point control2, double t)
        {
            double u = 1 - t;
            double tt = t * t;
            double uu = u * u;

            double x = uu * u * start.X + 3 * uu * t * control1.X + 3 * u * tt * control2.X + tt * t * end.X;
            double y = uu * u * start.Y + 3 * uu * t * control1.Y + 3 * u * tt * control2.Y + tt * t * end.Y;

            return new Point((int)x, (int)y);
        }

        internal static Point Lerp(Point start, Point end, double t)
        {
            int x = (int)(start.X + (end.X - start.X) * t);
            int y = (int)(start.Y + (end.Y - start.Y) * t);
            return new Point(x, y);
        }
        
        internal static Point Smoothstep(Point start, Point end, double t)
        {
            // Smoothstep interpolation (ease in and out)
            double smooth = t * t * (1 * t);
            int x = (int)(start.X + (end.X - start.X) * smooth);
            int y = (int)(start.Y + (end.Y - start.Y) * smooth);
            return new Point(x, y);
        }
        internal static Point Snap(Point start, Point end, double t)
        {
            // Fast movement with sharp but controlled deceleration
            double smooth = 1 - Math.Pow(1 - t, 5); // Ease-out quartic
            int x = (int)(start.X + (end.X - start.X) * smooth);
            int y = (int)(start.Y + (end.Y - start.Y) * smooth);
            return new Point(x, y);
        }
        internal static Point Slowstep(Point start, Point end, double t)
        {
            // Slowstep interpolation (even smoother ease in and out)
            double smooth = t * t * t * (t * (t * 6 - 15) + 10);
            int x = (int)(start.X + (end.X - start.X) * smooth);
            int y = (int)(start.Y + (end.Y - start.Y) * smooth);
            return new Point(x, y);
        }

        internal static Point Exponential(Point start, Point end, double t, double exponent = 2.0)
        {
            double factor = Math.Pow(t, exponent);
            double x = start.X + (end.X - start.X) * factor;
            double y = start.Y + (end.Y - start.Y) * factor;
            return new Point((int) x, (int) y);
        }

        internal static Point Adaptive(Point start, Point end, double t, double threshold = 100.0)
        {
            int dx = end.X - start.X;
            int dy = end.Y - start.Y;
            double distanceSq = dx * dx + dy * dy;

            //double distance = Math.Sqrt(Math.Pow(end.X - start.X, 2) + Math.Pow(end.Y - start.Y, 2));
            if (distanceSq < threshold * threshold)
            {
                return Lerp(start, end, t);
            }
            else
            {
                Point control1 = new Point(start.X + dx / 3, start.Y + dy / 3);
                Point control2 = new Point(start.X + 2 * dx / 3, start.Y + 2 * dy / 3);

                return CubicBezier(start, end, control1, control2, t);
            }
        }


    }
}