﻿using System;
using System.Threading.Tasks;

namespace Zebble.UWP
{
    public class GroupView : GeneralRecyclerListViewItem
    {
        TextView TextView = new TextView().Font(18).Margin(vertical: 10);

        public GroupView()
        {
            Item.Changed += () => TextView.Text = Item.Value.ToStringOrEmpty();
        }

        public override async Task OnInitializing()
        {
            await base.OnInitializing();
            await Content.Add(TextView);
        }
    }
}