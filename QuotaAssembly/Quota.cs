using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        public class Quota
        {
            public List<ProductionItem> items { get; }

            public Quota()
            {
                this.items = new List<ProductionItem>();
            }

            public Quota(List<ProductionItem> items)
            {
                this.items = items;
            }

            public void set(ProductionItem item)
            {
                if (this.items.Contains(item))
                {
                    int idx = this.items.IndexOf(item);
                    this.items[idx].amount = item.amount;
                } else
                {
                    this.items.Add(item);
                }
            }

            public void remove(ProductionItem item)
            {
                this.items.Remove(item);
            }
        }
    }
}
