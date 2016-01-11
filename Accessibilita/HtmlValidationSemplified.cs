using DoctypeEncodingValidation;
using HtmlValidTest;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using HtmlAgilityPack;
using System.Text.RegularExpressions;

namespace DoctypeEncodingValidation
{
    public class HtmlValidationSemplified
    {

        public class Error
        {
            public int line { get; set; }
            public int col { get; set; }
            public string message { get; set; }
            public string explanation { get; set; }
        }

        public class validationResults
        {
            public List<Error> Errors { get; set; }
            public List<Warning> Warnings { get; set; }
            public List<WarningPotentialIssue> Potentials { get; set; }

            public void writeResultsOnFile(string fileName, string url)
            {
                using (var wrt = new StreamWriter(fileName, true))
                {
                    wrt.WriteLine(string.Format("{0} - {1}", url, DateTime.Now));
                    wrt.WriteLine("{0} Errors", this.Errors.Count);
                    foreach (var e in Errors)
                    {
                        wrt.WriteLine("Line :{0} Col :{1} Error : {2}", e.line, e.col, e.message);
                        wrt.WriteLine("Explanation : {0}", e.explanation);

                    }
                    wrt.WriteLine("{0} Warnings", this.Warnings.Count);
                    foreach (var w in Warnings)
                    {
                        wrt.WriteLine("Line :{0} Col :{1} Warning : {2}", w.line, w.col, w.message);
                        wrt.WriteLine("Explanation : {0}", w.explanation);

                    }
                    wrt.WriteLine("{0} Potentials", this.Potentials.Count);
                    foreach (var p in Potentials)
                    {
                        wrt.WriteLine("Warning : {0}", p.message);
                        wrt.WriteLine("Explanation : {0}", p.explanation);

                    }
                    wrt.WriteLine("-----------------------------------------------------------------------------");
                }
            }

            public void writeResultsOnHtmlFile(string filename, string Url)
            {
                var htmlFileName = Path.GetDirectoryName(filename) + "/" + Path.GetFileNameWithoutExtension(filename) + ".html";
                using (var rdr = new StreamReader("./htmlTemplate/htmlTemplate.template"))
                {
                    var templateString = rdr.ReadToEnd();

                    templateString = templateString.Replace("@URL@", Url);

                    templateString = templateString.Replace("@DATE@", DateTime.Now.ToString());

                    templateString = templateString.Replace("@ERRORCOUNT@", this.Errors.Count.ToString());

                    var sb = new StringBuilder();
                    foreach (var e in Errors)
                    {
                        sb.AppendFormat("<tr><td>{0}</td><td>{1}</td><td>{2}</td><td>{3}</td></tr>", e.line, e.col, e.message, e.explanation);
                        sb.AppendLine();
                    }

                    templateString = templateString.Replace("@FORERROR@", sb.ToString());
                    sb.Clear();

                    templateString = templateString.Replace("@WARNINGCOUNT@", this.Warnings.Count.ToString());

                    foreach (var w in Warnings)
                    {
                        sb.AppendFormat("<tr><td>{0}</td><td>{1}</td><td>{2}</td><td>{3}</td></tr>", w.line, w.col, w.message, w.explanation);
                        sb.AppendLine();
                    }

                    templateString = templateString.Replace("@FORWARNING@", sb.ToString());
                    sb.Clear();

                    var header = string.Empty;
                    if (!File.Exists(htmlFileName))
                    {
                        using (var headerReader = new StreamReader("./htmlTemplate/header.template"))
                        {
                            header = headerReader.ReadToEnd();
                        }
                    }

                    using (var wrt = new StreamWriter(htmlFileName, true))
                    {
                        if (!string.IsNullOrWhiteSpace(header))
                        {
                            wrt.WriteLine(header);
                        }
                        wrt.WriteLine(templateString);
                    }
                }
            }

        }



        /// <summary>
        /// Creating the XNamespace for the "env" namespace used in the xml document that we obtain.
        /// </summary>
        public static XNamespace envNamespace = "http://www.w3.org/2003/05/soap-envelope";
        /// <summary>
        /// Creating the XNamespace for the "m" namespace used in the xml document that we obtain.
        /// </summary>
        public static XNamespace mNamespace = "http://www.w3.org/2005/10/markup-validator";

