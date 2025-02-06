using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Objects.DataClasses;
using System.Data;
using System.Web.UI.WebControls;
using System.IO;
using System.Web.UI;
using System.Text;
using System.Collections;
using System.Web.UI.HtmlControls;
using HarborEast.BAL;
using Microsoft.VisualBasic;

namespace HarborEast.BAL
{
    // Added for rank engine
    public class RankRotationProperties
    {
        private string _strPair;

        public string StrPair
        {
            get { return _strPair; }
            set { _strPair = value; }
        }

        private int[] _whoWins;

        public int[] WhoWins
        {
            get { return _whoWins; }
            set { _whoWins = value; }
        }

        private int isRotation;

        public int IsRotation
        {
            get { return isRotation; }
            set { isRotation = value; }
        }

        private double[] _dailyChangeFirst;

        public double[] DailyChangeFirst
        {
            get { return _dailyChangeFirst; }
            set { _dailyChangeFirst = value; }
        }


        private double[] _dailyChangeSecond;

        public double[] DailyChangeSecond
        {
            get { return _dailyChangeSecond; }
            set { _dailyChangeSecond = value; }
        }

        private double[] _priceTrendFirst;

        public double[] PriceTrendFirst
        {
            get { return _priceTrendFirst; }
            set { _priceTrendFirst = value; }
        }


        private double[] _priceTrendSecond;

        public double[] PriceTrendSecond
        {
            get { return _priceTrendSecond; }
            set { _priceTrendSecond = value; }
        }

        private double[] _priceFirst;

        public double[] PriceFirst
        {
            get { return _priceFirst; }
            set { _priceFirst = value; }
        }

        private double[] _priceSecond;

        public double[] PriceSecond
        {
            get { return _priceSecond; }
            set { _priceSecond = value; }
        }



    }
    public class AssetProperties
    {
        private string _assetSymbolID;

        public string AssetSymbolID
        {
            get { return _assetSymbolID; }
            set { _assetSymbolID = value; }
        }

        private double[] _assetWtCoefficient;

        public double[] AssetWtCoefficient
        {
            get { return _assetWtCoefficient; }
            set { _assetWtCoefficient = value; }
        }

        private double[] _assetDailyReturn;

        public double[] AssetDailyReturn
        {
            get { return _assetDailyReturn; }
            set { _assetDailyReturn = value; }
        }

        private double[] _shares;

        public double[] Shares
        {
            get { return _shares; }
            set { _shares = value; }
        }

        double[] dblCurrentWeight;

        public double[] CurrentWeight
        {
            get { return dblCurrentWeight; }
            set { dblCurrentWeight = value; }
        }
    }
    public class clsRotationEngine
    {
        #region Variable Declarations

        Harbor_EastEntities objHEEntities = new Harbor_EastEntities();
        //change 9-Mar-2011 trading days
        int intTradingDays = 0;
        
        bool isRotationHistorical = false;
        bool isDividendOrSplit = false;
        //** code added for RR

        DateTime[] dtRankDates;
        double[] dblDailyChange1;
        double[] dblDailyChange2;
        double[] dblPriceTrend1;
        double[] dblPriceTrend2;
        double[] dblPrice1;
        double[] dblPrice2;

        List<RankRotationProperties> lstRankRotation = null;
        List<AssetProperties> lstAssetProperties = null;

        //** end RR code

        #endregion

        #region Constructors

