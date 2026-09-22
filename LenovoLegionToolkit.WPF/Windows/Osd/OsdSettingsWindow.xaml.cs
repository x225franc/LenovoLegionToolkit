using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Controllers.Sensors;
using LenovoLegionToolkit.Lib.Extensions;
using LenovoLegionToolkit.Lib.Messaging;
using LenovoLegionToolkit.Lib.Messaging.Messages;
using LenovoLegionToolkit.Lib.Settings;
using LenovoLegionToolkit.Lib.Utils;
using LenovoLegionToolkit.WPF.Controls;
using LenovoLegionToolkit.WPF.Controls.Custom;
using LenovoLegionToolkit.WPF.Extensions;
using LenovoLegionToolkit.WPF.Resources;

namespace LenovoLegionToolkit.WPF.Windows.Osd;

public class OsdItemGroup
{
    public string Header { get; set; } = string.Empty;
    public List<OsdItem> Items { get; set; } = new List<OsdItem>();
}

public partial class OsdSettingsWindow
{
    private readonly OsdSettings _OsdSettings = IoCContainer.Resolve<OsdSettings>();
    private readonly SensorsGroupController _controller = IoCContainer.Resolve<SensorsGroupController>();
    private bool _isInitializing = true;

    public OsdSettingsWindow()
    {
        InitializeComponent();
        this.Loaded += OsdSettingsWindow_Loaded;
    }

    private void OsdSettingsWindow_Loaded(object sender, RoutedEventArgs e)
    {
        InitializeCheckboxes();

        _osdRefreshInterval.Value = _OsdSettings.Store.OsdRefreshInterval;
        _osdStyleComboBox.SelectedIndex = _OsdSettings.Store.SelectedStyleIndex;

        _osdOpacitySlider.Value = _OsdSettings.Store.BackgroundOpacity;
        _opacityValueText.Text = $"{(_OsdSettings.Store.BackgroundOpacity * 100):0}{Resource.Percent}";

        _osdFontSize.Value = _OsdSettings.Store.FontSize;

        _osdCornerRadiusTopSlider.Value = _OsdSettings.Store.CornerRadiusTop;
        _osdCornerRadiusBottomSlider.Value = _OsdSettings.Store.CornerRadiusBottom;
        _cornerRadiusValueText.Text = $"{_OsdSettings.Store.CornerRadiusTop} / {_OsdSettings.Store.CornerRadiusBottom}";

        _osdLockPosition.IsChecked = _OsdSettings.Store.IsLocked;

        try
        {
            var color = (Color)ColorConverter.ConvertFromString(_OsdSettings.Store.BackgroundColor);
            _osdBackgroundColorPicker.SelectedColor = color;
        }
        catch
        {
            _osdBackgroundColorPicker.SelectedColor = Color.FromRgb(0x1E, 0x1E, 0x1E);
        }

        _tempWarning.Value = _OsdSettings.Store.TempThresholdWarning;
        _tempCritical.Value = _OsdSettings.Store.TempThresholdCritical;
        _usageWarning.Value = _OsdSettings.Store.UsageThresholdWarning;
        _usageCritical.Value = _OsdSettings.Store.UsageThresholdCritical;
        _fpsCritical.Value = _OsdSettings.Store.FpsThresholdCritical;
        _lowFpsDelta.Value = _OsdSettings.Store.LowFpsDeltaThreshold;
        _osdSnapThreshold.Value = _OsdSettings.Store.SnapThreshold;

        BuildCategoryColorRows();
        _labelColorPicker.SelectedColor = GetColorFromHex(_OsdSettings.Store.LabelColor) ?? Colors.Transparent;
        _valueColorPicker.SelectedColor = GetColorFromHex(_OsdSettings.Store.ValueColor) ?? Colors.Transparent;
        _warningColorPicker.SelectedColor = GetColorFromHex(_OsdSettings.Store.WarningColor) ?? Colors.Transparent;
        _criticalColorPicker.SelectedColor = GetColorFromHex(_OsdSettings.Store.CriticalColor) ?? Colors.Transparent;

        _isInitializing = false;
    }

    private Color? GetColorFromHex(string? hex)
    {
        if (string.IsNullOrEmpty(hex)) return null;
        try { return (Color)ColorConverter.ConvertFromString(hex); }
        catch { return null; }
    }

