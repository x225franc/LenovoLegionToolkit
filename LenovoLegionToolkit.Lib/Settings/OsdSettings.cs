using System;
using System.Collections.Generic;
using System.Linq;
using LenovoLegionToolkit.Lib.Utils;

namespace LenovoLegionToolkit.Lib.Settings;

public class OsdSettings() : AbstractSettings<OsdSettings.OsdSettingsStore>("osd.json")
{
    public class OsdSettingsStore
    {
        public bool ShowOsd { get; set; }
        public double OsdRefreshInterval { get; set; } = 1;
        public int SelectedStyleIndex { get; set; } = 0;
        public List<OsdItem> Items { get; set; } = Enum.GetValues<OsdItem>().ToList();

        public double BackgroundOpacity { get; set; } = 0.6;
        public string BackgroundColor { get; set; } = "#1E1E1E";
        public int FontSize { get; set; } = 12;
        public int CornerRadiusTop { get; set; } = 6;
        public int CornerRadiusBottom { get; set; } = 6;
        public bool IsLocked { get; set; } = false;
        public double? PanelPositionX { get; set; }
        public double? PanelPositionY { get; set; }
        public double? BarPositionX { get; set; }
        public double? BarPositionY { get; set; }

        public int TempThresholdWarning { get; set; } = 75;
        public int TempThresholdCritical { get; set; } = 90;
        public int UsageThresholdWarning { get; set; } = 70;
        public int UsageThresholdCritical { get; set; } = 90;
        public int FpsThresholdCritical { get; set; } = 30;
        public int LowFpsDeltaThreshold { get; set; } = 30;

        // CategoryColor used to color every category header ("— FPS —", "— CPU —", ...) the same - it is now only
        // the Game header's color, kept under its original name so a customized value carries over without a
        // migration step. The other four headers get their own color, starting at the same default (#2196F3).
        public string CategoryColor { get; set; } = "#2196F3";
        public string CpuCategoryColor { get; set; } = "#2196F3";
        public string GpuCategoryColor { get; set; } = "#2196F3";
        public string MemoryCategoryColor { get; set; } = "#2196F3";
        public string MotherboardCategoryColor { get; set; } = "#2196F3";
        public string LabelColor { get; set; } = "#ADFF2F";
        public string ValueColor { get; set; } = "#FFFFFF";
        public string WarningColor { get; set; } = "#FFFF00";
        public string CriticalColor { get; set; } = "#FF0000";
        public int SnapThreshold { get; set; } = 20;

        /// <summary>Top-to-bottom (panel style) / left-to-right (bar style) order of the category groups.</summary>
        public List<OsdCategory> CategoryOrder { get; set; } = [OsdCategory.Game, OsdCategory.Cpu, OsdCategory.Gpu, OsdCategory.Memory, OsdCategory.Motherboard];
    }

    /// <summary>The configured category order, guaranteed to contain each <see cref="OsdCategory"/> exactly once -
    /// defensive against a hand-edited or future settings file missing one or repeating another. The single home
    /// for this logic - the OSD windows and the settings window all read the order through here.</summary>
    public List<OsdCategory> GetCategoryOrder()
    {
        var order = new List<OsdCategory>();
        foreach (var category in Store.CategoryOrder)
            if (!order.Contains(category)) order.Add(category);
        foreach (var category in Enum.GetValues<OsdCategory>())
            if (!order.Contains(category)) order.Add(category);
        return order;
    }
}
