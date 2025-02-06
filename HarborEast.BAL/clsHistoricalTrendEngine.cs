using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.Office.Interop.Excel;
using System.Reflection;
using System.Net;
using System.Web.UI.WebControls;
using System.Web.UI;
using System.IO;
using System.Data.Objects.DataClasses;
using log4net;
using log4net.Config;
using System.Collections;
using log4net.Appender;

namespace HarborEast.BAL
{
    public class clsHistoricalTrendEngine
    {
        Harbor_EastEntities objHEEntities = new Harbor_EastEntities();
        protected static readonly ILog log = LogManager.GetLogger("TElogger");
        //public static string filePath = "";

        #region variable declaration

        List<AssetPriceSet> objPriceList;
        double[] arrDates;
        double[] arrPrices;
        double[] arrGrowthVals;
        float[] arrDailyChange;
        //** dynamic wait trend change
        int[] arrDynamicDayCount;
        double[] arrPriceChange;
        //**
        int[] arrDayCount;        
        int[] arrFilteredTrades;
        int[] arrTimeFrameYear;
        float[] arrCompAvg;
        float[] arrLMS;
        float[] arrDSLP;
        //float[ ] arrCAGRSlope;
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
        #endregion



        public clsHistoricalTrendEngine(string parPath)
        {
            // filePath = parPath;
            log4net.Config.XmlConfigurator.Configure(new System.IO.FileInfo("log4net.xml"));

            foreach (var appender in LogManager.GetRepository().GetAppenders())
            {
                var fileAppender = appender as log4net.Appender.FileAppender;
                if (fileAppender != null)
                {
                    if (fileAppender.Name == "LogFileAppenderTE")
                    {
                        fileAppender.File = parPath;// ( "${LOCALAPPDATA}", dataDirectory);
                        fileAppender.ActivateOptions();
                    }
                }
            }

            //log4net.Appender.FileAppender FA = log4net.Appender.get
            //FA.File = HttpContext.Current.Server.MapPath("/Logs/log.txt");

        }

        //public static log4net.Appender.IAppender[] GetAllAppenders()
        //{
        //  ArrayList appenders = new ArrayList();

        //      log4net.Repository.Hierarchy.Hierarchy h =(log4net.Repository.Hierarchy.Hierarchy)log4net.LogManager.GetLoggerRepository();
        //      appenders.AddRange(h.Root.Appenders);

        //  foreach(log4net.Repository.Hierarchy.Logger logger in h.GetCurrentLoggers())
        //  {
        //    appenders.AddRange(logger.Appenders);
        //  }

        //  return (log4net.Appender.IAppender[])appenders.ToArray(typeof(log4net.Appender.IAppender));
        //}   

        #region Private Methods