        public clsRotationEngine()
        {
            //change 9-Mar-2011 trading days
            //intTradingDays = (int) objHEEntities.TrendEngineVariablesSet.ElementAt(0).YearTradingDays;
            intTradingDays = (int)objHEEntities.TrendEngineVariablesSet.Select(x => x.YearTradingDays).First();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// This method is used to check whether the object passed contains within the list collection.
        /// </summary>
        /// <param name="parLst"></param>
        /// <param name="parPairs"></param>
        /// <returns>returns true/false depending on the search.</returns>
        private bool SearchSymbol(List<Pairs> parLst, Pairs parPairs)
        {
            foreach (var objTemp in parLst)
            {
                if ((objTemp.IntFirst == parPairs.IntFirst || objTemp.IntSecond == parPairs.IntFirst || objTemp.IntFirst == parPairs.IntSecond || objTemp.IntSecond == parPairs.IntSecond))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// This method is used for calculating the values of Rebalance Trend Rotation.
        /// </summary>
        /// <param name="intSelectedIndex"></param>
        /// <param name="strSelectedValue"></param>
        /// <param name="dictFinalData"></param>
        /// <param name="dictArr"></param>
        /// <param name="isNegativePairtoZero"></param>
        private void PerformRebalanceTrendRotation(int intSelectedIndex, string strSelectedValue, Dictionary<string, int[]> dictFinalData, Dictionary<string, double[]> dictArr, bool isNegativePairtoZero)
        {
            DataTable dt = new DataTable();
            try
            {
                if (intSelectedIndex != -1)
                {
                    double[] decBHPortfolioValue = null;
                    double[] decPortofolioDailyChange = null;
                    double[] decYearRollChange = null;
                    int[] intDayCountQuarters = null;

                    int intPortfolioID = Convert.ToInt32(strSelectedValue);
                    List<AssetAllocation> lstAssetAllocation;

                    //Get the collection of all the PortfolioContentSet for a particular Portfolio Id.
                    List<PortfolioContentSet> lstPortfolioContentSet = objHEEntities.PortfolioContentSet.Where(x => x.PortfolioSet.Id.Equals(intPortfolioID)).ToList();
                    List<clsRECalculations> lstREColl = new List<clsRECalculations>();
                    if (lstPortfolioContentSet != null)
                    {
                        DateTime dtFinalStartDate = new DateTime(1, 1, 1);
                        int intCount = 0;
                        int intAssetSymbolID = 0;
                        int intArraySize = 0;
                        double dblAggressiveness = 0.9;

                        //Get the instance of PortfolioSet for a particular Portfolio Id.
                        var objPortfolioSet = objHEEntities.PortfolioSet.FirstOrDefault(x => x.Id.Equals(intPortfolioID));
                        if (objPortfolioSet != null)
                            dblAggressiveness = objPortfolioSet.Aggressiveness;

                        #region Get the earliest trading date for Portfolio

                        //Iterate through the collection and get the earliest trading date.
                        foreach (var objSet in lstPortfolioContentSet)
                        {
                            DateTime dtStartDate = CommonUtility.GetStartDate(Convert.ToInt32(((EntityReference)(objSet.AssetSymbolSetReference)).EntityKey.EntityKeyValues[0].Value));
                            if (intCount == 0)
                            {
                                dtFinalStartDate = dtStartDate;
                                intAssetSymbolID = Convert.ToInt32(((EntityReference)(objSet.AssetSymbolSetReference)).EntityKey.EntityKeyValues[0].Value);
                                intCount++;
                            }
                            else
                            {
                                if (dtStartDate >= dtFinalStartDate)
                                {
                                    dtFinalStartDate = dtStartDate;
                                    intAssetSymbolID = Convert.ToInt32(((EntityReference)(objSet.AssetSymbolSetReference)).EntityKey.EntityKeyValues[0].Value);
                                }
                            }
                        }

                        #endregion


                        DateTime dtFinalLastDate = new DateTime(1, 1, 1);
                        int intLastDateCount = 0;
                        #region Get the earliest Last Trading Date from Assets
                        foreach (var objSet in lstPortfolioContentSet)
                        {
                            DateTime dtLastDate = CommonUtility.GetLastTradingDate(Convert.ToInt32(((EntityReference)(objSet.AssetSymbolSetReference)).EntityKey.EntityKeyValues[0].Value));
                            if (intLastDateCount == 0)
                            {
                                dtFinalLastDate = dtLastDate;
                                //intAssetSymbolID = Convert.ToInt32(((EntityReference)(objSet.AssetSymbolSetReference)).EntityKey.EntityKeyValues[0].Value);
                                intLastDateCount++;
                            }
                            else
                            {
                                if (dtLastDate < dtFinalLastDate)
                                {
                                    dtFinalLastDate = dtLastDate;
                                    // intAssetSymbolID = Convert.ToInt32(((EntityReference)(objSet.AssetSymbolSetReference)).EntityKey.EntityKeyValues[0].Value);
                                }
                            }
                        }
                        #endregion

                        clsRECalculations objRECal;

                        #region Get the array size for this selected Portfolio from the AssetPriceSet table.

                        //Get the array size of the portfolio.
                        //intArraySize = CommonUtility.GetArraySize(intAssetSymbolID);

                        intArraySize = CommonUtility.GetArraySizeForPortfolioMethods(intAssetSymbolID, dtFinalLastDate);

                        #endregion

                        #region Compute the Buy and Hold operation values.

                        if (intArraySize != -1)
                        {
                            lstAssetAllocation = new List<AssetAllocation>();
                            AssetAllocation objAssetAllocation = null;
                            decBHPortfolioValue = new double[intArraySize];
                            decPortofolioDailyChange = new double[intArraySize];
                            decYearRollChange = new double[intArraySize];
                            intDayCountQuarters = new int[intArraySize];
                            int intMostRecentRebIndex = 217;

                            //Initialize the array by filling the date,price and pricetrend values.
                            foreach (var objSet in lstPortfolioContentSet)
                            {
                                objAssetAllocation = new AssetAllocation(intArraySize, 1);
                                intAssetSymbolID = Convert.ToInt32(((EntityReference)(objSet.AssetSymbolSetReference)).EntityKey.EntityKeyValues[0].Value);
                                objAssetAllocation.MAssetSymbolId = intAssetSymbolID;
                                objAssetAllocation.MDate = objHEEntities.AssetPriceSet.Where(x => x.PriceTrend != null && x.Date >= dtFinalStartDate && x.Date <= dtFinalLastDate && x.AssetSymbolSet.Id.Equals(intAssetSymbolID)).OrderBy(x => x.Date).Select(x => x.Date).ToArray().Select(x => x.ToOADate()).ToArray();
                                objAssetAllocation.MPrice = objHEEntities.AssetPriceSet.Where(x => x.PriceTrend != null && x.Date >= dtFinalStartDate && x.Date <= dtFinalLastDate && x.AssetSymbolSet.Id.Equals(intAssetSymbolID)).OrderBy(x => x.Date).Select(x => (double)x.Price).ToArray();
                                objAssetAllocation.MPriceTrend = objHEEntities.AssetPriceSet.Where(x => x.PriceTrend != null && x.Date >= dtFinalStartDate && x.Date <= dtFinalLastDate && x.AssetSymbolSet.Id.Equals(intAssetSymbolID)).OrderBy(x => x.Date).Select(x => (double)x.PriceTrend).ToArray();
                                objAssetAllocation.MAssetValue[217] = (double)objSet.Value;
                                lstAssetAllocation.Add(objAssetAllocation);
                            }

                            //Re-calculate the price trend value with the normalized value.
                            foreach (var objSet in lstAssetAllocation)
                            {
                                //Get the normalization constant.
                                double decNorConstant = objSet.MPrice[217] / objSet.MPriceTrend[217];
                                if (decNorConstant != 0)
                                {
                                    for (int intI = 0; intI < objSet.MPriceTrend.Count(); intI++)
                                    {
                                        objSet.MPriceTrend[intI] = objSet.MPriceTrend[intI] * decNorConstant;
                                    }
                                }
                            }

                            //Iterate through the collection and calculate SMA and Slope.
                            foreach (var objSet in lstAssetAllocation)
                            {
                                int intSMASkipCount = 163;
                                int intSlopeSkipCount = 213;
                                for (int intI = 213; intI < objSet.MPrice.Count(); intI++)
                                {
                                    objSet.MSMA[intI] = objSet.MPriceTrend.Skip(intSMASkipCount).Take(51).Average();
                                    intSMASkipCount++;

                                    if (intI >= 217)
                                    {
                                        objSet.MSlope[intI] = CommonUtility.Slope(objSet.MSMA.Skip(intSlopeSkipCount).Take(5).Select(x => (double)x).ToArray(), objSet.MDate.Skip(intSlopeSkipCount).Take(5).ToArray()) / (double)objSet.MSMA[intI];
                                        intSlopeSkipCount++;
                                    }
                                }
                            }
                            int intRollingCount = 261;
                           // int a = 29;
                            for (int intI = 217; intI <= lstAssetAllocation[0].MPrice.Count() - 1; intI++)
                            {
                                //Calculate the daily change.
                                foreach (var set in lstAssetAllocation)
                                {
                                    set.MDailyChange[intI] = (set.MPrice[intI] - set.MPrice[intI - 1]) / set.MPrice[intI - 1];
                                }

                                if (intI == 217)
                                {
                                    intDayCountQuarters[intI] = 1;

                                    //Calculate Portfolio Value
                                    decBHPortfolioValue[intI] = CommonUtility.GetBHPortfolioTotalValue(lstAssetAllocation, intI);

                                    //Calculate Weight.
                                    foreach (var set in lstAssetAllocation)
                                    {
                                        set.MWeight[intI] = set.MAssetValue[intI] / decBHPortfolioValue[intI];
                                    }

                                    //Calculate Target Weight.
                                    foreach (var set in lstAssetAllocation)
                                    {
                                        set.MTargetWeight[intI] = set.MWeight[intI];
                                    }

                                    //Calculate Shares.
                                    foreach (var set in lstAssetAllocation)
                                    {
                                        set.MAssetShares[intI] = decBHPortfolioValue[intI] * set.MWeight[intI] / set.MPriceTrend[intI];
                                    }
                                }
                                else
                                {
                                    //Day count quarters.
                                    //annual rebalance of shares logic
                                    //if (intDayCountQuarters[intI - 1] == 65)
                                    //{
                                    //    intDayCountQuarters[intI] = 1;
                                    //    intMostRecentRebIndex = intI - 1;
                                    //}
                                    //else
                                    //    intDayCountQuarters[intI] = intDayCountQuarters[intI - 1] + 1;

                                    bool blnYearStartOnWeekend = false;
                                    if (intI > 0)
                                    {
                                        if (DateTime.FromOADate(lstAssetAllocation[0].MDate[intI - 1]).DayOfWeek == DayOfWeek.Friday && DateTime.FromOADate(lstAssetAllocation[0].MDate[intI - 1]).Month == 12 && (DateTime.FromOADate(lstAssetAllocation[0].MDate[intI - 1]).Day == 31 || DateTime.FromOADate(lstAssetAllocation[0].MDate[intI - 1]).Day == 30))
                                        {
                                            blnYearStartOnWeekend = true;
                                        }
                                    }

                                    if ((blnYearStartOnWeekend) || (DateTime.FromOADate(lstAssetAllocation[0].MDate[intI]).Day == 1 && DateTime.FromOADate(lstAssetAllocation[0].MDate[intI]).Month == 1))
                                    {
                                        intDayCountQuarters[intI] = 1;
                                        intMostRecentRebIndex = intI;
                                    }
                                    else
                                    {
                                        intDayCountQuarters[intI] = intDayCountQuarters[intI - 1] + 1;
                                    }

                                    //TargetWeight.
                                    foreach (var set in lstAssetAllocation)
                                    {
                                        set.MTargetWeight[intI] = set.MTargetWeight[intI - 1];
                                    }

                                    //Calculate Shares.
                                    foreach (var set in lstAssetAllocation)
                                    {
                                        //annual rebalance of shares logic
                                        //if (intDayCountQuarters[intI] < 65)
                                        //    set.MAssetShares[intI] = set.MAssetShares[intI - 1];
                                        //else
                                        //    set.MAssetShares[intI] = decBHPortfolioValue[intI - 1] * set.MTargetWeight[intI] / set.MPriceTrend[intI - 1];

                                        if(intDayCountQuarters[intI] == 1 )
                                            set.MAssetShares[intI] = decBHPortfolioValue[intI - 1] * set.MTargetWeight[intI] / set.MPriceTrend[intI - 1];
                                        else
                                            set.MAssetShares[intI] = set.MAssetShares[intI - 1];

                                    }

                                    //Calculate Asset value.
                                    foreach (var set in lstAssetAllocation)
                                    {
                                        set.MAssetValue[intI] = set.MAssetShares[intI] * set.MPriceTrend[intI];
                                    }
                                    
                                    ////Code changes for debugger
                                    //if (intI == 345 || intI==345+a)
                                    //{
                                    //    System.Diagnostics.Debugger.Break();
                                    //    a = a + a;

                                    //}
                                        //Calculate TargetValue.
                                    decBHPortfolioValue[intI] = CommonUtility.GetBHPortfolioTotalValue(lstAssetAllocation, intI);

                                    //Calculate Weight.
                                    foreach (var set in lstAssetAllocation)
                                    {
                                        //annual rebalance of shares logic
                                        //if (intDayCountQuarters[intI] < 65)
                                        //    set.MWeight[intI] = set.MAssetValue[intI] / decBHPortfolioValue[intI];
                                        //else
                                        //    set.MWeight[intI] = set.MTargetWeight[intI];
                                        if (intDayCountQuarters[intI] == 1)
                                            set.MWeight[intI] = set.MTargetWeight[intI];
                                        else
                                            set.MWeight[intI] = set.MAssetValue[intI] / decBHPortfolioValue[intI];
                                    }
                                }

                                //Calculate Portfolio daily change.
                                if (intI > 217)
                                {
                                    decPortofolioDailyChange[intI] = (decBHPortfolioValue[intI] - decBHPortfolioValue[intI - 1]) / decBHPortfolioValue[intI - 1];
                                }

                                //Calculate yearly rolling change.
                                if (intI >= 478)
                                {
                                    decYearRollChange[intI] = (decBHPortfolioValue[intI] - decBHPortfolioValue[intI - intRollingCount]) / decBHPortfolioValue[intI - intRollingCount];
                                }
                            }

                            Dictionary<string, double> dictPairDefaultValue = new Dictionary<string, double>();

                            foreach (string strPairKey in dictFinalData.Keys)
                            {
                                objRECal = new clsRECalculations(intArraySize);
                                objRECal.SymbolPair = strPairKey;
                                lstREColl.Add(objRECal);
                                string[] strPairColl = strPairKey.Split(":".ToCharArray());
                                int intFirstAssetSymbol = Convert.ToInt32(strPairColl[0]);
                                double dblFirstVal = (double)objHEEntities.PortfolioContentSet.FirstOrDefault(x => x.AssetSymbolSet.Id.Equals(intFirstAssetSymbol) && x.PortfolioSet.Id.Equals(intPortfolioID)).Value;

                                int intSecondAssetSymbol = 0;
                                double dblSecondVal = 0;
                                if (!string.IsNullOrEmpty(strPairColl[1]))
                                {
                                    intSecondAssetSymbol = Convert.ToInt32(strPairColl[1]);
                                    dblSecondVal = (double)objHEEntities.PortfolioContentSet.FirstOrDefault(x => x.AssetSymbolSet.Id.Equals(intSecondAssetSymbol) && x.PortfolioSet.Id.Equals(intPortfolioID)).Value;
                                }

                                dictPairDefaultValue.Add(strPairKey, dblFirstVal + dblSecondVal);
                            }
                            bool isNegative = false;

                            Dictionary<string, List<AssetAllocation>> dictNegativePairReb = new Dictionary<string, List<AssetAllocation>>();
                            Dictionary<string, double[]> dictNegativePFvalue = new Dictionary<string, double[]>();
                            foreach (var strPairKey in lstREColl)
                            {
                                string[] strPairColl = strPairKey.SymbolPair.Split(":".ToCharArray());

                                double[] dblArr = null;

                                dictArr.TryGetValue(strPairKey.SymbolPair, out dblArr);

                                if (dblArr != null)
                                {
                                    double dblVal = (((dblArr[1] - dblArr[0]) / dblArr[0]) * 100);
                                    if (dblVal < 0)
                                    {
                                        double[] decTempDBPortFolioValue = null;
                                        List<AssetAllocation> lstAllocation = PerformRebalanceTrendOperationForNegativePair(strPairKey.SymbolPair, intPortfolioID, intArraySize, dtFinalStartDate, out decTempDBPortFolioValue);
                                        dictNegativePairReb.Add(strPairKey.SymbolPair, lstAllocation);
                                        dictNegativePFvalue.Add(strPairKey.SymbolPair, decTempDBPortFolioValue);
                                    }
                                }
                            }

                            Dictionary<string, double[]> dictRotationRebalance = new Dictionary<string, double[]>();

                            for (int intI = 217; intI < lstAssetAllocation[0].MPrice.Count() ; intI++)
                            {
                                foreach (var strPairKey in lstREColl)
                                {
                                    isNegative = false;
                                    string[] strPairColl = strPairKey.SymbolPair.Split(":".ToCharArray());

                                    double[] dblArr = null;

                                    dictArr.TryGetValue(strPairKey.SymbolPair, out dblArr);

                                    if (dblArr != null)
                                    {
                                        double dblVal = (((dblArr[1] - dblArr[0]) / dblArr[0]) * 100);
                                        if (dblVal < 0)
                                            isNegative = true;
                                    }

                                    //Temporarly set the isNegative flag to false if the check box for setting the negative value to zero is not checked.
                                    //if (!isNegativePairtoZero)
                                    //    isNegative = false;


                                    //Depending upon the condition call different functions.
                                    if (string.IsNullOrEmpty(strPairColl[1]))
                                    {
                                        FillOddPairForRebalanceData(intI, dblAggressiveness, intDayCountQuarters, strPairKey, dictFinalData, strPairColl, dictPairDefaultValue, lstAssetAllocation, lstREColl, dictRotationRebalance);
                                    }
                                    else if (isNegative && isNegativePairtoZero)
                                    {
                                        //if (intI >= 1095)
                                        //{
                                        //    System.Diagnostics.Debugger.Break();
                                        //}
                                        FillPairRebalanceDataForNegativeValue(intI, dblAggressiveness, intDayCountQuarters, strPairKey, dictFinalData, strPairColl, dictPairDefaultValue, lstAssetAllocation, lstREColl, dictRotationRebalance);
                                    }
                                    else if (isNegative)
                                    {
                                        //if (intI >= 1095)
                                        //{
                                        //    System.Diagnostics.Debugger.Break();
                                        //}
                                        List<AssetAllocation> dictAssetAllocation;
                                        dictNegativePairReb.TryGetValue(strPairKey.SymbolPair, out dictAssetAllocation);
                                        PerformRebalanceTrendOperationForNegativePair(intI, dblAggressiveness, intDayCountQuarters, strPairKey, dictFinalData, strPairColl, dictPairDefaultValue, dictAssetAllocation, lstREColl, dictNegativePFvalue, dictRotationRebalance);
                                    }
                                    else
                                    {
                                        //if (intI >= 1095)
                                        //{
                                        //    System.Diagnostics.Debugger.Break();
                                        //}
                                        FillEvenPairForRebalanceData(intI, dblAggressiveness, intDayCountQuarters, strPairKey, dictFinalData, strPairColl, dictPairDefaultValue, lstAssetAllocation, lstREColl, dictRotationRebalance);
                                    }
                                }
                            }

                            int[] intUniqueRotations = new int[intArraySize];
                            int[] intCommonRotations = new int[intArraySize];
                            double[] dblPortfolioTotalVal = new double[intArraySize];
                            double[] dblPortfolioDailyChange = new double[intArraySize];
                            double[] dblYrRollingChange = new double[intArraySize];
                            int intTempUnqRot = 0;
                            double dblTempTotalVal = 0;
                            for (int intI = 217; intI < lstAssetAllocation[0].MPrice.Count(); intI++)
                            {
                                dblTempTotalVal = 0;
                                foreach (var objSet in lstREColl)
                                {
                                    dblTempTotalVal = dblTempTotalVal + objSet.TotalValue[intI];
                                }

                                //Calculate Portfolio Total Value.
                                dblPortfolioTotalVal[intI] = dblTempTotalVal;
                                if (intI > 217)
                                {
                                    intTempUnqRot = 0;
                                    foreach (var objSet in lstREColl)
                                    {
                                        intTempUnqRot = intTempUnqRot + objSet.RotationEvent[intI];
                                    }

                                    //Calculate Unique Rotations.
                                    if (intTempUnqRot == 2)
                                        intUniqueRotations[intI] = 1;
                                    else
                                        intUniqueRotations[intI] = intTempUnqRot;

                                    //Calculate Common Rotations.
                                    if (intTempUnqRot == 1)
                                    {
                                        if (intDayCountQuarters[intI] == 1)
                                            intCommonRotations[intI] = 1;
                                        else
                                            intCommonRotations[intI] = 0;
                                    }
                                    else
                                        intCommonRotations[intI] = 0;

                                    //Calculate Portfolio daily change.
                                    dblPortfolioDailyChange[intI] = (dblPortfolioTotalVal[intI] - dblPortfolioTotalVal[intI - 1]) / dblPortfolioTotalVal[intI - 1];
                                }

                                //Calculate Portfolio rolling change.
                                if (intI >= 478)
                                {
                                    dblYrRollingChange[intI] = (dblPortfolioTotalVal[intI] - dblPortfolioTotalVal[intI - intRollingCount]) / dblPortfolioTotalVal[intI - intRollingCount];
                                }
                            }
                            double dblEndingValue = (double)dblPortfolioTotalVal[dblPortfolioTotalVal.Count() - 1];
                            //double dblCAGR = (Math.Pow((double)(dblPortfolioTotalVal[dblPortfolioTotalVal.Count() - 1] / dblPortfolioTotalVal[217]), (double)(1.00 / ((dblPortfolioTotalVal.Count() - 1 - 217.00) / 260.00))) - 1.0) * 100;
                            double dblCAGR = (Math.Pow((double)(dblPortfolioTotalVal[dblPortfolioTotalVal.Count() - 1] / dblPortfolioTotalVal[217]), (double)(1.00 / ((dblPortfolioTotalVal.Count() - 1 - 217.00) / (double)intTradingDays))) - 1.0) * 100;
                            //double dblSTDev = CommonUtility.STDev(dblPortfolioDailyChange.Skip(218).Select(x => (double)x).ToArray()) * (double)Math.Pow(260, 0.5) * 100;
                            double dblSTDev = CommonUtility.STDev(dblPortfolioDailyChange.Skip(218).Select(x => (double)x).ToArray()) * (double)Math.Pow(intTradingDays, 0.5) * 100;
                            double dblIncrease = dblYrRollingChange.Max();
                            double dblDrawnDown = dblYrRollingChange.Min();
                            double dblSharpe = (dblCAGR - 0.025) / dblSTDev;

                            //HttpContext.Current.Response.Write("<br><br><br>Rebalance Trend Rotation Results.<br>");
                            //HttpContext.Current.Response.Write("---------------------------------------------------------------------------------------------<br>");
                            //HttpContext.Current.Response.Write("Ending Value &nbsp;&nbsp;&nbsp;CAGR BH&nbsp;&nbsp;&nbsp;STDEV&nbsp;&nbsp;&nbsp;Increase&nbsp;&nbsp;&nbsp;DrawDown&nbsp;&nbsp;&nbsp;Sharpe<br>");
                            //HttpContext.Current.Response.Write("---------------------------------------------------------------------------------------------<br>");
                            //HttpContext.Current.Response.Write(String.Format("{0:0.000}", dblEndingValue) + "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;" + String.Format("{0:0.000}", dblCAGR) + "&nbsp;&nbsp;&nbsp;&nbsp;" + String.Format("{0:0.000}", dblSTDev) + "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;" + String.Format("{0:0.000}", dblIncrease) + "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;" + String.Format("{0:0.000}", dblDrawnDown) + "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;" + String.Format("{0:0.000}", dblSharpe));
                            //HttpContext.Current.Response.Write("<br>---------------------------------------------------------------------------------------------");

                            #region Fill Data in PortfolioValueSet Entity
                            if (isRotationHistorical || isDividendOrSplit)
                            {
                                for (int i = 217; i < lstAssetAllocation[0].MDate.Count(); i++)
                                {
                                    DateTime dtFillDate = DateTime.FromOADate(lstAssetAllocation[0].MDate[i]);
                                    if (objHEEntities.PortfolioValueSet.Where(x => x.PortfolioSet.Id.Equals(intPortfolioID) && x.Date.Equals(dtFillDate)).Count() > 0)
                                    {
                                        objHEEntities.PortfolioValueSet.First(x => x.PortfolioSet.Id.Equals(intPortfolioID) && x.Date.Equals(dtFillDate)).RREB = (float)dblPortfolioTotalVal[i];
                                    }
                                    else
                                    {
                                        PortfolioValueSet objPortfolioValueSet = new PortfolioValueSet();
                                        objPortfolioValueSet.Date = dtFillDate;
                                        objPortfolioValueSet.RREB = (float)dblPortfolioTotalVal[i];
                                        objPortfolioValueSet.PortfolioSet = objHEEntities.PortfolioSet.First(x => x.Id.Equals(intPortfolioID));
                                        objHEEntities.AddToPortfolioValueSet(objPortfolioValueSet);
                                    }
                                }

                                if (isRotationHistorical)
                                objHEEntities.SaveChanges();
                            }
                            else
                            {
                                bool boolAddRecord = true;
                                int intDateCounter = lstAssetAllocation[0].MDate.Count() - 1;
                                bool flag = true;
                                while (flag)
                                {
                                    DateTime dtFillDate = DateTime.FromOADate(lstAssetAllocation[0].MDate[intDateCounter]);
                                    if (objHEEntities.PortfolioValueSet.Where(x => x.PortfolioSet.Id.Equals(intPortfolioID) && x.Date.Equals(dtFillDate)).Count() > 0)
                                    {
                                        boolAddRecord = false;
                                        if (objHEEntities.PortfolioValueSet.First(x => x.PortfolioSet.Id.Equals(intPortfolioID) && x.Date.Equals(dtFillDate)).RREB == null)
                                        {
                                            objHEEntities.PortfolioValueSet.First(x => x.PortfolioSet.Id.Equals(intPortfolioID) && x.Date.Equals(dtFillDate)).RREB = (float)dblPortfolioTotalVal[intDateCounter];
                                        }
                                        else
                                        {
                                          //  flag = false;
                                            //** change code for holiday
                                            DateTime dtPreviousDate = DateTime.FromOADate(lstAssetAllocation[0].MDate[intDateCounter - 1]);
                                            float dtPreviousPFValue = (float)dblPortfolioTotalVal[intDateCounter - 1];
                                            double dtDiff = Microsoft.VisualBasic.DateAndTime.DateDiff(DateInterval.Day, dtPreviousDate, dtFillDate, Microsoft.VisualBasic.FirstDayOfWeek.Sunday, FirstWeekOfYear.Jan1);
                                            if (dtDiff > 0)
                                            {
                                                
                                                while (dtPreviousDate < dtFillDate)
                                                {
                                                    if (!(dtPreviousDate.DayOfWeek.Equals(DayOfWeek.Saturday) || dtPreviousDate.DayOfWeek.Equals(DayOfWeek.Sunday)))
                                                    {
                                                        if (objHEEntities.PortfolioValueSet.Where(x => x.PortfolioSet.Id.Equals(intPortfolioID) && x.Date.Equals(dtPreviousDate)).Count() > 0)
                                                        {
                                                            if (objHEEntities.PortfolioValueSet.First(x => x.PortfolioSet.Id.Equals(intPortfolioID) && x.Date.Equals(dtPreviousDate)).RREB == null)
                                                            {
                                                                objHEEntities.PortfolioValueSet.First(x => x.PortfolioSet.Id.Equals(intPortfolioID) && x.Date.Equals(dtPreviousDate)).RREB = dtPreviousPFValue;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            PortfolioValueSet objPortfolioValueSet = new PortfolioValueSet();
                                                            objPortfolioValueSet.Date = dtPreviousDate;
                                                            objPortfolioValueSet.RREB = dtPreviousPFValue;
                                                            objPortfolioValueSet.PortfolioSet = objHEEntities.PortfolioSet.First(x => x.Id.Equals(intPortfolioID));
                                                            objHEEntities.AddToPortfolioValueSet(objPortfolioValueSet);
                                                        }
                                                    }

                                                    dtPreviousDate = dtPreviousDate.AddDays(1);
                                                }
                                            }
                                            flag = false;
                                            //**

                                        }
                                    }
                                    else
                                        boolAddRecord = true;

                                    if (boolAddRecord)
                                    {
                                        PortfolioValueSet objPortfolioValueSet = new PortfolioValueSet();
                                        objPortfolioValueSet.Date = dtFillDate;
                                        objPortfolioValueSet.RREB = (float)dblPortfolioTotalVal[intDateCounter];
                                        objPortfolioValueSet.PortfolioSet = objHEEntities.PortfolioSet.First(x => x.Id.Equals(intPortfolioID));
                                        objHEEntities.AddToPortfolioValueSet(objPortfolioValueSet);
                                    }

                                    intDateCounter--;
                                }
                            }
                            #endregion

                            if (isRotationHistorical)
                            objHEEntities.SaveChanges();
 
                            #region This region is used to fill the data into the PortfolioMethodSet table.

                            //If count is greater than zero indicates that record is present.
                            if (objHEEntities.PortfolioMethodSet.Where(x => x.PortfolioSet.Id.Equals(intPortfolioID)).Count() > 0)
                            {
                                //objHEEntities.PortfolioMethodSet.First(x => x.PortfolioSet.Id.Equals(intPortfolioID)).CAGR_RebRotation = Math.Pow((double)(dblPortfolioTotalVal[dblPortfolioTotalVal.Count() - 1] / dblPortfolioTotalVal[217]), (double)(1.00 / ((dblPortfolioTotalVal.Count() - 1 - 217.00) / 260.00))) - 1.0;
                                objHEEntities.PortfolioMethodSet.First(x => x.PortfolioSet.Id.Equals(intPortfolioID)).CAGR_RebRotation = Math.Pow((double)(dblPortfolioTotalVal[dblPortfolioTotalVal.Count() - 1] / dblPortfolioTotalVal[217]), (double)(1.00 / ((dblPortfolioTotalVal.Count() - 1 - 217.00) / (double)intTradingDays))) - 1.0;
                                objHEEntities.PortfolioMethodSet.First(x => x.PortfolioSet.Id.Equals(intPortfolioID)).Current_Value_RebRotation = dblPortfolioTotalVal[dblPortfolioTotalVal.Count() - 1];
                                objHEEntities.PortfolioMethodSet.First(x => x.PortfolioSet.Id.Equals(intPortfolioID)).Increase_RebRotation = dblYrRollingChange.Max();
                                objHEEntities.PortfolioMethodSet.First(x => x.PortfolioSet.Id.Equals(intPortfolioID)).DrawDownRebRotation = dblYrRollingChange.Min();
                                //objHEEntities.PortfolioMethodSet.First(x => x.PortfolioSet.Id.Equals(intPortfolioID)).Stdev_RebRotation = CommonUtility.STDev(dblPortfolioDailyChange.Skip(218).Select(x => (double)x).ToArray()) * (double)Math.Pow(260, 0.5);
                                objHEEntities.PortfolioMethodSet.First(x => x.PortfolioSet.Id.Equals(intPortfolioID)).Stdev_RebRotation = CommonUtility.STDev(dblPortfolioDailyChange.Skip(218).Select(x => (double)x).ToArray()) * (double)Math.Pow(intTradingDays, 0.5);
                                objHEEntities.PortfolioMethodSet.First(x => x.PortfolioSet.Id.Equals(intPortfolioID)).Rebalance_Date = DateTime.FromOADate(lstAssetAllocation[0].MDate[intMostRecentRebIndex]);
                            }
                            else
                            {
                                PortfolioMethodSet objMethodSet = new PortfolioMethodSet();
                                //objMethodSet.CAGR_RebRotation = Math.Pow((double)(dblPortfolioTotalVal[dblPortfolioTotalVal.Count() - 1] / dblPortfolioTotalVal[217]), (double)(1.00 / ((dblPortfolioTotalVal.Count() - 1 - 217.00) / 260.00))) - 1.0;
                                objMethodSet.CAGR_RebRotation = Math.Pow((double)(dblPortfolioTotalVal[dblPortfolioTotalVal.Count() - 1] / dblPortfolioTotalVal[217]), (double)(1.00 / ((dblPortfolioTotalVal.Count() - 1 - 217.00) / (double)intTradingDays))) - 1.0;
                                objMethodSet.Current_Value_RebRotation = dblPortfolioTotalVal[dblPortfolioTotalVal.Count() - 1];
                                objMethodSet.Increase_RebRotation = dblYrRollingChange.Max();
                                objMethodSet.DrawDownRebRotation = dblYrRollingChange.Min();
                                //objMethodSet.Stdev_RebRotation = CommonUtility.STDev(dblPortfolioDailyChange.Skip(218).Select(x => (double)x).ToArray()) * (double)Math.Pow(260, 0.5);
                                objMethodSet.Stdev_RebRotation = CommonUtility.STDev(dblPortfolioDailyChange.Skip(218).Select(x => (double)x).ToArray()) * (double)Math.Pow(intTradingDays, 0.5);
                                objMethodSet.PortfolioSet = objHEEntities.PortfolioSet.First(x => x.Id.Equals(intPortfolioID));
                                objMethodSet.Rebalance_Date = DateTime.FromOADate(lstAssetAllocation[0].MDate[intMostRecentRebIndex]);
                                objHEEntities.AddToPortfolioMethodSet(objMethodSet);
                            }

                            #endregion

                            if (isRotationHistorical)
                            objHEEntities.SaveChanges();
                            
                            #region This region is used for filling the data into the PortfolioAllocationInfo table.

                            //fill the data into the PortfolioAllocationInfo table.
                            foreach (var set in lstAssetAllocation)
                            {
                                double dblCurrentShare = 0;
                                double dblCurrentValue = 0;
                                foreach (var objSet in lstREColl)
                                {
                                    string[] strPairColl = objSet.SymbolPair.Split(":".ToCharArray());
                                    if (Convert.ToInt32(strPairColl[0]).Equals(set.MAssetSymbolId))
                                    {
                                        dblCurrentShare = objSet.FirstShare[objSet.FirstShare.Count() - 1];
                                    }
                                    else if (!string.IsNullOrEmpty(strPairColl[1]))
                                    {
                                        if (Convert.ToInt32(strPairColl[1]).Equals(set.MAssetSymbolId))
                                        {
                                            dblCurrentShare = objSet.SecondShare[objSet.SecondShare.Count() - 1];
                                        }
                                    }


                                    if (Convert.ToInt32(strPairColl[0]).Equals(set.MAssetSymbolId))
                                    {
                                        if (objSet.FirstShare[objSet.FirstShare.Count() - 1] != 0 && objSet.SecondShare[objSet.SecondShare.Count() - 1] != 0)
                                        {
                                            if (intDayCountQuarters[intDayCountQuarters.Count() - 1] > 1)
                                            {
                                                dblCurrentValue = dblCurrentShare * (double)set.MPriceTrend[set.MPriceTrend.Count() - 1];
                                                break;
                                            }
                                            else
                                            {
                                                dblCurrentValue = dblCurrentShare * (double)set.MPriceTrend[set.MPriceTrend.Count() - 1];
                                                break;
                                            }

                                        }
                                        else if (objSet.FirstShare[objSet.FirstShare.Count() - 1] == 0)
                                        {
                                            dblCurrentValue = 0;
                                            break;
                                        }
                                        else
                                        {
                                            dblCurrentValue = objSet.TotalValue[objSet.TotalValue.Count() - 1];
                                            break;
                                        }
                                    }
                                    else if (!string.IsNullOrEmpty(strPairColl[1]))
                                    {
                                        if (Convert.ToInt32(strPairColl[1]).Equals(set.MAssetSymbolId))
                                        {
                                            if (objSet.FirstShare[objSet.FirstShare.Count() - 1] != 0 && objSet.SecondShare[objSet.SecondShare.Count() - 1] != 0)
                                            {
                                                if (intDayCountQuarters[intDayCountQuarters.Count() - 1] > 1)
                                                {
                                                    dblCurrentValue = dblCurrentShare * (double)set.MPriceTrend[set.MPriceTrend.Count() - 1];
                                                    break;
                                                }
                                                else
                                                {
                                                    dblCurrentValue = dblCurrentShare * (double)set.MPriceTrend[set.MPriceTrend.Count() - 1];
                                                    break;
                                                }
                                            }
                                            else if (objSet.SecondShare[objSet.SecondShare.Count() - 1] == 0)
                                            {
                                                dblCurrentValue = 0;
                                                break;
                                            }
                                            else
                                            {
                                                dblCurrentValue = objSet.TotalValue[objSet.TotalValue.Count() - 1];
                                                break;
                                            }
                                        }
                                    }
                                }

                                if (objHEEntities.PorfolioAllocationInfo.Where(x => x.PortfolioSet.Id.Equals(intPortfolioID) && x.Allocation_Method.Equals("RREB") && x.AssetSymbolSet.Id.Equals(set.MAssetSymbolId)).Count() > 0)
                                {
                                    objHEEntities.PorfolioAllocationInfo.First(x => x.PortfolioSet.Id.Equals(intPortfolioID) && x.Allocation_Method.Equals("RREB") && x.AssetSymbolSet.Id.Equals(set.MAssetSymbolId)).Allocation_Method = "RREB";
                                    objHEEntities.PorfolioAllocationInfo.First(x => x.PortfolioSet.Id.Equals(intPortfolioID) && x.Allocation_Method.Equals("RREB") && x.AssetSymbolSet.Id.Equals(set.MAssetSymbolId)).Current_Value = dblCurrentValue;
                                    objHEEntities.PorfolioAllocationInfo.First(x => x.PortfolioSet.Id.Equals(intPortfolioID) && x.Allocation_Method.Equals("RREB") && x.AssetSymbolSet.Id.Equals(set.MAssetSymbolId)).Current_Weight = dblCurrentValue / dblPortfolioTotalVal[dblPortfolioTotalVal.Count() - 1];
                                    objHEEntities.PorfolioAllocationInfo.First(x => x.PortfolioSet.Id.Equals(intPortfolioID) && x.Allocation_Method.Equals("RREB") && x.AssetSymbolSet.Id.Equals(set.MAssetSymbolId)).Starting_Value = (double)set.MAssetValue[217];
                                    objHEEntities.PorfolioAllocationInfo.First(x => x.PortfolioSet.Id.Equals(intPortfolioID) && x.Allocation_Method.Equals("RREB") && x.AssetSymbolSet.Id.Equals(set.MAssetSymbolId)).Target_Weight = (double)set.MTargetWeight[set.MTargetWeight.Count() - 1];
                                    objHEEntities.PorfolioAllocationInfo.First(x => x.PortfolioSet.Id.Equals(intPortfolioID) && x.Allocation_Method.Equals("RREB") && x.AssetSymbolSet.Id.Equals(set.MAssetSymbolId)).Share_Price = (double)set.MPrice[217];
                                    objHEEntities.PorfolioAllocationInfo.First(x => x.PortfolioSet.Id.Equals(intPortfolioID) && x.Allocation_Method.Equals("RREB") && x.AssetSymbolSet.Id.Equals(set.MAssetSymbolId)).SharePriceTrend = (double)set.MPriceTrend[set.MPriceTrend.Count() - 1];
                                    objHEEntities.PorfolioAllocationInfo.First(x => x.PortfolioSet.Id.Equals(intPortfolioID) && x.Allocation_Method.Equals("RREB") && x.AssetSymbolSet.Id.Equals(set.MAssetSymbolId)).CurrentSharePrice = (double)set.MPrice[set.MPrice.Count() - 1];
                                    if (objHEEntities.PorfolioAllocationInfo.First(x => x.PortfolioSet.Id.Equals(intPortfolioID) && x.Allocation_Method.Equals("RREB") && x.AssetSymbolSet.Id.Equals(set.MAssetSymbolId)).CurrentShare != dblCurrentShare)
                                    {
                                        objHEEntities.PorfolioAllocationInfo.First(x => x.PortfolioSet.Id.Equals(intPortfolioID) && x.Allocation_Method.Equals("RREB") && x.AssetSymbolSet.Id.Equals(set.MAssetSymbolId)).CurrentShare = dblCurrentShare;
                                        objHEEntities.PorfolioAllocationInfo.First(x => x.PortfolioSet.Id.Equals(intPortfolioID) && x.Allocation_Method.Equals("RREB") && x.AssetSymbolSet.Id.Equals(set.MAssetSymbolId)).RotationDate = DateTime.FromOADate(set.MDate[set.MDate.Count() - 1]);
                                    }
                                    //objHEEntities.PorfolioAllocationInfo.First(x => x.PortfolioSet.Id.Equals(intPortfolioID) && x.Allocation_Method.Equals("RREB") && x.AssetSymbolSet.Id.Equals(set.MAssetSymbolId)).CurrentShare = dblCurrentShare;
                                    objHEEntities.PorfolioAllocationInfo.First(x => x.PortfolioSet.Id.Equals(intPortfolioID) && x.Allocation_Method.Equals("RREB") && x.AssetSymbolSet.Id.Equals(set.MAssetSymbolId)).TotalValue = dblPortfolioTotalVal[dblPortfolioTotalVal.Count() - 1];
                                }
                                else
                                {
                                    PorfolioAllocationInfo objPFAllocationSet = new PorfolioAllocationInfo();
                                    objPFAllocationSet.Allocation_Method = "RREB";
                                    objPFAllocationSet.Current_Value = dblCurrentValue;
                                    objPFAllocationSet.Current_Weight = dblCurrentValue / dblPortfolioTotalVal[dblPortfolioTotalVal.Count() - 1];
                                    objPFAllocationSet.Starting_Value = (double)set.MAssetValue[217];
                                    objPFAllocationSet.Target_Weight = (double)set.MTargetWeight[set.MTargetWeight.Count() - 1];
                                    objPFAllocationSet.Share_Price = (double)set.MPrice[217];
                                    objPFAllocationSet.SharePriceTrend = (double)set.MPriceTrend[set.MPriceTrend.Count() - 1];
                                    objPFAllocationSet.CurrentSharePrice = (double)set.MPrice[set.MPrice.Count() - 1];
                                    objPFAllocationSet.CurrentShare = dblCurrentShare;
                                    objPFAllocationSet.RotationDate = DateTime.FromOADate(set.MDate[set.MDate.Count() - 1]);
                                    objPFAllocationSet.TotalValue = dblPortfolioTotalVal[dblPortfolioTotalVal.Count() - 1];
                                    objPFAllocationSet.PortfolioSet = objHEEntities.PortfolioSet.First(x => x.Id.Equals(intPortfolioID));
                                    objPFAllocationSet.AssetSymbolSet = objHEEntities.AssetSymbolSet.First(x => x.Id.Equals(set.MAssetSymbolId));
                                    objHEEntities.AddToPorfolioAllocationInfo(objPFAllocationSet);
                                }
                            }

                            #endregion

                            if (isRotationHistorical)
                            objHEEntities.SaveChanges();
                            //#region Fill data into MostRecentRebalances Entity.

                            //foreach (var set in lstAssetAllocation)
                            //{
                            //    if (objHEEntities.MostRecentRebalances.Where(x => x.PortfolioSet.Id.Equals(intPortfolioID) && x.Allocation_Method.Equals("RREB") && x.AssetSymbolSet.Id.Equals(set.MAssetSymbolId)).Count() > 0)
                            //    {
                            //        objHEEntities.MostRecentRebalances.First(x => x.PortfolioSet.Id.Equals(intPortfolioID) && x.Allocation_Method.Equals("RREB") && x.AssetSymbolSet.Id.Equals(set.MAssetSymbolId)).Allocation_Method = "RREB";
                            //        objHEEntities.MostRecentRebalances.First(x => x.PortfolioSet.Id.Equals(intPortfolioID) && x.Allocation_Method.Equals("RREB") && x.AssetSymbolSet.Id.Equals(set.MAssetSymbolId)).RebalanceDate = DateTime.FromOADate(set.MDate[intMostRecentRebIndex]);
                            //        objHEEntities.MostRecentRebalances.First(x => x.PortfolioSet.Id.Equals(intPortfolioID) && x.Allocation_Method.Equals("RREB") && x.AssetSymbolSet.Id.Equals(set.MAssetSymbolId)).OldShare = (double)set.MAssetShares[intMostRecentRebIndex - 1];
                            //        objHEEntities.MostRecentRebalances.First(x => x.PortfolioSet.Id.Equals(intPortfolioID) && x.Allocation_Method.Equals("RREB") && x.AssetSymbolSet.Id.Equals(set.MAssetSymbolId)).NewShare = (double)set.MAssetShares[intMostRecentRebIndex];
                            //    }
                            //    else
                            //    {
                            //        MostRecentRebalances objMRR = new MostRecentRebalances();
                            //        objMRR.Allocation_Method = "RREB";
                            //        objMRR.RebalanceDate = DateTime.FromOADate(set.MDate[intMostRecentRebIndex]);
                            //        objMRR.NewShare = (double)set.MAssetShares[intMostRecentRebIndex];
                            //        objMRR.OldShare = (double)set.MAssetShares[intMostRecentRebIndex - 1];
                            //        objMRR.PortfolioSet = objHEEntities.PortfolioSet.First(x => x.Id.Equals(intPortfolioID));
                            //        objMRR.AssetSymbolSet = objHEEntities.AssetSymbolSet.First(x => x.Id.Equals(set.MAssetSymbolId));
                            //        objHEEntities.AddToMostRecentRebalances(objMRR);
                            //    }
                            //}

                            //#endregion


                            #region Fill data into MostRecentRebalances Entity.

                            foreach (var set in lstAssetAllocation)
                            {
                                // change 27-Dec-2010
                                double dblMostRecentOldShare = 0;
                                double dblMostRecentNewShare = 0;
                                foreach (var objSet in lstREColl)
                                {
                                    string[] strPairColl = objSet.SymbolPair.Split(":".ToCharArray());

                                    if (Convert.ToInt32(strPairColl[0]).Equals(set.MAssetSymbolId))
                                    {
                                        //annual rebalance of shares logic
                                        //dblMostRecentOldShare = objSet.FirstShare[intMostRecentRebIndex];
                                        //dblMostRecentNewShare = objSet.FirstShare[intMostRecentRebIndex + 1];
                                        dblMostRecentOldShare = objSet.FirstShare[intMostRecentRebIndex - 1];
                                        dblMostRecentNewShare = objSet.FirstShare[intMostRecentRebIndex];
                                        break;
                                    }
                                    else if (!string.IsNullOrEmpty(strPairColl[1]))
                                    {
                                        if (Convert.ToInt32(strPairColl[1]).Equals(set.MAssetSymbolId))
                                        {
                                            //annual rebalance of shares logic
                                            //dblMostRecentOldShare = objSet.SecondShare[intMostRecentRebIndex];
                                            //dblMostRecentNewShare = objSet.SecondShare[intMostRecentRebIndex + 1];
                                            dblMostRecentOldShare = objSet.SecondShare[intMostRecentRebIndex - 1];
                                            dblMostRecentNewShare = objSet.SecondShare[intMostRecentRebIndex];
                                            break;
                                        }
                                    }

                                }
                                //** change 27-Dec-2010
                                if (objHEEntities.MostRecentRebalances.Where(x => x.PortfolioSet.Id.Equals(intPortfolioID) && x.Allocation_Method.Equals("RREB") && x.AssetSymbolSet.Id.Equals(set.MAssetSymbolId)).Count() > 0)
                                {
                                    objHEEntities.MostRecentRebalances.First(x => x.PortfolioSet.Id.Equals(intPortfolioID) && x.Allocation_Method.Equals("RREB") && x.AssetSymbolSet.Id.Equals(set.MAssetSymbolId)).Allocation_Method = "RREB";
                                    objHEEntities.MostRecentRebalances.First(x => x.PortfolioSet.Id.Equals(intPortfolioID) && x.Allocation_Method.Equals("RREB") && x.AssetSymbolSet.Id.Equals(set.MAssetSymbolId)).RebalanceDate = DateTime.FromOADate(set.MDate[intMostRecentRebIndex]);
                                    //objHEEntities.MostRecentRebalances.First(x => x.PortfolioSet.Id.Equals(intPortfolioID) && x.Allocation_Method.Equals("RREB") && x.AssetSymbolSet.Id.Equals(set.MAssetSymbolId)).OldShare = (double)set.MAssetShares[intMostRecentRebIndex - 1];
                                    //objHEEntities.MostRecentRebalances.First(x => x.PortfolioSet.Id.Equals(intPortfolioID) && x.Allocation_Method.Equals("RREB") && x.AssetSymbolSet.Id.Equals(set.MAssetSymbolId)).NewShare = (double)set.MAssetShares[intMostRecentRebIndex];
                                    objHEEntities.MostRecentRebalances.First(x => x.PortfolioSet.Id.Equals(intPortfolioID) && x.Allocation_Method.Equals("RREB") && x.AssetSymbolSet.Id.Equals(set.MAssetSymbolId)).OldShare = (double)dblMostRecentOldShare;
                                    objHEEntities.MostRecentRebalances.First(x => x.PortfolioSet.Id.Equals(intPortfolioID) && x.Allocation_Method.Equals("RREB") && x.AssetSymbolSet.Id.Equals(set.MAssetSymbolId)).NewShare = (double)dblMostRecentNewShare;
                                }
                                else
                                {
                                    MostRecentRebalances objMRR = new MostRecentRebalances();
                                    objMRR.Allocation_Method = "RREB";
                                    objMRR.RebalanceDate = DateTime.FromOADate(set.MDate[intMostRecentRebIndex ]);
                                    //objMRR.NewShare = (double)set.MAssetShares[intMostRecentRebIndex];
                                    //objMRR.OldShare = (double)set.MAssetShares[intMostRecentRebIndex - 1];
                                    objMRR.NewShare = (double)dblMostRecentNewShare;
                                    objMRR.OldShare = (double)dblMostRecentOldShare;
                                    objMRR.PortfolioSet = objHEEntities.PortfolioSet.First(x => x.Id.Equals(intPortfolioID));
                                    objMRR.AssetSymbolSet = objHEEntities.AssetSymbolSet.First(x => x.Id.Equals(set.MAssetSymbolId));
                                    objHEEntities.AddToMostRecentRebalances(objMRR);
                                }
                            }

                            #endregion
                            
                            //Commit to reflect changes 
                            objHEEntities.SaveChanges();
                           
                            foreach (string strKey in dictRotationRebalance.Keys)
                            {
                                double[] dblTempData;
                                dictRotationRebalance.TryGetValue(strKey, out dblTempData);
                                if (dblTempData != null)
                                {
                                    string[] strAssetSymbols = strKey.Split(":".ToCharArray());
                                    if (string.IsNullOrEmpty(strAssetSymbols[1]))
                                    {
                                        int firstSymbol = Convert.ToInt32(strAssetSymbols[0]);
                                        //Entry for First Symbol.
                                        if (objHEEntities.AlertShare.Where(x => x.AssetSymbolSet.Id.Equals(firstSymbol) && x.Allocation_Method.Trim().ToLower().Equals("rreb") && x.PortfolioSet.Id.Equals(intPortfolioID)).Count() > 0)
                                        {
                                            objHEEntities.AlertShare.First(x => x.AssetSymbolSet.Id.Equals(firstSymbol) && x.Allocation_Method.Trim().ToLower().Equals("rreb") && x.PortfolioSet.Id.Equals(intPortfolioID)).OldShare = dblTempData[0];
                                            objHEEntities.AlertShare.First(x => x.AssetSymbolSet.Id.Equals(firstSymbol) && x.Allocation_Method.Trim().ToLower().Equals("rreb") && x.PortfolioSet.Id.Equals(intPortfolioID)).NewShare = dblTempData[1];
                                            objHEEntities.AlertShare.First(x => x.AssetSymbolSet.Id.Equals(firstSymbol) && x.Allocation_Method.Trim().ToLower().Equals("rreb") && x.PortfolioSet.Id.Equals(intPortfolioID)).RotationDate = DateTime.FromOADate(lstAssetAllocation[0].MDate[Convert.ToInt32(dblTempData[2])]);
                                        }
                                        else
                                        {
                                            AlertShare objAlertShare = new AlertShare();
                                            objAlertShare.OldShare = dblTempData[0];
                                            objAlertShare.NewShare = dblTempData[1];
                                            objAlertShare.Allocation_Method = "RREB";
                                            objAlertShare.RotationDate = DateTime.FromOADate(lstAssetAllocation[0].MDate[Convert.ToInt32(dblTempData[2])]);
                                            objAlertShare.PortfolioSet = objHEEntities.PortfolioSet.FirstOrDefault(x => x.Id.Equals(intPortfolioID));
                                            objAlertShare.AssetSymbolSet = objHEEntities.AssetSymbolSet.FirstOrDefault(x => x.Id.Equals(firstSymbol));
                                            objHEEntities.AddToAlertShare(objAlertShare);
                                        }
                                    }
                                    else
                                    {
                                        int firstSymbol = Convert.ToInt32(strAssetSymbols[0]);
                                        int secondSymbol = Convert.ToInt32(strAssetSymbols[1]);

                                        //Entry for First Symbol.
                                        if (objHEEntities.AlertShare.Where(x => x.AssetSymbolSet.Id.Equals(firstSymbol) && x.Allocation_Method.Trim().ToLower().Equals("rreb") && x.PortfolioSet.Id.Equals(intPortfolioID)).Count() > 0)
                                        {
                                            objHEEntities.AlertShare.First(x => x.AssetSymbolSet.Id.Equals(firstSymbol) && x.Allocation_Method.Trim().ToLower().Equals("rreb") && x.PortfolioSet.Id.Equals(intPortfolioID)).OldShare = dblTempData[0];
                                            objHEEntities.AlertShare.First(x => x.AssetSymbolSet.Id.Equals(firstSymbol) && x.Allocation_Method.Trim().ToLower().Equals("rreb") && x.PortfolioSet.Id.Equals(intPortfolioID)).NewShare = dblTempData[1];
                                            objHEEntities.AlertShare.First(x => x.AssetSymbolSet.Id.Equals(firstSymbol) && x.Allocation_Method.Trim().ToLower().Equals("rreb") && x.PortfolioSet.Id.Equals(intPortfolioID)).RotationDate = DateTime.FromOADate(lstAssetAllocation[0].MDate[Convert.ToInt32(dblTempData[4])]);
                                        }
                                        else
                                        {
                                            AlertShare objAlertShare = new AlertShare();
                                            objAlertShare.OldShare = dblTempData[0];
                                            objAlertShare.NewShare = dblTempData[1];
                                            objAlertShare.Allocation_Method = "RREB";
                                            objAlertShare.RotationDate = DateTime.FromOADate(lstAssetAllocation[0].MDate[Convert.ToInt32(dblTempData[4])]);
                                            objAlertShare.PortfolioSet = objHEEntities.PortfolioSet.FirstOrDefault(x => x.Id.Equals(intPortfolioID));
                                            objAlertShare.AssetSymbolSet = objHEEntities.AssetSymbolSet.FirstOrDefault(x => x.Id.Equals(firstSymbol));
                                            objHEEntities.AddToAlertShare(objAlertShare);
                                        }

                                        //Entry for Second Symbol.
                                        if (objHEEntities.AlertShare.Where(x => x.AssetSymbolSet.Id.Equals(secondSymbol) && x.Allocation_Method.Trim().ToLower().Equals("rreb") && x.PortfolioSet.Id.Equals(intPortfolioID)).Count() > 0)
                                        {
                                            objHEEntities.AlertShare.First(x => x.AssetSymbolSet.Id.Equals(secondSymbol) && x.Allocation_Method.Trim().ToLower().Equals("rreb") && x.PortfolioSet.Id.Equals(intPortfolioID)).OldShare = dblTempData[2];
                                            objHEEntities.AlertShare.First(x => x.AssetSymbolSet.Id.Equals(secondSymbol) && x.Allocation_Method.Trim().ToLower().Equals("rreb") && x.PortfolioSet.Id.Equals(intPortfolioID)).NewShare = dblTempData[3];
                                            objHEEntities.AlertShare.First(x => x.AssetSymbolSet.Id.Equals(secondSymbol) && x.Allocation_Method.Trim().ToLower().Equals("rreb") && x.PortfolioSet.Id.Equals(intPortfolioID)).RotationDate = DateTime.FromOADate(lstAssetAllocation[0].MDate[Convert.ToInt32(dblTempData[4])]);
                                        }
                                        else
                                        {
                                            AlertShare objAlertShare = new AlertShare();
                                            objAlertShare.OldShare = dblTempData[2];
                                            objAlertShare.NewShare = dblTempData[3];
                                            objAlertShare.Allocation_Method = "RREB";
                                            objAlertShare.RotationDate = DateTime.FromOADate(lstAssetAllocation[0].MDate[Convert.ToInt32(dblTempData[4])]);
                                            objAlertShare.PortfolioSet = objHEEntities.PortfolioSet.FirstOrDefault(x => x.Id.Equals(intPortfolioID));
                                            objAlertShare.AssetSymbolSet = objHEEntities.AssetSymbolSet.FirstOrDefault(x => x.Id.Equals(secondSymbol));
                                            objHEEntities.AddToAlertShare(objAlertShare);
                                        }
                                    }
                                }
                            }

                        }
                        #endregion
                    }
                }
                objHEEntities.SaveChanges();
            }
            catch (Exception)
            {

            }
        }


        private List<AssetAllocation> PerformRebalanceTrendOperationForNegativePair(string strPair, int intportFolioID, int intArraySize, DateTime dtFinalStartDate, out double[] decBHPortfolioValue)
        {
            List<AssetAllocation> lstAssetAllocation = new List<AssetAllocation>();
            decBHPortfolioValue = null;
            try
            {
                double[] decPortofolioDailyChange = null;
                double[] decYearRollChange = null;
                int[] intDayCountQuarters = null;


                int intAssetSymbolID = 0;

                #region Compute the Buy and Hold operation values.

                if (intArraySize != -1)
                {
                    lstAssetAllocation = new List<AssetAllocation>();
                    AssetAllocation objAssetAllocation = null;
                    decBHPortfolioValue = new double[intArraySize];
                    decPortofolioDailyChange = new double[intArraySize];
                    decYearRollChange = new double[intArraySize];
                    intDayCountQuarters = new int[intArraySize];

                    //Initialize the array by filling the date,price and pricetrend values.
                    foreach (string strPairVal in strPair.Split(":".ToCharArray()))
                    {
                        objAssetAllocation = new AssetAllocation(intArraySize, 1);
                        intAssetSymbolID = Convert.ToInt32(strPairVal);
                        objAssetAllocation.MAssetSymbolId = intAssetSymbolID;
                        objAssetAllocation.MDate = objHEEntities.AssetPriceSet.Where(x => x.PriceTrend != null && x.Date >= dtFinalStartDate && x.AssetSymbolSet.Id.Equals(intAssetSymbolID)).OrderBy(x => x.Date).Select(x => x.Date).ToArray().Select(x => x.ToOADate()).ToArray();
                        objAssetAllocation.MPrice = objHEEntities.AssetPriceSet.Where(x => x.PriceTrend != null && x.Date >= dtFinalStartDate && x.AssetSymbolSet.Id.Equals(intAssetSymbolID)).OrderBy(x => x.Date).Select(x => (double)x.Price).ToArray();
                        objAssetAllocation.MPriceTrend = objHEEntities.AssetPriceSet.Where(x => x.PriceTrend != null && x.Date >= dtFinalStartDate && x.AssetSymbolSet.Id.Equals(intAssetSymbolID)).OrderBy(x => x.Date).Select(x => (double)x.PriceTrend).ToArray();
                        objAssetAllocation.MAssetValue[217] = (double)objHEEntities.PortfolioContentSet.FirstOrDefault(x => x.PortfolioSet.Id.Equals(intportFolioID) && x.AssetSymbolSet.Id.Equals(intAssetSymbolID)).Value;
                        lstAssetAllocation.Add(objAssetAllocation);
                    }

                    //Re-calculate the price trend value with the normalized value.
                    foreach (var objSet in lstAssetAllocation)
                    {
                        //Get the normalization constant.
                        double decNorConstant = objSet.MPrice[217] / objSet.MPriceTrend[217];
                        if (decNorConstant != 0)
                        {
                            for (int intI = 0; intI <= objSet.MPriceTrend.Count() - 1; intI++)
                            {
                                objSet.MPriceTrend[intI] = objSet.MPriceTrend[intI] * decNorConstant;
                            }
                        }
                    }

                    //Iterate through the collection and calculate SMA and Slope.
                    foreach (var objSet in lstAssetAllocation)
                    {
                        int intSMASkipCount = 163;
                        int intSlopeSkipCount = 213;
                        for (int intI = 213; intI < objSet.MPrice.Count(); intI++)
                        {
                            objSet.MSMA[intI] = objSet.MPriceTrend.Skip(intSMASkipCount).Take(51).Average();
                            intSMASkipCount++;

                            if (intI >= 217)
                            {
                                objSet.MSlope[intI] = CommonUtility.Slope(objSet.MSMA.Skip(intSlopeSkipCount).Take(5).Select(x => (double)x).ToArray(), objSet.MDate.Skip(intSlopeSkipCount).Take(5).ToArray()) / (double)objSet.MSMA[intI];
                                intSlopeSkipCount++;
                            }
                        }
                    }
                    int intRollingCount = 261;
                    for (int intI = 217; intI <= lstAssetAllocation[0].MPrice.Count() - 1; intI++)
                    {
                        //Calculate the daily change.
                        foreach (var set in lstAssetAllocation)
                        {
                            set.MDailyChange[intI] = (set.MPrice[intI] - set.MPrice[intI - 1]) / set.MPrice[intI - 1];
                        }

                        if (intI == 217)
                        {
                            intDayCountQuarters[intI] = 1;

                            //Calculate Portfolio Value
                            decBHPortfolioValue[intI] = CommonUtility.GetBHPortfolioTotalValue(lstAssetAllocation, intI);

                            //Calculate Weight.
                            foreach (var set in lstAssetAllocation)
                            {
                                set.MWeight[intI] = set.MAssetValue[intI] / decBHPortfolioValue[intI];
                            }

                            //Calculate Target Weight.
                            foreach (var set in lstAssetAllocation)
                            {
                                set.MTargetWeight[intI] = set.MWeight[intI];
                            }

                            //Calculate Shares.
                            foreach (var set in lstAssetAllocation)
                            {
                                set.MAssetShares[intI] = decBHPortfolioValue[intI] * set.MWeight[intI] / set.MPriceTrend[intI];
                            }
                        }
                        else
                        {
                            //Day count quarters.
                            if (intDayCountQuarters[intI - 1] == 65)
                                intDayCountQuarters[intI] = 1;
                            else
                                intDayCountQuarters[intI] = intDayCountQuarters[intI - 1] + 1;

                            //Calculate TargetWeight.
                            foreach (var set in lstAssetAllocation)
                            {
                                set.MTargetWeight[intI] = set.MTargetWeight[intI - 1];
                            }

                            //Calculate Shares.
                            foreach (var set in lstAssetAllocation)
                            {
                                if (intDayCountQuarters[intI] < 65)
                                    set.MAssetShares[intI] = set.MAssetShares[intI - 1];
                                else
                                    set.MAssetShares[intI] = decBHPortfolioValue[intI - 1] * set.MTargetWeight[intI] / set.MPriceTrend[intI - 1];
                            }

                            //Calculate Asset value.
                            foreach (var set in lstAssetAllocation)
                            {
                                set.MAssetValue[intI] = set.MAssetShares[intI] * set.MPriceTrend[intI];
                            }

                            //Calculate TargetValue.
                            decBHPortfolioValue[intI] = CommonUtility.GetBHPortfolioTotalValue(lstAssetAllocation, intI);

                            //Calculate Weight.
                            foreach (var set in lstAssetAllocation)
                            {
                                if (intDayCountQuarters[intI] < 65)
                                    set.MWeight[intI] = set.MAssetValue[intI] / decBHPortfolioValue[intI];
                                else
                                    set.MWeight[intI] = set.MTargetWeight[intI];
                            }
                        }
                        //Calculate Portfolio daily change.
                        if (intI > 217)
                        {
                            decPortofolioDailyChange[intI] = (decBHPortfolioValue[intI] - decBHPortfolioValue[intI - 1]) / decBHPortfolioValue[intI - 1];
                        }

                        //Calculate Portfolio yearly rolling change.
                        if (intI >= 478)
                        {
                            decYearRollChange[intI] = (decBHPortfolioValue[intI] - decBHPortfolioValue[intI - intRollingCount]) / decBHPortfolioValue[intI - intRollingCount];
                        }
                    }
                }

                #endregion
            }
            catch (Exception)
            {

            }
            return lstAssetAllocation;
        }

        private void PerformRebalanceTrendOperationForNegativePair(int intI, double dblAggressiveness, int[] intDayCountQuarters, clsRECalculations strPairKey, Dictionary<string, int[]> dictFinalData, string[] strPairColl, Dictionary<string, double> dictPairDefaultValue, List<AssetAllocation> lstAssetAllocation, List<clsRECalculations> lstREColl, Dictionary<string, double[]> dictNegativePFvalue, Dictionary<string, double[]> dictRotationRebalance)
        {
            try
            {
                int[] dblWinPair;
                double[] decPFValue;

                //Get the win pair array for a symbol pair.
                dictFinalData.TryGetValue(strPairKey.SymbolPair, out dblWinPair);
                dictNegativePFvalue.TryGetValue(strPairKey.SymbolPair, out decPFValue);
                int intFirstAssetSymbol = Convert.ToInt32(strPairColl[0]);
                int intSecondAssetSymbol = Convert.ToInt32(strPairColl[1]);
                double dblDefaultValue = 0;
                dictPairDefaultValue.TryGetValue(strPairKey.SymbolPair, out dblDefaultValue);
                if (dblDefaultValue != 0)
                    strPairKey.TotalValue[217] = dblDefaultValue;
                else
                    strPairKey.TotalValue[217] = 5000;

                //Retrieve the Normalized Price Trend array`s for both the symbol`s.
                //decimal[] decFirstNormalizedPriceTrend = lstAssetAllocation.First(x => x.MAssetSymbolId.Equals(intFirstAssetSymbol)).MPriceTrend;
                //decimal[] decSecondNormalizedPriceTrend = lstAssetAllocation.First(x => x.MAssetSymbolId.Equals(intSecondAssetSymbol)).MPriceTrend;
                strPairKey.FirstShare[intI] = (double)lstAssetAllocation.First(x => x.MAssetSymbolId.Equals(intFirstAssetSymbol)).MAssetShares[intI];
                strPairKey.SecondShare[intI] = (double)lstAssetAllocation.First(x => x.MAssetSymbolId.Equals(intSecondAssetSymbol)).MAssetShares[intI];

                if (intI == 217)
                {
                    double[] dblTempArr = new double[5];
                    dblTempArr[0] = strPairKey.FirstShare[intI];
                    dblTempArr[1] = strPairKey.FirstShare[intI];
                    dblTempArr[2] = strPairKey.SecondShare[intI];
                    dblTempArr[3] = strPairKey.SecondShare[intI];
                    dblTempArr[4] = intI;
                    dictRotationRebalance.Add(strPairKey.SymbolPair, dblTempArr);
                }
                else
                {
                    //change 28-feb-2011
                    if (strPairKey.FirstShare[intI] != strPairKey.FirstShare[intI - 1] && strPairKey.SecondShare[intI] != strPairKey.SecondShare[intI - 1])
                    //change 28-Dec-2010
                    //if (dblWinPair[intI] != dblWinPair[intI - 1])
                    {
                        if (dictRotationRebalance.ContainsKey(strPairKey.SymbolPair))
                        {
                            double[] dblTempArr;
                            dictRotationRebalance.TryGetValue(strPairKey.SymbolPair, out dblTempArr);
                            dblTempArr[0] = dblTempArr[1];
                            dblTempArr[1] = strPairKey.FirstShare[intI];
                            dblTempArr[2] = dblTempArr[3];
                            dblTempArr[3] = strPairKey.SecondShare[intI];
                            dblTempArr[4] = intI;
                        }
                    }
                }


                strPairKey.TotalValue[intI] = (double)decPFValue[intI];
                ////calculate Total value.
                //if (intDayCountQuarters[intI] == 1)
                //    strPairKey.TotalValue[intI] = ((double)lstAssetAllocation.First(x => x.MAssetSymbolId.Equals(intFirstAssetSymbol)).MTargetWeight[intI] + (double)lstAssetAllocation.First(x => x.MAssetSymbolId.Equals(intSecondAssetSymbol)).MTargetWeight[intI]) * (GetPreviousDayTotalData(lstREColl, intI));
                //else
                //    strPairKey.TotalValue[intI] = strPairKey.FirstShare[intI] * (double)decFirstNormalizedPriceTrend[intI] + strPairKey.SecondShare[intI] * (double)decSecondNormalizedPriceTrend[intI];

                //Calculate Rotation Event.
                if (dblWinPair[intI] == dblWinPair[intI - 1])
                    strPairKey.RotationEvent[intI] = 0;
                else
                    strPairKey.RotationEvent[intI] = 1;

                //if (intI == 217)
                //{
                //    //Calculate Shares for both the symbol`s.
                //    if (dblWinPair[intI] == 1)
                //    {
                //        strPairKey.FirstShare[intI] = strPairKey.TotalValue[intI] * (1 - dblAggressiveness) / (double)decFirstNormalizedPriceTrend[intI];
                //        strPairKey.SecondShare[intI] = strPairKey.TotalValue[intI] * (dblAggressiveness) / (double)decSecondNormalizedPriceTrend[intI];
                //    }
                //    else
                //    {
                //        strPairKey.FirstShare[intI] = strPairKey.TotalValue[intI] * (dblAggressiveness) / (double)decFirstNormalizedPriceTrend[intI];
                //        strPairKey.SecondShare[intI] = strPairKey.TotalValue[intI] * (1 - dblAggressiveness) / (double)decSecondNormalizedPriceTrend[intI];
                //    }
                //}
                //else
                //{
                //    //calculate Total value.
                //    if (intDayCountQuarters[intI] == 1)
                //    {
                //        strPairKey.TotalValue[intI] = ((double)lstAssetAllocation.First(x => x.MAssetSymbolId.Equals(intFirstAssetSymbol)).MTargetWeight[intI] + (double)lstAssetAllocation.First(x => x.MAssetSymbolId.Equals(intSecondAssetSymbol)).MTargetWeight[intI]) * (GetPreviousDayTotalData(lstREColl, intI));
                //    }

                //    //Calculate Shares for both the symbol`s.
                //    if (intDayCountQuarters[intI] == 1)
                //    {
                //        strPairKey.SecondShare[intI] = strPairKey.TotalValue[intI] / strPairKey.TotalValue[intI - 1] * strPairKey.SecondShare[intI - 1];
                //        strPairKey.FirstShare[intI] = strPairKey.TotalValue[intI] / strPairKey.TotalValue[intI - 1] * strPairKey.FirstShare[intI - 1];
                //    }
                //    else if (dblWinPair[intI] == dblWinPair[intI - 1])
                //    {
                //        strPairKey.SecondShare[intI] = strPairKey.SecondShare[intI - 1];
                //        strPairKey.FirstShare[intI] = strPairKey.FirstShare[intI - 1];
                //    }
                //    else if (dblWinPair[intI] == 1)
                //    {
                //        strPairKey.SecondShare[intI] = strPairKey.TotalValue[intI - 1] * dblAggressiveness / (double)decSecondNormalizedPriceTrend[intI];
                //        strPairKey.FirstShare[intI] = strPairKey.TotalValue[intI - 1] * (1 - dblAggressiveness) / (double)decFirstNormalizedPriceTrend[intI];
                //    }
                //    else
                //    {
                //        strPairKey.SecondShare[intI] = strPairKey.TotalValue[intI - 1] * (1 - dblAggressiveness) / (double)decSecondNormalizedPriceTrend[intI];
                //        strPairKey.FirstShare[intI] = strPairKey.TotalValue[intI - 1] * (dblAggressiveness) / (double)decFirstNormalizedPriceTrend[intI];
                //    }

                //    //Calculate Total value.
                //    if (intDayCountQuarters[intI] > 1)
                //        strPairKey.TotalValue[intI] = strPairKey.FirstShare[intI] * (double)decFirstNormalizedPriceTrend[intI] + strPairKey.SecondShare[intI] * (double)decSecondNormalizedPriceTrend[intI];

                //    //Calculate Rotation Event.
                //    if (dblWinPair[intI] == dblWinPair[intI - 1])
                //        strPairKey.RotationEvent[intI] = 0;
                //    else
                //        strPairKey.RotationEvent[intI] = 1;
                //}
            }
            catch (Exception)
            {


            }
        }

        /// <summary>
        /// This method is used for calculating values for Buy and Hold allocation in which the pair yeilds a negative rotation.
        /// </summary>
        /// <param name="strPairColl"></param>
        /// <param name="strPairKey"></param>
        /// <param name="intArraySize"></param>
        /// <param name="dictFinalData"></param>
        /// <param name="dictPairDefaultValue"></param>
        /// <param name="lstAssetAllocation"></param>
        /// <param name="dblAggressiveness"></param>
        /// <returns></returns>
        private clsRECalculations FillPairBHDataForNegative(string[] strPairColl, string strPairKey, int intArraySize, Dictionary<string, int[]> dictFinalData, Dictionary<string, double> dictPairDefaultValue, List<AssetAllocation> lstAssetAllocation, double dblAggressiveness, Dictionary<string, double[]> dictBuyNHoldRotation)
        {
            clsRECalculations objRECal = new clsRECalculations(intArraySize);
            try
            {
                objRECal.SymbolPair = strPairKey;
                int[] dblWinPair;
                dictFinalData.TryGetValue(strPairKey, out dblWinPair);
                int intFirstAssetSymbol = Convert.ToInt32(strPairColl[0]);
                int intSecondAssetSymbol = Convert.ToInt32(strPairColl[1]);

                //Get the Normalized Price Trend values for both the symbols.
                double[] decFirstNormalizedPriceTrend = lstAssetAllocation.First(x => x.MAssetSymbolId.Equals(intFirstAssetSymbol)).MPriceTrend;
                double[] decSecondNormalizedPriceTrend = lstAssetAllocation.First(x => x.MAssetSymbolId.Equals(intSecondAssetSymbol)).MPriceTrend;


                //Get the Asset values for both the symbols.
                double[] decFirstAssetValues = lstAssetAllocation.FirstOrDefault(x => x.MAssetSymbolId.Equals(intFirstAssetSymbol)).MAssetValue;
                double[] decSecondAssetValues = lstAssetAllocation.FirstOrDefault(x => x.MAssetSymbolId.Equals(intSecondAssetSymbol)).MAssetValue;
                for (int intI = 217; intI <= lstAssetAllocation[0].MPrice.Count() - 1; intI++)
                {
                    if (intI == 217)
                    {
                        objRECal.FirstShare[intI] = lstAssetAllocation.First(x => x.MAssetSymbolId.Equals(intFirstAssetSymbol)).MAssetShares[intI];
                        objRECal.SecondShare[intI] = lstAssetAllocation.First(x => x.MAssetSymbolId.Equals(intSecondAssetSymbol)).MAssetShares[intI];
                        //objRECal.FirstShare[intI] = (double)(decFirstAssetValues[intI] / decFirstNormalizedPriceTrend[intI]);
                        //objRECal.SecondShare[intI] = (double)(decSecondAssetValues[intI] / decSecondNormalizedPriceTrend[intI]);

                        objRECal.TotalValue[intI] = decFirstAssetValues[intI] + decSecondAssetValues[intI];

                        double[] dblTempArr = new double[5];
                        dblTempArr[0] = objRECal.FirstShare[intI];
                        dblTempArr[1] = objRECal.FirstShare[intI];
                        dblTempArr[2] = objRECal.SecondShare[intI];
                        dblTempArr[3] = objRECal.SecondShare[intI];
                        dblTempArr[4] = intI;
                        dictBuyNHoldRotation.Add(strPairKey, dblTempArr);
                    }
                    else
                    {
                        //objRECal.FirstShare[intI] = objRECal.FirstShare[intI - 1];
                        //objRECal.SecondShare[intI] = objRECal.SecondShare[intI - 1];
                        objRECal.FirstShare[intI] = lstAssetAllocation.First(x => x.MAssetSymbolId.Equals(intFirstAssetSymbol)).MAssetShares[intI];
                        objRECal.SecondShare[intI] = lstAssetAllocation.First(x => x.MAssetSymbolId.Equals(intSecondAssetSymbol)).MAssetShares[intI];
                        //objRECal.TotalValue[intI] = objRECal.FirstShare[intI] * (double)decFirstNormalizedPriceTrend[intI] + objRECal.SecondShare[intI] * (double)decSecondNormalizedPriceTrend[intI];
                        objRECal.TotalValue[intI] = decFirstAssetValues[intI] + decSecondAssetValues[intI];
                        //if (objRECal.FirstShare[intI] != objRECal.FirstShare[intI - 1] && objRECal.SecondShare[intI] != objRECal.SecondShare[intI - 1])
                        //change 28-Dec-2010
                        if (dblWinPair[intI] != dblWinPair[intI - 1])
                        {
                            if (dictBuyNHoldRotation.ContainsKey(strPairKey))
                            {
                                double[] dblTempArr;
                                dictBuyNHoldRotation.TryGetValue(strPairKey, out dblTempArr);
                                dblTempArr[0] = dblTempArr[1];
                                dblTempArr[1] = objRECal.FirstShare[intI];
                                dblTempArr[2] = dblTempArr[3];
                                dblTempArr[3] = objRECal.SecondShare[intI];
                                dblTempArr[4] = intI;
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {

            }
            return objRECal;
        }

        /// <summary>
        /// This method is used to perform the Buy and Hold Trend engine operation.
        /// </summary>
        /// <param name="intSelectedIndex"></param>
        /// <param name="strSelectedValue"></param>
        /// <param name="dictFinalData"></param>
        /// <param name="dictArr"></param>
        /// <param name="isNegativePairtoZero"></param>
        private void PerformBuyHoldTrendRotation(int intSelectedIndex, string strSelectedValue, Dictionary<string, int[]> dictFinalData, Dictionary<string, double[]> dictArr, bool isNegativePairtoZero)
        {
            //System.Diagnostics.Debugger.Break();
            try
            {
                //If the passed selected index of the drop down is not -1.
                if (intSelectedIndex != -1)
                {
                    double[] decBHPortfolioValue = null;
                    double[] decPortofolioDailyChange = null;
                    double[] decYearRollChange = null;
                    List<clsRECalculations> lstREColl = new List<clsRECalculations>();
                    int intPortfolioID = Convert.ToInt32(strSelectedValue);
                    double dblAggressiveness = 0.9;
                    var objPortfolioSet = objHEEntities.PortfolioSet.FirstOrDefault(x => x.Id.Equals(intPortfolioID));
                    if (objPortfolioSet != null)
                        dblAggressiveness = objPortfolioSet.Aggressiveness;
                    List<AssetAllocation> lstAssetAllocation;

                    //Change code for schedular timing
                   // objPortfolioSet.REStartTime = DateTime.Now;

                    //Get the collection of all the symbol`s within the portfolio.
                    List<PortfolioContentSet> lstPortfolioContentSet = objHEEntities.PortfolioContentSet.Where(x => x.PortfolioSet.Id.Equals(intPortfolioID)).ToList();
                    if (lstPortfolioContentSet != null)
                    {
                        DateTime dtFinalStartDate = new DateTime(1, 1, 1);
                        int intCount = 0;
                        int intAssetSymbolID = 0;
                        int intArraySize = 0;

                        #region Get the earliest trading date for Portfolio

                        //Iterate through each object and get the earliest trading date.
                        foreach (var objSet in lstPortfolioContentSet)
                        {
                            DateTime dtStartDate = CommonUtility.GetStartDate(Convert.ToInt32(((EntityReference)(objSet.AssetSymbolSetReference)).EntityKey.EntityKeyValues[0].Value));
                            if (intCount == 0)
                            {
                                dtFinalStartDate = dtStartDate;
                                intAssetSymbolID = Convert.ToInt32(((EntityReference)(objSet.AssetSymbolSetReference)).EntityKey.EntityKeyValues[0].Value);
                                intCount++;
                            }
                            else
                            {
                                if (dtStartDate >= dtFinalStartDate)
                                {
                                    dtFinalStartDate = dtStartDate;
                                    intAssetSymbolID = Convert.ToInt32(((EntityReference)(objSet.AssetSymbolSetReference)).EntityKey.EntityKeyValues[0].Value);
                                }
                            }
                        }

                        #endregion

                        DateTime dtFinalLastDate = new DateTime(1, 1, 1);
                        int intLastDateCount = 0;
                        #region Get the earliest Last Trading Date from Assets
                        foreach (var objSet in lstPortfolioContentSet)
                        {
                            DateTime dtLastDate = CommonUtility.GetLastTradingDate(Convert.ToInt32(((EntityReference)(objSet.AssetSymbolSetReference)).EntityKey.EntityKeyValues[0].Value));
                            if (intLastDateCount == 0)
                            {
                                dtFinalLastDate = dtLastDate;
                                //intAssetSymbolID = Convert.ToInt32(((EntityReference)(objSet.AssetSymbolSetReference)).EntityKey.EntityKeyValues[0].Value);
                                intLastDateCount++;
                            }
                            else
                            {
                                if (dtLastDate < dtFinalLastDate)
                                {
                                    dtFinalLastDate = dtLastDate;
                                    // intAssetSymbolID = Convert.ToInt32(((EntityReference)(objSet.AssetSymbolSetReference)).EntityKey.EntityKeyValues[0].Value);
                                }
                            }



                        }
                        #endregion

                        #region Get the array size for this selected Portfolio from the AssetPriceSet table.

                        //Get the array size of the portfolio.
                        //intArraySize = CommonUtility.GetArraySize(intAssetSymbolID);
                        intArraySize = CommonUtility.GetArraySizeForPortfolioMethods(intAssetSymbolID, dtFinalLastDate);

                        #endregion



                        clsRECalculations objRECal;

                        #region Compute the Buy and Hold operation values.

                        if (intArraySize != -1)
                        {
                            lstAssetAllocation = new List<AssetAllocation>();
                            AssetAllocation objAssetAllocation = null;
                            decBHPortfolioValue = new double[intArraySize];
                            decPortofolioDailyChange = new double[intArraySize];
                            decYearRollChange = new double[intArraySize];
                            double decReturnCash = 0.0000961538461539792;

                            //Initialize the array by filling the date,price and pricetrend values.
                            foreach (var objSet in lstPortfolioContentSet)
                            {
                                objAssetAllocation = new AssetAllocation(intArraySize, 0);
                                intAssetSymbolID = Convert.ToInt32(((EntityReference)(objSet.AssetSymbolSetReference)).EntityKey.EntityKeyValues[0].Value);
                                objAssetAllocation.MAssetSymbolId = intAssetSymbolID;
                                objAssetAllocation.MDate = objHEEntities.AssetPriceSet.Where(x => x.PriceTrend != null && x.Date >= dtFinalStartDate && x.Date <= dtFinalLastDate && x.AssetSymbolSet.Id.Equals(intAssetSymbolID)).OrderBy(x => x.Date).Select(x => x.Date).ToArray().Select(x => x.ToOADate()).ToArray();
                                objAssetAllocation.MPrice = objHEEntities.AssetPriceSet.Where(x => x.PriceTrend != null && x.Date >= dtFinalStartDate && x.Date <= dtFinalLastDate && x.AssetSymbolSet.Id.Equals(intAssetSymbolID)).OrderBy(x => x.Date).Select(x => (double)x.Price).ToArray();
                                objAssetAllocation.MPriceTrend = objHEEntities.AssetPriceSet.Where(x => x.PriceTrend != null && x.Date >= dtFinalStartDate && x.Date <= dtFinalLastDate && x.AssetSymbolSet.Id.Equals(intAssetSymbolID)).OrderBy(x => x.Date).Select(x => (double)x.PriceTrend).ToArray();
// Change 13-Dec-2010
                                objAssetAllocation.MState = objHEEntities.AssetPriceSet.Where(x => x.PriceTrend != null && x.Date >= dtFinalStartDate && x.Date <= dtFinalLastDate && x.AssetSymbolSet.Id.Equals(intAssetSymbolID)).OrderBy(x => x.Date).Select(x => x.FilterState).ToArray();
                                objAssetAllocation.MAssetValue[217] = (double)objSet.Value;
                                lstAssetAllocation.Add(objAssetAllocation);
                            }

                            //Calculate normalization constant.
                            double decNormConstant = 0;
                            foreach (var objSet in lstAssetAllocation)
                            {
                                decNormConstant = objSet.MPrice[217] / objSet.MPriceTrend[217];
                                if (decNormConstant != 0)
                                {
                                    for (int intI = 163; intI < intArraySize; intI++)
                                    {
                                        objSet.MPriceTrend[intI] = objSet.MPriceTrend[intI] * decNormConstant;
                                    }
                                }
                            }

                            //Iterate through the collection and calculate SMA and Slope.
                            foreach (var objSet in lstAssetAllocation)
                            {
                                int intSMASkipCount = 163;
                                int intSlopeSkipCount = 213;
                                for (int intI = 213; intI < objSet.MPrice.Count(); intI++)
                                {
                                    objSet.MSMA[intI] = objSet.MPriceTrend.Skip(intSMASkipCount).Take(51).Average();
                                    intSMASkipCount++;

                                    if (intI >= 217)
                                    {
                                        objSet.MSlope[intI] = CommonUtility.Slope(objSet.MSMA.Skip(intSlopeSkipCount).Take(5).Select(x => (double)x).ToArray(), objSet.MDate.Skip(intSlopeSkipCount).Take(5).ToArray()) / (double)objSet.MSMA[intI];
                                        intSlopeSkipCount++;
                                    }
                                }
                            }
                            int intRollingCount = 261;
                            for (int intI = 217; intI <= lstAssetAllocation[0].MPrice.Count() - 1; intI++)
                            {
                                foreach (var set in lstAssetAllocation)
                                {
                                    //Calculate Portfolio daily change.
// Change 13-Dec-2010
                                    if (set.MState[intI].ToLower().Equals("in"))
                                        set.MDailyChange[intI] = (set.MPriceTrend[intI] - set.MPriceTrend[intI - 1]) / set.MPriceTrend[intI - 1];
                                    else
                                        if (set.MState[intI-1].ToLower().Equals("out"))
                                            set.MDailyChange[intI] = decReturnCash;
                                        else
                                            set.MDailyChange[intI] = (set.MPriceTrend[intI] - set.MPriceTrend[intI - 1]) / set.MPriceTrend[intI - 1];
                                }

                                if (intI == 217)
                                {
                                    //Calculate Portfolio TargetValue.
                                    decBHPortfolioValue[intI] = CommonUtility.GetBHPortfolioTotalValue(lstAssetAllocation, intI);

                                    //Calculate Weight.
                                    foreach (var set in lstAssetAllocation)
                                    {
                                        set.MWeight[intI] = set.MAssetValue[intI] / decBHPortfolioValue[intI];
                                    }

                                    //Calculate Asset shares.
                                    foreach (var set in lstAssetAllocation)
                                    {
                                        set.MAssetShares[intI] = decBHPortfolioValue[intI] * set.MWeight[intI] / set.MPriceTrend[intI];
                                    }
                                }
                                else
                                {
                                    //change 28-feb-2011
                                    //Calculate Asset shares.
                                    foreach (var set in lstAssetAllocation)
                                    {
                                        set.MAssetShares[intI] = set.MAssetShares[intI - 1];
                                    }

                                    //calculate Asset value.
                                    foreach (var set in lstAssetAllocation)
                                    {
                                        //change 28-feb-2011
                                       // set.MAssetValue[intI] = set.MAssetValue[intI - 1] * (1 + set.MDailyChange[intI - 1]);
                                        set.MAssetValue[intI] = set.MAssetShares[intI] * set.MPriceTrend[intI];
                                    }

                                    //Calculate Portfolio TargetValue.
                                    decBHPortfolioValue[intI] = CommonUtility.GetBHPortfolioTotalValue(lstAssetAllocation, intI);

                                    //Calculate Weight.
                                    foreach (var set in lstAssetAllocation)
                                    {
                                        set.MWeight[intI] = set.MAssetValue[intI] / decBHPortfolioValue[intI];
                                    }

                                    
                                }

                                //Calculate Portfolio daily change.
                                if (intI > 217)
                                {
                                    decPortofolioDailyChange[intI] = (decBHPortfolioValue[intI] - decBHPortfolioValue[intI - 1]) / decBHPortfolioValue[intI - 1];
                                }

                                //Calculate yearly rolling change.
                                if (intI >= 478)
                                {
                                    decYearRollChange[intI] = (decBHPortfolioValue[intI] - decBHPortfolioValue[intI - intRollingCount]) / decBHPortfolioValue[intI - intRollingCount];
                                }
                            }

                            Dictionary<string, double> dictPairDefaultValue = new Dictionary<string, double>();

                            foreach (string strPairKey in dictFinalData.Keys)
                            {
                                string[] strPairColl = strPairKey.Split(":".ToCharArray());
                                int intFirstAssetSymbol = Convert.ToInt32(strPairColl[0]);
                                double dblFirstVal = (double)objHEEntities.PortfolioContentSet.FirstOrDefault(x => x.AssetSymbolSet.Id.Equals(intFirstAssetSymbol) && x.PortfolioSet.Id.Equals(intPortfolioID)).Value;

                                int intSecondAssetSymbol = 0;
                                double dblSecondVal = 0;
                                if (!string.IsNullOrEmpty(strPairColl[1]))
                                {
                                    intSecondAssetSymbol = Convert.ToInt32(strPairColl[1]);
                                    dblSecondVal = (double)objHEEntities.PortfolioContentSet.FirstOrDefault(x => x.AssetSymbolSet.Id.Equals(intSecondAssetSymbol) && x.PortfolioSet.Id.Equals(intPortfolioID)).Value;
                                }

                                dictPairDefaultValue.Add(strPairKey, dblFirstVal + dblSecondVal);
                            }
                            bool isNegative = false;
                            Dictionary<string, double[]> dictBuyNHoldRotation = new Dictionary<string, double[]>();
                            foreach (string strPairKey in dictFinalData.Keys)
                            {
                                isNegative = false;
                                string[] strPairColl = strPairKey.Split(":".ToCharArray());

                                double[] dblArr = null;

                                dictArr.TryGetValue(strPairKey, out dblArr);

                                if (dblArr != null)
                                {
                                    double dblVal = (((dblArr[1] - dblArr[0]) / dblArr[0]) * 100);
                                    if (dblVal < 0)
                                        isNegative = true;
                                }

                                //Temporarly set the isNegative flag to false if the check box for setting the negative value to zero is not checked.
                                //if (!isNegativePairtoZero)
                                //    isNegative = false;

                                if (string.IsNullOrEmpty(strPairColl[1]))
                                {
                                    objRECal = FillOddPairForBHData(strPairColl, strPairKey, intArraySize, dictFinalData, dictPairDefaultValue, lstAssetAllocation, dblAggressiveness, dictBuyNHoldRotation);
                                    lstREColl.Add(objRECal);
                                }
                                else if (isNegative && isNegativePairtoZero)
                                {
                                    objRECal = FillPairBHDataForNegative(strPairColl, strPairKey, intArraySize, dictFinalData, dictPairDefaultValue, lstAssetAllocation, dblAggressiveness, dictBuyNHoldRotation);
                                    lstREColl.Add(objRECal);
                                }
                                else if (isNegative)
                                {
                                    objRECal = PerformBuyAndHoldTrendForNegativePair(intPortfolioID, dtFinalStartDate, strPairColl, strPairKey, intArraySize, dictFinalData, dictPairDefaultValue, dblAggressiveness, dictBuyNHoldRotation);
                                    lstREColl.Add(objRECal);
                                }
                                else
                                {
                                    objRECal = FillEvenPairForBHData(strPairColl, strPairKey, intArraySize, dictFinalData, dictPairDefaultValue, lstAssetAllocation, dblAggressiveness, dictBuyNHoldRotation);
                                    lstREColl.Add(objRECal);
                                }
                            }

                            int[] intUniqueRotations = new int[intArraySize];
                            double[] dblPortfolioTotalVal = new double[intArraySize];
                            double[] dblPortfolioDailyChange = new double[intArraySize];
                            double[] dblYrRollingChange = new double[intArraySize];
                            int intTempUnqRot = 0;
                            double dblTempTotalVal = 0;
                            for (int intI = 217; intI < lstAssetAllocation[0].MPrice.Count(); intI++)
                            {
                                dblTempTotalVal = 0;
                                foreach (var objSet in lstREColl)
                                {
                                    dblTempTotalVal = dblTempTotalVal + objSet.TotalValue[intI];
                                }
                                dblPortfolioTotalVal[intI] = dblTempTotalVal;
                                if (intI > 217)
                                {
                                    intTempUnqRot = 0;
                                    foreach (var objSet in lstREColl)
                                    {
                                        intTempUnqRot = intTempUnqRot + objSet.RotationEvent[intI];
                                    }
                                    if (intTempUnqRot == 2)
                                        intUniqueRotations[intI] = 1;
                                    else
                                        intUniqueRotations[intI] = intTempUnqRot;

                                    dblPortfolioDailyChange[intI] = (dblPortfolioTotalVal[intI] - dblPortfolioTotalVal[intI - 1]) / dblPortfolioTotalVal[intI - 1];
                                }
                                if (intI >= 478)
                                {
                                    dblYrRollingChange[intI] = (dblPortfolioTotalVal[intI] - dblPortfolioTotalVal[intI - intRollingCount]) / dblPortfolioTotalVal[intI - intRollingCount];
                                }
                            }

                            //double rebalancesPerYearChange = ((double)intUniqueRotations.Sum()) / (intUniqueRotations.Count() - 1 - 217.0) * 260.0;
                            double rebalancesPerYearChange = ((double)intUniqueRotations.Sum()) / (intUniqueRotations.Count() - 1 - 217.0) * (double)intTradingDays;
                            double dblEndingValue = (double)dblPortfolioTotalVal[dblPortfolioTotalVal.Count() - 1];
                            //double dblCAGR = (Math.Pow((double)(dblPortfolioTotalVal[dblPortfolioTotalVal.Count() - 1] / dblPortfolioTotalVal[217]), (double)(1.00 / ((dblPortfolioTotalVal.Count() - 1 - 217.00) / 260.00))) - 1.0) * 100.0;
                            double dblCAGR = (Math.Pow((double)(dblPortfolioTotalVal[dblPortfolioTotalVal.Count() - 1] / dblPortfolioTotalVal[217]), (double)(1.00 / ((dblPortfolioTotalVal.Count() - 1 - 217.00) / (double)intTradingDays))) - 1.0) * 100.0;
                            //double dblSTDev = CommonUtility.STDev(dblPortfolioDailyChange.Skip(218).Select(x => (double)x).ToArray()) * (double)Math.Pow(260, 0.5) * 100;
                            double dblSTDev = CommonUtility.STDev(dblPortfolioDailyChange.Skip(218).Select(x => (double)x).ToArray()) * (double)Math.Pow(intTradingDays, 0.5) * 100;
                            double dblIncrease = dblYrRollingChange.Max();
                            double dblDrawnDown = dblYrRollingChange.Min();
                            double dblSharpe = (dblCAGR - 0.025) / dblSTDev;

                            objHEEntities.PortfolioSet.First(x => x.Id.Equals(intPortfolioID)).BeginDate = DateTime.FromOADate(lstAssetAllocation[0].MDate[217]);
                            objHEEntities.PortfolioSet.First(x => x.Id.Equals(intPortfolioID)).Current_Date = DateTime.FromOADate(lstAssetAllocation[0].MDate[lstAssetAllocation[0].MDate.Count() - 1]);

                            //HttpContext.Current.Response.Write("<br><br><br>Buy and Hold Trend Rotation Results.<br>");
                            //HttpContext.Current.Response.Write("---------------------------------------------------------------------------------------------<br>");
                            //HttpContext.Current.Response.Write("Ending Value &nbsp;&nbsp;&nbsp;CAGR BH&nbsp;&nbsp;&nbsp;STDEV&nbsp;&nbsp;&nbsp;Increase&nbsp;&nbsp;&nbsp;DrawDown&nbsp;&nbsp;&nbsp;Sharpe<br>");
                            //HttpContext.Current.Response.Write("---------------------------------------------------------------------------------------------<br>");
                            //HttpContext.Current.Response.Write(String.Format("{0:0.000}", dblEndingValue) + "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;" + String.Format("{0:0.000}", dblCAGR) + "&nbsp;&nbsp;&nbsp;&nbsp;" + String.Format("{0:0.000}", dblSTDev) + "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;" + String.Format("{0:0.000}", dblIncrease) + "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;" + String.Format("{0:0.000}", dblDrawnDown) + "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;" + String.Format("{0:0.000}", dblSharpe));
                            //HttpContext.Current.Response.Write("<br>---------------------------------------------------------------------------------------------");

                            #region Fill Data in PortfolioValueSet Entity
                            if (isRotationHistorical || isDividendOrSplit)
                            {
                                for (int i = 217; i < lstAssetAllocation[0].MDate.Count(); i++)
                                {
                                    DateTime dtFillDate = DateTime.FromOADate(lstAssetAllocation[0].MDate[i]);
                                    if (objHEEntities.PortfolioValueSet.Where(x => x.PortfolioSet.Id.Equals(intPortfolioID) && x.Date.Equals(dtFillDate)).Count() > 0)
                                    {
                                        objHEEntities.PortfolioValueSet.First(x => x.PortfolioSet.Id.Equals(intPortfolioID) && x.Date.Equals(dtFillDate)).RBH = (float)dblPortfolioTotalVal[i];
                                    }
                                    else
                                    {
                                        PortfolioValueSet objPortfolioValueSet = new PortfolioValueSet();
                                        objPortfolioValueSet.Date = dtFillDate;
                                        objPortfolioValueSet.RBH = (float)dblPortfolioTotalVal[i];
                                        objPortfolioValueSet.PortfolioSet = objHEEntities.PortfolioSet.First(x => x.Id.Equals(intPortfolioID));
                                        objHEEntities.AddToPortfolioValueSet(objPortfolioValueSet);
                                    }
                                }
                                
                                if (isRotationHistorical)
                                objHEEntities.SaveChanges();

                            }
                            else
                            {
                                bool boolAddRecord = true;
                                int intDateCounter = lstAssetAllocation[0].MDate.Count() - 1;
                                bool flag = true;
                                while (flag)
                                {
                                    DateTime dtFillDate = DateTime.FromOADate(lstAssetAllocation[0].MDate[intDateCounter]);
                                    if (objHEEntities.PortfolioValueSet.Where(x => x.PortfolioSet.Id.Equals(intPortfolioID) && x.Date.Equals(dtFillDate)).Count() > 0)
                                    {
                                        boolAddRecord = false;
                                        if (objHEEntities.PortfolioValueSet.First(x => x.PortfolioSet.Id.Equals(intPortfolioID) && x.Date.Equals(dtFillDate)).RBH == null)
                                        {
                                            objHEEntities.PortfolioValueSet.First(x => x.PortfolioSet.Id.Equals(intPortfolioID) && x.Date.Equals(dtFillDate)).RBH = (float)dblPortfolioTotalVal[intDateCounter];
                                        }
                                        else
                                        {
                                           // flag = false;

                                            //**Change code for holidays
                                            DateTime dtPreviousDate = DateTime.FromOADate(lstAssetAllocation[0].MDate[intDateCounter - 1]);
                                            float dtPreviousPFValue = (float)dblPortfolioTotalVal[intDateCounter - 1];
                                            double dtDiff = Microsoft.VisualBasic.DateAndTime.DateDiff(DateInterval.Day, dtPreviousDate, dtFillDate, Microsoft.VisualBasic.FirstDayOfWeek.Sunday, FirstWeekOfYear.Jan1);
                                            if (dtDiff > 0)
                                            {

                                                while (dtPreviousDate < dtFillDate)
                                                {
                                                    if (!(dtPreviousDate.DayOfWeek.Equals(DayOfWeek.Saturday) || dtPreviousDate.DayOfWeek.Equals(DayOfWeek.Sunday)))
                                                    {
                                                        if (objHEEntities.PortfolioValueSet.Where(x => x.PortfolioSet.Id.Equals(intPortfolioID) && x.Date.Equals(dtPreviousDate)).Count() > 0)
                                                        {
                                                            if (objHEEntities.PortfolioValueSet.First(x => x.PortfolioSet.Id.Equals(intPortfolioID) && x.Date.Equals(dtPreviousDate)).RBH == null)
                                                            {
                                                                objHEEntities.PortfolioValueSet.First(x => x.PortfolioSet.Id.Equals(intPortfolioID) && x.Date.Equals(dtPreviousDate)).RBH = dtPreviousPFValue;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            PortfolioValueSet objPortfolioValueSet = new PortfolioValueSet();
                                                            objPortfolioValueSet.Date = dtPreviousDate;
                                                            objPortfolioValueSet.RBH = dtPreviousPFValue;
                                                            objPortfolioValueSet.PortfolioSet = objHEEntities.PortfolioSet.First(x => x.Id.Equals(intPortfolioID));
                                                            objHEEntities.AddToPortfolioValueSet(objPortfolioValueSet);
                                                        }
                                                    }

                                                    dtPreviousDate = dtPreviousDate.AddDays(1);
                                                }
                                            }
                                            flag = false;
                                            //**
                                        }
                                    }
                                    else
                                        boolAddRecord = true;

                                    if (boolAddRecord)
                                    {
                                        PortfolioValueSet objPortfolioValueSet = new PortfolioValueSet();
                                        objPortfolioValueSet.Date = dtFillDate;
                                        objPortfolioValueSet.RBH = (float)dblPortfolioTotalVal[intDateCounter];
                                        objPortfolioValueSet.PortfolioSet = objHEEntities.PortfolioSet.First(x => x.Id.Equals(intPortfolioID));
                                        objHEEntities.AddToPortfolioValueSet(objPortfolioValueSet);
                                    }

                                    intDateCounter--;
                                }
                            }
                            #endregion
                           
                            if (isRotationHistorical)
                            objHEEntities.SaveChanges();
                            
                            #region This region is used to fill the data into the PortfolioMethodSet table.

                            //If count is greater than zero indicates that record is present.
                            if (objHEEntities.PortfolioMethodSet.Where(x => x.PortfolioSet.Id.Equals(intPortfolioID)).Count() > 0)
                            {
                                //objHEEntities.PortfolioMethodSet.First(x => x.PortfolioSet.Id.Equals(intPortfolioID)).CAGR_BHRotation = Math.Pow((double)(dblPortfolioTotalVal[dblPortfolioTotalVal.Count() - 1] / dblPortfolioTotalVal[217]), (double)(1.00 / ((dblPortfolioTotalVal.Count() - 1 - 217.00) / 260.00))) - 1.0;
                                objHEEntities.PortfolioMethodSet.First(x => x.PortfolioSet.Id.Equals(intPortfolioID)).CAGR_BHRotation = Math.Pow((double)(dblPortfolioTotalVal[dblPortfolioTotalVal.Count() - 1] / dblPortfolioTotalVal[217]), (double)(1.00 / ((dblPortfolioTotalVal.Count() - 1 - 217.00) / (double)intTradingDays))) - 1.0;
                                objHEEntities.PortfolioMethodSet.First(x => x.PortfolioSet.Id.Equals(intPortfolioID)).Current_Value_BHRotation = dblPortfolioTotalVal[dblPortfolioTotalVal.Count() - 1];
                                objHEEntities.PortfolioMethodSet.First(x => x.PortfolioSet.Id.Equals(intPortfolioID)).Increase_BHRotation = dblYrRollingChange.Max();
                                objHEEntities.PortfolioMethodSet.First(x => x.PortfolioSet.Id.Equals(intPortfolioID)).DrawDownBHRotation = dblYrRollingChange.Min();
                                //objHEEntities.PortfolioMethodSet.First(x => x.PortfolioSet.Id.Equals(intPortfolioID)).Stdev_BHRotation = CommonUtility.STDev(dblPortfolioDailyChange.Skip(218).Select(x => (double)x).ToArray()) * (double)Math.Pow(260, 0.5);
                                objHEEntities.PortfolioMethodSet.First(x => x.PortfolioSet.Id.Equals(intPortfolioID)).Stdev_BHRotation = CommonUtility.STDev(dblPortfolioDailyChange.Skip(218).Select(x => (double)x).ToArray()) * (double)Math.Pow(intTradingDays, 0.5);
                                objHEEntities.PortfolioMethodSet.First(x => x.PortfolioSet.Id.Equals(intPortfolioID)).RebalancesPerYearAvg = rebalancesPerYearChange;
                            }
                            else
                            {
                                PortfolioMethodSet objMethodSet = new PortfolioMethodSet();
                                //objMethodSet.CAGR_BHRotation = Math.Pow((double)(dblPortfolioTotalVal[dblPortfolioTotalVal.Count() - 1] / dblPortfolioTotalVal[217]), (double)(1.00 / ((dblPortfolioTotalVal.Count() - 1 - 217.00) / 260.00))) - 1.0;
                                objMethodSet.CAGR_BHRotation = Math.Pow((double)(dblPortfolioTotalVal[dblPortfolioTotalVal.Count() - 1] / dblPortfolioTotalVal[217]), (double)(1.00 / ((dblPortfolioTotalVal.Count() - 1 - 217.00) / (double)intTradingDays))) - 1.0;
                                objMethodSet.Current_Value_BHRotation = dblPortfolioTotalVal[dblPortfolioTotalVal.Count() - 1];
                                objMethodSet.Increase_BHRotation = dblYrRollingChange.Max();
                                objMethodSet.DrawDownBHRotation = dblYrRollingChange.Min();
                                //objMethodSet.Stdev_BHRotation = CommonUtility.STDev(dblPortfolioDailyChange.Skip(218).Select(x => (double)x).ToArray()) * (double)Math.Pow(260, 0.5);
                                objMethodSet.Stdev_BHRotation = CommonUtility.STDev(dblPortfolioDailyChange.Skip(218).Select(x => (double)x).ToArray()) * (double)Math.Pow(intTradingDays, 0.5);
                                objMethodSet.PortfolioSet = objHEEntities.PortfolioSet.First(x => x.Id.Equals(intPortfolioID));
                                objMethodSet.RebalancesPerYearAvg = rebalancesPerYearChange;
                                objHEEntities.AddToPortfolioMethodSet(objMethodSet);
                            }

                            #endregion
                            
                            if (isRotationHistorical)
                            objHEEntities.SaveChanges();

                            #region This region is used for filling the data into the PortfolioAllocationInfo table.

                            //System.Diagnostics.Debugger.Break();

                            foreach (var set in lstAssetAllocation)
                            {
                                double dblCurrentShare = 0;
                                double dblCurrentValue = 0;
                                // bool istrue = false;
                                foreach (var objSet in lstREColl)
                                {
                                    string[] strPairColl = objSet.SymbolPair.Split(":".ToCharArray());
                                    if (Convert.ToInt32(strPairColl[0]).Equals(set.MAssetSymbolId))
                                    {
                                        dblCurrentShare = objSet.FirstShare[objSet.FirstShare.Count() - 1];
                                        //istrue = true;
                                    }
                                    else if (!string.IsNullOrEmpty(strPairColl[1]))
                                    {
                                        if (Convert.ToInt32(strPairColl[1]).Equals(set.MAssetSymbolId))
                                        {
                                            dblCurrentShare = objSet.SecondShare[objSet.SecondShare.Count() - 1];
                                            //istrue = true;
                                        }
                                    }

                                    if (Convert.ToInt32(strPairColl[0]).Equals(set.MAssetSymbolId))
                                    {
                                        if (objSet.FirstShare[objSet.FirstShare.Count() - 1] != 0 && objSet.SecondShare[objSet.SecondShare.Count() - 1] != 0)
                                        {
                                            dblCurrentValue = dblCurrentShare * (double)set.MPriceTrend[set.MPriceTrend.Count() - 1];
                                            break;
                                        }
                                        else if (objSet.FirstShare[objSet.FirstShare.Count() - 1] == 0)
                                        {
                                            dblCurrentValue = 0;
                                            break;
                                        }
                                        else
                                        {
                                            dblCurrentValue = objSet.TotalValue[objSet.TotalValue.Count() - 1];
                                            break;
                                        }
                                    }
                                    else if (!string.IsNullOrEmpty(strPairColl[1]))
                                    {
                                        if (Convert.ToInt32(strPairColl[1]).Equals(set.MAssetSymbolId))
                                        {
                                            if (objSet.FirstShare[objSet.FirstShare.Count() - 1] != 0 && objSet.SecondShare[objSet.SecondShare.Count() - 1] != 0)
                                            {
                                                dblCurrentValue = dblCurrentShare * (double)set.MPriceTrend[set.MPriceTrend.Count() - 1];
                                                break;
                                            }
                                            else if (objSet.SecondShare[objSet.SecondShare.Count() - 1] == 0)
                                            {
                                                dblCurrentValue = 0;
                                                break;
                                            }
                                            else
                                            {
                                                dblCurrentValue = objSet.TotalValue[objSet.TotalValue.Count() - 1];
                                                break;
                                            }
                                        }
                                    }
                                }



                                if (objHEEntities.PorfolioAllocationInfo.Where(x => x.PortfolioSet.Id.Equals(intPortfolioID) && x.Allocation_Method.Equals("RBH") && x.AssetSymbolSet.Id.Equals(set.MAssetSymbolId)).Count() > 0)
                                {
                                    objHEEntities.PorfolioAllocationInfo.First(x => x.PortfolioSet.Id.Equals(intPortfolioID) && x.Allocation_Method.Equals("RBH") && x.AssetSymbolSet.Id.Equals(set.MAssetSymbolId)).Allocation_Method = "RBH";
                                    objHEEntities.PorfolioAllocationInfo.First(x => x.PortfolioSet.Id.Equals(intPortfolioID) && x.Allocation_Method.Equals("RBH") && x.AssetSymbolSet.Id.Equals(set.MAssetSymbolId)).Current_Value = dblCurrentValue;
                                    objHEEntities.PorfolioAllocationInfo.First(x => x.PortfolioSet.Id.Equals(intPortfolioID) && x.Allocation_Method.Equals("RBH") && x.AssetSymbolSet.Id.Equals(set.MAssetSymbolId)).Current_Weight = dblCurrentValue / (double)dblPortfolioTotalVal[dblPortfolioTotalVal.Count() - 1];
                                    objHEEntities.PorfolioAllocationInfo.First(x => x.PortfolioSet.Id.Equals(intPortfolioID) && x.Allocation_Method.Equals("RBH") && x.AssetSymbolSet.Id.Equals(set.MAssetSymbolId)).Starting_Value = (double)set.MAssetValue[217];
                                    objHEEntities.PorfolioAllocationInfo.First(x => x.PortfolioSet.Id.Equals(intPortfolioID) && x.Allocation_Method.Equals("RBH") && x.AssetSymbolSet.Id.Equals(set.MAssetSymbolId)).Target_Weight = 0;
                                    objHEEntities.PorfolioAllocationInfo.First(x => x.PortfolioSet.Id.Equals(intPortfolioID) && x.Allocation_Method.Equals("RBH") && x.AssetSymbolSet.Id.Equals(set.MAssetSymbolId)).Share_Price = (double)set.MPrice[217];
                                    objHEEntities.PorfolioAllocationInfo.First(x => x.PortfolioSet.Id.Equals(intPortfolioID) && x.Allocation_Method.Equals("RBH") && x.AssetSymbolSet.Id.Equals(set.MAssetSymbolId)).SharePriceTrend = (double)set.MPriceTrend[set.MPriceTrend.Count() - 1];
                                    objHEEntities.PorfolioAllocationInfo.First(x => x.PortfolioSet.Id.Equals(intPortfolioID) && x.Allocation_Method.Equals("RBH") && x.AssetSymbolSet.Id.Equals(set.MAssetSymbolId)).CurrentSharePrice = (double)set.MPrice[set.MPrice.Count() - 1];
                                    //objHEEntities.PorfolioAllocationInfo.First(x => x.PortfolioSet.Id.Equals(intPortfolioID) && x.Allocation_Method.Equals("RBH") && x.AssetSymbolSet.Id.Equals(set.MAssetSymbolId)).CurrentShare = dblCurrentShare;
                                    objHEEntities.PorfolioAllocationInfo.First(x => x.PortfolioSet.Id.Equals(intPortfolioID) && x.Allocation_Method.Equals("RBH") && x.AssetSymbolSet.Id.Equals(set.MAssetSymbolId)).TotalValue = dblPortfolioTotalVal[dblPortfolioTotalVal.Count() - 1];
                                    if (objHEEntities.PorfolioAllocationInfo.First(x => x.PortfolioSet.Id.Equals(intPortfolioID) && x.Allocation_Method.Equals("RBH") && x.AssetSymbolSet.Id.Equals(set.MAssetSymbolId)).CurrentShare != dblCurrentShare)
                                    {
                                        objHEEntities.PorfolioAllocationInfo.First(x => x.PortfolioSet.Id.Equals(intPortfolioID) && x.Allocation_Method.Equals("RBH") && x.AssetSymbolSet.Id.Equals(set.MAssetSymbolId)).CurrentShare = dblCurrentShare;
                                        objHEEntities.PorfolioAllocationInfo.First(x => x.PortfolioSet.Id.Equals(intPortfolioID) && x.Allocation_Method.Equals("RBH") && x.AssetSymbolSet.Id.Equals(set.MAssetSymbolId)).RotationDate = DateTime.FromOADate(set.MDate[set.MDate.Count() - 1]);
                                    }
                                }
                                else
                                {
                                    PorfolioAllocationInfo objPFAllocationSet = new PorfolioAllocationInfo();
                                    objPFAllocationSet.Allocation_Method = "RBH";
                                    objPFAllocationSet.Current_Value = dblCurrentValue;
                                    objPFAllocationSet.Current_Weight = dblCurrentValue / (double)dblPortfolioTotalVal[dblPortfolioTotalVal.Count() - 1];
                                    objPFAllocationSet.Starting_Value = (double)set.MAssetValue[217];
                                    objPFAllocationSet.Target_Weight = 0;
                                    objPFAllocationSet.Share_Price = (double)set.MPrice[217];
                                    objPFAllocationSet.SharePriceTrend = (double)set.MPriceTrend[set.MPriceTrend.Count() - 1];
                                    objPFAllocationSet.CurrentSharePrice = (double)set.MPrice[set.MPrice.Count() - 1];
                                    objPFAllocationSet.CurrentShare = dblCurrentShare;
                                    objPFAllocationSet.RotationDate = DateTime.FromOADate(set.MDate[set.MDate.Count() - 1]);
                                    objPFAllocationSet.TotalValue = dblPortfolioTotalVal[dblPortfolioTotalVal.Count() - 1];
                                    objPFAllocationSet.PortfolioSet = objHEEntities.PortfolioSet.First(x => x.Id.Equals(intPortfolioID));
                                    objPFAllocationSet.AssetSymbolSet = objHEEntities.AssetSymbolSet.First(x => x.Id.Equals(set.MAssetSymbolId));
                                }
                            }

                            #endregion

                            //Commit to reflect changes
                            objHEEntities.SaveChanges();
                            
                            foreach (string strKey in dictBuyNHoldRotation.Keys)
                            {
                                double[] dblTempData;
                                dictBuyNHoldRotation.TryGetValue(strKey, out dblTempData);
                                if (dblTempData != null)
                                {
                                    string[] strAssetSymbols = strKey.Split(":".ToCharArray());
                                    if (string.IsNullOrEmpty(strAssetSymbols[1]))
                                    {
                                        int firstSymbol = Convert.ToInt32(strAssetSymbols[0]);
                                        //Entry for First Symbol.
                                        if (objHEEntities.AlertShare.Where(x => x.AssetSymbolSet.Id.Equals(firstSymbol) && x.Allocation_Method.Trim().ToLower().Equals("rbh") && x.PortfolioSet.Id.Equals(intPortfolioID)).Count() > 0)
                                        {
                                            objHEEntities.AlertShare.First(x => x.AssetSymbolSet.Id.Equals(firstSymbol) && x.Allocation_Method.Trim().ToLower().Equals("rbh") && x.PortfolioSet.Id.Equals(intPortfolioID)).OldShare = dblTempData[0];
                                            objHEEntities.AlertShare.First(x => x.AssetSymbolSet.Id.Equals(firstSymbol) && x.Allocation_Method.Trim().ToLower().Equals("rbh") && x.PortfolioSet.Id.Equals(intPortfolioID)).NewShare = dblTempData[1];
                                            objHEEntities.AlertShare.First(x => x.AssetSymbolSet.Id.Equals(firstSymbol) && x.Allocation_Method.Trim().ToLower().Equals("rbh") && x.PortfolioSet.Id.Equals(intPortfolioID)).RotationDate = DateTime.FromOADate(lstAssetAllocation[0].MDate[Convert.ToInt32(dblTempData[2])]);
                                        }
                                        else
                                        {
                                            AlertShare objAlertShare = new AlertShare();
                                            objAlertShare.OldShare = dblTempData[0];
                                            objAlertShare.NewShare = dblTempData[1];
                                            objAlertShare.Allocation_Method = "RBH";
                                            objAlertShare.RotationDate = DateTime.FromOADate(lstAssetAllocation[0].MDate[Convert.ToInt32(dblTempData[2])]);
                                            objAlertShare.PortfolioSet = objHEEntities.PortfolioSet.FirstOrDefault(x => x.Id.Equals(intPortfolioID));
                                            objAlertShare.AssetSymbolSet = objHEEntities.AssetSymbolSet.FirstOrDefault(x => x.Id.Equals(firstSymbol));
                                            objHEEntities.AddToAlertShare(objAlertShare);
                                        }
                                    }
                                    else
                                    {
                                        int firstSymbol = Convert.ToInt32(strAssetSymbols[0]);
                                        int secondSymbol = Convert.ToInt32(strAssetSymbols[1]);

                                        //Entry for First Symbol.
                                        if (objHEEntities.AlertShare.Where(x => x.AssetSymbolSet.Id.Equals(firstSymbol) && x.Allocation_Method.Trim().ToLower().Equals("rbh") && x.PortfolioSet.Id.Equals(intPortfolioID)).Count() > 0)
                                        {
                                            objHEEntities.AlertShare.First(x => x.AssetSymbolSet.Id.Equals(firstSymbol) && x.Allocation_Method.Trim().ToLower().Equals("rbh") && x.PortfolioSet.Id.Equals(intPortfolioID)).OldShare = dblTempData[0];
                                            objHEEntities.AlertShare.First(x => x.AssetSymbolSet.Id.Equals(firstSymbol) && x.Allocation_Method.Trim().ToLower().Equals("rbh") && x.PortfolioSet.Id.Equals(intPortfolioID)).NewShare = dblTempData[1];
                                            objHEEntities.AlertShare.First(x => x.AssetSymbolSet.Id.Equals(firstSymbol) && x.Allocation_Method.Trim().ToLower().Equals("rbh") && x.PortfolioSet.Id.Equals(intPortfolioID)).RotationDate = DateTime.FromOADate(lstAssetAllocation[0].MDate[Convert.ToInt32(dblTempData[4])]);
                                        }
                                        else
                                        {
                                            AlertShare objAlertShare = new AlertShare();
                                            objAlertShare.OldShare = dblTempData[0];
                                            objAlertShare.NewShare = dblTempData[1];
                                            objAlertShare.Allocation_Method = "RBH";
                                            objAlertShare.RotationDate = DateTime.FromOADate(lstAssetAllocation[0].MDate[Convert.ToInt32(dblTempData[4])]);
                                            objAlertShare.PortfolioSet = objHEEntities.PortfolioSet.FirstOrDefault(x => x.Id.Equals(intPortfolioID));
                                            objAlertShare.AssetSymbolSet = objHEEntities.AssetSymbolSet.FirstOrDefault(x => x.Id.Equals(firstSymbol));
                                            objHEEntities.AddToAlertShare(objAlertShare);
                                        }

                                        //Entry for Second Symbol.
                                        if (objHEEntities.AlertShare.Where(x => x.AssetSymbolSet.Id.Equals(secondSymbol) && x.Allocation_Method.Trim().ToLower().Equals("rbh") && x.PortfolioSet.Id.Equals(intPortfolioID)).Count() > 0)
                                        {
                                            objHEEntities.AlertShare.First(x => x.AssetSymbolSet.Id.Equals(secondSymbol) && x.Allocation_Method.Trim().ToLower().Equals("rbh") && x.PortfolioSet.Id.Equals(intPortfolioID)).OldShare = dblTempData[2];
                                            objHEEntities.AlertShare.First(x => x.AssetSymbolSet.Id.Equals(secondSymbol) && x.Allocation_Method.Trim().ToLower().Equals("rbh") && x.PortfolioSet.Id.Equals(intPortfolioID)).NewShare = dblTempData[3];
                                            objHEEntities.AlertShare.First(x => x.AssetSymbolSet.Id.Equals(secondSymbol) && x.Allocation_Method.Trim().ToLower().Equals("rbh") && x.PortfolioSet.Id.Equals(intPortfolioID)).RotationDate = DateTime.FromOADate(lstAssetAllocation[0].MDate[Convert.ToInt32(dblTempData[4])]);
                                        }
                                        else
                                        {
                                            AlertShare objAlertShare = new AlertShare();
                                            objAlertShare.OldShare = dblTempData[2];
                                            objAlertShare.NewShare = dblTempData[3];
                                            objAlertShare.Allocation_Method = "RBH";
                                            objAlertShare.RotationDate = DateTime.FromOADate(lstAssetAllocation[0].MDate[Convert.ToInt32(dblTempData[4])]);
                                            objAlertShare.PortfolioSet = objHEEntities.PortfolioSet.FirstOrDefault(x => x.Id.Equals(intPortfolioID));
                                            objAlertShare.AssetSymbolSet = objHEEntities.AssetSymbolSet.FirstOrDefault(x => x.Id.Equals(secondSymbol));
                                            objHEEntities.AddToAlertShare(objAlertShare);
                                        }
                                    }
                                }
                            }

                           
                        }

                        #endregion
                    }
                    //Change code for schedular timing
                    //objPortfolioSet.REEndTime = DateTime.Now;
                }
                objHEEntities.SaveChanges();
            }
            catch (Exception)
            {

            }
        }

        private clsRECalculations PerformBuyAndHoldTrendForNegativePair(int intPortfolioID, DateTime dtFinalStartDate, string[] strPairColl, string strPairKey, int intArraySize, Dictionary<string, int[]> dictFinalData, Dictionary<string, double> dictPairDefaultValue, double dblAggressiveness, Dictionary<string, double[]> dictBuyNHoldRotation)
        {
            clsRECalculations objRECal = new clsRECalculations(intArraySize);
            try
            {
                List<AssetAllocation> lstAssetAllocation = new List<AssetAllocation>();
                AssetAllocation objAssetAllocation = null;
                double[] decBHPortfolioValue = new double[intArraySize];
                double[] decPortofolioDailyChange = new double[intArraySize];
                double[] decYearRollChange = new double[intArraySize];
                double decReturnCash = 0.0000961538461539792;

                //Initialize the array by filling the date,price and pricetrend values.
                foreach (string strPairID in strPairKey.Split(":".ToCharArray()))
                {
                    objAssetAllocation = new AssetAllocation(intArraySize, 0);
                    int intAssetSymbolID = Convert.ToInt32(strPairID);
                    objAssetAllocation.MAssetSymbolId = intAssetSymbolID;
                    objAssetAllocation.MDate = objHEEntities.AssetPriceSet.Where(x => x.PriceTrend != null && x.Date >= dtFinalStartDate && x.AssetSymbolSet.Id.Equals(intAssetSymbolID)).OrderBy(x => x.Date).Select(x => x.Date).ToArray().Select(x => x.ToOADate()).ToArray();
                    objAssetAllocation.MPrice = objHEEntities.AssetPriceSet.Where(x => x.PriceTrend != null && x.Date >= dtFinalStartDate && x.AssetSymbolSet.Id.Equals(intAssetSymbolID)).OrderBy(x => x.Date).Select(x => (double)x.Price).ToArray();
                    objAssetAllocation.MPriceTrend = objHEEntities.AssetPriceSet.Where(x => x.PriceTrend != null && x.Date >= dtFinalStartDate && x.AssetSymbolSet.Id.Equals(intAssetSymbolID)).OrderBy(x => x.Date).Select(x => (double)x.PriceTrend).ToArray();
                    objAssetAllocation.MState = objHEEntities.AssetPriceSet.Where(x => x.PriceTrend != null && x.Date >= dtFinalStartDate && x.AssetSymbolSet.Id.Equals(intAssetSymbolID)).OrderBy(x => x.Date).Select(x => x.FilterState).ToArray();
                    objAssetAllocation.MAssetValue[217] = (double)objHEEntities.PortfolioContentSet.FirstOrDefault(x => x.AssetSymbolSet.Id.Equals(intAssetSymbolID) && x.PortfolioSet.Id.Equals(intPortfolioID)).Value;
                    lstAssetAllocation.Add(objAssetAllocation);
                }

                //Calculate normalization constant.
                double decNormConstant = 0;
                foreach (var objSet in lstAssetAllocation)
                {
                    //CHANGE 28-FEB-2011
                    decNormConstant = objSet.MPrice[217] / objSet.MPriceTrend[217];
                    if (decNormConstant != 0)
                    {
                        for (int intI = 163; intI < intArraySize; intI++)
                        {
                            objSet.MPriceTrend[intI] = objSet.MPriceTrend[intI] * decNormConstant;
                        }
                    }
                }

                //Iterate through the collection and calculate SMA and Slope.
                foreach (var objSet in lstAssetAllocation)
                {
                    int intSMASkipCount = 163;
                    int intSlopeSkipCount = 213;
                    for (int intI = 213; intI < objSet.MPrice.Count(); intI++)
                    {
                        objSet.MSMA[intI] = objSet.MPriceTrend.Skip(intSMASkipCount).Take(51).Average();
                        intSMASkipCount++;

                        if (intI >= 217)
                        {
                            objSet.MSlope[intI] = CommonUtility.Slope(objSet.MSMA.Skip(intSlopeSkipCount).Take(5).Select(x => (double)x).ToArray(), objSet.MDate.Skip(intSlopeSkipCount).Take(5).ToArray()) / (double)objSet.MSMA[intI];
                            intSlopeSkipCount++;
                        }
                    }
                }
                int intRollingCount = 261;
                for (int intI = 217; intI <= lstAssetAllocation[0].MPrice.Count() - 1; intI++)
                {
                    foreach (var set in lstAssetAllocation)
                    {
                        //Calculate Portfolio daily change.
                        if (set.MState[intI].ToLower().Equals("in"))
                            set.MDailyChange[intI] = (set.MPrice[intI] - set.MPrice[intI - 1]) / set.MPrice[intI - 1];
                        else
                            set.MDailyChange[intI] = decReturnCash;
                    }

                    if (intI == 217)
                    {
                        //Calculate Portfolio TargetValue.
                        decBHPortfolioValue[intI] = CommonUtility.GetBHPortfolioTotalValue(lstAssetAllocation, intI);

                        //Calculate Weight.
                        foreach (var set in lstAssetAllocation)
                        {
                            set.MWeight[intI] = set.MAssetValue[intI] / decBHPortfolioValue[intI];
                        }

                        //Calculate Asset shares.
                        foreach (var set in lstAssetAllocation)
                        {
                            //change 28-Feb-2011
                            set.MAssetShares[intI] = decBHPortfolioValue[intI] * set.MWeight[intI] / set.MPriceTrend[intI];
                        }
                    }
                    else
                    {
                        //change 28-Feb-2011
                        //Calculate Asset shares.
                        foreach (var set in lstAssetAllocation)
                        {
                            set.MAssetShares[intI] = set.MAssetShares[intI - 1];
                        }

                        //calculate Asset value.
                        foreach (var set in lstAssetAllocation)
                        {
                            //change 28-Feb-2011
                            //set.MAssetValue[intI] = set.MAssetValue[intI - 1] * (1 + set.MDailyChange[intI - 1]);
                            set.MAssetValue[intI] = set.MAssetShares[intI] * set.MPriceTrend[intI];
                        }

                        //Calculate Portfolio TargetValue.
                        decBHPortfolioValue[intI] = CommonUtility.GetBHPortfolioTotalValue(lstAssetAllocation, intI);

                        //Calculate Weight.
                        foreach (var set in lstAssetAllocation)
                        {
                            set.MWeight[intI] = set.MAssetValue[intI] / decBHPortfolioValue[intI];
                        }

                        
                        
                    }

                    //Calculate Portfolio daily change.
                    if (intI > 217)
                    {
                        decPortofolioDailyChange[intI] = (decBHPortfolioValue[intI] - decBHPortfolioValue[intI - 1]) / decBHPortfolioValue[intI - 1];
                    }

                    //Calculate yearly rolling change.
                    if (intI >= 478)
                    {
                        decYearRollChange[intI] = (decBHPortfolioValue[intI] - decBHPortfolioValue[intI - intRollingCount]) / decBHPortfolioValue[intI - intRollingCount];
                    }
                }

                objRECal.SymbolPair = strPairKey;
                int[] dblWinPair;
                dictFinalData.TryGetValue(strPairKey, out dblWinPair);
                int intFirstAssetSymbol = Convert.ToInt32(strPairColl[0]);
                int intSecondAssetSymbol = Convert.ToInt32(strPairColl[1]);
                double dblDefaultValue = 0;
                dictPairDefaultValue.TryGetValue(strPairKey, out dblDefaultValue);
                if (dblDefaultValue != 0)
                    objRECal.TotalValue[217] = dblDefaultValue;
                else
                    objRECal.TotalValue[217] = 5000;

                //Get the Normalized Price Trend values.
                double[] decFirstNormalizedPriceTrend = lstAssetAllocation.First(x => x.MAssetSymbolId.Equals(intFirstAssetSymbol)).MPriceTrend;
                double[] decSecondNormalizedPriceTrend = lstAssetAllocation.First(x => x.MAssetSymbolId.Equals(intSecondAssetSymbol)).MPriceTrend;

                objRECal.FirstShare = lstAssetAllocation.First(x => x.MAssetSymbolId.Equals(intFirstAssetSymbol)).MAssetShares.Select(x => (double)x).ToArray();
                objRECal.SecondShare = lstAssetAllocation.First(x => x.MAssetSymbolId.Equals(intSecondAssetSymbol)).MAssetShares.Select(x => (double)x).ToArray();

                for (int i = 217; i < objRECal.FirstShare.Count(); i++)
                {
                    if (i == 217)
                    {
                        double[] dblTempArr = new double[5];
                        dblTempArr[0] = objRECal.FirstShare[i];
                        dblTempArr[1] = objRECal.FirstShare[i];
                        dblTempArr[2] = objRECal.SecondShare[i];
                        dblTempArr[3] = objRECal.SecondShare[i];
                        dblTempArr[4] = i;
                        dictBuyNHoldRotation.Add(strPairKey, dblTempArr);
                    }
                    else
                    {
                        //if (objRECal.FirstShare[i] != objRECal.FirstShare[i - 1] && objRECal.SecondShare[i] != objRECal.SecondShare[i - 1])
                        //change 28-Dec-2010
                        if (dblWinPair[i] != dblWinPair[i-1])
                        {
                            if (dictBuyNHoldRotation.ContainsKey(strPairKey))
                            {
                                double[] dblTempArr;
                                dictBuyNHoldRotation.TryGetValue(strPairKey, out dblTempArr);
                                dblTempArr[0] = dblTempArr[1];
                                dblTempArr[1] = objRECal.FirstShare[i];
                                dblTempArr[2] = dblTempArr[3];
                                dblTempArr[3] = objRECal.SecondShare[i];
                                dblTempArr[4] = i;
                            }
                        }
                    }
                }

                // objRECal.TotalValue = decBHPortfolioValue.Select(x => (double)x).ToArray();
                for (int intI = 218; intI <= lstAssetAllocation[0].MPrice.Count() - 1; intI++)
                {
                    objRECal.TotalValue[intI] = objRECal.FirstShare[intI] * (double)decFirstNormalizedPriceTrend[intI] + (double)decSecondNormalizedPriceTrend[intI] * objRECal.SecondShare[intI];

                    if (dblWinPair[intI] == dblWinPair[intI - 1])
                        objRECal.RotationEvent[intI] = 0;
                    else
                        objRECal.RotationEvent[intI] = 1;
                    //if (intI == 217)
                    //{
                    //    //Calculate Shares.
                    //    if (dblWinPair[intI] == 1)
                    //    {
                    //        objRECal.FirstShare[intI] = objRECal.TotalValue[intI] * (1 - dblAggressiveness) / (double)decFirstNormalizedPriceTrend[intI];
                    //        objRECal.SecondShare[intI] = objRECal.TotalValue[intI] * (dblAggressiveness) / (double)decSecondNormalizedPriceTrend[intI];
                    //    }
                    //    else
                    //    {
                    //        objRECal.FirstShare[intI] = objRECal.TotalValue[intI] * (dblAggressiveness) / (double)decFirstNormalizedPriceTrend[intI];
                    //        objRECal.SecondShare[intI] = objRECal.TotalValue[intI] * (1 - dblAggressiveness) / (double)decSecondNormalizedPriceTrend[intI];
                    //    }
                    //}
                    //else
                    //{
                    //    //Calculate Shares.
                    //    if (dblWinPair[intI - 1] == dblWinPair[intI])
                    //    {
                    //        objRECal.FirstShare[intI] = objRECal.FirstShare[intI - 1];
                    //        objRECal.SecondShare[intI] = objRECal.SecondShare[intI - 1];
                    //    }
                    //    else if (dblWinPair[intI] == 1)
                    //    {
                    //        objRECal.FirstShare[intI] = objRECal.TotalValue[intI - 1] * (1 - dblAggressiveness) / (double)decFirstNormalizedPriceTrend[intI];
                    //        objRECal.SecondShare[intI] = objRECal.TotalValue[intI - 1] * (dblAggressiveness) / (double)decSecondNormalizedPriceTrend[intI];
                    //    }
                    //    else
                    //    {
                    //        objRECal.FirstShare[intI] = objRECal.TotalValue[intI - 1] * (dblAggressiveness) / (double)decFirstNormalizedPriceTrend[intI];
                    //        objRECal.SecondShare[intI] = objRECal.TotalValue[intI - 1] * (1 - dblAggressiveness) / (double)decSecondNormalizedPriceTrend[intI];
                    //    }
                    //}
                }
            }
            catch (Exception)
            {

            }
            return objRECal;
        }



        /// <summary>
        /// This method is used to validate the dictionary object by removing the entries associated with the best performing symbol pair.
        /// </summary>
        /// <param name="dictRotationImp">Dictionary object to be validated</param>
        /// <param name="strKey">Best performing pair.</param>
        /// <param name="lstRotation">list of double containing the Rotation improvements.</param>
        /// <returns></returns>
        private Dictionary<string, double> ValidateDictionaryObject(Dictionary<string, double> dictRotationImp, string strKey, List<double> lstRotation)
        {
            try
            {
                string[] strKeysArr = dictRotationImp.Keys.ToArray();
                string[] strPairColl = strKey.Split(":".ToCharArray());
                foreach (string key in strKeysArr)
                {
                    string[] strKeyColl = key.Split(":".ToCharArray());
                    if (strKeyColl[0].Equals(strPairColl[0]) || strKeyColl[1].Equals(strPairColl[1]) || strKeyColl[0].Equals(strPairColl[1]) || strKeyColl[1].Equals(strPairColl[0]))
                    {
                        double dblVal = 0;
                        dictRotationImp.TryGetValue(key, out dblVal);
                        lstRotation.Remove(dblVal);
                        dictRotationImp.Remove(key);
                    }
                }
            }
            catch (Exception)
            {

            }
            return dictRotationImp;
        }

        /// <summary>
        /// This method is used to fill the data into the RotationMatrixData class object.
        /// </summary>
        /// <param name="count"></param>
        /// <param name="strArr"></param>
        /// <param name="strColor"></param>
        /// <returns>returns an instance of RotationMatrixData</returns>
        private RotationMatrixData FillData(int count, string[] strArr, string[] strColor)
        {
            try
            {
                RotationMatrixData objRot = new RotationMatrixData();

                //Switch depending upon the type of case.
                switch (strArr.Count())
                {
                    case 1:
                        objRot.C1 = strArr[0];
                        objRot.ColorC1 = strColor[0];
                        break;

                    case 2:
                        objRot.C1 = strArr[0];
                        objRot.C2 = strArr[1];
                        objRot.ColorC1 = strColor[0];
                        objRot.ColorC2 = strColor[1];
                        break;

                    case 3:
                        objRot.C1 = strArr[0];
                        objRot.C2 = strArr[1];
                        objRot.C3 = strArr[2];
                        objRot.ColorC1 = strColor[0];
                        objRot.ColorC2 = strColor[1];
                        objRot.ColorC3 = strColor[2];
                        break;

                    case 4:
                        objRot.C1 = strArr[0];
                        objRot.C2 = strArr[1];
                        objRot.C3 = strArr[2];
                        objRot.C4 = strArr[3];
                        objRot.ColorC1 = strColor[0];
                        objRot.ColorC2 = strColor[1];
                        objRot.ColorC3 = strColor[2];
                        objRot.ColorC4 = strColor[3];
                        break;

                    case 5:
                        objRot.C1 = strArr[0];
                        objRot.C2 = strArr[1];
                        objRot.C3 = strArr[2];
                        objRot.C4 = strArr[3];
                        objRot.C5 = strArr[4];
                        objRot.ColorC1 = strColor[0];
                        objRot.ColorC2 = strColor[1];
                        objRot.ColorC3 = strColor[2];
                        objRot.ColorC4 = strColor[3];
                        objRot.ColorC5 = strColor[4];
                        break;

                    case 6:
                        objRot.C1 = strArr[0];
                        objRot.C2 = strArr[1];
                        objRot.C3 = strArr[2];
                        objRot.C4 = strArr[3];
                        objRot.C5 = strArr[4];
                        objRot.C6 = strArr[5];
                        objRot.ColorC1 = strColor[0];
                        objRot.ColorC2 = strColor[1];
                        objRot.ColorC3 = strColor[2];
                        objRot.ColorC4 = strColor[3];
                        objRot.ColorC5 = strColor[4];
                        objRot.ColorC6 = strColor[5];
                        break;

                    case 7:
                        objRot.C1 = strArr[0];
                        objRot.C2 = strArr[1];
                        objRot.C3 = strArr[2];
                        objRot.C4 = strArr[3];
                        objRot.C5 = strArr[4];
                        objRot.C6 = strArr[5];
                        objRot.C7 = strArr[6];
                        objRot.ColorC1 = strColor[0];
                        objRot.ColorC2 = strColor[1];
                        objRot.ColorC3 = strColor[2];
                        objRot.ColorC4 = strColor[3];
                        objRot.ColorC5 = strColor[4];
                        objRot.ColorC6 = strColor[5];
                        objRot.ColorC7 = strColor[6];
                        break;

                    case 8:
                        objRot.C1 = strArr[0];
                        objRot.C2 = strArr[1];
                        objRot.C3 = strArr[2];
                        objRot.C4 = strArr[3];
                        objRot.C5 = strArr[4];
                        objRot.C6 = strArr[5];
                        objRot.C7 = strArr[6];
                        objRot.C8 = strArr[7];
                        objRot.ColorC1 = strColor[0];
                        objRot.ColorC2 = strColor[1];
                        objRot.ColorC3 = strColor[2];
                        objRot.ColorC4 = strColor[3];
                        objRot.ColorC5 = strColor[4];
                        objRot.ColorC6 = strColor[5];
                        objRot.ColorC7 = strColor[6];
                        objRot.ColorC8 = strColor[7];
                        break;

                    case 9:
                        objRot.C1 = strArr[0];
                        objRot.C2 = strArr[1];
                        objRot.C3 = strArr[2];
                        objRot.C4 = strArr[3];
                        objRot.C5 = strArr[4];
                        objRot.C6 = strArr[5];
                        objRot.C7 = strArr[6];
                        objRot.C8 = strArr[7];
                        objRot.C9 = strArr[8];
                        objRot.ColorC1 = strColor[0];
                        objRot.ColorC2 = strColor[1];
                        objRot.ColorC3 = strColor[2];
                        objRot.ColorC4 = strColor[3];
                        objRot.ColorC5 = strColor[4];
                        objRot.ColorC6 = strColor[5];
                        objRot.ColorC7 = strColor[6];
                        objRot.ColorC8 = strColor[7];
                        objRot.ColorC9 = strColor[8];
                        break;

                    case 10:
                        objRot.C1 = strArr[0];
                        objRot.C2 = strArr[1];
                        objRot.C3 = strArr[2];
                        objRot.C4 = strArr[3];
                        objRot.C5 = strArr[4];
                        objRot.C6 = strArr[5];
                        objRot.C7 = strArr[6];
                        objRot.C8 = strArr[7];
                        objRot.C9 = strArr[8];
                        objRot.C10 = strArr[9];
                        objRot.ColorC1 = strColor[0];
                        objRot.ColorC2 = strColor[1];
                        objRot.ColorC3 = strColor[2];
                        objRot.ColorC4 = strColor[3];
                        objRot.ColorC5 = strColor[4];
                        objRot.ColorC6 = strColor[5];
                        objRot.ColorC7 = strColor[6];
                        objRot.ColorC8 = strColor[7];
                        objRot.ColorC9 = strColor[8];
                        objRot.ColorC10 = strColor[9];
                        break;

                    case 11:
                        objRot.C1 = strArr[0];
                        objRot.C2 = strArr[1];
                        objRot.C3 = strArr[2];
                        objRot.C4 = strArr[3];
                        objRot.C5 = strArr[4];
                        objRot.C6 = strArr[5];
                        objRot.C7 = strArr[6];
                        objRot.C8 = strArr[7];
                        objRot.C9 = strArr[8];
                        objRot.C10 = strArr[9];
                        objRot.C11 = strArr[10];
                        objRot.ColorC1 = strColor[0];
                        objRot.ColorC2 = strColor[1];
                        objRot.ColorC3 = strColor[2];
                        objRot.ColorC4 = strColor[3];
                        objRot.ColorC5 = strColor[4];
                        objRot.ColorC6 = strColor[5];
                        objRot.ColorC7 = strColor[6];
                        objRot.ColorC8 = strColor[7];
                        objRot.ColorC9 = strColor[8];
                        objRot.ColorC10 = strColor[9];
                        objRot.ColorC11 = strColor[10];
                        break;

                    case 12:
                        objRot.C1 = strArr[0];
                        objRot.C2 = strArr[1];
                        objRot.C3 = strArr[2];
                        objRot.C4 = strArr[3];
                        objRot.C5 = strArr[4];
                        objRot.C6 = strArr[5];
                        objRot.C7 = strArr[6];
                        objRot.C8 = strArr[7];
                        objRot.C9 = strArr[8];
                        objRot.C10 = strArr[9];
                        objRot.C11 = strArr[10];
                        objRot.C12 = strArr[11];
                        objRot.ColorC1 = strColor[0];
                        objRot.ColorC2 = strColor[1];
                        objRot.ColorC3 = strColor[2];
                        objRot.ColorC4 = strColor[3];
                        objRot.ColorC5 = strColor[4];
                        objRot.ColorC6 = strColor[5];
                        objRot.ColorC7 = strColor[6];
                        objRot.ColorC8 = strColor[7];
                        objRot.ColorC9 = strColor[8];
                        objRot.ColorC10 = strColor[9];
                        objRot.ColorC11 = strColor[10];
                        objRot.ColorC12 = strColor[11];
                        break;

                    case 13:
                        objRot.C1 = strArr[0];
                        objRot.C2 = strArr[1];
                        objRot.C3 = strArr[2];
                        objRot.C4 = strArr[3];
                        objRot.C5 = strArr[4];
                        objRot.C6 = strArr[5];
                        objRot.C7 = strArr[6];
                        objRot.C8 = strArr[7];
                        objRot.C9 = strArr[8];
                        objRot.C10 = strArr[9];
                        objRot.C11 = strArr[10];
                        objRot.C12 = strArr[11];
                        objRot.C13 = strArr[12];
                        objRot.ColorC1 = strColor[0];
                        objRot.ColorC2 = strColor[1];
                        objRot.ColorC3 = strColor[2];
                        objRot.ColorC4 = strColor[3];
                        objRot.ColorC5 = strColor[4];
                        objRot.ColorC6 = strColor[5];
                        objRot.ColorC7 = strColor[6];
                        objRot.ColorC8 = strColor[7];
                        objRot.ColorC9 = strColor[8];
                        objRot.ColorC10 = strColor[9];
                        objRot.ColorC11 = strColor[10];
                        objRot.ColorC12 = strColor[11];
                        objRot.ColorC13 = strColor[12];
                        break;

                    case 14:
                        objRot.C1 = strArr[0];
                        objRot.C2 = strArr[1];
                        objRot.C3 = strArr[2];
                        objRot.C4 = strArr[3];
                        objRot.C5 = strArr[4];
                        objRot.C6 = strArr[5];
                        objRot.C7 = strArr[6];
                        objRot.C8 = strArr[7];
                        objRot.C9 = strArr[8];
                        objRot.C10 = strArr[9];
                        objRot.C11 = strArr[10];
                        objRot.C12 = strArr[11];
                        objRot.C13 = strArr[12];
                        objRot.C14 = strArr[13];
                        objRot.ColorC1 = strColor[0];
                        objRot.ColorC2 = strColor[1];
                        objRot.ColorC3 = strColor[2];
                        objRot.ColorC4 = strColor[3];
                        objRot.ColorC5 = strColor[4];
                        objRot.ColorC6 = strColor[5];
                        objRot.ColorC7 = strColor[6];
                        objRot.ColorC8 = strColor[7];
                        objRot.ColorC9 = strColor[8];
                        objRot.ColorC10 = strColor[9];
                        objRot.ColorC11 = strColor[10];
                        objRot.ColorC12 = strColor[11];
                        objRot.ColorC13 = strColor[12];
                        objRot.ColorC14 = strColor[13];
                        break;

                    case 15:
                        objRot.C1 = strArr[0];
                        objRot.C2 = strArr[1];
                        objRot.C3 = strArr[2];
                        objRot.C4 = strArr[3];
                        objRot.C5 = strArr[4];
                        objRot.C6 = strArr[5];
                        objRot.C7 = strArr[6];
                        objRot.C8 = strArr[7];
                        objRot.C9 = strArr[8];
                        objRot.C10 = strArr[9];
                        objRot.C11 = strArr[10];
                        objRot.C12 = strArr[11];
                        objRot.C13 = strArr[12];
                        objRot.C14 = strArr[13];
                        objRot.C15 = strArr[14];
                        objRot.ColorC1 = strColor[0];
                        objRot.ColorC2 = strColor[1];
                        objRot.ColorC3 = strColor[2];
                        objRot.ColorC4 = strColor[3];
                        objRot.ColorC5 = strColor[4];
                        objRot.ColorC6 = strColor[5];
                        objRot.ColorC7 = strColor[6];
                        objRot.ColorC8 = strColor[7];
                        objRot.ColorC9 = strColor[8];
                        objRot.ColorC10 = strColor[9];
                        objRot.ColorC11 = strColor[10];
                        objRot.ColorC12 = strColor[11];
                        objRot.ColorC13 = strColor[12];
                        objRot.ColorC14 = strColor[13];
                        objRot.ColorC15 = strColor[14];
                        break;

                    case 16:
                        objRot.C1 = strArr[0];
                        objRot.C2 = strArr[1];
                        objRot.C3 = strArr[2];
                        objRot.C4 = strArr[3];
                        objRot.C5 = strArr[4];
                        objRot.C6 = strArr[5];
                        objRot.C7 = strArr[6];
                        objRot.C8 = strArr[7];
                        objRot.C9 = strArr[8];
                        objRot.C10 = strArr[9];
                        objRot.C11 = strArr[10];
                        objRot.C12 = strArr[11];
                        objRot.C13 = strArr[12];
                        objRot.C14 = strArr[13];
                        objRot.C15 = strArr[14];
                        objRot.C16 = strArr[15];
                        objRot.ColorC1 = strColor[0];
                        objRot.ColorC2 = strColor[1];
                        objRot.ColorC3 = strColor[2];
                        objRot.ColorC4 = strColor[3];
                        objRot.ColorC5 = strColor[4];
                        objRot.ColorC6 = strColor[5];
                        objRot.ColorC7 = strColor[6];
                        objRot.ColorC8 = strColor[7];
                        objRot.ColorC9 = strColor[8];
                        objRot.ColorC10 = strColor[9];
                        objRot.ColorC11 = strColor[10];
                        objRot.ColorC12 = strColor[11];
                        objRot.ColorC13 = strColor[12];
                        objRot.ColorC14 = strColor[13];
                        objRot.ColorC15 = strColor[14];
                        objRot.ColorC16 = strColor[15];
                        break;

                }
                return objRot;
            }
            catch (Exception)
            {

            }
            return null;
        }

        /// <summary>
        /// This method is used to generate the Html code.
        /// </summary>
        /// <param name="ctl"></param>
        /// <returns>returns the Html code in the string format.</returns>
        private string GenerateHTML(Control ctl)
        {
            string htmlContent = string.Empty;
            if ((ctl != null))
            {
                using (MemoryStream dataStream = new MemoryStream())
                {
                    using (StreamWriter textWriter = new StreamWriter(dataStream, Encoding.UTF8))
                    {
                        //UTF8Encoding.Default) 
                        using (HtmlTextWriter writer = new HtmlTextWriter(textWriter))
                        {
                            ctl.RenderControl(writer);
                            textWriter.Flush();
                            dataStream.Seek(0, SeekOrigin.Begin);

                            using (System.IO.StreamReader dataReader = new StreamReader(dataStream))
                            {
                                htmlContent = dataReader.ReadToEnd();
                                htmlContent = htmlContent.Replace("\n", "").Replace("\r", "").Replace("\t", "");
                            }
                        }
                    }
                }
            }
            return htmlContent;
        }

        /// <summary>
        /// This method is used for calculating values for odd count Buy and Hold allocation.
        /// </summary>
        /// <param name="strPairColl"></param>
        /// <param name="strPairKey"></param>
        /// <param name="intArraySize"></param>
        /// <param name="dictFinalData"></param>
        /// <param name="dictPairDefaultValue"></param>
        /// <param name="lstAssetAllocation"></param>
        /// <param name="dblAggressiveness"></param>
        /// <returns></returns>
        private clsRECalculations FillOddPairForBHData(string[] strPairColl, string strPairKey, int intArraySize, Dictionary<string, int[]> dictFinalData, Dictionary<string, double> dictPairDefaultValue, List<AssetAllocation> lstAssetAllocation, double dblAggressiveness, Dictionary<string, double[]> dictBuyNHoldRotation)
        {
            clsRECalculations objRECal = new clsRECalculations(intArraySize);
            try
            {
                objRECal.SymbolPair = strPairKey;
                int[] dblWinPair;
                dictFinalData.TryGetValue(strPairKey, out dblWinPair);
                int intFirstAssetSymbol = Convert.ToInt32(strPairColl[0]);

                //Get the Normalized Price Trend and Asset values.
                double[] decFirstNormalizedPriceTrend = lstAssetAllocation.First(x => x.MAssetSymbolId.Equals(intFirstAssetSymbol)).MPriceTrend;
                double[] decAssetValues = lstAssetAllocation.FirstOrDefault(x => x.MAssetSymbolId.Equals(intFirstAssetSymbol)).MAssetValue;
                for (int intI = 217; intI <= lstAssetAllocation[0].MPrice.Count() - 1; intI++)
                {
                    if (intI == 217)
                    {
                        //Calculate Shares.
                        objRECal.FirstShare[intI] = (decAssetValues[intI] / decFirstNormalizedPriceTrend[intI]);

                        //Calculate Total value.
                        objRECal.TotalValue[intI] = decAssetValues[intI];

                        double[] dblTempArr = new double[3];
                        dblTempArr[0] = objRECal.FirstShare[intI];
                        dblTempArr[1] = objRECal.FirstShare[intI];
                        dblTempArr[2] = intI;
                        dictBuyNHoldRotation.Add(strPairKey, dblTempArr);
                    }
                    else
                    {
                        //Calculate Shares.
                        objRECal.FirstShare[intI] = objRECal.FirstShare[intI - 1];

                        //Calculate Total value.
                        objRECal.TotalValue[intI] = objRECal.FirstShare[intI] * (double)decFirstNormalizedPriceTrend[intI];


                        if (objRECal.FirstShare[intI] != objRECal.FirstShare[intI - 1])
                        {
                            if (dictBuyNHoldRotation.ContainsKey(strPairKey))
                            {
                                double[] dblTempArr;
                                dictBuyNHoldRotation.TryGetValue(strPairKey, out dblTempArr);
                                dblTempArr[0] = dblTempArr[1];
                                dblTempArr[1] = objRECal.FirstShare[intI];
                                dblTempArr[2] = intI;
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {

            }
            return objRECal;
        }

        /// <summary>
        /// This method is used for calculating values for Buy and Hold Even count operations.
        /// </summary>
        /// <param name="strPairColl"></param>
        /// <param name="strPairKey"></param>
        /// <param name="intArraySize"></param>
        /// <param name="dictFinalData"></param>
        /// <param name="dictPairDefaultValue"></param>
        /// <param name="lstAssetAllocation"></param>
        /// <param name="dblAggressiveness"></param>
        /// <returns></returns>
        private clsRECalculations FillEvenPairForBHData(string[] strPairColl, string strPairKey, int intArraySize, Dictionary<string, int[]> dictFinalData, Dictionary<string, double> dictPairDefaultValue, List<AssetAllocation> lstAssetAllocation, double dblAggressiveness, Dictionary<string, double[]> dictBuyNHoldRotation)
        {
            clsRECalculations objRECal = new clsRECalculations(intArraySize);
            try
            {
                objRECal.SymbolPair = strPairKey;
                int[] dblWinPair;
                dictFinalData.TryGetValue(strPairKey, out dblWinPair);
                int intFirstAssetSymbol = Convert.ToInt32(strPairColl[0]);
                int intSecondAssetSymbol = Convert.ToInt32(strPairColl[1]);
                double dblDefaultValue = 0;
                dictPairDefaultValue.TryGetValue(strPairKey, out dblDefaultValue);
                if (dblDefaultValue != 0)
                    objRECal.TotalValue[217] = dblDefaultValue;
                else
                    objRECal.TotalValue[217] = 5000;

                //Get the Normalized Price Trend values.
                double[] decFirstNormalizedPriceTrend = lstAssetAllocation.First(x => x.MAssetSymbolId.Equals(intFirstAssetSymbol)).MPriceTrend;
                double[] decSecondNormalizedPriceTrend = lstAssetAllocation.First(x => x.MAssetSymbolId.Equals(intSecondAssetSymbol)).MPriceTrend;
                for (int intI = 217; intI <= lstAssetAllocation[0].MPrice.Count() - 1; intI++)
                {
                    if (intI == 217)
                    {
                        //Calculate Shares.
// Change 13-Dec-2010
                        if (dblWinPair[intI] == 0)
                        {
                            objRECal.FirstShare[intI] = objRECal.TotalValue[intI] * (1 - dblAggressiveness) / decFirstNormalizedPriceTrend[intI];
                            objRECal.SecondShare[intI] = objRECal.TotalValue[intI] * (dblAggressiveness) / decSecondNormalizedPriceTrend[intI];
                        }
                        else
                        {
                            objRECal.FirstShare[intI] = objRECal.TotalValue[intI] * (dblAggressiveness) / decFirstNormalizedPriceTrend[intI];
                            objRECal.SecondShare[intI] = objRECal.TotalValue[intI] * (1 - dblAggressiveness) / decSecondNormalizedPriceTrend[intI];
                        }

                        double[] dblTempArr = new double[5];
                        dblTempArr[0] = objRECal.FirstShare[intI];
                        dblTempArr[1] = objRECal.FirstShare[intI];
                        dblTempArr[2] = objRECal.SecondShare[intI];
                        dblTempArr[3] = objRECal.SecondShare[intI];
                        dblTempArr[4] = intI;
                        dictBuyNHoldRotation.Add(strPairKey, dblTempArr);
                    }
                    else
                    {
                        
                        //Calculate Shares.
                        if (dblWinPair[intI - 1] == dblWinPair[intI])
                        {
                            objRECal.FirstShare[intI] = objRECal.FirstShare[intI - 1];
                            objRECal.SecondShare[intI] = objRECal.SecondShare[intI - 1];
                        }
// Change 13-Dec-2010
                        else if (dblWinPair[intI] == 1)
                        {
                            objRECal.SecondShare[intI] = objRECal.TotalValue[intI - 1] * (1 - dblAggressiveness) / decSecondNormalizedPriceTrend[intI - 1];// change 17-Mar-2011
                            objRECal.FirstShare[intI] = objRECal.TotalValue[intI - 1] * (dblAggressiveness) / decFirstNormalizedPriceTrend[intI];
                        }
                        else
                        {
                            objRECal.SecondShare[intI] = objRECal.TotalValue[intI - 1] * (dblAggressiveness) / decSecondNormalizedPriceTrend[intI];
                            objRECal.FirstShare[intI] = objRECal.TotalValue[intI - 1] * (1 - dblAggressiveness) / decFirstNormalizedPriceTrend[intI - 1];// change 17-Mar-2011 
                        }
                        objRECal.TotalValue[intI] = objRECal.FirstShare[intI] * decFirstNormalizedPriceTrend[intI] + decSecondNormalizedPriceTrend[intI] * objRECal.SecondShare[intI];

                        if (dblWinPair[intI] == dblWinPair[intI - 1])
                            objRECal.RotationEvent[intI] = 0;
                        else
                            objRECal.RotationEvent[intI] = 1;

                        //if (objRECal.FirstShare[intI] != objRECal.FirstShare[intI - 1] && objRECal.SecondShare[intI] != objRECal.SecondShare[intI - 1])
                        //change 28-Dec-2010
                        if (dblWinPair[intI] != dblWinPair[intI - 1])
                        {
                            if (dictBuyNHoldRotation.ContainsKey(strPairKey))
                            {
                                double[] dblTempArr;
                                dictBuyNHoldRotation.TryGetValue(strPairKey, out dblTempArr);
                                dblTempArr[0] = dblTempArr[1];
                                dblTempArr[1] = objRECal.FirstShare[intI];
                                dblTempArr[2] = dblTempArr[3];
                                dblTempArr[3] = objRECal.SecondShare[intI];
                                dblTempArr[4] = intI;
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {

            }
            return objRECal;
        }

        /// <summary>
        /// This method is used for calculating the Rebalance data for Negative yeilding pairs.
        /// </summary>
        /// <param name="intI"></param>
        /// <param name="dblAggressiveness"></param>
        /// <param name="intDayCountQuarters"></param>
        /// <param name="strPairKey"></param>
        /// <param name="dictFinalData"></param>
        /// <param name="strPairColl"></param>
        /// <param name="dictPairDefaultValue"></param>
        /// <param name="lstAssetAllocation"></param>
        /// <param name="lstREColl"></param>
        private void FillPairRebalanceDataForNegativeValue(int intI, double dblAggressiveness, int[] intDayCountQuarters, clsRECalculations strPairKey, Dictionary<string, int[]> dictFinalData, string[] strPairColl, Dictionary<string, double> dictPairDefaultValue, List<AssetAllocation> lstAssetAllocation, List<clsRECalculations> lstREColl, Dictionary<string, double[]> dictRotationRebalance)
        {
            try
            {
                int[] dblWinPair;
                dictFinalData.TryGetValue(strPairKey.SymbolPair, out dblWinPair);

                int intFirstAssetSymbol = Convert.ToInt32(strPairColl[0]);
                int intSecondAssetSymbol = Convert.ToInt32(strPairColl[1]);

                //Fill the array`s for Normalized price trend and Asset values.
                double[] decFirstNormalizedPriceTrend = lstAssetAllocation.First(x => x.MAssetSymbolId.Equals(intFirstAssetSymbol)).MPriceTrend;
                double[] decSecondNormalizedPriceTrend = lstAssetAllocation.First(x => x.MAssetSymbolId.Equals(intSecondAssetSymbol)).MPriceTrend;
                double[] decFirstAssetValues = lstAssetAllocation.FirstOrDefault(x => x.MAssetSymbolId.Equals(intFirstAssetSymbol)).MAssetValue;
                double[] decSecondAssetValues = lstAssetAllocation.FirstOrDefault(x => x.MAssetSymbolId.Equals(intSecondAssetSymbol)).MAssetValue;
                if (intI == 217)
                {
                    //Calcualate shares.
                    strPairKey.FirstShare[intI] = decFirstAssetValues[intI] / decFirstNormalizedPriceTrend[intI];
                    strPairKey.SecondShare[intI] = decSecondAssetValues[intI] / decSecondNormalizedPriceTrend[intI];
                    double[] dblTempArr = new double[5];
                    dblTempArr[0] = strPairKey.FirstShare[intI];
                    dblTempArr[1] = strPairKey.FirstShare[intI];
                    dblTempArr[2] = strPairKey.SecondShare[intI];
                    dblTempArr[3] = strPairKey.SecondShare[intI];
                    dblTempArr[4] = intI;
                    dictRotationRebalance.Add(strPairKey.SymbolPair, dblTempArr);

                    //Calculate total value.
                    strPairKey.TotalValue[intI] = decFirstAssetValues[intI] + decSecondAssetValues[intI];
                }
                else
                {
                    //Calcualate shares.
                    strPairKey.FirstShare[intI] = strPairKey.FirstShare[intI - 1];
                    strPairKey.SecondShare[intI] = strPairKey.SecondShare[intI - 1];

                    //if (strPairKey.FirstShare[intI] != strPairKey.FirstShare[intI - 1] && strPairKey.SecondShare[intI] != strPairKey.SecondShare[intI - 1])
                    //change 28-Dec-2010
                    if (dblWinPair[intI] != dblWinPair[intI - 1])
                    {
                        if (dictRotationRebalance.ContainsKey(strPairKey.SymbolPair))
                        {
                            double[] dblTempArr;
                            dictRotationRebalance.TryGetValue(strPairKey.SymbolPair, out dblTempArr);
                            dblTempArr[0] = dblTempArr[1];
                            dblTempArr[1] = strPairKey.FirstShare[intI];
                            dblTempArr[2] = dblTempArr[3];
                            dblTempArr[3] = strPairKey.SecondShare[intI];
                            dblTempArr[4] = intI;
                        }
                    }

                    //Calculate total value.
                    strPairKey.TotalValue[intI] = decFirstAssetValues[intI] + decSecondAssetValues[intI];
                    // strPairKey.TotalValue[intI] = strPairKey.FirstShare[intI] * (double)decFirstNormalizedPriceTrend[intI] + strPairKey.SecondShare[intI] * (double)decSecondNormalizedPriceTrend[intI];

                    //Calculate Rotation Event.
                    if (dblWinPair[intI] == dblWinPair[intI - 1])
                        strPairKey.RotationEvent[intI] = 0;
                    else
                        strPairKey.RotationEvent[intI] = 1;
                }
            }
            catch (Exception)
            {

            }
        }

        /// <summary>
        /// This method is used for calculating the Rebalance data for Odd pairs.
        /// </summary>
        /// <param name="intI"></param>
        /// <param name="dblAggressiveness"></param>
        /// <param name="intDayCountQuarters"></param>
        /// <param name="strPairKey"></param>
        /// <param name="dictFinalData"></param>
        /// <param name="strPairColl"></param>
        /// <param name="dictPairDefaultValue"></param>
        /// <param name="lstAssetAllocation"></param>
        /// <param name="lstREColl"></param>
        private void FillOddPairForRebalanceData(int intI, double dblAggressiveness, int[] intDayCountQuarters, clsRECalculations strPairKey, Dictionary<string, int[]> dictFinalData, string[] strPairColl, Dictionary<string, double> dictPairDefaultValue, List<AssetAllocation> lstAssetAllocation, List<clsRECalculations> lstREColl, Dictionary<string, double[]> dictRotationRebalance)
        {
            try
            {
                int[] dblWinPair;
                dictFinalData.TryGetValue(strPairKey.SymbolPair, out dblWinPair);

                int intFirstAssetSymbol = Convert.ToInt32(strPairColl[0]);

                //Get an array of Normalized Price Trend.
                double[] decFirstNormalizedPriceTrend = lstAssetAllocation.First(x => x.MAssetSymbolId.Equals(intFirstAssetSymbol)).MPriceTrend;

                //Get an array of Asset values.
                double[] decAssetValues = lstAssetAllocation.FirstOrDefault(x => x.MAssetSymbolId.Equals(intFirstAssetSymbol)).MAssetValue;
                double[] decTargetWeight = lstAssetAllocation.FirstOrDefault(x => x.MAssetSymbolId.Equals(intFirstAssetSymbol)).MTargetWeight;
                if (intI == 217)
                {
                    //Calculate Share.
                    strPairKey.FirstShare[intI] = decAssetValues[intI] / decFirstNormalizedPriceTrend[intI];

                    double[] dblTempArr = new double[3];
                    dblTempArr[0] = strPairKey.FirstShare[intI];
                    dblTempArr[1] = strPairKey.FirstShare[intI];
                    dblTempArr[2] = intI;
                    dictRotationRebalance.Add(strPairKey.SymbolPair, dblTempArr);

                    //Calculate Total value.
                    strPairKey.TotalValue[intI] = (double)decAssetValues[intI];
                }
                else
                {
                    //Calculate Share.
                    double dblTotal = 0;
                    if (intDayCountQuarters[intI] > 1)
                        strPairKey.FirstShare[intI] = strPairKey.FirstShare[intI - 1];
                    else
                    {
                        foreach (var set in lstREColl)
                        {
                            dblTotal += set.TotalValue[intI - 1];
                        }
                        strPairKey.FirstShare[intI] = dblTotal * (double)decTargetWeight[intI - 1] / (double)decFirstNormalizedPriceTrend[intI];
                    }

                    if (strPairKey.FirstShare[intI] != strPairKey.FirstShare[intI - 1])
                    {
                        if (dictRotationRebalance.ContainsKey(strPairKey.SymbolPair))
                        {
                            double[] dblTempArr;
                            dictRotationRebalance.TryGetValue(strPairKey.SymbolPair, out dblTempArr);
                            dblTempArr[0] = dblTempArr[1];
                            dblTempArr[1] = strPairKey.FirstShare[intI];
                            dblTempArr[2] = intI;
                        }
                    }

                    //Calculate Total value.
                    if (intDayCountQuarters[intI] > 1)
                        strPairKey.TotalValue[intI] = (strPairKey.FirstShare[intI] * (double)decFirstNormalizedPriceTrend[intI]);
                    else
                        strPairKey.TotalValue[intI] = ((double)decTargetWeight[intI - 1] * dblTotal);

                }
            }
            catch (Exception)
            {

            }
        }

        /// <summary>
        /// This method is used for calculating the Rebalance data for Even pairs.
        /// </summary>
        /// <param name="intI"></param>
        /// <param name="dblAggressiveness"></param>
        /// <param name="intDayCountQuarters"></param>
        /// <param name="strPairKey"></param>
        /// <param name="dictFinalData"></param>
        /// <param name="strPairColl"></param>
        /// <param name="dictPairDefaultValue"></param>
        /// <param name="lstAssetAllocation"></param>
        /// <param name="lstREColl"></param>
        private void FillEvenPairForRebalanceData(int intI, double dblAggressiveness, int[] intDayCountQuarters, clsRECalculations strPairKey, Dictionary<string, int[]> dictFinalData, string[] strPairColl, Dictionary<string, double> dictPairDefaultValue, List<AssetAllocation> lstAssetAllocation, List<clsRECalculations> lstREColl, Dictionary<string, double[]> dictRotationRebalance)
        {
            try
            {
                int[] dblWinPair;

                //Get the win pair array for a symbol pair.
                dictFinalData.TryGetValue(strPairKey.SymbolPair, out dblWinPair);

                int intFirstAssetSymbol = Convert.ToInt32(strPairColl[0]);
                int intSecondAssetSymbol = Convert.ToInt32(strPairColl[1]);
                double dblDefaultValue = 0;
                dictPairDefaultValue.TryGetValue(strPairKey.SymbolPair, out dblDefaultValue);
                if (dblDefaultValue != 0)
                    strPairKey.TotalValue[217] = dblDefaultValue;
                else
                    strPairKey.TotalValue[217] = 5000;

                //Retrieve the Normalized Price Trend array`s for both the symbol`s.
                double[] decFirstNormalizedPriceTrend = lstAssetAllocation.First(x => x.MAssetSymbolId.Equals(intFirstAssetSymbol)).MPriceTrend;
                double[] decSecondNormalizedPriceTrend = lstAssetAllocation.First(x => x.MAssetSymbolId.Equals(intSecondAssetSymbol)).MPriceTrend;

                if (intI == 217)
                {
                    //Calculate Shares for both the symbol`s.
                 //   if (dblWinPair[intI] == 1)
// Change 13-Dec-2010
                    if (dblWinPair[intI] == 0)
                    {
                        strPairKey.FirstShare[intI] = strPairKey.TotalValue[intI] * (1 - dblAggressiveness) / decFirstNormalizedPriceTrend[intI];
                        strPairKey.SecondShare[intI] = strPairKey.TotalValue[intI] * (dblAggressiveness) / decSecondNormalizedPriceTrend[intI];
                    }
                    else
                    {
                        strPairKey.FirstShare[intI] = strPairKey.TotalValue[intI] * (dblAggressiveness) / decFirstNormalizedPriceTrend[intI];
                        strPairKey.SecondShare[intI] = strPairKey.TotalValue[intI] * (1 - dblAggressiveness) / decSecondNormalizedPriceTrend[intI];
                    }

                    double[] dblTempArr = new double[5];
                    dblTempArr[0] = strPairKey.FirstShare[intI];
                    dblTempArr[1] = strPairKey.FirstShare[intI];
                    dblTempArr[2] = strPairKey.SecondShare[intI];
                    dblTempArr[3] = strPairKey.SecondShare[intI];
                    dblTempArr[4] = intI;
                    dictRotationRebalance.Add(strPairKey.SymbolPair, dblTempArr);
                }
                else
                {
                    //calculate Total value.
                    if (intDayCountQuarters[intI] == 1)
                    {
                        strPairKey.TotalValue[intI] = ((double)lstAssetAllocation.First(x => x.MAssetSymbolId.Equals(intFirstAssetSymbol)).MTargetWeight[intI] + (double)lstAssetAllocation.First(x => x.MAssetSymbolId.Equals(intSecondAssetSymbol)).MTargetWeight[intI]) * (GetPreviousDayTotalData(lstREColl, intI));
                    }

                    //Calculate Shares for both the symbol`s.
                    if (intDayCountQuarters[intI] == 1)
                    {
                        strPairKey.SecondShare[intI] = strPairKey.TotalValue[intI] / strPairKey.TotalValue[intI - 1] * strPairKey.SecondShare[intI - 1];
                        strPairKey.FirstShare[intI] = strPairKey.TotalValue[intI] / strPairKey.TotalValue[intI - 1] * strPairKey.FirstShare[intI - 1];
                    }
                    else if (dblWinPair[intI] == dblWinPair[intI - 1])
                    {
                        strPairKey.SecondShare[intI] = strPairKey.SecondShare[intI - 1];
                        strPairKey.FirstShare[intI] = strPairKey.FirstShare[intI - 1];
                    }
// Change 13-Dec-2010
                    else if (dblWinPair[intI] == 1)
                    {
                        strPairKey.FirstShare[intI] = strPairKey.TotalValue[intI - 1] * dblAggressiveness / decFirstNormalizedPriceTrend[intI];
                        strPairKey.SecondShare[intI] = strPairKey.TotalValue[intI - 1] * (1 - dblAggressiveness) / decSecondNormalizedPriceTrend[intI - 1];// change 17-Mar-2011
                    }
                    else
                    {
                        strPairKey.FirstShare[intI] = strPairKey.TotalValue[intI - 1] * (1 - dblAggressiveness) / decFirstNormalizedPriceTrend[intI - 1];// change 17-Mar-2011
                        strPairKey.SecondShare[intI] = strPairKey.TotalValue[intI - 1] * (dblAggressiveness) / decSecondNormalizedPriceTrend[intI];
                    }

                    //Calculate Total value.
                    if (intDayCountQuarters[intI] > 1)
                        strPairKey.TotalValue[intI] = strPairKey.FirstShare[intI] * decFirstNormalizedPriceTrend[intI] + strPairKey.SecondShare[intI] * decSecondNormalizedPriceTrend[intI];

                    //Calculate Rotation Event.
                    if (dblWinPair[intI] == dblWinPair[intI - 1])
                        strPairKey.RotationEvent[intI] = 0;
                    else
                        strPairKey.RotationEvent[intI] = 1;

                    //if (strPairKey.FirstShare[intI] != strPairKey.FirstShare[intI - 1] && strPairKey.SecondShare[intI] != strPairKey.SecondShare[intI - 1])
                    //change 28-Dec-2010
                    if(dblWinPair[intI]!= dblWinPair[intI-1])
                    {
                        if (dictRotationRebalance.ContainsKey(strPairKey.SymbolPair))
                        {
                            double[] dblTempArr;
                            dictRotationRebalance.TryGetValue(strPairKey.SymbolPair, out dblTempArr);
                            dblTempArr[0] = dblTempArr[1];
                            dblTempArr[1] = strPairKey.FirstShare[intI];
                            dblTempArr[2] = dblTempArr[3];
                            dblTempArr[3] = strPairKey.SecondShare[intI];
                            dblTempArr[4] = intI;
                        }
                    }
                }
            }
            catch (Exception)
            {


            }
        }

        /// <summary>
        /// This method is used to get the summation of Total value for all the assets present in the portfolio for previous date.
        /// </summary>
        /// <param name="lstREColl"></param>
        /// <param name="intIndex"></param>
        /// <returns></returns>
        private double GetPreviousDayTotalData(List<clsRECalculations> lstREColl, int intIndex)
        {
            double dblResult = 0;
            try
            {
                foreach (clsRECalculations objSet in lstREColl)
                {
                    dblResult = dblResult + objSet.TotalValue[intIndex - 1];
                }
            }
            catch (Exception)
            {

            }
            return dblResult;
        }

        /// <summary>
        /// This method is used for comparing the two asset`s and returning the result in the form of double array.
        /// </summary>
        /// <param name="intPortfolioID"></param>
        /// <param name="dtStartDate"></param>
        /// <param name="intArraySize"></param>
        /// <param name="iID"></param>
        /// <param name="kID"></param>
        /// <param name="isChecked"></param>
        /// <param name="strPair"></param>
        /// <param name="intAWins"></param>
        /// <returns></returns>
        //private double[] CompareAssets(int intPortfolioID, DateTime dtStartDate, int intArraySize, int iID, int kID, bool isChecked, out string strPair, out int[] intAWins)
        //{
        //    //System.Diagnostics.Debugger.Break();
        //    strPair = string.Empty;
        //    intAWins = null;
        //    double[] arrValues = new double[3];
        //    try
        //    {
        //        List<clsRotationEngineProperties> lstREPColl = new List<clsRotationEngineProperties>();
        //        double[] dblSlopeDiff = new double[intArraySize];
        //        double[] dblRotatedDailyChange = new double[intArraySize];
        //        double[] dblValueRotation = new double[intArraySize];
        //        char[] chWinningAsset = new char[intArraySize];
        //        char[] chFilteredWinner = new char[intArraySize];
        //        int[] intDayCount = new int[intArraySize];
        //        int[] intRebalancesPerYr = new int[intArraySize];
        //        int[] intAssetWins = new int[intArraySize];
        //        double dblOptimumThreshold = 0;
        //        int intWaitRotation = 40;

        //        //Retrieve the value of WaitRotation from the database.
        //        if (objHEEntities.RotationEngineVariableSet.FirstOrDefault() != null)
        //        {
        //            intWaitRotation = (int)objHEEntities.RotationEngineVariableSet.FirstOrDefault().WaitRotation;
        //        }
        //        double dblAgressiveness = 1;

        //        //Generate a pair in the string format.
        //        string strSymbolPair = objHEEntities.AssetSymbolSet.FirstOrDefault(x => x.Id.Equals(iID)).Symbol + ":" + objHEEntities.AssetSymbolSet.FirstOrDefault(x => x.Id.Equals(kID)).Symbol;

        //        //Retrieve the Optimum threshold value from the database.
        //        if (objHEEntities.RotationEnginePair.Where(x => x.SymbolPair.Equals(strSymbolPair) && x.PortfolioSet.Id.Equals(intPortfolioID)).Count() > 0)
        //        {
        //            dblOptimumThreshold = objHEEntities.RotationEnginePair.First(x => x.SymbolPair.Equals(strSymbolPair) && x.PortfolioSet.Id.Equals(intPortfolioID)).OptimalTbase;
        //        }

        //        if (intArraySize > 0)
        //        {
        //            lstREPColl.Add(PopulateObjectProperties(dtStartDate, intArraySize, iID));
        //            lstREPColl.Add(PopulateObjectProperties(dtStartDate, intArraySize, kID));

        //            double dblLoopValMin = -0.0004;
        //            double dblLoopValMax = 0.0004;
        //            double dblLoopValIncrement = 0.00002;
        //            double dblTempLoopValMin = dblLoopValMin;
        //            int intTempCount = 0;

        //            while (dblTempLoopValMin <= dblLoopValMax)
        //            {
        //                dblTempLoopValMin = dblTempLoopValMin + dblLoopValIncrement;
        //                intTempCount++;
        //            }

        //            double[] dblThresholdRotation = new double[intTempCount];
        //            double[] dblEndingValue = new double[intTempCount];

        //            dblTempLoopValMin = dblLoopValMin;
        //            int intCount = 0;

        //            if (dblOptimumThreshold == 0)
        //            {
        //                //Calculate Threshold rotation optimum.
        //                while (dblTempLoopValMin <= dblLoopValMax)
        //                {
        //                    dblOptimumThreshold = dblTempLoopValMin;

        //                    for (int intI = 216; intI < intArraySize; intI++)
        //                    {
        //                        if (intI == 216)
        //                        {
        //                            chWinningAsset[intI] = 'B';
        //                            intDayCount[intI] = 1;
        //                            chFilteredWinner[intI] = 'B';
        //                            intRebalancesPerYr[intI] = 1;
        //                            dblValueRotation[intI] = 1;
        //                        }
        //                        if (intI >= 217)
        //                        {
        //                            //Calculate slope difference.
        //                            dblSlopeDiff[intI] = lstREPColl[1].DblSlope[intI] - lstREPColl[0].DblSlope[intI];

        //                            //Calculate Winning asset.
        //                            if (dblSlopeDiff[intI] > dblOptimumThreshold)
        //                                chWinningAsset[intI] = 'A';
        //                            else
        //                                chWinningAsset[intI] = 'B';

        //                            //Calculate day count.
        //                            if (chWinningAsset[intI].Equals(chWinningAsset[intI - 1]))
        //                                intDayCount[intI] = intDayCount[intI - 1] + 1;
        //                            else
        //                                intDayCount[intI] = 1;

        //                            //Calculate filter winning.
        //                            if (intDayCount[intI] > intWaitRotation)
        //                                chFilteredWinner[intI] = chWinningAsset[intI];
        //                            else
        //                                chFilteredWinner[intI] = chFilteredWinner[intI - 1];

        //                            //Calculate Rebalances/yr.
        //                            if (chFilteredWinner[intI].Equals(chFilteredWinner[intI - 1]))
        //                                intRebalancesPerYr[intI] = 0;
        //                            else
        //                                intRebalancesPerYr[intI] = 1;

        //                            //Calculate Rotated Daily Change.
        //                            if (chFilteredWinner[intI - 1] == 'A')
        //                                dblRotatedDailyChange[intI] = (lstREPColl[1].DblDailychange[intI] * dblAgressiveness) + lstREPColl[0].DblDailychange[intI] * (1 - dblAgressiveness);
        //                            else
        //                                dblRotatedDailyChange[intI] = (lstREPColl[0].DblDailychange[intI] * dblAgressiveness) + lstREPColl[1].DblDailychange[intI] * (1 - dblAgressiveness);

        //                            //Calculate Value Rotation.
        //                            dblValueRotation[intI] = dblValueRotation[intI - 1] * (1 + dblRotatedDailyChange[intI]);
        //                        }
        //                    }

        //                    dblThresholdRotation[intCount] = dblOptimumThreshold;
        //                    dblEndingValue[intCount] = dblValueRotation[dblValueRotation.Count() - 1];
        //                    dblTempLoopValMin = dblTempLoopValMin + dblLoopValIncrement;
        //                    intCount++;
        //                }

        //                //Finally get the optimum threshold value.
        //                for (int i = 0; i < intTempCount; i++)
        //                {
        //                    if (dblEndingValue[i] == dblEndingValue.Max())
        //                    {
        //                        dblOptimumThreshold = dblThresholdRotation[i];
        //                        break;
        //                    }
        //                } 
        //            }

        //            //Calculate the fresh values with the newly calculated Threshold value.
        //            for (int intI = 216; intI < intArraySize; intI++)
        //            {
        //                if (intI == 216)
        //                {
        //                    chWinningAsset[intI] = 'B';
        //                    intDayCount[intI] = 1;
        //                    chFilteredWinner[intI] = 'B';
        //                    intRebalancesPerYr[intI] = 1;
        //                    dblValueRotation[intI] = 1;
        //                }
        //                if (intI >= 217)
        //                {
        //                    //Calculate slope difference.
        //                    dblSlopeDiff[intI] = lstREPColl[1].DblSlope[intI] - lstREPColl[0].DblSlope[intI];

        //                    //Calculate Winning asset.
        //                    if (dblSlopeDiff[intI] > dblOptimumThreshold)
        //                        chWinningAsset[intI] = 'A';
        //                    else
        //                        chWinningAsset[intI] = 'B';

        //                    //Calculate day count.
        //                    if (chWinningAsset[intI].Equals(chWinningAsset[intI - 1]))
        //                        intDayCount[intI] = intDayCount[intI - 1] + 1;
        //                    else
        //                        intDayCount[intI] = 1;

        //                    //Calculate filter winning.
        //                    if (intDayCount[intI] > intWaitRotation)
        //                        chFilteredWinner[intI] = chWinningAsset[intI];
        //                    else
        //                        chFilteredWinner[intI] = chFilteredWinner[intI - 1];

        //                    //Calculate Rebalances/yr.
        //                    if (chFilteredWinner[intI].Equals(chFilteredWinner[intI - 1]))
        //                        intRebalancesPerYr[intI] = 0;
        //                    else
        //                        intRebalancesPerYr[intI] = 1;

        //                    //Calculate Rotated Daily Change.
        //                    if (chFilteredWinner[intI - 1] == 'A')
        //                        dblRotatedDailyChange[intI] = (lstREPColl[1].DblDailychange[intI] * dblAgressiveness) + lstREPColl[0].DblDailychange[intI] * (1 - dblAgressiveness);
        //                    else
        //                        dblRotatedDailyChange[intI] = (lstREPColl[0].DblDailychange[intI] * dblAgressiveness) + lstREPColl[1].DblDailychange[intI] * (1 - dblAgressiveness);

        //                    //Calculate Value Rotation.
        //                    dblValueRotation[intI] = dblValueRotation[intI - 1] * (1 + dblRotatedDailyChange[intI]);
        //                }
        //                if (chFilteredWinner[intI].Equals('B'))
        //                    intAssetWins[intI] = 0;
        //                else
        //                    intAssetWins[intI] = 1;
        //            }

        //            //double dblCAGRA = Math.Pow(lstREPColl[1].DblNormalizedPriceTrend[lstREPColl[1].DblNormalizedPriceTrend.Count() - 1], (1.0 / ((dblSlopeDiff.Count() - 216.0) / 260.0))) - 1.0;
        //            double dblCAGRA = Math.Pow(lstREPColl[1].DblNormalizedPriceTrend[lstREPColl[1].DblNormalizedPriceTrend.Count() - 1], (1.0 / ((dblSlopeDiff.Count() - 216.0) / (double)intTradingDays))) - 1.0;
        //            //double dblCAGRB = Math.Pow(lstREPColl[0].DblNormalizedPriceTrend[lstREPColl[0].DblNormalizedPriceTrend.Count() - 1], (1.0 / ((dblSlopeDiff.Count() - 216.0) / 260.0))) - 1.0;
        //            double dblCAGRB = Math.Pow(lstREPColl[0].DblNormalizedPriceTrend[lstREPColl[0].DblNormalizedPriceTrend.Count() - 1], (1.0 / ((dblSlopeDiff.Count() - 216.0) / (double)intTradingDays))) - 1.0;
        //            //double dblCAGRRotation = Math.Pow(dblValueRotation[dblValueRotation.Count() - 1], (1.0 / ((dblSlopeDiff.Count() - 216.0) / 260.0))) - 1.0;
        //            double dblCAGRRotation = Math.Pow(dblValueRotation[dblValueRotation.Count() - 1], (1.0 / ((dblSlopeDiff.Count() - 216.0) / (double)intTradingDays))) - 1.0;

        //            arrValues[0] = (dblCAGRA + dblCAGRB) / 2;
        //            arrValues[1] = dblCAGRRotation;

        //            //double dblAvg = intAssetWins.Skip(216).Average();
        //            // dblAvg=dblAvg*100;
        //            // if (dblAvg>=50)
        //            //     arrValues[2] = 1;
        //            // else
        //            //     arrValues[2] = 2;

        //            //if (dblCAGRA >= dblCAGRB)
        //            //    arrValues[2] = 2;
        //            //else
        //            //    arrValues[2] = 1;

        //            //if (intDayCount[intDayCount.Count() - 1] > intWaitRotation)
        //            //{
        //            //    if (dblSlopeDiff[dblSlopeDiff.Count() - 1] > dblThresholdRotation[dblThresholdRotation.Count() - 1])
        //            //        arrValues[2] = 1;
        //            //    else
        //            //        arrValues[2] = 2;
        //            //}
        //            //else
        //            //{

        //            //}

        //            //System.Diagnostics.Debugger.Break();
        //            //Choose winning asset from FilteredWinner.
        //            char chValue = chFilteredWinner[chFilteredWinner.Count() - 1];

        //            if (chValue.Equals('B'))
        //                arrValues[2] = 1;
        //            else
        //                arrValues[2] = 2;

        //            strPair = iID + ":" + kID;
        //            intAWins = intAssetWins;

        //            //If count is greater than zero indicates that record is present.
        //            if (objHEEntities.RotationEnginePair.Where(x => x.PortfolioSet.Id.Equals(intPortfolioID) && x.SymbolPair.Equals(strSymbolPair)).Count() > 0)
        //            {
        //                objHEEntities.RotationEnginePair.First(x => x.PortfolioSet.Id.Equals(intPortfolioID) && x.SymbolPair.Equals(strSymbolPair)).OptimalTbase = dblOptimumThreshold;
        //            }
        //            else
        //            {
        //                RotationEnginePair objRotationEngPair = new RotationEnginePair();
        //                objRotationEngPair.OptimalTbase = dblOptimumThreshold;
        //                objRotationEngPair.SymbolPair = strSymbolPair;
        //                objRotationEngPair.PortfolioSet = objHEEntities.PortfolioSet.First(x => x.Id.Equals(intPortfolioID));
        //                objHEEntities.AddToRotationEnginePair(objRotationEngPair);
        //            }

        //            if (isChecked)
        //            {
        //                //Export data to excel sheet...
        //                DataTable dt = new DataTable();
        //                string strFirstSymbol = objHEEntities.AssetSymbolSet.First(x => x.Id.Equals(iID)).Symbol;
        //                string strSecondSymbol = objHEEntities.AssetSymbolSet.First(x => x.Id.Equals(kID)).Symbol;

        //                //generate column header...
        //                DataColumn dcDate = new DataColumn("Date", typeof(string));
        //                dt.Columns.Add(dcDate);

        //                DataColumn dcFirstPrice = new DataColumn(strFirstSymbol + " Price", typeof(double));
        //                dt.Columns.Add(dcFirstPrice);

        //                DataColumn dcFirstNormalizedPrice = new DataColumn("Normalized Price " + strFirstSymbol, typeof(double));
        //                dt.Columns.Add(dcFirstNormalizedPrice);

        //                DataColumn dcFirstPriceTrend = new DataColumn(strFirstSymbol + " PriceTrend", typeof(double));
        //                dt.Columns.Add(dcFirstPriceTrend);

        //                DataColumn dcFirstNormalizedPriceTrend = new DataColumn("Normalized PriceTrend" + strFirstSymbol, typeof(double));
        //                dt.Columns.Add(dcFirstNormalizedPriceTrend);

        //                DataColumn dcFirstDailyChange = new DataColumn(strFirstSymbol + " Daily Change", typeof(double));
        //                dt.Columns.Add(dcFirstDailyChange);

        //                DataColumn dcFirstTrendState = new DataColumn(strFirstSymbol + " TrendState", typeof(string));
        //                dt.Columns.Add(dcFirstTrendState);

        //                DataColumn dcFirstSMA = new DataColumn(strFirstSymbol + " SMA", typeof(double));
        //                dt.Columns.Add(dcFirstSMA);

        //                DataColumn dcFirstSlope = new DataColumn(strFirstSymbol + " Slope", typeof(double));
        //                dt.Columns.Add(dcFirstSlope);

        //                DataColumn dcSecondPrice = new DataColumn(strSecondSymbol + " Price", typeof(double));
        //                dt.Columns.Add(dcSecondPrice);

        //                DataColumn dcSecondNormalizedPrice = new DataColumn("Normalized Price " + strSecondSymbol, typeof(double));
        //                dt.Columns.Add(dcSecondNormalizedPrice);

        //                DataColumn dcSecondPriceTrend = new DataColumn(strSecondSymbol + " PriceTrend", typeof(double));
        //                dt.Columns.Add(dcSecondPriceTrend);

        //                DataColumn dcSecondNormalizedPriceTrend = new DataColumn("Normalized PriceTrend" + strSecondSymbol, typeof(double));
        //                dt.Columns.Add(dcSecondNormalizedPriceTrend);

        //                DataColumn dcSecondDailyChange = new DataColumn(strSecondSymbol + " Daily Change", typeof(double));
        //                dt.Columns.Add(dcSecondDailyChange);

        //                DataColumn dcSecondTrendState = new DataColumn(strSecondSymbol + " TrendState", typeof(string));
        //                dt.Columns.Add(dcSecondTrendState);

        //                DataColumn dcSecondSMA = new DataColumn(strSecondSymbol + " SMA", typeof(double));
        //                dt.Columns.Add(dcSecondSMA);

        //                DataColumn dcSecondSlope = new DataColumn(strSecondSymbol + " Slope", typeof(double));
        //                dt.Columns.Add(dcSecondSlope);

        //                DataColumn dcSlopeDifference = new DataColumn("Slope Diff A-B", typeof(double));
        //                dt.Columns.Add(dcSlopeDifference);

        //                DataColumn dcWinningAsset = new DataColumn("Winning Asset", typeof(string));
        //                dt.Columns.Add(dcWinningAsset);

        //                DataColumn dcDayCount = new DataColumn("Day Count", typeof(int));
        //                dt.Columns.Add(dcDayCount);

        //                DataColumn dcFilteredWinner = new DataColumn("Filtered Winner", typeof(string));
        //                dt.Columns.Add(dcFilteredWinner);

        //                DataColumn dcRebalancesPerYr = new DataColumn("Rebalances/yr", typeof(int));
        //                dt.Columns.Add(dcRebalancesPerYr);

        //                DataColumn dcRotatedDailyChange = new DataColumn("Rotated Daily Change", typeof(double));
        //                dt.Columns.Add(dcRotatedDailyChange);

        //                DataColumn dcValueRotation = new DataColumn("Value Rotation", typeof(double));
        //                dt.Columns.Add(dcValueRotation);

        //                //Start binding data to the data column...

        //                //bind date.
        //                for (int i = 0; i < lstREPColl[0].DblDate.Count() - 1; i++)
        //                {
        //                    DataRow row = dt.NewRow();
        //                    row[dcDate] = DateTime.FromOADate(lstREPColl[0].DblDate[i]).ToShortDateString();
        //                    row[dcFirstPrice] = lstREPColl[0].DblPrice[i];
        //                    row[dcFirstNormalizedPrice] = lstREPColl[0].DblNormalizedPrice[i];
        //                    row[dcFirstPriceTrend] = lstREPColl[0].DblPriceTrend[i];
        //                    row[dcFirstNormalizedPriceTrend] = lstREPColl[0].DblNormalizedPriceTrend[i];
        //                    row[dcFirstDailyChange] = lstREPColl[0].DblDailychange[i];
        //                    row[dcFirstTrendState] = lstREPColl[0].StrState[i];
        //                    row[dcFirstSMA] = lstREPColl[0].DblSMA[i];
        //                    row[dcFirstSlope] = lstREPColl[0].DblSlope[i];

        //                    row[dcSecondPrice] = lstREPColl[1].DblPrice[i];
        //                    row[dcSecondNormalizedPrice] = lstREPColl[1].DblNormalizedPrice[i];
        //                    row[dcSecondPriceTrend] = lstREPColl[1].DblPriceTrend[i];
        //                    row[dcSecondNormalizedPriceTrend] = lstREPColl[1].DblNormalizedPriceTrend[i];
        //                    row[dcSecondDailyChange] = lstREPColl[1].DblDailychange[i];
        //                    row[dcSecondTrendState] = lstREPColl[1].StrState[i];
        //                    row[dcSecondSMA] = lstREPColl[1].DblSMA[i];
        //                    row[dcSecondSlope] = lstREPColl[1].DblSlope[i];

        //                    row[dcSlopeDifference] = dblSlopeDiff[i];
        //                    row[dcWinningAsset] = chWinningAsset[i];
        //                    row[dcDayCount] = intDayCount[i];
        //                    row[dcFilteredWinner] = chFilteredWinner[i];
        //                    row[dcRebalancesPerYr] = intRebalancesPerYr[i];
        //                    row[dcRotatedDailyChange] = dblRotatedDailyChange[i];
        //                    row[dcValueRotation] = dblValueRotation[i];

        //                    dt.Rows.Add(row);
        //                }

        //                //Finally export data to excel sheet.
        //                GridView gvExport = new GridView();
        //                gvExport.DataSource = dt;
        //                gvExport.DataBind();
        //                gvExport.EnableViewState = false;
        //                System.Globalization.CultureInfo myCItrad = new System.Globalization.CultureInfo("EN-US", true);
        //                System.IO.StringWriter oStringWriter = new System.IO.StringWriter(myCItrad);
        //                System.Web.UI.HtmlTextWriter oHtmlTextWriter = new System.Web.UI.HtmlTextWriter(oStringWriter);
        //                gvExport.RenderControl(oHtmlTextWriter);
        //                File.WriteAllText("C:\\Inetpub\\wwwroot\\HarborEast\\RotationEngineExcelSheets\\" + strFirstSymbol + strSecondSymbol + ".xls", oStringWriter.ToString());
        //            }
        //        }
        //    }
        //    catch (Exception)
        //    {

        //    }
        //    return arrValues;
        //}

        private double[] CompareAssetsRanking(int intPortfolioID, DateTime dtStartDate,DateTime dtLastDate, int intArraySize, int iID, int kID, bool isChecked, out string strPair, out int[] intAWins)
        {
            //System.Diagnostics.Debugger.Break();
            strPair = string.Empty;
            intAWins = null;
            double[] arrValues = new double[3];
            try
            {
                List<clsRotationEngineProperties> lstREPColl = new List<clsRotationEngineProperties>();
                double[] dblSlopeDiff = new double[intArraySize];
                double[] dblRotatedDailyChange = new double[intArraySize];
                double[] dblValueRotation = new double[intArraySize];
                char[] chWinningAsset = new char[intArraySize];
                char[] chFilteredWinner = new char[intArraySize];
                int[] intDayCount = new int[intArraySize];
                int[] intRebalancesPerYr = new int[intArraySize];
                int[] intAssetWins = new int[intArraySize];
                double dblOptimumThreshold = 0;
                int intWaitRotation = 40;

                //Retrieve the value of WaitRotation from the database.
                if (objHEEntities.RotationEngineVariableSet.FirstOrDefault() != null)
                {
                    intWaitRotation = (int)objHEEntities.RotationEngineVariableSet.FirstOrDefault().WaitRotation;
                }
// Change 13-Dec-2010
                double dblAgressiveness=0;

                PortfolioSet objPortfolioSet = new PortfolioSet();
                objPortfolioSet = objHEEntities.PortfolioSet.FirstOrDefault(x => x.Id.Equals(intPortfolioID));
                dblAgressiveness = objPortfolioSet.Aggressiveness;

                if (dblAgressiveness == 0)
                    dblAgressiveness = 1;

                //Generate a pair in the string format.
                string strSymbolPair = objHEEntities.AssetSymbolSet.FirstOrDefault(x => x.Id.Equals(iID)).Symbol + ":" + objHEEntities.AssetSymbolSet.FirstOrDefault(x => x.Id.Equals(kID)).Symbol;

                //Retrieve the Optimum threshold value from the database if historical rotation
                if (!isRotationHistorical)
                {
                    if (objHEEntities.RotationEnginePair.Where(x => x.SymbolPair.Equals(strSymbolPair) && x.PortfolioSet.Id.Equals(intPortfolioID)).Count() > 0)
                    {
                        dblOptimumThreshold = objHEEntities.RotationEnginePair.First(x => x.SymbolPair.Equals(strSymbolPair) && x.PortfolioSet.Id.Equals(intPortfolioID)).OptimalTbase;
                    }
                }

                if (intArraySize > 0)
                {
                    lstREPColl.Add(PopulateObjectProperties(dtStartDate, dtLastDate, intArraySize, iID));
                    lstREPColl.Add(PopulateObjectProperties(dtStartDate, dtLastDate, intArraySize, kID));

                    double dblLoopValMin = -0.0004;
                    double dblLoopValMax = 0.0004;
                    double dblLoopValIncrement = 0.00002;
                    double dblTempLoopValMin = dblLoopValMin;
                    int intTempCount = 0;

                    while (dblTempLoopValMin <= dblLoopValMax)
                    {
                        dblTempLoopValMin = dblTempLoopValMin + dblLoopValIncrement;
                        intTempCount++;
                    }

                    double[] dblThresholdRotation = new double[intTempCount];
                    double[] dblEndingValue = new double[intTempCount];

                    dblTempLoopValMin = dblLoopValMin;
                    int intCount = 0;

                    if (dblOptimumThreshold == 0)
                    {
                        //Calculate Threshold rotation optimum.
                        while (dblTempLoopValMin <= dblLoopValMax)
                        {
                            dblOptimumThreshold = dblTempLoopValMin;

                            for (int intI = 216; intI < intArraySize; intI++)
                            {
  // Change 13-Dec-2010
                                if (intI == 216)
                                {
                                    chWinningAsset[intI] = 'A';
                                    intDayCount[intI] = 1;
                                    chFilteredWinner[intI] = 'A';
                                    intRebalancesPerYr[intI] = 1;
                                    dblValueRotation[intI] = 1;
                                }
                                if (intI >= 217)
                                {
                                    //Calculate slope difference.
                                    dblSlopeDiff[intI] = lstREPColl[0].DblSlope[intI] - lstREPColl[1].DblSlope[intI];

                                    //Calculate Winning asset.
                                    if (dblSlopeDiff[intI] > dblOptimumThreshold)
                                        chWinningAsset[intI] = 'A';
                                    else
                                        chWinningAsset[intI] = 'B';

                                    //Calculate day count.
                                    if (chWinningAsset[intI].Equals(chWinningAsset[intI - 1]))
                                        intDayCount[intI] = intDayCount[intI - 1] + 1;
                                    else
                                        intDayCount[intI] = 1;

                                    //Calculate filter winning.
                                    if (intDayCount[intI] > intWaitRotation)
                                        chFilteredWinner[intI] = chWinningAsset[intI];
                                    else
                                        chFilteredWinner[intI] = chFilteredWinner[intI - 1];

                                    //Calculate Rebalances/yr.
                                    if (chFilteredWinner[intI].Equals(chFilteredWinner[intI - 1]))
                                        intRebalancesPerYr[intI] = 0;
                                    else
                                        intRebalancesPerYr[intI] = 1;

                                    //Calculate Rotated Daily Change.
  // Change 13-Dec-2010
                                    if (chFilteredWinner[intI - 1] == 'A')
                                        dblRotatedDailyChange[intI] = (lstREPColl[0].DblDailychange[intI] * dblAgressiveness) + lstREPColl[1].DblDailychange[intI] * (1 - dblAgressiveness);
                                    else
                                        dblRotatedDailyChange[intI] = (lstREPColl[1].DblDailychange[intI] * dblAgressiveness) + lstREPColl[0].DblDailychange[intI] * (1 - dblAgressiveness);

                                    //Calculate Value Rotation.
                                    dblValueRotation[intI] = dblValueRotation[intI - 1] * (1 + dblRotatedDailyChange[intI]);
                                }
                            }

                            dblThresholdRotation[intCount] = dblOptimumThreshold;
                            dblEndingValue[intCount] = dblValueRotation[dblValueRotation.Count() - 1];
                            dblTempLoopValMin = dblTempLoopValMin + dblLoopValIncrement;
                            intCount++;
                        }

                        //Finally get the optimum threshold value.
                        for (int i = 0; i < intTempCount; i++)
                        {
                            if (dblEndingValue[i] == dblEndingValue.Max())
                            {
                                dblOptimumThreshold = dblThresholdRotation[i];
                                break;
                            }
                        }
                    }

                    //Calculate the fresh values with the newly calculated Threshold value.
                    for (int intI = 216; intI < intArraySize; intI++)
                    {
                        if (intI == 216)
                        {
 // Change 13-Dec-2010
                            chWinningAsset[intI] = 'A';
                            intDayCount[intI] = 1;
                            chFilteredWinner[intI] = 'A';
                            intRebalancesPerYr[intI] = 1;
                            dblValueRotation[intI] = 1;

                            //** code added for RR
                            dtRankDates[intI] = DateTime.FromOADate(lstREPColl[0].DblDate[intI]);
                            dblDailyChange1[intI] = lstREPColl[0].DblDailychange[intI];
                            dblDailyChange2[intI] = lstREPColl[1].DblDailychange[intI];
                            dblPriceTrend1[intI] = lstREPColl[0].DblPriceTrend[intI];
                            dblPriceTrend2[intI] = lstREPColl[1].DblPriceTrend[intI];
                            dblPrice1[intI] = lstREPColl[0].DblPrice[intI];
                            dblPrice2[intI] = lstREPColl[1].DblPrice[intI];
                            //** end RR code
                            
                        }
                        if (intI >= 217)
                        {
                            //** code added for RR
                            dtRankDates[intI] = DateTime.FromOADate(lstREPColl[0].DblDate[intI]);
                            dblDailyChange1[intI] = lstREPColl[0].DblDailychange[intI];
                            dblDailyChange2[intI] = lstREPColl[1].DblDailychange[intI];
                            dblPriceTrend1[intI] = lstREPColl[0].DblPriceTrend[intI];
                            dblPriceTrend2[intI] = lstREPColl[1].DblPriceTrend[intI];
                            dblPrice1[intI] = lstREPColl[0].DblPrice[intI];
                            dblPrice2[intI] = lstREPColl[1].DblPrice[intI];
                            //** end RR code
                            

                            //Calculate slope difference.
// Change 13-Dec-2010
                            dblSlopeDiff[intI] = lstREPColl[0].DblSlope[intI] - lstREPColl[1].DblSlope[intI];

                            //Calculate Winning asset.
                            if (dblSlopeDiff[intI] > dblOptimumThreshold)
                                chWinningAsset[intI] = 'A';
                            else
                                chWinningAsset[intI] = 'B';

                            //Calculate day count.
                            if (chWinningAsset[intI].Equals(chWinningAsset[intI - 1]))
                                intDayCount[intI] = intDayCount[intI - 1] + 1;
                            else
                                intDayCount[intI] = 1;

                            //Calculate filter winning.
                            if (intDayCount[intI] > intWaitRotation)
                                chFilteredWinner[intI] = chWinningAsset[intI];
                            else
                                chFilteredWinner[intI] = chFilteredWinner[intI - 1];

                            //Calculate Rebalances/yr.
                            if (chFilteredWinner[intI].Equals(chFilteredWinner[intI - 1]))
                                intRebalancesPerYr[intI] = 0;
                            else
                                intRebalancesPerYr[intI] = 1;

                            //Calculate Rotated Daily Change.
// Change 13-Dec-2010
                            if (chFilteredWinner[intI - 1] == 'A')
                                dblRotatedDailyChange[intI] = (lstREPColl[0].DblDailychange[intI] * dblAgressiveness) + lstREPColl[1].DblDailychange[intI] * (1 - dblAgressiveness);
                            else
                                dblRotatedDailyChange[intI] = (lstREPColl[1].DblDailychange[intI] * dblAgressiveness) + lstREPColl[0].DblDailychange[intI] * (1 - dblAgressiveness);

                            //Calculate Value Rotation.
                            dblValueRotation[intI] = dblValueRotation[intI - 1] * (1 + dblRotatedDailyChange[intI]);
                        }
                        if (chFilteredWinner[intI].Equals('B'))
                            intAssetWins[intI] = 0;
                        else
                            intAssetWins[intI] = 1;
                    }

                    //double dblCAGRA = Math.Pow(lstREPColl[1].DblNormalizedPriceTrend[lstREPColl[1].DblNormalizedPriceTrend.Count() - 1], (1.0 / ((dblSlopeDiff.Count() - 216.0) / 260.0))) - 1.0;
                    double dblCAGRA = Math.Pow(lstREPColl[1].DblNormalizedPriceTrend[lstREPColl[1].DblNormalizedPriceTrend.Count() - 1], (1.0 / ((dblSlopeDiff.Count() - 216.0) / (double)intTradingDays))) - 1.0;
                    //double dblCAGRB = Math.Pow(lstREPColl[0].DblNormalizedPriceTrend[lstREPColl[0].DblNormalizedPriceTrend.Count() - 1], (1.0 / ((dblSlopeDiff.Count() - 216.0) / 260.0))) - 1.0;
                    double dblCAGRB = Math.Pow(lstREPColl[0].DblNormalizedPriceTrend[lstREPColl[0].DblNormalizedPriceTrend.Count() - 1], (1.0 / ((dblSlopeDiff.Count() - 216.0) / (double)intTradingDays))) - 1.0;
                    //double dblCAGRRotation = Math.Pow(dblValueRotation[dblValueRotation.Count() - 1], (1.0 / ((dblSlopeDiff.Count() - 216.0) / 260.0))) - 1.0;
                    double dblCAGRRotation = Math.Pow(dblValueRotation[dblValueRotation.Count() - 1], (1.0 / ((dblSlopeDiff.Count() - 216.0) / (double)intTradingDays))) - 1.0;

                    arrValues[0] = (dblCAGRA + dblCAGRB) / 2;
                    arrValues[1] = dblCAGRRotation;

                    //double dblAvg = intAssetWins.Skip(216).Average();
                    // dblAvg=dblAvg*100;
                    // if (dblAvg>=50)
                    //     arrValues[2] = 1;
                    // else
                    //     arrValues[2] = 2;

                    //if (dblCAGRA >= dblCAGRB)
                    //    arrValues[2] = 2;
                    //else
                    //    arrValues[2] = 1;

                    //if (intDayCount[intDayCount.Count() - 1] > intWaitRotation)
                    //{
                    //    if (dblSlopeDiff[dblSlopeDiff.Count() - 1] > dblThresholdRotation[dblThresholdRotation.Count() - 1])
                    //        arrValues[2] = 1;
                    //    else
                    //        arrValues[2] = 2;
                    //}
                    //else
                    //{

                    //}

                    //System.Diagnostics.Debugger.Break();
                    //Choose winning asset from FilteredWinner.
                    char chValue = chFilteredWinner[chFilteredWinner.Count() - 1];

                    if (chValue.Equals('A'))
                        arrValues[2] = 1;
                    else
                        arrValues[2] = 2;

                    strPair = iID + ":" + kID;
                    intAWins = intAssetWins;

                    //If count is greater than zero indicates that record is present.
                    if (objHEEntities.RotationEnginePair.Where(x => x.PortfolioSet.Id.Equals(intPortfolioID) && x.SymbolPair.Equals(strSymbolPair)).Count() > 0)
                    {
                        objHEEntities.RotationEnginePair.First(x => x.PortfolioSet.Id.Equals(intPortfolioID) && x.SymbolPair.Equals(strSymbolPair)).OptimalTbase = dblOptimumThreshold;
                    }
                    else
                    {
                        RotationEnginePair objRotationEngPair = new RotationEnginePair();
                        objRotationEngPair.OptimalTbase = dblOptimumThreshold;
                        objRotationEngPair.SymbolPair = strSymbolPair;
                        objRotationEngPair.PortfolioSet = objHEEntities.PortfolioSet.First(x => x.Id.Equals(intPortfolioID));
                        objHEEntities.AddToRotationEnginePair(objRotationEngPair);
                    }

                    if (isChecked)
                    {
                        //Export data to excel sheet...
                        DataTable dt = new DataTable();
                        string strFirstSymbol = objHEEntities.AssetSymbolSet.First(x => x.Id.Equals(iID)).Symbol;
                        string strSecondSymbol = objHEEntities.AssetSymbolSet.First(x => x.Id.Equals(kID)).Symbol;

                        //generate column header...
                        DataColumn dcDate = new DataColumn("Date", typeof(string));
                        dt.Columns.Add(dcDate);

                        DataColumn dcFirstPrice = new DataColumn(strFirstSymbol + " Price", typeof(double));
                        dt.Columns.Add(dcFirstPrice);

                        DataColumn dcFirstNormalizedPrice = new DataColumn("Normalized Price " + strFirstSymbol, typeof(double));
                        dt.Columns.Add(dcFirstNormalizedPrice);

                        DataColumn dcFirstPriceTrend = new DataColumn(strFirstSymbol + " PriceTrend", typeof(double));
                        dt.Columns.Add(dcFirstPriceTrend);

                        DataColumn dcFirstNormalizedPriceTrend = new DataColumn("Normalized PriceTrend" + strFirstSymbol, typeof(double));
                        dt.Columns.Add(dcFirstNormalizedPriceTrend);

                        DataColumn dcFirstDailyChange = new DataColumn(strFirstSymbol + " Daily Change", typeof(double));
                        dt.Columns.Add(dcFirstDailyChange);

                        DataColumn dcFirstTrendState = new DataColumn(strFirstSymbol + " TrendState", typeof(string));
                        dt.Columns.Add(dcFirstTrendState);

                        DataColumn dcFirstSMA = new DataColumn(strFirstSymbol + " SMA", typeof(double));
                        dt.Columns.Add(dcFirstSMA);

                        DataColumn dcFirstSlope = new DataColumn(strFirstSymbol + " Slope", typeof(double));
                        dt.Columns.Add(dcFirstSlope);

                        DataColumn dcSecondPrice = new DataColumn(strSecondSymbol + " Price", typeof(double));
                        dt.Columns.Add(dcSecondPrice);

                        DataColumn dcSecondNormalizedPrice = new DataColumn("Normalized Price " + strSecondSymbol, typeof(double));
                        dt.Columns.Add(dcSecondNormalizedPrice);

                        DataColumn dcSecondPriceTrend = new DataColumn(strSecondSymbol + " PriceTrend", typeof(double));
                        dt.Columns.Add(dcSecondPriceTrend);

                        DataColumn dcSecondNormalizedPriceTrend = new DataColumn("Normalized PriceTrend" + strSecondSymbol, typeof(double));
                        dt.Columns.Add(dcSecondNormalizedPriceTrend);

                        DataColumn dcSecondDailyChange = new DataColumn(strSecondSymbol + " Daily Change", typeof(double));
                        dt.Columns.Add(dcSecondDailyChange);

                        DataColumn dcSecondTrendState = new DataColumn(strSecondSymbol + " TrendState", typeof(string));
                        dt.Columns.Add(dcSecondTrendState);

                        DataColumn dcSecondSMA = new DataColumn(strSecondSymbol + " SMA", typeof(double));
                        dt.Columns.Add(dcSecondSMA);

                        DataColumn dcSecondSlope = new DataColumn(strSecondSymbol + " Slope", typeof(double));
                        dt.Columns.Add(dcSecondSlope);

                        DataColumn dcSlopeDifference = new DataColumn("Slope Diff A-B", typeof(double));
                        dt.Columns.Add(dcSlopeDifference);

                        DataColumn dcWinningAsset = new DataColumn("Winning Asset", typeof(string));
                        dt.Columns.Add(dcWinningAsset);

                        DataColumn dcDayCount = new DataColumn("Day Count", typeof(int));
                        dt.Columns.Add(dcDayCount);

                        DataColumn dcFilteredWinner = new DataColumn("Filtered Winner", typeof(string));
                        dt.Columns.Add(dcFilteredWinner);

                        DataColumn dcRebalancesPerYr = new DataColumn("Rebalances/yr", typeof(int));
                        dt.Columns.Add(dcRebalancesPerYr);

                        DataColumn dcRotatedDailyChange = new DataColumn("Rotated Daily Change", typeof(double));
                        dt.Columns.Add(dcRotatedDailyChange);

                        DataColumn dcValueRotation = new DataColumn("Value Rotation", typeof(double));
                        dt.Columns.Add(dcValueRotation);

                        //Start binding data to the data column...

                        //bind date.
                        for (int i = 0; i < lstREPColl[0].DblDate.Count() - 1; i++)
                        {
                            DataRow row = dt.NewRow();
                            row[dcDate] = DateTime.FromOADate(lstREPColl[0].DblDate[i]).ToShortDateString();
                            row[dcFirstPrice] = lstREPColl[0].DblPrice[i];
                            row[dcFirstNormalizedPrice] = lstREPColl[0].DblNormalizedPrice[i];
                            row[dcFirstPriceTrend] = lstREPColl[0].DblPriceTrend[i];
                            row[dcFirstNormalizedPriceTrend] = lstREPColl[0].DblNormalizedPriceTrend[i];
                            row[dcFirstDailyChange] = lstREPColl[0].DblDailychange[i];
                            row[dcFirstTrendState] = lstREPColl[0].StrState[i];
                            row[dcFirstSMA] = lstREPColl[0].DblSMA[i];
                            row[dcFirstSlope] = lstREPColl[0].DblSlope[i];

                            row[dcSecondPrice] = lstREPColl[1].DblPrice[i];
                            row[dcSecondNormalizedPrice] = lstREPColl[1].DblNormalizedPrice[i];
                            row[dcSecondPriceTrend] = lstREPColl[1].DblPriceTrend[i];
                            row[dcSecondNormalizedPriceTrend] = lstREPColl[1].DblNormalizedPriceTrend[i];
                            row[dcSecondDailyChange] = lstREPColl[1].DblDailychange[i];
                            row[dcSecondTrendState] = lstREPColl[1].StrState[i];
                            row[dcSecondSMA] = lstREPColl[1].DblSMA[i];
                            row[dcSecondSlope] = lstREPColl[1].DblSlope[i];

                            row[dcSlopeDifference] = dblSlopeDiff[i];
                            row[dcWinningAsset] = chWinningAsset[i];
                            row[dcDayCount] = intDayCount[i];
                            row[dcFilteredWinner] = chFilteredWinner[i];
                            row[dcRebalancesPerYr] = intRebalancesPerYr[i];
                            row[dcRotatedDailyChange] = dblRotatedDailyChange[i];
                            row[dcValueRotation] = dblValueRotation[i];

                            dt.Rows.Add(row);
                        }

                        //Finally export data to excel sheet.
                        GridView gvExport = new GridView();
                        gvExport.DataSource = dt;
                        gvExport.DataBind();
                        gvExport.EnableViewState = false;
                        System.Globalization.CultureInfo myCItrad = new System.Globalization.CultureInfo("EN-US", true);
                        System.IO.StringWriter oStringWriter = new System.IO.StringWriter(myCItrad);
                        System.Web.UI.HtmlTextWriter oHtmlTextWriter = new System.Web.UI.HtmlTextWriter(oStringWriter);
                        gvExport.RenderControl(oHtmlTextWriter);
                        File.WriteAllText("C:\\Inetpub\\wwwroot\\HarborEast\\RotationEngineExcelSheets\\" + strFirstSymbol + strSecondSymbol + ".xls", oStringWriter.ToString());
                    }
                }
            }
            catch (Exception)
            {

            }
            return arrValues;
        }

        /// <summary>
        /// This method is used to populate the properties for clsRotationEngineProperties class.
        /// </summary>
        /// <param name="dtStartDate"></param>
        /// <param name="intArraySize"></param>
        /// <param name="assetSymbolID"></param>
        /// <returns></returns>
        private clsRotationEngineProperties PopulateObjectProperties(DateTime dtStartDate, DateTime dtLastDate, int intArraySize, int assetSymbolID)
        {
            clsRotationEngineProperties objREP = new clsRotationEngineProperties(intArraySize);
            try
            {
                //Set all the properties of the object with the AssetPriceSet table data.
                objREP.IntAssetSymbolID = assetSymbolID;
                objREP.DblDate = objHEEntities.AssetPriceSet.Where(x => x.PriceTrend != null && x.Date >= dtStartDate && x.Date <= dtLastDate && x.AssetSymbolSet.Id.Equals(assetSymbolID)).OrderBy(x => x.Date).Select(x => x.Date).ToArray().Select(x => x.ToOADate()).ToArray();
                objREP.DblPrice = objHEEntities.AssetPriceSet.Where(x => x.PriceTrend != null && x.Date >= dtStartDate && x.Date <= dtLastDate && x.AssetSymbolSet.Id.Equals(assetSymbolID)).OrderBy(x => x.Date).Select(x => (double)x.Price).ToArray();
                objREP.DblPriceTrend = objHEEntities.AssetPriceSet.Where(x => x.PriceTrend != null && x.Date >= dtStartDate && x.Date <= dtLastDate && x.AssetSymbolSet.Id.Equals(assetSymbolID)).OrderBy(x => x.Date).Select(x => (double)x.PriceTrend).ToArray();
                objREP.StrState = objHEEntities.AssetPriceSet.Where(x => x.PriceTrend != null && x.Date >= dtStartDate && x.Date <= dtLastDate && x.AssetSymbolSet.Id.Equals(assetSymbolID)).OrderBy(x => x.Date).Select(x => x.FilterState).ToArray();

                int intSMARotation = 50;

                //Get the SMARotation value from the RotationEngineVariableSet database.
                if (objHEEntities.RotationEngineVariableSet.FirstOrDefault() != null)
                {
                    intSMARotation = (int)objHEEntities.RotationEngineVariableSet.FirstOrDefault().SMARotation;
                }
// Change 13-Dec-2010
                double dblConstPrice = objREP.DblPrice[216];
                double dblConstPriceTrend = objREP.DblPriceTrend[216];

                int intSMASkipCount = 163;
                int intSlopeSkipCount = 213;
                double dblConstDailyChange = 0.000096153846153899;

                for (int intI = 163; intI < intArraySize; intI++)
                {
                    if (intI == 216)
                    {
                        //set the Constant Price and Price Trend values.
                        dblConstPrice = objREP.DblPrice[intI];
                        dblConstPriceTrend = objREP.DblPriceTrend[intI];
                    }

                    //Calculate the Normalized Price.
                    if (dblConstPrice != 0)
                        objREP.DblNormalizedPrice[intI] = objREP.DblPrice[intI] / dblConstPrice;
                    else
                        objREP.DblNormalizedPrice[intI] = 0;

                    //Calculate the Normalized Price Trend.
                    if (dblConstPriceTrend != 0)
                        objREP.DblNormalizedPriceTrend[intI] = objREP.DblPriceTrend[intI] / dblConstPriceTrend;
                    else
                        objREP.DblNormalizedPriceTrend[intI] = 0;

                    //Depending upon the Current state value calculate the daily change.
                    if (intI >= 164)
                    {
                        //if (objREP.StrState[intI].ToLower().Equals("in"))
                        //    objREP.DblDailychange[intI] = (objREP.DblPrice[intI] - objREP.DblPrice[intI - 1]) / objREP.DblPrice[intI - 1];
                        //else
                        //    objREP.DblDailychange[intI] = dblConstDailyChange;

                        if (objREP.StrState[intI].ToLower().Equals("in"))
                            objREP.DblDailychange[intI] = (objREP.DblPriceTrend[intI] - objREP.DblPriceTrend[intI - 1]) / objREP.DblPriceTrend[intI - 1];
                        else
                            if (objREP.StrState[intI - 1].ToLower().Equals("out"))
                                objREP.DblDailychange[intI] = dblConstDailyChange;
                            else
                                objREP.DblDailychange[intI] = (objREP.DblPriceTrend[intI] - objREP.DblPriceTrend[intI - 1]) / objREP.DblPriceTrend[intI - 1];

                        //if (set.MState[intI].ToLower().Equals("in"))
                        //    set.MDailyChange[intI] = (set.MPriceTrend[intI] - set.MPriceTrend[intI - 1]) / set.MPriceTrend[intI - 1];
                        //else
                        //    if (set.MState[intI - 1].ToLower().Equals("out"))
                        //        set.MDailyChange[intI] = decReturnCash;
                        //    else
                        //        set.MDailyChange[intI] = (set.MPriceTrend[intI] - set.MPriceTrend[intI - 1]) / set.MPriceTrend[intI - 1];
                    }

                    //Calculate the SMA value.
                    if (intI >= 213)
                    {
                        objREP.DblSMA[intI] = objREP.DblNormalizedPriceTrend.Skip(intSMASkipCount).Take(intSMARotation).Average();
                        intSMASkipCount++;
                    }

                    //Calculate the Slope.
                    if (intI >= 217)
                    {
                        objREP.DblSlope[intI] = (CommonUtility.Slope(objREP.DblSMA.Skip(intSlopeSkipCount).Take(5).Select(x => x).ToArray(), objREP.DblDate.Skip(intSlopeSkipCount).Take(5).ToArray()) / (double)objREP.DblSMA[intI]);
                        intSlopeSkipCount++;
                    }
                }
            }
            catch (Exception)
            {

            }
            return objREP;
        }

        
        #endregion

        #region Public Methods

        /// <summary>
        /// This method is used to perform the Rotation Engine process for a particular portfolio ID passed as a parameter.
        /// </summary>
        /// <param name="intSelectedIndex"></param>
        /// <param name="strSelectedValue"></param>
        /// <param name="isNegativePairtoZero"></param>
        /// <param name="isChecked"></param>
        /// <param name="divTable"></param>
        /// <param name="divColorCode"></param>
        //public void PerformRotationEngine(int intSelectedIndex, string strSelectedValue, bool isNegativePairtoZero, bool isChecked, HtmlGenericControl divTable, HtmlGenericControl divColorCode, string[] arrExcludeSymSet)
        //{
        //    //System.Diagnostics.Debugger.Break();
        //    try
        //    {
        //        if (intSelectedIndex != -1)
        //        {
        //            int intID = Convert.ToInt32(strSelectedValue);
        //            List<PortfolioContentSet> lstPortfolioContentSet = objHEEntities.PortfolioContentSet.Where(x => x.PortfolioSet.Id.Equals(intID)).ToList();
        //            if (lstPortfolioContentSet != null)
        //            {
        //                #region Variable Initializations

        //                DateTime dtFinalStartDate = new DateTime(1, 1, 1);
        //                List<Pairs> combos = new List<Pairs>();
        //                List<Pairs> lstPerms = new List<Pairs>();
        //                List<double> lstRotatImpArr = new List<double>();
        //                Dictionary<string, double> dictRotationImp = new Dictionary<string, double>();
        //                Dictionary<string, double> dictFinalRotationImp = new Dictionary<string, double>();
        //                Dictionary<string, double> dictOriginalRotationImp = new Dictionary<string, double>();
        //                Dictionary<string, double[]> dictArr = new Dictionary<string, double[]>();
        //                Dictionary<string, int[]> dictLstData = new Dictionary<string, int[]>();
        //                Dictionary<double, string[]> dictPairComb = new Dictionary<double, string[]>();
        //                Dictionary<string, double> dictRotationAvg = new Dictionary<string, double>();
        //                double[] dblRotationImp = null;
        //                int intCount = 0;
        //                int intAssetSymbolID = 0;
        //                int intArraySize = 0;

        //                #endregion

        //                #region Get the earliest trading date for Portfolio

        //                //Get the start date of the least asset from the portfolio. 
        //                foreach (var objSet in lstPortfolioContentSet)
        //                {
        //                    //Get the start date for this asset.
        //                    DateTime dtStartDate = CommonUtility.GetStartDate(Convert.ToInt32(((EntityReference)(objSet.AssetSymbolSetReference)).EntityKey.EntityKeyValues[0].Value));
        //                    if (intCount == 0)
        //                    {
        //                        dtFinalStartDate = dtStartDate;

        //                        //Get the asset symbol id.
        //                        intAssetSymbolID = Convert.ToInt32(((EntityReference)(objSet.AssetSymbolSetReference)).EntityKey.EntityKeyValues[0].Value);
        //                        intCount++;
        //                    }
        //                    else
        //                    {
        //                        if (dtStartDate >= dtFinalStartDate)
        //                        {
        //                            dtFinalStartDate = dtStartDate;
        //                            intAssetSymbolID = Convert.ToInt32(((EntityReference)(objSet.AssetSymbolSetReference)).EntityKey.EntityKeyValues[0].Value);
        //                        }
        //                    }
        //                }

        //                #endregion

        //                #region Get the array size for this selected Portfolio from the AssetPriceSet table.

        //                //Get the actual size of array to be initialized.
        //                intArraySize = CommonUtility.GetArraySize(intAssetSymbolID);

        //                #endregion

        //                //HttpContext.Current.Response.Write("Paired Combos. &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; CAGR Ave Trend. &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; CAGR Rotation.&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;Winning Asset.&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;Rotation Improvement over average.<br>");
        //                //HttpContext.Current.Response.Write("-------------------------------------------------------------------------------------------------------------------------------------------");

        //                if (lstPortfolioContentSet.Count % 2 == 0)
        //                    dblRotationImp = new double[lstPortfolioContentSet.Count - 1];
        //                else
        //                    dblRotationImp = new double[lstPortfolioContentSet.Count];

        //                #region  This section is used for Comparing all Assets with each other.

        //                for (int i = 0; i < lstPortfolioContentSet.Count; i++)
        //                {
        //                    int iID = Convert.ToInt32(((System.Data.Objects.DataClasses.EntityReference)(lstPortfolioContentSet[i].AssetSymbolSetReference)).EntityKey.EntityKeyValues[0].Value);
        //                    for (int k = i + 1; k < lstPortfolioContentSet.Count; k++)
        //                    {
        //                        string strPair;
        //                        int[] intAWins;
        //                        Pairs objPairs = new Pairs(Convert.ToInt32(((System.Data.Objects.DataClasses.EntityReference)(lstPortfolioContentSet.ElementAt(i).AssetSymbolSetReference)).EntityKey.EntityKeyValues[0].Value), Convert.ToInt32(((System.Data.Objects.DataClasses.EntityReference)(lstPortfolioContentSet.ElementAt(k).AssetSymbolSetReference)).EntityKey.EntityKeyValues[0].Value));
        //                        lstPerms.Add(objPairs);
        //                        int kID = Convert.ToInt32(((System.Data.Objects.DataClasses.EntityReference)(lstPortfolioContentSet[k].AssetSymbolSetReference)).EntityKey.EntityKeyValues[0].Value);

        //                        double[] arrValues = CompareAssets(intID, dtFinalStartDate, intArraySize, iID, kID, isChecked, out strPair, out intAWins);
        //                        if (!dictArr.ContainsKey(iID + ":" + kID))
        //                            dictArr.Add(iID + ":" + kID, arrValues);
        //                        if (!dictLstData.ContainsKey(strPair))
        //                            dictLstData.Add(strPair, intAWins);
        //                    }
        //                }

        //                #endregion

        //                #region This region iterates through the dictinonary object and calculates the Rotation improvement.

        //                foreach (string strPair in dictLstData.Keys)
        //                {
        //                    double avgCAGRTrend = 0;
        //                    double avgCAGRRotation = 0;
        //                    string[] strPairColl = strPair.Split(":".ToCharArray());
        //                    int intFirstID = Convert.ToInt32(strPairColl[0]);
        //                    int intSecondID = Convert.ToInt32(strPairColl[1]);
        //                    string strFirstSymbol = objHEEntities.AssetSymbolSet.FirstOrDefault(x => x.Id.Equals(intFirstID)).Symbol;
        //                    string strSecondSymbol = objHEEntities.AssetSymbolSet.FirstOrDefault(x => x.Id.Equals(intSecondID)).Symbol;
        //                    double[] arrValues;
        //                    dictArr.TryGetValue(intFirstID + ":" + intSecondID, out arrValues);
        //                    avgCAGRTrend = avgCAGRTrend + arrValues[0];
        //                    avgCAGRRotation = avgCAGRRotation + arrValues[1];
        //                    //HttpContext.Current.Response.Write("%<br>" + strFirstSymbol + " : " + strSecondSymbol + "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;" + String.Format("{0:0.000}", arrValues[0] * 100) + "%&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;" + String.Format("{0:0.000}", arrValues[1] * 100) + "%&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;" + (arrValues[2] == 1 ? strFirstSymbol : strSecondSymbol) + "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;" + String.Format("{0:0.000}", ((arrValues[1] - arrValues[0]) / arrValues[0]) * 100));
        //                    dictRotationImp.Add(intFirstID + ":" + intSecondID, ((arrValues[1] - arrValues[0]) / arrValues[0]) * 100);
        //                    dictOriginalRotationImp.Add(intFirstID + ":" + intSecondID, ((arrValues[1] - arrValues[0]) / arrValues[0]) * 100);
        //                    lstRotatImpArr.Add(((arrValues[1] - arrValues[0]) / arrValues[0]) * 100);
        //                }

        //                #endregion

        //                #region This region iterates through the array list and finds the highest yeilding Rotation improvement pairs and stores the result in the dictionary.

        //                while (lstRotatImpArr.Count != 0)
        //                {
        //                    double dblMaxRotatValue = lstRotatImpArr.Max();
        //                    foreach (string strKey in dictRotationImp.Keys)
        //                    {
        //                        double dblRotVal = 0;
        //                        dictRotationImp.TryGetValue(strKey, out dblRotVal);
        //                        if (dblMaxRotatValue == dblRotVal)
        //                        {
        //                            dictFinalRotationImp.Add(strKey, dblRotVal);
        //                            dictRotationImp = ValidateDictionaryObject(dictRotationImp, strKey, lstRotatImpArr);
        //                            break;
        //                        }
        //                    }
        //                    if (lstRotatImpArr.Contains(dblMaxRotatValue))
        //                        lstRotatImpArr.Remove(dblMaxRotatValue);
        //                }

        //                #endregion

        //                #region This region is used to Print the Best Rotation Improvement pairs along with their Rotation Improvement values.

        //                //HttpContext.Current.Response.Write("<br><br>Best Pair Unique Combinations:<br>");
        //                //HttpContext.Current.Response.Write("Pair&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;Rotation Value<br>");
        //                foreach (string strBestPair in dictFinalRotationImp.Keys)
        //                {
        //                    string[] strPairCollection = strBestPair.Split(":".ToCharArray());
        //                    int intFirstSymbol = Convert.ToInt32(strPairCollection[0]);
        //                    int intSecondSymbol = Convert.ToInt32(strPairCollection[1]);
        //                    string strFirstSymbol = objHEEntities.AssetSymbolSet.FirstOrDefault(x => x.Id.Equals(intFirstSymbol)).Symbol;
        //                    string strSecondSymbol = objHEEntities.AssetSymbolSet.FirstOrDefault(x => x.Id.Equals(intSecondSymbol)).Symbol;
        //                    double dblPairValue = 0;
        //                    dictFinalRotationImp.TryGetValue(strBestPair, out dblPairValue);
        //                    //HttpContext.Current.Response.Write(strFirstSymbol + ":" + strSecondSymbol + "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;" + dblPairValue + "<br>");
        //                }

        //                #endregion

        //                #region This region is used to fill the dictionary object with the best improvement pairs.

        //                //dictPairComb.TryGetValue(dblRotationImp.Max(), out strWinningPairs);
        //                Dictionary<string, int[]> dictFinalData = new Dictionary<string, int[]>();
        //                if (dictFinalRotationImp.Count() > 0)
        //                {
        //                    foreach (string strPair in dictFinalRotationImp.Keys)
        //                    {
        //                        if (dictLstData.ContainsKey(strPair))
        //                        {
        //                            int[] intWinAssetArr;
        //                            dictLstData.TryGetValue(strPair, out intWinAssetArr);
        //                            if (!dictFinalData.ContainsKey(strPair))
        //                                dictFinalData.Add(strPair, intWinAssetArr);
        //                        }
        //                    }
        //                }

        //                #endregion

        //                #region This region is used to add the Odd Asset entry into the existing Dictionary object.

        //                //If asset count is odd.
        //                ArrayList arrLst = new ArrayList();
        //                string strOddAssetSymb = string.Empty;
        //                if (lstPortfolioContentSet.Count % 2 != 0)
        //                {
        //                    foreach (string strPair in dictFinalRotationImp.Keys)
        //                    {
        //                        foreach (string strSymbol in strPair.Split(":".ToCharArray()))
        //                        {
        //                            arrLst.Add(strSymbol);
        //                        }
        //                    }
        //                    foreach (var objSet in lstPortfolioContentSet)
        //                    {
        //                        string strVal = Convert.ToString(objSet.AssetSymbolSetReference.EntityKey.EntityKeyValues[0].Value);
        //                        if (!arrLst.Contains(strVal))
        //                        {
        //                            strOddAssetSymb = strVal;
        //                        }
        //                    }
        //                    if (!dictFinalData.ContainsKey(strOddAssetSymb + ":" + string.Empty))
        //                        dictFinalData.Add(strOddAssetSymb + ":" + string.Empty, new int[0]);
        //                }

        //                #endregion

        //                #region This region is used to generate and print the Rotation matrix.

        //                //Print the Table for Rank purpose...
        //                HtmlTable tblContainer = new HtmlTable();
        //                for (int i = 0; i <= lstPortfolioContentSet.Count; i++)
        //                {
        //                    HtmlTableRow row = new HtmlTableRow();
        //                    for (int j = 0; j <= lstPortfolioContentSet.Count; j++)
        //                    {
        //                        HtmlTableCell cell = new HtmlTableCell();
        //                        if (j == 0 && i != 0)
        //                        {
        //                            cell.InnerText = lstPortfolioContentSet.ElementAt(i - 1).AssetSymbolSet.Symbol;
        //                        }
        //                        else if (i == 0 && j > 0)
        //                        {
        //                            cell.InnerText = lstPortfolioContentSet.ElementAt(j - 1).AssetSymbolSet.Symbol;
        //                        }
        //                        else
        //                        {
        //                            cell.InnerText = string.Empty;
        //                        }
        //                        cell.Align = "Center";
        //                        row.Cells.Add(cell);
        //                    }
        //                    tblContainer.Rows.Add(row);

        //                }
        //                tblContainer.Border = 1;
        //                tblContainer.BorderColor = "Black";
        //                tblContainer.Width = "100%";
        //                tblContainer.Height = "70%";

        //                objHEEntities.SaveChanges();
        //                foreach (string strValPair in dictOriginalRotationImp.Keys)
        //                {
        //                    string[] strArr1 = strValPair.Split(":".ToCharArray());
        //                    if (!string.IsNullOrEmpty(strArr1[1]))
        //                    {
        //                        double dblRotation = 0;
        //                        int firstAssetID = Convert.ToInt32(strArr1[0]);
        //                        int secondAssetID = Convert.ToInt32(strArr1[1]);
        //                        int intFirstIndex = lstPortfolioContentSet.FindIndex(x => x.AssetSymbolSet.Id.Equals(firstAssetID));
        //                        int intSecondIndex = lstPortfolioContentSet.FindIndex(x => x.AssetSymbolSet.Id.Equals(secondAssetID));
        //                        ++intFirstIndex;
        //                        ++intSecondIndex;
        //                        double[] dblWinnAsset;
        //                        dictArr.TryGetValue(firstAssetID + ":" + secondAssetID, out dblWinnAsset);
        //                        dictOriginalRotationImp.TryGetValue(firstAssetID + ":" + secondAssetID, out dblRotation);
        //                        if (dblWinnAsset[2] == 1.0)
        //                        {
        //                            tblContainer.Rows[intSecondIndex].Cells[intFirstIndex].InnerText = lstPortfolioContentSet.First(x => x.AssetSymbolSet.Id.Equals(firstAssetID)).AssetSymbolSet.Symbol;
        //                            tblContainer.Rows[intFirstIndex].Cells[intSecondIndex].InnerText = lstPortfolioContentSet.First(x => x.AssetSymbolSet.Id.Equals(firstAssetID)).AssetSymbolSet.Symbol;
        //                        }
        //                        else if (dblWinnAsset[2] == 2.0)
        //                        {
        //                            tblContainer.Rows[intSecondIndex].Cells[intFirstIndex].InnerText = lstPortfolioContentSet.First(x => x.AssetSymbolSet.Id.Equals(secondAssetID)).AssetSymbolSet.Symbol;
        //                            tblContainer.Rows[intFirstIndex].Cells[intSecondIndex].InnerText = lstPortfolioContentSet.First(x => x.AssetSymbolSet.Id.Equals(secondAssetID)).AssetSymbolSet.Symbol;
        //                        }

        //                        if (dblRotation < 0)
        //                        {
        //                            tblContainer.Rows[intSecondIndex].Cells[intFirstIndex].BgColor = "FFFFFF";
        //                            tblContainer.Rows[intSecondIndex].Cells[intFirstIndex].Style.Add("Color", "Black");
        //                            tblContainer.Rows[intFirstIndex].Cells[intSecondIndex].BgColor = "FFFFFF";
        //                            tblContainer.Rows[intFirstIndex].Cells[intSecondIndex].Style.Add("Color", "Black");

        //                        }
        //                        else if (dblRotation >= 0 && dblRotation < 10)
        //                        {
        //                            tblContainer.Rows[intSecondIndex].Cells[intFirstIndex].BgColor = "ADDFFF";
        //                            tblContainer.Rows[intSecondIndex].Cells[intFirstIndex].Style.Add("Color", "White");
        //                            tblContainer.Rows[intFirstIndex].Cells[intSecondIndex].BgColor = "ADDFFF";
        //                            tblContainer.Rows[intFirstIndex].Cells[intSecondIndex].Style.Add("Color", "White");
        //                        }
        //                        else if (dblRotation >= 10 && dblRotation < 20)
        //                        {
        //                            tblContainer.Rows[intSecondIndex].Cells[intFirstIndex].BgColor = "6699CC";
        //                            tblContainer.Rows[intSecondIndex].Cells[intFirstIndex].Style.Add("Color", "White");
        //                            tblContainer.Rows[intFirstIndex].Cells[intSecondIndex].BgColor = "6699CC";
        //                            tblContainer.Rows[intFirstIndex].Cells[intSecondIndex].Style.Add("Color", "White");
        //                        }
        //                        else if (dblRotation >= 20 && dblRotation < 40)
        //                        {
        //                            tblContainer.Rows[intSecondIndex].Cells[intFirstIndex].BgColor = "336699";
        //                            tblContainer.Rows[intSecondIndex].Cells[intFirstIndex].Style.Add("Color", "White");
        //                            tblContainer.Rows[intFirstIndex].Cells[intSecondIndex].BgColor = "336699";
        //                            tblContainer.Rows[intFirstIndex].Cells[intSecondIndex].Style.Add("Color", "White");
        //                        }
        //                        else
        //                        {
        //                            tblContainer.Rows[intSecondIndex].Cells[intFirstIndex].BgColor = "003399";
        //                            tblContainer.Rows[intSecondIndex].Cells[intFirstIndex].Style.Add("Color", "White");
        //                            tblContainer.Rows[intFirstIndex].Cells[intSecondIndex].BgColor = "003399";
        //                            tblContainer.Rows[intFirstIndex].Cells[intSecondIndex].Style.Add("Color", "White");
        //                        }
        //                    }
        //                }

        //                if (divTable != null)
        //                    divTable.InnerHtml = GenerateHTML(tblContainer);

        //                HtmlTable tblColorCode = new HtmlTable();
        //                tblColorCode.Border = 1;
        //                tblColorCode.BorderColor = "Black";

        //                HtmlTableRow rowCode = new HtmlTableRow();

        //                HtmlTableCell cell00 = new HtmlTableCell();
        //                cell00.InnerText = "Color Code";
        //                rowCode.Cells.Add(cell00);

        //                HtmlTableCell cell10 = new HtmlTableCell();
        //                cell10.InnerText = "<10% Benefit";
        //                cell10.BgColor = "ADDFFF";
        //                cell10.Style.Add("Color", "White");
        //                rowCode.Cells.Add(cell10);

        //                HtmlTableCell cell20 = new HtmlTableCell();
        //                cell20.InnerText = "10 to 20% Benefit";
        //                cell20.BgColor = "6699CC";
        //                cell20.Style.Add("Color", "White");
        //                rowCode.Cells.Add(cell20);

        //                HtmlTableCell cell30 = new HtmlTableCell();
        //                cell30.InnerText = "20 to 40% Benefit";
        //                cell30.BgColor = "336699";
        //                cell30.Style.Add("Color", "White");
        //                rowCode.Cells.Add(cell30);

        //                HtmlTableCell cell40 = new HtmlTableCell();
        //                cell40.InnerText = ">40% Benefit";
        //                cell40.BgColor = "003399";
        //                cell40.Style.Add("Color", "White");
        //                rowCode.Cells.Add(cell40);

        //                tblColorCode.Rows.Add(rowCode);

        //                if (divColorCode != null)
        //                    divColorCode.InnerHtml = GenerateHTML(tblColorCode);


        //                #endregion

        //                #region This region is used to delete the existing entries from the RotationMatrixData table.

        //                if (objHEEntities.RotationMatrixData.Where(x => x.PortfolioSet.Id.Equals(intID)).Count() > 0)
        //                {
        //                    var extCollection = objHEEntities.RotationMatrixData.Where(x => x.PortfolioSet.Id.Equals(intID));
        //                    foreach (var set in extCollection)
        //                    {
        //                        objHEEntities.DeleteObject(set);
        //                    }

        //                    objHEEntities.SaveChanges();
        //                }

        //                #endregion

        //                #region This region is used to fill the RotationMatrixData table with the fresh entries of the Rotation Matrix.

        //                for (int i = 0; i < tblContainer.Rows.Count; i++)
        //                {
        //                    RotationMatrixData objRMat;
        //                    string[] strSymbol = new string[tblContainer.Rows[i].Cells.Count];
        //                    string[] strColor = new string[tblContainer.Rows[i].Cells.Count];
        //                    for (int j = 0; j < tblContainer.Rows[i].Cells.Count; j++)
        //                    {
        //                        strSymbol[j] = tblContainer.Rows[i].Cells[j].InnerText;
        //                        strColor[j] = tblContainer.Rows[i].Cells[j].BgColor;
        //                        string bgcolor = tblContainer.Rows[i].Cells[j].BgColor;
        //                    }
        //                    objRMat = FillData(lstPortfolioContentSet.Count, strSymbol, strColor);
        //                    objRMat.rowNo = i;
        //                    objRMat.PortfolioSet = objHEEntities.PortfolioSet.FirstOrDefault(x => x.Id.Equals(intID));
        //                    objHEEntities.AddToRotationMatrixData(objRMat);
        //                }
        //                objHEEntities.SaveChanges();

        //                #endregion

        //                //Perform Buy and Hold Trend for Rotation Engine...
        //                PerformBuyHoldTrendRotation(intSelectedIndex, strSelectedValue, dictFinalData, dictArr, isNegativePairtoZero);

        //                //Perform Rebalance Trend for Rotation Engine...
        //                PerformRebalanceTrendRotation(intSelectedIndex, strSelectedValue, dictFinalData, dictArr, isNegativePairtoZero);

                       
        //            }
        //        }
        //    }
        //    catch (Exception)
        //    {

        //    }
        //}

        public void PerformRotationEngineRanking(int intSelectedIndex, string strSelectedValue, bool isNegativePairtoZero, bool isChecked, HtmlGenericControl divTable, HtmlGenericControl divColorCode, string[] arrExcludeSymSet)
        {
            //System.Diagnostics.Debugger.Break();
            try
            {
                if (intSelectedIndex != -1)
                {
                    int intID = Convert.ToInt32(strSelectedValue);
                    List<PortfolioContentSet> lstPortfolioContentSet = objHEEntities.PortfolioContentSet.Where(x => x.PortfolioSet.Id.Equals(intID)).OrderBy(x=>x.AssetSymbolSet.Id).ToList();

                    isRotationHistorical = CommonUtility.IsHistoricalProcessing();
                    isDividendOrSplit = CommonUtility.IsDividendOrSplitForPortfolio(lstPortfolioContentSet);

                    if (lstPortfolioContentSet != null)
                    {
                        #region Variable Initializations

                        DateTime dtFinalStartDate = new DateTime(1, 1, 1);
                        List<Pairs> combos = new List<Pairs>();
                        List<Pairs> lstPerms = new List<Pairs>();
                        List<double> lstRotatImpArr = new List<double>();
                        Dictionary<string, double> dictRotationImp = new Dictionary<string, double>();
                        Dictionary<string, double> dictFinalRotationImp = new Dictionary<string, double>();
                        Dictionary<string, double> dictOriginalRotationImp = new Dictionary<string, double>();
                        Dictionary<string, double[]> dictArr = new Dictionary<string, double[]>();
                        Dictionary<string, int[]> dictLstData = new Dictionary<string, int[]>();
                        Dictionary<double, string[]> dictPairComb = new Dictionary<double, string[]>();
                        Dictionary<string, double> dictRotationAvg = new Dictionary<string, double>();
                        double[] dblRotationImp = null;
                        int intCount = 0;
                        int intAssetSymbolID = 0;
                        int intArraySize = 0;

                        #endregion

                        #region Get the earliest trading date for Portfolio

                        //Get the start date of the least asset from the portfolio. 
                        foreach (var objSet in lstPortfolioContentSet)
                        {
                            //Get the start date for this asset.
                            DateTime dtStartDate = CommonUtility.GetStartDate(Convert.ToInt32(((EntityReference)(objSet.AssetSymbolSetReference)).EntityKey.EntityKeyValues[0].Value));
                            if (intCount == 0)
                            {
                                dtFinalStartDate = dtStartDate;

                                //Get the asset symbol id.
                                intAssetSymbolID = Convert.ToInt32(((EntityReference)(objSet.AssetSymbolSetReference)).EntityKey.EntityKeyValues[0].Value);
                                intCount++;
                            }
                            else
                            {
                                if (dtStartDate >= dtFinalStartDate)
                                {
                                    dtFinalStartDate = dtStartDate;
                                    intAssetSymbolID = Convert.ToInt32(((EntityReference)(objSet.AssetSymbolSetReference)).EntityKey.EntityKeyValues[0].Value);
                                }
                            }
                        }

                        #endregion

                        DateTime dtFinalLastDate = new DateTime(1, 1, 1);
                        int intLastDateCount = 0;
                        #region Get the earliest Last Trading Date from Assets
                        foreach (var objSet in lstPortfolioContentSet)
                        {
                            DateTime dtLastDate = CommonUtility.GetLastTradingDate(Convert.ToInt32(((EntityReference)(objSet.AssetSymbolSetReference)).EntityKey.EntityKeyValues[0].Value));
                            if (intLastDateCount == 0)
                            {
                                dtFinalLastDate = dtLastDate;
                                //intAssetSymbolID = Convert.ToInt32(((EntityReference)(objSet.AssetSymbolSetReference)).EntityKey.EntityKeyValues[0].Value);
                                intLastDateCount++;
                            }
                            else
                            {
                                if (dtLastDate < dtFinalLastDate)
                                {
                                    dtFinalLastDate = dtLastDate;
                                    // intAssetSymbolID = Convert.ToInt32(((EntityReference)(objSet.AssetSymbolSetReference)).EntityKey.EntityKeyValues[0].Value);
                                }
                            }



                        }
                        #endregion

                        #region Get the array size for this selected Portfolio from the AssetPriceSet table.

                        //Get the actual size of array to be initialized.
                        intArraySize = CommonUtility.GetArraySizeForPortfolioMethods(intAssetSymbolID, dtFinalLastDate);

                        #endregion

                        //HttpContext.Current.Response.Write("Paired Combos. &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; CAGR Ave Trend. &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; CAGR Rotation.&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;Winning Asset.&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;Rotation Improvement over average.<br>");
                        //HttpContext.Current.Response.Write("-------------------------------------------------------------------------------------------------------------------------------------------");

                        if (lstPortfolioContentSet.Count % 2 == 0)
                            dblRotationImp = new double[lstPortfolioContentSet.Count - 1];
                        else
                            dblRotationImp = new double[lstPortfolioContentSet.Count];

                        //** code added for rank rotation

                        RankRotationProperties objRankRotation;
                        lstRankRotation = new List<RankRotationProperties>();

                        AssetProperties objAssetProperties;
                        lstAssetProperties = new List<AssetProperties>();

                        double[] dblWeightSum = new double[intArraySize];
                        double[] dblRotationEventMetric = new double[intArraySize];
                        int[] intRankChangeEventValue = new int[intArraySize];
                        dtRankDates = new DateTime[intArraySize];
                        double[] dblRankRotationPortfolioValue = new double[intArraySize];
                        double[] dblPcntChangePortVal = new double[intArraySize];
                        

                        DateTime[] dtAlertsRankRebalanceDate = new DateTime[intArraySize];

                            //calculate aggresiveness and portfolio begin value

                        PortfolioSet objPortfolioSet = new PortfolioSet();
                        objPortfolioSet = objHEEntities.PortfolioSet.FirstOrDefault(x => x.Id.Equals(intID));
                        double dblRotAgg = objPortfolioSet.Aggressiveness;
                        double dblPortfolioVal = (double)objHEEntities.PortfolioContentSet.Where(x => x.PortfolioSet.Id.Equals(intID)).Sum(x => x.Value).Value;

                        List<PortfolioContentSet> lstPortfolioContSet = objPortfolioSet.PortfolioContentSet.Where(x => x.PortfolioSet.Id.Equals(intID)).ToList();

                        //** end RR code

                        #region  This section is used for Comparing all Assets with each other.

                        for (int i = 0; i < lstPortfolioContentSet.Count; i++)
                        {
                            int iID = Convert.ToInt32(((System.Data.Objects.DataClasses.EntityReference)(lstPortfolioContentSet[i].AssetSymbolSetReference)).EntityKey.EntityKeyValues[0].Value);
                            for (int k = i + 1; k < lstPortfolioContentSet.Count; k++)
                            {
                              
                                //** code added for RR
                                dblDailyChange1 = new double[intArraySize];
                                dblDailyChange2 = new double[intArraySize];
                                dblPriceTrend1 = new double[intArraySize];
                                dblPriceTrend2 = new double[intArraySize];
                                dblPrice1 = new double[intArraySize];
                                dblPrice2 = new double[intArraySize];

                                //** end RR code

                                string strPair;
                                int[] intAWins;
                                Pairs objPairs = new Pairs(Convert.ToInt32(((System.Data.Objects.DataClasses.EntityReference)(lstPortfolioContentSet.ElementAt(i).AssetSymbolSetReference)).EntityKey.EntityKeyValues[0].Value), Convert.ToInt32(((System.Data.Objects.DataClasses.EntityReference)(lstPortfolioContentSet.ElementAt(k).AssetSymbolSetReference)).EntityKey.EntityKeyValues[0].Value));
                                lstPerms.Add(objPairs);
                                int kID = Convert.ToInt32(((System.Data.Objects.DataClasses.EntityReference)(lstPortfolioContentSet[k].AssetSymbolSetReference)).EntityKey.EntityKeyValues[0].Value);

                                double[] arrValues = CompareAssetsRanking(intID, dtFinalStartDate,dtFinalLastDate, intArraySize, iID, kID, isChecked, out strPair, out intAWins);
                                if (!dictArr.ContainsKey(iID + ":" + kID))
                                    dictArr.Add(iID + ":" + kID, arrValues);
                                if (!dictLstData.ContainsKey(strPair))
                                    dictLstData.Add(strPair, intAWins);

                                //code added for RR

                                objRankRotation = new RankRotationProperties();
                                objRankRotation.StrPair = strPair;
                                objRankRotation.WhoWins = intAWins;
                                objRankRotation.DailyChangeFirst = dblDailyChange1;
                                objRankRotation.DailyChangeSecond = dblDailyChange2;
                                objRankRotation.PriceTrendFirst = dblPriceTrend1;
                                objRankRotation.PriceTrendSecond = dblPriceTrend2;
                                objRankRotation.PriceFirst = dblPrice1;
                                objRankRotation.PriceSecond = dblPrice2;


                                if (arrValues[1] > arrValues[0])
                                    objRankRotation.IsRotation = 1;
                                else
                                    objRankRotation.IsRotation = 0;

                                lstRankRotation.Add(objRankRotation);

                                // end RR code
                            }
                        }



                        //** code added for RR

                        //** end RR code

                        #endregion

                        #region This region iterates through the dictinonary object and calculates the Rotation improvement.

                        foreach (string strPair in dictLstData.Keys)
                        {
                            double avgCAGRTrend = 0;
                            double avgCAGRRotation = 0;
                            string[] strPairColl = strPair.Split(":".ToCharArray());
                            int intFirstID = Convert.ToInt32(strPairColl[0]);
                            int intSecondID = Convert.ToInt32(strPairColl[1]);
                            string strFirstSymbol = objHEEntities.AssetSymbolSet.FirstOrDefault(x => x.Id.Equals(intFirstID)).Symbol;
                            string strSecondSymbol = objHEEntities.AssetSymbolSet.FirstOrDefault(x => x.Id.Equals(intSecondID)).Symbol;
                            double[] arrValues;
                            dictArr.TryGetValue(intFirstID + ":" + intSecondID, out arrValues);
                            avgCAGRTrend = avgCAGRTrend + arrValues[0];
                            avgCAGRRotation = avgCAGRRotation + arrValues[1];
                            //HttpContext.Current.Response.Write("%<br>" + strFirstSymbol + " : " + strSecondSymbol + "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;" + String.Format("{0:0.000}", arrValues[0] * 100) + "%&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;" + String.Format("{0:0.000}", arrValues[1] * 100) + "%&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;" + (arrValues[2] == 1 ? strFirstSymbol : strSecondSymbol) + "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;" + String.Format("{0:0.000}", ((arrValues[1] - arrValues[0]) / arrValues[0]) * 100));
                            dictRotationImp.Add(intFirstID + ":" + intSecondID, ((arrValues[1] - arrValues[0]) / arrValues[0]) * 100);
                            dictOriginalRotationImp.Add(intFirstID + ":" + intSecondID, ((arrValues[1] - arrValues[0]) / arrValues[0]) * 100);
                            lstRotatImpArr.Add(((arrValues[1] - arrValues[0]) / arrValues[0]) * 100);
                        }

                        #endregion

                        #region This region iterates through the array list and finds the highest yeilding Rotation improvement pairs and stores the result in the dictionary.

                        while (lstRotatImpArr.Count != 0)
                        {
                            double dblMaxRotatValue = lstRotatImpArr.Max();
                            foreach (string strKey in dictRotationImp.Keys)
                            {
                                double dblRotVal = 0;
                                dictRotationImp.TryGetValue(strKey, out dblRotVal);
                                if (dblMaxRotatValue == dblRotVal)
                                {
                                    dictFinalRotationImp.Add(strKey, dblRotVal);
                                    dictRotationImp = ValidateDictionaryObject(dictRotationImp, strKey, lstRotatImpArr);
                                    break;
                                }
                            }
                            if (lstRotatImpArr.Contains(dblMaxRotatValue))
                                lstRotatImpArr.Remove(dblMaxRotatValue);
                        }

                        #endregion

                        #region This region is used to Print the Best Rotation Improvement pairs along with their Rotation Improvement values.

                        //HttpContext.Current.Response.Write("<br><br>Best Pair Unique Combinations:<br>");
                        //HttpContext.Current.Response.Write("Pair&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;Rotation Value<br>");
                        foreach (string strBestPair in dictFinalRotationImp.Keys)
                        {
                            string[] strPairCollection = strBestPair.Split(":".ToCharArray());
                            int intFirstSymbol = Convert.ToInt32(strPairCollection[0]);
                            int intSecondSymbol = Convert.ToInt32(strPairCollection[1]);
                            string strFirstSymbol = objHEEntities.AssetSymbolSet.FirstOrDefault(x => x.Id.Equals(intFirstSymbol)).Symbol;
                            string strSecondSymbol = objHEEntities.AssetSymbolSet.FirstOrDefault(x => x.Id.Equals(intSecondSymbol)).Symbol;
                            double dblPairValue = 0;
                            dictFinalRotationImp.TryGetValue(strBestPair, out dblPairValue);
                            //HttpContext.Current.Response.Write(strFirstSymbol + ":" + strSecondSymbol + "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;" + dblPairValue + "<br>");
                        }

                        #endregion

                        #region This region is used to fill the dictionary object with the best improvement pairs.

                        //dictPairComb.TryGetValue(dblRotationImp.Max(), out strWinningPairs);
                        Dictionary<string, int[]> dictFinalData = new Dictionary<string, int[]>();
                        if (dictFinalRotationImp.Count() > 0)
                        {
                            foreach (string strPair in dictFinalRotationImp.Keys)
                            {
                                if (dictLstData.ContainsKey(strPair))
                                {
                                    int[] intWinAssetArr;
                                    dictLstData.TryGetValue(strPair, out intWinAssetArr);
                                    if (!dictFinalData.ContainsKey(strPair))
                                        dictFinalData.Add(strPair, intWinAssetArr);
                                }
                            }
                        }

                        #endregion

                        #region This region is used to add the Odd Asset entry into the existing Dictionary object.

                        //If asset count is odd.
                        ArrayList arrLst = new ArrayList();
                        string strOddAssetSymb = string.Empty;
                        if (lstPortfolioContentSet.Count % 2 != 0)
                        {
                            foreach (string strPair in dictFinalRotationImp.Keys)
                            {
                                foreach (string strSymbol in strPair.Split(":".ToCharArray()))
                                {
                                    arrLst.Add(strSymbol);
                                }
                            }
                            foreach (var objSet in lstPortfolioContentSet)
                            {
                                string strVal = Convert.ToString(objSet.AssetSymbolSetReference.EntityKey.EntityKeyValues[0].Value);
                                if (!arrLst.Contains(strVal))
                                {
                                    strOddAssetSymb = strVal;
                                }
                            }
                            if (!dictFinalData.ContainsKey(strOddAssetSymb + ":" + string.Empty))
                                dictFinalData.Add(strOddAssetSymb + ":" + string.Empty, new int[0]);
                        }

                        #endregion

                        #region This region is used to generate and print the Rotation matrix.

                        //Print the Table for Rank purpose...
                        HtmlTable tblContainer = new HtmlTable();
                        for (int i = 0; i <= lstPortfolioContentSet.Count; i++)
                        {
                            HtmlTableRow row = new HtmlTableRow();
                            for (int j = 0; j <= lstPortfolioContentSet.Count; j++)
                            {
                                HtmlTableCell cell = new HtmlTableCell();
                                if (j == 0 && i != 0)
                                {
                                    cell.InnerText = lstPortfolioContentSet.ElementAt(i - 1).AssetSymbolSet.Symbol;
                                }
                                else if (i == 0 && j > 0)
                                {
                                    cell.InnerText = lstPortfolioContentSet.ElementAt(j - 1).AssetSymbolSet.Symbol;
                                }
                                else
                                {
                                    cell.InnerText = string.Empty;
                                }
                                cell.Align = "Center";
                                row.Cells.Add(cell);
                            }
                            tblContainer.Rows.Add(row);

                        }
                        tblContainer.Border = 1;
                        tblContainer.BorderColor = "Black";
                        tblContainer.Width = "100%";
                        tblContainer.Height = "70%";

                        objHEEntities.SaveChanges();
                        foreach (string strValPair in dictOriginalRotationImp.Keys)
                        {
                            string[] strArr1 = strValPair.Split(":".ToCharArray());
                            if (!string.IsNullOrEmpty(strArr1[1]))
                            {
                                double dblRotation = 0;
                                int firstAssetID = Convert.ToInt32(strArr1[0]);
                                int secondAssetID = Convert.ToInt32(strArr1[1]);
                                int intFirstIndex = lstPortfolioContentSet.FindIndex(x => x.AssetSymbolSet.Id.Equals(firstAssetID));
                                int intSecondIndex = lstPortfolioContentSet.FindIndex(x => x.AssetSymbolSet.Id.Equals(secondAssetID));
                                ++intFirstIndex;
                                ++intSecondIndex;
                                double[] dblWinnAsset;
                                dictArr.TryGetValue(firstAssetID + ":" + secondAssetID, out dblWinnAsset);
                                dictOriginalRotationImp.TryGetValue(firstAssetID + ":" + secondAssetID, out dblRotation);
                                if (dblWinnAsset[2] == 1.0)
                                {
                                    tblContainer.Rows[intSecondIndex].Cells[intFirstIndex].InnerText = lstPortfolioContentSet.First(x => x.AssetSymbolSet.Id.Equals(firstAssetID)).AssetSymbolSet.Symbol;
                                    tblContainer.Rows[intFirstIndex].Cells[intSecondIndex].InnerText = lstPortfolioContentSet.First(x => x.AssetSymbolSet.Id.Equals(firstAssetID)).AssetSymbolSet.Symbol;
                                }
                                else if (dblWinnAsset[2] == 2.0)
                                {
                                    tblContainer.Rows[intSecondIndex].Cells[intFirstIndex].InnerText = lstPortfolioContentSet.First(x => x.AssetSymbolSet.Id.Equals(secondAssetID)).AssetSymbolSet.Symbol;
                                    tblContainer.Rows[intFirstIndex].Cells[intSecondIndex].InnerText = lstPortfolioContentSet.First(x => x.AssetSymbolSet.Id.Equals(secondAssetID)).AssetSymbolSet.Symbol;
                                }

                                if (dblRotation < 0)
                                {
                                    tblContainer.Rows[intSecondIndex].Cells[intFirstIndex].BgColor = "FFFFFF";
                                    tblContainer.Rows[intSecondIndex].Cells[intFirstIndex].Style.Add("Color", "Black");
                                    tblContainer.Rows[intFirstIndex].Cells[intSecondIndex].BgColor = "FFFFFF";
                                    tblContainer.Rows[intFirstIndex].Cells[intSecondIndex].Style.Add("Color", "Black");

                                }
                                else if (dblRotation >= 0 && dblRotation < 10)
                                {
                                    tblContainer.Rows[intSecondIndex].Cells[intFirstIndex].BgColor = "ADDFFF";
                                    tblContainer.Rows[intSecondIndex].Cells[intFirstIndex].Style.Add("Color", "White");
                                    tblContainer.Rows[intFirstIndex].Cells[intSecondIndex].BgColor = "ADDFFF";
                                    tblContainer.Rows[intFirstIndex].Cells[intSecondIndex].Style.Add("Color", "White");
                                }
                                else if (dblRotation >= 10 && dblRotation < 20)
                                {
                                    tblContainer.Rows[intSecondIndex].Cells[intFirstIndex].BgColor = "6699CC";
                                    tblContainer.Rows[intSecondIndex].Cells[intFirstIndex].Style.Add("Color", "White");
                                    tblContainer.Rows[intFirstIndex].Cells[intSecondIndex].BgColor = "6699CC";
                                    tblContainer.Rows[intFirstIndex].Cells[intSecondIndex].Style.Add("Color", "White");
                                }
                                else if (dblRotation >= 20 && dblRotation < 40)
                                {
                                    tblContainer.Rows[intSecondIndex].Cells[intFirstIndex].BgColor = "336699";
                                    tblContainer.Rows[intSecondIndex].Cells[intFirstIndex].Style.Add("Color", "White");
                                    tblContainer.Rows[intFirstIndex].Cells[intSecondIndex].BgColor = "336699";
                                    tblContainer.Rows[intFirstIndex].Cells[intSecondIndex].Style.Add("Color", "White");
                                }
                                else
                                {
                                    tblContainer.Rows[intSecondIndex].Cells[intFirstIndex].BgColor = "003399";
                                    tblContainer.Rows[intSecondIndex].Cells[intFirstIndex].Style.Add("Color", "White");
                                    tblContainer.Rows[intFirstIndex].Cells[intSecondIndex].BgColor = "003399";
                                    tblContainer.Rows[intFirstIndex].Cells[intSecondIndex].Style.Add("Color", "White");
                                }
                            }
                        }

                        if (divTable != null)
                            divTable.InnerHtml = GenerateHTML(tblContainer);

                        HtmlTable tblColorCode = new HtmlTable();
                        tblColorCode.Border = 1;
                        tblColorCode.BorderColor = "Black";

                        HtmlTableRow rowCode = new HtmlTableRow();

                        HtmlTableCell cell00 = new HtmlTableCell();
                        cell00.InnerText = "Color Code";
                        rowCode.Cells.Add(cell00);

                        HtmlTableCell cell10 = new HtmlTableCell();
                        cell10.InnerText = "<10% Benefit";
                        cell10.BgColor = "ADDFFF";
                        cell10.Style.Add("Color", "White");
                        rowCode.Cells.Add(cell10);

                        HtmlTableCell cell20 = new HtmlTableCell();
                        cell20.InnerText = "10 to 20% Benefit";
                        cell20.BgColor = "6699CC";
                        cell20.Style.Add("Color", "White");
                        rowCode.Cells.Add(cell20);

                        HtmlTableCell cell30 = new HtmlTableCell();
                        cell30.InnerText = "20 to 40% Benefit";
                        cell30.BgColor = "336699";
                        cell30.Style.Add("Color", "White");
                        rowCode.Cells.Add(cell30);

                        HtmlTableCell cell40 = new HtmlTableCell();
                        cell40.InnerText = ">40% Benefit";
                        cell40.BgColor = "003399";
                        cell40.Style.Add("Color", "White");
                        rowCode.Cells.Add(cell40);

                        tblColorCode.Rows.Add(rowCode);

                        if (divColorCode != null)
                            divColorCode.InnerHtml = GenerateHTML(tblColorCode);


                        #endregion

                        #region This region is used to delete the existing entries from the RotationMatrixData table.

                        if (objHEEntities.RotationMatrixData.Where(x => x.PortfolioSet.Id.Equals(intID)).Count() > 0)
                        {
                            var extCollection = objHEEntities.RotationMatrixData.Where(x => x.PortfolioSet.Id.Equals(intID));
                            foreach (var set in extCollection)
                            {
                                objHEEntities.DeleteObject(set);
                            }

                            objHEEntities.SaveChanges();
                        }

                        #endregion

                        #region This region is used to fill the RotationMatrixData table with the fresh entries of the Rotation Matrix.

                        for (int i = 0; i < tblContainer.Rows.Count; i++)
                        {
                            RotationMatrixData objRMat;
                            string[] strSymbol = new string[tblContainer.Rows[i].Cells.Count];
                            string[] strColor = new string[tblContainer.Rows[i].Cells.Count];
                            for (int j = 0; j < tblContainer.Rows[i].Cells.Count; j++)
                            {
                                strSymbol[j] = tblContainer.Rows[i].Cells[j].InnerText;
                                strColor[j] = tblContainer.Rows[i].Cells[j].BgColor;
                                string bgcolor = tblContainer.Rows[i].Cells[j].BgColor;
                            }
                            objRMat = FillData(lstPortfolioContentSet.Count, strSymbol, strColor);
                            objRMat.rowNo = i;
                            objRMat.PortfolioSet = objHEEntities.PortfolioSet.FirstOrDefault(x => x.Id.Equals(intID));
                            objHEEntities.AddToRotationMatrixData(objRMat);
                        }
                        objHEEntities.SaveChanges();

                        #endregion

                        //Perform Buy and Hold Trend for Rotation Engine...
                        PerformBuyHoldTrendRotation(intSelectedIndex, strSelectedValue, dictFinalData, dictArr, isNegativePairtoZero);

                        //Perform Rebalance Trend for Rotation Engine...
                        PerformRebalanceTrendRotation(intSelectedIndex, strSelectedValue, dictFinalData, dictArr, isNegativePairtoZero);

                       
                        ////////////////////////////////////////////
                        ///Perform Rank Rotation Rebalance Engine///
                        ////////////////////////////////////////////
                        #region RRR calculations
                        //** code for RRR

                        Hashtable objCalcWt = new Hashtable();



                        //separating asset symbol ids from pairs
                        for (int i = 0; i < lstRankRotation.Count; i++)
                        {
                            string[] arrSymSet = lstRankRotation[i].StrPair.Split(":".ToCharArray());
                            if (!objCalcWt.ContainsKey(arrSymSet[0]))
                                objCalcWt.Add(arrSymSet[0], 0);

                            if (!objCalcWt.ContainsKey(arrSymSet[1]))
                                objCalcWt.Add(arrSymSet[1], 0);
                        }
                        //adding asset symbol ids to Asset properties list
                        foreach (string strKey in objCalcWt.Keys)
                        {
                            objAssetProperties = new AssetProperties();
                            objAssetProperties.AssetSymbolID = strKey;
                            lstAssetProperties.Add(objAssetProperties);
                        }

                        //initialization
                        for (int i = 0; i < lstAssetProperties.Count; i++)
                        {
                            lstAssetProperties[i].AssetWtCoefficient = new double[intArraySize];
                            lstAssetProperties[i].Shares = new double[intArraySize];
                            lstAssetProperties[i].CurrentWeight = new double[intArraySize];
                            lstAssetProperties[i].AssetDailyReturn = new double[intArraySize];
                        }

                        for (int intK = 0; intK < lstAssetProperties.Count; intK++)
                        {

                            int intFirstSymID = Convert.ToInt32(lstAssetProperties[intK].AssetSymbolID); //objHEEntities.AssetSymbolSet.FirstOrDefault(x => x.Symbol.Equals(lstAssetWtCoeff[intK].AssetSymbol)).Id;

                            for (int intI = 217; intI < intArraySize; intI++)
                            {
                                //foreach (string strKey in objCalcWt.Keys)
                                //{
                                //    objCalcWt[strKey] = 0;
                                //}
                                double intWinner = 0;

                                if (intI == 217)
                                {
                                    for (int intL = 0; intL < lstRankRotation.Count; intL++)
                                    {
                                        //lstRankRotation[intL].WhoWins[intI] = 0;
                                     
                                        // Code change for handling negative pair
                                        bool isNegative = false;
                                        string strPairKey = lstRankRotation[intL].StrPair;

                                        isNegative = false;
                                        string[] strPairColl = strPairKey.Split(":".ToCharArray());

                                        double[] dblArr = null;

                                        dictArr.TryGetValue(strPairKey, out dblArr);

                                        if (dblArr != null)
                                        {
                                            double dblVal = (((dblArr[1] - dblArr[0]) / dblArr[0]) * 100);
                                            if (dblVal < 0)
                                                isNegative = true;
                                        }
                                        if (isNegative)
                                        {
                                            intWinner += 0.5;
                                        }
                                        else if (lstRankRotation[intL].StrPair.StartsWith(intFirstSymID.ToString() + ":"))
                                        {
                                            if (lstRankRotation[intL].WhoWins[intI] == 1)
                                            {
                                                if (lstRankRotation[intL].IsRotation == 1)
                                                {
                                                    //objCalcWt[intFirstSymID.ToString()] = ((double)objCalcWt[intFirstSymID.ToString()]) + 1;
                                                    intWinner += 1;
                                                }
                                            }
                                        }
                                        else if (lstRankRotation[intL].StrPair.EndsWith(":" + intFirstSymID))
                                        {
                                            if (lstRankRotation[intL].WhoWins[intI] == 0)
                                            {
                                                if (lstRankRotation[intL].IsRotation == 1)
                                                {
                                                    //objCalcWt[intFirstSymID.ToString()] = ((double)objCalcWt[intFirstSymID.ToString()]) + 1;
                                                    intWinner += 1;
                                                }
                                            }
                                        }
                                    }
                                }
                                if (intI >= 218)
                                {
                                    for (int intL = 0; intL < lstRankRotation.Count; intL++)
                                    {
                                        //Code change for handling negative pair
                                        bool isNegative = false;
                                        string strPairKey = lstRankRotation[intL].StrPair;

                                        isNegative = false;
                                        string[] strPairColl = strPairKey.Split(":".ToCharArray());

                                        double[] dblArr = null;

                                        dictArr.TryGetValue(strPairKey, out dblArr);

                                        if (dblArr != null)
                                        {
                                            double dblVal = (((dblArr[1] - dblArr[0]) / dblArr[0]) * 100);
                                            if (dblVal < 0)
                                                isNegative = true;
                                        }
                                        if (isNegative)
                                        {
                                            intWinner += 0.5;
                                        }
                                        else if (lstRankRotation[intL].StrPair.StartsWith(intFirstSymID.ToString() + ":"))
                                        {
                                            if (lstRankRotation[intL].WhoWins[intI] == 1)
                                            {
                                                if (lstRankRotation[intL].IsRotation == 1)
                                                {
                                                    // objCalcWt[intFirstSymID.ToString()] = ((double)objCalcWt[intFirstSymID.ToString()]) + 1;
                                                    intWinner += 1;
                                                }
                                            }
                                        }
                                        else if (lstRankRotation[intL].StrPair.EndsWith(":" + intFirstSymID.ToString()))
                                        {
                                            if (lstRankRotation[intL].WhoWins[intI] == 0)
                                            {
                                                if (lstRankRotation[intL].IsRotation == 1)
                                                {
                                                    //objCalcWt[intFirstSymID.ToString()] = ((double)objCalcWt[intFirstSymID.ToString()]) + 1;
                                                    intWinner += 1;
                                                }
                                            }
                                        }
                                    }
                                }
                                //lstAssetProperties[intK].AssetWtCoefficient[intI] = (double)objCalcWt[intFirstSymID.ToString()];
                                double weightingFactorOffset = (((1 - dblRotAgg) / (dblRotAgg - 0.4999)) * 0.5);
                                lstAssetProperties[intK].AssetWtCoefficient[intI] = (double)intWinner + weightingFactorOffset;
                            }
                        }

                        for (int intI = 217; intI < intArraySize; intI++)
                        {
                            dblWeightSum[intI] = 0;
                            dblRotationEventMetric[intI] = 0;
                            double intWt = 1;
                            for (int intK = 0; intK < lstAssetProperties.Count; intK++)
                            {
                                dblWeightSum[intI] += lstAssetProperties[intK].AssetWtCoefficient[intI];
                                dblRotationEventMetric[intI] += lstAssetProperties[intK].AssetWtCoefficient[intI] * intWt;
                                intWt = intWt * 10;
                            }

                            // calculation of asset daily returns
                            for (int intA = 0; intA < lstAssetProperties.Count; intA++)
                            {
                                for (int intB = 0; intB < lstRankRotation.Count; intB++)
                                {
                                    if (lstRankRotation[intB].StrPair.StartsWith(lstAssetProperties[intA].AssetSymbolID + ":"))
                                    {
                                        lstAssetProperties[intA].AssetDailyReturn[intI] = lstRankRotation[intB].DailyChangeFirst[intI - 1];
                                        break;
                                        //lstAssetWtCoeff[intA].Shares[intI] = (lstAssetWtCoeff[intA].AssetWtCoefficient[intI] / dblWtSum[intI]) * ((dblRankRotPortVal[intI]) / objWinAst.PriceTrend[intI]);
                                    }
                                    else if (lstRankRotation[intB].StrPair.EndsWith(":" + lstAssetProperties[intA].AssetSymbolID))
                                    {
                                        lstAssetProperties[intA].AssetDailyReturn[intI] = lstRankRotation[intB].DailyChangeSecond[intI - 1];
                                        break;
                                        // lstAssetWtCoeff[intA].Shares[intI] = (lstAssetWtCoeff[intA].AssetWtCoefficient[intI] / dblWtSum[intI]) * ((dblRankRotPortVal[intI]) / objWinAst.PriceTrendSecond[intI]);
                                    }
                                }
                            }

                            if (intI == 217)
                            {
                                dtAlertsRankRebalanceDate[intI] = dtRankDates[intI];
                                dblRankRotationPortfolioValue[intI] = dblPortfolioVal;

                                //calculation of asset shares
                                for (int intA = 0; intA < lstAssetProperties.Count; intA++)
                                {
                                    for (int intB = 0; intB < lstRankRotation.Count; intB++)
                                    {
                                        if (lstRankRotation[intB].StrPair.StartsWith(lstAssetProperties[intA].AssetSymbolID + ":"))
                                        {
                                            //lstAssetProperties[intA].AssetDailyReturn[intI] = lstRankRotation[intB].DailyChangeFirst[intI - 1];
                                            lstAssetProperties[intA].Shares[intI] = ((lstAssetProperties[intA].AssetWtCoefficient[intI] / dblWeightSum[intI]) * dblRankRotationPortfolioValue[intI]) / lstRankRotation[intB].PriceTrendFirst[intI];
                                            break;
                                        }
                                        else if (lstRankRotation[intB].StrPair.EndsWith(":" + lstAssetProperties[intA].AssetSymbolID))
                                        {
                                            //lstAssetProperties[intA].AssetDailyReturn[intI] = lstRankRotation[intB].DailyChangeSecond[intI - 1];
                                            lstAssetProperties[intA].Shares[intI] = ((lstAssetProperties[intA].AssetWtCoefficient[intI] / dblWeightSum[intI]) * dblRankRotationPortfolioValue[intI]) / lstRankRotation[intB].PriceTrendSecond[intI];
                                            break;
                                        }
                                    }
                                }
                            }
                            if (intI > 217)
                            {
                                intRankChangeEventValue[intI] = (dblRotationEventMetric[intI] == dblRotationEventMetric[intI - 1] ? 0 : 1);
                                dtAlertsRankRebalanceDate[intI] = (intRankChangeEventValue[intI] == 1 ? dtRankDates[intI] : dtAlertsRankRebalanceDate[intI - 1]);

                                //calculation of asset shares
                                for (int intA = 0; intA < lstAssetProperties.Count; intA++)
                                {
                                    for (int intB = 0; intB < lstRankRotation.Count; intB++)
                                    {
                                        if (lstRankRotation[intB].StrPair.StartsWith(lstAssetProperties[intA].AssetSymbolID + ":"))
                                        {
                                            //if (lstAssetProperties[intA].AssetWtCoefficient[intI] == lstAssetProperties[intA].AssetWtCoefficient[intI - 1])
                                            if(intRankChangeEventValue[intI] == 0)
                                            {
                                                lstAssetProperties[intA].Shares[intI] = lstAssetProperties[intA].Shares[intI - 1];
                                                break;
                                            }
                                            else
                                            {
                                                lstAssetProperties[intA].Shares[intI] = ((dblRankRotationPortfolioValue[intI - 1] * lstAssetProperties[intA].AssetWtCoefficient[intI]) / dblWeightSum[intI - 1]) / lstRankRotation[intB].PriceTrendFirst[intI - 1];
                                                break;
                                            }
                                        }
                                        else if (lstRankRotation[intB].StrPair.EndsWith(":" + lstAssetProperties[intA].AssetSymbolID))
                                        {
                                            //if (lstAssetProperties[intA].AssetWtCoefficient[intI] == lstAssetProperties[intA].AssetWtCoefficient[intI - 1])
                                            if (intRankChangeEventValue[intI] == 0)
                                            {
                                                lstAssetProperties[intA].Shares[intI] = lstAssetProperties[intA].Shares[intI - 1];
                                                break;
                                            }
                                            else
                                            {
                                                lstAssetProperties[intA].Shares[intI] = ((dblRankRotationPortfolioValue[intI - 1] * lstAssetProperties[intA].AssetWtCoefficient[intI]) / dblWeightSum[intI - 1]) / lstRankRotation[intB].PriceTrendSecond[intI - 1];
                                                break;
                                            }
                                        }

                                    }
                                }

                                dblRankRotationPortfolioValue[intI] = 0;
                                
                                for (int intK = 0; intK < lstAssetProperties.Count; intK++)
                                {
                                    double dblPrcTnd = 0;
                                    for (int intL = 0; intL < lstRankRotation.Count; intL++)
                                    {
                                        if (lstRankRotation[intL].StrPair.StartsWith(lstAssetProperties[intK].AssetSymbolID + ":"))
                                        {
                                            dblPrcTnd = lstRankRotation[intL].PriceTrendFirst[intI];
                                            break;
                                        }
                                        else if (lstRankRotation[intL].StrPair.EndsWith(":" + lstAssetProperties[intK].AssetSymbolID))
                                        {
                                            dblPrcTnd = lstRankRotation[intL].PriceTrendSecond[intI];
                                            break;
                                        }
                                    }
                                    dblRankRotationPortfolioValue[intI] += (double)(lstAssetProperties[intK].Shares[intI] * dblPrcTnd);
                                    //if((intI+1)<(intArraySize-1))
                                    //dblPcntChangePortVal[intI] = (dblRankRotPortVal[intI+1]-dblRankRotPortVal[intI])/dblRankRotPortVal[intI];

                                }


                            }

                        }

                        // Calculation of 
                        int intRollingCount = 261;
                        double[] dblYrRollingChange = new double[intArraySize];
                        int[] intDayCountQuarters = new int[intArraySize];
                        int intMostRecentRebIndex = 217;

                        for (int intI = 217; intI < intArraySize; intI++)
                        {
                            if (intI == 217)
                            {
                                intDayCountQuarters[intI] = 1;

                                dblPcntChangePortVal[intI] = (dblRankRotationPortfolioValue[intI + 1] - dblRankRotationPortfolioValue[intI]) / dblRankRotationPortfolioValue[intI];

                            }
                            else
                            {
                                if (intDayCountQuarters[intI - 1] == 65)
                                {
                                    intDayCountQuarters[intI] = 1;
                                    intMostRecentRebIndex = intI - 1;
                                }
                                else
                                    intDayCountQuarters[intI] = intDayCountQuarters[intI - 1] + 1;

                                if ((intI + 1) < (intArraySize - 1))
                                    dblPcntChangePortVal[intI] = (dblRankRotationPortfolioValue[intI + 1] - dblRankRotationPortfolioValue[intI]) / dblRankRotationPortfolioValue[intI];

                            }

                            if (intI >= 478)
                            {
                                dblYrRollingChange[intI] = (dblRankRotationPortfolioValue[intI] - dblRankRotationPortfolioValue[intI - intRollingCount]) / dblRankRotationPortfolioValue[intI - intRollingCount];
                            }
                        }

                        double dblCAGR;
                        double dblSTDEV;
                        double dblSHARPE;
                        double dblRANKRET;

                        //dblCAGR = (Math.Pow((double)(dblRankRotationPortfolioValue[dblRankRotationPortfolioValue.Length - 1] / dblRankRotationPortfolioValue[217]), (double)(1.00 /(double) ((dblRankRotationPortfolioValue.Count()-1.00-217.00) / 260.00)))) - 1.00;
                        dblCAGR = (Math.Pow((double)(dblRankRotationPortfolioValue[dblRankRotationPortfolioValue.Length - 1] / dblRankRotationPortfolioValue[217]), (double)(1.00 / (double)((dblRankRotationPortfolioValue.Count() - 1.00 - 217.00) / (double)intTradingDays)))) - 1.00;
                        //dblSTDEV = Math.Pow(CommonUtility.STDev(dblPcntChangePortVal.Skip(215).Take(dblPcntChangePortVal.Length - 215 - 1).ToArray()) * 252, 0.5);
                        //dblSTDEV = CommonUtility.STDev(dblPcntChangePortVal.Skip(217).Select(x => (double)x).ToArray()) * (double)Math.Pow(260, 0.5);
                        dblSTDEV = CommonUtility.STDev(dblPcntChangePortVal.Skip(217).Select(x => (double)x).ToArray()) * (double)Math.Pow(intTradingDays, 0.5);
                        dblSHARPE = (dblCAGR - 0.02d) / dblSTDEV;

                        //dblRANKRET =(double)( (((double)intRankChangeEventValue.Skip(217).Sum()) / (double)(3116 / 260)) / (double)lstAssetProperties.Count);
                        dblRANKRET = (double)((((double)intRankChangeEventValue.Skip(217).Sum()) / (double)(3116 / intTradingDays)) / (double)lstAssetProperties.Count);


                        for (int intK = 0; intK < lstAssetProperties.Count; intK++)
                        {
                            for (int intI = 217; intI < intArraySize; intI++)
                            {
                                for (int intL = 0; intL < lstRankRotation.Count; intL++)
                                {
                                    if (lstRankRotation[intL].StrPair.StartsWith(lstAssetProperties[intK].AssetSymbolID + ":"))
                                        lstAssetProperties[intK].CurrentWeight[intI] = lstAssetProperties[intK].Shares[intI] * lstRankRotation[intL].PriceTrendFirst[intI] / dblRankRotationPortfolioValue[intI];
                                    else if (lstRankRotation[intL].StrPair.EndsWith(":" + lstAssetProperties[intK].AssetSymbolID))
                                        lstAssetProperties[intK].CurrentWeight[intI] = lstAssetProperties[intK].Shares[intI] * lstRankRotation[intL].PriceTrendSecond[intI] / dblRankRotationPortfolioValue[intI];
                                }
                            }
                        }
                        //** code end for RRR
                        #region Fill Data in PortfolioValueSet Entity
                        if (isRotationHistorical || isDividendOrSplit)
                        {
                            for (int i = 217; i < dtRankDates.Count(); i++)
                            {
                                DateTime dtFillDate = (dtRankDates[i]);
                                if (objHEEntities.PortfolioValueSet.Where(x => x.PortfolioSet.Id.Equals(intID) && x.Date.Equals(dtFillDate)).Count() > 0)
                                {
                                    objHEEntities.PortfolioValueSet.First(x => x.PortfolioSet.Id.Equals(intID) && x.Date.Equals(dtFillDate)).RRR = (float)dblRankRotationPortfolioValue[i];
                                }
                                else
                                {
                                    PortfolioValueSet objPortfolioValueSet = new PortfolioValueSet();
                                    objPortfolioValueSet.Date = dtFillDate;
                                    objPortfolioValueSet.RRR = (float)dblRankRotationPortfolioValue[i];
                                    objPortfolioValueSet.PortfolioSet = objHEEntities.PortfolioSet.First(x => x.Id.Equals(intID));
                                    objHEEntities.AddToPortfolioValueSet(objPortfolioValueSet);
                                }
                            }

                            if (isRotationHistorical)
                            objHEEntities.SaveChanges();

                        }
                        else
                        {
                            bool boolAddRecord = true;
                            int intDateCounter = dtRankDates.Count() - 1;
                            bool flag = true;
                            while (flag)
                            {
                                DateTime dtFillDate = dtRankDates[intDateCounter];
                                if (objHEEntities.PortfolioValueSet.Where(x => x.PortfolioSet.Id.Equals(intID) && x.Date.Equals(dtFillDate)).Count() > 0)
                                {
                                    boolAddRecord = false;
                                    if (objHEEntities.PortfolioValueSet.First(x => x.PortfolioSet.Id.Equals(intID) && x.Date.Equals(dtFillDate)).RRR == null)
                                    {
                                        objHEEntities.PortfolioValueSet.First(x => x.PortfolioSet.Id.Equals(intID) && x.Date.Equals(dtFillDate)).RRR = (float)dblRankRotationPortfolioValue[intDateCounter];
                                    }
                                    else
                                    {
                                      //  flag = false;
                                      //**
                                        //DateTime dtPreviousDate = DateTime.FromOADate(lstAssetAllocation[0].MDate[intDateCounter - 1]);
                                        DateTime dtPreviousDate = dtRankDates[intDateCounter - 1];
                                        float dtPreviousPFValue = (float)dblRankRotationPortfolioValue[intDateCounter - 1];
                                        double dtDiff = Microsoft.VisualBasic.DateAndTime.DateDiff(DateInterval.Day, dtPreviousDate, dtFillDate, Microsoft.VisualBasic.FirstDayOfWeek.Sunday, FirstWeekOfYear.Jan1);
                                        if (dtDiff > 0)
                                        {

                                            while (dtPreviousDate < dtFillDate)
                                            {
                                                if (!(dtPreviousDate.DayOfWeek.Equals(DayOfWeek.Saturday) || dtPreviousDate.DayOfWeek.Equals(DayOfWeek.Sunday)))
                                                {
                                                    if (objHEEntities.PortfolioValueSet.Where(x => x.PortfolioSet.Id.Equals(intID) && x.Date.Equals(dtPreviousDate)).Count() > 0)
                                                    {
                                                        if (objHEEntities.PortfolioValueSet.First(x => x.PortfolioSet.Id.Equals(intID) && x.Date.Equals(dtPreviousDate)).RRR == null)
                                                        {
                                                            objHEEntities.PortfolioValueSet.First(x => x.PortfolioSet.Id.Equals(intID) && x.Date.Equals(dtPreviousDate)).RRR = dtPreviousPFValue;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        PortfolioValueSet objPortfolioValueSet = new PortfolioValueSet();
                                                        objPortfolioValueSet.Date = dtPreviousDate;
                                                        objPortfolioValueSet.RRR = dtPreviousPFValue;
                                                        objPortfolioValueSet.PortfolioSet = objHEEntities.PortfolioSet.First(x => x.Id.Equals(intID));
                                                        objHEEntities.AddToPortfolioValueSet(objPortfolioValueSet);
                                                    }
                                                }

                                                dtPreviousDate = dtPreviousDate.AddDays(1);
                                            }
                                        }
                                        flag = false;

                                    }
                                }
                                else
                                    boolAddRecord = true;

                                if (boolAddRecord)
                                {
                                    PortfolioValueSet objPortfolioValueSet = new PortfolioValueSet();
                                    objPortfolioValueSet.Date = dtFillDate;
                                    objPortfolioValueSet.RRR = (float)dblRankRotationPortfolioValue[intDateCounter];
                                    objPortfolioValueSet.PortfolioSet = objHEEntities.PortfolioSet.First(x => x.Id.Equals(intID));
                                    objHEEntities.AddToPortfolioValueSet(objPortfolioValueSet);
                                }

                                intDateCounter--;
                            }
                        }
                        #endregion

                        //If historical then only commit changes
                        if (isRotationHistorical)
                        objHEEntities.SaveChanges();

                        #region This region is used to fill the data into the PortfolioMethodSet table.

                        //If count is greater than zero indicates that record is present.
                        if (objHEEntities.PortfolioMethodSet.Where(x => x.PortfolioSet.Id.Equals(intID)).Count() > 0)
                        {
                            objHEEntities.PortfolioMethodSet.First(x => x.PortfolioSet.Id.Equals(intID)).CAGR_RankRebRotation = dblCAGR;
                            objHEEntities.PortfolioMethodSet.First(x => x.PortfolioSet.Id.Equals(intID)).Current_Value_RankRebRotaion = dblRankRotationPortfolioValue[dblRankRotationPortfolioValue.Count() - 1];
                            objHEEntities.PortfolioMethodSet.First(x => x.PortfolioSet.Id.Equals(intID)).Increase_RankRebRotation = dblYrRollingChange.Max();
                            objHEEntities.PortfolioMethodSet.First(x => x.PortfolioSet.Id.Equals(intID)).DrawDown_RankRebRotation = dblYrRollingChange.Min();
                            objHEEntities.PortfolioMethodSet.First(x => x.PortfolioSet.Id.Equals(intID)).Stdev_RankRebRotation = dblSTDEV;
                            //objHEEntities.PortfolioMethodSet.First(x => x.PortfolioSet.Id.Equals(intID)).Rebalance_Date = dtAlertsRankRebalanceDate[intMostRecentRebIndex]; //lstAssetAllocation[0].MDate[intMostRecentRebIndex]);
                        }
                        else
                        {
                            PortfolioMethodSet objMethodSet = new PortfolioMethodSet();
                            objMethodSet.CAGR_RankRebRotation = dblCAGR;
                            objMethodSet.Current_Value_RankRebRotaion = dblRankRotationPortfolioValue[dblRankRotationPortfolioValue.Count() - 1];
                            objMethodSet.Increase_RankRebRotation = dblYrRollingChange.Max();
                            objMethodSet.DrawDown_RankRebRotation = dblYrRollingChange.Min();
                            objMethodSet.Stdev_RankRebRotation = dblSTDEV;
                            objMethodSet.PortfolioSet = objHEEntities.PortfolioSet.First(x => x.Id.Equals(intID));
                            //objMethodSet.Rebalance_Date = dtAlertsRankRebalanceDate[intMostRecentRebIndex]; //lstAssetAllocation[0].MDate[intMostRecentRebIndex]);//DateTime.FromOADate(lstAssetAllocation[0].MDate[intMostRecentRebIndex]);
                            objHEEntities.AddToPortfolioMethodSet(objMethodSet);
                        }

                        #endregion

                        if (isRotationHistorical)
                        objHEEntities.SaveChanges();

                        #region This region is used for filling the data into the PortfolioAllocationInfo table.

                        double currentVal = 0;
                        double startVal = 0;
                        double currWeight = 0;
                        double targetWt = 0;
                        double sharePriceTrend = 0;
                        double currShare = 0;
                        double sharePrice = 0;
                        double currSharePrice = 0;
                        int symID = 0;

                        for (int intA = 0; intA < lstAssetProperties.Count; intA++)
                        {
                            double dblCurShares = lstAssetProperties[intA].Shares[lstAssetProperties[intA].Shares.Count() - 1];
                            double oldShare = 0;
                            double newShare = 0;
                            symID = Convert.ToInt32(lstAssetProperties[intA].AssetSymbolID);
                            DateTime rotationDate = new DateTime();
                            for (int intB = lstAssetProperties[intA].Shares.Count() - 2; intB >= 0; intB--)
                            {
                                if (dblCurShares.Equals(lstAssetProperties[intA].Shares[intB]))
                                {

                                }
                                else
                                {
                                    oldShare = lstAssetProperties[intA].Shares[intB];
                                    newShare = lstAssetProperties[intA].Shares[intB + 1];
                                    rotationDate = dtRankDates[intB + 1];
                                    break;
                                }
                            }

                            for (int intL = 0; intL < lstRankRotation.Count; intL++)
                            {
                                if (lstRankRotation[intL].StrPair.StartsWith(lstAssetProperties[intA].AssetSymbolID + ":"))
                                {
                                    sharePrice = lstRankRotation[intL].PriceFirst[217];
                                    currentVal = lstAssetProperties[intA].Shares[lstAssetProperties[intA].Shares.Count() - 1] * lstRankRotation[intL].PriceTrendFirst[lstRankRotation[intL].PriceTrendFirst.Count() - 1];
                                    sharePriceTrend = lstRankRotation[intL].PriceTrendFirst[lstRankRotation[intL].PriceTrendFirst.Count() - 1];
                                    currSharePrice = lstRankRotation[intL].PriceFirst[lstRankRotation[intL].PriceFirst.Count() - 1];
                                }
                                else if (lstRankRotation[intL].StrPair.EndsWith(":" + lstAssetProperties[intA].AssetSymbolID))
                                {
                                    sharePrice = lstRankRotation[intL].PriceSecond[217];
                                    currentVal = lstAssetProperties[intA].Shares[lstAssetProperties[intA].Shares.Count() - 1] * lstRankRotation[intL].PriceTrendSecond[lstRankRotation[intL].PriceTrendSecond.Count() - 1];
                                    sharePriceTrend = lstRankRotation[intL].PriceTrendSecond[lstRankRotation[intL].PriceTrendSecond.Count() - 1];
                                    currSharePrice = lstRankRotation[intL].PriceSecond[lstRankRotation[intL].PriceSecond.Count() - 1];
                                }
                            }
                            startVal = (double)lstPortfolioContentSet.FirstOrDefault(x => x.AssetSymbolSet.Id.Equals(Convert.ToInt32(lstAssetProperties[intA].AssetSymbolID))).Value;
                            currWeight = lstAssetProperties[intA].CurrentWeight[lstAssetProperties[intA].CurrentWeight.Count() - 1];
                            targetWt = lstAssetProperties[intA].AssetWtCoefficient[lstAssetProperties[intA].AssetWtCoefficient.Count() - 1] * dblWeightSum[lstAssetProperties[intA].AssetWtCoefficient.Count() - 1];
                            currShare = lstAssetProperties[intA].Shares[lstAssetProperties[intA].Shares.Count() - 1];
                            symID = Convert.ToInt32(lstAssetProperties[intA].AssetSymbolID);

                            if (objHEEntities.PorfolioAllocationInfo.Where(x => x.PortfolioSet.Id.Equals(intID) && x.Allocation_Method.Equals("RRR") && x.AssetSymbolSet.Id.Equals(symID)).Count() > 0)
                            {
                                objHEEntities.PorfolioAllocationInfo.First(x => x.PortfolioSet.Id.Equals(intID) && x.Allocation_Method.Equals("RRR") && x.AssetSymbolSet.Id.Equals(symID)).Allocation_Method = "RRR";
                                objHEEntities.PorfolioAllocationInfo.First(x => x.PortfolioSet.Id.Equals(intID) && x.Allocation_Method.Equals("RRR") && x.AssetSymbolSet.Id.Equals(symID)).Current_Value = (double)currentVal;
                                objHEEntities.PorfolioAllocationInfo.First(x => x.PortfolioSet.Id.Equals(intID) && x.Allocation_Method.Equals("RRR") && x.AssetSymbolSet.Id.Equals(symID)).Current_Weight = currWeight;
                                objHEEntities.PorfolioAllocationInfo.First(x => x.PortfolioSet.Id.Equals(intID) && x.Allocation_Method.Equals("RRR") && x.AssetSymbolSet.Id.Equals(symID)).Starting_Value = startVal;
                                objHEEntities.PorfolioAllocationInfo.First(x => x.PortfolioSet.Id.Equals(intID) && x.Allocation_Method.Equals("RRR") && x.AssetSymbolSet.Id.Equals(symID)).Target_Weight = targetWt;
                                objHEEntities.PorfolioAllocationInfo.First(x => x.PortfolioSet.Id.Equals(intID) && x.Allocation_Method.Equals("RRR") && x.AssetSymbolSet.Id.Equals(symID)).Share_Price = sharePrice;
                                objHEEntities.PorfolioAllocationInfo.First(x => x.PortfolioSet.Id.Equals(intID) && x.Allocation_Method.Equals("RRR") && x.AssetSymbolSet.Id.Equals(symID)).SharePriceTrend = sharePriceTrend;
                                objHEEntities.PorfolioAllocationInfo.First(x => x.PortfolioSet.Id.Equals(intID) && x.Allocation_Method.Equals("RRR") && x.AssetSymbolSet.Id.Equals(symID)).CurrentSharePrice = currSharePrice;
                                //if (objHEEntities.PorfolioAllocationInfo.First(x => x.PortfolioSet.Id.Equals(intID) && x.Allocation_Method.Equals("RRR") && x.AssetSymbolSet.Id.Equals(symID)).CurrentShare != dblCurrentShare)
                                //{
                                //    objHEEntities.PorfolioAllocationInfo.First(x => x.PortfolioSet.Id.Equals(intID) && x.Allocation_Method.Equals("RRR") && x.AssetSymbolSet.Id.Equals(symID)).CurrentShare = dblCurrentShare;
                                //    objHEEntities.PorfolioAllocationInfo.First(x => x.PortfolioSet.Id.Equals(intID) && x.Allocation_Method.Equals("RRR") && x.AssetSymbolSet.Id.Equals(symID)).RotationDate = DateTime.FromOADate(set.MDate[set.MDate.Count() - 1]);
                                //}
                                //objHEEntities.PorfolioAllocationInfo.First(x => x.PortfolioSet.Id.Equals(intPortfolioID) && x.Allocation_Method.Equals("RREB") && x.AssetSymbolSet.Id.Equals(set.MAssetSymbolId)).CurrentShare = dblCurrentShare;
                                objHEEntities.PorfolioAllocationInfo.First(x => x.PortfolioSet.Id.Equals(intID) && x.Allocation_Method.Equals("RRR") && x.AssetSymbolSet.Id.Equals(symID)).CurrentShare = currShare;
                                objHEEntities.PorfolioAllocationInfo.First(x => x.PortfolioSet.Id.Equals(intID) && x.Allocation_Method.Equals("RRR") && x.AssetSymbolSet.Id.Equals(symID)).TotalValue = dblRankRotationPortfolioValue[dblRankRotationPortfolioValue.Count() - 1];
                                objHEEntities.PorfolioAllocationInfo.First(x => x.PortfolioSet.Id.Equals(intID) && x.Allocation_Method.Equals("RRR") && x.AssetSymbolSet.Id.Equals(symID)).RotationDate = rotationDate;//dtAlertsRankRebalanceDate[dtAlertsRankRebalanceDate.Count() - 1];
                            }
                            else
                            {
                                PorfolioAllocationInfo objPFAllocationSet = new PorfolioAllocationInfo();
                                objPFAllocationSet.Allocation_Method = "RRR";
                                objPFAllocationSet.Current_Value = (double)currentVal;
                                objPFAllocationSet.Current_Weight = currWeight;
                                objPFAllocationSet.Starting_Value = startVal;
                                objPFAllocationSet.Target_Weight = targetWt;
                                objPFAllocationSet.Share_Price = sharePrice;
                                objPFAllocationSet.SharePriceTrend = sharePriceTrend;
                                objPFAllocationSet.CurrentSharePrice = currSharePrice;
                                objPFAllocationSet.CurrentShare = currShare;
                                objPFAllocationSet.RotationDate = rotationDate;//dtAlertsRankRebalanceDate[dtAlertsRankRebalanceDate.Count() - 1];
                                objPFAllocationSet.TotalValue = dblRankRotationPortfolioValue[dblRankRotationPortfolioValue.Count() - 1];
                                objPFAllocationSet.PortfolioSet = objHEEntities.PortfolioSet.First(x => x.Id.Equals(intID));
                                objPFAllocationSet.AssetSymbolSet = objHEEntities.AssetSymbolSet.First(x => x.Id.Equals(symID));
                                objHEEntities.AddToPorfolioAllocationInfo(objPFAllocationSet);
                            }
                        }


                        #endregion

                        if (isRotationHistorical)
                        objHEEntities.SaveChanges();

                        #region Fill data into MostRecentRebalances Entity commented.

                        //for (int intA = 0; intA < lstAssetProperties.Count; intA++)
                        //{
                        //    symID = Convert.ToInt32(lstAssetProperties[intA].AssetSymbolID);
                        //    if (objHEEntities.MostRecentRebalances.Where(x => x.PortfolioSet.Id.Equals(intID) && x.Allocation_Method.Equals("RRR") && x.AssetSymbolSet.Id.Equals(symID)).Count() > 0)
                        //    {
                        //        objHEEntities.MostRecentRebalances.First(x => x.PortfolioSet.Id.Equals(intID) && x.Allocation_Method.Equals("RRR") && x.AssetSymbolSet.Id.Equals(symID)).Allocation_Method = "RRR";
                        //        objHEEntities.MostRecentRebalances.First(x => x.PortfolioSet.Id.Equals(intID) && x.Allocation_Method.Equals("RRR") && x.AssetSymbolSet.Id.Equals(symID)).RebalanceDate = dtAlertsRankRebalanceDate[dtAlertsRankRebalanceDate.Count()-1];
                        //        objHEEntities.MostRecentRebalances.First(x => x.PortfolioSet.Id.Equals(intID) && x.Allocation_Method.Equals("RRR") && x.AssetSymbolSet.Id.Equals(symID)).OldShare = lstAssetProperties[intA].Shares[intMostRecentRebIndex - 1];
                        //        objHEEntities.MostRecentRebalances.First(x => x.PortfolioSet.Id.Equals(intID) && x.Allocation_Method.Equals("RRR") && x.AssetSymbolSet.Id.Equals(symID)).NewShare = lstAssetProperties[intA].Shares[intMostRecentRebIndex];
                        //    }
                        //    else
                        //    {
                        //        MostRecentRebalances objMRR = new MostRecentRebalances();
                        //        objMRR.Allocation_Method = "RRR";
                        //        objMRR.RebalanceDate = dtAlertsRankRebalanceDate[dtAlertsRankRebalanceDate.Count()-1];
                        //        objMRR.NewShare = lstAssetProperties[intA].Shares[intMostRecentRebIndex];
                        //        objMRR.OldShare = lstAssetProperties[intA].Shares[intMostRecentRebIndex - 1];
                        //        objMRR.PortfolioSet = objHEEntities.PortfolioSet.First(x => x.Id.Equals(intID));
                        //        objMRR.AssetSymbolSet = objHEEntities.AssetSymbolSet.First(x => x.Id.Equals(symID));
                        //        objHEEntities.AddToMostRecentRebalances(objMRR);
                        //    }
                        //}

                        #endregion

                        objHEEntities.SaveChanges();

                        #region fill Alertshare table
                        for (int intA = 0; intA < lstAssetProperties.Count; intA++)
                        {
                            double dblCurShares = lstAssetProperties[intA].Shares[lstAssetProperties[intA].Shares.Count() - 1];
                            double oldShare = 0;
                            double newShare = 0;
                            symID = Convert.ToInt32(lstAssetProperties[intA].AssetSymbolID);
                            DateTime rotationDate = new DateTime();
                            for (int intB = lstAssetProperties[intA].Shares.Count() - 2; intB >= 0; intB--)
                            {
                                if (dblCurShares.Equals(lstAssetProperties[intA].Shares[intB]))
                                {

                                }
                                else
                                {
                                    oldShare = lstAssetProperties[intA].Shares[intB];
                                    newShare = lstAssetProperties[intA].Shares[intB + 1];
                                    rotationDate = dtRankDates[intB + 1];
                                    break;
                                }
                            }

                            if (objHEEntities.AlertShare.Where(x => x.AssetSymbolSet.Id.Equals(symID) && x.Allocation_Method.Trim().ToLower().Equals("rrr") && x.PortfolioSet.Id.Equals(intID)).Count() > 0)
                            {
                                objHEEntities.AlertShare.First(x => x.AssetSymbolSet.Id.Equals(symID) && x.Allocation_Method.Trim().ToLower().Equals("rrr") && x.PortfolioSet.Id.Equals(intID)).OldShare = oldShare;
                                objHEEntities.AlertShare.First(x => x.AssetSymbolSet.Id.Equals(symID) && x.Allocation_Method.Trim().ToLower().Equals("rrr") && x.PortfolioSet.Id.Equals(intID)).NewShare = newShare;
                                objHEEntities.AlertShare.First(x => x.AssetSymbolSet.Id.Equals(symID) && x.Allocation_Method.Trim().ToLower().Equals("rrr") && x.PortfolioSet.Id.Equals(intID)).RotationDate = rotationDate;
                            }
                            else
                            {
                                AlertShare objAlertShare = new AlertShare();
                                objAlertShare.OldShare = oldShare;
                                objAlertShare.NewShare = newShare;
                                objAlertShare.Allocation_Method = "RRR";
                                objAlertShare.RotationDate = rotationDate;
                                objAlertShare.PortfolioSet = objHEEntities.PortfolioSet.FirstOrDefault(x => x.Id.Equals(intID));
                                objAlertShare.AssetSymbolSet = objHEEntities.AssetSymbolSet.FirstOrDefault(x => x.Id.Equals(symID));
                                objHEEntities.AddToAlertShare(objAlertShare);
                            }
                        }
                        #endregion

                        //Commit to affect changes
                        objHEEntities.SaveChanges();

                        #endregion
                    }
                }
            }
            catch (Exception)
            {

            }
        }
        #endregion
    }

    public class Pairs
    {
        #region Constructors

        public Pairs(int x, int y)
        {
            IntFirst = x;
            IntSecond = y;
        }

        #endregion

        #region Private Varaibles

        private int intFirst;
        private int intSecond;

        #endregion

        #region Public Properties

        public int IntFirst
        {
            get { return intFirst; }
            set { intFirst = value; }
        }

        public int IntSecond
        {
            get { return intSecond; }
            set { intSecond = value; }
        }

        #endregion

        #region Public Methods

        public bool IsNumberPartOfPair(int n)
        {
            return ((intFirst == n) || (intSecond == n));
        }

        #endregion
    }
}
