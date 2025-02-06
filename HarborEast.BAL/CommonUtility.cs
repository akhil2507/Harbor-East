using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Web;
using Microsoft.VisualBasic;
using System.Net;
using System.Data;
using System.Data.Objects.DataClasses;
using System.Net.NetworkInformation;
using NPOI.HSSF.UserModel;
using NPOI.HPSF;
using NPOI.POIFS.FileSystem;
using NPOI.SS.Util;
using NPOI.SS.UserModel;
using System.IO;

namespace HarborEast.BAL
{
    public class CommonUtility
    {
        public struct DateCount
        {
            public DateTime date;
            public long count;
            public long totalcount;
        };

        HSSFWorkbook hssfworkbook;
        List<DateCount> lstRedAssets = new List<DateCount>();

        #region Variable Declarations

        static Harbor_EastEntities objHEEntities = new Harbor_EastEntities();

        #endregion

        #region Public Static Methods

        /// <summary>
        /// This method is used to return the trading date for a particular symbol id.
        /// </summary>
        /// <param name="intAssetSymbolID">Asset Symbol ID.</param>
        /// <returns></returns>
        public static DateTime GetStartDate(int intAssetSymbolID)
        {
            try
            {
                return (objHEEntities.AssetPriceSet.Where(x => x.AssetSymbolSet.Id.Equals(intAssetSymbolID)).OrderBy(x => x.Date).First().Date);
            }
            catch (Exception)
            {
                return new DateTime(1, 1, 1);
            }
        }

        /// <summary>
        /// This function returns the Last trading date of a Symbol
        /// </summary>
        /// <param name="intAssetSymbolID"></param>
        /// <returns></returns>
        public static DateTime GetLastTradingDate(int intAssetSymbolID)
        {
            try
            {
                //return (objHEEntities.AssetPriceSet.Where(x => x.AssetSymbolSet.Id.Equals(intAssetSymbolID)).OrderBy(x => x.Date).First().Date);
                //return (objHEEntities.AssetSet.Where(x => x.AssetSymbolSet.Id.Equals(intAssetSymbolID)).OrderBy(x=>x.Ending_Date).Last().Ending_Date);
                return (objHEEntities.AssetSet.FirstOrDefault(x => x.AssetSymbolSet.Id.Equals(intAssetSymbolID)).Ending_Date);
            }
            catch (Exception)
            {
                return new DateTime(1, 1, 1);
            }
        }

        /// <summary>
        /// This method is used to return the last valid trading date for a particular Asset symbol Id.
        /// </summary>
        /// <param name="dtFinalStartDate">The date for which need to check whether entry is present or not.</param>
        /// <param name="intAssetSymbId">Asset symbol Id.</param>
        /// <returns></returns>
        public static DateTime GetPreviousDate(DateTime dtFinalStartDate, int intAssetSymbId)
        {
            DateTime dtPrev = DateTime.Now;
            try
            {
                while (true)
                {
                    if (objHEEntities.AssetPriceSet.FirstOrDefault(x => x.Date.Equals(dtFinalStartDate) && x.AssetSymbolSet.Id.Equals(intAssetSymbId)) != null)
                    {
                        return dtFinalStartDate;
                    }
                    else
                    {
                        dtFinalStartDate = DateAndTime.DateAdd(DateInterval.Day, -1, dtFinalStartDate);
                        GetPreviousDate(dtFinalStartDate, intAssetSymbId);
                    }
                }
            }
            catch (Exception)
            {

            }
            return dtPrev;
        }

