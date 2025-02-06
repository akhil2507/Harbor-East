using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HarborEast.BAL
{
    public class clsRECalculations
    {
        #region Private Variables

        private string _symbolPair;
        private List<clsSharesInfo> _sharesInfo;
        private double[] _totalValue;
        private int[] _rotationEvent;
        private double[] _firstShare;
        private double[] _secondShare;

        #endregion

        #region Public Properties

        public string SymbolPair
        {
            get { return _symbolPair; }
            set { _symbolPair = value; }
        }
        public List<clsSharesInfo> SharesInfo
        {
            get { return _sharesInfo; }
            set { _sharesInfo = value; }
        }
        public double[] TotalValue
        {
            get { return _totalValue; }
            set { _totalValue = value; }
        }
        public int[] RotationEvent
        {
            get { return _rotationEvent; }
            set { _rotationEvent = value; }
        }
        public double[] FirstShare
        {
            get { return _firstShare; }
            set { _firstShare = value; }
        }
        public double[] SecondShare
        {
            get { return _secondShare; }
            set { _secondShare = value; }
        }

        #endregion

        #region Constructors

        public clsRECalculations(int intArraySize)
        {
            _totalValue = new double[intArraySize];
            _rotationEvent = new int[intArraySize];
            _firstShare = new double[intArraySize];
            _secondShare = new double[intArraySize];
        }

        #endregion
    }
}
