using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Collections.Generic;
using System.Linq;

namespace WordReporter
{
    public static class TableHelper
    {
        public static Table GenerateTestTable()
        {
            Table table1 = new Table();

            TableProperties tableProperties1 = new TableProperties();
            TableStyle tableStyle1 = new TableStyle() { Val = "a3" };
            TableWidth tableWidth1 = new TableWidth() { Width = "0", Type = TableWidthUnitValues.Auto };
            TableLook tableLook1 = new TableLook() { Val = "04A0", FirstRow = true, LastRow = false, FirstColumn = true, LastColumn = false, NoHorizontalBand = false, NoVerticalBand = true };
            TableCaption tableCaption1 = new TableCaption() { Val = "袁煣创建的表格" };

            tableProperties1.Append(tableStyle1);
            tableProperties1.Append(tableWidth1);
            tableProperties1.Append(tableLook1);
            tableProperties1.Append(tableCaption1);

            TableGrid tableGrid1 = new TableGrid();
            GridColumn gridColumn1 = new GridColumn() { Width = "3375" };//3539
            GridColumn gridColumn2 = new GridColumn() { Width = "2755" };//2835
            GridColumn gridColumn3 = new GridColumn() { Width = "2166" };

            tableGrid1.Append(gridColumn1);
            tableGrid1.Append(gridColumn2);
            tableGrid1.Append(gridColumn3);

            TableRow tableRow1 = new TableRow() { RsidTableRowAddition = "00A52F13", RsidTableRowProperties = "00A52F13", ParagraphId = "0103EE26", TextId = "77777777" };

            TableCell tableCell1 = new TableCell();

            TableCellProperties tableCellProperties1 = new TableCellProperties();
            TableCellWidth tableCellWidth1 = new TableCellWidth() { Width = "3539", Type = TableWidthUnitValues.Dxa };

            tableCellProperties1.Append(tableCellWidth1);

            Paragraph paragraph1 = new Paragraph() { RsidParagraphMarkRevision = "00A52F13", RsidParagraphAddition = "00A52F13", RsidRunAdditionDefault = "00A52F13", ParagraphId = "0B1F3C27", TextId = "7E635EC4" };

            ParagraphProperties paragraphProperties1 = new ParagraphProperties();

            ParagraphMarkRunProperties paragraphMarkRunProperties1 = new ParagraphMarkRunProperties();
            RunFonts runFonts1 = new RunFonts() { Hint = FontTypeHintValues.EastAsia, Ascii = "宋体", HighAnsi = "宋体", EastAsia = "宋体" };
            FontSize fontSize1 = new FontSize() { Val = "30" };
            FontSizeComplexScript fontSizeComplexScript1 = new FontSizeComplexScript() { Val = "30" };

            paragraphMarkRunProperties1.Append(runFonts1);
            paragraphMarkRunProperties1.Append(fontSize1);
            paragraphMarkRunProperties1.Append(fontSizeComplexScript1);

            paragraphProperties1.Append(paragraphMarkRunProperties1);

            Run run1 = new Run() { RsidRunProperties = "00A52F13" };

            RunProperties runProperties1 = new RunProperties();
            RunFonts runFonts2 = new RunFonts() { Ascii = "宋体", HighAnsi = "宋体", EastAsia = "宋体" };
            FontSize fontSize2 = new FontSize() { Val = "30" };
            FontSizeComplexScript fontSizeComplexScript2 = new FontSizeComplexScript() { Val = "30" };

            runProperties1.Append(runFonts2);
            runProperties1.Append(fontSize2);
            runProperties1.Append(fontSizeComplexScript2);
            LastRenderedPageBreak lastRenderedPageBreak1 = new LastRenderedPageBreak();
            Text text1 = new Text();
            text1.Text = "C";

            run1.Append(runProperties1);
            run1.Append(lastRenderedPageBreak1);
            run1.Append(text1);

            Run run2 = new Run() { RsidRunProperties = "00A52F13" };

            RunProperties runProperties2 = new RunProperties();
            RunFonts runFonts3 = new RunFonts() { Hint = FontTypeHintValues.EastAsia, Ascii = "宋体", HighAnsi = "宋体", EastAsia = "宋体" };
            FontSize fontSize3 = new FontSize() { Val = "30" };
            FontSizeComplexScript fontSizeComplexScript3 = new FontSizeComplexScript() { Val = "30" };

            runProperties2.Append(runFonts3);
            runProperties2.Append(fontSize3);
            runProperties2.Append(fontSizeComplexScript3);
            Text text2 = new Text();
            text2.Text = "ol";

            run2.Append(runProperties2);
            run2.Append(text2);

            Run run3 = new Run() { RsidRunProperties = "00A52F13" };

            RunProperties runProperties3 = new RunProperties();
            RunFonts runFonts4 = new RunFonts() { Ascii = "宋体", HighAnsi = "宋体", EastAsia = "宋体" };
            FontSize fontSize4 = new FontSize() { Val = "30" };
            FontSizeComplexScript fontSizeComplexScript4 = new FontSizeComplexScript() { Val = "30" };

            runProperties3.Append(runFonts4);
            runProperties3.Append(fontSize4);
            runProperties3.Append(fontSizeComplexScript4);
            Text text3 = new Text();
            text3.Text = "umnHeader1";

            run3.Append(runProperties3);
            run3.Append(text3);

            paragraph1.Append(paragraphProperties1);
            paragraph1.Append(run1);
            paragraph1.Append(run2);
            paragraph1.Append(run3);

            tableCell1.Append(tableCellProperties1);
            tableCell1.Append(paragraph1);

            TableCell tableCell2 = new TableCell();

            TableCellProperties tableCellProperties2 = new TableCellProperties();
            TableCellWidth tableCellWidth2 = new TableCellWidth() { Width = "2835", Type = TableWidthUnitValues.Dxa };

            tableCellProperties2.Append(tableCellWidth2);

            Paragraph paragraph2 = new Paragraph() { RsidParagraphAddition = "00A52F13", RsidRunAdditionDefault = "00A52F13", ParagraphId = "485357CB", TextId = "7FC9F798" };

            ParagraphProperties paragraphProperties2 = new ParagraphProperties();

            ParagraphMarkRunProperties paragraphMarkRunProperties2 = new ParagraphMarkRunProperties();
            RunFonts runFonts5 = new RunFonts() { Hint = FontTypeHintValues.EastAsia };

            paragraphMarkRunProperties2.Append(runFonts5);

            paragraphProperties2.Append(paragraphMarkRunProperties2);

            Run run4 = new Run() { RsidRunProperties = "00A52F13" };

            RunProperties runProperties4 = new RunProperties();
            RunFonts runFonts6 = new RunFonts() { Ascii = "宋体", HighAnsi = "宋体", EastAsia = "宋体" };
            FontSize fontSize5 = new FontSize() { Val = "30" };
            FontSizeComplexScript fontSizeComplexScript5 = new FontSizeComplexScript() { Val = "30" };

            runProperties4.Append(runFonts6);
            runProperties4.Append(fontSize5);
            runProperties4.Append(fontSizeComplexScript5);
            Text text4 = new Text();
            text4.Text = "C";

            run4.Append(runProperties4);
            run4.Append(text4);

            Run run5 = new Run() { RsidRunProperties = "00A52F13" };

            RunProperties runProperties5 = new RunProperties();
            RunFonts runFonts7 = new RunFonts() { Hint = FontTypeHintValues.EastAsia, Ascii = "宋体", HighAnsi = "宋体", EastAsia = "宋体" };
            FontSize fontSize6 = new FontSize() { Val = "30" };
            FontSizeComplexScript fontSizeComplexScript6 = new FontSizeComplexScript() { Val = "30" };

            runProperties5.Append(runFonts7);
            runProperties5.Append(fontSize6);
            runProperties5.Append(fontSizeComplexScript6);
            Text text5 = new Text();
            text5.Text = "ol";

            run5.Append(runProperties5);
            run5.Append(text5);

            Run run6 = new Run() { RsidRunProperties = "00A52F13" };

            RunProperties runProperties6 = new RunProperties();
            RunFonts runFonts8 = new RunFonts() { Ascii = "宋体", HighAnsi = "宋体", EastAsia = "宋体" };
            FontSize fontSize7 = new FontSize() { Val = "30" };
            FontSizeComplexScript fontSizeComplexScript7 = new FontSizeComplexScript() { Val = "30" };

            runProperties6.Append(runFonts8);
            runProperties6.Append(fontSize7);
            runProperties6.Append(fontSizeComplexScript7);
            Text text6 = new Text();
            text6.Text = "umnHeader";

            run6.Append(runProperties6);
            run6.Append(text6);

            Run run7 = new Run();

            RunProperties runProperties7 = new RunProperties();
            RunFonts runFonts9 = new RunFonts() { Ascii = "宋体", HighAnsi = "宋体", EastAsia = "宋体" };
            FontSize fontSize8 = new FontSize() { Val = "30" };
            FontSizeComplexScript fontSizeComplexScript8 = new FontSizeComplexScript() { Val = "30" };

            runProperties7.Append(runFonts9);
            runProperties7.Append(fontSize8);
            runProperties7.Append(fontSizeComplexScript8);
            Text text7 = new Text();
            text7.Text = "2";

            run7.Append(runProperties7);
            run7.Append(text7);

            paragraph2.Append(paragraphProperties2);
            paragraph2.Append(run4);
            paragraph2.Append(run5);
            paragraph2.Append(run6);
            paragraph2.Append(run7);

            tableCell2.Append(tableCellProperties2);
            tableCell2.Append(paragraph2);

            TableCell tableCell3 = new TableCell();

            TableCellProperties tableCellProperties3 = new TableCellProperties();
            TableCellWidth tableCellWidth3 = new TableCellWidth() { Width = "1701", Type = TableWidthUnitValues.Dxa };

            tableCellProperties3.Append(tableCellWidth3);

            Paragraph paragraph3 = new Paragraph() { RsidParagraphAddition = "00A52F13", RsidRunAdditionDefault = "00A52F13", ParagraphId = "405BE65B", TextId = "013253F3" };

            ParagraphProperties paragraphProperties3 = new ParagraphProperties();

            ParagraphMarkRunProperties paragraphMarkRunProperties3 = new ParagraphMarkRunProperties();
            RunFonts runFonts10 = new RunFonts() { Hint = FontTypeHintValues.EastAsia };

            paragraphMarkRunProperties3.Append(runFonts10);

            paragraphProperties3.Append(paragraphMarkRunProperties3);

            Run run8 = new Run() { RsidRunProperties = "00A52F13" };

            RunProperties runProperties8 = new RunProperties();
            RunFonts runFonts11 = new RunFonts() { Ascii = "宋体", HighAnsi = "宋体", EastAsia = "宋体" };
            FontSize fontSize9 = new FontSize() { Val = "30" };
            FontSizeComplexScript fontSizeComplexScript9 = new FontSizeComplexScript() { Val = "30" };

            runProperties8.Append(runFonts11);
            runProperties8.Append(fontSize9);
            runProperties8.Append(fontSizeComplexScript9);
            Text text8 = new Text();
            text8.Text = "C";

            run8.Append(runProperties8);
            run8.Append(text8);

            Run run9 = new Run() { RsidRunProperties = "00A52F13" };

            RunProperties runProperties9 = new RunProperties();
            RunFonts runFonts12 = new RunFonts() { Hint = FontTypeHintValues.EastAsia, Ascii = "宋体", HighAnsi = "宋体", EastAsia = "宋体" };
            FontSize fontSize10 = new FontSize() { Val = "30" };
            FontSizeComplexScript fontSizeComplexScript10 = new FontSizeComplexScript() { Val = "30" };

            runProperties9.Append(runFonts12);
            runProperties9.Append(fontSize10);
            runProperties9.Append(fontSizeComplexScript10);
            Text text9 = new Text();
            text9.Text = "ol";

            run9.Append(runProperties9);
            run9.Append(text9);

            Run run10 = new Run() { RsidRunProperties = "00A52F13" };

            RunProperties runProperties10 = new RunProperties();
            RunFonts runFonts13 = new RunFonts() { Ascii = "宋体", HighAnsi = "宋体", EastAsia = "宋体" };
            FontSize fontSize11 = new FontSize() { Val = "30" };
            FontSizeComplexScript fontSizeComplexScript11 = new FontSizeComplexScript() { Val = "30" };

            runProperties10.Append(runFonts13);
            runProperties10.Append(fontSize11);
            runProperties10.Append(fontSizeComplexScript11);
            Text text10 = new Text();
            text10.Text = "umnHeader";

            run10.Append(runProperties10);
            run10.Append(text10);

            Run run11 = new Run();

            RunProperties runProperties11 = new RunProperties();
            RunFonts runFonts14 = new RunFonts() { Ascii = "宋体", HighAnsi = "宋体", EastAsia = "宋体" };
            FontSize fontSize12 = new FontSize() { Val = "30" };
            FontSizeComplexScript fontSizeComplexScript12 = new FontSizeComplexScript() { Val = "30" };

            runProperties11.Append(runFonts14);
            runProperties11.Append(fontSize12);
            runProperties11.Append(fontSizeComplexScript12);
            Text text11 = new Text();
            text11.Text = "3";

            run11.Append(runProperties11);
            run11.Append(text11);

            paragraph3.Append(paragraphProperties3);
            paragraph3.Append(run8);
            paragraph3.Append(run9);
            paragraph3.Append(run10);
            paragraph3.Append(run11);

            tableCell3.Append(tableCellProperties3);
            tableCell3.Append(paragraph3);

            tableRow1.Append(tableCell1);
            tableRow1.Append(tableCell2);
            tableRow1.Append(tableCell3);

            TableRow tableRow2 = new TableRow() { RsidTableRowAddition = "00A52F13", RsidTableRowProperties = "003C72CC", ParagraphId = "5FC7DA98", TextId = "77777777" };

            TableRowProperties tableRowProperties1 = new TableRowProperties();
            TableRowHeight tableRowHeight1 = new TableRowHeight() { Val = (UInt32Value)803U };

            tableRowProperties1.Append(tableRowHeight1);

            TableCell tableCell4 = new TableCell();

            TableCellProperties tableCellProperties4 = new TableCellProperties();
            TableCellWidth tableCellWidth4 = new TableCellWidth() { Width = "3539", Type = TableWidthUnitValues.Dxa };

            tableCellProperties4.Append(tableCellWidth4);

            Paragraph paragraph4 = new Paragraph() { RsidParagraphAddition = "00A52F13", RsidParagraphProperties = "003C72CC", RsidRunAdditionDefault = "00A52F13", ParagraphId = "0ED3CA93", TextId = "23715FBC" };

            ParagraphProperties paragraphProperties4 = new ParagraphProperties();
            Justification justification1 = new Justification() { Val = JustificationValues.Left };

            ParagraphMarkRunProperties paragraphMarkRunProperties4 = new ParagraphMarkRunProperties();
            RunFonts runFonts15 = new RunFonts() { Hint = FontTypeHintValues.EastAsia };

            paragraphMarkRunProperties4.Append(runFonts15);

            paragraphProperties4.Append(justification1);
            paragraphProperties4.Append(paragraphMarkRunProperties4);

            Run run12 = new Run();

            RunProperties runProperties12 = new RunProperties();
            RunFonts runFonts16 = new RunFonts() { Hint = FontTypeHintValues.EastAsia };

            runProperties12.Append(runFonts16);
            Text text12 = new Text();
            text12.Text = "列1文本";

            run12.Append(runProperties12);
            run12.Append(text12);

            paragraph4.Append(paragraphProperties4);
            paragraph4.Append(run12);

            tableCell4.Append(tableCellProperties4);
            tableCell4.Append(paragraph4);

            TableCell tableCell5 = new TableCell();

            TableCellProperties tableCellProperties5 = new TableCellProperties();
            TableCellWidth tableCellWidth5 = new TableCellWidth() { Width = "2835", Type = TableWidthUnitValues.Dxa };
            TableCellVerticalAlignment tableCellVerticalAlignment1 = new TableCellVerticalAlignment() { Val = TableVerticalAlignmentValues.Center };

            tableCellProperties5.Append(tableCellWidth5);
            tableCellProperties5.Append(tableCellVerticalAlignment1);

            Paragraph paragraph5 = new Paragraph() { RsidParagraphMarkRevision = "00A52F13", RsidParagraphAddition = "00A52F13", RsidParagraphProperties = "003C72CC", RsidRunAdditionDefault = "00A52F13", ParagraphId = "6E0F27AB", TextId = "3A14DE04" };

            ParagraphProperties paragraphProperties5 = new ParagraphProperties();
            Justification justification2 = new Justification() { Val = JustificationValues.Center };

            ParagraphMarkRunProperties paragraphMarkRunProperties5 = new ParagraphMarkRunProperties();
            RunFonts runFonts17 = new RunFonts() { Hint = FontTypeHintValues.EastAsia, Ascii = "微软雅黑", HighAnsi = "微软雅黑", EastAsia = "微软雅黑" };

            paragraphMarkRunProperties5.Append(runFonts17);

            paragraphProperties5.Append(justification2);
            paragraphProperties5.Append(paragraphMarkRunProperties5);

            Run run13 = new Run() { RsidRunProperties = "00A52F13" };

            RunProperties runProperties13 = new RunProperties();
            RunFonts runFonts18 = new RunFonts() { Hint = FontTypeHintValues.EastAsia, Ascii = "微软雅黑", HighAnsi = "微软雅黑", EastAsia = "微软雅黑" };

            runProperties13.Append(runFonts18);
            Text text13 = new Text();
            text13.Text = "列2文字";

            run13.Append(runProperties13);
            run13.Append(text13);

            paragraph5.Append(paragraphProperties5);
            paragraph5.Append(run13);

            tableCell5.Append(tableCellProperties5);
            tableCell5.Append(paragraph5);

            TableCell tableCell6 = new TableCell();

            TableCellProperties tableCellProperties6 = new TableCellProperties();
            TableCellWidth tableCellWidth6 = new TableCellWidth() { Width = "1701", Type = TableWidthUnitValues.Dxa };

            tableCellProperties6.Append(tableCellWidth6);

            Paragraph paragraph6 = new Paragraph() { RsidParagraphMarkRevision = "00A52F13", RsidParagraphAddition = "00A52F13", RsidParagraphProperties = "003C72CC", RsidRunAdditionDefault = "00A52F13", ParagraphId = "5671AE4A", TextId = "163710C9" };

            ParagraphProperties paragraphProperties6 = new ParagraphProperties();
            Justification justification3 = new Justification() { Val = JustificationValues.Right };

            ParagraphMarkRunProperties paragraphMarkRunProperties6 = new ParagraphMarkRunProperties();
            RunFonts runFonts19 = new RunFonts() { Hint = FontTypeHintValues.EastAsia, Ascii = "Microsoft JhengHei UI Light", HighAnsi = "Microsoft JhengHei UI Light", EastAsia = "Microsoft JhengHei UI Light" };

            paragraphMarkRunProperties6.Append(runFonts19);

            paragraphProperties6.Append(justification3);
            paragraphProperties6.Append(paragraphMarkRunProperties6);

            Run run14 = new Run() { RsidRunProperties = "00A52F13" };

            RunProperties runProperties14 = new RunProperties();
            RunFonts runFonts20 = new RunFonts() { Hint = FontTypeHintValues.EastAsia, Ascii = "Microsoft JhengHei UI Light", HighAnsi = "Microsoft JhengHei UI Light", EastAsia = "Microsoft JhengHei UI Light" };

            runProperties14.Append(runFonts20);
            Text text14 = new Text();
            text14.Text = "列三";

            run14.Append(runProperties14);
            run14.Append(text14);

            paragraph6.Append(paragraphProperties6);
            paragraph6.Append(run14);

            tableCell6.Append(tableCellProperties6);
            tableCell6.Append(paragraph6);

            tableRow2.Append(tableRowProperties1);
            tableRow2.Append(tableCell4);
            tableRow2.Append(tableCell5);
            tableRow2.Append(tableCell6);

            table1.Append(tableProperties1);
            table1.Append(tableGrid1);
            table1.Append(tableRow1);
            table1.Append(tableRow2);

            return table1;
        }