        /// <summary>
        /// This method is used for the daily Trend engine calculations.
        /// </summary>
        /// <param name="parCurrentDate"></param>
        /// <param name="strSymbol"></param>
        /// <param name="trendEngineVariableSetID"></param>
        /// <param name="extTbase"></param>
        private void PerformDailyTrendEngineAnalysis(DateTime parCurrentDate, string strSymbol, int trendEngineVariableSetID, double extTbase)
        {
            DateTime dtStart = parCurrentDate;
            DateTime dtEnd = parCurrentDate;
            DateTime dtCurrentState = new DateTime();
            AssetSet objAssetSet = null;
            try
            {
                
                int intDSLPCount = 149;
                int intSTDevCount = 142;
                int intSumCount = -1;
                string strPrevState = string.Empty;
                objHEEntities = new Harbor_EastEntities();
                var objAssetSymSet = objHEEntities.AssetSymbolSet.First(x => x.Symbol.Equals(strSymbol, StringComparison.InvariantCultureIgnoreCase));
                
                //Change code to analyse schedular process timing
                //objAssetSymSet.TEStartTime = DateTime.Now;
                
                //Get the existing Trend Engine variable set instance and initialise all the variables with the database values.
                var objTrendEngine = objHEEntities.TrendEngineVariablesSet.First(x => x.Id.Equals(trendEngineVariableSetID));
                float decSMATrend = (float)objTrendEngine.SMATrend;
                int intTradingDays = (int)objTrendEngine.YearTradingDays;
                float fltIncrement = (float)objTrendEngine.ThresholdIncrement;
                float fltMax = (float)objTrendEngine.ThresholdtMax;
                float fltMin = (float)objTrendEngine.ThresholdMin;
                float fltHysteresisOffset = (float)objTrendEngine.Hysteresis;
                float fltLoopValMax = (float)objTrendEngine.LoopValMax;
                float fltLoopValMin = (float)objTrendEngine.LoopValMin;
                float fltLoopValIncrement = (float)objTrendEngine.LoopValIncrement;
                //** Dynamic wait Trend
                bool isDynamicWaitTrend =(bool) objTrendEngine.IsDynamicWaitTrend;
                //**
                int intStabilityWindowState = (int)objTrendEngine.WaitTrend;
                dtEnd = GetAssetLastTradingDate(objHEEntities.AssetPriceSet.Where(x => x.AssetSymbolSet.Id.Equals(objAssetSymSet.Id) && x.Price > 0).ToList());
                if (objAssetSymSet != null)
                {
                    objPriceList = objHEEntities.AssetPriceSet.Where(x => x.Date <= dtEnd && x.AssetSymbolSet.Id.Equals(objAssetSymSet.Id)).OrderBy(x => x.Date).ToList();

                    #region Initialize array`s.

                    arrDates = objPriceList.Select(x => x.Date).ToArray().Select(x => x.ToOADate()).ToArray();
                    arrPrices = objPriceList.Select(x => x.Price).ToArray().Select(x => (double)x).ToArray();
                    try
                    {
                        arrGrowthVals = objPriceList.Select(x => x.Growth).ToArray().Select(x => (double)x).ToArray();
                    }
                    catch (Exception ex)
                    {

                        //List<AssetPriceSet> objPriceListRecalcGrowth;

                        //objPriceListRecalcGrowth = objHEEntities.AssetPriceSet.Where(x => x.Date > dtEnd && x.AssetSymbolSet.Id.Equals(objAssetSymSet.Id)).OrderBy(x => x.Date).ToList();
                        //objPriceListRecalcGrowth = objPriceList.Take(objPriceList.Count - 65).ToList();

                        //double[] arrGrDate = new double[objPriceListRecalcGrowth.Count];
                        //double[] arrGrPrices = new double[objPriceListRecalcGrowth.Count];
                        //double[] arrGrFutureDates = new double[65];

                        //arrGrFutureDates = objPriceList.Skip(objPriceList.Count - 65).Select(x => x.Date).ToArray().Select(x => x.ToOADate()).ToArray();

                        //arrGrDate = objPriceListRecalcGrowth.Select(x => x.Date).ToArray().Select(x => x.ToOADate()).ToArray();

                        //arrGrPrices = objPriceListRecalcGrowth.Select(x => (double)x.Price).ToArray();

                        //double[] arrFutureGrowthVals = CommonUtility.Growth(arrGrDate,arrGrPrices); //, arrGrDate, arrGrFutureDates, true)).OfType<object>().ToArray().Select(x => Convert.ToDouble(x.ToString())).ToArray();

                        //for (int intIndex = 0; intIndex < arrGrFutureDates.Count(); intIndex++)
                        //{
                        //    //objAssetPriceSet = new AssetPriceSet();
                        //    DateTime dt = DateTime.FromOADate(arrGrFutureDates[intIndex]);
                        //    AssetPriceSet objAPS;
                        //    objAPS = objHEEntities.AssetPriceSet.Where(x => x.AssetSymbolSet.Symbol.Equals(strSymbol) && x.Date.Equals(dt)).ToList()[0];
                        //    objAPS.Growth = (decimal)arrFutureGrowthVals[intIndex];
                        //}
                        //objHEEntities.SaveChanges();
                        //objPriceList = objHEEntities.AssetPriceSet.Where(x => x.Date <= dtEnd && x.AssetSymbolSet.Id.Equals(objAssetSymSet.Id)).OrderBy(x => x.Date).ToList();
                        //arrDates = objPriceList.Select(x => x.Date).ToArray().Select(x => x.ToOADate()).ToArray();
                        //arrPrices = objPriceList.Select(x => x.Price).ToArray().Select(x => (double)x).ToArray();
                        //arrGrowthVals = objPriceList.Select(x => x.Growth).ToArray().Select(x => (double)x).ToArray();


                        
                        List<AssetPriceSet> objPriceListRecalcGrowth;

                        //objPriceListRecalcGrowth = objHEEntities.AssetPriceSet.Where(x => x.Date > dtEnd && x.AssetSymbolSet.Id.Equals(objAssetSymSet.Id)).OrderBy(x => x.Date).ToList();
                        objPriceListRecalcGrowth = objPriceList.Take(objPriceList.Count).ToList();

                        double[] arrGrDate = new double[objPriceListRecalcGrowth.Count];
                        double[] arrGrPrices = new double[objPriceListRecalcGrowth.Count];
                        double[] arrGrFutureDates = new double[65];

                        arrGrFutureDates = objPriceList.Select(x => x.Date).ToArray().Select(x => x.ToOADate()).ToArray();

                        arrGrDate = objPriceListRecalcGrowth.Select(x => x.Date).ToArray().Select(x => x.ToOADate()).ToArray();

                        arrGrPrices = objPriceList.Select(x => (double)x.Price).ToArray();

                        double[] arrFutureGrowthVals = CommonUtility.Growth(arrGrFutureDates,arrGrPrices, objAssetSymSet.Id);//arrGrPrices, arrGrDate, arrGrFutureDates, true)).OfType<object>().ToArray().Select(x => Convert.ToDouble(x.ToString())).ToArray();

                        for (int intIndex = 0; intIndex < arrGrFutureDates.Count(); intIndex++)
                        {
                            //objAssetPriceSet = new AssetPriceSet();
                            DateTime dt = DateTime.FromOADate(arrGrFutureDates[intIndex]);
                            AssetPriceSet objAPS;
                            objAPS = objHEEntities.AssetPriceSet.Where(x => x.AssetSymbolSet.Symbol.Equals(strSymbol) && x.Date.Equals(dt)).ToList()[0];
                            objAPS.Growth = (decimal)arrFutureGrowthVals[intIndex];
                        }
                        objHEEntities.SaveChanges();
                        objPriceList = objHEEntities.AssetPriceSet.Where(x => x.Date <= dtEnd && x.AssetSymbolSet.Id.Equals(objAssetSymSet.Id)).OrderBy(x => x.Date).ToList();
                        arrDates = objPriceList.Select(x => x.Date).ToArray().Select(x => x.ToOADate()).ToArray();
                        arrPrices = objPriceList.Select(x => x.Price).ToArray().Select(x => (double)x).ToArray();
                        arrGrowthVals = objPriceList.Select(x => x.Growth).ToArray().Select(x => (double)x).ToArray();
                    }
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
                    //** dynamic wait trend change
                    arrDynamicDayCount = new int[objPriceList.Count()];
                    arrPriceChange = new double[objPriceList.Count()];
                    //**
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

                    #endregion

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
                            // Dated 08-Dec-2010
                            if (intI == arrPrices.Length - 1)
                            {
                                double[] arrPricesDummy = new double[arrPrices.Length + 1];
                                arrPrices.CopyTo(arrPricesDummy, 0);
                                arrPricesDummy[arrPricesDummy.Length - 1] = arrPrices[arrPrices.Length - 1];
                                arrCompAvg[intI] = ((decSMATrend * (float)arrPrices.Skip(intSumCount + 1).Take(150).Average()) + ((float)arrPricesDummy.Skip(intSumCount + 101).Take(51).Average())) / (decSMATrend + 1);
                            }
                            else
                                arrCompAvg[intI] = ((decSMATrend * (float)arrPrices.Skip(intSumCount + 1).Take(150).Average()) + ((float)arrPrices.Skip(intSumCount + 101).Take(51).Average())) / (decSMATrend + 1);

                            if (arrPrices[intI] != 0)
                                arrLMS[intI] = (float)(arrPrices[intI] - arrGrowthVals[intI]) / (float)arrPrices[intI];
                            else
                                arrLMS[intI] = 0;
                        }

                        if (intI == 163)
                        {
                            arrDayCount[intI] = 0;
                            //** dynamic wait trend change
                            arrPriceChange[intI] = arrPrices[intI];
                            arrDynamicDayCount[intI] = 0;
                            //**
                            arrInOutFilterState[intI] = "IN";
                            arrInOutState[intI] = "IN";
                            arrEqAdjClosePrice[intI] = (float)arrPrices[163];
                            strPrevState = arrInOutFilterState[163];
                            dtCurrentState = DateTime.FromOADate(arrDates[163]);
                        }

                        if (intI >= 164)
                        {
                            #region Calculations

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
                            arrUpperThreshold[intI] = (((float)extTbase + (arrLMS[intI] * fltIncrement)) > fltMax ? fltMax : (((float)extTbase + (arrLMS[intI] * fltIncrement)) < fltMin ? fltMin : (float)extTbase + (arrLMS[intI] * fltIncrement)));

                            //Calculate Lower Threshold.
                            arrLowerThreshold[intI] = arrUpperThreshold[intI] - fltHysteresisOffset;

                            //Calculate UpperCAG.
                            arrUpperCAG[intI] = arrUpperThreshold[intI] * (float)intTradingDays;

                            //Calculate LowerCAG.
                            arrLowerCAG[intI] = arrLowerThreshold[intI] * (float)intTradingDays;

                            //Calculate IN/OUT State.
                            arrInOutState[intI] = (arrDSLP[intI] > arrUpperThreshold[intI] ? "IN" : "OUT");

                            //Calculate STDeviation.
                            //arrSTDev[intI] = (float)(CommonUtility.STDev(arrDailyChange.Skip(intI - 1 - 21).Take(21).Select(x => (double)x).ToArray()) * Math.Pow(intTradingDays, 0.5));

                            //Calculate Day Count.
                            // change 13-Dec-2010
                            if (intI == 164)
                                arrDayCount[intI] = 1;
                            else
                                arrDayCount[intI] = (arrInOutState[intI].Equals(arrInOutState[intI - 1], StringComparison.InvariantCultureIgnoreCase) ? arrDayCount[intI - 1] + 1 : 1);
                            //arrDayCount[intI] = (arrInOutState[intI].Equals(arrInOutState[intI - 1], StringComparison.InvariantCultureIgnoreCase) ? arrDayCount[intI - 1] + 1 : 1);

                            //** dynamic wait trend change
                            if(isDynamicWaitTrend)
                            {
                                arrPriceChange[intI] = (arrInOutState[intI].Equals(arrInOutState[intI - 1], StringComparison.InvariantCultureIgnoreCase) ? arrPriceChange[intI - 1] : arrPrices[intI]);

                                if (arrInOutState[intI].Equals(arrInOutState[intI - 1], StringComparison.InvariantCultureIgnoreCase))
                                {
                                    if (arrInOutState[intI].Equals("OUT", StringComparison.InvariantCultureIgnoreCase))
                                    {
                                        if (arrPrices[intI] > arrPriceChange[intI])
                                        {
                                            arrDynamicDayCount[intI] = 1;
                                        }
                                        else
                                        {
                                            arrDynamicDayCount[intI] = arrDynamicDayCount[intI - 1] + 1;
                                        }
                                    }
                                    else
                                    {
                                        arrDynamicDayCount[intI] = arrDynamicDayCount[intI - 1] + 1;
                                    }
                                }
                                else
                                {
                                    arrDynamicDayCount[intI] = 1;
                                }
                                //**

                                
                                //** dynamic wait trend change
                                arrInOutFilterState[intI] = (arrDynamicDayCount[intI] > intStabilityWindowState ? arrInOutState[intI] : arrInOutFilterState[intI - 1]);
                                //**

                            }
                            else
                            {
                                //Calculate IN/OUT Filter State.
                                arrInOutFilterState[intI] = (arrDayCount[intI] > intStabilityWindowState ? arrInOutState[intI] : arrInOutFilterState[intI - 1]);
                            }
                            
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
                                //change 17-Mar-2011 gain/yr
                                if (arrPrices[163] != 0)
                                    arrCAGR[intI] = (float)(Math.Pow((arrPrices[intI] / arrPrices[163]), (float)(1 / ((float)(intI - 163) / intTradingDays))) - 1.0);
                                else
                                    arrCAGR[intI] = 0;

                                //Calculate Price Trend.
                                if (arrEqAdjClosePrice[163] != 0)
                                    arrCAGRTrend[intI] = (float)(Math.Pow((arrEqAdjClosePrice[intI] / arrEqAdjClosePrice[163]), (float)(1 / ((float)(intI - 163) / (float)intTradingDays))) - 1.0);
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
                            if (!arrInOutFilterState[intI].Equals(strPrevState, StringComparison.InvariantCultureIgnoreCase))
                            {
                                strPrevState = arrInOutFilterState[intI];
                                dtCurrentState = dtCurrDate;
                            }

                            #endregion

                            #region Store data into the database.

                            if (intI == (objPriceList.Count() - 1))
                            {
                                int intAssetSetCount = objHEEntities.AssetSet.Where(x => x.AssetSymbolSet.Id.Equals(objAssetSymSet.Id)).Count();

                                //If record does not exists in the database then only make entry for the new record.
                                if (intAssetSetCount == 0)
                                {
                                    objAssetSet = new AssetSet();
                                    objAssetSet.Ending_Date = dtCurrDate;
                                    //objAssetSet.Starting_Date = GetEarliestTradingDate( objAssetSymSet.Symbol );
                                    objAssetSet.Starting_Date = DateTime.FromOADate(arrDates[163]);
                                    objAssetSet.Starting_Price = (float)arrPrices[163];
                                    objAssetSet.Ending_Price = (float)arrPrices[intI];
                                    objAssetSet.EndingPriceTrend = arrEqAdjClosePrice[intI];
                                    objAssetSet.StateDate = dtCurrentState;
                                    objAssetSet.AssetSymbol = objAssetSymSet.Symbol;
                                    if (arrInOutState[intI].Equals(arrInOutFilterState[intI]))
                                        objAssetSet.isWindowStable = true;
                                    else
                                        objAssetSet.isWindowStable = false;
                                    //objAssetSet.CAR = ( float )arrCAGRSlope[ intI ];
                                    objAssetSet.CAGR = (float)arrCAGR[intI];
                                    objAssetSet.CAGRTrend = (float)arrCAGRTrend[intI];
                                    //objAssetSet.Symbol = objAssetSymSet.Symbol;
                                    objAssetSet.CurrentState = arrInOutFilterState[intI];
                                    if (arrInOutState[intI].ToLower() == "in")
                                    {
                                        if (arrInOutFilterState[intI].ToLower() == "in")
                                        {
                                            objAssetSet.StateColor = "G";
                                        }
                                        else
                                        {
                                            objAssetSet.StateColor = "R.Y";
                                        }
                                    }
                                    else
                                    {
                                        if (arrInOutFilterState[intI].ToLower() == "out")
                                        {
                                            objAssetSet.StateColor = "R";
                                        }
                                        else
                                        {
                                            objAssetSet.StateColor = "G.Y";
                                        }

                                    }

                                    objAssetSet.MTBS = arrMTBS[intI];
                                    objAssetSet.Stdev = (float)arrSTDevDb[intI];
                                    objAssetSet.StdevTrend = (float)arrSTDevTrend[intI];
                                    objAssetSet.Tbase = extTbase;
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
                                    objAssetSet.TrendEngineVariablesSet = objHEEntities.TrendEngineVariablesSet.FirstOrDefault(x => x.Id.Equals(trendEngineVariableSetID));
                                    objHEEntities.AddToAssetSet(objAssetSet);
                                }
                                else  //Update the existing record.
                                {
                                    var existingAssetSetColl = objHEEntities.AssetSet.Where(x => x.AssetSymbolSet.Id.Equals(objAssetSymSet.Id));

                                    existingAssetSetColl.First(x => x.AssetSymbolSet.Id.Equals(objAssetSymSet.Id)).Ending_Price = (float)arrPrices[intI];
                                    existingAssetSetColl.First(x => x.AssetSymbolSet.Id.Equals(objAssetSymSet.Id)).EndingPriceTrend = arrEqAdjClosePrice[intI];
                                    existingAssetSetColl.First(x => x.AssetSymbolSet.Id.Equals(objAssetSymSet.Id)).Ending_Date = dtCurrDate;
                                    existingAssetSetColl.First(x => x.AssetSymbolSet.Id.Equals(objAssetSymSet.Id)).Starting_Date = DateTime.FromOADate(arrDates[163]);
                                    existingAssetSetColl.First(x => x.AssetSymbolSet.Id.Equals(objAssetSymSet.Id)).Starting_Price = (float)arrPrices[163];
                                    existingAssetSetColl.First(x => x.AssetSymbolSet.Id.Equals(objAssetSymSet.Id)).StateDate = dtCurrentState;
                                    existingAssetSetColl.First(x => x.AssetSymbolSet.Id.Equals(objAssetSymSet.Id)).AssetSymbol = objAssetSymSet.Symbol;
                                    if (arrInOutState[intI].Equals(arrInOutFilterState[intI]))
                                        existingAssetSetColl.First(x => x.AssetSymbolSet.Id.Equals(objAssetSymSet.Id)).isWindowStable = true;
                                    else
                                        existingAssetSetColl.First(x => x.AssetSymbolSet.Id.Equals(objAssetSymSet.Id)).isWindowStable = false;

                                    existingAssetSetColl.First(x => x.AssetSymbolSet.Id.Equals(objAssetSymSet.Id)).CAGR = (float)arrCAGR[intI];
                                    existingAssetSetColl.First(x => x.AssetSymbolSet.Id.Equals(objAssetSymSet.Id)).CAGRTrend = (float)arrCAGRTrend[intI];
                                    //existingAssetSetColl.First( x => x.AssetSymbolSet.Id.Equals( objAssetSymSet.Id ) ).Symbol = objAssetSymSet.Symbol;
                                    existingAssetSetColl.First(x => x.AssetSymbolSet.Id.Equals(objAssetSymSet.Id)).CurrentState = arrInOutFilterState[intI];

                                    if (arrInOutState[intI].ToLower() == "in")
                                    {
                                        if (arrInOutFilterState[intI].ToLower() == "in")
                                        {
                                            existingAssetSetColl.First(x => x.AssetSymbolSet.Id.Equals(objAssetSymSet.Id)).StateColor = "G";

                                        }
                                        else
                                        {
                                            existingAssetSetColl.First(x => x.AssetSymbolSet.Id.Equals(objAssetSymSet.Id)).StateColor = "R.Y";
                                        }
                                    }
                                    else
                                    {
                                        if (arrInOutFilterState[intI].ToLower() == "out")
                                        {
                                            existingAssetSetColl.First(x => x.AssetSymbolSet.Id.Equals(objAssetSymSet.Id)).StateColor = "R";
                                        }
                                        else
                                        {
                                            existingAssetSetColl.First(x => x.AssetSymbolSet.Id.Equals(objAssetSymSet.Id)).StateColor = "G.Y";
                                        }

                                    }

                                    existingAssetSetColl.First(x => x.AssetSymbolSet.Id.Equals(objAssetSymSet.Id)).MTBS = arrMTBS[intI];
                                    existingAssetSetColl.First(x => x.AssetSymbolSet.Id.Equals(objAssetSymSet.Id)).Stdev = (float)arrSTDevDb[intI];
                                    existingAssetSetColl.First(x => x.AssetSymbolSet.Id.Equals(objAssetSymSet.Id)).StdevTrend = (float)arrSTDevTrend[intI];
                                    existingAssetSetColl.First(x => x.AssetSymbolSet.Id.Equals(objAssetSymSet.Id)).Tbase = extTbase;
                                    existingAssetSetColl.First(x => x.AssetSymbolSet.Id.Equals(objAssetSymSet.Id)).INT = arrINT[intI];
                                    existingAssetSetColl.First(x => x.AssetSymbolSet.Id.Equals(objAssetSymSet.Id)).DividentSplit = 0;
                                    existingAssetSetColl.First(x => x.AssetSymbolSet.Id.Equals(objAssetSymSet.Id)).TrendEngineVariablesSet = objHEEntities.TrendEngineVariablesSet.FirstOrDefault(x => x.Id.Equals(trendEngineVariableSetID));
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
                            //Update AssetPriceSet record....
                            objPriceList.First(x => x.Date.Equals(dtCurrDate)).PriceTrend = (decimal)arrEqAdjClosePrice[intI];
                            objPriceList.First(x => x.Date.Equals(dtCurrDate)).MeanDeviation = (decimal)arrLMS[intI];
                            objPriceList.First(x => x.Date.Equals(dtCurrDate)).Threshold = (decimal)arrUpperCAG[intI];
                            objPriceList.First(x => x.Date.Equals(dtCurrDate)).State = arrInOutState[intI];
                            objPriceList.First(x => x.Date.Equals(dtCurrDate)).FilterState = arrInOutFilterState[intI];

                            #endregion
                        }
                    }
                    //Change code for schedular timing
                  //  objAssetSymSet.TEEndTime = DateTime.Now;
                    objHEEntities.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                _errorMsg = ex.Message;
                log.Error(strSymbol + "\t" + ex.Message);
                return;

            }
        }

        /// <summary>
        /// This method is used to perform the Trend Engine calculations.
        /// </summary>
        /// <param name="trendEngineVariableSetID"></param>
        /// <param name="dtStart"></param>
        /// <param name="objAssetSymSet"></param>
        /// <param name="isHistorical"></param>
        /// <param name="fltLoopValIncrement"></param>
        /// <param name="fltLoopValMax"></param>
        /// <param name="fltLoopValMin"></param>
        /// <param name="fltIncrement"></param>
        /// <param name="fltMax"></param>
        /// <param name="fltMin"></param>
        /// <param name="intTradingDays"></param>
        /// <param name="intStabilityWindowState"></param>
        /// <param name="decHysteresisOffset"></param>
        /// <returns></returns>
        private float PerformTrendEngineAnalysis(int trendEngineVariableSetID, DateTime dtStart, AssetSymbolSet objAssetSymSet, bool isHistorical, float fltLoopValIncrement, float fltLoopValMax, float fltLoopValMin, float fltIncrement, float fltMax, float fltMin, int intTradingDays, int intStabilityWindowState, float decHysteresisOffset)
        {
            try
            {
                var extAssetObj = objHEEntities.AssetPriceSet.FirstOrDefault(x => x.Date.Equals(dtStart) && x.AssetSymbolSet.Id.Equals(objAssetSymSet.Id));
                bool isDynamicWaitTrend =(bool)objHEEntities.TrendEngineVariablesSet.FirstOrDefault(x => x.Id.Equals(trendEngineVariableSetID)).IsDynamicWaitTrend;
                float fltMaxPriceTrend;
                float decThreshBase = 0;
                float fltOptimalTbase = 0;
                AssetSet objAssetSet = null;
                string strPrevState = string.Empty;
                DateTime dtCurrentState;

                if (arrPrices.Length >= 166)
                {
                    if (arrEqAdjClosePrice == null)
                    {
                        throw new Exception("Growth value is null.");
                    }
                    else
                    {
                        fltMaxPriceTrend = arrEqAdjClosePrice[arrEqAdjClosePrice.Count() - 1];
                    }
                    //Calculate the optimal Tbase.
                    //List<double> arrMaxClPrice = new List<double>();
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

                            //** dynamic wait trend change
                            if (isDynamicWaitTrend)
                            {
                                arrPriceChange[intI] = (arrInOutState[intI].Equals(arrInOutState[intI - 1], StringComparison.InvariantCultureIgnoreCase) ? arrPriceChange[intI - 1] : arrPrices[intI]);

                                if (arrInOutState[intI].Equals(arrInOutState[intI - 1], StringComparison.InvariantCultureIgnoreCase))
                                {
                                    if (arrInOutState[intI].Equals("OUT", StringComparison.InvariantCultureIgnoreCase))
                                    {
                                        if (arrPrices[intI] > arrPriceChange[intI])
                                        {
                                            arrDynamicDayCount[intI] = 1;
                                        }
                                        else
                                        {
                                            arrDynamicDayCount[intI] = arrDynamicDayCount[intI - 1] + 1;
                                        }
                                    }
                                    else
                                    {
                                        arrDynamicDayCount[intI] = arrDynamicDayCount[intI - 1] + 1;
                                    }
                                }
                                else
                                {
                                    arrDynamicDayCount[intI] = 1;
                                }
                                //**

                                //** dynamic wait trend change
                                arrInOutFilterState[intI] = (arrDynamicDayCount[intI] > intStabilityWindowState ? arrInOutState[intI] : arrInOutFilterState[intI - 1]);
                                //**
                            }
                            else
                            {
                                //Calculate IN/OUT Filter State.
                                arrInOutFilterState[intI] = (arrDayCount[intI] > intStabilityWindowState ? arrInOutState[intI] : arrInOutFilterState[intI - 1]);
                            }
                            //Calculate Filtered Trades.
                            //arrFilteredTrades[ intI ] = ( arrInOutFilterState[ intI ].Equals( arrInOutFilterState[ intI - 1 ] ) ? 0 : 1 );

                            //Calculate Equivalent adjacent closing price.
                            arrEqAdjClosePrice[intI] = (arrInOutFilterState[intI - 1].Equals("IN", StringComparison.InvariantCultureIgnoreCase) ? (arrEqAdjClosePrice[intI - 1] * (float)(1 + arrDailyChange[intI])) : (arrEqAdjClosePrice[intI - 1] * ((float)(1 + 0.025 / (float)intTradingDays))));
                        }
                        //arrMaxClPrice.Add((double)arrEqAdjClosePrice[arrEqAdjClosePrice.Count() - 1]);
                        if (arrEqAdjClosePrice[arrEqAdjClosePrice.Count() - 1] > fltMaxPriceTrend)
                        {
                            fltMaxPriceTrend = arrEqAdjClosePrice[arrEqAdjClosePrice.Count() - 1];
                            fltOptimalTbase = fltTbase;
                        }
                    }
                    //Initialize the variables for default values when the count is 163.
                    arrDayCount[163] = 0;
                    //** dynamic wait trend change
                    arrDynamicDayCount[163] = 0;
                    arrPriceChange[163] = arrPrices[163];
                    //**
                    arrInOutFilterState[163] = "IN";
                    arrInOutState[163] = "IN";
                    //change 13-Dec-2010
                    arrEqAdjClosePrice[163] = (float)arrPrices[163];
                    strPrevState = arrInOutFilterState[163];
                    dtCurrentState = DateTime.FromOADate(arrDates[163]);

                    //HttpContext.Current.Response.Write("Tbase Optimal Calculation ends at :" + DateTime.Now);
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
                        //change 13-Dec-2010
                        //arrSTDev[intI] = (float)(CommonUtility.STDev(arrDailyChange.Skip(intI - 1 - 21).Take(21).Select(x => (double)x).ToArray()) * Math.Pow(intTradingDays, 0.5));
                        arrSTDev[intI] = (float)(CommonUtility.STDev(arrDailyChange.Skip(intI  - 21).Take(22).Select(x => (double)x).ToArray()) * Math.Pow(intTradingDays, 0.5));

                        //Calculate Day Count.
                        //change 13-Dec-2010
                        if (intI == 164)
                            arrDayCount[intI] = 1;
                        else
                            arrDayCount[intI] = (arrInOutState[intI].Equals(arrInOutState[intI - 1], StringComparison.InvariantCultureIgnoreCase) ? arrDayCount[intI - 1] + 1 : 1);

                        //arrDayCount[intI] = (arrInOutState[intI].Equals(arrInOutState[intI - 1], StringComparison.InvariantCultureIgnoreCase) ? arrDayCount[intI - 1] + 1 : 1);

                        //** dynamic wait trend change
                        if (isDynamicWaitTrend)
                        {
                            arrPriceChange[intI] = (arrInOutState[intI].Equals(arrInOutState[intI - 1], StringComparison.InvariantCultureIgnoreCase) ? arrPriceChange[intI - 1] : arrPrices[intI]);

                            if (arrInOutState[intI].Equals(arrInOutState[intI - 1], StringComparison.InvariantCultureIgnoreCase))
                            {
                                if (arrInOutState[intI].Equals("OUT", StringComparison.InvariantCultureIgnoreCase))
                                {
                                    if (arrPrices[intI] > arrPriceChange[intI])
                                    {
                                        arrDynamicDayCount[intI] = 1;
                                    }
                                    else
                                    {
                                        arrDynamicDayCount[intI] = arrDynamicDayCount[intI - 1] + 1;
                                    }
                                }
                                else
                                {
                                    arrDynamicDayCount[intI] = arrDynamicDayCount[intI - 1] + 1;
                                }
                            }
                            else
                            {
                                arrDynamicDayCount[intI] = 1;
                            }
                            //**

                            
                            //** dynamic wait trend change
                            arrInOutFilterState[intI] = (arrDynamicDayCount[intI] > intStabilityWindowState ? arrInOutState[intI] : arrInOutFilterState[intI - 1]);
                            //**
                        }
                        else
                        {
                            //Calculate IN/OUT Filter State.
                            arrInOutFilterState[intI] = (arrDayCount[intI] > intStabilityWindowState ? arrInOutState[intI] : arrInOutFilterState[intI - 1]);
                        }
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
                            //change 17-Mar-2011 gain/yr
                            if (arrPrices[163] != 0)
                                arrCAGR[intI] = (float)(Math.Pow((arrPrices[intI] / arrPrices[163]), (float)(1 / ((float)(intI - 163) / intTradingDays))) - 1.0);
                            else
                                arrCAGR[intI] = 0;

                            //Calculate Price Trend.
                            //change 17-Mar-2011 gain/yr
                            if (arrEqAdjClosePrice[163] != 0)
                                arrCAGRTrend[intI] = (float)(Math.Pow((arrEqAdjClosePrice[intI] / arrEqAdjClosePrice[163]), (float)(1 / ((float)(intI - 163) / (float)intTradingDays))) - 1.0);
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
                        //StateDate based on arrInOutFilterState
                        if (!arrInOutFilterState[intI].Equals(strPrevState, StringComparison.InvariantCultureIgnoreCase))
                        {
                            strPrevState = arrInOutFilterState[intI];
                            dtCurrentState = dtCurrDate;
                        }

                        #endregion

                        #region store results into the database

                        if (!isHistorical)
                        {
                            if (intI == (objPriceList.Count() - 1))
                            {
                                int intAssetSetCount = objHEEntities.AssetSet.Where(x => x.AssetSymbolSet.Id.Equals(objAssetSymSet.Id)).Count();

                                //If record does not exists in the database then only make entry for the new record.
                                if (intAssetSetCount == 0)
                                {
                                    objAssetSet = new AssetSet();
                                    objAssetSet.Ending_Date = dtCurrDate;
                                    //objAssetSet.Starting_Date = GetEarliestTradingDate( objAssetSymSet.Symbol );
                                    objAssetSet.Starting_Date = DateTime.FromOADate(arrDates[163]);
                                    objAssetSet.Starting_Price = (float)arrPrices[163];
                                    objAssetSet.Ending_Price = (float)arrPrices[intI];
                                    objAssetSet.EndingPriceTrend = arrEqAdjClosePrice[intI];
                                    objAssetSet.StateDate = dtCurrentState;
                                    objAssetSet.AssetSymbol = objAssetSymSet.Symbol;
                                    //objAssetSet.CAR = ( float )arrCAGRSlope[ intI ];
                                    objAssetSet.CAGR = (float)arrCAGR[intI];
                                    if (arrInOutState[intI].Equals(arrInOutFilterState[intI]))
                                        objAssetSet.isWindowStable = true;
                                    else
                                        objAssetSet.isWindowStable = false;

                                    objAssetSet.CAGRTrend = (float)arrCAGRTrend[intI];
                                    //objAssetSet.Symbol = objAssetSymSet.Symbol;
                                    objAssetSet.CurrentState = arrInOutFilterState[intI];
                                    if (arrInOutState[intI].ToLower() == "in")
                                    {
                                        if (arrInOutFilterState[intI].ToLower() == "in")
                                        {
                                            objAssetSet.StateColor = "G";
                                        }
                                        else
                                        {
                                            objAssetSet.StateColor = "R.Y";
                                        }
                                    }
                                    else
                                    {
                                        if (arrInOutFilterState[intI].ToLower() == "out")
                                        {
                                            objAssetSet.StateColor = "R";
                                        }
                                        else
                                        {
                                            objAssetSet.StateColor = "G.Y";
                                        }

                                    }
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
                                    objAssetSet.TrendEngineVariablesSet = objHEEntities.TrendEngineVariablesSet.FirstOrDefault(x => x.Id.Equals(trendEngineVariableSetID));
                                    objHEEntities.AddToAssetSet(objAssetSet);
                                }
                                else  //Update the existing record.
                                {
                                    var existingAssetSetColl = objHEEntities.AssetSet.Where(x => x.AssetSymbolSet.Id.Equals(objAssetSymSet.Id));

                                    existingAssetSetColl.First(x => x.AssetSymbolSet.Id.Equals(objAssetSymSet.Id)).Ending_Price = (float)arrPrices[intI];
                                    existingAssetSetColl.First(x => x.AssetSymbolSet.Id.Equals(objAssetSymSet.Id)).EndingPriceTrend = arrEqAdjClosePrice[intI];
                                    existingAssetSetColl.First(x => x.AssetSymbolSet.Id.Equals(objAssetSymSet.Id)).Ending_Date = dtCurrDate;
                                    existingAssetSetColl.First(x => x.AssetSymbolSet.Id.Equals(objAssetSymSet.Id)).Starting_Date = DateTime.FromOADate(arrDates[163]);
                                    existingAssetSetColl.First(x => x.AssetSymbolSet.Id.Equals(objAssetSymSet.Id)).Starting_Price = (float)arrPrices[163];
                                    existingAssetSetColl.First(x => x.AssetSymbolSet.Id.Equals(objAssetSymSet.Id)).StateDate = dtCurrentState;
                                    existingAssetSetColl.First(x => x.AssetSymbolSet.Id.Equals(objAssetSymSet.Id)).AssetSymbol = objAssetSymSet.Symbol;
                                    //existingAssetSetColl.First( x => x.UpdateDate.Equals( dtCurrDate ) ).CAR = arrCAGRSlope[ intI ];
                                    existingAssetSetColl.First(x => x.AssetSymbolSet.Id.Equals(objAssetSymSet.Id)).CAGR = (float)arrCAGR[intI];
                                    existingAssetSetColl.First(x => x.AssetSymbolSet.Id.Equals(objAssetSymSet.Id)).CAGRTrend = (float)arrCAGRTrend[intI];

                                    if (arrInOutState[intI].Equals(arrInOutFilterState[intI]))
                                        existingAssetSetColl.First(x => x.AssetSymbolSet.Id.Equals(objAssetSymSet.Id)).isWindowStable = true;
                                    else
                                        existingAssetSetColl.First(x => x.AssetSymbolSet.Id.Equals(objAssetSymSet.Id)).isWindowStable = false;

                                    //existingAssetSetColl.First( x => x.AssetSymbolSet.Id.Equals( objAssetSymSet.Id ) ).Symbol = objAssetSymSet.Symbol;
                                    existingAssetSetColl.First(x => x.AssetSymbolSet.Id.Equals(objAssetSymSet.Id)).CurrentState = arrInOutFilterState[intI];

                                    if (arrInOutState[intI].ToLower() == "in")
                                    {
                                        if (arrInOutFilterState[intI].ToLower() == "in")
                                        {
                                            existingAssetSetColl.First(x => x.AssetSymbolSet.Id.Equals(objAssetSymSet.Id)).StateColor = "G";
                                        }
                                        else
                                        {
                                            existingAssetSetColl.First(x => x.AssetSymbolSet.Id.Equals(objAssetSymSet.Id)).StateColor = "R.Y";
                                        }
                                    }
                                    else
                                    {
                                        if (arrInOutFilterState[intI].ToLower() == "out")
                                        {
                                            existingAssetSetColl.First(x => x.AssetSymbolSet.Id.Equals(objAssetSymSet.Id)).StateColor = "R";
                                        }
                                        else
                                        {
                                            existingAssetSetColl.First(x => x.AssetSymbolSet.Id.Equals(objAssetSymSet.Id)).StateColor = "G.Y";
                                        }

                                    }

                                    existingAssetSetColl.First(x => x.AssetSymbolSet.Id.Equals(objAssetSymSet.Id)).MTBS = arrMTBS[intI];
                                    existingAssetSetColl.First(x => x.AssetSymbolSet.Id.Equals(objAssetSymSet.Id)).Stdev = (float)arrSTDevDb[intI];
                                    existingAssetSetColl.First(x => x.AssetSymbolSet.Id.Equals(objAssetSymSet.Id)).StdevTrend = (float)arrSTDevTrend[intI];
                                    existingAssetSetColl.First(x => x.AssetSymbolSet.Id.Equals(objAssetSymSet.Id)).Tbase = decThreshBase;
                                    existingAssetSetColl.First(x => x.AssetSymbolSet.Id.Equals(objAssetSymSet.Id)).INT = arrINT[intI];
                                    existingAssetSetColl.First(x => x.AssetSymbolSet.Id.Equals(objAssetSymSet.Id)).DividentSplit = 0;
                                    existingAssetSetColl.First(x => x.AssetSymbolSet.Id.Equals(objAssetSymSet.Id)).TrendEngineVariablesSet = objHEEntities.TrendEngineVariablesSet.FirstOrDefault(x => x.Id.Equals(trendEngineVariableSetID));
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
                            //Update AssetPriceSet record....
                            objPriceList.First(x => x.Date.Equals(dtCurrDate)).PriceTrend = (decimal)arrEqAdjClosePrice[intI];
                            objPriceList.First(x => x.Date.Equals(dtCurrDate)).MeanDeviation = (decimal)arrLMS[intI];
                            objPriceList.First(x => x.Date.Equals(dtCurrDate)).Threshold = (decimal)arrUpperCAG[intI];
                            objPriceList.First(x => x.Date.Equals(dtCurrDate)).State = arrInOutState[intI];
                            //change 13-Dec-2010
                        
                            objPriceList.First(x => x.Date.Equals(dtCurrDate)).FilterState = arrInOutFilterState[intI];
                        }
                        else
                        {
                            //objPriceList.First(x => x.Date.Equals(dtCurrDate)).PriceTrend = (decimal)arrEqAdjClosePrice[intI];
                            //objPriceList.First(x => x.Date.Equals(dtCurrDate)).MeanDeviation = (decimal)arrLMS[intI];
                            //objPriceList.First(x => x.Date.Equals(dtCurrDate)).Threshold = (decimal)arrUpperCAG[intI];
                            //objPriceList.First(x => x.Date.Equals(dtCurrDate)).State = arrInOutState[intI];
                        }


                        #endregion
                    }
                }
                objHEEntities.SaveChanges();
            }
            catch (Exception ex)
            {

                log.Error(objAssetSymSet.Symbol + "\t" + ex.Message);
                return 0;
            }
            if (arrCAGRTrend.Count() > 0)
                return (arrCAGRTrend[arrCAGRTrend.Count() - 1]);
            else
                return 0;

        }

