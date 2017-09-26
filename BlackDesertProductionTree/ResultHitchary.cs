using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;

namespace BlackDesertProductionTree
{
    public class ResultHitchary
    {
        public string URL { get; set; }
        public string ItemID { get; set; }
        public string ItemName { get; set; }
        public string OriginalItemName { get
            {
                return ItemName.Replace("或", "");
            }
        }
        public string ItemCount { get; set; }
        public string ItemTimeCost { get; set; }
        public string ItemSkillRequire { get; set; }

        public IEnumerable<ResultHitchary> Children { get; set; }

        public ResultHitchary()
        {
            URL = "";
            ItemName = "";
            ItemCount = "";
            Children = null;
        }

        public ResultHitchary(string id, string Url, string itemName, string itemCount)
        {
            ItemID = id;
            URL = Url;
            ItemName = itemName;
            ItemCount = itemCount;
        }
        public ResultHitchary(string id, string Url, string itemName, string itemCount, string timecost , string skillreq)
        {
            ItemID = id;
            URL = Url;
            ItemName = itemName;
            ItemCount = itemCount;
            ItemTimeCost = timecost;
            ItemSkillRequire = skillreq;
        }
        public ResultHitchary(string id, string Url, string itemName, string itemCount, IEnumerable<ResultHitchary> child)
        {
            ItemID = id;
            URL = Url;
            ItemName = itemName;
            ItemCount = itemCount;
            Children = child;
        }
    }
}
