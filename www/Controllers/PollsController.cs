using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using SnitzConfig;
using SnitzCore.Extensions;
using SnitzCore.Filters;
using SnitzCore.Utility;
using SnitzDataModel.Controllers;
using SnitzDataModel.Database;
using SnitzDataModel.Extensions;
using SnitzDataModel.Models;
using SnitzMembership;
using WWW.ViewModels;

namespace WWW.Controllers
{
    public class PollsController : BaseController
    {
        //
        // GET: /Polls/
        public ActionResult Index(bool admin=false)
        {
            List<Poll> polls;
            using (var con = new PollsRepository())
            {
                polls = con.GetAllEntries();
                if (!admin)
                {
                    polls = polls.Where(p => p.Active == 1).ToList();
                }
                foreach (Poll poll in polls)
                {
                    poll.Votes = con.GetPollVotes(poll.Id);
                }
            }

            return View(polls);
        }

        //
        // GET: /Polls/Add
        /// <summary>
        /// add a Poll
        /// </summary>
        /// <param name="id">Id of Topic</param>
        /// <param name="returnUrl">Url of calling page</param>
        /// <returns></returns>
        [Authorize]
        public ActionResult Add(int id, string returnUrl)
        {
            if (!Url.IsLocalUrl(returnUrl))
            {
                ViewBag.ErrTitle = "Error";
                ViewBag.Error = "Invalid return Url";
                return View("Error");
            }
            using (PollsRepository b = new PollsRepository())
            {
                b.AddPoll(id);
            }
            
            return Redirect(returnUrl);

        }

        //
        // GET: /Polls/Delete/5
        /// <summary>
        /// Remove a Poll
        /// </summary>
        /// <param name="id">Id of Poll</param>
        /// <param name="returnUrl">Url of calling page</param>
        /// <returns></returns>        
        [Authorize(Roles = "Administrator,Moderator")]
        public ActionResult Delete(int id, string returnUrl)
        {

            using (PollsRepository b = new PollsRepository())
            {
                b.DeletePoll(id);
            }

            return RedirectToAction("Index", new { admin = true });

        }

        [Authorize(Roles = "Administrator,Moderator")]
        public ActionResult Lock(int id, string returnUrl)
        {

            var topic = Topic.FetchTopic(id);
            topic.UpdateStatus(Snitz.Base.Enumerators.PostStatus.Closed);

            if (ClassicConfig.GetIntValue("INTFEATUREDPOLLID") == id)
            {
                ClassicConfig.ConfigDictionary.CreateNewOrUpdateExisting("INTFEATUREDPOLLID", "0");
            }
            ClassicConfig.Update(new[]
                                 {
                                         "INTFEATUREDPOLLID"
                                     });
            return RedirectToAction("Index", new { admin = true });

        }

        [HttpPost]
        public ActionResult Vote(FormCollection form)
        {

            PollViewModel pvm = new PollViewModel();
            using (var con = new PollsRepository())
            {
                var poll = con.GetPoll(Convert.ToInt32(form["PollId"]));
                poll.LastVoteDate = DateTime.UtcNow;
                if (poll.AllowedRoles == "everyone")
                {
                    if (WebSecurity.IsAuthenticated)
                    {
                        pvm.Voted = con.Vote(WebSecurity.CurrentUserId, poll, Convert.ToInt32(form["voteid"]));
                        SnitzCookie.PollVote(poll.Id);
                    }
                    else
                    {
                        pvm.Voted = con.Vote(-1, poll, Convert.ToInt32(form["voteid"]));
                        SnitzCookie.PollVote(poll.Id);
                    }
                    
                }
                else
                {
                    pvm.Voted = con.Vote(WebSecurity.CurrentUserId, poll, Convert.ToInt32(form["voteid"]));
                    SnitzCookie.PollVote(poll.Id);
                }
                
                pvm.Poll = con.GetPoll(poll.Id); 
                pvm.Votes = con.GetPollVotes(pvm.Poll.Id);
                
                pvm.TotalVotes = pvm.Poll.Answers.Sum(a=>a.Votes);
            }

            return View("Results", pvm);
        }

        [DoNotLogActionFilter]
        public PartialViewResult FeaturedPoll()
        {
            PollViewModel vm = new PollViewModel
                               {
                                   Id = Convert.ToInt32(ClassicConfig.GetValue("INTFEATUREDPOLLID"))
                               };
            using (var db = new PollsRepository())
            {
                vm.Poll = db.GetPoll(vm.Id);
                List<PollVotes> votes = db.GetPollVotes(vm.Id);
                vm.Votes = votes;
                bool voted = votes.Any(v => v.MemberId == WebSecurity.CurrentUserId);
                vm.Voted = voted || SnitzCookie.HasVoted(vm.Id);
            }

            return PartialView("_FeaturedPoll", vm);
        }

        /// <summary>
        /// Remove selected Polls
        /// </summary>
        /// <param name="form"></param>
        /// <param name="returnUrl"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize(Roles = "Administrator,Moderator")]
        public ActionResult Delete(FormCollection form, string returnUrl)
        {
            if (!Url.IsLocalUrl(returnUrl))
            {
                ViewBag.ErrTitle = "Error";
                ViewBag.Error = "Invalid return Url";
                return View("Error");
            }
            IEnumerable<int> polls = form["delete-me"].StringToIntList();

            using (PollsRepository b = new PollsRepository())
            {
                //TODO: Not yet implemented delete polls?
            }
            //return View(form);
            return RedirectToAction("Index",new { admin = true });

        }

        [Authorize(Roles = "Administrator")]
        public ActionResult MakeFeaturedPoll(int id)
        {
            var routinfo = Common.GetReferrRouteData(Request.UrlReferrer.ToString());
            ClassicConfig.ConfigDictionary["INTFEATUREDPOLLID"] = id.ToString();
            ClassicConfig.Update(new[]
                                         {
                                         "INTFEATUREDPOLLID"
                                     });

            //db.Update(forum);
            return RedirectToRoute(routinfo.Values);            
        }

    }
}
