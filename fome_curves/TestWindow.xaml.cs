using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using fome_curves.Physics;
using ScottPlot;
using ScottPlot.Plottable;
using System.Text.Json;
using fome_curves.PlotTools;
using Color = System.Drawing.Color;

namespace fome_curves
{
    class Output
    {
        public List<double> T = new List<double>(); //temperature
        public List<double> ElectronsMobility = new List<double>(); //electron mobility
        public List<double> HolesMobility = new List<double>(); //holes mobility
        public List<double> Nd = new List<double>(); //
        public List<double> Na = new List<double>();
        public List<double> DonorConcentration = new List<double>(); //Charged donor concentration
        public List<double> AcceptorConcentration = new List<double>(); //Charged acceptor concentration
        public List<double> ElectronConcentration = new List<double>(); //Electron concentration
        public List<double> HoleConcentration = new List<double>(); //Hole concentration
        public List<double> Sigma = new List<double>(); //Conductivity
        public List<double> Resistivity = new List<double>(); //Resistivity
        public List<double> Nc = new List<double>();
        public List<double> Nv = new List<double>();
    }

    public partial class TestWindow : Window
    {
        Random rand = new Random(0);
        private ScatterPlot MyScatterPlot;
        private readonly ScatterPlot HighlightedPoint;
        int _T = 600;

        public double Tmin
        {
          get => parameters.TMin;
          set
          {
            if (value >= Tmax)
              return;

            parameters.TMin = value;
            recalculateMobilityT();
          }
        }

        public double Tmax
        {
          get => parameters.TMax;
          set
          {
            if (value <= Tmin)
              return;

            parameters.TMax = value;
            recalculateMobilityT();
          }
        }

        public double dT
        {
          get => parameters.TStep;
          set
          {
            if (value >= Tmax - Tmin)
              return;

            parameters.TStep = value;
            recalculateMobilityT();
          }
        }

    public int T
        {
            get => _T;
            set
            {
                _T = value;
                recalculateMobilityNa();
            }
        }

        public double Nd
        {
            get => parameters.Nd0 / 1e15;
            set
            {
                parameters.Nd0 = value * 1e15;
                recalculateMobilityNa();
                recalculateMobilityT();
            }
        }
        public double Na
        {
            get => parameters.Na0 / 1e15;
            set
            {
                parameters.Na0 = value * 1e15;
                recalculateMobilityNa();
                recalculateMobilityT();
            }
        }
        Output fillArrays(Semiconductor semiconductor)
        {
            Output output = new Output();
            int count = 0;
            for (double t = parameters.TMin; t <= parameters.TMax; t += parameters.TStep)
            {
                output.T.Add(t);
                output.Nc.Add(PhysicsCalculations.getEffectiveDensityState(semiconductor.me, t));
                output.Nv.Add(PhysicsCalculations.getEffectiveDensityState(semiconductor.mh, t));
                var Ef = PhysicsCalculations.getFermi(output.Nc[count], output.Nv[count], t, parameters.Na0, parameters.Nd0,
                    semiconductor.Eg, parameters.Ea, parameters.Ed);
                output.ElectronConcentration.Add(PhysicsCalculations.getN(output.Nc[count], semiconductor.Eg, Ef, t));
                output.HoleConcentration.Add(PhysicsCalculations.getP(output.Nv[count], Ef, t));
                output.DonorConcentration.Add(PhysicsCalculations.getNdPlus(parameters.Ed, parameters.Nd0, parameters.Ed, Ef, t));
                output.AcceptorConcentration.Add(PhysicsCalculations.getNaMinus(parameters.Na0, parameters.Ea, Ef, t));

                output.ElectronsMobility.Add(PhysicsCalculations.getMobility(semiconductor.Ae, semiconductor.Be, parameters.Nd0, parameters.Na0, t));
                output.HolesMobility.Add(PhysicsCalculations.getMobility(semiconductor.Ah, semiconductor.Bh, parameters.Nd0, parameters.Na0, t));
                output.Sigma.Add(PhysicsCalculations.getConductivity(output.ElectronConcentration[count], output.HoleConcentration[count], output.ElectronsMobility[count], output.HolesMobility[count]));
                count++;
            }

            return output;
        }

