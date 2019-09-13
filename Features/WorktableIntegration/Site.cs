using System;
using Tecan.Sila2;

namespace Tecan.Sila2.WorktableIntegration
{
    public class Site
    {
        public Site(string name, string template, Offset offset, double rotation)
        {
            Name = name;
            Template = template;
            Offset = offset;
            Rotation = rotation;
        }

        public string Name { get; }

        public string Template { get; }

        public Offset Offset { get; }
        
        [Unit("Degree", Factor = 180 / Math.PI)]
        public double Rotation { get; }
    }
}
