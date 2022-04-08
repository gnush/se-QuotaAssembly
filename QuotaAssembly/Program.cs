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
        private MyIni _ini = new MyIni();
        private readonly String CONFIG = "QuotaAssembly";
        private readonly String CONFIG_Mode = "mode";

        // Quota ini sections
        private readonly String COMPONENT = "Component";
        private readonly String AMMO = "AmmoMagazine";
        private readonly String TOOL = "PhysicalGunObject";

        private readonly String DefaultMode = "cooperative";

        private readonly String ModeCooperative = "cooperative";
        private readonly String ModeBalanced = "balanced";

        private readonly String MyObjectBuilderPrefix = "MyObjectBuilder_";
        //private readonly String ComponentPrefix = "Component/";
        private readonly String BlueprintDefinitionPrefix = "BlueprintDefinition/";

        private String Mode;
        private List<IMyAssembler> assemblers = new List<IMyAssembler>();
        private Quota quota;

        public Program()
        {
            //Runtime.UpdateFrequency = UpdateFrequency.Update100;

            if(Me.CustomData.Any())
            {
                // Parse ini
                MyIniParseResult _pareseIni;
                if (!_ini.TryParse(Me.CustomData, out _pareseIni))
                    throw new Exception(_pareseIni.ToString());

                Mode = _ini.Get(CONFIG, CONFIG_Mode).ToString();
            } else
            {
                // Create default ini
                _ini.AddSection(CONFIG);

                _ini.Set(CONFIG, CONFIG_Mode, DefaultMode);
                _ini.SetComment(CONFIG, CONFIG_Mode, "cooperative | balanced");

                _ini.AddSection(COMPONENT);
                _ini.AddSection(AMMO);
                _ini.AddSection(TOOL);

                Me.CustomData = _ini.ToString();
                Echo("Saved default ini to CustomData");
                return;
            }

            // Get assemblers
            GridTerminalSystem.GetBlocksOfType<IMyAssembler>(assemblers, assembler => MyIni.HasSection(assembler.CustomData, CONFIG));

            if (!assemblers.Any())
                throw new Exception("Need at least one assembler");

            // Construct Quota
            this.quota = new Quota();

            List<MyIniKey> components = new List<MyIniKey>();
            List<MyIniKey> ammo = new List<MyIniKey>();
            List<MyIniKey> tools = new List<MyIniKey>();

            _ini.GetKeys(COMPONENT, components);
            _ini.GetKeys(AMMO, ammo);
            _ini.GetKeys(TOOL, tools);;

            components.ForEach(key => addQuotaEntry(COMPONENT, key));
        }

        public void Main(string argument, UpdateType updateSource)
        {
            Assembly assembly;

            if (this.Mode == ModeCooperative)
            {
                Inventory inventory = new Inventory(this.GridTerminalSystem);
                assembly = new CooperativeAssembly(this.quota, inventory, this.assemblers);
            } else if (this.Mode == ModeBalanced)
            {
                throw new Exception("balanced mode not yet implemented");
            } else
            {
                // Default to cooperative mode
                Inventory inventory = new Inventory(this.GridTerminalSystem);
                assembly = new CooperativeAssembly(this.quota, inventory, this.assemblers);
            }

            assembly.produceQuota();
        }

        private void addQuotaEntry(String type, MyIniKey key)
        {
            if (type == COMPONENT)
            {
                try
                {
                    this.quota.set(makeComponent(key));
                }
                catch (Exception e)
                {
                    Echo("couldn't create production item " + key.Name);
                    Echo(e.ToString());
                }
            } else if (type == AMMO)
            {
                try
                {
                    this.quota.set(makeAmmo(key));
                }
                catch (Exception e)
                {
                    Echo("couldn't create production item " + key.Name);
                    Echo(e.ToString());
                }
            } else if (type == TOOL)
            {
                try
                {
                    this.quota.set(makeTool(key));
                }
                catch (Exception e)
                {
                    Echo("couldn't create production item " + key.Name);
                    Echo(e.ToString());
                }
            }
        }

        private ProductionItem makeComponent(MyIniKey key)
        {
            MyDefinitionId blueprint = MyDefinitionId.Parse(this.MyObjectBuilderPrefix + this.BlueprintDefinitionPrefix + key.Name);

            // Component fix for inventory items:
            // Blueprints that end with "Component" need to be trimmed for their inventory item counterpart.
            MyItemType type = blueprint.SubtypeName.EndsWith("Component") ? MyItemType.MakeComponent(blueprint.SubtypeName.Substring(0, blueprint.SubtypeName.Length-9)) : MyItemType.MakeComponent(blueprint.SubtypeName);

            MyFixedPoint amount = _ini.Get(key).ToInt32();

            //MyItemType type = MyItemType.Parse(this.MyObjectBuilderPrefix + this.ComponentPrefix + key.Name);
            //MyItemType foo = MyItemType.MakeComponent(blueprint.SubtypeName); // <- this is the way. need to change the ini format from one section [Quota] to multiple sections [Component], [Ammo],  ...

            return new ProductionItem(type, blueprint, amount);
        }

        private ProductionItem makeAmmo(MyIniKey key)
        {
            MyDefinitionId blueprint = MyDefinitionId.Parse(this.MyObjectBuilderPrefix + this.BlueprintDefinitionPrefix + key.Name);
            MyItemType type = MyItemType.MakeAmmo(blueprint.SubtypeName);
            MyFixedPoint amount = _ini.Get(key).ToInt32();

            return new ProductionItem(type, blueprint, amount);
        }

        private ProductionItem makeTool(MyIniKey key)
        {
            MyDefinitionId blueprint = MyDefinitionId.Parse(this.MyObjectBuilderPrefix + this.BlueprintDefinitionPrefix + key.Name);
            MyItemType type = MyItemType.MakeTool(blueprint.SubtypeName);
            MyFixedPoint amount = _ini.Get(key).ToInt32();

            return new ProductionItem(type, blueprint, amount);
        }
    }
}