        public static List<Error> HTMLFaults(XDocument urlDocument)
        {
            var faults = new List<Error>();

            var errorsXml = from e in urlDocument.Descendants(mNamespace + "error")
                            select e;

            foreach (var itm in errorsXml)
            {
                var err = new Error();

                var lineXml = from l in itm.Descendants(mNamespace + "line") select l;

                err.line = int.Parse(lineXml.Single().Value);

                var colXml = from c in itm.Descendants(mNamespace + "col") select c;

                err.col = int.Parse(colXml.Single().Value);

                var messageXml = from l in itm.Descendants(mNamespace + "message") select l;

                err.message = messageXml.Single().Value;

                var explanationXml = from l in itm.Descendants(mNamespace + "explanation") select l;

                err.explanation = explanationXml.Single().Value;

                faults.Add(err);

            }


            return faults;
        }

        public static validationResults HTMLWarnings(XDocument urlDocument)
        {
            var warnings = new List<Warning>();
            var warningPotentialIssues = new List<WarningPotentialIssue>();

            //Obtaining the descendants of the elements labeled "warnings". With this we obtain all the warnings
            var warningsElements = from e in urlDocument.Descendants(mNamespace + "warnings")
                                   select e;
            //Obtaining the descendants of the elements labeled "warningcount". With this we can obtain the number of warnings.
            var warningCountElement = from e in warningsElements.Descendants(mNamespace + "warningcount")
                                      select e;
            //Obtaining the descendants of the elements labeled "warning". With this we can obtain information from each of the warnings. 
            var warningListElements = from e in warningsElements.Descendants(mNamespace + "warning")
                                      select e;

            //Iterate over the 'warningaccount' variable to obtain the number of warnings
            foreach (var element in warningCountElement)
            {
                //Store the value of the count
                //warnings.warningCount = element.Value;

                //Iterate over the 'warningListElements' variable to obtain each error
                foreach (var warningElement in warningListElements)
                {
                    //Create an instance of a Warning
                    Warning warning = new Warning();

                    //If there is a number of line
                    if (warningElement.Descendants(mNamespace + "line").Count() > 0)
                        //Store all the información of the warning.
                        warning.line = warningElement.Descendants(mNamespace + "line").First().Value;
                    //If there is a number of column
                    if (warningElement.Descendants(mNamespace + "col").Count() > 0)
                        //Store all the información of the warning.
                        warning.col = warningElement.Descendants(mNamespace + "col").First().Value;
                    //If there is an explnation
                    if (warningElement.Descendants(mNamespace + "explanation").Count() > 0)
                        //Store all the información of the warning.
                        warning.explanation = warningElement.Descendants(mNamespace + "explanation").First().Value;
                    //If there is a source
                    if (warningElement.Descendants(mNamespace + "source").Count() > 0)
                        //Store all the información of the warning.
                        warning.source = warningElement.Descendants(mNamespace + "source").First().Value;
                    //If there is a messageid
                    if (warningElement.Descendants(mNamespace + "messageid").Count() > 0)
                    {
                        //If the messageid stars with a 'W' it means that the warning is a PotentialIssue
                        if (warningElement.Descendants(mNamespace + "messageid").First().Value.StartsWith("W"))
                        {
                            //Create an instance of a WarningPotentialIssue
                            WarningPotentialIssue warningPotentialIssue = new WarningPotentialIssue();

                            //Store the messageid in the warningPotentialIssue object
                            warningPotentialIssue.messageid = warningElement.Descendants(mNamespace + "messageid").First().Value;
                            //If there is a message
                            if (warningElement.Descendants(mNamespace + "message").Count() > 0)
                                //Store the message in the warningPotentialIssue object
                                warningPotentialIssue.message = warningElement.Descendants(mNamespace + "message").First().Value;
                            ////Add the warningPotentialIssue to the list of warningPotentialIssues.
                            warningPotentialIssues.Add(warningPotentialIssue);
                        }
                        //If the messageid not stars with a 'W'
                        else
                        {
                            //Store the messageid
                            warning.messageid = warningElement.Descendants(mNamespace + "messageid").First().Value;
                            //If there is a message
                            if (warningElement.Descendants(mNamespace + "message").Count() > 0)
                                //Store the message
                                warning.message = warningElement.Descendants(mNamespace + "message").First().Value;

                            //Add the warning to the list of warnings
                            warnings.Add(warning);
                        }
                    }
                }
            }

            return new validationResults() { Warnings = warnings, Potentials = warningPotentialIssues };
        }