    private void InitializeCheckboxes()
    {
        var groups = new List<OsdItemGroup>
        {
            new OsdItemGroup { Header = Resource.Osd_Game, Items =
                [OsdItem.Fps, OsdItem.LowFps, OsdItem.FrameTime]
            },
            new OsdItemGroup { Header = Resource.Osd_Cpu, Items =
                [
                    OsdItem.CpuUtilization, OsdItem.CpuFrequency,
                    OsdItem.CpuTemperature,
                    OsdItem.CpuPower, OsdItem.CpuVoltage,
                    OsdItem.CpuFan
                ]
            },
            new OsdItemGroup { Header = Resource.Osd_Gpu, Items =
                [
                    OsdItem.GpuUtilization, OsdItem.GpuFrequency,
                    OsdItem.GpuTemperature,
                    OsdItem.GpuVramUtilization, OsdItem.GpuVramTemperature,
                    OsdItem.GpuPower, OsdItem.GpuFan
                ]
            },
            new OsdItemGroup { Header = Resource.Osd_Pch, Items =
                [
                    OsdItem.MemoryUtilization, OsdItem.MemoryTemperature,
                    OsdItem.Disk1Temperature, OsdItem.Disk2Temperature,
                    OsdItem.PchTemperature, OsdItem.PchFan
                ]
            }
        };

        var cpuGroup = groups[1];
        if (_controller.IsHybrid)
        {
            int baseFrequencyIndex = cpuGroup.Items.IndexOf(OsdItem.CpuFrequency);
            if (baseFrequencyIndex >= 0)
            {
                cpuGroup.Items.Insert(baseFrequencyIndex + 1, OsdItem.CpuPCoreFrequency);
                cpuGroup.Items.Insert(baseFrequencyIndex + 2, OsdItem.CpuECoreFrequency);
                cpuGroup.Items.Remove(OsdItem.CpuFrequency);
            }
        }

        var activeItems = new HashSet<OsdItem>(_OsdSettings.Store.Items);

        if (activeItems.Count == 0)
        {
            activeItems = new HashSet<OsdItem>(
                _OsdSettings.Store.Items.Cast<OsdItem>()
            );
        }

        bool isFirst = true;

        foreach (var group in groups)
        {
            var headerText = new TextBlock
            {
                Text = group.Header,
                Margin = new Thickness(0, isFirst ? 0 : 20, 0, 8),
                FontWeight = FontWeights.SemiBold,
                FontSize = 14,
                Foreground = (Brush)Application.Current.Resources["TextFillColorPrimaryBrush"]
            };

            _itemsStackPanel.Children.Add(headerText);
            isFirst = false;

            var stackPanel = new StackPanel
            {
                Margin = new Thickness(0, 0, 0, 0)
            };

            foreach (var item in group.Items)
            {
                var checkBox = new CheckBox
                {
                    Content = item.GetDisplayName(),
                    Tag = item,
                    IsChecked = activeItems.Contains(item)
                };
                checkBox.Checked += CheckBox_CheckedOrUnchecked;
                checkBox.Unchecked += CheckBox_CheckedOrUnchecked;

                stackPanel.Children.Add(checkBox);
            }

            _itemsStackPanel.Children.Add(stackPanel);
        }
    }

    // ---- category colors + order ----

    // This list's row names are spelled out in full even where the OSD itself abbreviates them (Motherboard here vs
    // "MB" on the actual overlay) - there is room for the whole word in a settings list, unlike on the overlay.
    private static string GetCategoryHeader(OsdCategory category) => category switch
    {
        OsdCategory.Game => Resource.Osd_Game,
        OsdCategory.Cpu => Resource.Osd_Cpu,
        OsdCategory.Gpu => Resource.Osd_Gpu,
        OsdCategory.Memory => Resource.SensorsControl_Memory_Title,
        OsdCategory.Motherboard => Resource.SensorsControl_Motherboard_Title,
        _ => category.ToString()
    };

