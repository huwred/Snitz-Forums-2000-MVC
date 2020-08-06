using System;
using System.Collections.Generic;

namespace WWW.Models
{
    public class CategoryFilter
    {
        public int GroupId { get; set; }
        public String Name { get; set; }
        public Dictionary<int,string> Categories { get; set; }
    }
}