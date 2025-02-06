using System;
using System.Data;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarborEast.BAL;
using System.Web.UI.HtmlControls;
using System.Web;
using System.IO;
using System.Data.EntityClient;
using Microsoft.VisualBasic;



namespace HarborEastScheduler
{
    class Scheduler
    {
        static void Main(string[] args)
        {
            try
            {

                string DSfilePath = @"D:\PROJECTS\TFS_HARBOREAST(122.170.121.6)\HarborEastScheduler\HarborEastScheduler\Logs\DS_Log.txt";
                string TEfilePath = @"D:\PROJECTS\TFS_HARBOREAST(122.170.121.6)\HarborEastScheduler\HarborEastScheduler\Logs\TE_Log.txt";

                string[] assetSymbols = null;
                int intPortfolioId = -1;
                if (args.Length > 0)
                {
                    intPortfolioId = Convert.ToInt32(args[0]);
                }

                if (intPortfolioId != -1)
                {
                    Harbor_EastEntities objHEentities = new Harbor_EastEntities();
                    assetSymbols = objHEentities.PortfolioContentSet.Where(x => x.PortfolioSet.Id.Equals(intPortfolioId)).Select(x => x.AssetSymbolSet.Symbol).ToArray();
                }

                //DataScrapper functionality.
                Console.WriteLine("Processing DataScrapper...");
                Console.WriteLine(DateTime.Now);
                clsDataScrapperBAL objDataScrapper = new clsDataScrapperBAL(DSfilePath);

                if (intPortfolioId != -1)
                {
                    foreach (string strAssetSymbol in assetSymbols)
                    {
                        objDataScrapper.PushDataIntoDataBase(DateTime.Now.Subtract(TimeSpan.FromDays(1)), false, strAssetSymbol);
                    }
                }
                else
                {
                    objDataScrapper.PushDataIntoDataBase(DateTime.Now.Subtract(TimeSpan.FromDays(1)), false, string.Empty);
                    objDataScrapper.ReadDSLog(DSfilePath);
                }
                Console.WriteLine("Processed DataScrapper...");
                Console.WriteLine("");
                Console.WriteLine(DateTime.Now);
                //TrendEngine functionality.
                Console.WriteLine("Processing TrendEngine...");
                Console.WriteLine(DateTime.Now);
                clsHistoricalTrendEngine objDailyTrendEngine = new clsHistoricalTrendEngine(TEfilePath);
                Harbor_EastEntities entities = new Harbor_EastEntities();
                List<HarborEast.BAL.AssetSymbolSet> lstAssetSymbolSet = entities.AssetSymbolSet.ToList();

                // Changed code for removing symbols processing in the symbol added in windows service 2/4/2012
                //List<NewAssets> lstNewAsset = entities.NewAssets.ToList();
                //foreach (NewAssets objNewAsset in lstNewAsset)
                //{
                //    AssetSymbolSet objAssetSymbolToRemove = entities.AssetSymbolSet.FirstOrDefault(x => x.Symbol.Equals(objNewAsset.Symbol));
                //    lstAssetSymbolSet.Remove(objAssetSymbolToRemove);
                //}

                if (intPortfolioId != -1)
                {
                    foreach (string strAssetSymbol in assetSymbols)
                    {
                        //if (DateTime.Now.Day.Equals(DayOfWeek.Sunday))
                        //{
                        //    objDailyTrendEngine.PerformTrendEngineAnalysis(DateTime.Now, strAssetSymbol, false, true);
                        //}
                        //else
                        //{
                        //    if (entities.AssetSet.Where(x => x.AssetSymbolSet.Symbol.Trim().ToLower().Equals(strAssetSymbol.Trim().ToLower())).Count() > 0)
                        //        objDailyTrendEngine.PerformTrendEngineAnalysis(DateTime.Now, strAssetSymbol, false, false);
                        //    else
                        //        objDailyTrendEngine.PerformTrendEngineAnalysis(DateTime.Now, strAssetSymbol, false, true);
                        //}
                        objDailyTrendEngine.PerformTrendEngineAnalysis(DateTime.Now.Subtract(TimeSpan.FromDays(1)), strAssetSymbol, false, false);
                    }
                }
                else
                {
                    if (lstAssetSymbolSet != null)
                    {
                        //Iterate through the list of AssetSymbolSet.
                        foreach (HarborEast.BAL.AssetSymbolSet objSymbolSet in lstAssetSymbolSet)
                        {

                            //Process Historical TrendEngine if the day is Sunday.
                            //if (DateTime.Now.Day.Equals(DayOfWeek.Sunday))
                            //{
                            //    objDailyTrendEngine.PerformTrendEngineAnalysis(DateTime.Now, objSymbolSet.Symbol, false, true);
                            //}
                            //else
                            //{
                            //    if (entities.AssetSet.Where(x => x.AssetSymbolSet.Id.Equals(objSymbolSet.Id)).Count() > 0)
                            //        objDailyTrendEngine.PerformTrendEngineAnalysis(DateTime.Now, objSymbolSet.Symbol, false, false);
                            //    else
                            //        objDailyTrendEngine.PerformTrendEngineAnalysis(DateTime.Now, objSymbolSet.Symbol, false, true);
                            //}
                            objDailyTrendEngine.PerformTrendEngineAnalysis(DateTime.Now.Subtract(TimeSpan.FromDays(1)), objSymbolSet.Symbol, false, false);
                        }
                    }
                }

                objDailyTrendEngine.ReadTELog(TEfilePath);
                Console.WriteLine("Processed TrendEngine...");
                Console.WriteLine("");
                Console.WriteLine(DateTime.Now);

                //Allocation Methods...
                Console.WriteLine("Processing AllocationEngines...");
                Console.WriteLine(DateTime.Now);
                List<HarborEast.BAL.PortfolioSet> lstPortFolioSet = entities.PortfolioSet.ToList();
                if (lstPortFolioSet != null)
                {
                    //Iterate through the list of PortfolioSet.
                    foreach (HarborEast.BAL.PortfolioSet objPFSet in lstPortFolioSet)
                    {
                        if (intPortfolioId != -1)
                        {
                            //** code added for web service reference
                            //MyServiceClient client = new MyServiceClient();
                            //client.RunAllAllocationMethodsForPortfolio(intPortfolioId);
                            //break;
                            //**



                            //Process Buy And Hold.
                            clsBuyAndHold objBuyNHold = new clsBuyAndHold();
                            objBuyNHold.PerformBuyAndHoldOperation(0, intPortfolioId.ToString(), false);

                            //Process Buy Amd Hold Trend.
                            clsBuyAndHoldTrend objBuyNHoldTrend = new clsBuyAndHoldTrend();
                            objBuyNHoldTrend.PerformBuyAndHoldTrendOperation(0, intPortfolioId.ToString(), false);

                            //Process Rebalance.
                            clsRebalance objRebalance = new clsRebalance();
                            objRebalance.PerformRebalanceOperation(0, intPortfolioId.ToString(), false);

                            //Process Rebalance Trend.
                            clsRebalanceTrend objRebalanceTrend = new clsRebalanceTrend();
                            objRebalanceTrend.PerformRebalanceTrend(0, intPortfolioId.ToString(), false);
                            break;
                        }
                        else
                        {

                            //Process Buy And Hold.
                            clsBuyAndHold objBuyNHold = new clsBuyAndHold();
                            objBuyNHold.PerformBuyAndHoldOperation(0, objPFSet.Id.ToString(), false);

                            //Process Buy Amd Hold Trend.
                            clsBuyAndHoldTrend objBuyNHoldTrend = new clsBuyAndHoldTrend();
                            objBuyNHoldTrend.PerformBuyAndHoldTrendOperation(0, objPFSet.Id.ToString(), false);

                            //Process Rebalance.
                            clsRebalance objRebalance = new clsRebalance();
                            objRebalance.PerformRebalanceOperation(0, objPFSet.Id.ToString(), false);

                            //Process Rebalance Trend.
                            clsRebalanceTrend objRebalanceTrend = new clsRebalanceTrend();
                            objRebalanceTrend.PerformRebalanceTrend(0, objPFSet.Id.ToString(), false);
                        }
                    }
                    Console.WriteLine("Processed AllocationEngines...");
                    Console.WriteLine("");
                    Console.WriteLine(DateTime.Now);

                    //Process RotationEngine.
                    Console.WriteLine("Processing RotationEngine...");
                    Console.WriteLine(DateTime.Now);
                    //Iterate through the list of PortfolioSet.
                    foreach (HarborEast.BAL.PortfolioSet objPFSet in lstPortFolioSet)
                    {
                        if (intPortfolioId != -1)
                        {
                            //** code added for web service reference
                            //MyServiceClient client = new MyServiceClient();
                            //client.RunRotationEngineForPortfolio(intPortfolioId);
                            //break;
                            //**

                            clsRotationEngine objRotationEngine = new clsRotationEngine();
                            // objRotationEngine.PerformRotationEngineRanking(0, intPortfolioId.ToString(), false, false, new HtmlGenericControl(), new HtmlGenericControl(), new string[] { });
                            objRotationEngine.PerformRotationEngineRanking(0, intPortfolioId.ToString(), false, false, null, null, new string[] { });
                            break;
                        }
                        else
                        {
                            clsRotationEngine objRotationEngine = new clsRotationEngine();
                            //objRotationEngine.PerformRotationEngineRanking(0, objPFSet.Id.ToString(), false, false, new HtmlGenericControl(), new HtmlGenericControl(), new string[] { });
                            objRotationEngine.PerformRotationEngineRanking(0, objPFSet.Id.ToString(), false, false, null, null, new string[] { });
                        }

                    }
                    Console.WriteLine("Processed RotationEngine...");
                    Console.WriteLine(DateTime.Now);
                }


                //using (Harbor_EastEntities objHEentities = new Harbor_EastEntities())
                Harbor_EastEntities objHarborEntities = new Harbor_EastEntities();
                
                DateTime dtFirst = objHarborEntities.AssetSet.First().Ending_Date;
                //DateTime dtLast = objHarborEntities.AssetSet.Ending_Date;
                if (Microsoft.VisualBasic.DateAndTime.DateDiff(DateInterval.Day, dtFirst, DateTime.Now, Microsoft.VisualBasic.FirstDayOfWeek.Sunday, FirstWeekOfYear.Jan1) == 2)
                {


                    SqlConnection connection = new SqlConnection("Integrated Security=\"false\";Persist Security Info=False;User ID=sa;Password=root123;Initial Catalog=Harbor East;Data Source=OCS-WKS-098\\SQL2K8;");

                    //'System.Data.Common.DbConnection connection = objHEentities.Connection;
                    connection.Open();

                    SqlCommand command = connection.CreateCommand();
                    command.CommandText = "FireShareAlerts";
                    command.CommandType = CommandType.StoredProcedure;
                    try
                    {
                        command.ExecuteNonQuery();
                        command.CommandText = "FireSymAlerts";
                        command.CommandType = CommandType.StoredProcedure;
                        command.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {

                    }
                    finally
                    {
                        connection.Close();
                    }
                }
                CommonUtility.CalculateMonthlyDetails();
                                             
            }
            catch (Exception ex)
            {

            }
        }
    }
}
