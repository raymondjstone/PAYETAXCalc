using System;
using System.Collections.Generic;

namespace PAYETAXCalc.Models
{
    public class WindowSettings
    {
        public int X { get; set; } = -1;
        public int Y { get; set; } = -1;
        public int Width { get; set; } = 1100;
        public int Height { get; set; } = 800;
    }

    public class AppData
    {
        public WindowSettings Window { get; set; } = new();
        public List<TaxYearData> TaxYears { get; set; } = new();
        public bool BuyMeCoffeeClicked { get; set; } = false;
        public DateTimeOffset? LastCoffeePrompt { get; set; }
        public DateTimeOffset? FirstAppUse { get; set; }
    }
}
