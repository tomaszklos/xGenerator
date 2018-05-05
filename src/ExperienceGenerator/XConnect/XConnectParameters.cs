using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExperienceGenerator.XConnect
{
    class XConnectParameters
    {
        decimal Identified { get; set; }
        decimal BounceRate { get; set; }
        int VisitCount { get; set; }

        List<DayOfWeek> DayOfWeek { get; set; }
    }
    public class DayOfWeek 
    {
        string Name { get; set; }
        decimal Value { get; set; }

    }
}
