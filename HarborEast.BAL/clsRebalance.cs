using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Objects.DataClasses;
using System.Data;
using Microsoft.VisualBasic;

namespace HarborEast.BAL
{
    public class clsRebalance
    {
        #region Variable Declarations

        Harbor_EastEntities objHEEntities = new Harbor_EastEntities();
        //change 9-Mar-2011 trading days
        int intTradingDays = 0;
        bool isREBHistorical = false;
        bool isSplitOrDiv = false;
        #endregion

        #region Constructors

        public clsRebalance()
        {
            //change 9-Mar-2011 trading days
            //intTradingDays = (int)objHEEntities.TrendEngineVariablesSet.ElementAt(0).YearTradingDays;
            intTradingDays = (int) objHEEntities.TrendEngineVariablesSet.Select(x => x.YearTradingDays).First();
        }

        #endregion
        #region Public Methods

        /// <summary>
        /// This method is used for calculating the values for Rebalance operation.
        /// </summary>
        /// <param name="intSelectedIndex"></param>
        /// <param name="strSelectedValue"></param>
        /// <param name="isChecked"></param>
        /// <returns></returns>
        public DataTable PerformRebalanceOperation(int intSelectedIndex, string strSelectedValue, bool isChecked)
        {
            DataTable dt = new DataTable();
            try
            {
                //If the passed selected index of the drop down is not -1.
                if (intSelectedIndex != -1)
                {
                    double[] decBHPortfolioValue = null;
                    double[] decPortofolioDailyChange = null;
                    double[] decYearRollChange = null;
                    int[] intDayCountQuarters = null;
                    int intMostRecentRebIndex = 217;
                    int intID = Convert.ToInt32(strSelectedValue);
                    List<AssetAllocation> lstAssetAllocation;

                    //Get the collection of all the symbol`s within the portfolio.
                    List<PortfolioContentSet> lstPortfolioContentSet = objHEEntities.PortfolioContentSet.Where(x => x.PortfolioSet.Id.Equals(intID)).ToList();

                    isREBHistorical = CommonUtility.IsHistoricalProcessing();
                    isSplitOrDiv = CommonUtility.IsDividendOrSplitForPortfolio(lstPortfolioContentSet);

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
                            foreach (var objSet in lstPortfolioContentSet)
                            {
                                //decBHTotal = decBHTotal + (decimal)objSet.Value;
                                objAssetAllocation = new AssetAllocation(intArraySize, 1);
                                intAssetSymbolID = Convert.ToInt32(((EntityReference)(objSet.AssetSymbolSetReference)).EntityKey.EntityKeyValues[0].Value);
                                objAssetAllocation.MAssetSymbolId = intAssetSymbolID;
                                objAssetAllocation.MDate = objHEEntities.AssetPriceSet.Where(x => x.PriceTrend != null && x.Date >= dtFinalStartDate && x.Date <= dtFinalLastDate && x.AssetSymbolSet.Id.Equals(intAssetSymbolID)).OrderBy(x => x.Date).Select(x => x.Date).ToArray().Select(x => x.ToOADate()).ToArray();
                                objAssetAllocation.MPrice = objHEEntities.AssetPriceSet.Where(x => x.PriceTrend != null && x.Date >= dtFinalStartDate && x.Date <= dtFinalLastDate && x.AssetSymbolSet.Id.Equals(intAssetSymbolID)).OrderBy(x => x.Date).Select(x => (double)x.Price).ToArray();
                                objAssetAllocation.MPriceTrend = objHEEntities.AssetPriceSet.Where(x => x.PriceTrend != null && x.Date >= dtFinalStartDate && x.Date <= dtFinalLastDate && x.AssetSymbolSet.Id.Equals(intAssetSymbolID)).OrderBy(x => x.Date).Select(x => (double)x.PriceTrend).ToArray();
                                objAssetAllocation.MAssetValue[217] =(double)objSet.Value;
                                lstAssetAllocation.Add(objAssetAllocation);
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
                                        objSet.MSlope[intI] = (CommonUtility.Slope(objSet.MSMA.Skip(intSlopeSkipCount).Take(5).Select(x => (double)x).ToArray(), objSet.MDate.Skip(intSlopeSkipCount).Take(5).ToArray()) / (double)objSet.MSMA[intI]);
                                        intSlopeSkipCount++;
                                    }
                                }
                            }
                            int intRollingCount = 261;
                            for (int intI = 217; intI <= lstAssetAllocation[0].MPrice.Count() - 1; intI++)
                            {
                                if (intI == 217)
                                {
                                    //annual rebalance of shares logic
                                    intDayCountQuarters[intI] = 1;
                                    

                                    //Calculate Portfolio total value.
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

                                    //Calculate Asset Share.
                                    foreach (var set in lstAssetAllocation)
                                    {
                                        //changed 23-Feb-2011 for first day anomaly
                                        set.MAssetShares[intI] = decBHPortfolioValue[intI] * set.MWeight[intI] / set.MPrice[intI];
                                    }
                                }
                                else
                                {
                                    //Day count quarters.
                                    //annual rebalance of shares logic
                                    //if (intDayCountQuarters[intI - 1] == 65)
                                    //{
                                    //    intDayCountQuarters[intI] = 1;
                                    //    intMostRecentRebIndex = intI-1;
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
                                        intMostRecentRebIndex = intI;
                                        intDayCountQuarters[intI] = 1;
                                    }
                                    else
                                    {
                                        intDayCountQuarters[intI] = intDayCountQuarters[intI - 1] + 1; 
                                    }

                                    //Calculate Target Weight.
                                    foreach (var objSet in lstAssetAllocation)
                                    {
                                        objSet.MTargetWeight[intI] = objSet.MTargetWeight[intI - 1];
                                    }

                                    //Calculate Asset Share.
                                    foreach (var set in lstAssetAllocation)
                                    {
                                        //annual rebalance of shares logic
                                        //if (intDayCountQuarters[intI] < 65)
                                        //    set.MAssetShares[intI] = set.MAssetShares[intI - 1];
                                        //else
                                        //    set.MAssetShares[intI] = decBHPortfolioValue[intI - 1] * set.MTargetWeight[intI] / set.MPrice[intI - 1];
                                        if (intDayCountQuarters[intI] == 1)
                                            set.MAssetShares[intI] = decBHPortfolioValue[intI - 1] * set.MTargetWeight[intI] / set.MPrice[intI - 1];
                                        else
                                            set.MAssetShares[intI] = set.MAssetShares[intI - 1];
                                    }
                                    //Calculate Asset value.
                                    foreach (var set in lstAssetAllocation)
                                    {
                                        set.MAssetValue[intI] = set.MAssetShares[intI] * set.MPrice[intI];
                                    }

                                    //Calculate Portfolio total value.
                                    decBHPortfolioValue[intI] = CommonUtility.GetBHPortfolioTotalValue(lstAssetAllocation, intI);

                                    //Calculate Weight.
                                    foreach (var objSet in lstAssetAllocation)
                                    {
                                        //annual rebalance of shares logic
                                        //if (intDayCountQuarters[intI] < 65)
                                        //    objSet.MWeight[intI] = objSet.MAssetValue[intI] / decBHPortfolioValue[intI];
                                        //else
                                        //    objSet.MWeight[intI] = objSet.MTargetWeight[intI];
                                        if (intDayCountQuarters[intI] == 1)
                                            objSet.MWeight[intI] = objSet.MTargetWeight[intI];
                                        else
                                            objSet.MWeight[intI] = objSet.MAssetValue[intI] / decBHPortfolioValue[intI];
                                            
                                    }
                                }

                                //Calculate Portfolio daily change.
                                if (intI > 217)
                                {
                                    decPortofolioDailyChange[intI] = (decBHPortfolioValue[intI] - decBHPortfolioValue[intI - 1]) / decBHPortfolioValue[intI - 1];
                                }

                                //calculate Portfolio yearly rolling change.
                                if (intI >= 478)
                                {
                                    decYearRollChange[intI] = (decBHPortfolioValue[intI] - decBHPortfolioValue[intI - intRollingCount]) / decBHPortfolioValue[intI - intRollingCount];
                                }
                            }

