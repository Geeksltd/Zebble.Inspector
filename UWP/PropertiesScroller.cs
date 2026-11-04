namespace Zebble.UWP
{
    using System.Threading.Tasks;

    public class PropertiesScroller : ScrollView
    {
        PropertiesList PropertiesBox = new PropertiesList();

        public PropertiesScroller()
        {
            this.Width(50.Percent()).Background(color: "#222");
            PropertiesBox.ScrollEnabledChange.Handle(x => EnableScrolling = x);
        }

        public override async Task OnInitializing()
        {
            await base.OnInitializing();
            await Add(PropertiesBox);
        }

        internal async Task Load()
        {
            var currentView = Inspector.Current.CurrentView;
            if (currentView == null) return;

            await PropertiesBox.Load(currentView);
        }

        internal Task Reset() => PropertiesBox.Reset();
    }
}