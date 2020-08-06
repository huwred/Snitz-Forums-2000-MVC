using System;
using System.Collections.Generic;
using System.Text;
using SnitzCore.Utility;
using SnitzDataModel.Models;
using SnitzMembership.Models;

namespace WWW.Views.Helpers
{
    public class RankInfoHelper
    {
        private Dictionary<int, Ranking> _ranking;

        private int _Level;
        private int _repeat;
        public string Title { get; set; }
        private readonly int? _Posts;
        private readonly string _User;
        private bool _IsAdmin;
        private bool _IsModerator;
        public string Stars
        {
            get { return GetStars(); }
        }

        public RankInfoHelper(string username, ref string title, int? posts, Dictionary<int, Ranking> rankings)
        {
            _ranking = rankings;
            _User = username;
            _Posts = posts;
            SetLevel();
            if (String.IsNullOrWhiteSpace(title))
            {
                title = Title;
            }
            else
            {
                Title = title.Trim();
            }

        }
        public RankInfoHelper(Member user, ref string title, int? posts, Dictionary<int, Ranking> rankings)
        {
            _ranking = rankings;
            _User = user.Username;
            _Posts = posts;
            SetLevel();
            if (String.IsNullOrWhiteSpace(title))
            {
                title = Title;
            }
            else
            {
                Title = title.Trim();
            }
            _IsAdmin = user.UserLevel == 3;
            _IsModerator = user.UserLevel == 2;
        }
        public RankInfoHelper(UserProfile user, ref string title, int? posts, Dictionary<int, Ranking> rankings)
        {
            _ranking = rankings;
            _User = user.UserName;
            _Posts = posts;
            SetLevel();
            if (String.IsNullOrWhiteSpace(title))
            {
                title = Title;
            }
            else
            {
                Title = title.Trim();
            }
            _IsAdmin = user.UserLevel == 3;
            _IsModerator = user.UserLevel == 2;
        }
        public string GetStars()
        {
            
            StringBuilder imageString = new StringBuilder("");

            int imageRepeat = _repeat;// _ranking[_Level + 1].Repeat;

            string rankImage = _ranking[_Level + 1].Image;
            if (_IsAdmin)
            {
                //imageRepeat = _ranking[0].Repeat;
                rankImage = _ranking[0].Image; //Admin;
            }
            else if (_IsModerator)
            {
                //imageRepeat = _ranking[1].Repeat;
                rankImage = _ranking[1].Image;
            }
            if (_Level == 0) return "";

            if (rankImage != "")
            {
                for (int ii = 1; ii <= imageRepeat; ii++)
                {
                    string clientpath = Common.RootFolder + "/Content/rankimages/";
                    imageString.AppendFormat("<img src='{0}{1}' alt='star'/>", clientpath, rankImage);
                }
            }

            return imageString.ToString();
        }

        private void SetLevel()
        {
            _repeat = -1;
            string rankTitle = "";
            _Level = 0;

            foreach (KeyValuePair<int, Ranking> ranking in _ranking)
            {
                if (ranking.Key < 2)
                    continue;
                if (_Posts >= ranking.Value.RankLevel)
                {
                    rankTitle = ranking.Value.Title;
                    _Level++;
                    _repeat++;
                }
                if (_Posts < ranking.Value.RankLevel)
                    break;
            }
            if (_IsAdmin)
            {
                rankTitle = "Forum Administrator";
                _Level = -1;
            }
            if (_IsModerator)
            {
                rankTitle = "Forum Moderator";
                _Level = -1;
            }
            Title = rankTitle;
        }

    }


}