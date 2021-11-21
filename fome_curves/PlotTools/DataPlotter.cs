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

        public static void plotData(PlotData data, WpfPlot plot, bool logY = false)
        {
            plot.Plot.AddScatter(data.xData, logY ? Tools.Log10(data.yData) : data.yData);

            plot.Plot.XLabel(data.xLabel);
            plot.Plot.YLabel(data.yLabel);
        }
    }
}