        public static bool IsHistoricalProcessing()
        {
            bool IsHistorical = false;
            try
            {
                objHEEntities = new Harbor_EastEntities();
                int intUpdateInterval = objHEEntities.TrendEngineVariablesSet.Select(x => x.LoopUpdateInterval.Id).First();

                if (!IsHistorical)
                {
                    if (intUpdateInterval == 1)
                    {
                        IsHistorical = true;
                    }
                    else if (intUpdateInterval == 2)
                    {
                        if (DateTime.Today.DayOfWeek.Equals(DayOfWeek.Saturday))
                        {
                            IsHistorical = true;
                        }
                        else
                        {
                            IsHistorical = false;
                        }
                    }
                    else if (intUpdateInterval == 3)
                    {
                        if (DateTime.Today.Day.Equals(1))
                        {
                            IsHistorical = true;
                        }
                        else
                        {
                            IsHistorical = false;
                        }
                    }
                    else if (intUpdateInterval == 4)
                    {
                        bool boolIsHistorical = false;

                        if (DateTime.Today.Day.Equals(1))
                            if (DateTime.Today.Month == 1 || DateTime.Today.Month == 4 || DateTime.Today.Month == 7 || DateTime.Today.Month == 10)
                                boolIsHistorical = true;

                        if (boolIsHistorical)
                        {
                            IsHistorical = true;
                        }
                        else
                        {
                            IsHistorical = false;
                        }
                    }
                    else if (intUpdateInterval == 5)
                    {
                        if ((DateTime.Today.Month.Equals(1) && DateTime.Today.Day.Equals(1)))
                        {
                            IsHistorical = true;
                        }
                        else
                        {
                            IsHistorical = false;
                        }
                    }
                    else if (intUpdateInterval == 6)
                    {
                        IsHistorical = false;
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }
            return IsHistorical;
        }

        /// <summary>
        /// This method is used to return the array size for a particular asset symbol id from the AssetPriceSet table.
        /// </summary>
        /// <param name="intAssetSymbolId"></param>
        /// <returns></returns>
        public static int GetArraySize(int intAssetSymbolId)
        {
            try
            {
                return (objHEEntities.AssetPriceSet.Where(x => x.AssetSymbolSet.Id.Equals(intAssetSymbolId) && x.PriceTrend != null).Count());
            }
            catch (Exception)
            {
                return -1;
            }
        }

        public static string GetEmailID(int parUserID)
        {
            try
            {
                return (objHEEntities.PersonSet.FirstOrDefault(x => x.Id == parUserID).Email);
            }
            catch (Exception ex)
            {

                return "";
            }
            return "";
        }

        /// <summary>
        /// This method returns arraySize for Portfolio Calculation
        /// </summary>
        /// <param name="intAssetSymbolId"></param>
        /// <returns></returns>
        public static int GetArraySizeForPortfolioMethods(int intAssetSymbolId, DateTime dtFinalLastDate)
        {
            try
            {
                return (objHEEntities.AssetPriceSet.Where(x => x.AssetSymbolSet.Id.Equals(intAssetSymbolId) && x.PriceTrend != null && x.Date <= dtFinalLastDate).Count());
            }
            catch (Exception)
            {
                return -1;
            }
        }

        public static double Slope(double[] dblFirstParam, double[] dblSecondParam)
        {
            if (dblSecondParam.Length != dblFirstParam.Length)
            {
                throw new ArgumentException("The size of dependent array is not the same as the size of independent array");
            }
            double dblNum2 = 0.0;
            double dblNum3 = 0.0;
            int intIndex = 0;
            for (intIndex = 0; intIndex < dblSecondParam.Length; intIndex++)
            {
                dblNum2 += dblSecondParam[intIndex];
                dblNum3 += dblFirstParam[intIndex];
            }
            double dblNum5 = dblNum2 / ((double)dblSecondParam.Length);
            double dblNum6 = dblNum3 / ((double)dblFirstParam.Length);
            double dblNum7 = 0.0;
            double dblNum8 = 0.0;
            for (intIndex = 0; intIndex < dblSecondParam.Length; intIndex++)
            {
                dblNum7 += (dblSecondParam[intIndex] - dblNum5) * (dblFirstParam[intIndex] - dblNum6);
                dblNum8 += Math.Pow(dblSecondParam[intIndex] - dblNum5, 2.0);
            }
            return (dblNum7 / dblNum8);
        }

        public static double STDev(double[] dblData)
        {
            return Math.Sqrt(VAR(dblData));
        }

        public static double VAR(double[] dblData)
        {
            long lngNum = 0L;
            double dblNum2 = 0.0;
            foreach (double num3 in dblData)
            {
                lngNum += 1L;
                dblNum2 += num3;
            }
            double dblNum4 = dblNum2 / ((double)lngNum);
            double dblNum5 = 0.0;
            foreach (double num6 in dblData)
            {
                dblNum5 += (num6 - dblNum4) * (num6 - dblNum4);
            }
            return (dblNum5 / ((double)(lngNum - 1L)));
        }

        public static double FORECAST(double x, double[] known_y, double[] known_x)
        {
            if (known_x.Length != known_y.Length)
            {
                throw new ArgumentException("The size of dependent array is not the same as the size of independent array");
            }
            double num3 = 0.0;
            double num4 = 0.0;
            int index = 0;
            for (index = 0; index < known_x.Length; index++)
            {
                num3 += known_x[index];
                num4 += known_y[index];
            }
            double num6 = num3 / ((double)known_x.Length);
            double num7 = num4 / ((double)known_y.Length);
            double num8 = 0.0;
            double num9 = 0.0;
            for (index = 0; index < known_x.Length; index++)
            {
                num8 += (known_x[index] - num6) * (known_y[index] - num7);
                num9 += Math.Pow(known_x[index] - num6, 2.0);
            }
            double num2 = num8 / num9;
            double num = num7 - (num2 * num6);
            return (num + (num2 * x));
        }

        /// <summary>
        /// This private method is used to get the sum the portfolio value.
        /// </summary>
        /// <param name="lstAssetAllocation">collection of AssetAllocation class.</param>
        /// <param name="intIndex">Array index.</param>
        /// <returns>returns the total.</returns>
        public static double GetBHPortfolioTotalValue(List<AssetAllocation> lstAssetAllocation, int intIndex)
        {
            try
            {
                double dblSum = 0;
                foreach (var objSet in lstAssetAllocation)
                {
                    dblSum = dblSum + objSet.MAssetValue[intIndex];
                }
                return dblSum;
            }
            catch (Exception)
            {
                return 0;
            }
        }
        /// <summary>
        /// Calculates the Growth values without using Excel Growth function
        /// </summary>
        /// <param name="arrDates"></param>
        /// <param name="arrPrices"></param>
        /// <returns></returns>
        public static double[] Growth(double[] arrDates, double[] arrPrices, int symbolID)
        {
            try
            {
                Harbor_EastEntities objHEEntities = new Harbor_EastEntities();
                double[] arrGrowthVals = new double[arrPrices.Length];
                double[] arrLnPrice = new double[arrPrices.Length];
                double[] arrDayCountMulPrice = new double[arrPrices.Length];
                double[] arrSquareDayCount = new double[arrPrices.Length];
                int[] arrDayCount = new int[arrPrices.Length];


                double dblM = 0;
                double dblA = 0;
                double dblSumLnPrice = 0;
                double dblSumDayCountMulPrice = 0;
                double dblSumSquareDayCount = 0;
                int intSumDayCount = 0;

                for (int index = 0; index < arrPrices.Length; index++)
                {
                    arrDayCount[index] = index + 1;
                    arrLnPrice[index] = Math.Log(arrPrices[index]);
                    arrDayCountMulPrice[index] = (arrDayCount[index]) * arrLnPrice[index];
                    arrSquareDayCount[index] = (arrDayCount[index]) * (arrDayCount[index]);
                }
                dblSumLnPrice = arrLnPrice.Sum();
                dblSumSquareDayCount = arrSquareDayCount.Sum();
                dblSumDayCountMulPrice = arrDayCountMulPrice.Sum();
                intSumDayCount = arrDayCount.Sum();

                dblM = (arrDayCount[arrDayCount.Length - 1] * dblSumDayCountMulPrice - (intSumDayCount * dblSumLnPrice)) / (arrDayCount[arrDayCount.Length - 1] * dblSumSquareDayCount - Math.Pow(intSumDayCount, 2));
                dblA = Math.Exp((dblSumLnPrice - (dblM * intSumDayCount)) / arrDayCount[arrDayCount.Length - 1]);

                //** growth reduction logic
                objHEEntities.AssetSet.First(x => x.AssetSymbolSet.Id.Equals(symbolID)).M = (float)dblM;
                objHEEntities.AssetSet.First(x => x.AssetSymbolSet.Id.Equals(symbolID)).A = (float)dblA;
                objHEEntities.AssetSet.First(x => x.AssetSymbolSet.Id.Equals(symbolID)).LoopUpdateDate = DateTime.Today.Date;
                objHEEntities.SaveChanges();
                //** 
                for (int index = 0; index < arrPrices.Length; index++)
                {
                    arrGrowthVals[index] = dblA * Math.Exp(dblM * arrDayCount[index]);
                }

                return arrGrowthVals;
            }
            catch (Exception ex)
            {
                return null;
            }

        }

        /// <summary>
        /// This method is used to get the datetime instance from the string.
        /// </summary>
        /// <param name="parStrDate">date in the string format.</param>
        /// <returns>datetime instance from the string.</returns>
        public static DateTime GenerateDateFromInputString(string parStrDate)
        {
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
            catch (Exception)
            {
                return new DateTime(1, 1, 1);
            }
        }

        /// <summary>
        /// This method is used to get the earliest trading date for a particular asset symbol.
        /// </summary>
        /// <param name="parSymbol">Asset symbol in the string format.</param>
        /// <returns>returns the earliest trading date for the asset symbol passed as a parameter.</returns>
        public static DateTime GetEarliestTradingDate(string parSymbol)
        {
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
                        return GenerateDateFromInputString(strCommaList[0]);
                    }
                }
                return new DateTime(1, 1, 1);
            }
            catch (Exception)
            {
                return new DateTime(1, 1, 1);
            }
        }

