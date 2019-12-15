using System.Threading.Tasks;

namespace Zebble.UWP
{
    public class PropertiesScroller : ScrollView
    {
        PropertiesList PropertiesBox = new PropertiesList();

        public PropertiesScroller()
        {
            this.Width(40.Percent()).Background(color: "#222");
            PropertiesBox.ScrollEnabledChange.Handle(x => EnableScrolling = x);
        }

        public override async Task OnInitializing()
        {
            await base.OnInitializing();
            await Add(PropertiesBox);
        }

        internal async Task Load()
        {
            if (Inspector.Current.CurrentView == null) return;
            await PropertiesBox.Load(Inspector.Current.CurrentView);
        }

        internal Task Reset() => PropertiesBox.Reset();
    }
}
