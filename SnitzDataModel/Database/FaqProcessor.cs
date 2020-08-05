

// /*
// ####################################################################################################################
// ##
// ## FaqProcessor
// ##   
// ## Author:		Huw Reddick
// ## Copyright:	Huw Reddick, Snitz Forums
// ## based on code from Snitz Forums 2000 (c) Huw Reddick, Michael Anderson, Pierre Gorissen and Richard Kinser
// ## Created:		17/06/2020
// ## 
// ## The use and distribution terms for this software are covered by the 
// ## Eclipse License 1.0 (http://opensource.org/licenses/eclipse-1.0)
// ## which can be found in the file Eclipse.txt at the root of this distribution.
// ## By using this software in any fashion, you are agreeing to be bound by 
// ## the terms of this license.
// ##
// ## You must not remove this notice, or any other, from this software.  
// ##
// #################################################################################################################### 
// */

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using System.Xml;
using System.Xml.Linq;

namespace SnitzDataModel
{
    public class Question
    {
        public int Id { get; set; }
        [Required]
        public string Description { get; set; }
        [Required]
        public string Answer { get; set; }
        public int Order { get; set; }
        public int CategoryId { get; set; }
    }
    public class FAQCategory
    {
        public int Id { get; set; }
        [Required]
        public string Description { get; set; }
        public string[] Roles { get; set; }
    }
    public class FaqProcessor
    {
        public int NextId { get { return _maxid + 1; } }
        private XElement _categories;
        private XElement _questions;
        private int _maxid;
        private string _culture;
        private string _filename;
        private XDocument faqDocument;

        public FaqProcessor()
        {
            _culture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            if (_culture == "nn" || _culture == "nb")
            {
                _culture = "no";
            }
            string deffile = HttpContext.Current.Server.MapPath("~/App_Data/faq.category.en.xml");

            if (File.Exists(HttpContext.Current.Server.MapPath("~/App_Data/faq.category." + _culture + ".xml")))
            {
                deffile = HttpContext.Current.Server.MapPath("~/App_Data/faq.category." + _culture + ".xml");
            }

            _filename = deffile;
            faqDocument = XDocument.Load(deffile);

            XElement root = faqDocument.Element("FAQ");
            if (root != null)
            {
                _categories = root.Element("Categories");
                _questions = root.Element("Questions");
                if (_questions != null)
                    _maxid = Convert.ToInt32(_questions.Elements("Question").Max(q => Int32.Parse(q.Attribute("id").Value)));
            }

        }

        public IEnumerable<KeyValuePair<int, string>> Categories()
        {
            var res = new List<KeyValuePair<int, string>>();
            foreach (var category in _categories.Elements("Category").Where(c => c.Attribute("Roles").Value == ""))
            {
                res.Add(new KeyValuePair<int, string>(Convert.ToInt32(category.Attribute("id").Value), category.Attribute("Description").Value));
            }

            foreach (var category in _categories.Elements("Category").Where(c => c.Attribute("Roles").Value != ""))
            {
                var allowed = category.Attribute("Roles").Value.Split(',');
                var listCommon = allowed.Intersect(Roles.GetRolesForUser(HttpContext.Current.User.Identity.Name));

                if (listCommon.Any())
                    res.Add(new KeyValuePair<int, string>(Convert.ToInt32(category.Attribute("id").Value), category.Attribute("Description").Value));
            }
            return res;
        }

        public FAQCategory GetCategory(int id)
        {
            var category = _categories.Elements("Category")
                .SingleOrDefault(q => q.Attribute("id").Value == id.ToString());
            FAQCategory res = null;
            if (category != null)
            {
                res = new FAQCategory
                {
                    Id = Convert.ToInt32(category.Attribute("id").Value),
                    Description = category.Attribute("Description").Value,
                    Roles = category.Attribute("Roles").Value.Split(',')
                };

            }
            return res;
        }

