using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Text;
using Microsoft.VisualBasic;
using System.Net;
using Microsoft.Office.Interop.Excel;
using System.Reflection;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.Util;
using log4net;
using log4net.Config;
using System.Configuration;
using System.IO;
using System.Collections;


namespace HarborEast.BAL
{
    public class clsDataScrapperBAL
    {

        #region Variable Declarations

        private string _errorMsg;
        private Harbor_EastEntities objEDMModel = new Harbor_EastEntities();
        protected static readonly ILog log = LogManager.GetLogger("DSlogger");

        #endregion

        #region Properties

        public string ErrorMsg
        {
            get
            {
                return _errorMsg;
            }
            set
            {
                _errorMsg = value;
            }
        }

        #endregion

        public clsDataScrapperBAL(string parPath)
        {
            try
            {
                log4net.Config.XmlConfigurator.Configure(new System.IO.FileInfo("log4net.xml"));
                foreach (var appender in LogManager.GetRepository().GetAppenders())
                {
                    var fileAppender = appender as log4net.Appender.FileAppender;
                    if (fileAppender != null)
                    {
                        if (fileAppender.Name == "LogFileAppender")
                        {
                            fileAppender.File = parPath;// ( "${LOCALAPPDATA}", dataDirectory);
                            fileAppender.ActivateOptions();
                        }
                    }
                }
            }
            catch (Exception ex)
            { 
                throw;
            }
            
            //log4net.Appender.FileAppender FA = new log4net.Appender.FileAppender();
            //FA.File = HttpContext.Current.Server.MapPath("/Logs/log.txt");
           
        }

        #region Private Methods

        /// <summary>
        /// This method is used to generate the URL for fetching the data from the yahoo site.
        /// </summary>
        /// <param name="strSymbol">The symbol for which the data should be fetch.</param>
        /// <param name="dtStartDate">The date from which the data should be fetched.</param>
        /// <param name="dtEndDate">The date till which the data should be fetched.</param>
        /// <returns>Returns the URL in the string format.</returns>
        private string GenerateURL(string strSymbol, DateTime dtStartDate, DateTime dtEndDate)
        {
            StringBuilder strUrl = new StringBuilder();
            _errorMsg = string.Empty;
            try
            {
               // strUrl.Append("http://ichart.finance.yahoo.com/table.csv?s=");
                strUrl.Append("http://ichart.finance.yahoo.com/table.csv?s=");

                if (!string.IsNullOrEmpty(strSymbol.Trim()))
                {
                    strUrl.Append(strSymbol + "&a=");
                }
                if (dtStartDate != null)
                {
                    strUrl.Append(Convert.ToString(dtStartDate.Month - 1) + "&b=");
                    strUrl.Append(Convert.ToString(dtStartDate.Day) + "&c=");
                    strUrl.Append(Convert.ToString(dtStartDate.Year) + "&d=");
                }
                if (dtEndDate != null)
                {
                    strUrl.Append(Convert.ToString(dtEndDate.Month - 1) + "&e=");
                    strUrl.Append(Convert.ToString(dtEndDate.Day) + "&f=");
                    strUrl.Append(Convert.ToString(dtEndDate.Year) + "&g=d&ignore=.csv");
                }
            }
            catch (Exception ex)
            {
                _errorMsg = ex.Message;
               // log.Error("Error in Data Scrapper "+ "  Exception : " + ex.Message); 
                log.Error(strSymbol + "\t" + ex.Message);
            }
            return strUrl.ToString();
        }

        private void ExportAssetPriceSetToExcel()
        {
            try
            {
                GridView gvExport = new GridView();
                gvExport.DataSource = objEDMModel.AssetPriceSet.OrderBy(x => x.Date);
                gvExport.DataBind();
                HttpContext.Current.Response.Clear();

                HttpContext.Current.Response.AddHeader("content-disposition", "attachment;filename=AssetPriceSet.xls");

                HttpContext.Current.Response.Charset = "";

                // If you want the option to open the Excel file without saving than

                // comment out the line below

                // Response.Cache.SetCacheability(HttpCacheability.NoCache);

                HttpContext.Current.Response.ContentType = "application/vnd.xls";

                System.IO.StringWriter stringWrite = new System.IO.StringWriter();

                HtmlTextWriter htmlWrite =
                new HtmlTextWriter(stringWrite);
                gvExport.RenderControl(htmlWrite);

                HttpContext.Current.Response.Write(stringWrite.ToString());

                HttpContext.Current.Response.End();
            }
            catch (Exception ex)
            {
                _errorMsg = ex.Message;
               // log.Error("Error in Data Scrapper " + "  Exception : " + ex.Message);
            }
        }

        /// <summary>
        /// This method is used to return a recent trading date from the AssetPriceSet collection passed as a parameter.
        /// </summary>
        /// <param name="lstAssetSet">list collection of all the AssetPriceSet associated with the current AssetSymbolID.</param>
        /// <returns>returns the recently published trading date from the AssetSet collection.</returns>
        private DateTime GetAssetPriceSetLastTradingDate(List<AssetPriceSet> lstAssetPriceSet)
        {
            DateTime dtLastTradingDate = new DateTime();
            try
            {
                var existingAssetPriceCollection = lstAssetPriceSet.OrderBy(x => x.Date).Last();
                dtLastTradingDate = existingAssetPriceCollection.Date;
            }
            catch (Exception ex)
            {
                _errorMsg = ex.Message;
                //log.Error("Error in Data Scrapper " + "  Exception : " + ex.Message);
            }
            return dtLastTradingDate;
        }

        /// <summary>
        /// This method is used to return a recent trading date from the AssetSet collection passed as a parameter.
        /// </summary>
        /// <param name="lstAssetSet">list collection of all the AssetSet associated with the current AssetSymbolID.</param>
        /// <returns>returns the recently published trading date from the AssetSet collection.</returns>
        private DateTime GetLastTradingDate(List<AssetSet> lstAssetSet)
        {
            DateTime dtLastTrading = new DateTime();
            try
            {
                var existingAssetCollection = lstAssetSet.OrderBy(x => x.Ending_Date).Last();
                dtLastTrading = existingAssetCollection.Ending_Date;
            }
            catch (Exception ex)
            {
                _errorMsg = ex.Message;
                //log.Error("Error in Data Scrapper " + "  Exception : " + ex.Message);
            }
            return dtLastTrading;
        }

