using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fome_curves.Physics
{
    class PhysicsCalculations
    {
        const double ElectronVoltInErgs = 1.60218e-12;
        const double VOLT_IN_CGS = 1.0 / 300;

        public static double electronVoltToErg(double electron_volt)
        {
            return electron_volt * ElectronVoltInErgs;
        }
        public static double ergToElectronVolt(double erg)
        {
            return erg / ElectronVoltInErgs;
        }
        //--
        public static double voltToCgs(double volt)
        {
            return volt * VOLT_IN_CGS;
        }
        public static double cgsToVolt(double cgs)
        {
            return cgs / VOLT_IN_CGS;
        }
        //--
        public static double getEffectiveDensityState(double m, double T)
        {
            return 2.51e19 * Math.Pow((m * T / (Constants.m0 * 300.0)), 1.5);
        }

        public static double getN(double Nc, double Eg, double Ef, double T)
        {
            return Nc * Math.Exp((Ef - Eg) / (Constants.k * T));
        }

        public static double getP(double Nv, double Ef, double T)
        {
            return Nv * Math.Exp((-Ef) / (Constants.k * T));
        }

        public static double getNaMinus(double Na0, double Ea, double Ef, double T)
        {
            return Na0 / (1 + Math.Exp((Ea - Ef) / (Constants.k * T)));
        }

        public static double getNdPlus(double Eg, double Nd0, double Ed, double Ef, double T)
        {
            return Nd0 / (1 + Math.Exp((Eg - Ef - Ed) / (Constants.k * T)));
        }

        public static double getConductivity(double n, double p, double mue, double mup)
        {
            return Constants.e * (n * mue + p * mup) * 100;
        }

        public static double func(double Ef, double Nc, double Nv, double T, double Na0, double Nd0, double Eg,
            double Ea, double Ed)
        {
            double n = getN(Nc, Eg, Ef, T);
            double p = getP(Nv, Ef, T);
            double NdPlus = getNdPlus(Eg, Nd0, Ed, Ef, T);
            double NaMinus = getNaMinus(Na0, Ea, Ef, T);
            return NdPlus + p - n - NaMinus;
        }

        public static double getFermi(double Nc, double Nv, double T, double Na0, double Nd0, double Eg, double Ea,
            double Ed)
        {
            double left = 0;
            double right = 10.0;
            double middle = (left + right) / 2.0;
            double fm = func(middle, Nc, Nv, T, Na0, Nd0, Eg, Ea, Ed);
            double iterations = 0;
            while (Math.Abs(fm) > 1 && iterations < 1000)
            {
                double fleft = func(left, Nc, Nv, T, Na0, Nd0, Eg, Ea, Ed);
                double fright = func(right, Nc, Nv, T, Na0, Nd0, Eg, Ea, Ed);
                if (fleft * fm < 0)
                {
                    right = middle;
                }
                else if (fright * fm < 0)
                {
                    left = middle;
                }
                else
                {
                    break;
                }

                middle = (left + right) / 2.0;
                fm = func(middle, Nc, Nv, T, Na0, Nd0, Eg, Ea, Ed);

                ++iterations;
            }

            //console.log(middle, fm, Nc, Nv, T, Na0, Nd0, Eg, Ea, Ed);
            return middle;
        }

        public static double getMobility(double a, double b, double NdPlus, double NaMinus, double T)
        {
            return a / (Math.Pow(T, 1.5) + b * (NdPlus + NaMinus) / Math.Pow(T, 1.5));
        }
    }
}