        public static validationResults ValidateSource(string htmlSourceCode)
        {
            var diz = new Dictionary<string, object>();

            diz.Add("uploaded_file", htmlSourceCode);
            diz.Add("output", "soap12");

            using (var resp = FormUpload.MultipartFormDataPost(
                "http://validator.w3.org/check",
                "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)",
                diz
                ))
            {
                using (var str = resp.GetResponseStream())
                {

                    var xDoc = XDocument.Load(str);

                    var response = new validationResults();
                    response.Errors = HTMLFaults(xDoc);
                    var warns = HTMLWarnings(xDoc);
                    response.Warnings = warns.Warnings;
                    response.Potentials = warns.Potentials;

                    return response;
                }
            }
        }


        public static validationResults ValidateAccessibility(string htmlSourceCode)
        {
            var diz = new Dictionary<string, object>();


            diz.Add("MAX_FILE_SIZE", "52428800");
            diz.Add("pastehtml", htmlSourceCode);
            diz.Add("validate_paste", "Check it");
            diz.Add("enable_html_validation", "1");
            diz.Add("radio_gid[]", "7");
            diz.Add("checkbox_gid[]", "8");
            diz.Add("rpt_format", "1");

            //diz.Add("enable_html_validation", "1");

            using (var resp = FormUpload.MultipartFormDataPost(
                "http://achecker.ca/checker/index.php",
                "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)",
                diz
                ))
            {
                using (var str = resp.GetResponseStream())
                {

                    HtmlDocument doc = new HtmlDocument();
                    doc.Load(str);
                    var errorsHtml = doc.DocumentNode.SelectSingleNode(@"//*[@id='AC_errors']");
                    var errorNodes = errorsHtml.SelectNodes(@"child::*[@class='gd_one_check']");

                    var errorList = new List<Error>();
                    if (errorNodes != null)
                    {
                        foreach (var itm in errorNodes)
                        {
                            try
                            {
                                var err = new Error();
                                err.message = itm.SelectSingleNode(@"child::*[@class='gd_msg']/a").InnerText;
                                err.explanation = itm.SelectSingleNode(@"child::*[@class='gd_question_section']").InnerText;
                                err.explanation = Regex.Replace(err.explanation, @"[\s]+", " ");

                                var location = itm.SelectSingleNode(@"child::table/tr/td/em").InnerText;
                                var m = Regex.Match(location, @"Line\s(?:(?<line>[0-9]+)),\sColumn\s(?:(?<col>[0-9]+))");
                                err.col = int.Parse(m.Groups["col"].Value);
                                err.line = int.Parse(m.Groups["line"].Value);
                                errorList.Add(err);
                            }
                            catch (Exception ex)
                            {
                                throw ex;
                            }

                        }
                    }

                    var potentialHtml = doc.DocumentNode.SelectSingleNode(@"//*[@id='AC_potential_problems']");

                    var potenzialNodes = potentialHtml.SelectNodes(@"child::*[@class='gd_one_check']");

                    var potentialList = new List<Warning>();
                    if (potenzialNodes != null)
                    {
                        foreach (var itm in potenzialNodes)
                        {
                            try
                            {
                                var message = itm.SelectSingleNode(@"child::*[@class='gd_msg']/a").InnerText;
                                var explanation = itm.SelectSingleNode(@"child::*[@class='gd_question_section']").InnerText;
                                explanation = Regex.Replace(explanation, @"[\s]+", " ");
                                var locations = itm.SelectNodes(@"child::table/tr");
                                foreach (var l in locations)
                                {
                                    var warn = new Warning();
                                    warn.message = message;
                                    warn.explanation = explanation;
                                    var location = l.SelectSingleNode(@"child::td/em").InnerHtml;
                                    var m = Regex.Match(location, @"Line\s(?:(?<line>[0-9]+)),\sColumn\s(?:(?<col>[0-9]+))");
                                    warn.col = m.Groups["col"].Value;
                                    warn.line = m.Groups["line"].Value;
                                    potentialList.Add(warn);
                                }
                            }
                            catch (Exception ex)
                            {
                                throw ex;
                            }
                        }
                    }
                    var result = new validationResults();
                    result.Errors = errorList;
                    result.Potentials = new List<WarningPotentialIssue>();
                    result.Warnings = potentialList;
                    return result;

                }
            }
        }
    }
}

