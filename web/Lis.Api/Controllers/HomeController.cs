using Lis.Api.App_Start;
using Microsoft.AspNet.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace Lis.Api.Controllers
{
    public class HomeController : Controller
    {
       
        public async Task<ActionResult> Index()
        {
            ViewBag.Title = "DXI 800";
           

            return View();
        }

        public ActionResult Error()
        {
            return RedirectToAction("Index", "Home");
        }
    }
}
