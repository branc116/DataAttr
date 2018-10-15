namespace DataAtr
{
    public enum State
    {
        HtmlBegin,
        HtmlStartTag,
        HtmlOpenningBrackets,
        HtmlAttributeName,
        HtmlAttributeValue,
        HtmlInnerText,
        CSharShajt,

        Begin,
        String,
        Razor,
        CSharp,
        TemplateVariable,
        End,
        Invalid,
        HtmlElement
    }
}
