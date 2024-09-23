namespace Zebble.WinUI
{
    using System;
    using System.Threading.Tasks;
    using Olive;
    using Zebble;
    using Zebble.Services;

    abstract class PropertyView<T> : GeneralRecyclerListViewItem where T : View, FormField.IControl, new()
    {
        protected Inspector.PropertySettings Setting => ((Prop)Item.Value).Setting;

        protected T Control = new T();
        TextView Label = new TextView().Font(12, color: "#888").Padding(3).Width(45.Percent()).Wrap(false);
        // TextView Notes = new TextView("?").Background("#333").Size(15).TextAlignment(Alignment.Middle).Margin(left: 3, top: 2).Round();
        TextView TrackerIcon = new TextView("⌕").Font(16, color: Colors.Pink).Size(16).Margin(left: 2)
                                 .TextAlignment(Alignment.Middle).Round();

        protected PropertyView() => Item.Changed += Load;

        public override async Task OnInitializing()
        {
            await base.OnInitializing();
            this.Height(20).Margin(bottom: 1);

            await Content.Add(TrackerIcon.On(x => x.Tapped, () => TrackerIconTapped()));
            await Content.Add(Label);
            await Content.Add(Control);
            // await Content.Add(Notes.On(x => x.Tapped, () => NotesTapped()));
        }

        protected virtual void Load()
        {
            Label.Text = Setting.Label.TrimStart(Setting.Group).ToLiteralFromPascalCase();
        }

        public async Task Save(string newValue)
        {
            Setting.View.Animate(x => ApplyValue(newValue)).RunInParallel();
            await Inspector.Current.HighlightItem();
        }

        void ApplyValue(string newValue)
        {
            try
            {
                Setting.Property.SetValue(Setting.Instance, newValue.To(Setting.Property.PropertyType));
                this.Background(color: Colors.Transparent);
            }
            catch
            {
                this.Background(color: Colors.DarkRed);
            }
        }

        Task NotesTapped()
        {
            Log.For(this).Warning(Setting.Notes);
            return Task.CompletedTask;
        }

        Task TrackerIconTapped()
        {
            if (Setting.Instance is ITrackable tra)
            {
                LayoutTracker.StartTracking(tra);
                Nav.Reload().RunInParallel();
            }

            return Task.CompletedTask;
        }
    }
}