        public static TableRow TableTemplateRow { get; set; }

        public static void AddTableRow(this Table table, string[] values)
        {
            var colNum = table.GetTableColumnCount();
            List<TableCell> TableCellLsit = new List<TableCell>();

            if (TableTemplateRow == null)
            {
                return;
            }
            TableRow tr = TableTemplateRow.Clone() as TableRow; //复制 一个行作为 模板

            for (int i = 0; i <= colNum -1; i++)
            {
                TableCell tc = tr.GetTableCell(i)?.Clone() as TableCell;
                Paragraph par = tc.GetFirstParagraph()?.Clone() as Paragraph;
                Run run = par.GetFirstRun()?.Clone() as Run;
                Text text = run.GetFirstText()?.Clone() as Text;

                string val = "";
                if(i <= values.Length - 1)
                {
                    val = values[i];
                }
                text.SetCellText(val);

                run.ClearText();
                par.ClearRun();
                tc.ClearParagraph();

                run.Append(text);
                par.Append(run);
                tc.Append(par);

                TableCellLsit.Add(tc);
            }

            tr.ClearTableCell();
            foreach (var tc in TableCellLsit)
            {
                tr.Append(tc);
            }
            table.Append(tr);
        }

        static int GetTableColumnCount(this Table table)
        {
            IEnumerable<TableGrid> TableGrids = table.Descendants<TableGrid>();
            if (TableGrids != null && TableGrids.Count() > 0)
            {
                IEnumerable<GridColumn> GridColumns = TableGrids.ToList()[0].Descendants<GridColumn>();
                if (GridColumns != null)
                {
                    return GridColumns.Count();
                }
                else
                {
                    return 0;
                }
            }
            else
            {
                return 0;
            }
        }

