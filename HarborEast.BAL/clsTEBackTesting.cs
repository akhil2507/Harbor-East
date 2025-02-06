using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net;
using System.Web.UI.WebControls;
using System.IO;
using System.Web.UI;

namespace HarborEast.BAL
{
    public class clsTEBackTesting
    {
      

        #region variable declaration

        List<AssetPriceSet> objPriceList;
        double[] arrDates;
        double[] arrPrices;
        double[] arrGrowthVals;
        float[] arrDailyChange;

        int[] arrDayCount;
        int[] arrFilteredTrades;
        int[] arrTimeFrameYear;
        float[] arrCompAvg;
        float[] arrLMS;
        float[] arrDSLP;
        float[] arrLowerThreshold;
        float[] arrUpperThreshold;
        float[] arrUpperCAG;
        float[] arrLowerCAG;
        float[] arrEqAdjClosePrice;
        float[] arrVolatility;
        float[] arrSTDev;
        float[] arrCAGR;
        float[] arrCAGRTrend;
        float[] arrSTDevDb;
        float[] arrSTDevTrend;
        float[] arrMTBS;
        float[] arrINT;
        float[] arrIncrease;
        float[] arrIncreaseTrend;
        string[] arrInOutState;
        string[] arrInOutFilterState;
        private string _errorMsg;

        Harbor_EastEntities objHEEntities = new Harbor_EastEntities();
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