        Output fillMobilityFromTemperature(Semiconductor semiconductor)
        {
            Output output = new Output();
            for (double t = parameters.TMin; t <= parameters.TMax; t += parameters.TStep)
            {
                output.T.Add(t);
                output.ElectronsMobility.Add(PhysicsCalculations.getMobility(semiconductor.Ae, semiconductor.Be,
                    parameters.Nd0, parameters.Na0, t));
                output.HolesMobility.Add(PhysicsCalculations.getMobility(semiconductor.Ah, semiconductor.Bh,
                    parameters.Nd0, parameters.Na0, t));
            }

            return output;
        }
        Output fillMobilityDonorConcentration(Semiconductor semiconductor, double T, double dN = 1e16, double from = 1e15, double to = 1e20)
        {
            Output output = new Output();
            for (double n = from; n <= to; n += dN)
            {
                output.Nd.Add(n);
                
                output.ElectronsMobility.Add(PhysicsCalculations.getMobility(semiconductor.Ae, semiconductor.Be,
                    n, parameters.Na0, T));
                output.HolesMobility.Add(PhysicsCalculations.getMobility(semiconductor.Ah, semiconductor.Bh,
                    n, parameters.Na0, T));
            }

            return output;
        }
        Output fillMobilityAcceptorConcentration(Semiconductor semiconductor, double T, double dN = 1e18, double from = 1e10, double to = 1e20)
        {
            Output output = new Output();
            for (double n = from; n <= to; n += dN)
            {
                output.Na.Add(n);

                output.ElectronsMobility.Add(PhysicsCalculations.getMobility(semiconductor.Ae, semiconductor.Be,
                    parameters.Nd0, n, T));
                output.HolesMobility.Add(PhysicsCalculations.getMobility(semiconductor.Ah, semiconductor.Bh,
                    parameters.Nd0, n, T));
            }

            return output;
        }
        Output fillConductivity(Semiconductor semiconductor, double T, double dN = 1e18, double from = 1e10, double to = 1e20)
        {
            Output output = new Output();
            int count = 0;
            for (double n = from; n <= to; n += dN)
            {
                output.Na.Add(n);
                output.Nc.Add(PhysicsCalculations.getEffectiveDensityState(semiconductor.me, T));
                output.Nv.Add(PhysicsCalculations.getEffectiveDensityState(semiconductor.mh, T));
                var Ef = PhysicsCalculations.getFermi(output.Nc[count], output.Nv[count], T, n, parameters.Nd0,
                    semiconductor.Eg, parameters.Ea, parameters.Ed);
                output.ElectronConcentration.Add(PhysicsCalculations.getN(output.Nc[count], semiconductor.Eg, Ef, T));
                output.HoleConcentration.Add(PhysicsCalculations.getP(output.Nv[count], Ef, T));

                output.ElectronsMobility.Add(PhysicsCalculations.getMobility(semiconductor.Ae, semiconductor.Be, parameters.Nd0, n, T));
                output.HolesMobility.Add(PhysicsCalculations.getMobility(semiconductor.Ah, semiconductor.Bh, parameters.Nd0, n, T));
                output.Sigma.Add(PhysicsCalculations.getConductivity(output.ElectronConcentration[count], output.HoleConcentration[count], output.ElectronsMobility[count], output.HolesMobility[count]));
                count++;
            }

            return output;
        }

        Output fillResistivity(Semiconductor semiconductor, double T, double dN = 1e18, double from = 1e10,
            double to = 1e20)
        {
            var output = fillConductivity(semiconductor, T, dN, from, to);

            for (int i = 0; i < output.Sigma.Count; i++)
            {
                output.Resistivity.Add(1.0/output.Sigma[i]);
            }

            return output;
        }

        //TODO handle exceptions
            void saveSemiconductorProperties(Semiconductor semiconductor, string path)
        {
            string jsonString = JsonSerializer.Serialize(semiconductor);
            File.WriteAllText(path, jsonString);
        }

        //TODO handle exceptions
        Semiconductor readSemiconductor(string path)
        {
            string jsonString = File.ReadAllText(path);
            var semiconductor = JsonSerializer.Deserialize<Semiconductor>(jsonString);
            return semiconductor;
        }

