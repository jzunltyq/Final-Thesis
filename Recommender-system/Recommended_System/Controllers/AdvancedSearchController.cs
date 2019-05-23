using System;
using System.IO;
using System.Text;

using System.Data;
using System.Globalization;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;

//using System.Data.SqlServerCe;
using System.Data.SqlClient;

using Korzh.EasyQuery.Db;
using Korzh.EasyQuery.Services;
using Korzh.EasyQuery.Services.Db;
using Korzh.EasyQuery.Mvc;
using Korzh.Utils.Db;

namespace Recommended_System {
    public class AdvancedSearchController : Controller {

        static AdvancedSearchController() {
            //The following section adds support for different types of databases you may use in your app.
            //It's necessary for proper work of LoadFromConnection method of DbModel class
            //Please note: for each used DbGate class you will need to reference a corresponding DbGate assembly
            Korzh.EasyQuery.DataGates.OleDbGate.Register();
            Korzh.EasyQuery.DataGates.SqlClientGate.Register();
            //Korzh.EasyQuery.SqlCEDBGate.SqlCeGate.Register();
        }

        private EqServiceProviderDb eqService;

        public AdvancedSearchController() {
            eqService = new EqServiceProviderDb();
            eqService.DefaultModelName = "Recommended_SystemModel";
			eqService.StoreModelInSession = true;
			eqService.StoreQueryInSession = true;
			

            string connectionString = ConfigurationManager.ConnectionStrings["Recommended_System"].ConnectionString;
            eqService.Connection = new SqlConnection(connectionString);

            //You can set DbConnection directly (without using ConfigurationManager)
            //eqService.Connection = new SqlCeConnection("Data Source=" + System.IO.Path.Combine(dataPath, "Northwind.sdf"));

            //to support saving/loading models and queries to/from Session 
            eqService.SessionGetter = key => Session[key];
            eqService.SessionSetter = (key, value) => Session[key] = value;

            //Uncomment in case you need to implement your own model loader or add some changes to existing one
            //eqService.ModelLoader = (model, modelName) => {
            //    model.LoadFromConnection(eqService.Connection, FillModelOptions.Default);
            //};


			//Setting format of result SQL		
            eqService.Formats.SetDefaultFormats(FormatType.MsSqlServer);
            eqService.Formats.UseSchema = false;

			
            //Custom lists resolver
            eqService.CustomListResolver = (listname) => {
                if (listname == "ListName") {
                    return new List<ListItem> {
                        new ListItem("ID1","Item 1"),
                        new ListItem("ID2","Item 2")
                    };
                }   
                return Enumerable.Empty<ListItem>();
            };

            //EqServiceProvider needs to know where to save/load queries to/from
            eqService.DataPath = System.Web.HttpContext.Current.Server.MapPath("~/App_Data");   
        }



        public ActionResult Index() {
            return View("AdvancedSearch");
        }

        
        #region EasyQuery actions
		
        /// <summary>
        /// Gets the model by its name
        /// </summary>
        /// <param name="modelName">The name.</param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult GetModel(string modelName) {
            var model = eqService.GetModel(modelName);
            return Json(model.SaveToDictionary());
        }