        #region Private methods
       
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
                _errorMsg = ex.Message;
                return new DateTime(1, 1, 1);
            }
        }


        /// <summary>
        /// This method is used to return a recent trading date from the AssetBackTestSet collection passed as a parameter.
        /// </summary>
        /// <param name="lstAssetSet">list collection of all the AssetBackTestSet associated with the current AssetSymbolID.</param>
        /// <returns>returns the recently published trading date from the AssetBackTestSet collection.</returns>
        private DateTime GetAssetLastTradingDate(List<AssetPriceSet> lstAssetSet)
        {
            DateTime dt = new DateTime();
            try
            {
                var existingAssetCollection = lstAssetSet.Where(x => x.Price > 0).OrderBy(x => x.Date).Last();
                dt = (DateTime)existingAssetCollection.Date;
            }
            catch (Exception ex)
            {
                _errorMsg = ex.Message;
            }
            return dt;
        }

        /// <summary>
        /// this method export AssetPriceSet data to Excel
        /// </summary>
        /// <param name="strAssetSymbol"></param>
        private void ExportAssetPriceSetToExcel(string strAssetSymbol)
        {
            try
            {
                GridView gvExport = new GridView();
                gvExport.DataSource = objHEEntities.AssetBackTestSet.Where(x => x.AssetSymbolSet.Symbol.Equals(strAssetSymbol, StringComparison.InvariantCultureIgnoreCase)).OrderBy(x => x.Ending_Date);
                gvExport.DataBind();
                HttpContext.Current.Response.Clear();

                HttpContext.Current.Response.AddHeader("content-disposition", "attachment;filename=" + strAssetSymbol + "AssetBackTestSet.xls");

                HttpContext.Current.Response.Charset = "";

                // If you want the option to open the Excel file without saving than

                // comment out the line below

                // Response.Cache.SetCacheability(HttpCacheability.NoCache);

                HttpContext.Current.Response.ContentType = "application/vnd.xls";

                StringWriter stringWrite = new StringWriter();

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
        #endregion

        #region public Methods
        
       
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
        /// This method is used to perform the Trend Engine calculations.
        /// </summary>
        /// <param name="parCurrentDate"></param>
        /// <param name="strSymbol"></param>
        /// <param name="isExportData"></param>
        public void PerformTrendEngineAnalysis(DateTime parCurrentDate, string strSymbol, bool isExportData)
        {
            try
            {
                DateTime dtStart = parCurrentDate;
                DateTime dtEnd = parCurrentDate;
                objHEEntities = new Harbor_EastEntities();
                AssetBackTestSet objAssetSet = null;
                var objAssetSymSet = objHEEntities.AssetSymbolSet.First(x => x.Symbol.Equals(strSymbol, StringComparison.InvariantCultureIgnoreCase));
                float decThreshBase = 0;
                float fltMaxPriceTrend;
                float fltOptimalTbase = 0;

                float decSMATrend;
                if (objAssetSymSet != null)
                {
                    //Calculate for the Historical data.
                    float MaxPriceTrend = PerformHistoricalTrendEngineAnalysis(parCurrentDate, strSymbol);

                    var extAssetObj = objHEEntities.AssetPriceSet.FirstOrDefault(x => x.Date.Equals(dtStart) && x.AssetSymbolSet.Id.Equals(objAssetSymSet.Id));
                    var objTrendEngine = objHEEntities.TrendEngineVariablesSet.First();
                    decSMATrend = (float)objTrendEngine.SMATrend;

                    float fltIncrement = (float)objTrendEngine.ThresholdIncrement;
                    float fltMax = (float)objTrendEngine.ThresholdtMax;
                    float fltMin = (float)objTrendEngine.ThresholdMin;
                    float decHysteresisOffset = (float)objTrendEngine.Hysteresis;
                    float fltLoopValMax = (float)objTrendEngine.LoopValMax;
                    float fltLoopValMin = (float)objTrendEngine.LoopValMin;
                    float fltLoopValIncrement = (float)objTrendEngine.LoopValIncrement;
                    int intStabilityWindowState = (int)objTrendEngine.WaitTrend;
                    int intTradingDays = (int)objTrendEngine.YearTradingDays;

                    string strPrevState = string.Empty;
                    DateTime dtCurrentState;

                    if (arrPrices.Length >= 166)
                    {
                        fltMaxPriceTrend = MaxPriceTrend;
                        if (true)
                        {
                            bool isDateEqual = false;
                            //Calculate the optimal Tbase.
                            for (float fltTbase = (float)fltLoopValMin; fltTbase <= (float)fltLoopValMax; fltTbase = fltTbase + (float)fltLoopValIncrement)
                            {
                                for (int intI = 166; intI < arrPrices.Length; intI++)
                                {
                                    decThreshBase = (float)fltTbase;

                                    //Calculate Upper Threshold.
                                    arrUpperThreshold[intI] = ((decThreshBase + (arrLMS[intI] * fltIncrement)) > fltMax ? fltMax : ((decThreshBase + (arrLMS[intI] * fltIncrement)) < fltMin ? fltMin : decThreshBase + (arrLMS[intI] * fltIncrement)));

                                    //Calculate IN/OUT State.
                                    arrInOutState[intI] = (arrDSLP[intI] > arrUpperThreshold[intI] ? "IN" : "OUT");

                                    //Calculate STDeviation.
                                    //arrSTDev[ intI ] = ( float )( STDev( arrDailyChange.Skip( intI - 1 - 21 ).Take( 21 ).Select( x => ( double )x ).ToArray( ) ) * Math.Pow( intTradingDays, 0.5 ) );

                                    //Calculate Day Count.
                                    arrDayCount[intI] = (arrInOutState[intI].Equals(arrInOutState[intI - 1]) ? arrDayCount[intI - 1] + 1 : 1);

                                    //Calculate IN/OUT Filter State.
                                    arrInOutFilterState[intI] = (arrDayCount[intI] > intStabilityWindowState ? arrInOutState[intI] : arrInOutFilterState[intI - 1]);

                                    //Calculate Filtered Trades.
                                    //arrFilteredTrades[ intI ] = ( arrInOutFilterState[ intI ].Equals( arrInOutFilterState[ intI - 1 ] ) ? 0 : 1 );

                                    //Calculate Equivalent adjacent closing price.
                                    arrEqAdjClosePrice[intI] = (arrInOutFilterState[intI - 1].Equals("IN", StringComparison.InvariantCultureIgnoreCase) ? (arrEqAdjClosePrice[intI - 1] * (float)(1 + arrDailyChange[intI])) : (arrEqAdjClosePrice[intI - 1] * ((float)(1 + 0.025 / (float)intTradingDays))));
                                }
                                if (isDateEqual)
                                {
                                    fltOptimalTbase = fltTbase;
                                    break;
                                }
                                if (arrEqAdjClosePrice[arrEqAdjClosePrice.Count() - 1] > fltMaxPriceTrend)
                                {
                                    fltMaxPriceTrend = arrEqAdjClosePrice[arrEqAdjClosePrice.Count() - 1];
                                    fltOptimalTbase = fltTbase;
                                }
                            }
                        }

                        //Initialize the variables for default values when the count is 163.
                        arrDayCount[163] = 0;
                        arrInOutFilterState[163] = "IN";
                        arrInOutState[163] = "IN";
                        strPrevState = arrInOutState[163];
                        dtCurrentState = DateTime.FromOADate(arrDates[163]);

                        HttpContext.Current.Response.Write("Tbase Optimal Calculation ends at :" + DateTime.Now);
                        for (int intI = 164; intI < arrPrices.Length; intI++)
                        {
                            #region Calculations
                            decThreshBase = (float)fltOptimalTbase;

                            //Calculate Upper Threshold.
                            arrUpperThreshold[intI] = ((decThreshBase + (arrLMS[intI] * fltIncrement)) > fltMax ? fltMax : ((decThreshBase + (arrLMS[intI] * fltIncrement)) < fltMin ? fltMin : decThreshBase + (arrLMS[intI] * fltIncrement)));

                            //Calculate Lower Threshold.
                            arrLowerThreshold[intI] = arrUpperThreshold[intI] - decHysteresisOffset;

                            //Calculate UpperCAG.
                            arrUpperCAG[intI] = arrUpperThreshold[intI] * (float)intTradingDays;

                            //Calculate LowerCAG.
                            arrLowerCAG[intI] = arrLowerThreshold[intI] * (float)intTradingDays;

                            //Calculate IN/OUT State.
                            arrInOutState[intI] = (arrDSLP[intI] > arrUpperThreshold[intI] ? "IN" : "OUT");

                            //Calculate STDeviation.
                            arrSTDev[intI] = (float)(CommonUtility.STDev(arrDailyChange.Skip(intI - 1 - 21).Take(21).Select(x => (double)x).ToArray()) * Math.Pow(intTradingDays, 0.5));

                            //Calculate Day Count.
                            arrDayCount[intI] = (arrInOutState[intI].Equals(arrInOutState[intI - 1], StringComparison.InvariantCultureIgnoreCase) ? arrDayCount[intI - 1] + 1 : 1);

                            //Calculate IN/OUT Filter State.
                            arrInOutFilterState[intI] = (arrDayCount[intI] > intStabilityWindowState ? arrInOutState[intI] : arrInOutFilterState[intI - 1]);

                            //Calculate Filtered Trades.
                            arrFilteredTrades[intI] = (arrInOutFilterState[intI].Equals(arrInOutFilterState[intI - 1]) ? 0 : 1);

                            //Calculate Equivalent adjacent closing price.
                            arrEqAdjClosePrice[intI] = (arrInOutFilterState[intI - 1].Equals("IN", StringComparison.InvariantCultureIgnoreCase) ? (arrEqAdjClosePrice[intI - 1] * (float)(1 + arrDailyChange[intI])) : (arrEqAdjClosePrice[intI - 1] * ((float)(1 + 0.025 / (float)intTradingDays))));

                            //Calculate Volatility.
                            if (arrEqAdjClosePrice[intI - 1] != 0)
                                arrVolatility[intI] = (float)((arrEqAdjClosePrice[intI] - arrEqAdjClosePrice[intI - 1]) / arrEqAdjClosePrice[intI - 1]);
                            else
                                arrVolatility[intI] = 0;

                            //Calculate Time frame for Year.
                            arrTimeFrameYear[intI] = (arrInOutFilterState[intI].Equals("IN", StringComparison.InvariantCultureIgnoreCase) ? 1 : 0);
                            if (intI > 166)
                            {
                                //Calculate Compound Annual Growth Rate.
                                if (arrPrices[164] != 0)
                                    arrCAGR[intI] = (float)(Math.Pow((arrPrices[intI] / arrPrices[164]), (float)(1 / ((float)(intI - 166) / intTradingDays))) - 1.0);
                                else
                                    arrCAGR[intI] = 0;

                                //Calculate Price Trend.
                                if (arrEqAdjClosePrice[164] != 0)
                                    arrCAGRTrend[intI] = (float)(Math.Pow((arrEqAdjClosePrice[intI] / arrEqAdjClosePrice[164]), (float)(1 / ((float)(intI - 166) / (float)intTradingDays))) - 1.0);
                                else
                                    arrCAGRTrend[intI] = 0;

                                //Calculate Standard Deviation.
                                arrSTDevDb[intI] = (float)(CommonUtility.STDev(arrDailyChange.Skip(149).Select(x => (double)x).Take(intI).ToArray()) * Math.Pow(intTradingDays, 0.5));

                                //Calculate Standard Deviation Trend.
                                arrSTDevTrend[intI] = (float)(CommonUtility.STDev((arrVolatility.Skip(165).Select(x => (double)x).Take(intI).ToArray())) * Math.Pow(intTradingDays, 0.5));

                                var colToSum = arrFilteredTrades.Skip(164);
                                if (intI - 165 != 0)
                                    arrMTBS[intI] = (float)(Convert.ToDouble(colToSum.Sum()) / ((float)(intI - 165) / (float)intTradingDays));
                                else
                                    arrMTBS[intI] = 0;

                                arrINT[intI] = (float)arrTimeFrameYear.Skip(165).Average();
                            }

                            if (intI >= 423)
                            {
                                //Calculate Increase Price.
                                if (arrPrices[intI - intTradingDays] != 0)
                                    arrIncrease[intI] = (float)((arrPrices[intI] - arrPrices[intI - intTradingDays]) / arrPrices[intI - intTradingDays]);
                                else
                                    arrIncrease[intI] = 0;

                                //Calculate Increase PriceTrend.
                                if (arrEqAdjClosePrice[intI - intTradingDays] != 0)
                                    arrIncreaseTrend[intI] = (float)((arrEqAdjClosePrice[intI] - arrEqAdjClosePrice[intI - intTradingDays]) / arrEqAdjClosePrice[intI - intTradingDays]);
                                else
                                    arrIncreaseTrend[intI] = 0;
                            }

                            DateTime dtCurrDate = DateTime.FromOADate(arrDates[intI]);
                            if (!arrInOutState[intI].Equals(strPrevState, StringComparison.InvariantCultureIgnoreCase))
                            {
                                strPrevState = arrInOutState[intI];
                                dtCurrentState = dtCurrDate;
                            }

                            #endregion

                            if (intI == (objPriceList.Count() - 1))
                            {
                                int intAssetSetCount = objHEEntities.AssetBackTestSet.Where(x => x.AssetSymbolSet.Id.Equals(objAssetSymSet.Id)).Count();

                                //If record does not exists in the database then only make entry for the new record.
                                if (intAssetSetCount == 0)
                                {
                                    objAssetSet = new AssetBackTestSet();
                                    objAssetSet.Ending_Date = dtCurrDate;
                                    //objAssetSet.Starting_Date = GetEarliestTradingDate( objAssetSymSet.Symbol );
                                    objAssetSet.Starting_Date = DateTime.FromOADate(arrDates[149]);
                                    objAssetSet.Starting_Price = (float)arrPrices[149];
                                    objAssetSet.Ending_Price = (float)arrPrices[intI];
                                    objAssetSet.EndingPriceTrend = arrEqAdjClosePrice[intI];
                                    objAssetSet.StateDate = dtCurrentState;
                                    objAssetSet.AssetSymbol = objAssetSymSet.Symbol;
                                    //objAssetSet.CAR = ( float )arrCAGRSlope[ intI ];
                                    objAssetSet.CAGR = (float)arrCAGR[intI];
                                    objAssetSet.CAGRTrend = (float)arrCAGRTrend[intI];
                                    //objAssetSet.Symbol = objAssetSymSet.Symbol;
                                    objAssetSet.CurrentState = arrInOutFilterState[intI];
                                    objAssetSet.MTBS = arrMTBS[intI];
                                    objAssetSet.Stdev = (float)arrSTDevDb[intI];
                                    objAssetSet.StdevTrend = (float)arrSTDevTrend[intI];
                                    objAssetSet.Tbase = decThreshBase;
                                    objAssetSet.DividentSplit = 0;
                                    objAssetSet.INT = arrINT[intI];
                                    if (intI > 423)
                                    {
                                        objAssetSet.Increase = arrIncrease.Skip(423).Max();
                                        objAssetSet.IncreaseTrend = arrIncreaseTrend.Skip(423).Max();
                                        objAssetSet.DrawDown = arrIncrease.Skip(423).Min();
                                        objAssetSet.DrawdownTrend = arrIncreaseTrend.Skip(423).Min();
                                    }
                                    else
                                    {
                                        objAssetSet.Increase = null;
                                        objAssetSet.IncreaseTrend = null;
                                        objAssetSet.DrawDown = null;
                                        objAssetSet.DrawdownTrend = null;
                                    }
                                    objAssetSet.AssetSymbolSet = objHEEntities.AssetSymbolSet.FirstOrDefault(x => x.Id.Equals(objAssetSymSet.Id));
                                    objAssetSet.TrendEngineVariablesSet = objHEEntities.TrendEngineVariablesSet.FirstOrDefault(x => x.Id.Equals(objTrendEngine.Id));
                                    objHEEntities.AddToAssetBackTestSet(objAssetSet);
                                }
                                else  //Update the existing record.
                                {
                                    var existingAssetSetColl = objHEEntities.AssetBackTestSet.Where(x => x.AssetSymbolSet.Id.Equals(objAssetSymSet.Id));

                                    existingAssetSetColl.First(x => x.AssetSymbolSet.Id.Equals(objAssetSymSet.Id)).Ending_Price = (float)arrPrices[intI];
                                    existingAssetSetColl.First(x => x.AssetSymbolSet.Id.Equals(objAssetSymSet.Id)).EndingPriceTrend = arrEqAdjClosePrice[intI];
                                    existingAssetSetColl.First(x => x.AssetSymbolSet.Id.Equals(objAssetSymSet.Id)).Ending_Date = dtCurrDate;
                                    existingAssetSetColl.First(x => x.AssetSymbolSet.Id.Equals(objAssetSymSet.Id)).StateDate = dtCurrentState;
                                    existingAssetSetColl.First(x => x.AssetSymbolSet.Id.Equals(objAssetSymSet.Id)).AssetSymbol = objAssetSymSet.Symbol;
                                    //existingAssetSetColl.First( x => x.UpdateDate.Equals( dtCurrDate ) ).CAR = arrCAGRSlope[ intI ];
                                    existingAssetSetColl.First(x => x.AssetSymbolSet.Id.Equals(objAssetSymSet.Id)).CAGR = (float)arrCAGR[intI];
                                    existingAssetSetColl.First(x => x.AssetSymbolSet.Id.Equals(objAssetSymSet.Id)).CAGRTrend = (float)arrCAGRTrend[intI];
                                    //existingAssetSetColl.First( x => x.AssetSymbolSet.Id.Equals( objAssetSymSet.Id ) ).Symbol = objAssetSymSet.Symbol;
                                    existingAssetSetColl.First(x => x.AssetSymbolSet.Id.Equals(objAssetSymSet.Id)).CurrentState = arrInOutFilterState[intI];
                                    existingAssetSetColl.First(x => x.AssetSymbolSet.Id.Equals(objAssetSymSet.Id)).MTBS = arrMTBS[intI];
                                    existingAssetSetColl.First(x => x.AssetSymbolSet.Id.Equals(objAssetSymSet.Id)).Stdev = (float)arrSTDevDb[intI];
                                    existingAssetSetColl.First(x => x.AssetSymbolSet.Id.Equals(objAssetSymSet.Id)).StdevTrend = (float)arrSTDevTrend[intI];
                                    existingAssetSetColl.First(x => x.AssetSymbolSet.Id.Equals(objAssetSymSet.Id)).Tbase = decThreshBase;
                                    existingAssetSetColl.First(x => x.AssetSymbolSet.Id.Equals(objAssetSymSet.Id)).INT = arrINT[intI];
                                    existingAssetSetColl.First(x => x.AssetSymbolSet.Id.Equals(objAssetSymSet.Id)).DividentSplit = 0;
                                    if (objPriceList.Count() > 423)
                                    {
                                        existingAssetSetColl.First(x => x.AssetSymbolSet.Id.Equals(objAssetSymSet.Id)).Increase = arrIncrease.Skip(423).Max();
                                        existingAssetSetColl.First(x => x.AssetSymbolSet.Id.Equals(objAssetSymSet.Id)).IncreaseTrend = arrIncreaseTrend.Skip(423).Max();
                                        existingAssetSetColl.First(x => x.AssetSymbolSet.Id.Equals(objAssetSymSet.Id)).DrawDown = arrIncrease.Skip(423).Min();
                                        existingAssetSetColl.First(x => x.AssetSymbolSet.Id.Equals(objAssetSymSet.Id)).DrawdownTrend = arrIncreaseTrend.Skip(423).Min();
                                    }
                                    else
                                    {
                                        existingAssetSetColl.First(x => x.AssetSymbolSet.Id.Equals(objAssetSymSet.Id)).Increase = null;
                                        existingAssetSetColl.First(x => x.AssetSymbolSet.Id.Equals(objAssetSymSet.Id)).IncreaseTrend = null;
                                        existingAssetSetColl.First(x => x.AssetSymbolSet.Id.Equals(objAssetSymSet.Id)).DrawDown = null;
                                        existingAssetSetColl.First(x => x.AssetSymbolSet.Id.Equals(objAssetSymSet.Id)).DrawdownTrend = null;
                                    }
                                }
                            }
                            //Update AssetPriceBackTestSet record....
                            //objPriceList.First(x => x.Date.Equals(dtCurrDate)).PriceTrend = (decimal)arrEqAdjClosePrice[intI];
                            //objPriceList.First(x => x.Date.Equals(dtCurrDate)).MeanDeviation = (decimal)arrLMS[intI];
                            //objPriceList.First(x => x.Date.Equals(dtCurrDate)).Threshold = (decimal)arrUpperCAG[intI];
                            //objPriceList.First(x => x.Date.Equals(dtCurrDate)).State = arrInOutState[intI];
                        }
                    }
                    objHEEntities.SaveChanges();
                }
                if (isExportData)
                    ExportAssetPriceSetToExcel(strSymbol);
            }
            catch (Exception ex)
            {
                _errorMsg = ex.Message;
            }
        }

        /// <summary>
        /// This method is used to perform the Trend Engine calculations from transaction Start date .
        /// </summary>
        /// <param name="parCurrentDate"></param>
        /// <param name="strSymbol"></param>
         public float PerformHistoricalTrendEngineAnalysis(DateTime parCurrentDate, string strSymbol)
        {

            DateTime dtStart = parCurrentDate;
            DateTime dtEnd = parCurrentDate;
            float dblPriceMax = 0;
            bool isDateEqual = false;
            try
            {
                int intSumCount = -1;
                int intDSLPCount = 149;
                int intSTDevCount = 142;
                objHEEntities = new Harbor_EastEntities();
                var objAssetSymSet = objHEEntities.AssetSymbolSet.First(x => x.Symbol.Equals(strSymbol, StringComparison.InvariantCultureIgnoreCase));

                var objTrendEngine = objHEEntities.TrendEngineVariablesSet.First();
                float decSMATrend = (float)objTrendEngine.SMATrend;
                int intTradingDays = (int)objTrendEngine.YearTradingDays;
                float fltIncrement = (float)objTrendEngine.ThresholdIncrement;
                float fltMax = (float)objTrendEngine.ThresholdtMax;
                float fltMin = (float)objTrendEngine.ThresholdMin;
                float fltHysteresisOffset = (float)objTrendEngine.Hysteresis;
                float fltLoopValMax = (float)objTrendEngine.LoopValMax;
                float fltLoopValMin = (float)objTrendEngine.LoopValMin;
                float fltLoopValIncrement = (float)objTrendEngine.LoopValIncrement;

                int intStabilityWindowState = (int)objTrendEngine.WaitTrend;
                float fltThreshBase = (float)-0.000090;
                dtEnd = GetAssetLastTradingDate(objHEEntities.AssetPriceSet.Where(x => x.AssetSymbolSet.Id.Equals(objAssetSymSet.Id) && x.Price > 0).ToList());
                if (objAssetSymSet != null)
                {
                    objPriceList = objHEEntities.AssetPriceSet.Where(x => x.Date <= dtEnd && x.AssetSymbolSet.Id.Equals(objAssetSymSet.Id)).OrderBy(x => x.Date).ToList();

                    arrDates = objPriceList.Select(x => x.Date).ToArray().Select(x => x.ToOADate()).ToArray();
                    arrPrices = objPriceList.Select(x => x.Price).ToArray().Select(x => (double)x).ToArray();
                    arrGrowthVals = objPriceList.Select(x => x.Growth).ToArray().Select(x => (double)x).ToArray();
                    arrDailyChange = new float[objPriceList.Count()];
                    arrCompAvg = new float[objPriceList.Count()];
                    arrLMS = new float[objPriceList.Count()];
                    arrDSLP = new float[objPriceList.Count()];
                    //arrCAGRSlope = new float[ objPriceList.Count( ) ];
                    arrSTDev = new float[objPriceList.Count()];
                    arrUpperThreshold = new float[objPriceList.Count()];
                    arrLowerThreshold = new float[objPriceList.Count()];
                    arrUpperCAG = new float[objPriceList.Count()];
                    arrLowerCAG = new float[objPriceList.Count()];
                    arrInOutState = new string[objPriceList.Count()];
                    arrDayCount = new int[objPriceList.Count()];
                    arrInOutFilterState = new string[objPriceList.Count()];
                    arrEqAdjClosePrice = new float[objPriceList.Count()];
                    arrFilteredTrades = new int[objPriceList.Count()];
                    arrVolatility = new float[objPriceList.Count()];
                    arrCAGR = new float[objPriceList.Count()];
                    arrCAGRTrend = new float[objPriceList.Count()];
                    arrSTDevDb = new float[objPriceList.Count()];
                    arrSTDevTrend = new float[objPriceList.Count()];
                    arrMTBS = new float[objPriceList.Count()];
                    arrINT = new float[objPriceList.Count()];
                    arrIncrease = new float[objPriceList.Count()];
                    arrIncreaseTrend = new float[objPriceList.Count()];
                    arrTimeFrameYear = new int[objPriceList.Count()];

                    for (int intI = 0; intI < objPriceList.Count(); intI++)
                    {
                        dtStart = DateTime.FromOADate(arrDates[intI]);
                        //Calculate daily change.
                        if (intI > 0)
                        {
                            if (arrPrices[intI - 1] != 0)
                                arrDailyChange[intI] = (float)((arrPrices[intI] - arrPrices[intI - 1]) / arrPrices[intI - 1]);
                            else
                                arrDailyChange[intI] = 0;
                        }

                        if (intI >= 150)
                        {
                            intSumCount++;

                            //Calculate compound average.
                            arrCompAvg[intI] = ((decSMATrend * (float)arrPrices.Skip(intSumCount).Take(150).Average()) + ((float)arrPrices.Skip(intSumCount + 100).Take(50).Average())) / (decSMATrend + 1);

                            if (arrPrices[intI] != 0)
                                arrLMS[intI] = (float)(arrPrices[intI] - arrGrowthVals[intI]) / (float)arrPrices[intI];
                            else
                                arrLMS[intI] = 0;
                        }

                        if (intI == 163)
                        {
                            arrDayCount[intI] = 0;
                            arrInOutFilterState[intI] = "IN";
                            arrInOutState[intI] = "IN";
                            arrEqAdjClosePrice[intI] = (float)arrPrices[163];
                        }

                        if (intI == 163)
                        {
                            float dblTbase = 0;
                            for (dblTbase = (float)fltLoopValMin; fltThreshBase <= fltLoopValMax; fltThreshBase = fltThreshBase + (float)fltLoopValIncrement)
                            {

                                if (DateTime.FromOADate(arrDates[intI]).Equals(parCurrentDate))
                                {
                                    isDateEqual = true;
                                    break;
                                }

                                //Calculate Upper Threshold.
                                arrUpperThreshold[intI] = ((fltThreshBase + (arrLMS[intI] * fltIncrement)) > fltMax ? fltMax : (fltThreshBase + (arrLMS[intI] * fltIncrement)) < fltMin ? fltMin : (fltThreshBase + (arrLMS[intI] * fltIncrement)));

                                //Calculate Lower Threshold.
                                arrLowerThreshold[intI] = arrUpperThreshold[intI] - fltHysteresisOffset;

                                //Calculate UpperCAG.
                                arrUpperCAG[intI] = arrUpperThreshold[intI] * (float)intTradingDays;

                                //Calculate LowerCAG.
                                arrLowerCAG[intI] = arrLowerThreshold[intI] * (float)intTradingDays;

                                //Calculate IN/OUT State.
                                arrInOutState[intI] = (arrDSLP[intI] > arrUpperThreshold[intI] ? "IN" : "OUT");
                                if (intI == 163)
                                {
                                    arrDayCount[intI] = 1;
                                    arrInOutFilterState[intI] = "IN";
                                    arrEqAdjClosePrice[intI] = (float)arrPrices[162];
                                }
                                else
                                {
                                    //Calculate STDeviation.
                                    arrSTDev[intI] = (float)(CommonUtility.STDev(arrDailyChange.Skip(intI - 1 - 21).Take(21).Select(x => (double)x).ToArray()) * Math.Pow(intTradingDays, 0.5));

                                    //Calculate Day Count.
                                    arrDayCount[intI] = (arrInOutState[intI].Equals(arrInOutState[intI - 1]) ? arrDayCount[intI - 1] + 1 : 1);

                                    //Calculate IN/OUT Filter State.
                                    arrInOutFilterState[intI] = (arrDayCount[intI] > intStabilityWindowState ? arrInOutState[intI] : arrInOutFilterState[intI - 1]);

                                    //Calculate Filtered Trades.
                                    arrFilteredTrades[intI] = (arrInOutFilterState[intI].Equals(arrInOutFilterState[intI - 1]) ? 0 : 1);

                                    //Calculate Equivalent adjacent closing price.
                                    arrEqAdjClosePrice[intI] = (arrInOutFilterState[intI - 1].Equals("IN", StringComparison.InvariantCultureIgnoreCase) ? (arrEqAdjClosePrice[intI - 1] * (float)(1 + arrDailyChange[intI])) : (arrEqAdjClosePrice[intI - 1] * ((float)(1 + 0.025 / (float)intTradingDays))));
                                }
                                if (isDateEqual)
                                {
                                    break;
                                }

                                if ((double)arrEqAdjClosePrice[intI] > dblPriceMax)
                                {
                                    fltThreshBase = dblTbase;
                                    dblPriceMax = arrEqAdjClosePrice[intI];
                                }
                            }
                        }

                        if (intI >= 164)
                        {
                            intDSLPCount++;
                            intSTDevCount++;

                            //Calculate DSLP of last 15 days.
                            if (arrCompAvg[intI - 14] != 0)
                                arrDSLP[intI] = (float)(CommonUtility.Slope(arrCompAvg.Skip(intDSLPCount).Take(15).Select(x => (double)x).ToArray(), arrDates.Skip(intDSLPCount).Take(15).ToArray()) / (double)arrCompAvg[intI - 14]);
                            else
                                arrDSLP[intI] = 0;

                            //Calculate Slope CAGR.
                            //arrCAGRSlope[ intI ] = ( float )arrDSLP[ intI ] * ( float )intTradingDays;

                            //Calculate STDeviation.
                            arrSTDev[intI] = (float)(CommonUtility.STDev(arrDailyChange.Skip(intSTDevCount).Take(22).Select(x => (double)x).ToArray()) * Math.Pow(intTradingDays, 0.5));

                            //Calculate Upper Threshold.
                            arrUpperThreshold[intI] = ((fltThreshBase + (arrLMS[intI] * fltIncrement)) > fltMax ? fltMax : (fltThreshBase + (arrLMS[intI] * fltIncrement)) < fltMin ? fltMin : (fltThreshBase + (arrLMS[intI] * fltIncrement)));

                            //Calculate Lower Threshold.
                            arrLowerThreshold[intI] = arrUpperThreshold[intI] - fltHysteresisOffset;

                            //Calculate UpperCAG.
                            arrUpperCAG[intI] = arrUpperThreshold[intI] * (float)intTradingDays;

                            //Calculate LowerCAG.
                            arrLowerCAG[intI] = arrLowerThreshold[intI] * (float)intTradingDays;

                            //Calculate IN/OUT State.
                            arrInOutState[intI] = (arrDSLP[intI] > arrUpperThreshold[intI] ? "IN" : "OUT");

                            //Calculate Day Count.
                            arrDayCount[intI] = (arrInOutState[intI].Equals(arrInOutState[intI - 1]) ? arrDayCount[intI - 1] + 1 : 1);

                            //Calculate IN/OUT Filter State.
                            arrInOutFilterState[intI] = (arrDayCount[intI] > intStabilityWindowState ? arrInOutState[intI] : arrInOutFilterState[intI - 1]);

                            //Calculate Filtered Trades.
                            arrFilteredTrades[intI] = (arrInOutFilterState[intI].Equals(arrInOutFilterState[intI - 1]) ? 0 : 1);

                            //Calculate Equivalent adjacent closing price.
                            arrEqAdjClosePrice[intI] = (arrInOutFilterState[intI - 1].Equals("IN", StringComparison.InvariantCultureIgnoreCase) ? (arrEqAdjClosePrice[intI - 1] * (float)(1 + arrDailyChange[intI])) : (arrEqAdjClosePrice[intI - 1] * ((float)(1 + 0.025 / (float)intTradingDays))));
                        }
                    }

                    //for (int intI = 0; intI < 166; intI++)
                    //{
                    //    dtStart = DateTime.FromOADate(arrDates[intI]);

                    //    //Update AssetPriceBackTestSet record....
                    //    //objPriceList.First(x => x.Date.Equals(dtStart)).PriceTrend = (decimal)arrEqAdjClosePrice[intI];
                    //    //objPriceList.First(x => x.Date.Equals(dtStart)).MeanDeviation = (decimal)arrLMS[intI];
                    //    //objPriceList.First(x => x.Date.Equals(dtStart)).Threshold = (decimal)arrUpperCAG[intI];
                    //    //objPriceList.First(x => x.Date.Equals(dtStart)).State = arrInOutState[intI];
                    //}
                }
            }
            catch (Exception ex)
            {
                _errorMsg = ex.Message;
            }
            return dblPriceMax;
        }
        #endregion

    }
}
