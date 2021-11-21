using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public List<double> ElectronConcentration = new List<double>(); //Electron concentration
        public List<double> HoleConcentration = new List<double>(); //Hole concentration
        public List<double> Sigma = new List<double>(); //Conductivity
        public List<double> Resistivity = new List<double>(); //Resistivity
        public List<double> Nc = new List<double>();
        public List<double> Nv = new List<double>();
    }
}