        /// <summary>
        /// This method is used to return a recent trading date from the AssetSet collection passed as a parameter.
        /// </summary>
        /// <param name="lstAssetSet">list collection of all the AssetSet associated with the current AssetSymbolID.</param>
        /// <returns>returns the recently published trading date from the AssetSet collection.</returns>
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
        /// This method is used for calculating the values for the historical data.
        /// </summary>
        /// <param name="parCurrentDate"></param>
        /// <param name="strSymbol"></param>
        /// <param name="trendEngineVariableSetID"></param>
        public void PerformHistoricalTrendEngineAnalysis(DateTime parCurrentDate, string strSymbol, int trendEngineVariableSetID)
        {
            DateTime dtStart = parCurrentDate;
            DateTime dtEnd = parCurrentDate;
            try
            {
                int intSumCount = -1;
                int intDSLPCount = 149;
                int intSTDevCount = 142;
                objHEEntities = new Harbor_EastEntities();
                var objAssetSymSet = objHEEntities.AssetSymbolSet.First(x => x.Symbol.Equals(strSymbol, StringComparison.InvariantCultureIgnoreCase));


                //Get the values for the passed trend engine variable set ID.
                var objTrendEngine = objHEEntities.TrendEngineVariablesSet.First(x => x.Id.Equals(trendEngineVariableSetID));
                float decSMATrend = (float)objTrendEngine.SMATrend;
                int intTradingDays = (int)objTrendEngine.YearTradingDays;
                float fltIncrement = (float)objTrendEngine.ThresholdIncrement;
                float fltMax = (float)objTrendEngine.ThresholdtMax;
                float fltMin = (float)objTrendEngine.ThresholdMin;
                float fltHysteresisOffset = (float)objTrendEngine.Hysteresis;
                float fltLoopValMax = (float)objTrendEngine.LoopValMax;
                float fltLoopValMin = (float)objTrendEngine.LoopValMin;
                float fltLoopValIncrement = (float)objTrendEngine.LoopValIncrement;
                bool isDynamicWaitTrend = (bool)objTrendEngine.IsDynamicWaitTrend;

                int intStabilityWindowState = (int)objTrendEngine.WaitTrend;
                float fltThreshBase = (float)-0.000090;
                float dblPriceMax = 0;
                dtEnd = GetAssetLastTradingDate(objHEEntities.AssetPriceSet.Where(x => x.AssetSymbolSet.Id.Equals(objAssetSymSet.Id) && x.Price > 0).ToList());
                if (objAssetSymSet != null)
                {
                    objPriceList = objHEEntities.AssetPriceSet.Where(x => x.Date <= dtEnd && x.AssetSymbolSet.Id.Equals(objAssetSymSet.Id)).OrderBy(x => x.Date).ToList();

                    arrDates = objPriceList.Select(x => x.Date).ToArray().Select(x => x.ToOADate()).ToArray();
                    arrPrices = objPriceList.Select(x => x.Price).ToArray().Select(x => (double)x).ToArray();
                    //arrGrowthVals = objPriceList.Select(x => x.Growth).ToArray().Select(x => (double)x).ToArray();
                    try
                    {
                        arrGrowthVals = objPriceList.Select(x => x.Growth).ToArray().Select(x => (double)x).ToArray();
                    }
                    catch (Exception ex)
                    {

                        //List<AssetPriceSet> objPriceListRecalcGrowth;

                        //objPriceListRecalcGrowth = objHEEntities.AssetPriceSet.Where(x => x.Date > dtEnd && x.AssetSymbolSet.Id.Equals(objAssetSymSet.Id)).OrderBy(x => x.Date).ToList();
                        //objPriceListRecalcGrowth = objPriceList.Take(objPriceList.Count - 65).ToList();

                        //double[] arrGrDate = new double[objPriceListRecalcGrowth.Count];
                        //double[] arrGrPrices = new double[objPriceListRecalcGrowth.Count];
                        //double[] arrGrFutureDates = new double[65];

                        //arrGrFutureDates = objPriceList.Skip(objPriceList.Count - 65).Select(x => x.Date).ToArray().Select(x => x.ToOADate()).ToArray();

                        //arrGrDate = objPriceListRecalcGrowth.Select(x => x.Date).ToArray().Select(x => x.ToOADate()).ToArray();

                        //arrGrPrices = objPriceListRecalcGrowth.Select(x => (double)x.Price).ToArray();

                        //double[] arrFutureGrowthVals = CommonUtility.Growth(arrGrDate,arrGrPrices); //, arrGrDate, arrGrFutureDates, true)).OfType<object>().ToArray().Select(x => Convert.ToDouble(x.ToString())).ToArray();

                        //for (int intIndex = 0; intIndex < arrGrFutureDates.Count(); intIndex++)
                        //{
                        //    //objAssetPriceSet = new AssetPriceSet();
                        //    DateTime dt = DateTime.FromOADate(arrGrFutureDates[intIndex]);
                        //    AssetPriceSet objAPS;
                        //    objAPS = objHEEntities.AssetPriceSet.Where(x => x.AssetSymbolSet.Symbol.Equals(strSymbol) && x.Date.Equals(dt)).ToList()[0];
                        //    objAPS.Growth = (decimal)arrFutureGrowthVals[intIndex];
                        //}
                        //objHEEntities.SaveChanges();
                        //objPriceList = objHEEntities.AssetPriceSet.Where(x => x.Date <= dtEnd && x.AssetSymbolSet.Id.Equals(objAssetSymSet.Id)).OrderBy(x => x.Date).ToList();
                        //arrDates = objPriceList.Select(x => x.Date).ToArray().Select(x => x.ToOADate()).ToArray();
                        //arrPrices = objPriceList.Select(x => x.Price).ToArray().Select(x => (double)x).ToArray();
                        //arrGrowthVals = objPriceList.Select(x => x.Growth).ToArray().Select(x => (double)x).ToArray();



                        List<AssetPriceSet> objPriceListRecalcGrowth;

                        //objPriceListRecalcGrowth = objHEEntities.AssetPriceSet.Where(x => x.Date > dtEnd && x.AssetSymbolSet.Id.Equals(objAssetSymSet.Id)).OrderBy(x => x.Date).ToList();
                        objPriceListRecalcGrowth = objPriceList.Take(objPriceList.Count).ToList();

                        double[] arrGrDate = new double[objPriceListRecalcGrowth.Count];
                        double[] arrGrPrices = new double[objPriceListRecalcGrowth.Count];
                        double[] arrGrFutureDates = new double[65];

                        arrGrFutureDates = objPriceList.Select(x => x.Date).ToArray().Select(x => x.ToOADate()).ToArray();

                        arrGrDate = objPriceListRecalcGrowth.Select(x => x.Date).ToArray().Select(x => x.ToOADate()).ToArray();

                        arrGrPrices = objPriceList.Select(x => (double)x.Price).ToArray();

                        double[] arrFutureGrowthVals = CommonUtility.Growth(arrGrFutureDates, arrGrPrices, objAssetSymSet.Id);//arrGrPrices, arrGrDate, arrGrFutureDates, true)).OfType<object>().ToArray().Select(x => Convert.ToDouble(x.ToString())).ToArray();

                        for (int intIndex = 0; intIndex < arrGrFutureDates.Count(); intIndex++)
                        {
                            //objAssetPriceSet = new AssetPriceSet();
                            DateTime dt = DateTime.FromOADate(arrGrFutureDates[intIndex]);
                            AssetPriceSet objAPS;
                            objAPS = objHEEntities.AssetPriceSet.Where(x => x.AssetSymbolSet.Symbol.Equals(strSymbol) && x.Date.Equals(dt)).ToList()[0];
                            objAPS.Growth = (decimal)arrFutureGrowthVals[intIndex];
                        }
                        objHEEntities.SaveChanges();
                        objPriceList = objHEEntities.AssetPriceSet.Where(x => x.Date <= dtEnd && x.AssetSymbolSet.Id.Equals(objAssetSymSet.Id)).OrderBy(x => x.Date).ToList();
                        arrDates = objPriceList.Select(x => x.Date).ToArray().Select(x => x.ToOADate()).ToArray();
                        arrPrices = objPriceList.Select(x => x.Price).ToArray().Select(x => (double)x).ToArray();
                        arrGrowthVals = objPriceList.Select(x => x.Growth).ToArray().Select(x => (double)x).ToArray();
                    }
                    #region Array initialization.

                    //Memory allocation for all the array`s.
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
                    //** dynamic wait trend change
                    arrDynamicDayCount = new int[objPriceList.Count()];
                    arrPriceChange = new double[objPriceList.Count()];
                    //**
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

                    //if (strSymbol.Equals("PRMSX"))
                    //{
                    //    throw new Exception();
                    //}

                    #endregion

                    #region Compute Trend Engine calculations.

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
                            //change 13-Dec-2010
                                                      

                            if (intI == arrPrices.Length - 1)
                            {
                                double[] arrPricesDummy = new double[arrPrices.Length + 1];
                                arrPrices.CopyTo(arrPricesDummy, 0);
                                arrPricesDummy[arrPricesDummy.Length - 1] = arrPrices[arrPrices.Length - 1];
                                arrCompAvg[intI] = ((decSMATrend * (float)arrPrices.Skip(intSumCount + 1).Take(150).Average()) + ((float)arrPricesDummy.Skip(intSumCount + 101).Take(51).Average())) / (decSMATrend + 1);
                            }
                            else
                                arrCompAvg[intI] = ((decSMATrend * (float)arrPrices.Skip(intSumCount + 1).Take(150).Average()) + ((float)arrPrices.Skip(intSumCount + 101).Take(51).Average())) / (decSMATrend + 1);

                            if (arrPrices[intI] != 0)
                                arrLMS[intI] = (float)(arrPrices[intI] - arrGrowthVals[intI]) / (float)arrPrices[intI];
                            else
                                arrLMS[intI] = 0;
                        }

                        if (intI == 163)
                        {
                            arrDayCount[intI] = 0;
                            //** dynamic wait trend change
                            arrPriceChange[163] = arrPrices[163];
                            arrDynamicDayCount[163] = 0;
                            //**
                            arrInOutFilterState[intI] = "IN";
                            arrInOutState[intI] = "IN";
                            arrEqAdjClosePrice[intI] = (float)arrPrices[163];
                        }
                        #region intI=163 commented
                        if (intI == 163)
                        {
                            float dblTbase = 0;
                            for (dblTbase = (float)fltLoopValMin; fltThreshBase <= fltLoopValMax; fltThreshBase = fltThreshBase + (float)fltLoopValIncrement)
                            {
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
                                    //change 13-Dec-2010
                                    arrInOutState[intI] = "IN";
                                    arrEqAdjClosePrice[intI] = (float)arrPrices[163];
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
                                if ((double)arrEqAdjClosePrice[intI] > dblPriceMax)
                                {
                                    fltThreshBase = dblTbase;
                                    dblPriceMax = arrEqAdjClosePrice[intI];
                                }
                            }
                        }
                        #endregion
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
                            //change 13-Dec-2010
                            if (intI == 164)
                                arrDayCount[intI] = 1;
                            else
                                arrDayCount[intI] = (arrInOutState[intI].Equals(arrInOutState[intI - 1]) ? arrDayCount[intI - 1] + 1 : 1);

                            //** dynamic wait trend change
                            if (isDynamicWaitTrend)
                            {
                                arrPriceChange[intI] = (arrInOutState[intI].Equals(arrInOutState[intI - 1], StringComparison.InvariantCultureIgnoreCase) ? arrPriceChange[intI - 1] : arrPrices[intI]);

                                if (arrInOutState[intI].Equals(arrInOutState[intI - 1], StringComparison.InvariantCultureIgnoreCase))
                                {
                                    if (arrInOutState[intI].Equals("OUT", StringComparison.InvariantCultureIgnoreCase))
                                    {
                                        if (arrPrices[intI] > arrPriceChange[intI])
                                        {
                                            arrDynamicDayCount[intI] = 1;
                                        }
                                        else
                                        {
                                            arrDynamicDayCount[intI] = arrDynamicDayCount[intI - 1] + 1;
                                        }
                                    }
                                    else
                                    {
                                        arrDynamicDayCount[intI] = arrDynamicDayCount[intI - 1] + 1;
                                    }
                                }
                                else
                                {
                                    arrDynamicDayCount[intI] = 1;
                                }
                                //**

                             
                                //** dynamic wait trend change
                                arrInOutFilterState[intI] = (arrDynamicDayCount[intI] > intStabilityWindowState ? arrInOutState[intI] : arrInOutFilterState[intI - 1]);
                                //**                        
                            }
                            else
                            {
                                //Calculate IN/OUT Filter State.
                                arrInOutFilterState[intI] = (arrDayCount[intI] > intStabilityWindowState ? arrInOutState[intI] : arrInOutFilterState[intI - 1]);
                            }
                            //Calculate Filtered Trades.
                            arrFilteredTrades[intI] = (arrInOutFilterState[intI].Equals(arrInOutFilterState[intI - 1]) ? 0 : 1);

                            //Calculate Equivalent adjacent closing price.
                            arrEqAdjClosePrice[intI] = (arrInOutFilterState[intI - 1].Equals("IN", StringComparison.InvariantCultureIgnoreCase) ? (arrEqAdjClosePrice[intI - 1] * (float)(1 + arrDailyChange[intI])) : (arrEqAdjClosePrice[intI - 1] * ((float)(1 + 0.025 / (float)intTradingDays))));
                        }
                    }