        public List<Question> GetCategoryQuestions(int catid)
        {
            var res = new List<Question>();
            foreach (var question in _questions.Elements("Question").Where(q => q.Attribute("category").Value == catid.ToString()))
            {
                var q = new Question
                {
                    Id = Convert.ToInt32(question.Attribute("id").Value),
                    Description = question.Attribute("title").Value,
                    Answer = question.Element("Answer").Value
                };

                res.Add(q);
            }

            return res;
        }

        public Question GetQuestion(int id)
        {
            var question = _questions.Elements("Question").SingleOrDefault(q => q.Attribute("id").Value == id.ToString());
            Question res = null;
            if (question != null)
            {
                res = new Question
                {
                    Id = Convert.ToInt32(question.Attribute("id").Value),
                    Description = question.Attribute("title").Value,
                    Answer = question.Element("Answer").Value,
                    CategoryId = Convert.ToInt32(question.Attribute("category").Value),
                    Order = Convert.ToInt32(question.Attribute("order").Value)
                };
            }
            return res;
        }

        public void SaveQuestion(Question model)
        {

            var questions = faqDocument.Descendants("Questions").Descendants("Question");

            XElement emp = questions.FirstOrDefault(p => p.Attribute("id").Value == model.Id.ToString());
            if (emp != null)
            {
                emp.Attribute("title").Value = model.Description;
                emp.Attribute("order").Value = model.Order.ToString();
                emp.Attribute("category").Value = model.CategoryId.ToString();
                emp.Element("Answer").ReplaceNodes(new XCData(model.Answer));
                //xmldoc.Root.Element("Questions").Add(emp);
                //xmldoc.Root.Add(emp);
                faqDocument.Save(_filename);

            }
            else
            {
                emp = new XElement("Question",
                    new XAttribute("id", model.Id),
                    new XAttribute("title", model.Description),
                    new XAttribute("order", model.Order.ToString()),
                    new XAttribute("category", model.CategoryId.ToString()),
                    new XElement("Answer", new XCData(model.Answer)));

                faqDocument.Root.Element("Questions").Add(emp);
                faqDocument.Save(_filename);
            }

        }

        public void DeleteQuestion(int id)
        {

            var questions = faqDocument.Descendants("Questions").Descendants("Question");

            XElement emp = questions.FirstOrDefault(p => p.Attribute("id").Value == id.ToString());
            if (emp != null)
            {
                emp.Remove();
                faqDocument.Save(_filename);
            }

        }

        public void AddCategory(int id, string description, string[] roles = null)
        {
            var cat = new XElement("Category",
                new XAttribute("id", id),
                new XAttribute("Description", description),
                new XAttribute("Roles", ""));

            if (roles != null)
            {
                cat.Attribute("Roles").Value = String.Join(",", roles);
            }


            _categories.Add(cat);
            faqDocument.Save(_filename);
        }

        public void SaveCategory(FAQCategory cat)
        {
            var categories = faqDocument.Descendants("Categories").Descendants("Category");

            XElement ecat = categories.FirstOrDefault(p => p.Attribute("id").Value == cat.Id.ToString());
            if (ecat != null)
            {
                ecat.Attribute("Description").Value = cat.Description;
                if (cat.Roles != null)
                {
                    ecat.Attribute("Roles").Value = String.Join(",", cat.Roles);

                }
                else
                {
                    ecat.Attribute("Roles").Value = "";
                }

                faqDocument.Save(_filename);

            }
            else
            {
                ecat = new XElement("Category",
                    new XAttribute("id", cat.Id),
                    new XAttribute("Description", cat.Description),
                    new XAttribute("Roles", String.Join(",", cat.Roles))
                );

                faqDocument.Root.Element("Categories").Add(ecat);
                faqDocument.Save(_filename);
            }
        }

        public void DeleteCategory(int id)
        {
            var categories = faqDocument.Descendants("Categories").Descendants("Category");

            XElement cat = categories.FirstOrDefault(p => p.Attribute("id").Value == id.ToString());
            if (cat != null)
            {
                cat.Remove();
                faqDocument.Save(_filename);
            }
        }
    }
}