        private bool DeleteExistingPortfolio(string portfolioName, bool isDelete)
        {
            //System.Diagnostics.Debugger.Break();
            bool isDeleteSuccessfull = false;
            Harbor_EastEntities entities = new Harbor_EastEntities();
            try
            {
                int portfolioID = entities.PortfolioSet.FirstOrDefault(x => x.Portfolio_Name.Trim().Equals(portfolioName.Trim())).Id;


                var extContentSet = entities.PortfolioContentSet.Where(x => x.PortfolioSet.Id.Equals(portfolioID));
                foreach (var set in extContentSet)
                {
                    entities.DeleteObject(set);
                }


                var extMethodSet = entities.PortfolioMethodSet.Where(x => x.PortfolioSet.Id.Equals(portfolioID));
                foreach (var set in extMethodSet)
                {
                    entities.DeleteObject(set);
                }

                var extAllocationInfo = entities.PorfolioAllocationInfo.Where(x => x.PortfolioSet.Id.Equals(portfolioID));
                foreach (var set in extAllocationInfo)
                {
                    entities.DeleteObject(set);
                }

                var extRotationEnginePair = entities.RotationEnginePair.Where(x => x.PortfolioSet.Id.Equals(portfolioID));
                foreach (var set in extRotationEnginePair)
                {
                    entities.DeleteObject(set);
                }

                var extRotationMatrix = entities.RotationMatrixData.Where(x => x.PortfolioSet.Id.Equals(portfolioID));
                foreach (var set in extRotationMatrix)
                {
                    entities.DeleteObject(set);
                }

                var extAlertShare = entities.AlertShare.Where(x => x.PortfolioSet.Id.Equals(portfolioID));
                foreach (var set in extAlertShare)
                {
                    entities.DeleteObject(set);
                }

                var extMRR = entities.MostRecentRebalances.Where(x => x.PortfolioSet.Id.Equals(portfolioID));
                foreach (var set in extMRR)
                {
                    entities.DeleteObject(set);
                }

                var extPVS = entities.PortfolioValueSet.Where(x => x.PortfolioSet.Id.Equals(portfolioID));
                foreach (var set in extPVS)
                {
                    entities.DeleteObject(set);
                }

                if (isDelete)
                {
                    var extPF = entities.PortfolioSet.FirstOrDefault(x => x.Id.Equals(portfolioID));
                    entities.DeleteObject(extPF);
                }
                entities.SaveChanges();
                entities.Connection.Close();
                isDeleteSuccessfull = true;

            }
            catch (Exception)
            {
                return false;
            }
            return isDeleteSuccessfull;
        }


        public bool ExportRedAssets2Excel(string path)
        {

            //System.Diagnostics.Debugger.Break();
            hssfworkbook = new HSSFWorkbook();
            try
            {
                DateTime dtCurrent = new DateTime(2007, 1, 1);
                DateTime dtCurrentMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

                while (dtCurrent <= dtCurrentMonth)
                {
                    long lngCountRed = objHEEntities.AssetPriceSet.Count(x => x.State.ToLower() == "out" && x.FilterState.ToLower() == "out" && x.Date == dtCurrent);
                    long lngCountTotal = objHEEntities.AssetPriceSet.Count(x => x.Date == dtCurrent);
                    if (lngCountRed == 0)
                    {
                        dtCurrent = dtCurrent.AddDays(1);
                        lngCountRed = objHEEntities.AssetPriceSet.Count(x => x.State.ToLower() == "out" && x.FilterState.ToLower() == "out" && x.Date == dtCurrent);
                        lngCountTotal = objHEEntities.AssetPriceSet.Count(x => x.Date == dtCurrent);
                        if (lngCountRed == 0)
                        {
                            dtCurrent = dtCurrent.AddDays(1);
                            lngCountRed = objHEEntities.AssetPriceSet.Count(x => x.State.ToLower() == "out" && x.FilterState.ToLower() == "out" && x.Date == dtCurrent);
                            lngCountTotal = objHEEntities.AssetPriceSet.Count(x => x.Date == dtCurrent);
                        }
                    }
                    DateCount objDtc = new DateCount();
                    objDtc.date = dtCurrent;
                    objDtc.count = lngCountRed;
                    objDtc.totalcount = lngCountTotal;
                    lstRedAssets.Add(objDtc);

                    dtCurrent = dtCurrent.AddMonths(1);
                    if (dtCurrent.Day.Equals(2))
                    {
                        dtCurrent = dtCurrent.AddDays(-1);
                    }
                    if (dtCurrent.Day.Equals(3))
                    {
                        dtCurrent = dtCurrent.AddDays(-2);
                    }
                }

                InitializeWorkbook();
                Sheet sheet = hssfworkbook.CreateSheet("Sheet A");
                CreateCellArray(sheet);
                StreamWriter objMs = new StreamWriter(path);
                hssfworkbook.Write(objMs.BaseStream);
                objMs.Close();
                return true;


            }
            catch (Exception ex)
            {

                throw;
            }
            return false;

        }


        //void WriteToFile()
        //{
        //    //Write the stream data of workbook to the root directory
        //    //saveExcel.FileName;
        //    //FileStream file = new FileStream(@"c:\t1.xls", FileMode.Create);
        //    FileStream file = new FileStream(saveExcel.FileName, FileMode.Create);
        //    hssfworkbook.Write(file);
        //    file.Close();
        //}