    private static Wpf.Ui.Common.SymbolRegular GetCategoryIcon(OsdCategory category) => category switch
    {
        OsdCategory.Game => Wpf.Ui.Common.SymbolRegular.Games24,
        OsdCategory.Cpu => Wpf.Ui.Common.SymbolRegular.BrainCircuit24,
        OsdCategory.Gpu => Wpf.Ui.Common.SymbolRegular.DeveloperBoard24,
        OsdCategory.Memory => Wpf.Ui.Common.SymbolRegular.Ram20,
        OsdCategory.Motherboard => Wpf.Ui.Common.SymbolRegular.Server24,
        _ => Wpf.Ui.Common.SymbolRegular.Grid24
    };

    private string GetCategoryColor(OsdCategory category) => category switch
    {
        OsdCategory.Game => _OsdSettings.Store.CategoryColor,
        OsdCategory.Cpu => _OsdSettings.Store.CpuCategoryColor,
        OsdCategory.Gpu => _OsdSettings.Store.GpuCategoryColor,
        OsdCategory.Memory => _OsdSettings.Store.MemoryCategoryColor,
        OsdCategory.Motherboard => _OsdSettings.Store.MotherboardCategoryColor,
        _ => "#2196F3"
    };

    private void SetCategoryColor(OsdCategory category, string hex)
    {
        switch (category)
        {
            case OsdCategory.Game: _OsdSettings.Store.CategoryColor = hex; break;
            case OsdCategory.Cpu: _OsdSettings.Store.CpuCategoryColor = hex; break;
            case OsdCategory.Gpu: _OsdSettings.Store.GpuCategoryColor = hex; break;
            case OsdCategory.Memory: _OsdSettings.Store.MemoryCategoryColor = hex; break;
            case OsdCategory.Motherboard: _OsdSettings.Store.MotherboardCategoryColor = hex; break;
        }
    }

