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
        public class CooperativeAssembly : Assembly
        {
            private Quota quota;
            private Inventory inventory;
            private IMyAssembler master;
            private List<IMyAssembler> slaves;
            private List<MyProductionItem> queue;
            private List<MyProductionItem> subQueue;

            public CooperativeAssembly(Quota quota, Inventory inventory, List<IMyAssembler> assemblers)
            {
                this.quota = quota;
                this.inventory = inventory;
                this.master = assemblers[0];

                assemblers.RemoveAt(0);
                this.slaves = assemblers;

                this.queue = new List<MyProductionItem>();
                this.subQueue = new List<MyProductionItem>();
            }
            public override void produceQuota()
            {
                // Make sure the assemblers are configured correctly
                this.master.Enabled = true;
                this.master.Mode = MyAssemblerMode.Assembly;
                this.master.CooperativeMode = false;

                this.slaves.ForEach(slave => {
                    slave.Enabled = true;
                    slave.Mode = MyAssemblerMode.Assembly;
                    slave.CooperativeMode = true;
                });

                // Update inventory
                inventory.update();

                // Construct the current production queue of the quota assemblers.
                // Don't account for other assemblers.
                this.queue.RemoveAll(_ => true);
                //List<MyProductionItem> queue = new List<MyProductionItem>();

                if (!master.IsQueueEmpty)
                    master.GetQueue(queue);
                slaves.ForEach(slave =>
                {
                    if (!slave.IsQueueEmpty)
                    {
                        //List<MyProductionItem> tmp = new List<MyProductionItem>();
                        subQueue.RemoveAll(_ => true);
                        slave.GetQueue(subQueue);
                        queue.AddRange(subQueue);
                    }
                });

                quota.items.ForEach(item => {
                    MyFixedPoint storing = inventory.items.GetValueOrDefault(item.type, MyFixedPoint.Zero);
                    MyFixedPoint queued = getQueueAmount(queue, item);
                    MyFixedPoint production = item.amount - storing - queued;

                    if (production > 0)
                    {
                        this.master.AddQueueItem(item.blueprint, production);
                    }
                });
            }

            private MyFixedPoint getQueueAmount(List<MyProductionItem> queue, ProductionItem item)
            {
                MyFixedPoint queued = MyFixedPoint.Zero;

                queue.ForEach(queueItem => {
                    if (queueItem.BlueprintId == item.blueprint)
                        queued += queueItem.Amount;
                });

                return queued;
            }
        }
    }
}
