using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using StarEvents.Models;
using StarEvents.ViewModels;

namespace StarEvents.Controllers
{
    [Authorize]
    public class CustomerLoyaltyController : Controller
    {
        private StarEventsDBEntities db = new StarEventsDBEntities();

        // GET: /CustomerLoyalty/Index
        public ActionResult Index()
        {
            var user = db.Users.FirstOrDefault(u => u.Email == User.Identity.Name);
            if (user == null)
                return RedirectToAction("Login", "Account", new { returnUrl = Request.RawUrl });

            DateTime expiryDate = DateTime.Now.AddMonths(-12);

            var expiredEarns = db.LoyaltyPoints
                .Where(t => t.UserId == user.UserId &&
                            t.TransactionType == "Earn" &&
                            t.CreatedDate < expiryDate &&
                            t.Status == "Active")
                .ToList();

            foreach (var earn in expiredEarns)
            {
                earn.Status = "Deactive";
            }
            if (expiredEarns.Count > 0)
                db.SaveChanges();

            var transactions = db.LoyaltyPoints
                .Where(t => t.UserId == user.UserId && t.Status == "Active")
                .OrderBy(t => t.CreatedDate)
                .ToList();

            int runningBalance = 0;
            var txnVMs = new List<LoyaltyTransactionViewModel>();
            foreach (var txn in transactions)
            {
                int points = txn.TransactionType == "Redeem" ? -txn.Points : txn.Points;
                runningBalance += points;
                txnVMs.Add(new LoyaltyTransactionViewModel
                {
                    Date = (DateTime)txn.CreatedDate,
                    Type = txn.TransactionType,
                    Description = txn.Description,
                    Amount = (decimal)txn.Amount,
                    Points = points,
                    Balance = runningBalance
                });
            }
            txnVMs.Reverse();

            var model = new LoyaltyViewModel
            {
                CurrentPoints = runningBalance,
                Transactions = txnVMs
            };

            return View(model);
        }
    }
}