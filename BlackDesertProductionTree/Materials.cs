public class Material
{
    public string MaterialUrl { get; set; }
    public string MaterialName { get; set; }
    public string MaterialCount { get; set; }
    public string MaterialID { get; set; }

    public Material()
    {
        MaterialUrl = "";
        MaterialName = "";
        MaterialCount = "";
    }
    public Material(int id, string url, string name, int count)
    {
        MaterialID = id.ToString();
        MaterialUrl = url;
        MaterialName = name;
        MaterialCount = "x" + count.ToString();
        if (count <= 0)
        {
            MaterialCount = "";
        }
    }
}