        void InitializeWorkbook()
        {
            hssfworkbook = new HSSFWorkbook();

            //create a entry of DocumentSummaryInformation
            DocumentSummaryInformation dsi = PropertySetFactory.CreateDocumentSummaryInformation();
            dsi.Company = "InvestEngines.com";
            hssfworkbook.DocumentSummaryInformation = dsi;

            //create a entry of SummaryInformation
            SummaryInformation si = PropertySetFactory.CreateSummaryInformation();
            si.Subject = "SQL Query Output";
            hssfworkbook.SummaryInformation = si;
        }


        void CreateCellArray(Sheet sheet)
        {

            int intRow = 0;
            Row row = sheet.CreateRow(intRow);
            Cell cell = row.CreateCell(0);
            cell.SetCellValue("Date");
            cell = row.CreateCell(1);
            cell.SetCellValue("%Red Assets");
            for (int intI = 1; intI <= lstRedAssets.Count; intI++)
            {
                row = sheet.CreateRow(intI);
                cell = row.CreateCell(0);
                cell.SetCellValue(lstRedAssets[intI - 1].date);
                cell = row.CreateCell(1);
                cell.SetCellValue((double)(lstRedAssets[intI - 1].totalcount == 0 ? 0 : (lstRedAssets[intI - 1].count * 100 / lstRedAssets[intI - 1].totalcount)));
            }




        }

