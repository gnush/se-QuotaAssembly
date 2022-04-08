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
    partial class Program : MyGridProgram
    {
        enum PRODUCTION_TYPE
        {
            AMMO,
            COMPONENT,
            TOOL
        }

        private readonly Dictionary<PRODUCTION_TYPE, String> production_section = new Dictionary<PRODUCTION_TYPE, string> {
            { PRODUCTION_TYPE.AMMO, "AmmoMagazine"},
            { PRODUCTION_TYPE.COMPONENT, "Component"},
            { PRODUCTION_TYPE.TOOL, "PhysicalGunObject"},
        };

        // Begin config values#
        private readonly String NAME = "QuotaTemplateFactory"; // Name of the Assembler/Cargo Container to generate the quota from
        private readonly int DEFAULT_COMPONENT_VALUE = 0;
        private readonly int DEFAULT_AMMO_VALUE = 0;
        private readonly int DEFAULT_TOOL_VALUE = 0;
        // End config values

        private readonly String MyObjectBuilderPrefix = "MyObjectBuilder_";

        private MyIni quota = new MyIni();

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Once;
        }

        public void Main(string argument, UpdateType updateSource)
        {
            quota.Clear();

            string[] args = argument.Split(',');

            if (args.Length == 1 && args[0] == "ammo")
                makeQuotaFromAssemblerQueue(PRODUCTION_TYPE.AMMO);
            else if (args.Length == 1 && args[0] == "component")
                makeQuotaFromAssemblerQueue(PRODUCTION_TYPE.COMPONENT);
            else if (args.Length == 1 && args[0] == "tool")
                makeQuotaFromAssemblerQueue(PRODUCTION_TYPE.TOOL);
            else if (args.Length == 1 && args[0] == "cargo")
                makeQuotaFromCargoInventory(false);
            else if (args.Length == 1 && args[0] == "cargo-default")
                makeQuotaFromCargoInventory(true);
            else if (args.Length == 2 && args[0] == "assembler" && args[1] == "ammo")
                makeQuotaFromAssemblerQueue(PRODUCTION_TYPE.AMMO);
            else if (args.Length == 2 && args[0] == "assembler" && args[1] == "component")
                makeQuotaFromAssemblerQueue(PRODUCTION_TYPE.COMPONENT);
            else if (args.Length == 2 && args[0] == "assembler" && args[1] == "tool")
                makeQuotaFromAssemblerQueue(PRODUCTION_TYPE.TOOL);
            else // Default: Assembler, Component
                makeQuotaFromAssemblerQueue(PRODUCTION_TYPE.COMPONENT);

            Me.CustomData = this.quota.ToString();
        }



        private void makeQuotaFromCargoInventory(Boolean useInventoryAmount)
        {
            List<IMyCargoContainer> cargo = new List<IMyCargoContainer>();

            // Search for matching cargo container
            GridTerminalSystem.GetBlocksOfType<IMyCargoContainer>(cargo, container => container.CustomName == NAME);

            // No cargo container found
            if (!cargo.Any())
                return;

            IMyInventory inventory = cargo[0].GetInventory();
            List<MyInventoryItem> items = new List<MyInventoryItem>();
            inventory.GetItems(items);

            items.ForEach(item => makeIniEntry(item, useInventoryAmount));
        }

        private void makeQuotaFromAssemblerQueue(PRODUCTION_TYPE type)
        {
            List<IMyAssembler> assemblers = new List<IMyAssembler>();

            // Search for matching assembler
            GridTerminalSystem.GetBlocksOfType<IMyAssembler>(assemblers, assembler => assembler.CustomName == NAME);

            // No assembler found
            if (!assemblers.Any())
                return;

            List<MyProductionItem> queue = new List<MyProductionItem>();

            assemblers[0].GetQueue(queue);
            queue.ForEach(item => makeIniEntry(item, type));
        }

        private void makeIniEntry(MyInventoryItem item, Boolean useInventoryAmount)
        {
            String key = item.Type.SubtypeId;  // TODO: component is missing from e.g. ConstructionComponent, is only Construction
            String type = item.Type.TypeId;

            Echo("item = " + item.ToString());
            Echo("item.Type = " + item.Type.ToString());
            Echo("subtype = " + key);
            Echo("typeId = " + type);

            if (type.Contains(production_section[PRODUCTION_TYPE.COMPONENT]))
            {
                // Component workaraound
                // components that have Component as part of their blueprint name but not as part of their inventory name
                try
                {
                    // TODO: doesnt work either, as the subtype is not enforced
                    MyDefinitionId.Parse(type + "/" + key + production_section[PRODUCTION_TYPE.COMPONENT] + "foo");

                    quota.Set(production_section[PRODUCTION_TYPE.COMPONENT], key+production_section[PRODUCTION_TYPE.COMPONENT], useInventoryAmount ? item.Amount.ToIntSafe() : DEFAULT_COMPONENT_VALUE);
                }
                catch
                {
                    quota.Set(production_section[PRODUCTION_TYPE.COMPONENT], key, useInventoryAmount ? item.Amount.ToIntSafe() : DEFAULT_COMPONENT_VALUE);
                }
            }
            else if (type.Contains(production_section[PRODUCTION_TYPE.AMMO]))
                quota.Set(production_section[PRODUCTION_TYPE.AMMO], key, useInventoryAmount ? item.Amount.ToIntSafe() : DEFAULT_AMMO_VALUE);
            else if (type.Contains(production_section[PRODUCTION_TYPE.TOOL]))
                quota.Set(production_section[PRODUCTION_TYPE.TOOL], key, useInventoryAmount ? item.Amount.ToIntSafe() : DEFAULT_TOOL_VALUE);
        }

        private void makeIniEntry(MyProductionItem item, PRODUCTION_TYPE type)
        {
            String key = item.BlueprintId.SubtypeName;

            quota.Set(production_section[type], key, DEFAULT_COMPONENT_VALUE);

            //if (isComponent(key))
            //    quota.Set(production_section[PRODUCTION_TYPE.COMPONENT], key, DEFAULT_COMPONENT_VALUE);

            //if (isAmmo(key))
            //    quota.Set(production_section[PRODUCTION_TYPE.AMMO], key, DEFAULT_AMMO_VALUE);

            //if (isTool(key))
            //    quota.Set(production_section[PRODUCTION_TYPE.TOOL], key, DEFAULT_TOOL_VALUE);
        }

        // TODO: doesnt work as long as the category exists
        // TODO: how to determine the category of an item based on the blueprint?
        // TODO: Parse() doesn't work, as the subtype is not enforced
        private Boolean isComponent(String subtypeName)
        {
            try
            {
                MyDefinitionId.Parse(MyObjectBuilderPrefix + production_section[PRODUCTION_TYPE.COMPONENT] + "/" + subtypeName);

                return true;
            }
            catch
            {
                return false;
            }
        }

        private Boolean isAmmo(String subtypeName)
        {
            try
            {
                MyDefinitionId.Parse(MyObjectBuilderPrefix + production_section[PRODUCTION_TYPE.AMMO] + "/" + subtypeName);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private Boolean isTool(String subtypeName)
        {
            try
            {
                MyDefinitionId.Parse(MyObjectBuilderPrefix + production_section[PRODUCTION_TYPE.TOOL] + "/" + subtypeName);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
