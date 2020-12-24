namespace Zebble.UWP
{
    using System.Threading.Tasks;

    partial class InspectionBox
    {
        DevicePanel DevicePanel;
        internal async Task LoadDevice()
        {
            await DeviceScroller.ClearChildren();

            var currentView = Inspector.Current.CurrentView;
            if (currentView == null) return;

            await DeviceScroller.Add(DevicePanel = new DevicePanel(currentView));
        }
    }
}