                    #endregion

                    #region Fill the data for first 165 records into the Asset Price set table.

                    for (int intI = 0; intI < 166; intI++)
                    {
                        dtStart = DateTime.FromOADate(arrDates[intI]);
                        //Update AssetPriceSet record....
                        objPriceList.First(x => x.Date.Equals(dtStart)).PriceTrend = (decimal)arrEqAdjClosePrice[intI];
                        objPriceList.First(x => x.Date.Equals(dtStart)).MeanDeviation = (decimal)arrLMS[intI];
                        objPriceList.First(x => x.Date.Equals(dtStart)).Threshold = (decimal)arrUpperCAG[intI];
                        objPriceList.First(x => x.Date.Equals(dtStart)).State = arrInOutState[intI];
                        //change 13-Dec-2010
                        objPriceList.First(x => x.Date.Equals(dtStart)).FilterState = arrInOutFilterState[intI];
                    }

                    #endregion
                }
            }
            catch (Exception ex)
            {
                _errorMsg = ex.Message;
                log.Error(strSymbol + "\t" + ex.Message);
                return;
            }
        }

        private void ExportAssetPriceSetToExcel(string strAssetSymbol)
        {
            try
            {
                GridView gvExport = new GridView();
                gvExport.DataSource = objHEEntities.AssetSet.Where(x => x.AssetSymbolSet.Symbol.Equals(strAssetSymbol, StringComparison.InvariantCultureIgnoreCase)).OrderBy(x => x.Ending_Date);
                gvExport.DataBind();
                HttpContext.Current.Response.Clear();

                HttpContext.Current.Response.AddHeader("content-disposition", "attachment;filename=" + strAssetSymbol + "AssetSet.xls");

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
                log.Error(strAssetSymbol + "\t" + ex.Message);
                return;
            }
        }

        #endregion

        #region Public Methods

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

        /// <summary>
        /// This method is used to perform the Trend Engine calculations.
        /// </summary>
        /// <param name="parCurrentDate">Ending date.</param>
        /// <param name="strSymbol">Asset symbol.</param>
        /// <param name="isExportData"></param>
        /// <param name="isHistorical"></param>
        public void PerformTrendEngineAnalysis(DateTime parCurrentDate, string strSymbol, bool isExportData, bool isHistorical)
        {
            // log.Info("Entering Trend Engine");
            try
            {
                //if (strSymbol.Equals("PRPFX"))
                //{
                //    throw new Exception();
                //}
                DateTime dtStart = parCurrentDate;
                DateTime dtEnd = parCurrentDate;
                objHEEntities = new Harbor_EastEntities();
                var objAssetSymSet = objHEEntities.AssetSymbolSet.First(x => x.Symbol.Equals(strSymbol, StringComparison.InvariantCultureIgnoreCase));

                var lstTrendEngine = objHEEntities.TrendEngineVariablesSet;

                float decSMATrend;
                float fltIncrement;
                float fltMax;
                float fltMin;
                float decHysteresisOffset;
                float fltLoopValMax;
                float fltLoopValMin;
                float fltLoopValIncrement;
                int intStabilityWindowState;
                int intTradingDays;

                //If not historical then do...
                if (!isHistorical)
                {
                    
                    if (objAssetSymSet != null)
                    {
                        var objExtAssetSet = objHEEntities.AssetSet.FirstOrDefault(x => x.AssetSymbolSet.Id.Equals(objAssetSymSet.Id));
                        if (objExtAssetSet != null)
                        {
                            int intTEVID = Convert.ToInt32(((EntityReference)(objExtAssetSet.TrendEngineVariablesSetReference)).EntityKey.EntityKeyValues[0].Value);
                            
                            //** Dividend or Split validation commented
                            //If no divident or split occurred.
                            //if (objExtAssetSet.DividentSplit == 0)
                            //{
                                double extTbase = (double)objExtAssetSet.Tbase;
                                int intUpdateInterval = objHEEntities.TrendEngineVariablesSet.Select(x => x.LoopUpdateInterval.Id).First();
                                if (intUpdateInterval == 1)
                                {
                                    isHistorical = true;
                                }
                                else if (intUpdateInterval == 2)
                                {
                                    if (DateTime.Today.DayOfWeek.Equals(DayOfWeek.Saturday))
                                    {
                                        isHistorical = true;    
                                    }
                                    else
                                    {
                                        PerformDailyTrendEngineAnalysis(parCurrentDate, strSymbol, intTEVID, extTbase);
                                    }
                                }
                                else if (intUpdateInterval == 3)
                                {
                                    if (DateTime.Today.Day.Equals(1))
                                    {
                                        isHistorical = true;
                                    }
                                    else
                                    {
                                        PerformDailyTrendEngineAnalysis(parCurrentDate, strSymbol, intTEVID, extTbase);
                                    }
                                }
                                else if (intUpdateInterval == 4)
                                {
                                    bool boolIsHistoricalTE = false;

                                    if (DateTime.Today.Day.Equals(1))
                                        if (DateTime.Today.Month == 1 || DateTime.Today.Month == 4 || DateTime.Today.Month == 7 || DateTime.Today.Month == 10)
                                            boolIsHistoricalTE = true;

                                    if (boolIsHistoricalTE)
                                    {
                                        isHistorical = true;
                                    }
                                    else
                                    {
                                        PerformDailyTrendEngineAnalysis(parCurrentDate, strSymbol, intTEVID, extTbase);
                                    }
                                }
                                else if (intUpdateInterval == 5)
                                {
                                    if ((DateTime.Today.Month.Equals(1) && DateTime.Today.Day.Equals(1)))
                                    {
                                        isHistorical = true;
                                    }
                                    else
                                    {
                                        PerformDailyTrendEngineAnalysis(parCurrentDate, strSymbol, intTEVID, extTbase);
                                    }
                                }
                                else if (intUpdateInterval == 6)
                                {
                                    PerformDailyTrendEngineAnalysis(parCurrentDate, strSymbol, intTEVID, extTbase);
                                }
                                //Calculate for the Daily Trend data.
                                
                            //}
                            ////If divident or split occurred then.
                            //else
                            //{
                                //Calculate for the Historical data.
                                //PerformHistoricalTrendEngineAnalysis(parCurrentDate, strSymbol, intTEVID);

                                //var objTrendEngine = objHEEntities.TrendEngineVariablesSet.FirstOrDefault(x => x.Id.Equals(intTEVID));

                                //decSMATrend = (float)objTrendEngine.SMATrend;
                                //fltIncrement = (float)objTrendEngine.ThresholdIncrement;
                                //fltMax = (float)objTrendEngine.ThresholdtMax;
                                //fltMin = (float)objTrendEngine.ThresholdMin;
                                //decHysteresisOffset = (float)objTrendEngine.Hysteresis;
                                //fltLoopValMax = (float)objTrendEngine.LoopValMax;
                                //fltLoopValMin = (float)objTrendEngine.LoopValMin;
                                //fltLoopValIncrement = (float)objTrendEngine.LoopValIncrement;
                                //intStabilityWindowState = (int)objTrendEngine.WaitTrend;
                                //intTradingDays = (int)objTrendEngine.YearTradingDays;

                                //PerformTrendEngineAnalysis(objTrendEngine.Id, dtStart, objAssetSymSet, false, fltLoopValIncrement, fltLoopValMax, fltLoopValMin, fltIncrement, fltMax, fltMin, intTradingDays, intStabilityWindowState, decHysteresisOffset);
                           // }
                        }
                    }
                }

                //If historical then do...
                if (isHistorical)
                {
                    int intI = 0;
                    float[] fltCAGRTrend = new float[lstTrendEngine.Count()];
                    Dictionary<int, float> dictCAGRTrend = new Dictionary<int, float>();

                    foreach (var objTrendEngine in lstTrendEngine)
                    {
                        decSMATrend = (float)objTrendEngine.SMATrend;
                        fltIncrement = (float)objTrendEngine.ThresholdIncrement;
                        fltMax = (float)objTrendEngine.ThresholdtMax;
                        fltMin = (float)objTrendEngine.ThresholdMin;
                        decHysteresisOffset = (float)objTrendEngine.Hysteresis;
                        fltLoopValMax = (float)objTrendEngine.LoopValMax;
                        fltLoopValMin = (float)objTrendEngine.LoopValMin;
                        fltLoopValIncrement = (float)objTrendEngine.LoopValIncrement;
                        intStabilityWindowState = (int)objTrendEngine.WaitTrend;
                        intTradingDays = (int)objTrendEngine.YearTradingDays;

                        //Calculate for the Historical data.
                        PerformHistoricalTrendEngineAnalysis(parCurrentDate, strSymbol, objTrendEngine.Id);

                        fltCAGRTrend[intI] = PerformTrendEngineAnalysis(objTrendEngine.Id, dtStart, objAssetSymSet, isHistorical, fltLoopValIncrement, fltLoopValMax, fltLoopValMin, fltIncrement, fltMax, fltMin, intTradingDays, intStabilityWindowState, decHysteresisOffset);
                        dictCAGRTrend.Add(objTrendEngine.Id, fltCAGRTrend[intI]);
                        intI++;
                    }
                    
                        float fltCAGRMax = fltCAGRTrend.Max();
                        int fltMaxSMATrendID = 0;
                        foreach (int fltKey in dictCAGRTrend.Keys)
                        {
                            float fltTempVal = 0;
                            dictCAGRTrend.TryGetValue(fltKey, out fltTempVal);
                            if (fltTempVal == fltCAGRMax)
                                fltMaxSMATrendID = fltKey;
                        }

                        var extTEVSObj = lstTrendEngine.FirstOrDefault(x => x.Id.Equals(fltMaxSMATrendID));
                        fltIncrement = (float)extTEVSObj.ThresholdIncrement;
                        fltMax = (float)extTEVSObj.ThresholdtMax;
                        fltMin = (float)extTEVSObj.ThresholdMin;
                        decHysteresisOffset = (float)extTEVSObj.Hysteresis;
                        fltLoopValMax = (float)extTEVSObj.LoopValMax;
                        fltLoopValMin = (float)extTEVSObj.LoopValMin;
                        fltLoopValIncrement = (float)extTEVSObj.LoopValIncrement;
                        intStabilityWindowState = (int)extTEVSObj.WaitTrend;
                        intTradingDays = (int)extTEVSObj.YearTradingDays;

                        //Calculate for the Historical data.
                        PerformHistoricalTrendEngineAnalysis(parCurrentDate, strSymbol, extTEVSObj.Id);

                        PerformTrendEngineAnalysis(extTEVSObj.Id, dtStart, objAssetSymSet, false, fltLoopValIncrement, fltLoopValMax, fltLoopValMin, fltIncrement, fltMax, fltMin, intTradingDays, intStabilityWindowState, decHysteresisOffset);
                    
                }

                if (isExportData)
                    ExportAssetPriceSetToExcel(strSymbol);
            }
            catch (Exception ex)
            {
                _errorMsg = ex.Message;
                log.Error(strSymbol + "\t" + ex.Message);
                return;
            }
        }

        public void ReadTELog(string parPath)
        {
            try
            {
                FileStream fs = new FileStream(parPath, FileMode.Open, FileAccess.Read);
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

                //foreach(string strItem in errorSymbols)
                //{
                //    if (!noDups.Contains(strItem.Trim()))
                //    {
                //        noDups.Add(strItem.Trim());
                //    }
                //}

                for (int i = 0; i < errorSymbols.Count; i++)
                {
                    string tempPath = parPath.Replace("TE_Log.txt", "DS_Log.txt");
                    clsDataScrapperBAL objclsDataScrapperBAL = new clsDataScrapperBAL(tempPath);
                    objclsDataScrapperBAL.PushDataIntoDataBase(DateTime.Now, false, errorSymbols[i].ToString());
                    PerformTrendEngineAnalysis(DateTime.Now, errorSymbols[i].ToString(), false, true);
                }

            }
            catch (Exception ex)
            {

            }

            // Suspend the screen.


        }

        #endregion
    }
}
