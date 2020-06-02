using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Windows.System;

namespace Zebble.UWP
{
    class PropertiesRecyclerList : GeneralRecyclerListView
    {
        public PropertiesRecyclerList() => GetTemplateHeight = GetTemplateHeightOfType;

        protected override Type GetTemplateOfType(Type dataType)
        {
            if (dataType == typeof(string)) return typeof(GroupView);

            return dataType.GetGenericArguments().Single();
        }

        protected override float GetTemplateHeightOfType(Type dataType) => dataType == typeof(string) ? 40 : 24;

        internal Task Load(Inspector.PropertySettings[] settings)
        {
            return UpdateSource(settings.GroupBy(x => x.Group).SelectMany(GetTypedObjects).Concat(" ").Concat(" "));
        }

        IEnumerable<object> GetTypedObjects(IGrouping<string, Inspector.PropertySettings> group)
        {
            yield return group.Key;

            foreach (var setting in group)
            {
                if (setting.Property.CanWrite == false) yield return new Prop<ReadonlySetting>(setting);
                else if (setting.Property.PropertyType == typeof(bool)) yield return new Prop<BooleanSetting>(setting);
                else yield return new Prop<TextSetting>(setting);
            }
        }
    }

    class Prop
    {
        public readonly Inspector.PropertySettings Setting;
        public Prop(Inspector.PropertySettings setting) => Setting = setting;
    }

    class Prop<T> : Prop
    {
        public Prop(Inspector.PropertySettings setting) : base(setting) { }
    }

    class ReadonlySetting : PropertyView<TextView>
    {
        public override async Task OnInitialized()
        {
            await base.OnInitialized();
            Control.TextColor(Colors.Yellow);
        }

        protected override void Load()
        {
            base.Load();
            Control.Text = Setting.ExistingValue.ToStringOrEmpty();
        }
    }

    class BooleanSetting : PropertyView<CheckBox>
    {
        public override async Task OnInitialized()
        {
            await base.OnInitialized();
            Control.CheckedChanged.Handle(() => Save(Control.Checked.ToString()));
        }

        protected override void Load()
        {
            base.Load();
            Control.Checked = Setting.ExistingValue.ToStringOrEmpty().TryParseAs<bool>() ?? false;
        }
    }

    class TextSetting : PropertyView<TextInput>
    {
        public override async Task OnInitialized()
        {
            await base.OnInitialized();
            Control.UserKeyUp.Handle(OnKeyUp);
            Control.UserTextChanged.Handle(() => Save(Control.Text.OrEmpty()));

            Control.ChangeInBatch(x => x.Font(12).Background(color: "#111").TextColor("#eee").Padding(3).Border(0));
        }

        async Task OnKeyUp(int key)
        {
            if (!Control.Text.Is<double>()) return;
            if (key != (int)VirtualKey.Up && key != (int)VirtualKey.Down) return;

            var add = 1.0;

            if (Control.Text.Contains(".")) add = 1.0 / Math.Pow(10, Control.Text.TrimBefore(".", trimPhrase: true).Length);

            if (add == 1 && Setting.Label == "Opacity") add = 0.1;

            if (key == (int)VirtualKey.Down) add *= -1;

            Control.Text = (Control.Text.To<double>() + add).ToString();

            await Save(Control.Text);
        }

        protected override void Load()
        {
            base.Load();
            Control.Placeholder = "-Auto-".OnlyWhen(Setting.Label.IsAnyOf("Width", "Height", "X", "Y"));

            Control.Text = Setting.ExistingValue.ToStringOrEmpty();
        }
    }
}
