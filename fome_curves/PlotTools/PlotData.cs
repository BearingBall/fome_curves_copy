using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fome_curves
{
    class PlotData
    {
        public double[] xData { get; set; }
        public double[] yData { get; set; }

        public string xLabel { get; set; } = "";
        public string yLabel { get; set; } = "";
    }
}
