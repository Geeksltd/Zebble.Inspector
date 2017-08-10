namespace Zebble.UWP
{
    using System.Threading.Tasks;

    partial class InspectionBox
    {
        DevicePanel DevicePanel;
        internal async Task LoadDevice()
        {
            await DeviceScroller.ClearChildren();
            if (Inspector.Current.CurrentView != null)
                await DeviceScroller.Add(DevicePanel = new DevicePanel(Inspector.Current.CurrentView));
        }
    }
}
