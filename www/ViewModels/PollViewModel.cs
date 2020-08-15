using System.Collections.Generic;
using SnitzDataModel.Models;

namespace WWW.ViewModels
{
    public class PollViewModel
    {
        public int Id { get; set; }
        public Poll Poll { get; set; }
        public bool Voted { get; set; }
        public List<PollVotes> Votes { get; set; }
        public int TotalVotes { get; set; }
    }
    public class SimplePollViewModel
    {
        public Poll Poll { get; set; }
        public int Id { get; set; }
    }
}