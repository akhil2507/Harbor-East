using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HarborEast.BAL
{
    public class clsSharesInfo
    {
        #region Constructors

        public clsSharesInfo(int intArraySize)
        {
            _assetShares = new double[intArraySize];
        }


        #endregion

        #region Private Variables

        private int _assetSymbolID;
        private double[] _assetShares;

        #endregion

        #region Public Properties

        public int AssetSymbolID
        {
            get { return _assetSymbolID; }
            set { _assetSymbolID = value; }
        }
        public double[] AssetShares
        {
            get { return _assetShares; }
            set { _assetShares = value; }
        }

        #endregion
     
    }
}
