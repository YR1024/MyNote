using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace WordReporter
{
    public class TextProcess
    {    
        public List<WordParagraph> WordParagraphList = new List<WordParagraph>();

        public WordprocessingDocument WordDocument;


      

        public TextProcess(WordprocessingDocument wpd) 
        {
            WordDocument = wpd;

            List<Paragraph> ParagraphList = WordDocument.MainDocumentPart.Document.Descendants<Paragraph>().ToList();
            foreach (var par in ParagraphList)
            {
                WordParagraphList.Add(new WordParagraph(par));
            }

        }    

        public void ParseWordParagraph(Dictionary<string, object> TextRepaceDictionary)
        {
            foreach (var textData in TextRepaceDictionary)
            {
                foreach (var wordParagrap in WordParagraphList)
                {
                    RepaceTextOfParagraph(wordParagrap, textData.Key, textData.Value.ToString());
                }
            }
          
        }



        void RepaceTextOfParagraph(WordParagraph wordParg, string key, string val)
        {
            try
            {
                var wtList = wordParg.WordTextList;
                int txtBeginIndex = wordParg.ParagraphText.IndexOf(key);
                int txtEndIndex = txtBeginIndex + key.Length - 1;
                if (txtBeginIndex != -1)
                {
                    int BeginWordText = 0;
                    int EndWordText = 0;
                    for (int i = 0; i < wtList.Count; i++)
                    {
                        if (wtList[i].FirstCharIndexInParagraph >= txtBeginIndex)
                        {
                            BeginWordText = i;
                            EndWordText = i;
                            for (int j = i; j < wtList.Count; j++)
                            {
                                if (wtList[j].LastCharIndexInParagraph >= txtEndIndex)
                                {
                                    EndWordText = j;
                                    break;
                                }
                            }
                            break;
                        }
                    }

                    List<WordText> WordTextList = new List<WordText>();
                    for (int i = BeginWordText; i <= EndWordText; i++)
                    {
                        WordTextList.Add(wtList[i]);
                    }

                    string oldText = WordParagraph.GetWordTextListText(WordTextList);
                    string newText = string.Empty;
                    Regex regexText = new Regex(key);
                    newText = regexText.Replace(oldText, val);


                    var run = WordTextList[0].Text.Parent;
                    var text = WordTextList[0].Text.Clone() as Text;
                    text.Text = newText;
                    foreach (var wt in WordTextList)
                    {
                        //var r = wt.Text.Parent;
                        if (wt.Text.Parent != null)
                        {
                            wt.Text.Remove();
                        }
                        //r.Remove();
                    }
                    if (run == null)
                    {
                    }
                    else
                    {
                        run.Append(text);
                    }
                }
            }
            catch(System.Exception ex)
            {
                throw ex;
            }
           
        }

    }


    public class WordParagraph
    {
        public List<WordText> WordTextList =new List<WordText>();

        private string _paragraphText;

        public string ParagraphText
        {
            get { return _paragraphText; }
            set { _paragraphText = value; }
        }


        public Paragraph Paragraph;

        public WordParagraph(Paragraph paragraph) 
        {
            Paragraph = paragraph;
            List<Text> TextList = Paragraph.Descendants<Text>().ToList();
            var index = 0; 
            foreach (var item in TextList)
            {
                var wt = new WordText(item);
                if(WordTextList.Count == 0)
                {
                    wt.FirstCharIndexInParagraph = 0;
                    wt.LastCharIndexInParagraph = wt.Value.Length - 1;
                }
                else
                {
                    wt.FirstCharIndexInParagraph = index;
                    wt.LastCharIndexInParagraph = index + wt.Value.Length - 1;
                }
                WordTextList.Add(wt);
                index += wt.Value.Length;
            }
            GetParagraphText();
        }

        public string GetParagraphText()
        {
            ParagraphText = string.Empty;
            foreach (var t in WordTextList)
            {
                ParagraphText += t.Value;
            }
            return ParagraphText;
        }


        public static string GetWordTextListText(List<WordText> wordTextList)
        {
            string text = string.Empty;
            foreach (var t in wordTextList)
            {
                text += t.Value;
            }
            return text;
        }
    }

    public class WordText
    {
        public Text Text;

        public string Value
        {
            get { return Text.Text; }
            set { Text.Text = value; }
        }

        public int FirstCharIndexInParagraph;

        public int LastCharIndexInParagraph;

        public WordText(Text text) 
        {
            Text = text;
        }

        public void SetValue(string value) 
        {
            Value = value;
        }
    }
}
