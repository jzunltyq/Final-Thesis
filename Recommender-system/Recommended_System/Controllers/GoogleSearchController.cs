using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Google.Apis.Customsearch.v1;
using Google.Apis.Customsearch.v1.Data;
using Google.Apis.Services;

namespace Recommended_System.Controllers
{
    public class GoogleSearchController : Controller, IEnumerable
    {
        //
        // GET: /GoogleSearch/

        public ActionResult SearchPost()
        {
            var apiKey = "AIzaSyAeyUzivp1tuMpDp_6cnVXb8DWVb1i9rJQ";
            var searchEngineId = "000444561497760690246:j6o8r-ulmmm";
            var query = "testing";

            var customSearchService = new CustomsearchService(new BaseClientService.Initializer { ApiKey = apiKey });
            var listRequest = customSearchService.Cse.List(query);
            listRequest.Cx = searchEngineId;
            IList<Result> paging = new List<Result>();
            var count = 0;
            while (paging != null)
            {
                listRequest.Start = count * 10 + 1;
                paging = listRequest.Execute().Items;
                if (paging != null)
                    foreach (var item in paging)
                        Console.WriteLine("Title : " + item.Title + Environment.NewLine + "Link : " + item.Link +
                                          Environment.NewLine + Environment.NewLine);
                count++;
            }
            return View(paging);
        }

        public IEnumerator GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
}
