namespace Zebble.UWP
{
    using System;
    using Services;

    [EscapeGCop("Hardcoded values are fine here.")]
    partial class Inspector
    {
        /// <summary>
        /// Gets the current width of the inspector.
        /// </summary>
        public int CurrentWidth => InspectionBox?.Ignored == false ? InspectionBox.WIDTH : 0;

        static void CreateStyles()
        {
            CssEngine.Add("InspectionBox", s =>
            {
                s.Ignored = true;
                s.Border.Left = 3;
                s.Border.Color = "#999";
                s.BackgroundColor = "#333";
                s.Width(InspectionBox.WIDTH);
            });

            CssEngine.Add("InspectionBox #HeaderBar", s => { s.BackgroundColor = "#555"; s.Height(30); });

            CssEngine.Add("InspectionBox ScrollView TextView", s => s.Font(12).Padding(2));
            CssEngine.Add("InspectionBox ScrollView TextView.toggle-icon", s => { s.Width(12).Margin(right: 3); });

            CssEngine.Add("InspectionBox PropertiesList CheckBox", s => s.Size(14));
            CssEngine.Add("InspectionBox PropertiesList OptionsList-Option #Label", x => x.TextColor("#eee"));

            CssEngine.Add("InspectionBox TextView", s => s.TextColor = "#ddd");

            CssEngine.Add("InspectionBox CheckBox", s => s.Border(1, color: Colors.Gray).Background(Colors.Transparent));
            CssEngine.Add("InspectionBox CheckBox #CheckedImage", s => s.Ignored());
            CssEngine.Add("InspectionBox CheckBox:checked", s => s.Background(Colors.LightGray));
        }
    }
}