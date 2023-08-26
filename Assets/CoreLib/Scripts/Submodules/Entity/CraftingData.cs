namespace CoreLib.Submodules.ModEntity
{
    public struct CraftingData
    {
        public ObjectID objectID { get; set; }
        public int amount { get; set; }

        public CraftingData(ObjectID objectID, int amount)
        {
            this.objectID = objectID;
            this.amount = amount;
        }
    }
}