        /// <summary>
        /// Check portfolio count for an asset
        /// </summary>
        /// <param name="intSymbolID"></param>
        /// <returns></returns>
        public List<string> checkPortfolioAssetCount(int intSymbolID)
        {
            List<string> pfNames = new List<string>();

            int[] portfolioIDs = objHEEntities.PortfolioContentSet.Where(x => x.AssetSymbolSet.Id.Equals(intSymbolID)).Select(x => x.PortfolioSet.Id).Distinct().ToArray();
            foreach (int intA in portfolioIDs)
            {
                int portfolioAssetCount = objHEEntities.PortfolioContentSet.Where(x => x.PortfolioSet.Id.Equals(intA)).Count();
                if (portfolioAssetCount == 2)
                {
                    string portfolioName = objHEEntities.PortfolioSet.FirstOrDefault(x => x.Id.Equals(intA)).Portfolio_Name;
                    pfNames.Add(portfolioName);
                }
            }


            return pfNames;
        }
        /// <summary>
        /// Delete an Asset with all references from database
        /// </summary>
        /// <param name="symbolName"></param>
        /// <param name="intSymbolID"></param>
        /// <returns></returns>
        public bool DeleteAsset(string symbolName, int intSymbolID)
        {
            try
            {
                //List<PortfolioContentSet> lstPortfolioContent;
                int[] portfolioIDs = objHEEntities.PortfolioContentSet.Where(x => x.AssetSymbolSet.Id.Equals(intSymbolID)).Select(x => x.PortfolioSet.Id).Distinct().ToArray();
                List<string> pfNames = checkPortfolioAssetCount(intSymbolID);
                if (pfNames != null && pfNames.Count != 0)
                {
                    foreach (string strPfName in pfNames)
                        DeleteExistingPortfolio(strPfName, true);
                }

                //Delete entry from AlertShare Table
                List<AlertShare> lstAlertShare = objHEEntities.AlertShare.Where(x => x.AssetSymbolSet.Id.Equals(intSymbolID)).ToList();
                foreach (AlertShare objAlertShare in lstAlertShare)
                {
                    objHEEntities.DeleteObject(objAlertShare);
                }

                //Delete entry from MostRecentRebalace Table
                List<MostRecentRebalances> lstMostRecentRebalances = objHEEntities.MostRecentRebalances.Where(x => x.AssetSymbolSet.Id.Equals(intSymbolID)).ToList();
                foreach (MostRecentRebalances objMostRecentRebalances in lstMostRecentRebalances)
                {
                    objHEEntities.DeleteObject(objMostRecentRebalances);
                }

                //Delete entry from AlertSymbolState Table
                AlertSymbolState objAlertSymbolState = objHEEntities.AlertSymbolState.FirstOrDefault(x => x.AssetSymbolSet.Id.Equals(intSymbolID));
                if (objAlertSymbolState != null)
                    objHEEntities.DeleteObject(objAlertSymbolState);

                //Delete entry from AssetMonitoring Table
                List<AssetMonitoring> lstAssetMonitoring = objHEEntities.AssetMonitoring.Where(x => x.Symbol.Equals(symbolName)).ToList();
                foreach (AssetMonitoring objAssetMonitoring in lstAssetMonitoring)
                {
                    objHEEntities.DeleteObject(objAssetMonitoring);
                }

                //Delete entry from AssetBackTestSet Table
                List<AssetBackTestSet> lstAssetBackTestSet = objHEEntities.AssetBackTestSet.Where(x => x.AssetSymbolSet.Id.Equals(intSymbolID)).ToList();
                foreach (AssetBackTestSet objAssetBackTestSet in lstAssetBackTestSet)
                {
                    objHEEntities.DeleteObject(objAssetBackTestSet);
                }

                //Delete entry from AssetPriceBackTestSet Table
                List<AssetPriceBackTestSet> lstAssetPriceBackTestSet = objHEEntities.AssetPriceBackTestSet.Where(x => x.AssetSymbolSet.Id.Equals(intSymbolID)).ToList();
                foreach (AssetPriceBackTestSet objAssetPriceBackTestSet in lstAssetPriceBackTestSet)
                {
                    objHEEntities.DeleteObject(objAssetPriceBackTestSet);
                }

                //Delete entry from AssetPriceSet Table
                List<AssetPriceSet> lstAssetPriceSet = objHEEntities.AssetPriceSet.Where(x => x.AssetSymbolSet.Id.Equals(intSymbolID)).ToList();
                foreach (AssetPriceSet objAssetPriceSet in lstAssetPriceSet)
                {
                    objHEEntities.DeleteObject(objAssetPriceSet);
                }

                //Delete entry from AssetSet Table
                List<AssetSet> lstAssetSet = objHEEntities.AssetSet.Where(x => x.AssetSymbolSet.Id.Equals(intSymbolID)).ToList();
                foreach (AssetSet objAssetSet in lstAssetSet)
                {
                    objHEEntities.DeleteObject(objAssetSet);
                }

                //Delete entry from PorfolioAllocationInfo Table
                List<PorfolioAllocationInfo> lstPorfolioAllocationInfo = objHEEntities.PorfolioAllocationInfo.Where(x => x.AssetSymbolSet.Id.Equals(intSymbolID)).ToList();
                foreach (PorfolioAllocationInfo objPorfolioAllocationInfo in lstPorfolioAllocationInfo)
                {
                    objHEEntities.DeleteObject(objPorfolioAllocationInfo);
                }

                //Delete entry from PortfolioContentSet Table
                List<PortfolioContentSet> lstPortfolioContentSet = objHEEntities.PortfolioContentSet.Where(x => x.AssetSymbolSet.Id.Equals(intSymbolID)).ToList();
                foreach (PortfolioContentSet objPortfolioContentSet in lstPortfolioContentSet)
                {
                    objHEEntities.DeleteObject(objPortfolioContentSet);
                }


                //Delete entry from AssetSymbolSet Table
                AssetSymbolSet objAssetSymbolSet = objHEEntities.AssetSymbolSet.FirstOrDefault(x => x.Id.Equals(intSymbolID));
                if (objAssetSymbolSet != null)
                    objHEEntities.DeleteObject(objAssetSymbolSet);

                // save changes
                objHEEntities.SaveChanges();




                //Run Allocation engine for all methods for portfolios which contained this asset
                DataTable dt = null;


                if (portfolioIDs != null && portfolioIDs.Count() > 0)
                {
                    foreach (int id in portfolioIDs)
                    {
                        dt = new DataTable();
                        clsBuyAndHold objBnH = new clsBuyAndHold();
                        dt = objBnH.PerformBuyAndHoldOperation(0, id.ToString(), false);

                        dt = new DataTable();
                        clsBuyAndHoldTrend objBnHTrend = new clsBuyAndHoldTrend();
                        dt = objBnHTrend.PerformBuyAndHoldTrendOperation(0, id.ToString(), false);


                        dt = new DataTable();
                        clsRebalance objRebalance = new clsRebalance();
                        dt = objRebalance.PerformRebalanceOperation(0, id.ToString(), false);


                        dt = new DataTable();
                        clsRebalanceTrend objRebalanceTrend = new clsRebalanceTrend();
                        dt = objRebalanceTrend.PerformRebalanceTrend(0, id.ToString(), false);

                    }
                }

                //Run rotation Engine for all portfolios that contained this deleted asset

                if (portfolioIDs != null && portfolioIDs.Count() > 0)
                {
                    foreach (int id in portfolioIDs)
                    {

                        clsRotationEngine objRotationEng = new clsRotationEngine();
                        objRotationEng.PerformRotationEngineRanking(0, id.ToString(), false, false, null, null, null);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }

        }

        public static bool IsDividendOrSplitForPortfolio(List<PortfolioContentSet> lstPortfolioContentSet)
        {
            bool isDivOrSplit = false;
            foreach (PortfolioContentSet objPFContentSet in lstPortfolioContentSet)
            {
                int intSymbolID = Convert.ToInt32(((EntityReference)(objPFContentSet.AssetSymbolSetReference)).EntityKey.EntityKeyValues[0].Value);
                int intDividendSplit = (int)objHEEntities.AssetSet.First(x => x.AssetSymbolSet.Id.Equals(intSymbolID)).DividentSplit;
                if (intDividendSplit == 1)
                {
                    isDivOrSplit = true;
                    break;
                }
            }
            return isDivOrSplit;
        }

        public static bool isConnectionAvailable()
        {
            bool _success = false;
            //build a list of sites to ping, you can use your own
            string[] sitesList = { "www.google.com", "http://finance.yahoo.com", "www.yahoo.com" };
            //create an instance of the System.Net.NetworkInformation Namespace
            Ping ping = new Ping();
            //Create an instance of the PingReply object from the same Namespace
            PingReply reply;
            //int variable to hold # of pings not successful
            int notReturned = 0;
            try
            {
                //start a loop that is the lentgh of th string array we
                //created above
                for (int i = 0; i < sitesList.Length; i++)
                {
                    //use the Send Method of the Ping object to send the
                    //Ping request
                    reply = ping.Send(sitesList[i], 10);
                    //now we check the status, looking for,
                    //of course a Success status
                    if (reply.Status != IPStatus.Success)
                    {
                        //now valid ping so increment
                        notReturned += 1;
                    }
                    //check to see if any pings came back
                    if (notReturned == sitesList.Length)
                    {
                        _success = false;
                        //comment this back in if you have your own excerption
                        //library you use for you applications (use you own
                        //exception names)
                        //throw new ConnectivityNotFoundException(@"There doest seem to be a network/internet connection.\r\n
                        //Please contact your system administrator");
                        //use this is if you don't your own custom exception library
                        throw new Exception(@"There doest seem to be a network/internet connection.\r\n
                                                    Please contact your system administrator");
                    }
                    else
                    {
                        _success = true;
                    }
                }
            }
            //comment this back in if you have your own excerption
            //library you use for you applications (use you own
            //exception names)
            //catch (ConnectivityNotFoundException ex)
            //use this line if you don't have your own custom exception
            //library
            catch (Exception ex)
            {
                _success = false;
                Console.WriteLine(ex.Message);
            }
            Console.WriteLine(_success.ToString());


            return _success;
        }


        public static bool CalculateMonthlyDetails()
        {
            try
            {
                objHEEntities = new Harbor_EastEntities();
                if (DateTime.Now.Day.Equals(1))
                {
                    DateTime currentDate = objHEEntities.AssetSet.First().Ending_Date;
                    Monthly_Statistics objMonthlyStat = new Monthly_Statistics();
                    objMonthlyStat.Date = DateTime.Now.Date;
                    //To find total number of Users
                    objMonthlyStat.Total_Users = objHEEntities.PersonSet.Count();
                    //To find Level One Users
                    objMonthlyStat.Level_1_Total_Users = objHEEntities.PersonSet.Where(x => x.UserLevels.ID.Equals(1)).Count();
                    //To find Level Two Users
                    objMonthlyStat.Level_2_Total_Users = objHEEntities.PersonSet.Where(x => x.UserLevels.ID.Equals(2)).Count();

                    //To find Level Three Users
                    objMonthlyStat.Level_3_Total_Users = objHEEntities.PersonSet.Where(x => x.UserLevels.ID.Equals(3)).Count();
                    objMonthlyStat.Total_Users_Log_On = objHEEntities.PersonSet.Sum(x => x.Total_Logins);
                    objMonthlyStat.Total_Assets = objHEEntities.AssetSet.Count();
                    objMonthlyStat.Percentage_of_Red_Assets = ((float)objHEEntities.AssetSet.Where(x => x.StateColor.Equals("R")).Count() / (float)objHEEntities.AssetSet.Count()) * 100.00;
                    objMonthlyStat.Percentage_of_Assets_TGPY_gt_GPY = ((float)objHEEntities.AssetSet.Where(x => x.CAGRTrend > x.CAGR).Count() / (float)objHEEntities.AssetSet.Count()) * 100.00;
                    objMonthlyStat.Total_Portfolios = objHEEntities.PortfolioSet.Count();
                    objMonthlyStat.Percentage_of_Portfolios_GTREB_gt_or_eq_REB = ((float)objHEEntities.PortfolioMethodSet.Where(x => x.CAGR_RebTrend >= x.CAGR_Reb).Count() / (float)objHEEntities.PortfolioMethodSet.Count()) * 100.00;
                    objMonthlyStat.Percentage_of_Portfolios_GRREB_gt_or_eq_TREB = ((float)objHEEntities.PortfolioMethodSet.Where(x => x.CAGR_RebRotation >= x.CAGR_RebTrend).Count() / (float)objHEEntities.PortfolioMethodSet.Count()) * 100.00;
                    string[] arrSymbol = objHEEntities.AssetSet.Select(x => x.AssetSymbol).ToArray();

                    //To insert value for the Asset past 12 month trend gain par year greater than gain par year
                    if (currentDate.AddYears(-1).DayOfWeek.Equals(DayOfWeek.Saturday))
                    {
                        DateTime lastYearDate = currentDate.AddYears(-1).AddDays(2);
                        objMonthlyStat.Percentage_of_Assets_P12M_TGPY_gt_GPY = ((float)CommonUtility.GetAnnualizedGain(lastYearDate, currentDate, arrSymbol) / (float)objHEEntities.AssetSet.Count()) * 100;
                        objMonthlyStat.Percentage_of_Portfolios_L12M_GTREB_gt_or_eq_REB = ((float)CommonUtility.GetPortfolioGreaterThan(lastYearDate, currentDate, "TREB", "REB") / (float)objHEEntities.PortfolioMethodSet.Count()) * 100;
                        objMonthlyStat.Percentage_of_Portfolios_L12M_GRREB_gt_or_eq_TREB = ((float)CommonUtility.GetPortfolioGreaterThan(lastYearDate, currentDate, "RREB", "TREB") / (float)objHEEntities.PortfolioMethodSet.Count()) * 100;
                    }
                    else
                    {
                        if (currentDate.AddYears(-1).DayOfWeek.Equals(DayOfWeek.Sunday))
                        {
                            DateTime lastYearDate = currentDate.AddYears(-1).AddDays(1);
                            objMonthlyStat.Percentage_of_Assets_P12M_TGPY_gt_GPY = ((float)CommonUtility.GetAnnualizedGain(lastYearDate, currentDate, arrSymbol) / (float)objHEEntities.AssetSet.Count()) * 100;
                            objMonthlyStat.Percentage_of_Portfolios_L12M_GTREB_gt_or_eq_REB = ((float)CommonUtility.GetPortfolioGreaterThan(lastYearDate, currentDate, "TREB", "REB") / (float)objHEEntities.PortfolioMethodSet.Count()) * 100;
                            objMonthlyStat.Percentage_of_Portfolios_L12M_GRREB_gt_or_eq_TREB = ((float)CommonUtility.GetPortfolioGreaterThan(lastYearDate, currentDate, "RREB", "TREB") / (float)objHEEntities.PortfolioMethodSet.Count()) * 100;
                        }
                        else
                        {
                            DateTime lastYearDate = currentDate.AddYears(-1);
                            objMonthlyStat.Percentage_of_Assets_P12M_TGPY_gt_GPY = ((float)CommonUtility.GetAnnualizedGain(lastYearDate, currentDate, arrSymbol) / (float)objHEEntities.AssetSet.Count()) * 100;
                            objMonthlyStat.Percentage_of_Portfolios_L12M_GTREB_gt_or_eq_REB = ((float)CommonUtility.GetPortfolioGreaterThan(lastYearDate, currentDate, "TREB", "REB") / (float)objHEEntities.PortfolioMethodSet.Count()) * 100;
                            objMonthlyStat.Percentage_of_Portfolios_L12M_GRREB_gt_or_eq_TREB = ((float)CommonUtility.GetPortfolioGreaterThan(lastYearDate, currentDate, "RREB", "TREB") / (float)objHEEntities.PortfolioMethodSet.Count()) * 100;
                        }
                    }


                    objHEEntities.AddToMonthly_Statistics(objMonthlyStat);
                    List<PersonSet> lstPersonSet = objHEEntities.PersonSet.ToList<PersonSet>();
                    foreach (PersonSet objPersonSet in lstPersonSet)
                    {
                        objPersonSet.Total_Logins = 0;
                    }
                    objHEEntities.SaveChanges();
                    return true;
                    //    }
                    //    }
                }
                else
                {
                    if (objHEEntities.Monthly_Statistics.Count() == 0)
                    {
                        DateTime currentDate = objHEEntities.AssetSet.First().Ending_Date;
                        Monthly_Statistics objMonthlyStat = new Monthly_Statistics();
                        objMonthlyStat.Date = DateTime.Now.Date;
                        //To find total number of Users
                        objMonthlyStat.Total_Users = objHEEntities.PersonSet.Count();
                        //To find Level One Users
                        objMonthlyStat.Level_1_Total_Users = objHEEntities.PersonSet.Where(x => x.UserLevels.ID.Equals(1)).Count();
                        //To find Level Two Users
                        objMonthlyStat.Level_2_Total_Users = objHEEntities.PersonSet.Where(x => x.UserLevels.ID.Equals(2)).Count();

                        //To find Level Three Users
                        objMonthlyStat.Level_3_Total_Users = objHEEntities.PersonSet.Where(x => x.UserLevels.ID.Equals(3)).Count();
                        objMonthlyStat.Total_Users_Log_On = objHEEntities.PersonSet.Sum(x => x.Total_Logins);
                        objMonthlyStat.Total_Assets = objHEEntities.AssetSet.Count();
                        objMonthlyStat.Percentage_of_Red_Assets = ((float)objHEEntities.AssetSet.Where(x => x.StateColor.Equals("R")).Count() / (float)objHEEntities.AssetSet.Count()) * 100.00;
                        objMonthlyStat.Percentage_of_Assets_TGPY_gt_GPY = ((float)objHEEntities.AssetSet.Where(x => x.CAGRTrend > x.CAGR).Count() / (float)objHEEntities.AssetSet.Count()) * 100.00;
                        objMonthlyStat.Total_Portfolios = objHEEntities.PortfolioSet.Count();
                        objMonthlyStat.Percentage_of_Portfolios_GTREB_gt_or_eq_REB = ((float)objHEEntities.PortfolioMethodSet.Where(x => x.CAGR_RebTrend >= x.CAGR_Reb).Count() / (float)objHEEntities.PortfolioMethodSet.Count()) * 100.00;
                        objMonthlyStat.Percentage_of_Portfolios_GRREB_gt_or_eq_TREB = ((float)objHEEntities.PortfolioMethodSet.Where(x => x.CAGR_RebRotation >= x.CAGR_RebTrend).Count() / (float)objHEEntities.PortfolioMethodSet.Count()) * 100.00;
                        string[] arrSymbol = objHEEntities.AssetSet.Select(x => x.AssetSymbol).ToArray();

                        //To insert value for the Asset past 12 month trend gain par year greater than gain par year
                        if (currentDate.AddYears(-1).DayOfWeek.Equals(DayOfWeek.Saturday))
                        {
                            DateTime lastYearDate = currentDate.AddYears(-1).AddDays(2);
                            objMonthlyStat.Percentage_of_Assets_P12M_TGPY_gt_GPY = ((float)CommonUtility.GetAnnualizedGain(lastYearDate, currentDate, arrSymbol) / (float)objHEEntities.AssetSet.Count()) * 100;
                            objMonthlyStat.Percentage_of_Portfolios_L12M_GTREB_gt_or_eq_REB = ((float)CommonUtility.GetPortfolioGreaterThan(lastYearDate, currentDate, "TREB", "REB") / (float)objHEEntities.PortfolioMethodSet.Count()) * 100;
                            objMonthlyStat.Percentage_of_Portfolios_L12M_GRREB_gt_or_eq_TREB = ((float)CommonUtility.GetPortfolioGreaterThan(lastYearDate, currentDate, "RREB", "TREB") / (float)objHEEntities.PortfolioMethodSet.Count()) * 100;
                        }
                        else
                        {
                            if (currentDate.AddYears(-1).DayOfWeek.Equals(DayOfWeek.Sunday))
                            {
                                DateTime lastYearDate = currentDate.AddYears(-1).AddDays(1);
                                objMonthlyStat.Percentage_of_Assets_P12M_TGPY_gt_GPY = ((float)CommonUtility.GetAnnualizedGain(lastYearDate, currentDate, arrSymbol) / (float)objHEEntities.AssetSet.Count()) * 100;
                                objMonthlyStat.Percentage_of_Portfolios_L12M_GTREB_gt_or_eq_REB = ((float)CommonUtility.GetPortfolioGreaterThan(lastYearDate, currentDate, "TREB", "REB") / (float)objHEEntities.PortfolioMethodSet.Count()) * 100;
                                objMonthlyStat.Percentage_of_Portfolios_L12M_GRREB_gt_or_eq_TREB = ((float)CommonUtility.GetPortfolioGreaterThan(lastYearDate, currentDate, "RREB", "TREB") / (float)objHEEntities.PortfolioMethodSet.Count()) * 100;
                            }
                            else
                            {
                                DateTime lastYearDate = currentDate.AddYears(-1);
                                objMonthlyStat.Percentage_of_Assets_P12M_TGPY_gt_GPY = ((float)CommonUtility.GetAnnualizedGain(lastYearDate, currentDate, arrSymbol) / (float)objHEEntities.AssetSet.Count()) * 100;
                                objMonthlyStat.Percentage_of_Portfolios_L12M_GTREB_gt_or_eq_REB = ((float)CommonUtility.GetPortfolioGreaterThan(lastYearDate, currentDate, "TREB", "REB") / (float)objHEEntities.PortfolioMethodSet.Count()) * 100;
                                objMonthlyStat.Percentage_of_Portfolios_L12M_GRREB_gt_or_eq_TREB = ((float)CommonUtility.GetPortfolioGreaterThan(lastYearDate, currentDate, "RREB", "TREB") / (float)objHEEntities.PortfolioMethodSet.Count()) * 100;
                            }
                        }


                        objHEEntities.AddToMonthly_Statistics(objMonthlyStat);
                        List<PersonSet> lstPersonSet = objHEEntities.PersonSet.ToList<PersonSet>();
                        foreach (PersonSet objPersonSet in lstPersonSet)
                        {
                            objPersonSet.Total_Logins = 0;
                        }
                        objHEEntities.SaveChanges();

                        return true;

                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        /// <summary>
        /// This function finds duplicate row the data that fetched from yahoo and removes it
        /// </summary>
        /// <param name="strRowLevelSplitCollection"></param>
        /// <returns></returns>
        public static string[] ValidatestrRowLevelSplitCollection(string[] strRowLevelSplitCollection)
        {
            string[] strFirstRowColumnLevelSplitCollection = strRowLevelSplitCollection[1].Split(",".ToCharArray());
            string[] strSecondRowColumnLevelSplitCollection = strRowLevelSplitCollection[2].Split(",".ToCharArray());
            string[] strNewRowColumnLevelSplitCollection=new string[strRowLevelSplitCollection.Length];

            DateTime FirstRowDate = Convert.ToDateTime(strFirstRowColumnLevelSplitCollection[0]);
            DateTime SecondRowDate = Convert.ToDateTime(strSecondRowColumnLevelSplitCollection[0]);
                   

            if (FirstRowDate.Equals(SecondRowDate))
            {
                strNewRowColumnLevelSplitCollection[0] = strRowLevelSplitCollection[0];

                for (int intTemp = 2; intTemp < strRowLevelSplitCollection.Length; intTemp++)
                {                    
                    strNewRowColumnLevelSplitCollection[intTemp-1] = strRowLevelSplitCollection[intTemp];
                }
            }
            else
            {
                strNewRowColumnLevelSplitCollection = strRowLevelSplitCollection;
            }

            return strNewRowColumnLevelSplitCollection;
        }

        private static float GetAnnualizedGain(DateTime fromDate, DateTime toDate, string[] arrSymbol)
        {
            float greaterTrendGain = 0;
            try
            {

                foreach (string strSymbol in arrSymbol)
                {

                    double dblToPrice, dblFromPrice, dblToPriceTrend, dblFromPriceTrend;

                    Harbor_EastEntities entities = new Harbor_EastEntities();
                    int intAssetSymboID = entities.AssetSymbolSet.FirstOrDefault(x => x.Symbol.Trim().ToLower().Equals(strSymbol.Trim().ToLower())).Id;
                    AssetPriceSet objFromAssetPriceSet = entities.AssetPriceSet.FirstOrDefault(x => x.Date.Equals(fromDate) && x.AssetSymbolSet.Id.Equals(intAssetSymboID));
                    dblFromPrice = (double)objFromAssetPriceSet.Price;
                    dblFromPriceTrend = (double)objFromAssetPriceSet.PriceTrend;

                    AssetPriceSet objToAssetPriceSet = entities.AssetPriceSet.FirstOrDefault(x => x.Date.Equals(toDate) && x.AssetSymbolSet.Id.Equals(intAssetSymboID));
                    dblToPrice = (double)objToAssetPriceSet.Price;
                    dblToPriceTrend = (double)objToAssetPriceSet.PriceTrend;

                    TimeSpan dateDiff = toDate - fromDate;
                    double yearDiff = (double)dateDiff.Days / 365;
                    double dblBHAnnualizedGain = Math.Pow((dblToPrice / dblFromPrice), (1.00 / yearDiff)) - 1;
                    double dblTEAnnualizedGain = Math.Pow((dblToPriceTrend / dblFromPriceTrend), (1.00 / yearDiff)) - 1;

                    dblBHAnnualizedGain = Math.Round(dblBHAnnualizedGain, 3);
                    dblTEAnnualizedGain = Math.Round(dblTEAnnualizedGain, 3);

                    if ((dblTEAnnualizedGain) >= (dblBHAnnualizedGain))
                    {
                        greaterTrendGain = greaterTrendGain + 1;
                        if (greaterTrendGain == 102)
                        {

                        }
                    }

                }
                return greaterTrendGain;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return -1;

            }

        }

        private static double GetPFAnnualizedGain(DateTime fromDate, DateTime toDate, string allocationMethod, int portfolioID)
        {
            try
            {

                double dblToPFvalue = 0, dblFromPFvalue = 0;

                Harbor_EastEntities entities = new Harbor_EastEntities();
                PortfolioValueSet objFromPFvalueSet = entities.PortfolioValueSet.FirstOrDefault(x => x.Date.Equals(fromDate) && x.PortfolioSet.Id.Equals(portfolioID));
                PortfolioValueSet objToPFvalueSet = entities.PortfolioValueSet.FirstOrDefault(x => x.Date.Equals(toDate) && x.PortfolioSet.Id.Equals(portfolioID));

                if (allocationMethod.Equals("BH"))
                {
                    dblFromPFvalue = (double)objFromPFvalueSet.BH;
                    dblToPFvalue = (double)objToPFvalueSet.BH;
                }
                else if (allocationMethod.Equals("TBH"))
                {
                    dblFromPFvalue = (double)objFromPFvalueSet.TBH;
                    dblToPFvalue = (double)objToPFvalueSet.TBH;
                }
                else if (allocationMethod.Equals("REB"))
                {
                    dblFromPFvalue = (double)objFromPFvalueSet.REB;
                    dblToPFvalue = (double)objToPFvalueSet.REB;
                }
                else if (allocationMethod.Equals("TREB"))
                {
                    dblFromPFvalue = (double)objFromPFvalueSet.TREB;
                    dblToPFvalue = (double)objToPFvalueSet.TREB;
                }
                else if (allocationMethod.Equals("RBH"))
                {
                    dblFromPFvalue = (double)objFromPFvalueSet.RBH;
                    dblToPFvalue = (double)objToPFvalueSet.RBH;
                }
                else if (allocationMethod.Equals("RREB"))
                {
                    dblFromPFvalue = (double)objFromPFvalueSet.RREB;
                    dblToPFvalue = (double)objToPFvalueSet.RREB;
                }
                else if (allocationMethod.Equals("RRR"))
                {
                    dblFromPFvalue = (double)objFromPFvalueSet.RRR;
                    dblToPFvalue = (double)objToPFvalueSet.RRR;
                }


                TimeSpan dateDiff = toDate - fromDate;
                double yearDiff = (double)dateDiff.Days / 365.00;

                double dblPFGain = Math.Pow((dblToPFvalue / dblFromPFvalue), (1.00 / yearDiff)) - 1;

                dblPFGain = Math.Round(dblPFGain, 3);
                //double dblBHAnnualizedGain = Math.Pow((dblToPrice / dblFromPrice), (1 / yearDiff)) - 1;
                //double dblTEAnnualizedGain = Math.Pow((dblToPriceTrend / dblFromPriceTrend), (1 / yearDiff)) - 1;
                //string annualizedGain = Convert.ToString(dblBHAnnualizedGain) + "," + Convert.ToString(dblTEAnnualizedGain);
                return dblPFGain;
            }
            catch (Exception ex)
            {
                double d = 0.0;
                return d;
            }

        }

        private static int GetPortfolioGreaterThan(DateTime lastYearDate, DateTime currentDate, string allocationMethod, string secondAllocationMethod)
        {
            int intNoPortfolio = 0;
            int[] totalPortfolioSet = objHEEntities.PortfolioMethodSet.Select(x => x.PortfolioSet.Id).ToArray();
            foreach (int intportfolioID in totalPortfolioSet)
            {
                if (CommonUtility.GetPFAnnualizedGain(lastYearDate, currentDate, allocationMethod, intportfolioID) >= CommonUtility.GetPFAnnualizedGain(lastYearDate, currentDate, secondAllocationMethod, intportfolioID))
                {
                    intNoPortfolio = intNoPortfolio + 1;
                }
            }
            return intNoPortfolio;
        }

        #endregion
    }
}
