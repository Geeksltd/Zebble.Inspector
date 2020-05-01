using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zebble.UWP
{
    public class GroupView : TextView
    {
        public GroupView(string group) : base(group)
        {
            this.Font(20).Margin(vertical: 10);
        }
    }
}
