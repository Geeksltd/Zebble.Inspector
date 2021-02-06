namespace Zebble.UWP
{
    using Olive;
    using System.Collections.Generic;
    using System.Linq;

    public static class ViewExtensions
    {
        /// <summary>
        /// This method is an extended version of GetFullPath, but the result string includes
        /// an string like :nth-child(n) in the generate path. This will help us to distinguish
        /// views with similar parts in their path like below:<br/>
        /// Column => Row => TextView<br/>
        /// Column => Row => TextView
        /// </summary>
        /// <param name="view"></param>
        /// <returns></returns>
        public static string GetFullyQualifiedPath(this View view)
        {
            var array = new List<string>
            {
                view.ToString()
            };

            var parent = view.parent;

            while (parent != null && parent != View.Root)
            {
                var viewType = view.GetType();
                var children = parent.CurrentChildren.ToArray();

                var childrenWithSameType = children.Where(x => x.GetType() == viewType);

                if (childrenWithSameType.Skip(count: 1).Any())
                    array[array.Count - 1] += $":nth-child({parent.CurrentChildren.IndexOf(view)})";

                array.Add(parent.ToString());

                view = parent;
                parent = view.parent;
            }

            return array.ToArray().Reverse().ToString(" ➔ ");
        }
    }
}