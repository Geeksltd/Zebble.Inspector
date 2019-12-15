namespace Zebble.UWP
{
    using System;
    using System.Threading.Tasks;
    using Zebble;
    using Zebble.Services;

    public class PropertyFormField
    {
        public FormField Field;
        internal Inspector.PropertySettings Item;
        public string SearchKeywords, Group;

        TextView Notes = new TextView("?").Background(Colors.Gray).Size(15).TextAlignment(Alignment.Middle).Round();

        TextView TrackerIcon = new TextView("⌕").Font(16, color: Colors.Pink).Size(16).Margin(left: 2)
                                 .TextAlignment(Alignment.Middle).Round();

        public PropertyFormField(FormField field)
        {
            Field = field;
            field.Add(Notes.On(x => x.Tapped, () => NotesTapped()));
            field.Add(TrackerIcon.On(x => x.Tapped, () => TrackerIconTapped()));
        }

        internal void Load(Inspector.PropertySettings item)
        {
            Item = item;
            Field.Data("Prop", item);
            Group = item.Group;
            SearchKeywords = item.Group + " " + Field.LabelText;

            var value = item.ExistingValue.ToStringOrEmpty();

            if (Field is FormField<TextView> tv) tv.Value = value;
            if (Field is FormField<TextInput> ti) ti.Value = value;
            if (Field is FormField<CheckBox> tc) tc.Control.Checked = value.TryParseAs<bool>() ?? false;
        }

        Task NotesTapped()
        {
            Device.Log.Warning(Item.Notes);
            return Task.CompletedTask;
        }

        Task TrackerIconTapped()
        {
            if (Item.Instance is ITrackable tra)
            {
                LayoutTracker.StartTracking(tra);
                Nav.Reload().RunInParallel();
            }

            return Task.CompletedTask;
        }
    }
}