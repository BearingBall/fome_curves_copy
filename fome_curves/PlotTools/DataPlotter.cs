using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ScottPlot;

namespace fome_curves.PlotTools
{
    class DataPlotter
    {
        public static void clear(WpfPlot plot)
        {
            plot.Plot.Clear();
        }

        public static void refresh(WpfPlot plot)
        {
            plot.Refresh();
        }

        public static void plotData(PlotData data, WpfPlot plot, bool logY = false, bool logX = false, int xTicks = 10, int yTicks = 10)
        {
            var maxY = data.yData.Max();
            var minY = data.yData.Min();

            var maxX = data.xData.Max();
            var minX = data.xData.Min();

            plot.Plot.AddScatter(logX ? Tools.Log10(data.xData) : data.xData, logY ? Tools.Log10(data.yData) : data.yData, markerSize: 3);

            plot.Plot.XLabel(data.xLabel);
            plot.Plot.YLabel(data.yLabel);

            List<double> xPositions = new List<double>();
            List<string> xLabels = new List<string>();

            for (int i = 0; i < xTicks; ++i)
            {
                double pos = logX ? Math.Pow(10, lerp(0, xTicks - 1, i, Math.Log10(minX), Math.Log10(maxX))) : minX + (maxX - minX) * i / (xTicks - 1);
                xPositions.Add(logX? Math.Log10(pos) : pos);
                xLabels.Add(pos.ToString("0.00E0"));
            }

            List<double> yPositions = new List<double>();
            List<string> yLabels = new List<string>();
            for (int i = 0; i < yTicks; ++i)
            {
                double pos = logY ? Math.Pow(10, lerp(0, yTicks - 1, i, Math.Log10(minY), Math.Log10(maxY))) : minY + (maxY - minY) * i / (yTicks - 1);

                yPositions.Add(logY ? Math.Log10(pos) : pos);
                yLabels.Add(pos.ToString("0.00E0"));
            }

            plot.Plot.YAxis.ManualTickPositions(yPositions.ToArray(), yLabels.ToArray());
            plot.Plot.XAxis.ManualTickPositions(xPositions.ToArray(), xLabels.ToArray());
        }
        static double lerp(double a, double b, double val, double from, double to)
        {
            double progress = (val - a) / (b - a);
            return from + (to - from) * progress;
        }
    }
}