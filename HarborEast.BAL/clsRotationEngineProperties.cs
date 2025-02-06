using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HarborEast.BAL
{
    public class clsRotationEngineProperties
    {
        #region Private Variables

        private int intAssetSymbolID;
        private double[] dblDate;
        private double[] dblPrice;
        private double[] dblPriceTrend;
        private double[] dblNormalizedPrice;
        private double[] dblNormalizedPriceTrend;
        private double[] dblSMA;
        private double[] dblSlope;
        private double[] dblDailychange;
        private string[] strState;

        #endregion

        #region Constructors

        public clsRotationEngineProperties()
        {

        }

        public clsRotationEngineProperties(int arraySize)
        {
            dblDate = new double[arraySize];
            dblPrice = new double[arraySize];
            dblPriceTrend = new double[arraySize];
            dblNormalizedPrice = new double[arraySize];
            dblNormalizedPriceTrend = new double[arraySize];
            dblSMA = new double[arraySize];
            dblSlope = new double[arraySize];
            strState = new string[arraySize];
            dblDailychange = new double[arraySize];
        }

        #endregion

        #region Public Properties

        public int IntAssetSymbolID
        {
            get { return intAssetSymbolID; }
            set { intAssetSymbolID = value; }
        }

        public double[] DblDate
        {
            get { return dblDate; }
            set { dblDate = value; }
        }

        public double[] DblPrice
        {
            get { return dblPrice; }
            set { dblPrice = value; }
        }

        public double[] DblPriceTrend
        {
            get { return dblPriceTrend; }
            set { dblPriceTrend = value; }
        }

        public double[] DblNormalizedPrice
        {
            get { return dblNormalizedPrice; }
            set { dblNormalizedPrice = value; }
        }

        public double[] DblNormalizedPriceTrend
        {
            get { return dblNormalizedPriceTrend; }
            set { dblNormalizedPriceTrend = value; }
        }

        public double[] DblSMA
        {
            get { return dblSMA; }
            set { dblSMA = value; }
        }

        public double[] DblSlope
        {
            get { return dblSlope; }
            set { dblSlope = value; }
        }

        public double[] DblDailychange
        {
            get { return dblDailychange; }
            set { dblDailychange = value; }
        }

        public string[] StrState
        {
            get { return strState; }
            set { strState = value; }
        }

        #endregion
    }
}
