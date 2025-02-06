using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net;
using Microsoft.VisualBasic;
using System.Web.UI.WebControls;
using Microsoft.Office.Interop.Excel;
using System.Text;
using System.Web.UI;
using System.Reflection;

namespace HarborEast.BAL
{
    public class clsDSBackTesting
    {

        #region Variables Declaration
        private string _errorMsg;
        private Harbor_EastEntities objEDMModel = new Harbor_EastEntities();
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

        #region Private Methods
        /// <summary>
        /// This method is used to get the datetime instance from the string.
        /// </summary>
        /// <param name="parStrDate">date in the string format.</param>
        /// <returns>datetime instance from the string.</returns>
        private DateTime GetDateFromString(string parStrDate)
        {
            _errorMsg = string.Empty;
            try
            {
                //Split the the string array by "-".
                string[] arrStrDateComps = parStrDate.Split("-".ToCharArray());
                DateTime dtRetDate;
                if (arrStrDateComps.Length == 3)
                    dtRetDate = new DateTime(int.Parse(arrStrDateComps[0]), int.Parse(arrStrDateComps[1]), int.Parse(arrStrDateComps[2]));
                else
                    dtRetDate = new DateTime(1, 1, 1);
                return dtRetDate;
            }
            catch (Exception ex)
            {
                _errorMsg = ex.Message.ToString();
                return new DateTime(1, 1, 1);
            }
        }


        private void ExportAssetPriceSetToExcel()
        {
            try
            {
                GridView gvExport = new GridView();
                gvExport.DataSource = objEDMModel.AssetPriceBackTestSet.OrderBy(x => x.Date);
                gvExport.DataBind();
                HttpContext.Current.Response.Clear();

                HttpContext.Current.Response.AddHeader("content-disposition", "attachment;filename=AssetPriceBackTestSet.xls");

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
            }
        }

        /// <summary>
        /// This method is used to return a recent trading date from the AssetBackTestSet collection passed as a parameter.
        /// </summary>
        /// <param name="lstAssetSet">list collection of all the AssetBackTestSet associated with the current AssetSymbolID.</param>
        /// <returns>returns the recently published trading date from the AssetBackTestSet collection.</returns>
        private DateTime GetAssetLastTradingDate(List<AssetPriceBackTestSet> lstAssetSet)
        {
            DateTime dt = new DateTime();
            try
            {
                var existingAssetCollection = lstAssetSet.OrderBy(x => x.Date).Last();
                dt = existingAssetCollection.Date;
            }
            catch (Exception ex)
            {
                _errorMsg = ex.Message;
            }
            return dt;
        }