    /// <summary>Rebuilds the five category rows (name, move up/down, color) in the current order - called again
    /// after every reorder so the row positions and the enabled state of the up/down buttons stay correct.</summary>
    private void BuildCategoryColorRows()
    {
        _categoryColorsPanel.Children.Clear();

        var order = _OsdSettings.GetCategoryOrder();
        for (int i = 0; i < order.Count; i++)
        {
            var category = order[i];
            bool isFirst = i == 0, isLast = i == order.Count - 1;

            var upButton = new Wpf.Ui.Controls.Button
            {
                Content = "▲",
                Width = 32,
                Margin = new Thickness(0, 0, 4, 0),
                IsEnabled = !isFirst
            };
            upButton.Click += (_, _) => MoveCategory(category, -1);

            var downButton = new Wpf.Ui.Controls.Button
            {
                Content = "▼",
                Width = 32,
                Margin = new Thickness(0, 0, 12, 0),
                IsEnabled = !isLast
            };
            downButton.Click += (_, _) => MoveCategory(category, 1);

            var picker = new ColorPickerControl
            {
                SelectedColor = GetColorFromHex(GetCategoryColor(category)) ?? Colors.Transparent
            };
            picker.ColorChangedDelayed += (_, _) =>
            {
                if (_isInitializing || !IsLoaded) return;

                var color = picker.SelectedColor;
                SetCategoryColor(category, $"#{color.R:X2}{color.G:X2}{color.B:X2}");
                _OsdSettings.SynchronizeStore();
                MessagingCenter.Publish(new OsdAppearanceChangedMessage());
            };

            var content = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            content.Children.Add(upButton);
            content.Children.Add(downButton);
            content.Children.Add(picker);

            var card = new CardControl
            {
                Margin = new Thickness(0, 0, 0, 4),
                Icon = GetCategoryIcon(category),
                Header = new TextBlock { Text = GetCategoryHeader(category), Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"] },
                Content = content
            };

            _categoryColorsPanel.Children.Add(card);
        }
    }

    private void MoveCategory(OsdCategory category, int direction)
    {
        var order = _OsdSettings.GetCategoryOrder();
        int index = order.IndexOf(category);
        int newIndex = index + direction;
        if (newIndex < 0 || newIndex >= order.Count) return;

        (order[index], order[newIndex]) = (order[newIndex], order[index]);
        _OsdSettings.Store.CategoryOrder = order;
        _OsdSettings.SynchronizeStore();

        BuildCategoryColorRows();
        MessagingCenter.Publish(new OsdAppearanceChangedMessage());
    }

    private void CheckBox_CheckedOrUnchecked(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;

        var selectedItems = new List<OsdItem>();

        foreach (var stackPanel in _itemsStackPanel.Children.OfType<StackPanel>())
        {
            foreach (var child in stackPanel.Children.OfType<CheckBox>())
            {
                if (child is { IsChecked: true, Tag: OsdItem item })
                {
                    selectedItems.Add(item);
                }
            }
        }

        _OsdSettings.Store.Items = selectedItems;
        _OsdSettings.SynchronizeStore();
        MessagingCenter.Publish(new OsdElementChangedMessage(selectedItems));

        App.Current.OsdWindow?.Dispatcher.BeginInvoke(new Action(() =>
        {
            (App.Current.OsdWindow as OsdWindowBase)?.RecalculatePosition();
        }), DispatcherPriority.Render);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OsdRefreshInterval_ValueChanged(object sender, RoutedEventArgs e)
    {
        if (_isInitializing || !IsLoaded)
            return;

        _OsdSettings.Store.OsdRefreshInterval = _osdRefreshInterval.Value ?? 1.0;
        _OsdSettings.SynchronizeStore();
    }

    private void StyleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing || !IsLoaded)
            return;

        try
        {
            _OsdSettings.Store.SelectedStyleIndex = _osdStyleComboBox.SelectedIndex;
            _OsdSettings.SynchronizeStore();

            if (_OsdSettings.Store.ShowOsd && App.Current.OsdWindow != null)
            {
                var styleTypeMapping = new Dictionary<int, Type>
                {
                    [0] = typeof(OsdPanelWindow),
                    [1] = typeof(OsdBarWindow)
                };

                var constructorMapping = new Dictionary<int, Func<Window>>
                {
                    [0] = () => new OsdPanelWindow(),
                    [1] = () => new OsdBarWindow()
                };

                int selectedStyle = _OsdSettings.Store.SelectedStyleIndex;
                if (styleTypeMapping.TryGetValue(selectedStyle, out Type? targetType) &&
                    App.Current.OsdWindow.GetType() != targetType)
                {
                    var oldOsdPos = new Point(App.Current.OsdWindow.Left, App.Current.OsdWindow.Top);
                    App.Current.OsdWindow.Close();

                    if (constructorMapping.TryGetValue(selectedStyle, out Func<Window>? constructor))
                    {
                        App.Current.OsdWindow = constructor();
                        App.Current.OsdWindow.Left = oldOsdPos.X;
                        App.Current.OsdWindow.Top = oldOsdPos.Y;
                        App.Current.OsdWindow.Show();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Instance.Trace($"StyleComboBox_SelectionChanged error: {ex.Message}");

            _isInitializing = true;
            _osdStyleComboBox.SelectedIndex = _OsdSettings.Store.SelectedStyleIndex;
            _isInitializing = false;
        }
    }

    private void OsdOpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isInitializing || !IsLoaded)
            return;

        _OsdSettings.Store.BackgroundOpacity = _osdOpacitySlider.Value;
        _opacityValueText.Text = $"{(_osdOpacitySlider.Value * 100):0}{Resource.Percent}";
        _OsdSettings.SynchronizeStore();
        MessagingCenter.Publish(new OsdAppearanceChangedMessage());
    }

    private void OsdBackgroundColorPicker_ColorChangedDelayed(object? sender, EventArgs e)
    {
        if (_isInitializing || !IsLoaded)
            return;

        var color = _osdBackgroundColorPicker.SelectedColor;
        _OsdSettings.Store.BackgroundColor = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        _OsdSettings.SynchronizeStore();
        MessagingCenter.Publish(new OsdAppearanceChangedMessage());
    }

    private void OsdSnapThreshold_ValueChanged(object sender, RoutedEventArgs e)
    {
        if (_isInitializing || !IsLoaded) return;
        _OsdSettings.Store.SnapThreshold = (int)(_osdSnapThreshold.Value ?? 20);
        _OsdSettings.SynchronizeStore();
    }

    private void OsdFontSize_ValueChanged(object sender, RoutedEventArgs e)
    {
        if (_isInitializing || !IsLoaded)
            return;

        _OsdSettings.Store.FontSize = (int)(_osdFontSize.Value ?? 12);
        _OsdSettings.SynchronizeStore();
        MessagingCenter.Publish(new OsdAppearanceChangedMessage());

        App.Current.OsdWindow?.Dispatcher.BeginInvoke(new Action(() =>
        {
            (App.Current.OsdWindow as OsdWindowBase)?.RecalculatePosition();
        }), DispatcherPriority.Render);
    }

    private void OsdCornerRadiusTopSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isInitializing || !IsLoaded)
            return;

