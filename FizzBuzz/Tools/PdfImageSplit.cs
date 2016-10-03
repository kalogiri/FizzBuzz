using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FizzBuzz.Tools
{
    class PdfImageSplit
    {
        public PdfImageSplit()
        {
            WriteImageFile();
        }

        private static List<System.Drawing.Image> ExtractImages(string pdfSourcePath)
        {
            List<System.Drawing.Image> imgList = new List<System.Drawing.Image>();

            RandomAccessFileOrArray rafObj = null;
            PdfReader pdfReaderObj = null;
            PdfObject pdfObj = null;
            PdfStream pdfStreamObj = null;

            try
            {
                rafObj = new RandomAccessFileOrArray(pdfSourcePath);
                pdfReaderObj = new PdfReader(rafObj, null);

                for (int i = 0; i < pdfReaderObj.XrefSize - 1; i++)
                {
                    pdfObj = pdfReaderObj.GetPdfObject(i);

                    if((pdfObj != null) && pdfObj.IsStream())
                    {
                        pdfStreamObj = (PdfStream)pdfObj;
                        PdfObject subType = pdfStreamObj.Get(PdfName.SUBTYPE);
                        if(subType != null && subType.ToString() == PdfName.IMAGE.ToString())
                        {
                            try
                            {
                                PdfImageObject pdfImageObj = new PdfImageObject((PRStream)pdfStreamObj);
                                System.Drawing.Image imgPdf = pdfImageObj.GetDrawingImage();

                                imgList.Add(imgPdf);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex);
                            }
                        }
                    }
                }
                pdfReaderObj.Close();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            return imgList;
        }

        private static List<string> ExtractText(string pdfPath)
        {
            List<string> _text = new List<string>();

            StringBuilder text = new StringBuilder();

            using (PdfReader pdfReader = new PdfReader(pdfPath))
            {
                // Loop through each page of the pdf
                for (int page = 1; page < pdfReader.NumberOfPages; page++)
                {
                    ITextExtractionStrategy strat = new SimpleTextExtractionStrategy();

                    string currentText = PdfTextExtractor.GetTextFromPage(pdfReader, page, strat);

                    currentText = Encoding.UTF8.GetString(Encoding.Convert(Encoding.Default, Encoding.UTF8, Encoding.Default.GetBytes(currentText)));

                    text.Append(currentText);

                }
            }

            List<string> splitText = text.ToString().Split('\n').ToList();
            foreach (string item in splitText)
            {
                if (!string.IsNullOrWhiteSpace(item))
                {
                    _text.Add(item);
                }
            }

            return _text;
        }

        private static void WriteImageFile()
        {
            try
            {
                Console.WriteLine("Wait for extracting image from PDF file...");

                List<string> pdfFiles = Directory.EnumerateFiles(@"C:\PPProjects\c# Projects\Test\PDF Test\", "*.pdf").ToList();

                foreach (string pdf in pdfFiles)
                {
                    // Get a list of images
                    List<System.Drawing.Image> listImage = ExtractImages(pdf);
                    List<string> imageNames = ExtractText(pdf);
                    foreach (var pair in listImage.Zip(imageNames, (li, IN) => new { ListImage = li, ImageNames = IN }))
                    {
                        pair.ListImage.Save(@"C:\PPProjects\c# Projects\Test\PDF Test\Second Split\" + pair.ImageNames.TrimEnd(' ') + ".jpg");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
    }
}