                            #region Fill Data in PortfolioValueSet Entity
                            if (isREBHistorical || isSplitOrDiv)
                            {
                                for (int i = 217; i < lstAssetAllocation[0].MDate.Count(); i++)
                                {
                                    DateTime dtFillDate = DateTime.FromOADate(lstAssetAllocation[0].MDate[i]);
                                    if (objHEEntities.PortfolioValueSet.Where(x => x.PortfolioSet.Id.Equals(intID) && x.Date.Equals(dtFillDate)).Count() > 0)
                                    {
                                        objHEEntities.PortfolioValueSet.First(x => x.PortfolioSet.Id.Equals(intID) && x.Date.Equals(dtFillDate)).REB = (float)decBHPortfolioValue[i];
                                    }
                                    else
                                    {
                                        PortfolioValueSet objPortfolioValueSet = new PortfolioValueSet();
                                        objPortfolioValueSet.Date = dtFillDate;
                                        objPortfolioValueSet.REB = (float)decBHPortfolioValue[i];
                                        objPortfolioValueSet.PortfolioSet = objHEEntities.PortfolioSet.First(x => x.Id.Equals(intID));
                                        objHEEntities.AddToPortfolioValueSet(objPortfolioValueSet);
                                    }
                                }
                                if(isREBHistorical)
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
                                    if (objHEEntities.PortfolioValueSet.Where(x => x.PortfolioSet.Id.Equals(intID) && x.Date.Equals(dtFillDate)).Count() > 0)
                                    {
                                        boolAddRecord = false;
                                        if (objHEEntities.PortfolioValueSet.First(x => x.PortfolioSet.Id.Equals(intID) && x.Date.Equals(dtFillDate)).REB == null)
                                        {
                                            objHEEntities.PortfolioValueSet.First(x => x.PortfolioSet.Id.Equals(intID) && x.Date.Equals(dtFillDate)).REB = (float)decBHPortfolioValue[intDateCounter];
                                        }
                                        else
                                        {
                                           // flag = false;
                                            //** Change Code for holiday
                                            DateTime dtPreviousDate = DateTime.FromOADate(lstAssetAllocation[0].MDate[intDateCounter - 1]);
                                            float dtPreviousPFValue = (float)decBHPortfolioValue[intDateCounter - 1];
                                            double dtDiff = Microsoft.VisualBasic.DateAndTime.DateDiff(DateInterval.Day, dtPreviousDate, dtFillDate, Microsoft.VisualBasic.FirstDayOfWeek.Sunday, FirstWeekOfYear.Jan1);
                                            if (dtDiff > 0)
                                            {

                                                while (dtPreviousDate < dtFillDate)
                                                {
                                                    if (!(dtPreviousDate.DayOfWeek.Equals(DayOfWeek.Saturday) || dtPreviousDate.DayOfWeek.Equals(DayOfWeek.Sunday)))
                                                    {
                                                        if (objHEEntities.PortfolioValueSet.Where(x => x.PortfolioSet.Id.Equals(intID) && x.Date.Equals(dtPreviousDate)).Count() > 0)
                                                        {
                                                            if (objHEEntities.PortfolioValueSet.First(x => x.PortfolioSet.Id.Equals(intID) && x.Date.Equals(dtPreviousDate)).REB == null)
                                                            {
                                                                objHEEntities.PortfolioValueSet.First(x => x.PortfolioSet.Id.Equals(intID) && x.Date.Equals(dtPreviousDate)).REB = dtPreviousPFValue;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            PortfolioValueSet objPortfolioValueSet = new PortfolioValueSet();
                                                            objPortfolioValueSet.Date = dtPreviousDate;
                                                            objPortfolioValueSet.REB = dtPreviousPFValue;
                                                            objPortfolioValueSet.PortfolioSet = objHEEntities.PortfolioSet.First(x => x.Id.Equals(intID));
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
                                        objPortfolioValueSet.REB = (float)decBHPortfolioValue[intDateCounter];
                                        objPortfolioValueSet.PortfolioSet = objHEEntities.PortfolioSet.First(x => x.Id.Equals(intID));
                                        objHEEntities.AddToPortfolioValueSet(objPortfolioValueSet);
                                    }

                                    intDateCounter--;
                                }
                            }
                            #endregion
                            //When Historical then only commit
                            if (isREBHistorical)
                            objHEEntities.SaveChanges();

                            #region This region is used to fill the data into the PortfolioMethodSet table.

                            //If count is greater than zero indicates that record is present.
                            if (objHEEntities.PortfolioMethodSet.Where(x => x.PortfolioSet.Id.Equals(intID)).Count() > 0)
                            {
                                //objHEEntities.PortfolioMethodSet.First(x => x.PortfolioSet.Id.Equals(intID)).CAGR_Reb = (float)Math.Pow((double)(decBHPortfolioValue[decBHPortfolioValue.Count() - 1] / decBHPortfolioValue[217]), (double)(1.00 / ((decBHPortfolioValue.Count() - 1 - 217.00) / 260.00))) - 1;
                                objHEEntities.PortfolioMethodSet.First(x => x.PortfolioSet.Id.Equals(intID)).CAGR_Reb = (float)Math.Pow((double)(decBHPortfolioValue[decBHPortfolioValue.Count() - 1] / decBHPortfolioValue[217]), (double)(1.00 / ((decBHPortfolioValue.Count() - 1 - 217.00) / (double)intTradingDays))) - 1;
                                objHEEntities.PortfolioMethodSet.First(x => x.PortfolioSet.Id.Equals(intID)).Current_Value_Reb = (float)decBHPortfolioValue[decBHPortfolioValue.Count() - 1];
                                objHEEntities.PortfolioMethodSet.First(x => x.PortfolioSet.Id.Equals(intID)).Increase_Reb = (float)decYearRollChange.Max();
                                objHEEntities.PortfolioMethodSet.First(x => x.PortfolioSet.Id.Equals(intID)).DrawDown_Reb = (float)decYearRollChange.Min();
                                //objHEEntities.PortfolioMethodSet.First(x => x.PortfolioSet.Id.Equals(intID)).Stdev_Reb = CommonUtility.STDev(decPortofolioDailyChange.Skip(218).Select(x => (double)x).ToArray()) * (double)Math.Pow(260, 0.5);
                                objHEEntities.PortfolioMethodSet.First(x => x.PortfolioSet.Id.Equals(intID)).Stdev_Reb = CommonUtility.STDev(decPortofolioDailyChange.Skip(218).Select(x => (double)x).ToArray()) * (double)Math.Pow(intTradingDays, 0.5);
                            }
                            else
                            {
                                PortfolioMethodSet objMethodSet = new PortfolioMethodSet();
                                //objMethodSet.CAGR_Reb = (float)Math.Pow((double)(decBHPortfolioValue[decBHPortfolioValue.Count() - 1] / decBHPortfolioValue[217]), (double)(1.00 / ((decBHPortfolioValue.Count() - 1 - 217.00) / 260.00))) - 1;
                                objMethodSet.CAGR_Reb = (float)Math.Pow((double)(decBHPortfolioValue[decBHPortfolioValue.Count() - 1] / decBHPortfolioValue[217]), (double)(1.00 / ((decBHPortfolioValue.Count() - 1 - 217.00) / (double)intTradingDays))) - 1;
                                objMethodSet.Current_Value_Reb = (float)decBHPortfolioValue[decBHPortfolioValue.Count() - 1];
                                objMethodSet.Increase_Reb = (float)decYearRollChange.Max();
                                objMethodSet.DrawDown_Reb = (float)decYearRollChange.Min();
                                //objMethodSet.Stdev_Reb = CommonUtility.STDev(decPortofolioDailyChange.Skip(218).Select(x => (double)x).ToArray()) * (double)Math.Pow(260, 0.5);
                                objMethodSet.Stdev_Reb = CommonUtility.STDev(decPortofolioDailyChange.Skip(218).Select(x => (double)x).ToArray()) * (double)Math.Pow(intTradingDays, 0.5);
                                objMethodSet.PortfolioSet = objHEEntities.PortfolioSet.First(x => x.Id.Equals(intID));
                                objHEEntities.AddToPortfolioMethodSet(objMethodSet);
                            }

                            #endregion
                            
                            if (isREBHistorical)
                            objHEEntities.SaveChanges();
                            
                            #region This region is used for filling the data into the PortfolioAllocationInfo table.

                            //fill the data into the PortfolioAllocationInfo table.
                            foreach (var set in lstAssetAllocation)
                            {
                                if (objHEEntities.PorfolioAllocationInfo.Where(x => x.PortfolioSet.Id.Equals(intID) && x.Allocation_Method.Equals("REB") && x.AssetSymbolSet.Id.Equals(set.MAssetSymbolId)).Count() > 0)
                                {
                                    objHEEntities.PorfolioAllocationInfo.First(x => x.PortfolioSet.Id.Equals(intID) && x.Allocation_Method.Equals("REB") && x.AssetSymbolSet.Id.Equals(set.MAssetSymbolId)).Allocation_Method = "REB";
                                    objHEEntities.PorfolioAllocationInfo.First(x => x.PortfolioSet.Id.Equals(intID) && x.Allocation_Method.Equals("REB") && x.AssetSymbolSet.Id.Equals(set.MAssetSymbolId)).Current_Value = (double)set.MAssetValue[set.MAssetValue.Count() - 1];
                                    objHEEntities.PorfolioAllocationInfo.First(x => x.PortfolioSet.Id.Equals(intID) && x.Allocation_Method.Equals("REB") && x.AssetSymbolSet.Id.Equals(set.MAssetSymbolId)).Current_Weight = (double)set.MWeight[set.MWeight.Count() - 1];
                                    objHEEntities.PorfolioAllocationInfo.First(x => x.PortfolioSet.Id.Equals(intID) && x.Allocation_Method.Equals("REB") && x.AssetSymbolSet.Id.Equals(set.MAssetSymbolId)).Starting_Value = (double)set.MAssetValue[217];
                                    objHEEntities.PorfolioAllocationInfo.First(x => x.PortfolioSet.Id.Equals(intID) && x.Allocation_Method.Equals("REB") && x.AssetSymbolSet.Id.Equals(set.MAssetSymbolId)).Target_Weight = (double)set.MTargetWeight[set.MTargetWeight.Count() - 1];
                                    objHEEntities.PorfolioAllocationInfo.First(x => x.PortfolioSet.Id.Equals(intID) && x.Allocation_Method.Equals("REB") && x.AssetSymbolSet.Id.Equals(set.MAssetSymbolId)).Share_Price = (double)set.MPrice[217];
                                    objHEEntities.PorfolioAllocationInfo.First(x => x.PortfolioSet.Id.Equals(intID) && x.Allocation_Method.Equals("REB") && x.AssetSymbolSet.Id.Equals(set.MAssetSymbolId)).SharePriceTrend = (double)set.MPriceTrend[set.MPriceTrend.Count() - 1];
                                    objHEEntities.PorfolioAllocationInfo.First(x => x.PortfolioSet.Id.Equals(intID) && x.Allocation_Method.Equals("REB") && x.AssetSymbolSet.Id.Equals(set.MAssetSymbolId)).CurrentSharePrice = (double)set.MPrice[set.MPrice.Count() - 1];
                                    objHEEntities.PorfolioAllocationInfo.First(x => x.PortfolioSet.Id.Equals(intID) && x.Allocation_Method.Equals("REB") && x.AssetSymbolSet.Id.Equals(set.MAssetSymbolId)).CurrentShare = (double)set.MAssetShares[set.MAssetShares.Count() - 1];
                                }
                                else
                                {
                                    PorfolioAllocationInfo objPFAllocationSet = new PorfolioAllocationInfo();
                                    objPFAllocationSet.Allocation_Method = "REB";
                                    objPFAllocationSet.Current_Value = (double)set.MAssetValue[set.MAssetValue.Count() - 1];
                                    objPFAllocationSet.Current_Weight = (double)set.MWeight[set.MWeight.Count() - 1];
                                    objPFAllocationSet.Starting_Value = (double)set.MAssetValue[217];
                                    objPFAllocationSet.Target_Weight = (double)set.MTargetWeight[set.MTargetWeight.Count() - 1];
                                    objPFAllocationSet.Share_Price = (double)set.MPrice[217];
                                    objPFAllocationSet.SharePriceTrend = (double)set.MPriceTrend[set.MPriceTrend.Count() - 1];
                                    objPFAllocationSet.CurrentSharePrice = (double)set.MPrice[set.MPrice.Count() - 1];
                                    objPFAllocationSet.CurrentShare = (double)set.MAssetShares[set.MAssetShares.Count() - 1];
                                    objPFAllocationSet.PortfolioSet = objHEEntities.PortfolioSet.First(x => x.Id.Equals(intID));
                                    objPFAllocationSet.AssetSymbolSet = objHEEntities.AssetSymbolSet.First(x => x.Id.Equals(set.MAssetSymbolId));
                                    objHEEntities.AddToPorfolioAllocationInfo(objPFAllocationSet);
                                }
                            }

                            #endregion
                            
                            if (isREBHistorical)
                            objHEEntities.SaveChanges();

                            #region Fill data into MostRecentRebalances Entity.

                            foreach (var set in lstAssetAllocation)
                            {
                                if (objHEEntities.MostRecentRebalances.Where(x => x.PortfolioSet.Id.Equals(intID) && x.Allocation_Method.Equals("REB") && x.AssetSymbolSet.Id.Equals(set.MAssetSymbolId)).Count() > 0)
                                {
                                    objHEEntities.MostRecentRebalances.First(x => x.PortfolioSet.Id.Equals(intID) && x.Allocation_Method.Equals("REB") && x.AssetSymbolSet.Id.Equals(set.MAssetSymbolId)).Allocation_Method = "REB";
                                    objHEEntities.MostRecentRebalances.First(x => x.PortfolioSet.Id.Equals(intID) && x.Allocation_Method.Equals("REB") && x.AssetSymbolSet.Id.Equals(set.MAssetSymbolId)).RebalanceDate = DateTime.FromOADate(set.MDate[intMostRecentRebIndex]);
                                    objHEEntities.MostRecentRebalances.First(x => x.PortfolioSet.Id.Equals(intID) && x.Allocation_Method.Equals("REB") && x.AssetSymbolSet.Id.Equals(set.MAssetSymbolId)).OldShare = (double)set.MAssetShares[intMostRecentRebIndex-1];
                                    objHEEntities.MostRecentRebalances.First(x => x.PortfolioSet.Id.Equals(intID) && x.Allocation_Method.Equals("REB") && x.AssetSymbolSet.Id.Equals(set.MAssetSymbolId)).NewShare = (double)set.MAssetShares[intMostRecentRebIndex];
                                }
                                else
                                {
                                    MostRecentRebalances objMRR = new MostRecentRebalances();
                                    objMRR.Allocation_Method = "REB";
                                    objMRR.RebalanceDate = DateTime.FromOADate(set.MDate[intMostRecentRebIndex]);
                                    objMRR.NewShare = (double)set.MAssetShares[intMostRecentRebIndex];
                                    objMRR.OldShare = (double)set.MAssetShares[intMostRecentRebIndex-1];
                                    objMRR.PortfolioSet = objHEEntities.PortfolioSet.First(x => x.Id.Equals(intID));
                                    objMRR.AssetSymbolSet = objHEEntities.AssetSymbolSet.First(x => x.Id.Equals(set.MAssetSymbolId));
                                    objHEEntities.AddToMostRecentRebalances(objMRR);
                                }
                            }

                            #endregion

                            //Commit the changes to the database.
                            objHEEntities.SaveChanges();

                            #region This region is used for binding the data to the datatable and then returning this datatable to the calling function.

                            if (isChecked)
                            {
                                DataColumn dcTradeDate = new DataColumn();
                                dcTradeDate.DataType = typeof(string);
                                dcTradeDate.ColumnName = "Trading Date";
                                dt.Columns.Add(dcTradeDate);

                                foreach (var set in lstPortfolioContentSet)
                                {
                                    intAssetSymbolID = Convert.ToInt32(((EntityReference)(set.AssetSymbolSetReference)).EntityKey.EntityKeyValues[0].Value);
                                    DataColumn dcWeight = new DataColumn();
                                    dcWeight.DataType = typeof(decimal);
                                    dcWeight.ColumnName = "Weight" + objHEEntities.AssetSymbolSet.First(x => x.Id.Equals(intAssetSymbolID)).Symbol;
                                    dt.Columns.Add(dcWeight);

                                    DataColumn dcShares = new DataColumn();
                                    dcShares.DataType = typeof(decimal);
                                    dcShares.ColumnName = "Shares" + objHEEntities.AssetSymbolSet.First(x => x.Id.Equals(intAssetSymbolID)).Symbol;
                                    dt.Columns.Add(dcShares);

                                    DataColumn dcValue = new DataColumn();
                                    dcValue.DataType = typeof(decimal);
                                    dcValue.ColumnName = "Value" + objHEEntities.AssetSymbolSet.First(x => x.Id.Equals(intAssetSymbolID)).Symbol;
                                    dt.Columns.Add(dcValue);
                                }
                                DataColumn dcBHPortfolioValue = new DataColumn();
                                dcBHPortfolioValue.DataType = typeof(decimal);
                                dcBHPortfolioValue.ColumnName = "BH Portfolio Value";
                                dt.Columns.Add(dcBHPortfolioValue);

                                DataColumn dcPortfolioDailyChange = new DataColumn();
                                dcPortfolioDailyChange.DataType = typeof(decimal);
                                dcPortfolioDailyChange.ColumnName = "Portfolio Daily Change";
                                dt.Columns.Add(dcPortfolioDailyChange);

                                DataColumn dcRollingChange = new DataColumn();
                                dcRollingChange.DataType = typeof(decimal);
                                dcRollingChange.ColumnName = "Year Rolling Change";
                                dt.Columns.Add(dcRollingChange);

                                for (int i = 217; i < objAssetAllocation.MAssetValue.Count() - 1; i++)
                                {
                                    DataRow dRow = dt.NewRow();
                                    string strColumnName = string.Empty;
                                    string strSymbol = string.Empty;
                                    dRow["Trading Date"] = DateTime.FromOADate(objAssetAllocation.MDate[i]).ToShortDateString();

                                    foreach (var set in lstAssetAllocation)
                                    {
                                        strSymbol = objHEEntities.AssetSymbolSet.First(x => x.Id.Equals(set.MAssetSymbolId)).Symbol;
                                        strColumnName = "Weight" + strSymbol;
                                        dRow[strColumnName] = Math.Round(set.MWeight[i] * 100, 2);

                                        strColumnName = "Shares" + strSymbol;
                                        dRow[strColumnName] = Math.Round(set.MAssetShares[i], 2);

                                        strColumnName = "Value" + strSymbol;
                                        dRow[strColumnName] = Math.Round(set.MAssetValue[i], 2);
                                    }

                                    dRow["BH Portfolio Value"] = Math.Round(decBHPortfolioValue[i] * 100, 2);
                                    dRow["Portfolio Daily Change"] = Math.Round(decPortofolioDailyChange[i] * 100, 2);
                                    dRow["Year Rolling Change"] = Math.Round(decYearRollChange[i] * 100, 2);
                                    dt.Rows.Add(dRow);
                                }
                            }

                            #endregion
                        }

                        #endregion
                    }
                }
                objHEEntities.SaveChanges();
            }
            catch (Exception)
            {

            }
            return dt;
        }

        #endregion
    }
}
