using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Objects.DataClasses;
using Microsoft.VisualBasic;

namespace HarborEast.BAL
{
    public class clsPortfolioNormalization
    {
        #region Variable Declarations

        Harbor_EastEntities objHEEntities = new Harbor_EastEntities();

        #endregion

        #region Constructors

        public clsPortfolioNormalization()
        {

        }

        #endregion

        #region Private Methods

        /// <summary>
        /// This private method is used to get the total number of record count present within the assetpriceset database for a particular date.
        /// </summary>
        /// <param name="lstPortfolioContentSet">collection of portfoliocontentset.</param>
        /// <param name="dtFinalStartDate">The date for which the count is supposed to be calculated.</param>
        /// <returns>number of record count.</returns>
        private int GetTotalRecordCountForDate(List<PortfolioContentSet> lstPortfolioContentSet, DateTime dtFinalStartDate)
        {
            int intTotalDateCount = 0;
            try
            {
                //Iterate through the loop of portfoliocontentset and return the number of record count present for the particular date in the Assetpriceset database.
                foreach (var set in lstPortfolioContentSet)
                {
                    int intAssetSymbolSetID = Convert.ToInt32(((EntityReference)(set.AssetSymbolSetReference)).EntityKey.EntityKeyValues[0].Value);
                    var objAssetPriceSet = objHEEntities.AssetPriceSet.FirstOrDefault(x => x.AssetSymbolSet.Id.Equals(intAssetSymbolSetID) && x.Date.Equals(dtFinalStartDate));
                    if (objAssetPriceSet != null)
                        intTotalDateCount++;
                }
            }
            catch (Exception)
            {

            }
            return intTotalDateCount;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// This method is used to fill the missing values for a particular date in the AssetPriceSet table.
        /// </summary>
        /// <param name="intSelectedIndex"></param>
        /// <param name="strSelectedValue">PortofolioID</param>
        public void PerformPortfolioNormalization(int intSelectedIndex, string strSelectedValue)
        {
            try
            {
                if (intSelectedIndex != -1)
                {
                    int intPortfolioID = Convert.ToInt32(strSelectedValue);

                    //Get the collection of PortFolioContentSet table for a Portfolio.
                    List<PortfolioContentSet> lstPortfolioContentSet = objHEEntities.PortfolioContentSet.Where(x => x.PortfolioSet.Id.Equals(intPortfolioID)).ToList();
                    if (lstPortfolioContentSet != null)
                    {
                        DateTime dtFinalStartDate = new DateTime(1, 1, 1);
                        int intCount = 0;
                        int intAssetSymbolID = 0;
                        int intArraySize = 0;

                        #region Get the earliest trading date for Portfolio

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

                        #region Get the array size for this selected Portfolio from the AssetPriceSet table.

                        intArraySize = CommonUtility.GetArraySize(intAssetSymbolID);

                        #endregion

                        #region Fill the missing date values in the AssetPriceSet table.

                        if (intArraySize != -1)
                        {
                            while (dtFinalStartDate < DateTime.Now)
                            {
                                //Get the record count present in the AssetPriceSet table for a particular date.
                                if (objHEEntities.AssetPriceSet.Where(x => x.Date.Equals(dtFinalStartDate)).Count() > 0)
                                {
                                    //Get the record count present in the AssetPriceSet table for a particular date and for symbols present in Portfoliocontentset.
                                    int intTotalDateCount = GetTotalRecordCountForDate(lstPortfolioContentSet, dtFinalStartDate);
                                    if (intTotalDateCount != lstPortfolioContentSet.Count && intTotalDateCount != 0)
                                    {
                                        foreach (var objSet in lstPortfolioContentSet)
                                        {
                                            int intAssetSymbId = Convert.ToInt32(objSet.AssetSymbolSetReference.EntityKey.EntityKeyValues[0].Value);
                                            if (objHEEntities.AssetPriceSet.Where(x => x.Date.Equals(dtFinalStartDate) && x.AssetSymbolSet.Id.Equals(intAssetSymbId)).Count() == 0)
                                            {
                                                //Get the last valid trading date.
                                                DateTime dtPrevDate = CommonUtility.GetPreviousDate(dtFinalStartDate, intAssetSymbId);

                                                //Get the data for the last valid trading date.
                                                AssetPriceSet objPrevPriceSet = objHEEntities.AssetPriceSet.FirstOrDefault(x => x.Date.Equals(dtPrevDate) && x.AssetSymbolSet.Id.Equals(intAssetSymbId));
                                                
                                                //Add a new record in the Assetpriceset table with the new values for a missing date.
                                                if (objPrevPriceSet != null)
                                                {
                                                    AssetPriceSet objAssetPriceSet = new AssetPriceSet();
                                                    objAssetPriceSet.Date = dtFinalStartDate;
                                                    objAssetPriceSet.Growth = objPrevPriceSet.Growth;
                                                    objAssetPriceSet.MeanDeviation = objPrevPriceSet.MeanDeviation;
                                                    objAssetPriceSet.Price = objPrevPriceSet.Price;
                                                    objAssetPriceSet.PriceTrend = objPrevPriceSet.PriceTrend;
                                                    objAssetPriceSet.State = objPrevPriceSet.State;
                                                    objAssetPriceSet.Threshold = objPrevPriceSet.Threshold;
                                                    AssetSymbolSet objAssetSymSet = objHEEntities.AssetSymbolSet.First(x => x.Id.Equals(intAssetSymbId));
                                                    objAssetPriceSet.AssetSymbolSet = objAssetSymSet;
                                                    objHEEntities.AddToAssetPriceSet(objAssetPriceSet);
                                                }
                                            }
                                        }
                                    }
                                }
                                dtFinalStartDate = dtFinalStartDate.AddDays(1);
                            }
                            objHEEntities.SaveChanges();
                        }

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
}
