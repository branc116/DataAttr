using DataAtr.Models;
using DataAtr.Models.Html;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataAtr
{
    public class ParsingObject
    {
        public static readonly new HashSet<char> SpecialCSharpChars = new HashSet<char> { '.', ' ', '\n', '\r', '?', ':', '[', ']', '"', '\'' };
        public static readonly new HashSet<char> ParenthsOpen = new HashSet<char> { '{', '[', '(' };
        public static readonly new HashSet<char> WhiteSpace = new HashSet<char> { ' ', '\t', '\n', '\r' };
        public static readonly new Dictionary<char, char> ParenthsPair = new Dictionary<char, char> { { '}', '{' }, { ']', '[' }, { ')', '(' } };
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        public State State { get; set; }
        public Stack<ParsingObject> StateStack { get; set; }
        public HtmlObject CurrentHtmlObject { get; set; }
        public string RawHtml { get; set; }
        public int Index { get; set; }
        public string CurrentToken { get; set; }
        public List<ParsingObject> TokenParameters { get; set; }
        public Stack<char> ParenthStack { get; set; }
        public string GetString => ToString();
        public string Html => RawHtml.Substring(_startIndex, _endIndex - _startIndex);
        private int _startIndex;
        private int _endIndex;
        private bool _important;
        private DataAtrModel currentAttr = new DataAtrModel(string.Empty, false);
        public ParsingObject ToStack(State newState, bool important = true)
        {
            logger.Debug($"Push {newState.ToString()}");
            if (important)
                CurrentToken += '{' + TokenParameters.Count.ToString() + '}';
            StateStack.Push(this);
            var newObject = new ParsingObject
            {
                State = newState,
                Index = Index,
                CurrentHtmlObject = new HtmlObject
                {
                    Attributes = new List<DataAtrModel>(),
                    Children = new List<HtmlObject>(),
                    Parent = CurrentHtmlObject,
                    Tag = string.Empty
                },
                CurrentToken = string.Empty,
                ParenthStack = new Stack<char>(),
                RawHtml = RawHtml,
                StateStack = StateStack,
                TokenParameters = new List<ParsingObject>()
            };
            newObject._startIndex = Index;
            newObject._important = important;
            return newObject;
        }
        public ParsingObject Pop()
        {
            logger.Debug($"Pop {State}");
            _endIndex = Index;
            if (ParenthStack.Count != 0)
                throw new Exception("Parenths error");
            
            var oldObj = StateStack.Any() ? StateStack.Pop() : this;
            if (_important)
                oldObj.TokenParameters.Add(this);
            oldObj.Index = Index;
            return oldObj;
        }
        public ParsingObject PopAttributeVariable()
        {
            logger.Debug($"PopAttributeVariable {State}");
            var attributeValue = StateStack.Pop();
            var attribute = attributeValue.StateStack.Pop();
            var element = attribute.StateStack.Pop();
            element.currentAttr.ValueParameters.Add(CurrentToken);
            StateStack.Push(element);
            StateStack.Push(attribute);
            attributeValue.Index = Index;
            attributeValue.TokenParameters.Add(this);
            return attributeValue;

        }
        public ParsingObject PopAttributeValue()
        {
            logger.Debug($"PopAttributeValue {State}");
            var attribute = StateStack.Pop();
            var element = attribute.StateStack.Pop();
            element.currentAttr.ValueTemplate = CurrentToken;
            attribute.Index = Index;
            attribute.StateStack.Push(element);
            return attribute;
        }
        public ParsingObject PopAttribute()
        {
            logger.Debug($"PopAttribute {State}");
            var element = StateStack.Pop();
            element.currentAttr.AtrName = CurrentToken;
            //element.currentAttr.AttributeType = CurrentToken == "id" ? AttributeType.Id : AttributeType.DataAttribute;
            element.currentAttr.HasValue = !string.IsNullOrWhiteSpace(element.currentAttr.ValueTemplate);
            if (!string.IsNullOrWhiteSpace(element.currentAttr.AtrName) && !element.currentAttr.AtrName.Any(i => SpecialCSharpChars.Contains(i)) && element.currentAttr.ValueTemplate.IsFormatValid(element.currentAttr.ValueParameters.ToArray()))
                element.CurrentHtmlObject.Attributes.Add(element.currentAttr);

            element.currentAttr = new DataAtrModel(string.Empty, false);
            element.Index = Index;
            return element;
        }
        public ParsingObject AdvanceToNextNonWhiteSpace()
        {
            Index++;
            while (Index < RawHtml.Length && WhiteSpace.Contains(RawHtml[Index]))
                Index++;
            return this;
        }
        public override string ToString() {
            return ToString(0);
        }
        public string ToString(int indent)
        {
            var retStr = CurrentHtmlObject.ToString() + "\n" + 
                (TokenParameters.Any() ? TokenParameters.Select(i => new string(' ',indent) + i.ToString(indent + 4)).Aggregate((i, j) => i + "\n" + j) : string.Empty);
            return retStr;
        }
    }
    public class RazorParser
    {

        public Dictionary<(char curChar, State state), Func<ParsingObject, ParsingObject>> parser = new Dictionary<(char curChar, State state), Func<ParsingObject, ParsingObject>>
        {
#region State.Begin
            {('@', State.Begin), i => i.ToStack(State.CSharShajt) },
            {('<', State.Begin), i => {
                return i
                    .ToStack(State.HtmlElement)
                    .ToStack(State.HtmlStartTag, false)
                    .AdvanceToNextNonWhiteSpace();
            }},
            #endregion
#region State.HtmlStartTag
            {(' ', State.HtmlStartTag), i => {
                var old = i.Pop();
                old.CurrentHtmlObject.Tag = i.CurrentToken;
                old.AdvanceToNextNonWhiteSpace();
                return old.ToStack(State.HtmlOpenningBrackets, false);//.State = State.HtmlOpenningBrackets;
                
            }},
            {('>', State.HtmlStartTag), i => {
                var old =i.Pop();
                old.CurrentHtmlObject.Tag = i.CurrentToken;
                return old
                     .ToStack(State.HtmlInnerText, false)
                     .AdvanceToNextNonWhiteSpace();
            } },
            {('/', State.HtmlStartTag), i =>
            {
                var old = i.Pop();
                old.CurrentHtmlObject.Tag = i.CurrentToken;
                return old.Pop()
                    .ToStack(State.HtmlInnerText, false);
            } },
            #endregion
#region State.HtmlOpenningBrackets
            {(' ', State.HtmlOpenningBrackets), i => {
                var old = i.PopAttribute();
                old.AdvanceToNextNonWhiteSpace();
                return old.ToStack(State.HtmlOpenningBrackets, false);
            }},
            {('/', State.HtmlOpenningBrackets), i => {
                i.AdvanceToNextNonWhiteSpace();
                i.Index++;
                return i.PopAttribute()
                    .Pop()
                    .ToStack(State.HtmlInnerText, false)
                    .AdvanceToNextNonWhiteSpace();
            }},
            {('>', State.HtmlOpenningBrackets), i =>
            {
                var old = i.PopAttribute();
                if (old.CurrentHtmlObject.Tag == "link")
                {
                    return old
                        .Pop()
                        .AdvanceToNextNonWhiteSpace();
                }
                return old
                    .ToStack(State.HtmlInnerText, false)
                    .AdvanceToNextNonWhiteSpace();
            }},
            {('@', State.HtmlOpenningBrackets), i => i.ToStack(State.CSharShajt, false)
                                                      .AdvanceToNextNonWhiteSpace()},
            {('=', State.HtmlOpenningBrackets), i => i.ToStack(State.HtmlAttributeValue, false)
                                                        .AdvanceToNextNonWhiteSpace()
                                                        .AdvanceToNextNonWhiteSpace()},
            #endregion
#region State.HtmlInnerText
            {('<', State.HtmlInnerText), i =>
            {
                var nextChar = i.RawHtml[i.Index+1];
                if (nextChar == '/')
                {
                    var old = i
                        .Pop()
                        .Pop();
                    for(;old.Index < old.RawHtml.Length && old.RawHtml[old.Index] != '>'; old.Index++);
                    old.Index++;
                    return old
                        .ToStack(State.HtmlInnerText, false)
                        .AdvanceToNextNonWhiteSpace();
                }else
                {
                    return i
                        .Pop()
                        .ToStack(State.HtmlElement)
                        .ToStack(State.HtmlStartTag, false)
                        .AdvanceToNextNonWhiteSpace();
                }
            } },
            #endregion
#region State.HtmlAttributeValue
            {('"', State.HtmlAttributeValue), i => {
                return i.PopAttributeValue()
                        .PopAttribute()
                        .ToStack(State.HtmlOpenningBrackets)
                        .AdvanceToNextNonWhiteSpace();
                                                    } },
            {('@', State.HtmlAttributeValue), i => i.ToStack(State.CSharp)
                                                    .AdvanceToNextNonWhiteSpace()},
            {('$', State.HtmlAttributeValue), i => i.ToStack(State.TemplateVariable)
                                                    .AdvanceToNextNonWhiteSpace()
                                                    .AdvanceToNextNonWhiteSpace()},
          
            #endregion
#region State.CSharp
            {('"', State.CSharp), i => {
                if (i.ParenthStack.Count == 0)
                    return i.PopAttributeVariable();
                return i.ToStack(State.String)
                         .AdvanceToNextNonWhiteSpace();
            } },
            #endregion
#region State.String
            {('"', State.String), i => i.Pop()
                                        .AdvanceToNextNonWhiteSpace()},
            {('@', State.String), i => i.ToStack(State.CSharShajt)
                                        .AdvanceToNextNonWhiteSpace()},
            #endregion
#region State.CSharShajt
            {('<', State.CSharShajt), i => i.ToStack(State.HtmlStartTag)
                                            .AdvanceToNextNonWhiteSpace()},
            #endregion
#region State.TemplateVariable
            {('}', State.TemplateVariable), i => i.PopAttributeVariable()
                                                  .AdvanceToNextNonWhiteSpace()}
#endregion

        };
        private List<(AttributeType attributeType, string startIndex, List<char> endIndices)> parseCycles = new List<(AttributeType attributeType, string startIndex, List<char> endIndices)>
        {
            (AttributeType.DataAttribute, "data-", new List<char>{ ' ', '=', '>' }),
            (AttributeType.Id, "id", new List<char>{'=', '>', ' ' })
        };
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        public RazorParser()
        {
            foreach (var paranth in ParsingObject.ParenthsPair)
            {
                parser.Add((paranth.Key, State.CSharp), i =>
                {
                    if (i.ParenthStack.Any())
                    {
                        var p = i.ParenthStack.Pop();
                        if (p != paranth.Value)
                            throw new Exception("Paranth not ok");
                        if (i.ParenthStack.Count == 0)
                            return i.PopAttributeVariable()
                                    .AdvanceToNextNonWhiteSpace();
                    }
                    i.Index++;
                    return i;
                });
                parser.Add((paranth.Key, State.CSharShajt), i =>
                {
                    if (i.ParenthStack.Any())
                    {
                        var p = i.ParenthStack.Pop();
                        if (i.ParenthStack.Count == 0)
                            return i.Pop()
                                    .AdvanceToNextNonWhiteSpace();
                    }
                    i.Index++;
                    return i;
                });
                parser.Add((paranth.Value, State.CSharp), i =>
                {
                    i.ParenthStack.Push(paranth.Value);
                    i.Index++;
                    return i;
                });
                parser.Add((paranth.Value, State.CSharShajt), i =>
                {
                    i.ParenthStack.Push(paranth.Value);
                    i.Index++;
                    return i;
                });

            }
        }
        public ParsingObject ParseAllAll(string razorText)
        {
            razorText = RemoveShityLines(razorText);
            if (string.IsNullOrWhiteSpace(razorText))
                return null;
            var temp = new ParsingObject
            {
                CurrentHtmlObject = new HtmlObject
                {
                    Attributes = new List<DataAtrModel>(),
                    Children = new List<HtmlObject>(),
                    Parent = null,
                    Tag = string.Empty
                },
                CurrentToken = string.Empty,
                Index = 0,
                ParenthStack = new Stack<char>(),
                RawHtml = razorText,
                State = State.Begin,
                StateStack = new Stack<ParsingObject>(),
                TokenParameters = new List<ParsingObject>()
            };
            
            do
            {
                var v = temp.RawHtml[temp.Index];
                var state = temp.State;
                if (v == 'f')
                {
                    ;
                }
                if (parser.ContainsKey((v, state)))
                {
                    temp = parser[(v, state)](temp);
                    logger.Debug($"({v}, {state.ToString()}) => {temp.State}");
                }
                else
                {
                    if (!ParsingObject.WhiteSpace.Contains(v))
                        temp.CurrentToken += v;
                    temp.Index++;
                }
            } while (temp.Index < temp.RawHtml.Length);
            while (temp.StateStack.Count > 0) temp = temp.Pop();
            logger.Info($"\n-----------in------------{razorText}\n-----------out-----------{temp.ToString()}\n-------------------------");
            return temp;
        }
        public string RemoveShityLines(string razorWithShityLines)
        {
            return razorWithShityLines
                .Replace("\r\n", "\n")
                .Replace('\'', '"')
                .Split('\n')
                .Select(i => i.Trim())
                .Where(i => !string.IsNullOrWhiteSpace(i))
                .Where(i => i[0] != '@')
                .AggregateOrNull((i, j) => i + '\n' + j);
        }
    }
}
