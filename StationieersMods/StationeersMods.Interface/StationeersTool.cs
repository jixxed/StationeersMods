namespace StationeersMods.Interface
{
    public class StationeersTool
    {
        public static StationeersTool ANGLE_GRINDER = new StationeersTool( "ItemAngleGrinder");
        public static StationeersTool WELDER = new StationeersTool( "ItemWeldingTorch");
        public static StationeersTool CABLE_CUTTERS = new StationeersTool( "ItemWireCutters");
        public static StationeersTool CROWBAR = new StationeersTool( "ItemCrowbar");
        public static StationeersTool DRILL = new StationeersTool( "ItemDrill");
        public static StationeersTool MINING_DRILL = new StationeersTool( "ItemMiningDrill");
        public static StationeersTool PICKAXE = new StationeersTool( "ItemPickaxe");
        public static StationeersTool DUCT_TAPE = new StationeersTool( "ItemDuctTape");
        public static StationeersTool FIRE_EXTINGUISHER = new StationeersTool( "ItemFireExtinguisher");
        public static StationeersTool LABELLER = new StationeersTool( "ItemLabeller");
        public static StationeersTool WRENCH = new StationeersTool( "ItemWrench");
        public static StationeersTool SCREWDRIVER = new StationeersTool( "ItemScrewdrive");
        public string PrefabName { get; private set; }

        public StationeersTool(string prefabName)
        {
            PrefabName = prefabName;
        }
    }
}