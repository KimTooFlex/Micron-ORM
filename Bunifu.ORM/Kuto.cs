// ***********************************************************************
// Assembly         : Extractor
// Author           : KIM TOO FLEX
// Created          : 07-31-2017
//
// Last Modified By : KIM TOO FLEX
// Last Modified On : 02-16-2018
// ***********************************************************************
// <copyright file="Extractor.cs" company="">
//     Copyright ©  2017
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Micron
{
    /// <summary>
    /// Class ExtractDoc.
    /// </summary>
    public class Kuto
    {

        /// <summary>
        /// The throw excepions
        /// </summary>
        public bool throwExcepions = false;
        /// <summary>
        /// The document
        /// </summary>
        string doc = "";

        /// <summary>
        /// Initializes a new instance of the <see cref="Kuto" /> class.
        /// </summary>
        /// <param name="ExtractSring">The extract sring.</param>
        /// <exception cref="Exception">String empty</exception>
        public Kuto(string ExtractSring)
        {

            doc = ExtractSring.Trim();

        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        public string ToString()
        {
            return doc;
        }

        /// <summary>
        /// Extracts the specified start string.
        /// </summary>
        /// <param name="startString">The start string.</param>
        /// <param name="endString">The end string.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.Exception">
        /// Start string not found
        /// or
        /// End string not found
        /// or
        /// Start string poss >= end string poss
        /// </exception>
        /// <exception cref="Exception">Start string not found
        /// or
        /// End string not found
        /// or
        /// Start string poss &gt;= end string poss</exception>
        public Kuto Extract(string startString, string endString)
        {


            int startPoss = doc.IndexOf(startString);
            if (startPoss < 0 && startString != "")
            {
                if (throwExcepions)
                {
                    throw new Exception("Start string not found");

                }
                else
                {
                    return new Kuto("");
                }
            }

            int endPoss = doc.IndexOf(endString, startPoss + startString.Length);
            if (endPoss < 0 && endString != "")
            {
                if (throwExcepions)
                {
                    throw new Exception("End string not found");
                }
                else
                {
                    return new Kuto("");
                }
            }

            if (startString == "")
            {
                startPoss = 0;
            }

            if (endString == "")
            {
                endPoss = doc.Length;
            }


            if (startPoss >= endPoss)
            {

                if (throwExcepions)
                {
                    throw new Exception("Start string poss >= end string poss");
                }
                else
                {
                    return new Kuto("");
                }
            }



            return new Kuto(doc.Substring(startPoss + startString.Length, (endPoss - startPoss - startString.Length)));
        }

        public static string Reverse(string s)
        {
            char[] charArray = s.ToCharArray();
            Array.Reverse(charArray);
            return new string(charArray);
        }

        /// <summary>
        /// Extracts the specified rule.
        /// </summary>
        /// <param name="Rule">The rule.</param>
        /// <returns>ExtractDoc.</returns>
        public Kuto Extract(ScrapRule Rule)
        {
            return Extract(Rule.startString, Rule.endString);
        }
        /// <summary>
        /// Determines whether [contains] [the specified needle].
        /// </summary>
        /// <param name="needle">The needle.</param>
        /// <returns><c>true</c> if [contains] [the specified needle]; otherwise, <c>false</c>.</returns>
        public bool Contains(string needle)
        {
            return (doc.Contains(needle));
        }

        /// <summary>
        /// Scraps the specified column rules.
        /// </summary>
        /// <param name="ColumnRules">The column rules.</param>
        /// <param name="Reduce">if set to <c>true</c> [reduce].</param>
        /// <returns>List&lt;ExtractDoc&gt;.</returns>
        public List<Kuto> Scrap(List<ScrapRule> ColumnRules, bool Reduce = false)
        {
            List<Kuto> ret = new List<Kuto>();
            Kuto tmpDoc = new Kuto(this.ToString());
            foreach (ScrapRule Rule in ColumnRules)
            {
                ret.Add(tmpDoc.Extract(Rule.startString, Rule.endString));
                if (Reduce)
                {
                    tmpDoc = tmpDoc.Extract(Rule.endString, "");
                }
            }
            return ret;
        }

        public string StripTags()
        {
            return StripTags(this.ToString());
        }

            public static string StripTags(string sourceString)
        {
          string ret = sourceString;
            while (ret.Contains( "<") && ret.Contains( ">"))
            {
                int startIndex = ret.IndexOf("<");
                int  endIndex = ret.IndexOf(">",startIndex) ;
                ret = ret.Replace(ret.Substring(startIndex,(endIndex- startIndex)+1), "");
            }
            return ret;

        }


        /// <summary>
        /// Scraps the specified condition.
        /// </summary>
        /// <param name="Condition">The condition.</param>
        /// <param name="ColumnRules">The column rules.</param>
        /// <returns>List&lt;List&lt;ExtractDoc&gt;&gt;.</returns>
        public List<List<Kuto>> Scrap(string Condition, List<ScrapRule> ColumnRules)
        {

            List<List<Kuto>> rows = new List<List<Kuto>>();
            Kuto tmpDoc = new Kuto(this.ToString());
            while (tmpDoc.Contains(Condition))
            {
                rows.Add(tmpDoc.Scrap(ColumnRules));
                tmpDoc = tmpDoc.Extract(Condition, "");

            }


            return rows;
        }

        public Kuto Trim()
        {
            return new Kuto(this.ToString().Trim());
        }
    }


    /// <summary>
    /// Class ScrapRule.
    /// </summary>
    public class ScrapRule
    {
        /// <summary>
        /// The start string
        /// </summary>
        public string startString;
        /// <summary>
        /// The end string
        /// </summary>
        public string endString;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScrapRule"/> class.
        /// </summary>
        /// <param name="StartString">The start string.</param>
        /// <param name="EndString">The end string.</param>
        public ScrapRule(string StartString, string EndString)
        {
            startString = StartString;
            endString = EndString;
        }
    }

}