using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataAtr.Models.Html
{
    public class HtmlObject
    {
        public HtmlObject()
        {
            Attributes = new List<DataAtrModel>();
            Children = new List<HtmlObject>();
        }
        public string Tag { get; set; }
        public List<DataAtrModel> Attributes { get; set; }
        public List<HtmlObject> Children { get; set; }
        public HtmlObject Parent { get; set; }

        public string RawHtml => rawHtml.Substring(startIndex, endIndex - startIndex);

        private string rawHtml;
        private int startIndex;
        private int endIndex;
        public override string ToString()
        {
            if (Attributes.Any())
                return Tag + ": (" + Attributes.Select(i => i.ToString()).Aggregate((i, j) => i + "; " + j) + ")";
            return Tag;
        }

    }
}
