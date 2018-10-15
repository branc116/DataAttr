using System.Collections.Generic;
using System.Linq;

namespace DataAtr.Models
{
    public class DataAtrModel
    {
        public DataAtrModel(string atrName, bool hasValue)
        {
            AtrName = atrName;
            HasValue = hasValue;
            ValueTemplate = string.Empty;
            ValueTemplate = string.Empty;
            ValueParameters = new List<string>();
        }
        public DataAtrModel(string atrName, bool hasValue, string valueTemplate, List<string> valueParameters) : this(atrName, hasValue)
        {
            ValueTemplate = valueTemplate;
            ValueParameters = valueParameters;
        }
        public DataAtrModel(string atrName, bool hasValue, AttributeType attributeType) : this(atrName, hasValue)
        {
            //AttributeType = attributeType;
        }
        public DataAtrModel(string atrName, bool hasValue, string valueTemplate, List<string> valueParameters, AttributeType attributeType) : this(atrName, hasValue, valueTemplate, valueParameters)
        {
            //AttributeType = attributeType;
        }

        public string AtrName { get; set; }
        public bool HasValue { get; set; }
        public string ValueTemplate { get; set; }
        public List<string> ValueParameters { get; set; }
        public AttributeType AttributeType => AtrName.StartsWith("asp-") ? AttributeType.TagHelper :
                                              AtrName.StartsWith("data-") ? AttributeType.DataAttribute :
                                              AtrName == "id" ? AttributeType.Id :
                                              AtrName == "class" ? AttributeType.Class :
                                              AtrName == "style" ? AttributeType.Style :
                                              AttributeType.Attribute;
        public override string ToString()
        {
            if(ValueParameters.Any())
                return AtrName + "='" + ValueTemplate + "', " + ValueParameters.Aggregate((i, j) => i + ", " + j) + ": " + AttributeType.ToString();
            return AtrName + ": " + AttributeType.ToString();
        }
    }
    public enum AttributeType
    {
        Id,
        DataAttribute,
        TagHelper,
        Attribute,
        Class,
        Style
    }
}