        /// <summary>
        /// This method is used to return a recent trading date from the AssetBackTestSet collection passed as a parameter.
        /// </summary>
        /// <param name="lstAssetSet">list collection of all the AssetBackTestSet associated with the current AssetSymbolID.</param>
        /// <returns>returns the recently published trading date from the AssetBackTestSet collection.</returns>
        private DateTime GetLastTradingDate(List<AssetBackTestSet> lstAssetSet)
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
            }
            return dtLastTrading;
        }

        #endregion

        #region Public Methods
        

        /// <summary>
        /// This method is used to generate the URL for fetching the data from the yahoo site.
        /// </summary>
        /// <param name="strSymbol">The symbol for which the data should be fetch.</param>
        /// <param name="dtStartDate">The date from which the data should be fetched.</param>
        /// <param name="dtEndDate">The date till which the data should be fetched.</param>
        /// <returns>Returns the URL in the string format.</returns>
        public string GenerateURL(string strSymbol, DateTime dtStartDate, DateTime dtEndDate)
        {
            StringBuilder strUrl = new StringBuilder();
            _errorMsg = string.Empty;
            try
            {
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
            }
            return strUrl.ToString();
        }

        /// <summary>
        /// This method returns  error message
        /// </summary>
        /// <returns></returns>
        public string GetErrorMsg()
        {
            return _errorMsg;
        }
              
        /// <summary>
        /// This method is used to get the earliest trading date for a particular asset symbol.
        /// </summary>
        /// <param name="parSymbol">Asset symbol in the string format.</param>
        /// <returns>returns the earliest trading date for the asset symbol passed as a parameter.</returns>
        public DateTime GetEarliestTradingDate(string parSymbol)
        {
            _errorMsg = string.Empty;
            try
            {
                WebClient objWebClient = new WebClient();
                string strPreviousDateData = string.Empty;

                //bind URL to the property of WebClient instance.
                objWebClient.BaseAddress = "http://ichart.finance.yahoo.com/table.csv?s=" + parSymbol + "&a=00&b=00&c=0000&d=00&e=00&f=0000&g=m&ignore=.csv";

                //Fetch the data from the site in the string format.
                strPreviousDateData = objWebClient.DownloadString(objWebClient.BaseAddress);

                if (!string.IsNullOrEmpty(strPreviousDateData))
                {
                    //Split the string data for each new line character.
                    string[] strArrList = strPreviousDateData.Split("\n".ToCharArray());
                    if (strArrList.Length > 0)
                    {
                        int intCount = strArrList.Length - 2;
                        string[] strCommaList = strArrList[intCount].Split(",".ToCharArray());

                        //Return the date at the particular array index.
                        return GetDateFromString(strCommaList[0]);
                    }
                }
                return new DateTime(1, 1, 1);
            }
            catch (Exception ex)
            {
                _errorMsg = ex.Message;
                return new DateTime(1, 1, 1);
            }
        }

        /// <summary>
        /// This method is used to compare the local price with the site Adjusted price and check whether any divident or split
        /// is occured and update the databases accordingly.
        /// </summary>
        public void PushDataIntoDataBase(DateTime dtEndDate, bool isExportData)
        {
            _errorMsg = string.Empty;
            try
            {
                WebClient objWebClient = new WebClient();

                string strData = string.Empty;
                string strPreviousDateData = string.Empty;

                var lstAssetSymbolSet = objEDMModel.AssetSymbolSet;

                //var lstAssetSet = objEDMModel.AssetBackTestSet;

                decimal decNewPrice;
                bool isNewDate;
                bool isSplitOrDivident = false;
                bool isWindowUpdated;
                bool isAssetEligible;
                DateTime dtStartDate = new DateTime();
                DateTime dtPreviousStartDate = new DateTime();
                DateTime dtCurrentDate = new DateTime();

                //Iterate through the collection of AssetSymbolSet`s.
                foreach (AssetSymbolSet objAssetSymbolSet in lstAssetSymbolSet.ToArray())
                {
                    var lstAssetPriceSet = objEDMModel.AssetPriceBackTestSet.Where(x => x.AssetSymbolSet.Id.Equals(objAssetSymbolSet.Id));
                    isNewDate = false;
                    isWindowUpdated = false;
                    isAssetEligible = false;

                    //Get the update date from the AssetBackTestSet table+.
                    List<AssetBackTestSet> lstExistingAssetSet = objEDMModel.AssetBackTestSet.Where(x => x.AssetSymbolSet.Id.Equals(objAssetSymbolSet.Id)).ToList();

                    //if the date object is null then get the earliest trading date.
                    if (lstExistingAssetSet.Count() == 0)
                    {
                        //Get the earliest trading date.
                        dtStartDate = GetEarliestTradingDate(objAssetSymbolSet.Symbol);

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
                        //Get the start date from the existing AssetBackTestSet collection.
                        dtStartDate = GetLastTradingDate(lstExistingAssetSet);

                        //Get the previous date prior to the date in the database.
                        dtPreviousStartDate = DateAndTime.DateAdd(DateInterval.Day, -1, dtStartDate);

                        //Fetch the data from the site in the string format.
                        strPreviousDateData = GetPreviousDateData(dtPreviousStartDate, objAssetSymbolSet.Symbol);

                        if (!string.IsNullOrEmpty(strPreviousDateData))
                        {
                            //Split the string data for each new line character.
                            string[] strSplitNewLineCollection = strPreviousDateData.Split("\n".ToCharArray());
                            //Split the string data by comma.
                            string[] strSplitByCommaCollection = strSplitNewLineCollection[1].Split(",".ToCharArray());

                            //If the collection count is equal to 7 then go for further calculations.
                            if (strSplitByCommaCollection.Length == 7)
                            {
                                dtCurrentDate = GetDateFromString(strSplitByCommaCollection[0]);

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
                                        dtEarliestTradingDate = GetEarliestTradingDate(objAssetSymbolSet.Symbol);

                                        //update the old entries with the new asset price till previous date.
                                        ReUpdate(objAssetSymbolSet.Symbol, dtEarliestTradingDate, dtPreviousStartDate, objAssetSymbolSet.Id);

                                        //update the entries with the new asset price till end date starting with the updated date from the database.
                                        UpdateCurrentWindow(objAssetSymbolSet.Symbol, dtStartDate, dtEndDate, objAssetSymbolSet.Id);
                                        isWindowUpdated = true;
                                    }
                                }
                                else
                                {
                                    //If the instance is null then that indicates that there is no such entry in the database.
                                    //So update this new entry in the local database.
                                    if (objExistingAsset == null)
                                    {
                                        AssetPriceBackTestSet objAssetPriceSet = new AssetPriceBackTestSet();

                                        objAssetPriceSet.Date = dtCurrentDate;

                                        objAssetPriceSet.Price = Convert.ToDecimal(strSplitByCommaCollection[6]);

                                        objAssetPriceSet.AssetSymbolSet = objEDMModel.AssetSymbolSet.FirstOrDefault(x => x.Id.Equals(objAssetSymbolSet.Id));
                                        objEDMModel.AddToAssetPriceBackTestSet(objAssetPriceSet);
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
                            UpdateCurrentWindow(objAssetSymbolSet.Symbol, dtStartDate, dtEndDate, objAssetSymbolSet.Id);
                        }
                        isWindowUpdated = true;
                    }
                    if (!isWindowUpdated)
                        UpdateCurrentWindow(objAssetSymbolSet.Symbol, dtStartDate, dtEndDate, objAssetSymbolSet.Id);

                    //Set the End date as the last update date in the AssetBackTestSet database.
                    //lstAssetSet.FirstOrDefault( x => x.Id.Equals( objAssetSymbolSet.Id ) ).UpdateDate = dtEndDate;

                    //If divident or split occurs then set the value of the dividentsplit property to 1 or 0.
                    int intAssetCount = 0;
                    intAssetCount = objEDMModel.AssetBackTestSet.Where(x => x.AssetSymbolSet.Id.Equals(objAssetSymbolSet.Id)).Count();
                    if (intAssetCount > 0)
                    {
                        if (isSplitOrDivident)
                            objEDMModel.AssetBackTestSet.First(x => x.AssetSymbolSet.Id.Equals(objAssetSymbolSet.Id)).DividentSplit = 1;
                        else
                            objEDMModel.AssetBackTestSet.First(x => x.AssetSymbolSet.Id.Equals(objAssetSymbolSet.Id)).DividentSplit = 0;
                    }

                    //Finally commit the changes to the database.
                    objEDMModel.Connection.Close();
                    objEDMModel.SaveChanges();
                }



                //Calculate Growth.
                Application objXlApp = new Application();
                foreach (AssetSymbolSet objAssetSymbolSet in lstAssetSymbolSet.ToArray())
                {
                    int intAssetPriceCount = objEDMModel.AssetPriceBackTestSet.Where(x => x.AssetSymbolSet.Id.Equals(objAssetSymbolSet.Id)).Count();
                    if (intAssetPriceCount > 0)
                    {
                        dtEndDate = new DateTime(dtEndDate.Year, dtEndDate.Month, dtEndDate.Day);
                        int intAssetCount = objEDMModel.AssetPriceBackTestSet.Where(x => x.Date > dtEndDate && x.AssetSymbolSet.Id.Equals(objAssetSymbolSet.Id)).Count();
                        if (intAssetCount == 0)
                        {
                            var objAssetColl = objEDMModel.AssetPriceBackTestSet.Where(x => x.AssetSymbolSet.Id.Equals(objAssetSymbolSet.Id)).OrderBy(x => x.Date);
                            dtEndDate = GetAssetLastTradingDate(objAssetColl.ToList());
                            double[] arrDates = objAssetColl.Select(x => x.Date).ToArray().Select(x => x.ToOADate()).ToArray();
                            double[] arrPrices = objAssetColl.Select(x => x.Price).ToArray().Select(x => (double)x).ToArray();
                            double[] arrGrowthVals = ((Array)objXlApp.WorksheetFunction.Growth(arrPrices, arrDates, Missing.Value, true)).OfType<object>().ToArray().Select(x => Convert.ToDouble(x.ToString())).ToArray();

                            //Update the existing records growth values.
                            for (int intIndex = 0; intIndex < arrDates.Count(); intIndex++)
                            {
                                DateTime dtUpdateDate = DateTime.FromOADate(arrDates[intIndex]);
                                objAssetColl.First(x => x.Date.Equals(dtUpdateDate)).Growth = (decimal)arrGrowthVals[intIndex];
                            }

                            //Calculate future growth values.
                            AssetPriceBackTestSet objAssetPriceSet = new AssetPriceBackTestSet();
                            double[] arrFutureDates = new double[65];
                            DateTime dtDate = dtEndDate;
                            int intDayCount = 0;
                            while (intDayCount < 65)
                            {
                                dtDate = dtDate.AddDays(1);
                                if (dtDate.DayOfWeek.Equals(DayOfWeek.Saturday) || dtDate.DayOfWeek.Equals(DayOfWeek.Sunday))
                                {

                                }
                                else
                                {
                                    arrFutureDates[intDayCount] = dtDate.ToOADate();
                                    intDayCount++;
                                }
                            }

                            double[] arrFutureGrowthVals = ((Array)objXlApp.WorksheetFunction.Growth(arrPrices, arrDates, arrFutureDates, true)).OfType<object>().ToArray().Select(x => Convert.ToDouble(x.ToString())).ToArray();

                            //Make a new entry for all the future growth values.
                            for (int intIndex = 0; intIndex < arrFutureDates.Count(); intIndex++)
                            {
                                objAssetPriceSet = new AssetPriceBackTestSet();

                                objAssetPriceSet.Date = DateTime.FromOADate(arrFutureDates[intIndex]);
                                objAssetPriceSet.Growth = (decimal)arrFutureGrowthVals[intIndex];

                                objAssetPriceSet.AssetSymbolSet = objEDMModel.AssetSymbolSet.FirstOrDefault(x => x.Id.Equals(objAssetSymbolSet.Id));
                                objEDMModel.AddToAssetPriceBackTestSet(objAssetPriceSet);
                            }
                        }
                    }
                    objEDMModel.Connection.Close();
                    objEDMModel.SaveChanges();
                }

                //Finally commit the changes to the database.
                objEDMModel.SaveChanges();
                if (isExportData)
                    ExportAssetPriceSetToExcel();

            }
            catch (Exception ex)
            {
                _errorMsg = ex.Message;
            }
        }
            
        /// <summary>
        /// This method is used to return the data in the string format for a particular date.
        /// If no data found then get data for the previous date of that date and so on.
        /// </summary>
        /// <param name="dtPreviousDate">holds the previous date.</param>
        /// <param name="parSymbol">AssetBackTestSet symbol.</param>
        /// <returns>returns the data for the pervious date.</returns>
        public string GetPreviousDateData(DateTime dtPreviousDate, string parSymbol)
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
        public void UpdateCurrentWindow(string parSymbol, DateTime dtStartDate, DateTime dtEndDate, int intAssetSymbolId)
        {
            _errorMsg = string.Empty;
            try
            {
                string strData = string.Empty;
                WebClient objWebClient = new WebClient();
                DateTime dtCurrentDate = new DateTime();
                decimal decNewPrice;
                var lstAssetPriceSet = objEDMModel.AssetPriceBackTestSet.Where(x => x.AssetSymbolSet.Id.Equals(intAssetSymbolId));

                //Generate the URL and bind it to the property of WebClient instance.
                objWebClient.BaseAddress = GenerateURL(parSymbol, dtStartDate, dtEndDate);

                //Fetch the data from the site in the string format.
                strData = objWebClient.DownloadString(objWebClient.BaseAddress);

                if (!string.IsNullOrEmpty(strData))
                {
                    //Split the string data for each new line character.
                    string[] strRowLevelSplitCollection = strData.Split("\n".ToCharArray());

                    for (int intI = 1; intI < strRowLevelSplitCollection.Length; intI++)
                    {
                        //Split the string data by comma.
                        string[] strColumnLevelSplitCollection = strRowLevelSplitCollection[intI].Split(",".ToCharArray());

                        //If the collection count is equal to 7 then go for further calculations.
                        if (strColumnLevelSplitCollection.Length == 7)
                        {
                            dtCurrentDate = Convert.ToDateTime(strColumnLevelSplitCollection[0]);

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
                                    AssetPriceBackTestSet objAssetPriceSet = new AssetPriceBackTestSet();

                                    objAssetPriceSet.Date = dtCurrentDate;

                                    objAssetPriceSet.Price = decNewPrice;

                                    objAssetPriceSet.AssetSymbolSet = objEDMModel.AssetSymbolSet.FirstOrDefault(x => x.Id.Equals(intAssetSymbolId));
                                    objEDMModel.AddToAssetPriceBackTestSet(objAssetPriceSet);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _errorMsg = ex.Message;
            }
        }

        /// <summary>
        /// This method is used to update the records from the earliest date to updated date from the database.
        /// </summary>
        /// <param name="parSymbol">Asset symbol of the current asset in the string format.</param>
        /// <param name="dtStartDate">The date from which the data should be fetched.</param>
        /// <param name="dtEndDate">The date till which the data should be fetched.</param>
        /// <param name="intAssetSymbolId">Asset symbol id of the current asset.</param>
        public void ReUpdate(string parSymbol, DateTime dtStartDate, DateTime dtEndDate, int intAssetSymbolId)
        {
            _errorMsg = string.Empty;
            try
            {
                string strData;
                WebClient objWebClient = new WebClient();
                DateTime dtCurrentDate = new DateTime();
                decimal decNewPrice = 0;
                var lstAssetPriceSet = objEDMModel.AssetPriceBackTestSet.Where(x => x.AssetSymbolSet.Id.Equals(intAssetSymbolId));

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

                        //If the collection count is equal to 7 then go for further calculations.
                        if (strColumnLevelSplitCollection.Length == 7)
                        {
                            dtCurrentDate = Convert.ToDateTime(strColumnLevelSplitCollection[0]);

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
                                    AssetPriceBackTestSet objAssetPriceSet = new AssetPriceBackTestSet();

                                    objAssetPriceSet.Date = dtCurrentDate;

                                    objAssetPriceSet.Price = decNewPrice;

                                    objAssetPriceSet.AssetSymbolSet = objEDMModel.AssetSymbolSet.FirstOrDefault(x => x.Id.Equals(intAssetSymbolId));
                                    objEDMModel.AddToAssetPriceBackTestSet(objAssetPriceSet);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _errorMsg = ex.Message;
            }
        }
       
        
        #endregion
    }
}
