using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MyService;
using System.Threading;

namespace HEPortfolioCalculations
{
    class Program
    {
        static void Main(string[] args)
        {
            int pfID = -1;
            Harbor_EastEntities objHEentities = new Harbor_EastEntities();
            //PortfolioSet objPortfolioSet = objHEentities.PortfolioSet.FirstOrDefault(x => x.IsProcessingCompleted.Equals("N"));
            //if (objPortfolioSet != null)
            //{
            //    pfID = objPortfolioSet.Id;
            //    PortfoliosInProgress objPFprogress = objHEentities.PortfoliosInProgress.FirstOrDefault(x => x.PortfolioID.Equals(pfID));
            //    if (objPFprogress != null)
            //        pfID = -1;

            //}

            if (args.Length > 0)
            {
                pfID =  Convert.ToInt32(args[0]);
            }
            try
            {
                if (pfID != -1)
                {
                    PortfoliosInProgress objPF = new PortfoliosInProgress();
                    objPF.PortfolioID = pfID;
                    objHEentities.AddToPortfoliosInProgress(objPF);
                    objHEentities.SaveChanges();

                    clsBuyAndHold objBuynHold = new clsBuyAndHold();
                    objBuynHold.PerformBuyAndHoldOperation(0, pfID.ToString(), false);

                    clsBuyAndHoldTrend objBuynHoldTrend = new clsBuyAndHoldTrend();
                    objBuynHoldTrend.PerformBuyAndHoldTrendOperation(0, pfID.ToString(), false);

                    clsRebalance objRebalance = new clsRebalance();
                    objRebalance.PerformRebalanceOperation(0, pfID.ToString(), false);

                    clsRebalanceTrend objRebalanceTrend = new clsRebalanceTrend();
                    objRebalanceTrend.PerformRebalanceTrend(0, pfID.ToString(), false);

                    clsRotationEngine objRotationEngine = new clsRotationEngine();
                    objRotationEngine.PerformRotationEngineRanking(0, pfID.ToString(), true, false, null, null, null);

                    objHEentities.PortfolioSet.FirstOrDefault(x => x.Id.Equals(pfID)).IsProcessingCompleted = "Y";
                    objHEentities.DeleteObject(objPF);
                    objHEentities.SaveChanges();

                }
                Console.WriteLine("Completed");
            }
            catch (Exception ex)
            {     
                throw;
            }
            
        }
            
    }
}

