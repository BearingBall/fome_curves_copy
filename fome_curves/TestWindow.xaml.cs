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
using ScottPlot.Styles;
using Color = System.Drawing.Color;

namespace fome_curves
{
    public partial class TestWindow : Window
    {
        Random rand = new Random(0);
        private ScatterPlot MyScatterPlot;
        private readonly ScatterPlot HighlightedPoint;
        double _T = 600;

        #region MaterialList

        private List<string> materials = new List<string>()
        {
            "Si",
            "Ge",
            "GaAs"
        };

        private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string selected_item = ((sender as ListBox).SelectedItem as string);
            semiconductor = semiconductors[(sender as ListBox).SelectedIndex];

            recalculateEverything();
        }

        #endregion


        public double Tmin
        {
            get => parameters.TMin;
            set
            {
                if (value >= Tmax)
                    return;

                parameters.TMin = value;
                recalculateEverything();
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
                recalculateEverything();
            }
        }

        public double dT
        {
            get => parameters.TStep;
            set
            {
                if (value >= Tmax - Tmin || value < 1)
                    return;

                parameters.TStep = value;
                recalculateEverything();
            }
        }

        public double T
        {
            get => _T;
            set
            {
                _T = value;
                recalculateEverything();
            }
        }

        public double Nd
        {
            get => parameters.Nd0 / 1e15;
            set
            {
                parameters.Nd0 = value * 1e15;
                recalculateEverything();
            }
        }

        public double Na
        {
            get => parameters.Na0 / 1e15;
            set
            {
                parameters.Na0 = value * 1e15;
                recalculateEverything();
            }
        }

        public double Ea
        {
            get => parameters.Ea;
            set
            {
                parameters.Ea = value;
                recalculateEverything();
            }
        }

        public float MarkerSize
        {
            get { return DataPlotter.MarkerSize;}
            set { DataPlotter.MarkerSize = value; }
        }

        public double Ed
        {
            get => parameters.Ed;
            set
            {
                parameters.Ed = value;
            }
        }