        public static TableRow GetTableTemplateRow(this Table table)
        {
            IEnumerable<TableRow> TableRows = table.Descendants<TableRow>();
            if (TableRows != null && TableRows.Count() >= 1)
            {
                TableTemplateRow = TableRows.ToList().Last();
                return TableTemplateRow;
            }
            else 
            {
                return null;
            } 
        }

        public static void ClearRowofTemplateExceptHeader(this Table table)
        {
            List<TableRow> TableRows = table.Descendants<TableRow>().ToList();
            if (TableRows != null)
            {
                for (int i = 0; i < TableRows.Count; i++)
                {
                    if( i != 0)
                    {
                        TableRows[0].Remove();
                    }
                }
            }
        }

        static TableCell GetTableCell(this TableRow tablerow,int columnIndex)
        {
            IEnumerable<TableCell> TableCells = tablerow.Descendants<TableCell>();
            if(columnIndex <= TableCells.Count() -1)
            {
                return TableCells.ToList()[columnIndex];
            }
            else
            {
                return null;
            }
        }


        static Paragraph GetFirstParagraph(this TableCell cell)
        {
            IEnumerable<Paragraph> Paragraphs = cell.Descendants<Paragraph>();
            return Paragraphs?.FirstOrDefault();
        }

        static Run GetFirstRun(this Paragraph paragraph)
        {
            IEnumerable<Run> runs = paragraph.Descendants<Run>();
            return runs?.FirstOrDefault();
        }

        static Text GetFirstText(this Run run)
        {
            IEnumerable<Text> texts = run.Descendants<Text>();
            return texts?.FirstOrDefault();
        }

        static void ClearTableCell(this TableRow row)
        {
            List<TableCell> TableCells = row.Descendants<TableCell>().ToList();
            try
            {
                for (int i = 0; i < TableCells.Count; i++)
                {
                    TableCells[i].Remove();
                }
            }
            catch(System.Exception e)
            {

            }
            
        }

        static void ClearParagraph(this TableCell cell)
        {
            IEnumerable<Paragraph> Paragraphs = cell.Descendants<Paragraph>();
            foreach (var p in Paragraphs)
            {
                p.Remove();
            }
        }

        static void ClearRun(this Paragraph paragraph)
        {
            IEnumerable<Run> Runs = paragraph.Descendants<Run>();
            foreach (var r in Runs)
            {
                r.Remove();
            }
        }

        static void ClearText(this Run run)
        {
            IEnumerable<Text> Texts = run.Descendants<Text>();
            foreach (var t in Texts)
            {
                t.Remove();
            }
        }

        static void SetCellText(this Text text, string val)
        {
            text.Text = val;
        }
    }
}
