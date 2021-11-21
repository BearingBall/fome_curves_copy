using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fome_curves.Physics
{
    class Semiconductor
    {
        //EV
        public double Eg { get; set; }
        public double Ae { get; set; }
        public double Be { get; set; }

        public double Ah { get; set; }
        public double Bh { get; set; }
        public double me { get; set; }
        public double mh { get; set; }
    }

    class Params
    {
        public double Ed { get; set; } = 0.1;
        public double Nd0 { get; set; } = 1e16;
        public double Ea { get; set; } = 0.1;
        public double Na0 { get; set; } = 0;
        public double TStep { get; set; } = 10;
        public double TMax { get; set; } = 1000;
        public double TMin { get; set; } = 200;
    }
}