        /// <summary>
        /// This method is used to return the data in the string format for a particular date.
        /// If no data found then get data for the previous date of that date and so on.
        /// </summary>
        /// <param name="dtPreviousDate">holds the previous date.</param>
        /// <param name="parSymbol">AssetSet symbol.</param>
        /// <returns>returns the data for the pervious date.</returns>
        private string GetPreviousDateData(DateTime dtPreviousDate, string parSymbol)
        {
            try
            {
                string strPreviousDateData = string.Empty;
                bool isflag = true;
                while (isflag)
                {
                    WebClient objWebClient = new WebClient();
                    //Generate the URL and bind it to the property of WebClient instance.
                    objWebClient.BaseAddress = GenerateURL(parSymbol, dtPreviousDate, dtPreviousDate);

                    //Fetch the data from the site in the string format.
                    strPreviousDateData = objWebClient.DownloadString(objWebClient.BaseAddress);

                    if (!string.IsNullOrEmpty(strPreviousDateData))
                    {
                        //Split the string data for each new line character.
                        string[] strSplitNewLineCollection = strPreviousDateData.Split("\n".ToCharArray());
                        if (strSplitNewLineCollection.Length >= 3)
                        {
                            isflag = false;
                            break;
                        }
                        else
                        {
                            isflag = true;
                            dtPreviousDate = DateAndTime.DateAdd(DateInterval.Day, -1, dtPreviousDate);
                        }
                    }
                }
                return strPreviousDateData;
            }
            catch (Exception ex)
            {
                _errorMsg = ex.Message;
                //log.Error("Error in Data Scrapper " + "  Exception : " + ex.Message);
                log.Error(parSymbol + "\t" + ex.Message);
                return string.Empty;
            }
        }

        /// <summary>
        /// This method is used to update the records for the current Window.
        /// </summary>
        /// <param name="parSymbol">Asset symbol of the current asset in the string format.</param>
        /// <param name="dtStartDate">The date from which the data should be fetched.</param>
        /// <param name="dtEndDate">The date till which the data should be fetched.</param>
        /// <param name="intAssetSymbolId">Asset symbol id of the current asset.</param>
        private DateTime UpdateCurrentWindow(string parSymbol, DateTime dtStartDate, DateTime dtEndDate, int intAssetSymbolId)
        {
           
            _errorMsg = string.Empty;
            try
            {
                string strData = string.Empty;
                WebClient objWebClient = new WebClient();
                DateTime dtCurrentDate = new DateTime();
                decimal decNewPrice;
                var lstAssetPriceSet = objEDMModel.AssetPriceSet.Where(x => x.AssetSymbolSet.Id.Equals(intAssetSymbolId));

                //Generate the URL and bind it to the property of WebClient instance.
                objWebClient.BaseAddress = GenerateURL(parSymbol, dtStartDate, dtEndDate);

                //Fetch the data from the site in the string format.
                strData = objWebClient.DownloadString(objWebClient.BaseAddress);

                if (!string.IsNullOrEmpty(strData))
                {
                    //Split the string data for each new line character.
                    string[] strRowLevelSplitCollection = strData.Split("\n".ToCharArray());

                    //Check for Duplicate rows in the data and removes them                                         
                    strRowLevelSplitCollection = CommonUtility.ValidatestrRowLevelSplitCollection(strRowLevelSplitCollection);                        
                    
                    //strRowLevelSplitCollection.
                    for (int intI = 1; intI < strRowLevelSplitCollection.Length; intI++)
                    {
                        //Split the string data by comma.
                        string[] strColumnLevelSplitCollection = strRowLevelSplitCollection[intI].Split(",".ToCharArray());

                        //If the collection count is equal to 7 then go for further calculations.
                        if (strColumnLevelSplitCollection.Length == 7)
                        {
                            dtCurrentDate = Convert.ToDateTime(strColumnLevelSplitCollection[0]);
                            if (intI == 1)
                                dtEndDate = dtCurrentDate;

                            //Fetch the data from the local database for the particular date.
                            var objExistingAsset = lstAssetPriceSet.FirstOrDefault(x => x.Date.Equals(dtCurrentDate));

                            //Get the latest Price from the site.
                            decNewPrice = Convert.ToDecimal(strColumnLevelSplitCollection[6]);
                            if (objExistingAsset != null)
                            {
                                if (objExistingAsset.Price == 0)
                                    lstAssetPriceSet.FirstOrDefault(x => x.Date.Equals(dtCurrentDate)).Price = decNewPrice;

                                //If already divident or split has occured then directly update the price in the local database.
                                else if (objExistingAsset.Price != decNewPrice)
                                    lstAssetPriceSet.FirstOrDefault(x => x.Date.Equals(dtCurrentDate)).Price = decNewPrice;
                            }
                            else
                            {
                                //If the instance is null then that indicates that there is no succh entry in the database.
                                //So update this new entry in the local database.
                                if (objExistingAsset == null)
                                {
                                    AssetPriceSet objAssetPriceSet = new AssetPriceSet();

                                    objAssetPriceSet.Date = dtCurrentDate;

                                    objAssetPriceSet.Price = decNewPrice;

                                    objAssetPriceSet.AssetSymbolSet = objEDMModel.AssetSymbolSet.FirstOrDefault(x => x.Id.Equals(intAssetSymbolId));
                                    objEDMModel.AddToAssetPriceSet(objAssetPriceSet);
                                    
                                }
                            }
                        }
                    }
                }  
            }
            catch (Exception ex)
            {
                _errorMsg = ex.Message;
                //log.Error("Error in Data Scrapper " + "  Exception : " + ex.Message);
                log.Error(parSymbol + "\t" + ex.Message);
            }
            return dtEndDate;
        }

