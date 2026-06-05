using DocumentUpload_App.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace DocumentUpload_App.Controllers
{
    public class AccountController : Controller
    {
        private AppDbContext db = new AppDbContext();

        // GET: Account
        [AllowAnonymous]
        public ActionResult Login()
        {
            if (Session["UserName"] != null)
                return RedirectToAction("Index", "Home");

            return View();
        }
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Login(string User_Name, string Password)
        {
            if (string.IsNullOrEmpty(User_Name) || string.IsNullOrEmpty(Password))
            {
                ViewBag.Error = "Username and Password required.";
                return View();
            }

            var user = db.UserLogins.Where(u => u.User_Name == User_Name && u.Password == Password).FirstOrDefault();
            if (user != null)
            {
                Session["User_Name"] = user.User_Name;
                Session["Password"]  =user.Password;
                return RedirectToAction("Index", "Document");
            }
            else
            {
                ViewBag.Error = "Invalid Username or Password.";
                return View();
            }
        }

        // Logout
        public ActionResult Logout()
        {
            Session.Clear();
            Session.Abandon();
            return RedirectToAction("Login", "Account");
        }

        public ActionResult Index()
        {
            return View();
        }
    }
}