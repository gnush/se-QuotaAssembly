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
        public class Inventory
        {
            private IMyGridTerminalSystem system;
            public Dictionary<MyItemType, MyFixedPoint> items { get; }

            private List<IMyInventory> inventories;

            private int updateCounter = 0;

            public Inventory(IMyGridTerminalSystem system)
            {
                this.system = system;
                this.inventories = getBlockInventories<IMyTerminalBlock>();
                this.items = new Dictionary<MyItemType, MyFixedPoint>();

            }

            public List<IMyInventory> getBlockInventories<T>() where T : class, IMyTerminalBlock
            {
                List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
                List<IMyInventory> inventories = new List<IMyInventory>();

                system.GetBlocksOfType<T>(blocks, block => block.HasInventory);
                blocks.ForEach(block =>
                    {
                        for (int i = 0; i < block.InventoryCount; i++)
                        {
                            IMyInventory inv = block.GetInventory(i);
                            if (inv != null)
                                inventories.Add(inv);
                        }
                    });

                return inventories;
            }

            public void update()
            {
                // Don't update the available inventories each call to save performance
                if (updateCounter > 100)
                {
                    this.inventories = getBlockInventories<IMyTerminalBlock>();
                    updateCounter = 0;
                }
                this.inventories.ForEach(inventory => update(inventory));
                updateCounter++;
            }

            private void update(IMyInventory inventory)
            {
                List<MyInventoryItem> items = new List<MyInventoryItem>();
                inventory.GetItems(items);
                
                items.ForEach(item => update(item));
            }

            private void update(MyInventoryItem item)
            {
                if (this.items.ContainsKey(item.Type))
                    this.items[item.Type] += item.Amount;
                else
                    this.items.Add(item.Type, item.Amount);
            }
        }
    }
}
