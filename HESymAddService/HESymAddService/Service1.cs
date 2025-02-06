using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Configuration;
using MyService;
using System.Messaging;
using System.IO;
using System.Timers;


namespace HESymAddService
{
    public partial class SymbolAddService : ServiceBase
    {
        private Timer objTimer = null;
        public SymbolAddService()
        {
            InitializeComponent();

            double interval = 60000;
            objTimer = new Timer(interval);
            objTimer.Elapsed += new ElapsedEventHandler(objTimer_Elapsed);

            if (!System.Diagnostics.EventLog.SourceExists("SymAddLogSource"))
                System.Diagnostics.EventLog.CreateEventSource("SymAddLogSource","SymAddLog");

            eventLog1.Source = "SymAddLogSource";
            // the event log source by which             
            //the application is registered on the computer

            eventLog1.Log = "SymAddLog";
        }

        void objTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            this.objTimer.Stop();
            try
            {
                string validAssets = string.Empty;
                string errorAssets = string.Empty;
                string successAssets = string.Empty;
                List<clsErrorDetails> lstDSErrorDetails = null;
                List<clsErrorDetails> lstTEErrorDetails = new List<clsErrorDetails>();

                Harbor_EastEntities objEntities = new Harbor_EastEntities();
                string[] arrSymbols = objEntities.NewAssets.Where(x => x.Flag.Equals("F")).Select(x => x.Symbol).ToArray<string>();
                if (arrSymbols.Count() > 0)
                {
                    clsDataScrapperBAL objDS = new clsDataScrapperBAL();
                    objDS.PushDataIntoDataBase(DateTime.Now, arrSymbols);
                    lstDSErrorDetails = objDS.GetDSErrorDetails();


                    foreach (string strAsset in arrSymbols)
                    {
                        clsErrorDetails tempError = lstDSErrorDetails.FirstOrDefault(x => x.Symbol.ToLower().Equals(strAsset.ToLower()));

                        if (tempError == null)
                        {
                            validAssets += strAsset + ",";
                        }
                        else
                        {
                            errorAssets += strAsset + ",";
                        }
                    }

                    clsHistoricalTrendEngine objHistTrendEngine = new clsHistoricalTrendEngine();
                    foreach (string strSymbol in validAssets.Trim(',').Split(",".ToCharArray()))
                    {
                        bool isTrendEngineCompleted = objHistTrendEngine.PerformTrendEngineAnalysis(DateTime.Now, strSymbol.Trim(), false, true);

                        if (isTrendEngineCompleted == false)
                        {
                            clsErrorDetails objErrorDetails = new clsErrorDetails();
                            objErrorDetails.Symbol = strSymbol;
                            objErrorDetails.ErrorDescription = objHistTrendEngine.TrendEngineErrorMsg;
                            lstTEErrorDetails.Add(objErrorDetails);
                        }
                    }

                    foreach (string strsymbol in validAssets.Trim(',').Split(','))
                    {
                        clsErrorDetails tempError = lstTEErrorDetails.FirstOrDefault(x => x.Symbol.ToLower().Equals(strsymbol.ToLower()));

                        if (tempError != null)
                        {
                            errorAssets += strsymbol + ",";
                        }
                        else
                        {
                            successAssets += strsymbol + ",";
                        }
                    }

                    var lstNewAssets = objEntities.NewAssets.Where(x => x.Flag.Equals("F"));

                    foreach (string strSymbol in arrSymbols)
                    {
                        if (successAssets.Trim(',').Split(',').Contains(strSymbol))
                        {
                            lstNewAssets.FirstOrDefault(x => x.Symbol.Equals(strSymbol)).Flag = "T";
                        }
                        else
                        {
                            lstNewAssets.FirstOrDefault(x => x.Symbol.Equals(strSymbol)).Flag = "E";
                            if (lstDSErrorDetails.Where(x=>x.Symbol.Equals(strSymbol)).Count() > 0)
                            {
                                lstNewAssets.FirstOrDefault(x => x.Symbol.Equals(strSymbol)).ErrorDescription = lstDSErrorDetails.FirstOrDefault(x => x.Symbol.Equals(strSymbol)).ErrorDescription;
                            }
                            else if (lstTEErrorDetails.Where(x => x.Symbol.Equals(strSymbol)).Count() > 0)
                            {
                                lstNewAssets.FirstOrDefault(x => x.Symbol.Equals(strSymbol)).ErrorDescription = lstTEErrorDetails.FirstOrDefault(x => x.Symbol.Equals(strSymbol)).ErrorDescription;
                            }
                        }
                    }

                    objEntities.SaveChanges();
                }

            }
            catch (Exception ex)
            {
                eventLog1.WriteEntry(ex.Message);
            }

            this.objTimer.Start();
            
        }

        
        protected override void OnStart(string[] args)
        {
            eventLog1.WriteEntry("Symbol addition Service Started");
            objTimer.AutoReset = true;
            objTimer.Enabled = true;
            objTimer.Start();
            //fswSymbols.Path = ConfigurationSettings.AppSettings["SymbolMonitorPath"];
        }

        protected override void OnStop()
        {
            eventLog1.WriteEntry("Symbol addition Service Stopped");
            objTimer.AutoReset = false;
            objTimer.Enabled = false;
        }

        protected override void OnContinue()
        {
            eventLog1.WriteEntry("Symbol addition Service Stopped");
            this.objTimer.Start();
        }

        protected override void OnPause()
        {
           this.objTimer.Stop();
        }

        //private void fswSymbols_Created(object sender, System.IO.FileSystemEventArgs e)
        //{
        //    string strName=string.Empty;
        //    //MessageQueue mq=null;
        //    try 
        //    {	        
        //        Harbor_EastEntities entities = new Harbor_EastEntities();
        //        string DSfilePath = @"D:\PROJECTS\TFS_HARBOREAST(122.170.121.6)\HarborEastScheduler\HarborEastScheduler\Logs\DS_Log.txt";
        //        string TEfilePath = @"D:\PROJECTS\TFS_HARBOREAST(122.170.121.6)\HarborEastScheduler\HarborEastScheduler\Logs\TE_Log.txt";
        //        string SymbolName =  e.Name.Split(".".ToCharArray())[1];

        //        clsDataScrapperBAL objDS = new clsDataScrapperBAL(DSfilePath);
        //        objDS.PushDataIntoDataBase(DateTime.Now, false, SymbolName);

        //        clsHistoricalTrendEngine objTE = new clsHistoricalTrendEngine(TEfilePath);
        //        objTE.PerformTrendEngineAnalysis(DateTime.Now, SymbolName, false, true);
        //        string strsSymbolAddPath = ConfigurationSettings.AppSettings["SymbolMonitorPath"].ToString();

        //        File.Create(ConfigurationSettings.AppSettings["ProcessedSymbolsPath"] + "\\" + e.Name);   
        //        File.Delete( strsSymbolAddPath + "\\"+e.Name);
                
        //        //mq = new MessageQueue(@".\Private$\harboreast");

        //        strName = e.Name;
        //    }
        //    catch (Exception)
        //    {
        //        strName = "";
        //    }

        //    //mq.Send(strName);
            
        //}

        private void fswSymbols_Deleted(object sender, System.IO.FileSystemEventArgs e)
        {

        }
    }
}