        _OsdSettings.Store.CornerRadiusTop = (int)_osdCornerRadiusTopSlider.Value;
        _cornerRadiusValueText.Text = $"{(int)_osdCornerRadiusTopSlider.Value} / {(int)_osdCornerRadiusBottomSlider.Value}";
        _OsdSettings.SynchronizeStore();
        MessagingCenter.Publish(new OsdAppearanceChangedMessage());
    }

    private void OsdCornerRadiusBottomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isInitializing || !IsLoaded)
            return;

        _OsdSettings.Store.CornerRadiusBottom = (int)_osdCornerRadiusBottomSlider.Value;
        _cornerRadiusValueText.Text = $"{(int)_osdCornerRadiusTopSlider.Value} / {(int)_osdCornerRadiusBottomSlider.Value}";
        _OsdSettings.SynchronizeStore();
        MessagingCenter.Publish(new OsdAppearanceChangedMessage());
    }

    private void OsdLockPosition_CheckedOrUnchecked(object sender, RoutedEventArgs e)
    {
        if (_isInitializing || !IsLoaded)
            return;

        _OsdSettings.Store.IsLocked = _osdLockPosition.IsChecked ?? false;
        _OsdSettings.SynchronizeStore();
        MessagingCenter.Publish(new OsdAppearanceChangedMessage());
    }

    private void ResetPositionButton_Click(object sender, RoutedEventArgs e)
    {
        _OsdSettings.Store.PanelPositionX = null;
        _OsdSettings.Store.PanelPositionY = null;
        _OsdSettings.Store.BarPositionX = null;
        _OsdSettings.Store.BarPositionY = null;
        _OsdSettings.SynchronizeStore();
        MessagingCenter.Publish(new OsdAppearanceChangedMessage());
    }

    private void Threshold_ValueChanged(object sender, RoutedEventArgs e)
    {
        if (_isInitializing || !IsLoaded) return;

        _OsdSettings.Store.TempThresholdWarning = (int)(_tempWarning.Value ?? 75);
        _OsdSettings.Store.TempThresholdCritical = (int)(_tempCritical.Value ?? 90);
        _OsdSettings.Store.UsageThresholdWarning = (int)(_usageWarning.Value ?? 70);
        _OsdSettings.Store.UsageThresholdCritical = (int)(_usageCritical.Value ?? 90);
        _OsdSettings.Store.FpsThresholdCritical = (int)(_fpsCritical.Value ?? 30);
        _OsdSettings.Store.LowFpsDeltaThreshold = (int)(_lowFpsDelta.Value ?? 30);

        _OsdSettings.SynchronizeStore();
        MessagingCenter.Publish(new OsdAppearanceChangedMessage());
    }

    private void ColorPicker_ColorChangedDelayed(object? sender, EventArgs e)
    {
        if (_isInitializing || !IsLoaded) return;

        _OsdSettings.Store.LabelColor = $"#{_labelColorPicker.SelectedColor.R:X2}{_labelColorPicker.SelectedColor.G:X2}{_labelColorPicker.SelectedColor.B:X2}";
        _OsdSettings.Store.ValueColor = $"#{_valueColorPicker.SelectedColor.R:X2}{_valueColorPicker.SelectedColor.G:X2}{_valueColorPicker.SelectedColor.B:X2}";
        _OsdSettings.Store.WarningColor = $"#{_warningColorPicker.SelectedColor.R:X2}{_warningColorPicker.SelectedColor.G:X2}{_warningColorPicker.SelectedColor.B:X2}";
        _OsdSettings.Store.CriticalColor = $"#{_criticalColorPicker.SelectedColor.R:X2}{_criticalColorPicker.SelectedColor.G:X2}{_criticalColorPicker.SelectedColor.B:X2}";

        _OsdSettings.SynchronizeStore();
        MessagingCenter.Publish(new OsdAppearanceChangedMessage());
    }

}