        public bool logYAxis = false;
        public bool logXAxis = false;
        Output fillArrays(Semiconductor semiconductor)
        {
            Output output = new Output();
            int count = 0;
            for (double t = parameters.TMin; t <= parameters.TMax; t += parameters.TStep)
            {
                output.T.Add(t);
                output.Nc.Add(PhysicsCalculations.getEffectiveDensityState(semiconductor.me, t));
                output.Nv.Add(PhysicsCalculations.getEffectiveDensityState(semiconductor.mh, t));
                var Ef = PhysicsCalculations.getFermi(output.Nc[count], output.Nv[count], t, parameters.Na0,
                    parameters.Nd0,
                    semiconductor.Eg, parameters.Ea, parameters.Ed);
                output.ElectronConcentration.Add(PhysicsCalculations.getN(output.Nc[count], semiconductor.Eg, Ef, t));
                output.HoleConcentration.Add(PhysicsCalculations.getP(output.Nv[count], Ef, t));
                output.DonorConcentration.Add(PhysicsCalculations.getNdPlus(parameters.Ed, parameters.Nd0,
                    parameters.Ed, Ef, t));
                output.Na.Add(PhysicsCalculations.getNaMinus(parameters.Na0, parameters.Ea, Ef, t));

                output.ElectronsMobility.Add(PhysicsCalculations.getMobility(semiconductor.Ae, semiconductor.Be,
                    parameters.Nd0, parameters.Na0, t));
                output.HolesMobility.Add(PhysicsCalculations.getMobility(semiconductor.Ah, semiconductor.Bh,
                    parameters.Nd0, parameters.Na0, t));
                output.Sigma.Add(PhysicsCalculations.getConductivity(output.ElectronConcentration[count],
                    output.HoleConcentration[count], output.ElectronsMobility[count], output.HolesMobility[count]));
                ++count;
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

        Output fillHolesFromTemperature(Semiconductor semiconductor)
        {
            Output output = new Output();
            int count = 0;

            for (double t = parameters.TMin; t <= parameters.TMax; t += parameters.TStep)
            {
                output.T.Add(t);
                output.Nc.Add(PhysicsCalculations.getEffectiveDensityState(semiconductor.me, t));
                output.Nv.Add(PhysicsCalculations.getEffectiveDensityState(semiconductor.mh, t));
                var Ef = PhysicsCalculations.getFermi(output.Nc[count], output.Nv[count], t, parameters.Na0,
                    parameters.Nd0,
                    semiconductor.Eg, parameters.Ea, parameters.Ed);
                output.Na.Add(PhysicsCalculations.getNaMinus(parameters.Na0, parameters.Ea, Ef, t));
                output.HoleConcentration.Add(PhysicsCalculations.getP(output.Nv[count], Ef, t));
                ++count;
            }

            return output;
        }

        Output fillHolesFromNa(Semiconductor semiconductor, double T)
        {
            Output output = new Output();
            int count = 0;
            double dN = 1e18;
            double from = 1e18;
            double to = 1e20;

            for (double n = from; n <= to; n += dN)
            {
                output.Na.Add(n);
                output.Nc.Add(PhysicsCalculations.getEffectiveDensityState(semiconductor.me, T));
                output.Nv.Add(PhysicsCalculations.getEffectiveDensityState(semiconductor.mh, T));

                var Ef = PhysicsCalculations.getFermi(output.Nc[count], output.Nv[count], T, n,
                    parameters.Nd0,
                    semiconductor.Eg, parameters.Ea, parameters.Ed);

                output.HoleConcentration.Add(PhysicsCalculations.getP(output.Nv[count], Ef, T));
                ++count;
            }
            
            return output;
        }

        Output fillMobilityAcceptorConcentration(Semiconductor semiconductor, double T, double dN = 1e18,
            double from = 1e10, double to = 1e20)
        {
            Output output = new Output();
            double n = from;
            while (n <= to)
            {
                output.Na.Add(n);

                output.ElectronsMobility.Add(PhysicsCalculations.getMobility(semiconductor.Ae, semiconductor.Be,
                    parameters.Nd0, n, T));
                output.HolesMobility.Add(PhysicsCalculations.getMobility(semiconductor.Ah, semiconductor.Bh,
                    parameters.Nd0, n, T));

                if (n < 1e16)
                {
                    n += lerp(from, 1e16, n, 1e10, 5e15);// dN / 10;
                }
                else if (n < 1e18)
                {
                    n += lerp(1e16, 1e18, n, 5e15, 1e18);
                }
                else
                {
                    n += lerp(1e18, to, n, 1e18, 5e18);
                }
            }

            return output;
        }

        Output fillConductivity(Semiconductor semiconductor, double T, double dN = 1e18, double from = 1e10,
            double to = 1e20)
        {
            Output output = new Output();
            int count = 0;
            double n = from;
            while (n <= to)//for (double n = from; n <= to; n += dN)
            {
                output.Na.Add(n);
                
                output.Nc.Add(PhysicsCalculations.getEffectiveDensityState(semiconductor.me, T));
                output.Nv.Add(PhysicsCalculations.getEffectiveDensityState(semiconductor.mh, T));
                var Ef = PhysicsCalculations.getFermi(output.Nc[count], output.Nv[count], T, n, parameters.Nd0,
                    semiconductor.Eg, parameters.Ea, parameters.Ed);
                output.ElectronConcentration.Add(PhysicsCalculations.getN(output.Nc[count], semiconductor.Eg, Ef, T));
                output.HoleConcentration.Add(PhysicsCalculations.getP(output.Nv[count], Ef, T));
                
                output.ElectronsMobility.Add(PhysicsCalculations.getMobility(semiconductor.Ae, semiconductor.Be,
                    parameters.Nd0, n, T));
                output.HolesMobility.Add(PhysicsCalculations.getMobility(semiconductor.Ah, semiconductor.Bh,
                    parameters.Nd0, n, T));
                output.Sigma.Add(PhysicsCalculations.getConductivity(output.ElectronConcentration[count], output.HoleConcentration[count],
                    output.ElectronsMobility[count], output.HolesMobility[count]));
                //output.Sigma.Add(Ef);
                
                count++;

                if (n < 1e16)
                {
                    n += lerp(from, 1e16, n, 1e10, 1e15);// dN / 10;
                }
                else if (n < 1e18)
                {
                    n += lerp(1e16, 1e18, n, 1e15, 1e17);
                }
                else
                {
                    n += lerp(1e18, to, n, 1e17, 1e19);
                }
            }

            return output;
        }

        double lerp(double a, double b, double val, double from, double to)
        {
            double progress = (val - a) / (b - a);
            return from + (to - from) * progress;
        }
        Output fillResistivity(Semiconductor semiconductor, double T, double dN = 1e18, double from = 1e10,
            double to = 1e20)
        {
            var output = fillConductivity(semiconductor, T, dN, from, to);

            for (int i = 0; i < output.Sigma.Count; i++)
            {
                output.Resistivity.Add(1.0 / output.Sigma[i]);
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

        private List<Semiconductor> semiconductors;
        private Semiconductor semiconductor;
        Params parameters = new Params();

        private Dictionary<int, Action> refreshers;

        private ScottPlot.Styles.IStyle style = ScottPlot.Style.Seaborn;
        IStyle[] plotStyles = ScottPlot.Style.GetStyles();

        public TestWindow()
        {
            //lol without it everything falls apart
            CultureInfo.CurrentCulture = new CultureInfo("en-US", false);

            InitializeComponent();

            Tabs.FixedHeaderCount = Tabs.Items.Count;

            refreshers = new Dictionary<int, Action>()
            {
                {0, recalculateMobilityNa},
                {1, recalculateMobilityT},
                {2, recalculateResistivity},
                {3, recalculateConductivity},
                {4, recalculateHoleConcentrationT},
                {5, recalculateHoleConcentrationNa},
            };

            setStyle(style);

            semiconductors = new List<Semiconductor>()
            {
                readSemiconductor("Semiconductors/Si.json"),
                readSemiconductor("Semiconductors/Ge.json"),
                readSemiconductor("Semiconductors/GaAs.json"),
            };
            semiconductor = semiconductors[0];

            recalculateMobilityNa();
            recalculateMobilityT();
            recalculateHoleConcentrationT();
            recalculateConductivity();
            recalculateResistivity();

            ApplyBtn.Click += ApplyBtnOnClick;

            MaterialBox.SelectionMode = SelectionMode.Single;
            MaterialBox.ItemsSource = materials;
            MaterialBox.SelectedIndex = 0;

            LogY.Checked += switchYAxisLog;
            LogY.Unchecked += switchYAxisLog;

            LogX.Checked += switchXAxisLog;
            LogX.Unchecked += switchXAxisLog;

            Tabs.SelectionChanged += TabsOnSelectionChanged;
            Tabs.SelectedIndex = 0;

            recalculateEverything();

            for (int i = 0; i < plotStyles.Length; i++)
            {
                StylesCombo.Items.Add(plotStyles[i].ToString());
            }

            StylesCombo.SelectedIndex = plotStyles.Length - 1;
            StylesCombo.SelectionChanged += StylesComboOnSelectionChanged;
        }

        private void StylesComboOnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            setStyle(plotStyles[StylesCombo.SelectedIndex]);
        }

        private void ApplyBtnOnClick(object sender, RoutedEventArgs e)
        {
            recalculateEverything();
        }

        private void setStyle(IStyle style)
        {
            wpfPlot1.Plot.Style(style);
            wpfPlot2.Plot.Style(style);
            wpfPlot3.Plot.Style(style);
            wpfPlot4.Plot.Style(style);
            wpfPlot5.Plot.Style(style);
            wpfPlot6.Plot.Style(style);
        }

        private void TabsOnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            recalculateEverything();
        }

        private void switchYAxisLog(object sender, RoutedEventArgs e)
        {
            if (LogY.IsChecked != null)
            {
                logYAxis = LogY.IsChecked.Value;
                recalculateEverything();
            }
        }
        private void switchXAxisLog(object sender, RoutedEventArgs e)
        {
            if (LogX.IsChecked != null)
            {
                logXAxis = LogX.IsChecked.Value;
                recalculateEverything();
            }
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
            DataPlotter.plotData(
                new PlotData()
                {
                    xData = res.Na.ToArray(), yData = res.Resistivity.ToArray(), xLabel = "Na, 1/cm³",
                    yLabel = "Resistance, Om * cm"
                }, wpfPlot3, logYAxis, logXAxis);

            
            DataPlotter.refresh(wpfPlot3);
        }

        private void recalculateConductivity()
        {
            var res = fillConductivity(semiconductor, _T, from: 1e10);

            DataPlotter.clear(wpfPlot4);
            DataPlotter.plotData(
                new PlotData()
                {
                    xData = res.Na.ToArray(), yData = res.Sigma.ToArray(), xLabel = "Na, 1/cm³",
                    yLabel = "Conductivity, *"
                }, wpfPlot4, logYAxis, logXAxis);

            DataPlotter.refresh(wpfPlot4);
        }

        private void recalculateMobilityNa()
        {
            var res = fillMobilityAcceptorConcentration(semiconductor, _T);

            DataPlotter.clear(wpfPlot1);
            DataPlotter.plotData(
                new PlotData()
                {
                    xData = res.Na.ToArray(), yData = res.HolesMobility.ToArray(), xLabel = "Na, 1/cm³",
                    yLabel = "Hole Mobility, SM²/Vs"
                }, wpfPlot1, logYAxis, logXAxis);

            DataPlotter.refresh(wpfPlot1);
        }

        private void recalculateMobilityT()
        {
            var res = fillMobilityFromTemperature(semiconductor);

            DataPlotter.clear(wpfPlot2);
            DataPlotter.plotData(
                new PlotData()
                {
                    xData = res.T.ToArray(), yData = res.HolesMobility.ToArray(), xLabel = "T, K",
                    yLabel = "Hole Mobility, SM²/Vs"
                }, wpfPlot2, logYAxis, logXAxis);

            //wpfPlot1.Plot.Add(plottable);
            //wpfPlot1.Plot.XAxis.ManualTickPositions(new[] { 1e15, 1e19, 1e20 }, new[] { "1e15", "1e19", "1e20" });
            DataPlotter.refresh(wpfPlot2);
        }

        static string customTickFormatter(double position)
        {
            return position.ToString("0.0000E0");
            
        }

        private void recalculateHoleConcentrationT()
        {
            var res = fillHolesFromTemperature(semiconductor);

            DataPlotter.clear(wpfPlot5);
            DataPlotter.plotData(
                new PlotData()
                    {xData = res.T.ToArray(), yData = res.HoleConcentration.ToArray(), xLabel = "T, K", yLabel = "Holes, 1/cm³"},
                wpfPlot5, logYAxis, logXAxis);

            DataPlotter.refresh(wpfPlot5);

            wpfPlot5.Plot.YAxis.TickLabelFormat(customTickFormatter);
        }
        private void recalculateHoleConcentrationNa()
        {
            var res = fillHolesFromNa(semiconductor, _T);

            DataPlotter.clear(wpfPlot6);
            DataPlotter.plotData(
                new PlotData()
                    { xData = res.Na.ToArray(), yData = res.HoleConcentration.ToArray(), xLabel = "Na, 1/cm³", yLabel = "Holes, 1/cm³" },
                wpfPlot6, logYAxis, logXAxis);

            DataPlotter.refresh(wpfPlot6);

            wpfPlot6.Plot.YAxis.TickLabelFormat(customTickFormatter);
            wpfPlot6.Plot.XAxis.TickLabelFormat(customTickFormatter);
        }

        void recalculateEverything()
        {
            
            int id = Tabs.SelectedIndex;
            if(refreshers.TryGetValue(id, out var action))
            {
                action();
            }
            
            //recalculateMobilityT();
            //recalculateHoleConcentrationT();
            //recalculateMobilityNa();
            //recalculateConductivity();
            //recalculateResistivity();
            //recalculateHoleConcentrationNa();
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

            if (double.TryParse(TBox?.Text, out result) && result != T)
            {
                T = result;
            }

            if (double.TryParse(NaBox?.Text, out result) && result != Na)
            {
                Na = result;
            }

            if (double.TryParse(EaBox?.Text, out result) && result != Ea)
            {
                Ea = result;
            }
            
        }

        private void SettingsTextOnChange(object sender, TextChangedEventArgs e)
        {
            if (double.TryParse(MarkerSizeBox?.Text, out var result) && result != DataPlotter.MarkerSize)
            {
                DataPlotter.MarkerSize = (float) result;
            }

            if (double.TryParse(EdBox?.Text, out result) && result != Ed)
            {
                Ed = result;
            }

            if (double.TryParse(NdBox?.Text, out result) && result != Nd)
            {
                Nd = result;
            }
        }
    }
}