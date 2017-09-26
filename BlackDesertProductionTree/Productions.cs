namespace BlackDesertProductionTree
{
    public class Productions
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; }
        public string ProductPictureURL { get; set; }
        public int ProductBatchSize { get; set; }

        public int RecipeID { get; set; }

        public Productions(int id, string name, string url, int bsize, int rid)
        {
            ProductID = id;
            ProductName = name;
            ProductPictureURL = url;
            ProductBatchSize = bsize;
            RecipeID = rid;
        }

        public override string ToString()
        {
            return ProductName;
        }
    }
}
