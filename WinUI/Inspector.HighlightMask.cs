using System.Linq;
using System.Threading.Tasks;
using Olive;

namespace Zebble.WinUI
{
    partial class Inspector
    {
        static Canvas HighlightMask, HighlightBorder;

        static async Task CreateHighlightMask()
        {
            HighlightMask = new Canvas().Id("ZebbleInspectorHighlightMask").Absolute().Border(1, color: "#ffff00").Background("#ffff00").Opacity(0.2f);
            HighlightBorder = new Canvas().Id("ZebbleInspectorHighlightBorder").Absolute().Border(1, color: Colors.Red).Opacity(0.5f);

            await UIRuntime.RenderRoot.Add(HighlightMask, awaitNative: true);
            await UIRuntime.RenderRoot.Add(HighlightBorder, awaitNative: true);

            Thread.UI
                .Post(() =>
            {
                HighlightMask.Native().IsHitTestVisible = false;
                HighlightBorder.Native().IsHitTestVisible = false;
            });
        }

        internal async Task HighlightItem()
        {
            var item = CurrentView;
            if (item == null) return;

            if (item.ActualWidth > Device.Screen.Width + 10) return;

            foreach (var highlighter in new[] { HighlightMask, HighlightBorder })
            {
                highlighter.X.BindTo(item.X, a => item.CalculateAbsoluteX());
                highlighter.Y.BindTo(item.Y, a => item.CalculateAbsoluteY());
                highlighter.Width.BindTo(item.Width);
                highlighter.Height.BindTo(item.Height);

                await highlighter.Visible().BringToFront();

                foreach (var s in item.GetAllParents().OfType<ScrollView>())
                {
                    highlighter.X.UpdateOn(s.UserScrolledHorizontally, s.ApiScrolledTo);
                    highlighter.Y.UpdateOn(s.UserScrolledVertically, s.ApiScrolledTo);
                }
            }
        }

        void HideHighlighters() => new[] { HighlightBorder, HighlightMask }.ExceptNull().Do(x => x.Hide());
    }
}