        /// <summary>
        /// This action returns a custom list by different list request options (list name).
        /// </summary>
        /// <param name="options">List request options - an instance of <see cref="ListRequestOptions"/> type.</param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult GetList(ListRequestOptions options) {
            return Json(eqService.GetList(options));
        }

        /// <summary>
        /// Gets the query by its name
        /// </summary>
        /// <param name="queryName">The name.</param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult GetQuery(string queryName) {
            var query = eqService.GetQuery(queryName);
            return Json(query.SaveToDictionary());
        }

        /// <summary>
        /// Saves the query.
        /// </summary>
        /// <param name="queryJson">The JSON representation of the query .</param>
        /// <param name="queryName">Query name.</param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult SaveQuery(string queryJson, string queryName) {
            eqService.SaveQueryDict(queryJson.ToDictionary(), queryName);
            Dictionary<string, object> dict = new Dictionary<string, object>();
            dict.Add("result", "OK");
            return Json(dict);
        }

        /// <summary>
        /// It's called when it's necessary to synchronize query on client side with its server-side copy.
        /// Additionally this action can be used to return a generated SQL statement (or several statements) as JSON string
        /// </summary>
        /// <param name="queryJson">The JSON representation of the query .</param>
        /// <param name="optionsJson">The additional parameters which can be passed to this method to adjust query statement generation.</param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult SyncQuery(string queryJson, string optionsJson) {
            var query = eqService.SyncQueryDict(queryJson.ToDictionary());

            var statement = eqService.BuildQuery(query, optionsJson.ToDictionary());
            Dictionary<string, object> dict = new Dictionary<string, object>();
            dict.Add("statement", statement);
            return Json(dict);
        }


        /// <summary>
        /// Executes the query passed as JSON string and returns the result record set (again as JSON).
        /// </summary>
        /// <param name="queryJson">The JSON representation of the query.</param>
        /// <param name="optionsJson">Different options in JSON format</param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult ExecuteQuery(string queryJson, string optionsJson) {
            var query = eqService.LoadQueryDict(queryJson.ToDictionary());

            //query.Options.SelectTop = "1000";

            var statement = eqService.BuildQuery(query, optionsJson.ToDictionary());
            var resultSet = eqService.GetDataSetBySql(statement);


            var resultSetDict = eqService.DataSetToDictionary(resultSet, optionsJson.ToDictionary());

            Dictionary<string, object> dict = new Dictionary<string, object>();
            dict.Add("statement", statement);
            dict.Add("resultSet", resultSetDict);

            return Json(dict);
        }

        [HttpGet]
        public JsonResult GetQueryList(string modelName) {
            var queries = eqService.GetQueryList(modelName);

            return Json(queries, JsonRequestBehavior.AllowGet);
        }


        [HttpGet]
        public FileResult GetCurrentQuery() {
            var query = eqService.GetQuery();
            FileContentResult result = new FileContentResult(Encoding.UTF8.GetBytes(query.SaveToString()), "Content-disposition: attachment;");
            result.FileDownloadName = "CurrentQuery.xml";
            return result;
        }

        [HttpPost]
        public ActionResult LoadQueryFromFile(HttpPostedFileBase queryFile) {  
            if (queryFile != null && queryFile.ContentLength > 0)  
                try {
                    var query = eqService.GetQuery();
                    query.LoadFromStream(queryFile.InputStream);
                    eqService.SyncQuery(query);
                }  
                catch (Exception ex){  
                    TempData["Message"] = "ERROR:" + ex.Message.ToString();  
                }  
            else{  
                TempData["Message"] = "You have not specified a file.";  
            }

            return RedirectToAction("Index");
        }
		
        private void ErrorResponse(string msg) {
            Response.StatusCode = 400;
            Response.Write(msg);
            Response.Output.Flush();
        }
		
        [HttpGet]
        public void ExportToFileExcel() {
            Response.Clear();

            var query = eqService.GetQuery();

            if (!query.IsEmpty) {
                var sql = eqService.BuildQuery(query);
                eqService.Paging.Enabled = false;
                DataSet dataset = eqService.GetDataSetBySql(sql);
                if (dataset != null) {
                    Response.ContentType = "application/vnd.ms-excel";
                    Response.AddHeader("Content-Disposition",
                        string.Format("attachment; filename=\"{0}\"", HttpUtility.UrlEncode("report.xls")));
                    DbExport.ExportToExcelHtml(dataset, Response.Output, ExcelFormats.Default);
                }
                else
                    ErrorResponse("Empty dataset");
            }
            else
                ErrorResponse("Empty query");
            
        }

        [HttpGet]
        public void ExportToFileCsv() {
            Response.Clear();
            var query = eqService.GetQuery();

            if (!query.IsEmpty) {
                var sql = eqService.BuildQuery(query);
                eqService.Paging.Enabled = false;
                DataSet dataset = eqService.GetDataSetBySql(sql);
                if (dataset != null) {
                    Response.ContentType = "text/csv";
                    Response.AddHeader("Content-Disposition",
                        string.Format("attachment; filename=\"{0}\"", HttpUtility.UrlEncode("report.csv")));
                    DbExport.ExportToCsv(dataset, Response.Output, CsvFormats.Default);
                }
                else
                    ErrorResponse("Empty dataset");
            }
            else
                ErrorResponse("Empty query");

        }

        #endregion

    }

}
