using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace SignalInfo
{
    public struct SigData
    {
        public const int GSM_SIG_STRENGTH = 1;
        public const int GSM_BIT_ERROR = 2;
        public const int CDMA_SIGNAL = 3;
        public const int CDMA_ECIO = 4;
        public const int EVDO_SIGNAL = 5;
        public const int EVDO_ECIO = 6;
        public const int EVDO_SNR = 7;
        public const int LTE_SIG_STRENGTH = 8;
        public const int LTE_RSRP = 9;
        public const int LTE_RSRQ = 10;
        public const int LTE_SNR = 11;
        public const int LTE_CQI = 12;
        public const int IS_GSM = 13;
        public const int LTE_RSSI = 14;
    };
}