using HtmlAgilityPack;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Greenwerx.Models.Membership;
using Greenwerx.Utilites.Extensions;

namespace Greenwerx.WebAPITests.Parser.Celebirty
{
    [TestClass()]
    public class ParseCelebritiesTestz
    {
        [TestMethod()]
        public void ParseCelebritiesFileTest()
        {
            List<Celebrity> celebs = new List<Celebrity>();
            string tmp = AppDomain.CurrentDomain.BaseDirectory.Replace("bin\\Debug", "").Replace("bin\\Release", "");
            string pathToFIle = Path.Combine(tmp, "Data\\Files\\celebrity.csv");
            var lines = File.ReadAllLines(pathToFIle);

            int index = 0;
            foreach (string line in lines)
            {
                if (index == 0) {
                    index++;
                    continue;//headers.
                }
                var celeb = new Celebrity();

                string html =  line.Substring("<div", "</div>", true);
                var doc = new HtmlDocument();
                doc.LoadHtml(html);
                var nodes = doc.DocumentNode.SelectNodes("//p");
                celeb.Name = GetNode(nodes, "dbname").InnerText.Replace("Name:", "").Trim();
                celeb.Aliases= GetNode(nodes, "dbaka").InnerText.Replace("AKA:", "").Trim();
                DateTime dob = new DateTime();
                string dt =GetNode(nodes, "dbborn").InnerText.Replace("dbborn:", "").Replace("Born:", "").Trim();
                if (DateTime.TryParse(dt, out dob))
                    celeb.DOB = dob;

                celeb.BirthPLaceUUID= GetNode(nodes, "dbbirthplace").InnerText.Replace("Birthplace:", "").Trim();
                celeb.Gender = GetNode(nodes, "dbGender").InnerText.Replace("Gender:", "").Trim();
                celeb.Race = GetNode(nodes, "dbrace").InnerText.Replace("Race:", "").Trim();
                celeb.Sex = GetNode(nodes, "dbsex").InnerText.Replace("Sexual Orientation:", "").Trim();
                celeb.Occupation = GetNode(nodes, "dboccupation").InnerText.Replace("Occupation:", "").Trim();
                celeb.PoliticalParty = GetNode(nodes, "dbparty").InnerText.Replace("Party Affiliation:", "").Trim();
                celeb.Nationality = GetNode(nodes, "dbnationality").InnerText.Replace("Nationality:", "").Trim();
                celeb.Description = GetNode(nodes, "dbdescription").InnerText.Replace("Description:", "").Trim();

                string[] elements = line.Split(',');

                Console.WriteLine(line);
                celebs.Add(celeb);
                index++;
            }

            //50 Cent,post,
            //"<div id=celebrities">
                //<p class="dbname">Name: 50 Cent</p>
                //<p class="dbaka">AKA: Curtis Jackson</p>
                //<p class="dbborn">Born: 6-Jul-1975</p>
                //<p class="dbbirthplace">Birthplace: Queens, NY</p>
                //<p class="dbGender">Gender: Male</p>
                //<p class="dbrace">Race: Black</p>
                //<p class="dbsex">Sexual Orientation: Straight</p>
                //<p class="dboccupation">Occupation: Rapper</p>
                //<p class="dbparty">Party Affiliation: Republican</p>
                //<p class="dbnationality">Nationality: United States</p>
                //<p class="dbdescription">Description: Wanksta, P.I.M.P.</p>
            //</div>"
            //,999996
            //,50 Cent
            //,"Queens, NY"
            //,Rapper
            //,United States
            //,"Wanksta, P.I.M.P."
        }

        public HtmlNode GetNode(HtmlNodeCollection nodes, string className)
        {
            foreach (var node in nodes)
            {
                if (node.HasClass(className))
                    return node;

            }
            return null;
        }

    }
}
