using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HEDeletePortfolioData
{
    class Program
    {
        static void Main(string[] args)
        {
            try
             {
                 bool isDeleteSuccessful = false;

                 
                if (true)
                 {
                     
                     Harbor_EastEntities objEntity = new Harbor_EastEntities();
                    
                    //To remove those person whoes user level is 1 from lstpersonset list..
                     List<PersonSet> lstPersonSetToRemove = new List<PersonSet>();
                    
                    //To delete data of the user who has not subscribed..
                     List<PersonSet> lstPersonSet = new List<PersonSet>();//(System.Data.Objects.DataClasses.EntityReference)(objPortfolioContent.AssetSymbolSetReference)).EntityKey.EntityKeyValues[0].Value
                       
                     lstPersonSet = objEntity.PersonSet.Where(x => x.SubscriptionStatus.Equals(false) && x.StartDate.Equals(null) && x.ComplementryAccountStatus.Equals(false)).ToList();
                     #region Commented Code
                     //List<DeletePortfolio> lstDeletePortfolios = objEntity.DeletePortfolio.Where(x=> x.IsDeleted.Equals(true)).ToList();
                     //foreach (DeletePortfolio objDeletePortfolio in lstDeletePortfolios)
                     //{ 
                     //    foreach(PersonSet objPersonSet in lstPersonSet)
                     //    {
                     //         if(objDeletePortfolio.PersonSetId.Equals(objPersonSet.Id))
                     //         {
                     //          lstPersonSet.Remove(objPersonSet);

                     //         }
                     //    }
                     //} 
                     #endregion
                    
                    foreach(PersonSet objPersonSet in lstPersonSet)
                    {
                        if ((((System.Data.Objects.DataClasses.EntityReference)(objPersonSet.UserLevelsReference)).EntityKey.EntityKeyValues[0].Value).Equals(1))
                        {
                            lstPersonSetToRemove.Add(objPersonSet);
                        }
                    }
                    foreach (PersonSet objPersonSet in lstPersonSetToRemove)
                    {
                        lstPersonSet.Remove(objPersonSet);
                    }

                    foreach (PersonSet objPerSonSet in lstPersonSet)
                     {
                      //For Deleting Portfolios  
                      
                              TimeSpan tsDaysDiff= DateTime.Today.Subtract(objPerSonSet.Date);
                              if (tsDaysDiff.TotalDays>=30)
                              {
                                     List<int> lstPortfolioId = objEntity.PortfolioSet.Where(x => x.PersonSet.Id.Equals(objPerSonSet.Id)).Select(x => x.Id).ToList();
                                     foreach (int intPortfolioId in lstPortfolioId)
                                     {
                                         bool isDeleted = DeleteExistingPortfolio(intPortfolioId,true);
                                     }
                                     isDeleteSuccessful = true;
                                     //For Deleting Assets 
                                     if (isDeleteSuccessful)
                                     {

                                         List<AssetMonitoring> lstAssetMonitoring = objEntity.AssetMonitoring.Where(x => x.PersonSet.Id.Equals(objPerSonSet.Id)).ToList();
                                         foreach (AssetMonitoring objAssetMonitoring in lstAssetMonitoring)
                                         {
                                             objEntity.DeleteObject(objAssetMonitoring);
                                         }
                                         //Setting the state when user of free trial deleted
                                         //DeletePortfolio objDeletePortfolio = new DeletePortfolio();
                                         //objDeletePortfolio.PersonSetId = objPerSonSet.Id;
                                         //objDeletePortfolio.IsDeleted = true;
                                         //objDeletePortfolio.EndingDate = DateTime.Now;
                                         
                                         //objEntity.AddToDeletePortfolio(objDeletePortfolio);
                                         objPerSonSet.FreeTrialStatus = false;
                                         objEntity.SaveChanges();
                                         isDeleteSuccessful = true;
                                     }
                                }
                        
                     }

                     //To delete data who has ended their subscription
                     List<DeletePortfolio> lstDeletePortfolio = objEntity.DeletePortfolio.Where(x => x.IsDeleted.Equals(false)).ToList();
                     foreach (DeletePortfolio objDeletePortfolio in lstDeletePortfolio)
                     {
                         PersonSet objPersonSet = objEntity.PersonSet.FirstOrDefault(x => x.Id.Equals(objDeletePortfolio.PersonSetId));
                         
                         if (DateTime.Today.CompareTo(objDeletePortfolio.EndingDate) == 1)
                         {
                             #region Below code to find out termination date
                             //Below code to find out termination date
                             //DateTime terminationDate;
                             //terminationDate = objPersonSet.EndDate.Value;

                             //if (objPersonSet.EndDate.Value.Day.CompareTo(objPersonSet.StartDate.Value.Day) > 0)
                             //{

                             //    terminationDate = terminationDate.AddDays((double)objPersonSet.StartDate.Value.Day - objPersonSet.EndDate.Value.Day - 1);
                             //    terminationDate = terminationDate.AddMonths(1);

                             //}
                             //else
                             //{
                             //    terminationDate = terminationDate.AddDays(objPersonSet.StartDate.Value.Day - objPersonSet.EndDate.Value.Day - 1);
                             //}
                             //if (DateTime.Now.CompareTo(terminationDate).Equals(1))
                             //{


                             //} 
                             #endregion

                             //For Deleting Portfolio of User Ended his/her subscription
                             List<int> lstPortfolioId = objEntity.PortfolioSet.Where(x => x.PersonSet.Id.Equals(objPersonSet.Id)).Select(x => x.Id).ToList();
                             foreach (int intPortfolioId in lstPortfolioId)
                             {
                                 bool isDeleted = DeleteExistingPortfolio(intPortfolioId,true);
                             }
                             isDeleteSuccessful = true;

                             //For Deleting Assets of User Ended his/her subscription
                             if (isDeleteSuccessful)
                             {

                                 List<AssetMonitoring> lstAssetMonitoring = objEntity.AssetMonitoring.Where(x => x.PersonSet.Id.Equals(objDeletePortfolio.PersonSetId)).ToList();
                                 foreach (AssetMonitoring objAssetMonitoring in lstAssetMonitoring)
                                 {
                                     objEntity.DeleteObject(objAssetMonitoring);
                                 }
                                 objEntity.SaveChanges();
                                 isDeleteSuccessful = true;
                             }

                             if (isDeleteSuccessful)
                             {
                                 objDeletePortfolio.IsDeleted = true;
                                 objEntity.DeleteObject(objDeletePortfolio);
                                 objPersonSet.SubscriptionStatus = false;
                                 objEntity.SaveChanges();
                             }
                         }

                     }
                 }
            }
            catch (Exception ex)
            {
          
            }
          
        }

        private static bool DeleteExistingPortfolio(int pfID, bool isDelete)
        {
            bool isDeleteSuccessfull = false;
            Harbor_EastEntities entities = new Harbor_EastEntities();
            bool isDeleteAssetSymbol;
            try
            {    
                int portfolioID = pfID;
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
                #region Commented code
                //     if (isAssetsDeletable)
                //    {
                // List<int> lstSymbolId = entities.PortfolioContentSet.Where(x => x.PortfolioSet.Id.Equals(portfolioID)).Select(x => x.AssetSymbolSet.Id).ToList();
                ////below needs personset id
                //// string strLoginName = Convert.ToString(HttpContext.Current.Session["username"]);
                // int intPersonId = entities.PersonSet.FirstOrDefault(x => x.LoginName.Equals(strLoginName)).Id;
                // List<int> lstSymbolIdNotToDelete = entities.PortfolioContentSet.Where(x => x.PortfolioSet.Id != portfolioID && x.PortfolioSet.PersonSet.Id.Equals(intPersonId)).Select(x => x.AssetSymbolSet.Id).ToList();
                // //int intSymbolID = Convert.ToInt32(((System.Data.Objects.DataClasses.EntityReference)(set.lstPortfolioContentSet)).EntityKey.EntityKeyValues[1].Value);
                // // lstPortfolioContentSet[0].
                // foreach (int symbolId in lstSymbolId)
                // {
                //     isDeleteAssetSymbol = true;

                //     foreach (int objSymbolIdNotToDelete in lstSymbolIdNotToDelete)
                //     {
                //         if (objSymbolIdNotToDelete == symbolId)
                //         {
                //             isDeleteAssetSymbol = false;
                //         }
                //     }
                //     if (isDeleteAssetSymbol)
                //     {
                //         string symbolToDelete = entities.AssetSymbolSet.First(x => x.Id.Equals(symbolId)).Symbol;
                //         AssetMonitoring objAssetMonitering = entities.AssetMonitoring.FirstOrDefault(x => x.PersonSet.Id.Equals(intPersonId) && x.Symbol.Trim().Equals(symbolToDelete));
                //         entities.DeleteObject(objAssetMonitering);

                //    }
                // }
                //   } 
                #endregion
                entities.SaveChanges();
                isDeleteSuccessfull = true;

            }
            catch (Exception)
            {
                return false;
            }
            return isDeleteSuccessfull;
        }


    }
}