        /// <summary>
        /// This method is used to update the records from the earliest date to updated date from the database.
        /// </summary>
        /// <param name="parSymbol">Asset symbol of the current asset in the string format.</param>
        /// <param name="dtStartDate">The date from which the data should be fetched.</param>
        /// <param name="dtEndDate">The date till which the data should be fetched.</param>
        /// <param name="intAssetSymbolId">Asset symbol id of the current asset.</param>
        private void ReUpdate(string parSymbol, DateTime dtStartDate, DateTime dtEndDate, int intAssetSymbolId)
        {
            _errorMsg = string.Empty;
            try
            {
                string strData;
                WebClient objWebClient = new WebClient();
                DateTime dtCurrentDate = new DateTime();
                //**
                DateTime dtPreviousDate = new DateTime();
                //**
                decimal decNewPrice = 0;
                var lstAssetPriceSet = objEDMModel.AssetPriceSet.Where(x => x.AssetSymbolSet.Id.Equals(intAssetSymbolId));

                //Generate the URL and bind it to the property of WebClient instance.
                objWebClient.BaseAddress = GenerateURL(parSymbol, dtStartDate, dtEndDate);

                //Fetch the data from the site in the string format.
                strData = objWebClient.DownloadString(objWebClient.BaseAddress);

                if (!string.IsNullOrEmpty(strData))
                {
                    //Split the string data for each new line character.
                    string[] strRowLevelSplitCollection = strData.Split("\n".ToCharArray());
                    
                    for (int i = 1; i < strRowLevelSplitCollection.Length; i++)
                    {
                        //Split the string data by comma.
                        string[] strColumnLevelSplitCollection = strRowLevelSplitCollection[i].Split(",".ToCharArray());
                        string[] strPreviousColLevelSplitColl=null;
                        //**
                        if (i + 1 < strRowLevelSplitCollection.Length)
                        {
                            if (!string.IsNullOrEmpty(strRowLevelSplitCollection[i + 1]))
                            {
                                strPreviousColLevelSplitColl = strRowLevelSplitCollection[i + 1].Split(",".ToCharArray());
                            }
                        }
                        //**

                        //If the collection count is equal to 7 then go for further calculations.
                        if (strColumnLevelSplitCollection.Length == 7)
                        {
                            dtCurrentDate = Convert.ToDateTime(strColumnLevelSplitCollection[0]);

                            //**
                            if (strPreviousColLevelSplitColl != null)
                            {
                                if (!string.IsNullOrEmpty(strPreviousColLevelSplitColl[0]))
                                {
                                    dtPreviousDate = Convert.ToDateTime(strPreviousColLevelSplitColl[0]);
                                    if (Microsoft.VisualBasic.DateAndTime.DateDiff(DateInterval.Day, dtPreviousDate, dtCurrentDate, Microsoft.VisualBasic.FirstDayOfWeek.Sunday, FirstWeekOfYear.Jan1) >= 1)
                                    {
                                        DateTime dtFillStartDate = Microsoft.VisualBasic.DateAndTime.DateAdd(DateInterval.Day, 1, dtPreviousDate);
                                        while (dtFillStartDate < dtCurrentDate)
                                        {
                                            if (!(dtFillStartDate.DayOfWeek.Equals(DayOfWeek.Saturday) || dtFillStartDate.DayOfWeek.Equals(DayOfWeek.Sunday)))
                                            {
                                                //DateTime dtOldDate = CommonUtility.GetPreviousDate(DateAndTime.DateAdd(DateInterval.Day, -1, dtFillStartDate), intAssetSymbolId);
                                                AssetPriceSet extAssetPriceSet = objEDMModel.AssetPriceSet.FirstOrDefault(x => x.Date.Equals(dtFillStartDate) && x.AssetSymbolSet.Id.Equals(intAssetSymbolId));
                                                if (extAssetPriceSet != null)
                                                {
                                                    lstAssetPriceSet.FirstOrDefault(x => x.Date.Equals(dtFillStartDate)).Price = Convert.ToDecimal(strPreviousColLevelSplitColl[6]);
                                                }
                                            }
                                            dtFillStartDate = Microsoft.VisualBasic.DateAndTime.DateAdd(DateInterval.Day, 1, dtFillStartDate);
                                        }
                                    }
                                }
                            }
                            //**

                            //Fetch the data from the local database for the particular date.
                            var objExistingAsset = lstAssetPriceSet.FirstOrDefault(x => x.Date.Equals(dtCurrentDate));

                            //Get the latest Price from the site.
                            decNewPrice = Convert.ToDecimal(strColumnLevelSplitCollection[6]);
                            if (objExistingAsset != null)
                            {
                                //If already divident or split has occured then directly update the price in the local database.
                                lstAssetPriceSet.FirstOrDefault(x => x.Date.Equals(dtCurrentDate)).Price = decNewPrice;
                            }
                            else
                            {
                                //If the instance is null then that indicates that there is no succh entry in the database.
                                //So update this new entry in the local database.
                                if (objExistingAsset == null)
                                {
                                    AssetPriceSet objAssetPriceSet = new AssetPriceSet();

                                    objAssetPriceSet.Date = dtCurrentDate;

                                    objAssetPriceSet.Price = decNewPrice;

                                    objAssetPriceSet.AssetSymbolSet = objEDMModel.AssetSymbolSet.FirstOrDefault(x => x.Id.Equals(intAssetSymbolId));
                                    objEDMModel.AddToAssetPriceSet(objAssetPriceSet);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _errorMsg = ex.Message;
                //log.Error("Error in Data Scrapper " + "  Exception : " + ex.Message);
                log.Error(parSymbol + "\t" + ex.Message);
            }
        }

        private void CalculateHistoricalGrowth(AssetSymbolSet objAssetSymbolSet, DateTime dtEndDate)
        {
            try
            {
                int intAssetPriceCount = objEDMModel.AssetPriceSet.Where(x => x.AssetSymbolSet.Id.Equals(objAssetSymbolSet.Id)).Count();
                if (intAssetPriceCount > 0)
                {
                    //dtEndDate = new DateTime(dtEndDate.Year, dtEndDate.Month, dtEndDate.Day);
                    //int intAssetCount = objEDMModel.AssetPriceSet.Where(x => x.Date.Equals(dtEndDate) && x.AssetSymbolSet.Id.Equals(objAssetSymbolSet.Id) && x.Growth.Equals(null)).Count();
                    //if (intAssetCount > 0)
                    //{
                        var objAssetPriceSetColl = objEDMModel.AssetPriceSet.Where(x => x.AssetSymbolSet.Id.Equals(objAssetSymbolSet.Id)).OrderBy(x => x.Date);
                        dtEndDate = GetAssetPriceSetLastTradingDate(objAssetPriceSetColl.ToList());
                        double[] arrDates = objAssetPriceSetColl.Select(x => x.Date).ToArray().Select(x => x.ToOADate()).ToArray();
                        double[] arrPrices = objAssetPriceSetColl.Select(x => x.Price).ToArray().Select(x => (double)x).ToArray();
                        double[] arrGrowthVals = CommonUtility.Growth(arrDates, arrPrices, objAssetSymbolSet.Id);

                        //Update the existing records growth values.
                        for (int intIndex = 0; intIndex < arrDates.Count(); intIndex++)
                        {
                            DateTime dtUpdateDate = DateTime.FromOADate(arrDates[intIndex]);
                            objAssetPriceSetColl.First(x => x.Date.Equals(dtUpdateDate)).Growth = (decimal)arrGrowthVals[intIndex];
                        }

                    //}
                }
                objEDMModel.SaveChanges();
            }
            catch (Exception ex)
            {

            }
            
        }

        #endregion

        #region Public Methods

        public string GetErrorMsg()
        {
            return _errorMsg;
        }


        /// <summary>
        /// This method is used to compare the local price with the site Adjusted price and check whether any divident or split
        /// is occured and update the databases accordingly.
        /// </summary>
        public void PushDataIntoDataBase(DateTime dtEndDate, bool isExportData, string strAssetSymbol)
        {
            DateTime dtTempEndDate = new DateTime(dtEndDate.Year,dtEndDate.Month,dtEndDate.Day);
           // log.Info("Entering Data Scrapper");
            _errorMsg = string.Empty;
            List<AssetSymbolSet> lstAssetsToProcess = new List<AssetSymbolSet>();
            try
            {
                WebClient objWebClient = new WebClient();

                string strData = string.Empty;
                string strPreviousDateData = string.Empty;

                if (!string.IsNullOrEmpty(strAssetSymbol))
                {
                    AssetSymbolSet objAssetSymbol = objEDMModel.AssetSymbolSet.FirstOrDefault(x => x.Symbol.ToLower().Equals(strAssetSymbol.ToLower()));

                    if (objAssetSymbol != null)
                        lstAssetsToProcess.Add(objAssetSymbol);
                }
                else
                {
                    lstAssetsToProcess = objEDMModel.AssetSymbolSet.ToList();
                }
                
                var lstAssetSymbolSet = objEDMModel.AssetSymbolSet;
                
                //var lstAssetSet = objEDMModel.AssetSet;
               
                // Changed code for removing symbols processing in the symbol add windows service 2/4/2012
                //List<NewAssets> lstNewAsset = objEDMModel.NewAssets.ToList();
                //foreach (NewAssets objNewAsset in lstNewAsset)
                //{
                //    AssetSymbolSet objAssetSymbolToRemove = objEDMModel.AssetSymbolSet.FirstOrDefault(x => x.Symbol.Equals(objNewAsset.Symbol));
                //    lstAssetsToProcess.Remove(objAssetSymbolToRemove);
                //}

                decimal decNewPrice;
                bool isNewDate;
                bool isSplitOrDivident = false;
                bool isWindowUpdated;
                bool isAssetEligible;
                DateTime dtStartDate = new DateTime();
                DateTime dtPreviousStartDate = new DateTime();
                DateTime dtCurrentDate = new DateTime();

                //Iterate through the collection of AssetSymbolSet`s.
                foreach (AssetSymbolSet objAssetSymbolSet in lstAssetsToProcess)
                {
                    try
                    {
                       
                        isSplitOrDivident = false;
                        dtEndDate = dtTempEndDate;
                     
                            var lstAssetPriceSet = objEDMModel.AssetPriceSet.Where(x => x.AssetSymbolSet.Id.Equals(objAssetSymbolSet.Id));
                            isNewDate = false;
                            isWindowUpdated = false;
                            isAssetEligible = false;
                           
                             
                            //Get the update date from the AssetSet table+.
                            List<AssetSet> lstExistingAssetSet = objEDMModel.AssetSet.Where(x => x.AssetSymbolSet.Id.Equals(objAssetSymbolSet.Id)).ToList();

                            //if the date object is null then get the earliest trading date.
                            if (lstExistingAssetSet.Count() == 0)
                            {
                                //Get the earliest trading date.
                                dtStartDate = CommonUtility.GetEarliestTradingDate(objAssetSymbolSet.Symbol);

                                DateTime dt2yrsPrevDate = new DateTime(dtEndDate.Year - 2, dtEndDate.Month, dtEndDate.Day);
                                if (dt2yrsPrevDate > dtStartDate)
                                {
                                    isAssetEligible = true;
                                }

                                //set the flag to true it the data scrapper is run first time for this asset.
                                isNewDate = true;
                            }

                            //If date exists in the database.
                            if (!isNewDate)
                            {
                                //Get the start date from the existing AssetSet collection.
                                dtStartDate = GetLastTradingDate(lstExistingAssetSet);

                                //Get the previous date prior to the date in the database.
                                //dtPreviousStartDate = DateAndTime.DateAdd(DateInterval.Day, -1, dtStartDate);

                                //Fetch the data from the site in the string format.
                                strPreviousDateData = GetPreviousDateData(dtStartDate, objAssetSymbolSet.Symbol);
                                
                                if (!string.IsNullOrEmpty(strPreviousDateData))
                                {
                                    //Split the string data for each new line character.
                                    string[] strSplitNewLineCollection = strPreviousDateData.Split("\n".ToCharArray());                                                                   

                                    //Split the string data by comma.
                                    string[] strSplitByCommaCollection = strSplitNewLineCollection[1].Split(",".ToCharArray());

                                    //If the collection count is equal to 7 then go for further calculations.
                                    if (strSplitByCommaCollection.Length == 7)
                                    {
                                        dtCurrentDate = CommonUtility.GenerateDateFromInputString(strSplitByCommaCollection[0]);

                                        //Fetch the data from the local database for the particular date.
                                        var objExistingAsset = lstAssetPriceSet.FirstOrDefault(x => x.Date.Equals(dtCurrentDate));

                                        //Get the latest Price from the site.
                                        decNewPrice = Convert.ToDecimal(strSplitByCommaCollection[6]);
                                        if (objExistingAsset != null)
                                        {
                                            //Compare the database price with the site price.
                                            //If dividend or split occurs then update the old entries with the new asset price.
                                            if (objExistingAsset.Price != decNewPrice)
                                            {
                                                isSplitOrDivident = true;
                                                lstAssetPriceSet.FirstOrDefault(x => x.Date.Equals(dtCurrentDate)).Price = decNewPrice;
                                                DateTime dtEarliestTradingDate;

                                                //Get the earliest trading date for these asset.
                                                dtEarliestTradingDate = CommonUtility.GetEarliestTradingDate(objAssetSymbolSet.Symbol);

                                                //update the old entries with the new asset price till previous date.
                                                ReUpdate(objAssetSymbolSet.Symbol, dtEarliestTradingDate, dtStartDate, objAssetSymbolSet.Id);



                                                //update the entries with the new asset price till end date starting with the updated date from the database.
                                                dtEndDate = UpdateCurrentWindow(objAssetSymbolSet.Symbol, dtStartDate, dtEndDate, objAssetSymbolSet.Id);
                                                isWindowUpdated = true;
                                            }
                                        }
                                        else
                                        {
                                            //If the instance is null then that indicates that there is no such entry in the database.
                                            //So update this new entry in the local database.
                                            if (objExistingAsset == null)
                                            {
                                                AssetPriceSet objAssetPriceSet = new AssetPriceSet();

                                                objAssetPriceSet.Date = dtCurrentDate;
                                                
                                                objAssetPriceSet.Price = Convert.ToDecimal(strSplitByCommaCollection[6]);

                                                objAssetPriceSet.AssetSymbolSet = objEDMModel.AssetSymbolSet.FirstOrDefault(x => x.Id.Equals(objAssetSymbolSet.Id));
                                                objEDMModel.AddToAssetPriceSet(objAssetPriceSet);
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (isAssetEligible)
                                {
                                    //update the entries with the new asset price till end date starting with startd date.
                                    dtEndDate = UpdateCurrentWindow(objAssetSymbolSet.Symbol, dtStartDate, dtEndDate, objAssetSymbolSet.Id);
                                }
                                else
                                {
                                    //log.Error("Error in Data Scrapper for symbol::"+ objAssetSymbolSet.Symbol + "  Exception : Data available is insufficient to process further" );
                                    log.Error(objAssetSymbolSet.Symbol + "\t" + "Price data available is insufficient to process Data Scrapper");
                                }
                                isWindowUpdated = true;
                            }
                            if (!isWindowUpdated)
                                dtEndDate = UpdateCurrentWindow(objAssetSymbolSet.Symbol, dtStartDate, dtEndDate, objAssetSymbolSet.Id);

                            //Set the End date as the last update date in the AssetSet database.
                            //lstAssetSet.FirstOrDefault( x => x.Id.Equals( objAssetSymbolSet.Id ) ).UpdateDate = dtEndDate;

                            //If divident or split occurs then set the value of the dividentsplit property to 1 or 0.
                            int intAssetCount = 0;
                            intAssetCount = objEDMModel.AssetSet.Where(x => x.AssetSymbolSet.Id.Equals(objAssetSymbolSet.Id)).Count();
                            if (intAssetCount > 0)
                            {
                                if (isSplitOrDivident)
                                    objEDMModel.AssetSet.First(x => x.AssetSymbolSet.Id.Equals(objAssetSymbolSet.Id)).DividentSplit = 1;
                                else
                                    objEDMModel.AssetSet.First(x => x.AssetSymbolSet.Id.Equals(objAssetSymbolSet.Id)).DividentSplit = 0;
                            }

                            //Finally commit the changes to the database.
                            objEDMModel.Connection.Close();
                            objEDMModel.SaveChanges();

                            //**Auto Correct Price Data
                            if (isSplitOrDivident)
                            {
                                List<AutoCorrectPriceData> lstAutoCorrPrcData = objEDMModel.AutoCorrectPriceData.Where(x => x.AssetSymbolSet.Id.Equals(objAssetSymbolSet.Id)).ToList();
                                if (lstAutoCorrPrcData.Count > 0)
                                {
                                    foreach (AutoCorrectPriceData objAutoCorrPrcData in lstAutoCorrPrcData)
                                    {
                                        decimal decCorrectPrice = objEDMModel.AssetPriceSet.FirstOrDefault(x => x.Date.Equals((DateTime)objAutoCorrPrcData.DateOfCorrectPrice) && x.AssetSymbolSet.Id.Equals(objAssetSymbolSet.Id)).Price;
                                        objEDMModel.AssetPriceSet.FirstOrDefault(x => x.Date.Equals((DateTime)objAutoCorrPrcData.DateOfBadPrice) && x.AssetSymbolSet.Id.Equals(objAssetSymbolSet.Id)).Price = decCorrectPrice;
                                    }
                                }
                            }
                            objEDMModel.SaveChanges();
                        

                    }
                    catch (Exception ex)
                    {
                        //log.Error("Error in Data Scrapper for Symbol::" + objAssetSymbolSet.Symbol +"  Exception : "+ ex.Message); 
                        if (CommonUtility.isConnectionAvailable())
                        {
                            log.Error(objAssetSymbolSet.Symbol + "\t" + ex.Message);
                        }
                        else
                        {
                            log.Error(objAssetSymbolSet.Symbol + "\t" + "It seems to be a problem in network/internet connection");
                        }   
                    }

                }

                #region Eliminate Zero Price values in AssetPriceSet
                foreach (AssetSymbolSet objAssetSymbolSet in lstAssetsToProcess)
                {
                    try
                    {
                        List<AssetPriceSet> lstAssetPriceData = objEDMModel.AssetPriceSet.Where(x => x.AssetSymbolSet.Id.Equals(objAssetSymbolSet.Id) && x.Date < dtEndDate.Date).OrderBy(x => x.Date).ToList<AssetPriceSet>();
                        if (lstAssetPriceData.Count > 0)
                        {
                            for (int i = 0; i < lstAssetPriceData.Count; i++)
                            {
                                if (lstAssetPriceData[i].Price == 0)
                                {
                                    if (i == 0)
                                        lstAssetPriceData[i].Price = (decimal)0.01;
                                    else
                                        lstAssetPriceData[i].Price = lstAssetPriceData[i - 1].Price;
                                }
                            }
                        }
                    
                        objEDMModel.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        log.Error(objAssetSymbolSet.Symbol + "\t" + ex.Message);
                    }
                }
                #endregion

                #region Fill Missing days
                //Fill in the missing days (Holidays) prices. But Saturdays and Sundays are skipped.

                foreach (AssetSymbolSet objAssetSymbolSet in lstAssetsToProcess)
                {
                    try
                    {
                        // Changing dtEndDate to dtTempEndDate
                        //List<AssetPriceSet> lstAssetPriceData = objEDMModel.AssetPriceSet.Where(x => x.AssetSymbolSet.Id.Equals(objAssetSymbolSet.Id) && x.Date < dtEndDate).OrderBy(x => x.Date).ToList<AssetPriceSet>();
                    List<AssetPriceSet> lstAssetPriceData = objEDMModel.AssetPriceSet.Where(x => x.AssetSymbolSet.Id.Equals(objAssetSymbolSet.Id) && x.Date < dtEndDate).OrderBy(x => x.Date).ToList<AssetPriceSet>();
                    
                    if (lstAssetPriceData.Count > 0)
                    {
                        DateTime dtPricePrv = lstAssetPriceData[0].Date;
                        DateTime dtPriceNxt;
                        for (int intI = 1; intI < lstAssetPriceData.Count; intI++)
                        {
                            dtPriceNxt = lstAssetPriceData[intI].Date;
                            if (Microsoft.VisualBasic.DateAndTime.DateDiff(DateInterval.Day, dtPricePrv, dtPriceNxt, Microsoft.VisualBasic.FirstDayOfWeek.Sunday, FirstWeekOfYear.Jan1) >= 1)
                            {
                                DateTime dtFillStartDate = Microsoft.VisualBasic.DateAndTime.DateAdd(DateInterval.Day, 1, dtPricePrv);
                                while (dtFillStartDate < dtPriceNxt)
                                {
                                    if (!(dtFillStartDate.DayOfWeek.Equals(DayOfWeek.Saturday) || dtFillStartDate.DayOfWeek.Equals(DayOfWeek.Sunday)))
                                    {
                                        DateTime dtPreviousDate = CommonUtility.GetPreviousDate(dtFillStartDate, objAssetSymbolSet.Id);
                                        AssetPriceSet extAssetPriceSet = objEDMModel.AssetPriceSet.FirstOrDefault(x => x.Date.Equals(dtPreviousDate) && x.AssetSymbolSet.Id.Equals(objAssetSymbolSet.Id));
                                      
                                        if(extAssetPriceSet != null)
                                        {
                                            AssetPriceSet objAstPrcSet = new AssetPriceSet();
                                            objAstPrcSet.Date = dtFillStartDate;
                                            objAstPrcSet.Price = extAssetPriceSet.Price;
                                            objAstPrcSet.MeanDeviation = extAssetPriceSet.MeanDeviation;
                                            objAstPrcSet.PriceTrend = extAssetPriceSet.PriceTrend;
                                            objAstPrcSet.Growth = extAssetPriceSet.Growth;
                                            objAstPrcSet.State = extAssetPriceSet.State;
                                            objAstPrcSet.Threshold = extAssetPriceSet.Threshold;
                                            objAstPrcSet.AssetSymbolSet = objEDMModel.AssetSymbolSet.FirstOrDefault(x => x.Id.Equals(objAssetSymbolSet.Id));
                                            objEDMModel.AddToAssetPriceSet(objAstPrcSet);
                                        }
                                    }
                                    dtFillStartDate = Microsoft.VisualBasic.DateAndTime.DateAdd(DateInterval.Day, 1, dtFillStartDate);
                                }
                                if (lstAssetPriceData.Where(x => x.Date.Equals(dtFillStartDate)).Count() > 0 && lstAssetPriceData.FirstOrDefault(x => x.Date.Equals(dtFillStartDate)).Price == 0)
                                {
                                    int intDays = 0;
                                    if (dtFillStartDate.Day == 1)
                                    {
                                      intDays = DateTime.DaysInMonth(dtFillStartDate.Year, dtFillStartDate.Month - 1);
                                    }
                                    else
                                    {
                                        intDays = dtFillStartDate.Day-1;
                                    }
                                    DateTime dtPreviousDate = CommonUtility.GetPreviousDate(new DateTime(dtFillStartDate.Year,dtFillStartDate.Month,intDays), objAssetSymbolSet.Id);
                                    AssetPriceSet extAssetPriceSet = objEDMModel.AssetPriceSet.FirstOrDefault(x => x.Date.Equals(dtPreviousDate) && x.AssetSymbolSet.Id.Equals(objAssetSymbolSet.Id));
                                    if (extAssetPriceSet != null)
                                       objEDMModel.AssetPriceSet.FirstOrDefault(x => x.Date.Equals(dtFillStartDate) && x.AssetSymbolSet.Id.Equals(objAssetSymbolSet.Id)).Price = extAssetPriceSet.Price;
                                }
                            }
                            dtPricePrv = dtPriceNxt;
                            //dblPriceToCarry = (double) lstAssetPriceData[intI].Price;
                        }
                        objEDMModel.SaveChanges();
                       
                        

                        #region MyRegion fill data for current date if price not found
                        List<AssetPriceSet> lstFillerPrices = objEDMModel.AssetPriceSet.Where(x => x.AssetSymbolSet.Id.Equals(objAssetSymbolSet.Id)).OrderBy(x => x.Date).ToList<AssetPriceSet>();
                        DateTime dtLastDate = lstFillerPrices.Last().Date;
                        while (dtLastDate < dtTempEndDate)
                        {
                            DateTime dtNextDate = Microsoft.VisualBasic.DateAndTime.DateAdd(DateInterval.Day, 1, dtLastDate);
                            if (dtNextDate < dtTempEndDate)
                            {
                                if (!(dtNextDate.DayOfWeek.Equals(DayOfWeek.Saturday) || dtNextDate.DayOfWeek.Equals(DayOfWeek.Sunday)))
                                {
                                    lstFillerPrices = objEDMModel.AssetPriceSet.Where(x => x.AssetSymbolSet.Id.Equals(objAssetSymbolSet.Id)).OrderBy(x => x.Date).ToList<AssetPriceSet>();
                                    AssetPriceSet extAssetPriceSet = lstFillerPrices.Last(); //objEDMModel.AssetPriceSet.FirstOrDefault(x => x.Date.Equals(dtLastDate) && x.AssetSymbolSet.Id.Equals(objAssetSymbolSet.Id));

                                    if (extAssetPriceSet != null)
                                    {
                                        AssetPriceSet objAstPrcSet = new AssetPriceSet();
                                        objAstPrcSet.Date = dtNextDate;
                                        objAstPrcSet.Price = extAssetPriceSet.Price;
                                        objAstPrcSet.MeanDeviation = extAssetPriceSet.MeanDeviation;
                                        objAstPrcSet.PriceTrend = extAssetPriceSet.PriceTrend;
                                        objAstPrcSet.Growth = extAssetPriceSet.Growth;
                                        objAstPrcSet.State = extAssetPriceSet.State;
                                        objAstPrcSet.Threshold = extAssetPriceSet.Threshold;
                                        objAstPrcSet.AssetSymbolSet = objEDMModel.AssetSymbolSet.FirstOrDefault(x => x.Id.Equals(objAssetSymbolSet.Id));
                                        objEDMModel.AddToAssetPriceSet(objAstPrcSet);
                                        objEDMModel.SaveChanges();
                                    }
                                }
                            }
                            dtLastDate = dtNextDate;
                        } 
                        #endregion
                    }
                    }
                    catch (Exception ex)
                    {
                        //log.Error("Error in Data Scrapper for Symbol::" + objAssetSymbolSet.Symbol + "  Exception : " + ex.Message); 
                        log.Error(objAssetSymbolSet.Symbol + "\t" + ex.Message);
                    }
 
                }
                objEDMModel.SaveChanges();
                #endregion



                //Calculate Growth.
                //Application objXlApp = new Application();
                foreach (AssetSymbolSet objAssetSymbolSet in lstAssetsToProcess)
                {
                    #region Excel growth functionality commented

                    //try
                    //{
                    //    int intAssetPriceCount = objEDMModel.AssetPriceSet.Where(x => x.AssetSymbolSet.Id.Equals(objAssetSymbolSet.Id)).Count();
                    //    if (intAssetPriceCount > 0)
                    //    {
                    //        dtEndDate = new DateTime(dtEndDate.Year, dtEndDate.Month, dtEndDate.Day);
                    //        int intAssetCount = objEDMModel.AssetPriceSet.Where(x => x.Date > dtEndDate && x.AssetSymbolSet.Id.Equals(objAssetSymbolSet.Id)).Count();
                    //        if (intAssetCount == 0)
                    //        {
                    //            var objAssetPriceSetColl = objEDMModel.AssetPriceSet.Where(x => x.AssetSymbolSet.Id.Equals(objAssetSymbolSet.Id)).OrderBy(x => x.Date);
                    //            dtEndDate = GetAssetPriceSetLastTradingDate(objAssetPriceSetColl.ToList());
                    //            double[] arrDates = objAssetPriceSetColl.Select(x => x.Date).ToArray().Select(x => x.ToOADate()).ToArray();
                    //            double[] arrPrices = objAssetPriceSetColl.Select(x => x.Price).ToArray().Select(x => (double)x).ToArray();
                    //            double[] arrGrowthVals = ((Array)objXlApp.WorksheetFunction.Growth(arrPrices, arrDates, Missing.Value, true)).OfType<object>().ToArray().Select(x => Convert.ToDouble(x.ToString())).ToArray();

                    //            //Update the existing records growth values.
                    //            for (int intIndex = 0; intIndex < arrDates.Count(); intIndex++)
                    //            {
                    //                DateTime dtUpdateDate = DateTime.FromOADate(arrDates[intIndex]);
                    //                objAssetPriceSetColl.First(x => x.Date.Equals(dtUpdateDate)).Growth = (decimal)arrGrowthVals[intIndex];
                    //            }

                    //            //Calculate future growth values.
                    //            AssetPriceSet objAssetPriceSet = new AssetPriceSet();
                    //            double[] arrFutureDates = new double[65];
                    //            DateTime dtDate = dtEndDate;
                    //            int intDayCount = 0;
                    //            while (intDayCount < 65)
                    //            {
                    //                dtDate = dtDate.AddDays(1);
                    //                if (dtDate.DayOfWeek.Equals(DayOfWeek.Saturday) || dtDate.DayOfWeek.Equals(DayOfWeek.Sunday))
                    //                {

                    //                }
                    //                else
                    //                {
                    //                    arrFutureDates[intDayCount] = dtDate.ToOADate();
                    //                    intDayCount++;
                    //                }
                    //            }

                    //            double[] arrFutureGrowthVals = ((Array)objXlApp.WorksheetFunction.Growth(arrPrices, arrDates, arrFutureDates, true)).OfType<object>().ToArray().Select(x => Convert.ToDouble(x.ToString())).ToArray();

                    //            //Make a new entry for all the future growth values.
                    //            for (int intIndex = 0; intIndex < arrFutureDates.Count(); intIndex++)
                    //            {
                    //                objAssetPriceSet = new AssetPriceSet();

                    //                objAssetPriceSet.Date = DateTime.FromOADate(arrFutureDates[intIndex]);
                    //                objAssetPriceSet.Growth = (decimal)arrFutureGrowthVals[intIndex];

                    //                objAssetPriceSet.AssetSymbolSet = objEDMModel.AssetSymbolSet.FirstOrDefault(x => x.Id.Equals(objAssetSymbolSet.Id));
                    //                objEDMModel.AddToAssetPriceSet(objAssetPriceSet);
                    //            }
                    //        }
                    //    }
                    //    objEDMModel.Connection.Close();
                    //    objEDMModel.SaveChanges();
                    //}
                    //catch (Exception ex)
                    //{
                    //    //log.Error("Error in Data Scrapper for Symbol::" + objAssetSymbolSet.Symbol + "  Exception : " + ex.Message); 
                    //    log.Error(objAssetSymbolSet.Symbol + "\t" + ex.Message);
                    //}
                    #endregion

                    //** growth reduction logic
                    var varAssetPriceSet = objEDMModel.AssetPriceSet.Where(x => x.AssetSymbolSet.Id.Equals(objAssetSymbolSet.Id) && x.Growth.Equals(null));
                    AssetSet objAssetSet = objEDMModel.AssetSet.First(x => x.AssetSymbolSet.Id.Equals(objAssetSymbolSet.Id));
                    if (objAssetSet != null)
                    {

                        if (objAssetSet.LoopUpdateDate != null)
                        {
                            Double dblM = (double)objAssetSet.M;
                            Double dblA = (double)objAssetSet.A;
                            DateTime dtUpdateDate = (DateTime)objAssetSet.LoopUpdateDate;
                            int intDividendOrSplit = (int)objAssetSet.DividentSplit;
                            DateTime dtAssetStartDate = CommonUtility.GetStartDate(objAssetSymbolSet.Id);

                            int intUpdateInterval = objEDMModel.TrendEngineVariablesSet.Select(x => x.LoopUpdateInterval.Id).First();
                            if (intUpdateInterval == 1)
                            {
                                CalculateHistoricalGrowth(objAssetSymbolSet, dtEndDate);
                            }
                            else if (intUpdateInterval == 2)
                            {
                                if (intDividendOrSplit == 1 || DateTime.Today.DayOfWeek.Equals(DayOfWeek.Saturday))
                                {
                                    CalculateHistoricalGrowth(objAssetSymbolSet, dtEndDate);
                                }
                                else
                                {                                    
                                    foreach (AssetPriceSet objAssetPriceSet in varAssetPriceSet)
                                    {
                                        int intDayCount = (objAssetPriceSet.Date.Subtract(dtAssetStartDate).Days + 1) * 5 / 7;
                                        objAssetPriceSet.Growth = (Decimal)(dblA * Math.Exp(dblM * intDayCount));
                                    }
                                }
                            }
                            else if (intUpdateInterval == 3)
                            {
                                if (intDividendOrSplit == 1 || DateTime.Today.Day.Equals(1) )
                                {
                                    CalculateHistoricalGrowth(objAssetSymbolSet, dtEndDate);
                                }
                                else
                                {
                                    foreach (AssetPriceSet objAssetPriceSet in varAssetPriceSet)
                                    {
                                        int intDayCount = (objAssetPriceSet.Date.Subtract(dtAssetStartDate).Days + 1) * 5 / 7;
                                        objAssetPriceSet.Growth = (Decimal)(dblA * Math.Exp(dblM * intDayCount));
                                    }
                                } 
                            }
                            else if (intUpdateInterval == 4)
                            {
                                bool boolIsHistoricalGrowth = false;

                                if (DateTime.Today.Day.Equals(1))
                                    if (DateTime.Today.Month == 1 || DateTime.Today.Month == 4 || DateTime.Today.Month == 7 || DateTime.Today.Month == 10)
                                        boolIsHistoricalGrowth = true;                                                                  
 
                                if (intDividendOrSplit == 1 || boolIsHistoricalGrowth )
                                {
                                    CalculateHistoricalGrowth(objAssetSymbolSet, dtEndDate);
                                }
                                else
                                {
                                    foreach (AssetPriceSet objAssetPriceSet in varAssetPriceSet)
                                    {
                                        int intDayCount = (objAssetPriceSet.Date.Subtract(dtAssetStartDate).Days + 1) * 5 / 7;
                                        objAssetPriceSet.Growth = (Decimal)(dblA * Math.Exp(dblM * intDayCount));
                                    }
                                }
                            }
                            else if (intUpdateInterval == 5)
                            {
                                if (intDividendOrSplit == 1 || (DateTime.Today.Month.Equals(1) && DateTime.Today.Day.Equals(1)))
                                {
                                    CalculateHistoricalGrowth(objAssetSymbolSet, dtEndDate);
                                }
                                else
                                {
                                    foreach (AssetPriceSet objAssetPriceSet in varAssetPriceSet)
                                    {
                                        int intDayCount = (objAssetPriceSet.Date.Subtract(dtAssetStartDate).Days + 1) * 5 / 7;
                                        objAssetPriceSet.Growth = (Decimal)(dblA * Math.Exp(dblM * intDayCount));
                                    }
                                }
                            }
                            else if (intUpdateInterval == 6)
                            {
                                foreach (AssetPriceSet objAssetPriceSet in varAssetPriceSet)
                                {
                                    int intDayCount = (objAssetPriceSet.Date.Subtract(dtAssetStartDate).Days + 1) * 5 / 7;
                                    objAssetPriceSet.Growth = (Decimal)(dblA * Math.Exp(dblM * intDayCount));
                                }                                
                            }
                        }
                        else
                        {
                            CalculateHistoricalGrowth(objAssetSymbolSet, dtEndDate);
                        }
                    }
                    else
                    {
                        CalculateHistoricalGrowth(objAssetSymbolSet, dtEndDate);
                    }
                    //    
                    //Change code to see schedular process timing
                   // objAssetSymbolSet.EndTime = DateTime.Now;
                }

                //Finally commit the changes to the database.
                objEDMModel.SaveChanges();
                objEDMModel.Connection.Close();
                if (isExportData)
                    ExportAssetPriceSetToExcel();

            }
            catch (Exception ex)
            {
                _errorMsg = ex.Message;
                //log.Error("Error in Data Scrapper "+ "  Exception : " + ex.Message); 
            }
            
        }

        

        public void ReadDSLog(string path)
        {
            try
            {
                FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read);
                StreamReader file = new StreamReader(fs);
                
                int counter = 0;
                string line;

                // Read the file and display it line by line.
                //StreamReader file = new System.IO.StreamReader("./TestApplication/Logs/DS_Log.txt");
                ArrayList errorSymbols = new ArrayList();
                while ((line = file.ReadLine()) != null)
                {
                    string[] splitLine = line.Split('\t').ToArray();
                    string[] splitDateTime = splitLine[1].Split(' ').ToArray();
                    string[] splitDate = splitDateTime[0].Split('-').ToArray();

                    DateTime logDate = new DateTime(Convert.ToInt32(splitDate[0]), Convert.ToInt32(splitDate[1]), Convert.ToInt32(splitDate[2]));
                    
                  
                    if (logDate.Equals(DateTime.Now.Date))
                    {
                        if (!errorSymbols.Contains(splitLine[3].ToString().Trim()))
                        {
                            errorSymbols.Add(splitLine[3].ToString().Trim());
                        }
                    }
                    counter++;
                }

                file.Close();
                fs.Close();

                //ArrayList noDups = new ArrayList();

                //foreach (string strItem in errorSymbols)
                //{
                //    if (!noDups.Contains(strItem.Trim()))
                //    {
                //        noDups.Add(strItem.Trim());
                //    }
                //}

                for (int i = 0; i < errorSymbols.Count; i++)
                {
                    PushDataIntoDataBase(DateTime.Now, false, errorSymbols[i].ToString());
                }
               

            }
            catch (Exception ex)
            {
               
            }
            
            // Suspend the screen.
            

        }

        public void ReloadPriceDataForSymbol(string strAssetSymbol)
        {
            try
            {
                DateTime dtEndDate = DateTime.Today.Date;
                DateTime dtStartDate = new DateTime();
                AssetSymbolSet objAssetSymbol = objEDMModel.AssetSymbolSet.FirstOrDefault(x => x.Symbol.ToLower().Equals(strAssetSymbol.ToLower()));
                var lstAssetPriceSet = objEDMModel.AssetPriceSet.Where(x => x.AssetSymbolSet.Id.Equals(objAssetSymbol.Id));
                List<AssetSet> lstExistingAssetSet = objEDMModel.AssetSet.Where(x => x.AssetSymbolSet.Id.Equals(objAssetSymbol.Id)).ToList();
                if (lstExistingAssetSet.Count > 0)
                {
                    dtEndDate = GetLastTradingDate(lstExistingAssetSet);                    
                }
                dtStartDate = CommonUtility.GetEarliestTradingDate(strAssetSymbol);
                ReUpdate(strAssetSymbol, dtStartDate, dtEndDate, objAssetSymbol.Id);
                objEDMModel.SaveChanges();
            }
            catch (Exception ex)
            {

            }
            
        }
        public List<AssetSymbolSet> GetAssetSymbols()
        {
            return objEDMModel.AssetSymbolSet.ToList();
        }

        #endregion
    }
}
