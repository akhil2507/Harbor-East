using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HarborEast.BAL
{
    public class AssetAllocation
    {
        #region Private Variables

        private int _mAssetSymbolId;
        private double[] _mDate;
        private double[] _mPrice;
        private double[] _mPriceTrend;
        private double[] _mSMA;
        private double[] _mSlope;
        private double[] _mAssetValue;
        private double[] _mAssetShares;
        private double[] _mTargetWeight;
        private double[] _mWeight;
        private double[] _mDailyChange;
        private string[] _mState;

        #endregion

        #region Public Properties

        public int MAssetSymbolId
        {
            get { return _mAssetSymbolId; }
            set { _mAssetSymbolId = value; }
        }

        public double[] MDate
        {
            get { return _mDate; }
            set { _mDate = value; }
        }

        public double[] MPrice
        {
            get { return _mPrice; }
            set { _mPrice = value; }
        }

        public double[] MPriceTrend
        {
            get { return _mPriceTrend; }
            set { _mPriceTrend = value; }
        }

        public double[] MSMA
        {
            get { return _mSMA; }
            set { _mSMA = value; }
        }

        public double[] MSlope
        {
            get { return _mSlope; }
            set { _mSlope = value; }
        }

        public double[] MAssetValue
        {
            get { return _mAssetValue; }
            set { _mAssetValue = value; }
        }

        public double[] MAssetShares
        {
            get { return _mAssetShares; }
            set { _mAssetShares = value; }
        }

        public double[] MTargetWeight
        {
            get { return _mTargetWeight; }
            set { _mTargetWeight = value; }
        }

        public double[] MWeight
        {
            get { return _mWeight; }
            set { _mWeight = value; }
        }

        public double[] MDailyChange
        {
            get { return _mDailyChange; }
            set { _mDailyChange = value; }
        }

        public string[] MState
        {
            get { return _mState; }
            set { _mState = value; }
        }

        #endregion

        #region Constructors

        public AssetAllocation(int NoOfDays, int switchCase)
        {
            _mDate = new double[NoOfDays];
            _mPrice = new double[NoOfDays];
            _mPriceTrend = new double[NoOfDays];
            _mSMA = new double[NoOfDays];
            _mSlope = new double[NoOfDays];
            _mWeight = new double[NoOfDays];
            _mDailyChange = new double[NoOfDays];
            if (switchCase != 0)
            {
                _mTargetWeight = new double[NoOfDays];
                _mState = new string[0];
            }
            else
            {
                _mTargetWeight = new double[0];
                _mState = new string[NoOfDays];
            }

            _mAssetShares = new double[NoOfDays];
            _mAssetValue = new double[NoOfDays];
        }

        #endregion 
    }
}