        private Semiconductor semiconductor;
        Params parameters = new Params();

        public TestWindow()
        {
            //lol without it everything falls apart
            CultureInfo.CurrentCulture = new CultureInfo("en-US", false);

            InitializeComponent();

            wpfPlot1.Plot.Style(ScottPlot.Style.Seaborn);
            wpfPlot2.Plot.Style(ScottPlot.Style.Seaborn);
            wpfPlot3.Plot.Style(ScottPlot.Style.Seaborn);
            wpfPlot4.Plot.Style(ScottPlot.Style.Seaborn);

            semiconductor = readSemiconductor("Semiconductors/Si.json");

            recalculateMobilityNa();
            recalculateMobilityT();
            recalculateConductivity();
            recalculateResistivity();
        }

        (double[], string[]) generatelabels(double from, int steps)
        {
            double[] values = new double[steps];
            string[] labels = new string[steps];

            for (int i = 0; i < steps; i++)
            {
                double current = Math.Pow(10, from + i);
                values[i] = current;
                labels[i] = current.ToString("0.E0");
            }

            return (values, labels);
        }
        private void recalculateResistivity()
        {
            var res = fillResistivity(semiconductor, _T);

            DataPlotter.clear(wpfPlot3);
            DataPlotter.plotData(new PlotData() { xData = res.Na.ToArray(), yData = res.Resistivity.ToArray(), xLabel = "Na, 1/cm³", yLabel = "Resistance, Om * cm" }, wpfPlot3);

            wpfPlot3.Plot.XAxis.ManualTickPositions(new double[] { 1e10, 1e20 }, new string[] { "1e10", "1e20" });
            DataPlotter.refresh(wpfPlot3);
        }
        private void recalculateConductivity()
        {
            var res = fillConductivity(semiconductor, _T);

            DataPlotter.clear(wpfPlot4);
            DataPlotter.plotData(new PlotData() { xData = res.Na.ToArray(), yData = res.Sigma.ToArray(), xLabel = "Na, 1/cm³", yLabel = "Conductivity, *" }, wpfPlot4);

            wpfPlot4.Plot.XAxis.ManualTickPositions(new double[] { 1e10, 1e20 }, new string[] { "1e10", "1e20" });
            DataPlotter.refresh(wpfPlot4);
        }
        private void recalculateMobilityNa()
        {
            var res = fillMobilityAcceptorConcentration(semiconductor, _T);

            DataPlotter.clear(wpfPlot1);
            DataPlotter.plotData(new PlotData() {xData = res.Na.ToArray(), yData = res.ElectronsMobility.ToArray(), xLabel = "Na, 1/cm³", yLabel = "Mobility, SM²/Vs" }, wpfPlot1);
          
            wpfPlot1.Plot.XAxis.ManualTickPositions(new double[]{1e10, 1e20}, new string[]{"1e10", "1e20"});
            DataPlotter.refresh(wpfPlot1);
        }
        private void recalculateMobilityT()
        {
            var res = fillMobilityFromTemperature(semiconductor);

            DataPlotter.clear(wpfPlot2);
            DataPlotter.plotData(new PlotData() { xData = res.T.ToArray(), yData = res.ElectronsMobility.ToArray(), xLabel = "T, K", yLabel = "Mobility, SM²/Vs" }, wpfPlot2);

            //wpfPlot1.Plot.Add(plottable);
            //wpfPlot1.Plot.XAxis.ManualTickPositions(new[] { 1e15, 1e19, 1e20 }, new[] { "1e15", "1e19", "1e20" });
            DataPlotter.refresh(wpfPlot2);
        }

        private void wpfPlot1_MouseMove(object sender, MouseEventArgs e)
        {
        }

        private void TextBoxBase_OnTextChanged(object sender, TextChangedEventArgs e)
        {
          if (double.TryParse(TmaxBox?.Text, out double result) && result != Tmax)
          {
            Tmax = result;
          }

          if (double.TryParse(TminBox?.Text, out result) && result != Tmin)
          {
            Tmin = result;
          }

          if (double.TryParse(dTBox?.Text, out result) && result != dT)
          {
            dT = result;
          }
        }
    }
}