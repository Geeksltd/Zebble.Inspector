namespace Zebble.UWP
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Olive;

    partial class Inspector
    {
        internal static IEnumerable<PropertySettings> GetSettings(View view)
        {
            return CalculateSettings(view, view).ExceptNull().ToList()
                .OrderBy(x => x.GetGroupOrder())
                .ThenBy(x => x.GetPropertyOrder())
                .ThenBy(s => s.Label);
        }

        static IEnumerable<PropertySettings> CalculateSettings(object instance, View view)
        {
            foreach (var property in instance.GetType().GetProperties().Except(x => x.IsStatic()))
            {
                if (property.GetSetMethod()?.IsPublic != true) continue;
                if (property.GetGetMethod()?.IsPublic != true) continue;

                var type = property.PropertyType;
                if (type.IsA<Type>() || type.IsA<View>() || type.IsA<Action>()) continue;

                if (property.Name == nameof(View.AutoFlash)) continue;

                if (type.IsA<IEnumerable>() && !type.IsA<string>()) continue;

                var group = (property.GetCustomAttribute<PropertyGroupAttribute>()?.Group).Or("General");

                if (type.IsA<Gap>() || type.IsA<IBorder>() || type.IsA<IFont>())
                {
                    var obj = property.GetValue(instance, null);

                    foreach (var i in CalculateSettings(obj, view).ExceptNull())
                    {
                        i.Group = property.Name;
                        i.Instance = typeof(Stylesheet).GetProperty(property.Name).GetValue(view.Style);
                        yield return i;
                    }
                }
                else
                    yield return new PropertySettings(instance, property, view, group);
            }

            if (instance is View)
            {
                foreach (var option in Enum.GetValues(typeof(Length.LengthType)))
                    yield return CreateLength(view, (Length.LengthType)option);

                foreach (var i in GetFrameCalculations(view)) yield return i;
            }
        }

        static PropertySettings CreateLength(View view, Length.LengthType type)
        {
            var name = type.ToString();
            var group = "Frame";
            object lengthOwner = view;

            if (name.StartsWith("Margin"))
            {
                lengthOwner = view.Margin;
                name = name.TrimStart("Margin");
                group = "Frame - Margin";
            }

            if (name.StartsWith("Padding"))
            {
                lengthOwner = view.Padding;
                name = name.TrimStart("Padding");
                group = "Frame - Padding";
            }

            var length = lengthOwner.GetType().GetField(name).GetValue(lengthOwner) as Length;
            var property = typeof(Length).GetProperty(nameof(Length.AsText));

            return new PropertySettings(length, property, view, group, type.ToString())
            {
                Label = name,
                Notes = length.Dependencies
            };
        }

        static IEnumerable<PropertySettings> GetFrameCalculations(View item)
        {
            var result = new List<PropertySettings>();

            void report(string p) => result.Add(new PropertySettings(item, item.GetType().GetProperty(p), item, "Frame"));

            if (item.Width.FixedValue == null) report(nameof(item.ActualWidth));
            if (item.Height.FixedValue == null) report(nameof(item.ActualHeight));

            if (item.X.FixedValue == null && item.ActualX != 0) report(nameof(item.ActualX));
            if (item.Y.FixedValue == null && item.ActualY != 0) report(nameof(item.ActualY));

            if (!item.NativeWidth.AlmostEquals(item.ActualWidth, 0.3f)) report(nameof(item.NativeWidth));
            if (!item.NativeHeight.AlmostEquals(item.ActualHeight, 0.3f)) report(nameof(item.NativeHeight));
            if (!item.NativeX.AlmostEquals(item.ActualX, 0.3f)) report(nameof(item.NativeX));
            if (!item.NativeY.AlmostEquals(item.ActualY, 0.3f)) report(nameof(item.NativeY));

            return result;
        }

        internal class PropertySettings
        {
            public View View;
            public object Instance, ExistingValue;
            public PropertyInfo Property;
            public string Group, Label, Notes, Key;

            public PropertySettings(object instance, PropertyInfo property, View view, string group, string middlePath = null)
            {
                Instance = instance;
                Property = property;
                Label = property.Name;
                ExistingValue = Property.GetValue(Instance);
                View = view;
                Group = group;

                Key = Group + "|" + middlePath + "-" + Property.DeclaringType.Name + "-" + Property.Name + "-" + Property.PropertyType.Name;
            }

            public override string ToString() => Group + "." + Label + " => " + ExistingValue;

            internal int GetGroupOrder()
            {
                if (Group == "General") return 0;
                if (Group == "Frame") return 1;
                if (Group.StartsWith("Frame")) return 2;
                if (Group == "Background") return 9;
                if (Group == "Border") return 10;
                if (Group == "Transformation") return 11;
                return 0;
            }

            [EscapeGCop("This is in effect a configuration module.")]
            internal double GetPropertyOrder()
            {
                var name = Label;

                var result = -1.0;

                if (name == "Id") return 0;
                if (name.Contains("Css")) return 100;

                if (name.IsAnyOf("Visible", "Ignored")) return 0;
                if (name == "Enabled") return 1;
                if (name == "Absolute") return 100;
                if (name.StartsWith("Rotation")) return 100;

                if (name.StartsWith("Radius"))
                {
                    if (name.EndsWith("TopLeft")) return 20;
                    if (name.EndsWith("TopRight")) return 21;
                    if (name.EndsWith("BottomRight")) return 22;
                    if (name.EndsWith("BottomLeft")) return 23;
                }

                if (name.EndsWith("Left") || name.EndsWith("X")) result = 0;
                if (name.EndsWith("Top") || name.EndsWith("Y")) result = 2;
                if (name.EndsWith("Right")) result = 3;
                if (name.EndsWith("Bottom")) result = 4;
                if (name.EndsWith("Width")) result = 5;
                if (name == "ActualHeight") result = 6;
                if (name.EndsWith("Height")) result = 7;

                if (name.StartsWith("Actual")) result += 0.1;
                if (name.StartsWith("Native")) result += 0.2;

                if (result == -1) return 50;
                else return result;
            }
        }
    }
}