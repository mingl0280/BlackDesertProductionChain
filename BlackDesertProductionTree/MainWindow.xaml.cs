using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Data.SQLite;
using System.IO;
using System.Data;
using System.Diagnostics;
using System.Windows.Controls.Primitives;
using Newtonsoft.Json;
using System.Net.WebSockets;
using System.Net;

namespace BlackDesertProductionTree
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window, IDisposable
    {
        
        public MainWindow()
        {
            InitializeComponent();
        }

        private int ProductCounts = 1;
        private double ProductsMultiplyFactor = 1.0;
        private double SideProductsMultiplyFactor = 1.0;
        private List<ResultHitchary> Results = new List<ResultHitchary>();
        private SQLiteConnection sConn;

        private string dbFile;
        List<Productions> pList = new List<Productions>();

        private void MainWindow1_Loaded(object sender, RoutedEventArgs e)
        {
            DirectoryInfo di = new DirectoryInfo(".");
            FileInfo[] fis = di.GetFiles("BlackDesertDB_*.db");
            dbFile = fis[0].FullName;
            sConn = new SQLiteConnection(@"Data Source=" + dbFile + @";Version=3");
            sConn.Open();
            //Initialize Craftable Items.
            SQLiteCommand sCmd = new SQLiteCommand(sConn)
            {
                CommandText = @"SELECT I.ItemName AS 'Name', CD.Count AS 'ProductCount', I.ItemID AS 'ProductID', I.IconURL AS 'URL', CD.RecipeID AS 'RecipeID' FROM CraftDrops AS CD INNER JOIN Items AS I ON CD.DropID = I.ItemID WHERE I.ItemName != ''"
            };
            DataTable dt = new DataTable();
            SQLiteDataAdapter sAdapter = new SQLiteDataAdapter(sCmd);
            sAdapter.Fill(dt);
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                pList.Add(new Productions(Convert.ToInt32(dt.Rows[i][2]), (string)dt.Rows[i][0], (string)dt.Rows[i][3], Convert.ToInt32(dt.Rows[i][1]), Convert.ToInt32(dt.Rows[i][4])));
            }
            ProductSelection.ItemsSource = pList;
            ProductSelection_ByMaterials.ItemsSource = pList;
        }

        private void TB_ProductCount_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !AllNumeric(e.Text);
            if (AllNumeric(e.Text))
            {
                if (int.Parse(e.Text) < 0)
                {
                    e.Handled = true;
                }
            }
            base.OnPreviewTextInput(e);
        }
        private bool AllNumeric(string inp)
        {
            return int.TryParse(inp, out int tmp);
        }

        private void TB_ProductCount_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (int.TryParse(TB_ProductCount.Text, out int tmp))
            {
                ProductCounts = tmp;
                if (MultiplyProductionCount != null && MultiplySideProductCount != null && ProductSideProductionSize != null)
                {
                    var t = (Productions)ProductSelection.SelectedValue;

                    if (!string.IsNullOrEmpty(MultiplyProductionCount.Content as string))
                    {
                        MultiplyProductionCount.Content = (t.ProductBatchSize * ProductsMultiplyFactor * ProductCounts).ToString();
                    }
                }
            }
            else
            {
                TB_ProductCount.Text = "0";
            }
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var tmp = (Productions)ProductSelection.SelectedValue;
            string SideProducts, SideProductsCount, SideProductsMultiplyCount;
            SideProducts = "";
            SideProductsCount = "";
            SideProductsMultiplyCount = "";
            ProductSize.Content = tmp.ProductBatchSize;
            //Set Main Product Values
            MultiplyProductionCount.Content = (tmp.ProductBatchSize * ProductsMultiplyFactor * ProductCounts).ToString();
            MainProductIcon.Source = new BitmapImage(new Uri(tmp.ProductPictureURL));
            //Query for side products;
            SQLiteCommand sCmd = new SQLiteCommand(sConn)
            {
                CommandText = "SELECT I.ItemName AS 'Name', I.IconURL AS 'URL', CD.Count, CD.DropID FROM CraftDrops AS CD INNER JOIN Items AS I ON I.ItemID = CD.DropID WHERE RecipeID = @rid AND DropID <> @did AND RecipeID <> DropID"
            };
            sCmd.Parameters.AddWithValue("@rid", tmp.RecipeID);
            sCmd.Parameters.AddWithValue("@did", tmp.ProductID);
            DataTable dt = new DataTable();
            SQLiteDataAdapter sda = new SQLiteDataAdapter(sCmd);
            sda.Fill(dt);
            List<string> URLList = new List<string>();
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                SideProducts += (string)dt.Rows[i][0] + ",";
                SideProductsCount += dt.Rows[i][2].ToString() + ",";
                SideProductsMultiplyCount += ((long)dt.Rows[i][2] * SideProductsMultiplyFactor * ProductCounts).ToString() + ",";
                URLList.Add((string)dt.Rows[i][1]);
            }
            ProductSideProduction.Content = SideProducts.TrimEnd(',');
            ProductSideProductionSize.Content = SideProductsCount.TrimEnd(',');
            MultiplySideProductCount.Content = SideProductsMultiplyCount.TrimEnd(',');
            SideProductImages.ItemsSource = URLList;

            SetMaterialListByRecipeID(tmp.ProductID);
        }

        private void SetMaterialListByRecipeID(int RecipeID)
        {
            SQLiteCommand sCmd = new SQLiteCommand(sConn)
            {
                CommandText = "SELECT RI.RecipeID, RI.Count, I.ItemName, I.IconURL, I.ItemID " +
                    "FROM CraftDrops AS CD " +
                    "INNER JOIN RecipeItems AS RI ON CD.RecipeID = RI.RecipeID " +
                    "INNER JOIN Items AS I ON RI.MaterialID = I.ItemID " +
                    "INNER JOIN CraftingRecipes AS CR ON RI.RecipeID = CR.RID " +
                    "WHERE CD.DropID = @pid AND RI.RecipeID <> CD.DropID ORDER BY RI.RecipeID"
            };
            sCmd.Parameters.AddWithValue("@pid", RecipeID);
            List<Material> mList = new List<Material>();
            DataTable dt = new DataTable();
            SQLiteDataAdapter sda = new SQLiteDataAdapter(sCmd);
            sda.Fill(dt);
            if (dt.Rows.Count < 1)
                return;
            int rid = Convert.ToInt32(dt.Rows[0][0]);
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                if (rid != Convert.ToInt32(dt.Rows[i][0]))
                {
                    mList.Add(new Material(-1, "", "或", -1));
                    rid = Convert.ToInt32(dt.Rows[i][0]);
                }
                mList.Add(new Material(Convert.ToInt32(dt.Rows[i][4]), dt.Rows[i][3].ToString(), dt.Rows[i][2].ToString(), Convert.ToInt32(dt.Rows[i][1])));
            }
            MaterialListView.ItemsSource = mList;
        }

        private void TB_ProductTimesTextChanged(object sender, TextChangedEventArgs e)
        {
            if (double.TryParse(TB_ProductMultiply.Text, out double tmp))
            {
                ProductsMultiplyFactor = tmp;
                if (MultiplyProductionCount != null && MultiplySideProductCount != null && ProductSideProductionSize != null)
                {
                    var t = (Productions)ProductSelection.SelectedValue;

                    if (!string.IsNullOrEmpty(MultiplyProductionCount.Content as string))
                    {
                        MultiplyProductionCount.Content = (t.ProductBatchSize * ProductsMultiplyFactor * ProductCounts).ToString();
                    }
                }
            }
            else
            {
                TB_ProductMultiply.Text = "0.0";
            }
        }

        private void TB_SideProductMultiplyChanged(object sender, TextChangedEventArgs e)
        {
            if (double.TryParse(TB_SideProductMultiply.Text, out double tmp))
            {
                SideProductsMultiplyFactor = tmp;
            }
            else
            {
                TB_SideProductMultiply.Text = "0.0";
            }
        }

        private void TB_DoubleInput_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !AllNumeric(e.Text);
            if (e.Text == ".")
                e.Handled = false;
            if (AllNumeric(e.Text))
            {
                if (int.Parse(e.Text) < 0)
                {
                    e.Handled = true;
                }
            }
            base.OnPreviewTextInput(e);
        }
        /*
ResultHitchary rHit = new ResultHitchary();
List<ResultHitchary> lrht = new List<ResultHitchary>();
rHit.ItemCount = "3";
rHit.ItemName = "abcdetest";
rHit.URL = "http://bd.youxidudu.com/source/new_icon/06_pc_equipitem/00_common/00_etc/00000263.png";
List<ResultHitchary> Clds = new List<ResultHitchary>();
for(int i=0;i<10;i++)
{
Clds.Add(new ResultHitchary("http://bd.youxidudu.com/source/new_icon/03_etc/08_potion/00000" + (670 + i).ToString() + ".png", "狂亂的靈藥", "1"));
}
rHit.Children = Clds;
lrht.Add(rHit);
Treeview_Result.ItemsSource = lrht;
*/
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var ChoosedProduct = (Productions)ProductSelection.SelectedValue;
            if (ChoosedProduct == null)
                return;
            Treeview_Result.ItemsSource = GetProductionTree(ChoosedProduct.ProductID, ProductCounts);
        }

        private List<ResultHitchary> GetProductionTree(int ProductID, long LastLevelCount, int RecipeID = -1)
        {
            List<ResultHitchary> LevelList = new List<ResultHitchary>();
            SQLiteCommand sCmd = new SQLiteCommand(sConn);

            if (ProductID > 65535)
            {
                sCmd.CommandText = "SELECT ItemID FROM ItemAlias WHERE GroupID = @gid";
                sCmd.Parameters.AddWithValue("@gid", ProductID);
                DataTable dt = new DataTable();
                SQLiteDataAdapter sda = new SQLiteDataAdapter(sCmd);
                sda.Fill(dt);
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    int mid = Convert.ToInt32(dt.Rows[i][0]);
                    sCmd.CommandText = "SELECT IconURL, ItemName FROM Items WHERE ItemID = @id";
                    sCmd.Parameters.Clear();
                    sCmd.Parameters.AddWithValue("@id", mid);
                    sda = new SQLiteDataAdapter(sCmd);
                    DataTable itemDT = new DataTable();
                    sda.Fill(itemDT);
                    ResultHitchary rItem = new ResultHitchary(mid.ToString(), itemDT.Rows[0][0].ToString(), itemDT.Rows[0][1].ToString() + ((i + 1 == dt.Rows.Count) ? "" : "或"), LastLevelCount.ToString());
                    if (CheckNextLevelExists(mid) || mid > 65535)
                    {
                        rItem.Children = GetProductionTree(mid, LastLevelCount);
                    }
                    LevelList.Add(rItem);
                }
                return LevelList;
            }
            else
            {
                DataTable dt = new DataTable();
                if (RecipeID != -1)
                {
                    sCmd.CommandText = "SELECT RI.RecipeID, RI.MaterialID, RI.Count, I.ItemName, I.IconURL, CR.LifeLevel, CR.CraftingTime " +
                    "FROM CraftDrops AS CD " +
                    "INNER JOIN RecipeItems AS RI ON CD.RecipeID = RI.RecipeID " +
                    "INNER JOIN Items AS I ON RI.MaterialID = I.ItemID " +
                    "INNER JOIN CraftingRecipes AS CR ON RI.RecipeID = CR.RID " +
                    "WHERE CD.DropID = @pid AND RI.RecipeID = @rid AND RI.RecipeID <> CD.DropID ";
                    sCmd.Parameters.AddWithValue("@pid", ProductID);
                    sCmd.Parameters.AddWithValue("@rid", RecipeID);
                    SQLiteDataAdapter sda = new SQLiteDataAdapter(sCmd);
                    sda.Fill(dt);
                }
                else
                {
                    sCmd.CommandText = "SELECT RI.RecipeID, RI.MaterialID, RI.Count, I.ItemName, I.IconURL, CR.LifeLevel, CR.CraftingTime " +
                    "FROM CraftDrops AS CD " +
                    "INNER JOIN RecipeItems AS RI ON CD.RecipeID = RI.RecipeID " +
                    "INNER JOIN Items AS I ON RI.MaterialID = I.ItemID " +
                    "INNER JOIN CraftingRecipes AS CR ON RI.RecipeID = CR.RID " +
                    "WHERE CD.DropID = @pid AND RI.RecipeID <> CD.DropID";
                    sCmd.Parameters.AddWithValue("@pid", ProductID);
                    SQLiteDataAdapter sda = new SQLiteDataAdapter(sCmd);
                    sda.Fill(dt);
                }
                //SQLiteDataReader sReader = sCmd.ExecuteReader();

                SQLiteCommand sCmdN = new SQLiteCommand("SELECT Distinct RecipeID FROM CraftDrops WHERE DropID = @pid AND DropID <> RecipeID GROUP BY RecipeID", sConn);
                sCmdN.Parameters.AddWithValue("@pid", ProductID);
                DataTable dtn = new DataTable();
                SQLiteDataAdapter sdap = new SQLiteDataAdapter(sCmdN);
                sdap.Fill(dtn);
                if (dtn.Rows.Count > 1 && RecipeID == -1)
                {
                    foreach (DataRow dr in dtn.Rows)
                    {
                        ResultHitchary rItem = new ResultHitchary("-1", "", "合成表#" + dr["RecipeID"].ToString(), LastLevelCount.ToString())
                        {
                            Children = GetProductionTree(ProductID, LastLevelCount, Convert.ToInt32(dr["RecipeID"]))
                        };
                        LevelList.Add(rItem);
                    }
                }
                else
                {
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        int mid = Convert.ToInt32(dt.Rows[i]["MaterialID"]);
                        ResultHitchary rItem = new ResultHitchary(mid.ToString(), dt.Rows[i]["IconURL"].ToString(), dt.Rows[i]["ItemName"].ToString(), (Convert.ToInt32(dt.Rows[i]["Count"]) * LastLevelCount).ToString(), dt.Rows[i]["CraftingTime"].ToString(), dt.Rows[i]["LifeLevel"].ToString());
                        if (CheckNextLevelExists(mid) || mid > 65535)
                        {
                            rItem.Children = GetProductionTree(mid, (Convert.ToInt32(dt.Rows[i]["Count"]) * LastLevelCount));
                        }
                        LevelList.Add(rItem);
                    }
                }

                return LevelList;
            }
        }

        private bool CheckNextLevelExists(int ProductID)
        {
            SQLiteCommand sCmd = new SQLiteCommand("SELECT RecipeID FROM CraftDrops WHERE DropID = @pid", sConn);
            sCmd.Parameters.AddWithValue("@pid", ProductID);
            DataTable dt = new DataTable();
            SQLiteDataAdapter sda = new SQLiteDataAdapter(sCmd);
            sda.Fill(dt);
            if (dt.Rows.Count >= 1)
            {
                if (Convert.ToInt32(dt.Rows[0][0]) == ProductID)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                return false;
            }
        }

        private List<Material> GetMaterialListByProduct(int ProductID)
        {
            List<Material> mList = new List<Material>();

            return mList;
        }

        private void MaterialListView_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed || e.LeftButton == MouseButtonState.Released)
            {
                e.Handled = true;
            }
            base.OnPreviewMouseDown(e);
        }

        private void MaterialListView_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            base.OnPreviewMouseDoubleClick(e);
        }

        private void ToolTip_Opened(object sender, RoutedEventArgs e)
        {
            var tipItem = (ToolTip)sender;

        }

        private void ListViewItem_MouseEnter(object sender, MouseEventArgs e)
        {
            ListViewItem lvi = e.Source as ListViewItem;
            Material m = lvi.Content as Material;
            //ListItemPopup.DataContext = m;
            //ListItemPopup.PlacementTarget = lvi;
            //ListItemPopup.Placement = PlacementMode.MousePoint;
            //List<UIElement> DescriptionElements = GetItemDescriptionElements(m.MaterialID);
            GetItemDescriptionElements(m.MaterialID);
            //ListItemDescription.Children.Clear();
            //ListItemDescription.RowDefinitions.Clear();
            /*
            int counter = 0;
            foreach(UIElement uie in DescriptionElements)
            {
                //ListItemDescription.RowDefinitions.Add(new RowDefinition());
                //uie.SetValue(Grid.RowProperty, counter);
                //uie.SetValue(Grid.ColumnSpanProperty, 1);
                ListItemDescription.Children.Add(uie);
                counter++;
            };
            ListItemPopup.IsOpen = true;
            */
        }

        private void GetItemDescriptionElements(string itemID)
        {
            using (var wClient = new WebClient())
            {
                wClient.Encoding = Encoding.UTF8;
                try
                {
                    Uri u = new Uri("http://bd.youxidudu.com/db/api/iteminfos.php?id=" + itemID);
                    wClient.DownloadStringCompleted += WClient_DownloadStringCompleted;
                    wClient.DownloadStringAsync(u);
                }
                catch (Exception)
                {
                    return;
                }
            }
        }
        /*
        private List<UIElement> GetItemDescriptionElements(string itemID)
        {
            string json = "";
            using (var wClient = new WebClient())
            {
                wClient.Encoding = Encoding.UTF8;
                try
                {
                    Uri u = new Uri("http://bd.youxidudu.com/db/api/iteminfos.php?id=" + itemID);
                    json = wClient.DownloadString(u);
                }catch (Exception)
                {
                    List<UIElement> ne = new List<UIElement>();
                    TextBlock etb = new TextBlock() { Text = "ERROR IN NETWORK QUERY!!!" };
                    ne.Add(etb);
                    return ne;
                }
            }
            RawItemInfo rInfo = JsonConvert.DeserializeObject<RawItemInfo>(json);
            string RawDescriptionStr = rInfo.Description;
            if (string.IsNullOrEmpty(RawDescriptionStr))
            {
                List<UIElement> ne = new List<UIElement>();
                TextBlock etb = new TextBlock() { Text = "没有描述." };
                ne.Add(etb);
                return ne;
            }
            string[] DescriptionLines = RawDescriptionStr.Split(new string[] { @"<br>" }, StringSplitOptions.RemoveEmptyEntries);
            List<UIElement> lui = new List<UIElement>();
            for(int i=0;i<DescriptionLines.Length;i++)
            {
                if (DescriptionLines[i].Contains("span"))
                {
                    WrapPanel sp = new WrapPanel
                    {
                        HorizontalAlignment = HorizontalAlignment.Left,
                        MaxHeight = 100,
                        Margin = new Thickness(0)
                    };
                    var thisLine = DescriptionLines[i];
                    var LineSpans = thisLine.Split(new string[] { "<span style=color:", @"</span>" }, StringSplitOptions.RemoveEmptyEntries);
                    SolidColorBrush LastForegroundBrush = new SolidColorBrush();
                    sp.Orientation = Orientation.Horizontal;
                    sp.HorizontalAlignment = HorizontalAlignment.Left;
                    foreach(string content in LineSpans)
                    {
                        TextBlock tb = new TextBlock
                        {
                            TextWrapping = TextWrapping.Wrap,
                            HorizontalAlignment = HorizontalAlignment.Left,
                            Margin = new Thickness(0)
                        };
                        if (content.StartsWith("#"))
                        {
                            tb.Foreground = (SolidColorBrush)(new BrushConverter().ConvertFrom(content.Substring(0, 7)));
                            LastForegroundBrush = (SolidColorBrush)(new BrushConverter().ConvertFrom(content.Substring(0, 7)));
                            var realcontent = content.Replace(@"</span>", "").Substring(8).Replace("/span>","").Replace("<","").Trim();
                            tb.Text = realcontent;
                        }
                        else
                        {
                            tb.Text = content.Replace(@"</span>", "").Trim();
                        }
                        sp.Children.Add(tb);
                    }
                    lui.Add(sp);
                }else
                {
                    TextBlock tb = new TextBlock
                    {
                        TextWrapping = TextWrapping.Wrap,
                        Text = DescriptionLines[i].Trim()
                    };
                    lui.Add(tb);
                }
            }
            return lui;
        }
        */
        private void WClient_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            stpHintData.Visibility = Visibility.Visible;
            string json = "";
            List<UIElement> ne = new List<UIElement>();
            if (e.Error != null)
            {
                TextBlock etb = new TextBlock() { Text = "ERROR IN NETWORK QUERY!!!" };
                ne.Add(etb);
            }
            else
            {
                json = e.Result;
                RawItemInfo rInfo = JsonConvert.DeserializeObject<RawItemInfo>(json);
                string RawDescriptionStr = rInfo.Description;
                Material m = new Material(Convert.ToInt32(rInfo.item_id, 10), rInfo.IconImageFile, rInfo.ItemName, 0);
                ItemDescription.Children.Clear();
                stpHintData.DataContext = m;
                
                if (string.IsNullOrEmpty(RawDescriptionStr))
                {
                    TextBlock etb = new TextBlock() { Text = "没有描述." };
                    ne.Add(etb);
                    goto PostProcess;
                }
                string[] DescriptionLines = RawDescriptionStr.Split(new string[] { @"<br>" }, StringSplitOptions.RemoveEmptyEntries);
                
                for (int i = 0; i < DescriptionLines.Length; i++)
                {
                    if (DescriptionLines[i].Contains("span"))
                    {
                        WrapPanel sp = new WrapPanel
                        {
                            HorizontalAlignment = HorizontalAlignment.Left,
                            MaxHeight = 100,
                            Margin = new Thickness(0)
                        };
                        var thisLine = DescriptionLines[i];
                        var LineSpans = thisLine.Split(new string[] { "<span style=color:", @"</span>" }, StringSplitOptions.RemoveEmptyEntries);
                        SolidColorBrush LastForegroundBrush = new SolidColorBrush();
                        sp.Orientation = Orientation.Horizontal;
                        sp.HorizontalAlignment = HorizontalAlignment.Left;
                        foreach (string content in LineSpans)
                        {
                            TextBlock tb = new TextBlock
                            {
                                TextWrapping = TextWrapping.Wrap,
                                HorizontalAlignment = HorizontalAlignment.Left,
                                Margin = new Thickness(0)
                            };
                            if (content.StartsWith("#"))
                            {
                                tb.Foreground = (SolidColorBrush)(new BrushConverter().ConvertFrom(content.Substring(0, 7)));
                                LastForegroundBrush = (SolidColorBrush)(new BrushConverter().ConvertFrom(content.Substring(0, 7)));
                                var realcontent = content.Replace(@"</span>", "").Substring(8).Replace("/span>", "").Replace("<", "").Trim();
                                tb.Text = realcontent;
                            }
                            else
                            {
                                tb.Text = content.Replace(@"</span>", "").Trim();
                            }
                            sp.Children.Add(tb);
                        }
                        ne.Add(sp);
                    }
                    else
                    {
                        TextBlock tb = new TextBlock
                        {
                            TextWrapping = TextWrapping.Wrap,
                            Text = DescriptionLines[i].Trim()
                        };
                        ne.Add(tb);
                    }
                }
            }
            PostProcess:
            int counter = 0;
            foreach (UIElement uie in ne)
            {
                //ListItemDescription.RowDefinitions.Add(new RowDefinition());
                //uie.SetValue(Grid.RowProperty, counter);
                //uie.SetValue(Grid.ColumnSpanProperty, 1);
                ItemDescription.Children.Add(uie);
                counter++;
            };
        }

        private void ListViewItem_MouseLeave(object sender, MouseEventArgs e)
        {
            //ListItemPopup.IsOpen = false;
            stpHintData.Visibility = Visibility.Collapsed;
        }

        private void BtnCalculateAllRecipes_Click(object sender, RoutedEventArgs e)
        {
            List<ResultHitchary> ResultList = Treeview_Result.ItemsSource as List<ResultHitchary>;
            Dictionary<string, int> StatCount = new Dictionary<string, int>();
            foreach (ResultHitchary item in ResultList)
            {
                if (StatCount.ContainsKey(item.ItemName))
                {
                    StatCount[item.ItemName] += Convert.ToInt32(item.ItemCount);
                }
                else
                {
                    StatCount.Add(item.ItemName, Convert.ToInt32(item.ItemCount));
                }
                if (item.Children != null)
                {
                    foreach (KeyValuePair<string, int> it in GetLayerStat(item.Children.ToList()))
                    {
                        if (StatCount.ContainsKey(it.Key))
                        {
                            StatCount[it.Key] += it.Value;
                        }
                        else
                        {
                            StatCount.Add(it.Key, it.Value);
                        }
                    }
                }
            }
            string statResults = "总计数据：";
            foreach (KeyValuePair<string, int> item in StatCount)
            {
                statResults += string.Format("{0}: {1} 个\r\n", new string[] { item.Key, item.Value.ToString() });
            }
            statResults += "\r\n";
            MessageBox.Show(statResults, "统计结果", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private Dictionary<string, int> GetLayerStat(List<ResultHitchary> input)
        {
            Dictionary<string, int> StatCount = new Dictionary<string, int>();
            foreach (ResultHitchary item in input)
            {
                if (StatCount.ContainsKey(item.ItemName))
                {
                    StatCount[item.ItemName] += Convert.ToInt32(item.ItemCount);
                }
                else
                {
                    StatCount.Add(item.ItemName, Convert.ToInt32(item.ItemCount));
                }
                if (item.Children != null)
                {
                    foreach (KeyValuePair<string, int> it in GetLayerStat(item.Children.ToList()))
                    {
                        if (StatCount.ContainsKey(it.Key))
                        {
                            StatCount[it.Key] += it.Value;
                        }
                        else
                        {
                            StatCount.Add(it.Key, it.Value);
                        }
                    }
                }
            }
            return StatCount;
        }

        private void ProductionGrid_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                BtnCalculate.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            }
        }

        private void TreeViewItem_MouseEnter(object sender, MouseEventArgs e)
        {
            TreeViewItem lvi = e.Source as TreeViewItem;
            ResultHitchary m = lvi.Header as ResultHitchary;
            if (m.ItemName.Contains("合成表"))
            {
                return;
            }
            //TreeViewPopup.DataContext = m;
            //TreeViewPopup.PlacementTarget = lvi;
            //TreeViewPopup.Placement = PlacementMode.MousePoint;
            //List<UIElement> DescriptionElements = GetItemDescriptionElements(m.ItemID);
            GetItemDescriptionElements(m.ItemID);
            //TreeItemDescription.Children.Clear();
            //ListItemDescription.RowDefinitions.Clear();
            /*
            int counter = 0;
            foreach (UIElement uie in DescriptionElements)
            {
                //ListItemDescription.RowDefinitions.Add(new RowDefinition());
                //uie.SetValue(Grid.RowProperty, counter);
                //uie.SetValue(Grid.ColumnSpanProperty, 1);
                TreeItemDescription.Children.Add(uie);
                counter++;
            };
            TreeViewPopup.IsOpen = true;
            */
        }

        private void TreeViewItem_MouseLeave(object sender, MouseEventArgs e)
        {
            //TreeViewPopup.IsOpen = false;
            stpHintData.Visibility = Visibility.Collapsed;
        }

        private void TVTooptip_ToolTipOpening(object sender, ToolTipEventArgs e)
        {
            Debug.WriteLine(e.Source.ToString());
        }

        private void MenuExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown(0);
        }

        private void AboutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            WindowAbout wAbout = new WindowAbout();
            wAbout.ShowDialog();
        }

        private void OpenBDLink_Click(object sender, RoutedEventArgs e)
        {
            Process p = new Process();
            p.StartInfo.FileName = "http://bd.youxidudu.com/";
            p.Start();
        }

        private void MenuItem_Click_1(object sender, RoutedEventArgs e)
        {
            Process p = new Process();
            p.StartInfo.FileName = "http://bd.youxidudu.com/";
            p.Start();
        }

        private void GetHelpMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("敬请期待...", "未实现");
        }

        private void MainProductIcon_MouseEnter(object sender, MouseEventArgs e)
        {
            var tmp = (Productions)ProductSelection.SelectedValue;
            Material m = new Material(tmp.ProductID, tmp.ProductPictureURL, tmp.ProductName, 1);
            //ListItemPopup.DataContext = m;
            //ListItemPopup.PlacementTarget = e.Source as Image;
            //ListItemPopup.Placement = PlacementMode.MousePoint;
            //List<UIElement> DescriptionElements = GetItemDescriptionElements(m.MaterialID);
            GetItemDescriptionElements(m.MaterialID);
            /*
            ListItemDescription.Children.Clear();
            //ListItemDescription.RowDefinitions.Clear();
            int counter = 0;
            foreach (UIElement uie in DescriptionElements)
            {
                //ListItemDescription.RowDefinitions.Add(new RowDefinition());
                //uie.SetValue(Grid.RowProperty, counter);
                //uie.SetValue(Grid.ColumnSpanProperty, 1);
                ListItemDescription.Children.Add(uie);
                counter++;
            };
            ListItemPopup.IsOpen = true;
            */
        }

        private void MainProductIcon_MouseLeave(object sender, MouseEventArgs e)
        {
            /*
            if (ListItemPopup.IsOpen)
            {
                ListItemPopup.IsOpen = false;
            }
            */
            if (stpHintData.Visibility == Visibility.Visible)
            {
                stpHintData.Visibility = Visibility.Collapsed;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        protected virtual void Dispose(bool disposing)
        {
            if (sConn != null)
            {
                if (sConn.State != ConnectionState.Broken && sConn.State != ConnectionState.Closed)
                {
                    sConn.Close();
                    sConn.Dispose();
                }
            }
        }

        ~MainWindow()
        {
            Dispose(false);
        }
    }
}