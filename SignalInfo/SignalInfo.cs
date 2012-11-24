using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Android.App;
using Android.Support.V4.App;
using Android.Telephony;
using Android.Util;
using Android.Views;
using Android.Widget;
using Android.OS;
using Java.Lang;

namespace SignalInfo
{
    /// <summary>
    /// Displays various signal data to the user.
    /// </summary>
    [Activity(Label = "SignalInfo", MainLauncher = true, Icon = "@drawable/icon")]
    public class SignalInfo : FragmentActivity
    {
        /// <summary>
        /// The default text to show to the user if no data
        /// </summary>
        private const string DEFAULT_TXT = "N/A";

        /// <summary>
        /// The max number of telephony entries
        /// </summary>
        private const int MAX_SIGNAL_ENTRIES = 14;

        private const string TAG = "SignalInfo";
        private MyPhoneStateListener listen;
        private TelephonyManager tm;

        /// <summary>
        /// Loads the app.
        /// </summary>
        /// <param name="bundle"></param>
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.Main);
            Window.AddFlags(WindowManagerFlags.KeepScreenOn);
/*        AdView ad = (AdView) findViewById(Resource.Id.adView);
        ad.loadAd(new AdRequest());*/
            listen = new MyPhoneStateListener(this);
            tm = (TelephonyManager) GetSystemService(TelephonyService);
            tm.Listen(listen, PhoneStateListenerFlags.SignalStrengths);

            setPhoneInfo();
        }

        /// <summary>
        /// Computes the rssi.
        /// </summary>
        /// <param name="rsrp">The RSRP.</param>
        /// <param name="rsrq">The RSRQ.</param>
        /// <returns></returns>
        private static int computeRssi(string rsrp, string rsrq)
        {
            return -17 - Integer.ParseInt(rsrp) - Integer.ParseInt(rsrq);
        }

        /// <summary>
        /// Formats the signal data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        private static string[] formatSignalData(string[] data)
        {
            data[0] = "N/A";

            for (int i = 1; i < data.Length - 1; ++i) {
                if ("-1".Equals(data[i])) {
                    data[i] = "N/A";
                }
                else if ("99".Equals(data[i])) {
                    data[i] = "N/A";
                }
                else if (Convert.ToInt32(data[i]) > 9999) {
                    data[i] = "N/A";
                }
                else {
                    data[i] = data[i];
                }
            }
            return data;
        }

        /// <summary>
        /// Set the phone model, OS version, carrier name on the screen
        /// </summary>
        private void setPhoneInfo()
        {
            var t = FindViewById<TextView>(Resource.Id.phoneName);
            t.Text = Build.Manufacturer + ' ' + Build.Model;

            t = FindViewById<TextView>(Resource.Id.phoneModel);
            t.Text = Build.Product + '/' + Build.Device + " (" + Build.Id + ") ";

            t = FindViewById<TextView>(Resource.Id.androidVersion);
            t.Text = Build.VERSION.Release + " (API version " + Build.VERSION.SdkInt + ')';

            t = FindViewById<TextView>(Resource.Id.carrierName);
            t.Text = tm.NetworkOperatorName;

            t = FindViewById<TextView>(Resource.Id.buildHost);
            t.Text = Build.Host;
        }

        /// <summary>
        /// Stop recording when screen is not in the front.
        /// </summary>
        protected override void OnPause()
        {
            base.OnPause();
            tm.Listen(listen, PhoneStateListenerFlags.None);
        }

        /// <summary>
        /// Start recording when the screen is on again.
        /// </summary>
        protected override void OnResume()
        {
            base.OnResume();
            tm.Listen(listen, PhoneStateListenerFlags.SignalStrengths);
        }

        /// <summary>
        /// Set the signal info the user sees.
        /// </summary>
        /// <param name="signalStrength">contains all the signal info</param>
        /// <see cref="Android.Telephony.SignalStrength"/>
        private void setSignalInfo(SignalStrength signalStrength)
        {
            Log.Debug(TAG, "formatting sig str");
            var spaceStr = new Regex(" ");
            string[] sigInfo = formatSignalData(spaceStr.Split(signalStrength.ToString()));

            Log.Debug("Signal Array", sigInfo.ToString());
            displaySignalInfo(sigInfo);
        }

        /// <summary>
        /// Displays the signal info.
        /// </summary>
        /// <param name="sigInfo">The sig info.</param>
        private void displaySignalInfo(string[] sigInfo)
        {
            var signalDataMap = getSignalDataMap();

            foreach (KeyValuePair<int, TextView> data in signalDataMap) {
                // TODO: maybe use an adapter of some sort instead of this (ListAdapter maybe?)
                var currentTextView = data.Value;

                try {
                    string sigValue;

                    if (data.Key == SigData.LTE_RSSI) {
                        sigValue = DEFAULT_TXT.Equals(sigInfo[SigData.LTE_RSRP]) ||
                                   DEFAULT_TXT.Equals(sigInfo[SigData.LTE_RSRQ])
                                       ? DEFAULT_TXT
                                       : "-" + computeRssi(sigInfo[SigData.LTE_RSRP], sigInfo[SigData.LTE_RSRQ]);
                    }
                    else {
                        sigValue = sigInfo[data.Key];
                    }
                    Log.Debug(TAG, "sigValue: " + sigValue);

                    if (!sigValue.Equals(DEFAULT_TXT)) {
                        var db = "";
                        if (data.Key != SigData.IS_GSM) {
                            db = " db";
                        }
                        Log.Debug(TAG, "sigvalue before setting: " + sigValue + db);
                        currentTextView.Text = sigValue + db;
                    }
                }
                catch (ArrayIndexOutOfBoundsException) {
                    Log.Wtf(TAG, "This phone is probably old or the OEM (likely samsung) " +
                                 "half-arsed the Telephony API :( " +
                                 "(see: http://code.google.com/p/android/issues/detail?id=18336)");
                }
                catch (Android.Content.Res.Resources.NotFoundException) {
                    currentTextView.Text = DEFAULT_TXT;
                }
            }
        }

        /// <summary>
        /// Get the TextView that matches with the signal data
        /// value and store both in a map entry. data value is tied to the
        /// order it would be returned in the tostring() method to get
        /// all data from SignalStrength.
        /// </summary>
        /// <returns>the mapped TextViews to their signal data key</returns>
        private Dictionary<int, TextView> getSignalDataMap()
        {
            var layout = FindViewById<LinearLayout>(Resource.Id.main);
            Log.Debug(TAG, "layout tag: " + layout.Tag);
            var uscore = new Regex("_");
            var signalData = new Dictionary<int, TextView>();

            for (int i = 1; i <= MAX_SIGNAL_ENTRIES; ++i) {
                try {
                    var currentView = (TextView) layout.FindViewWithTag(Convert.ToString(i));
                    if (currentView != null) {
                        Log.Debug(TAG, "Current text view: "
                                       + Resources.GetResourceEntryName(currentView.Id)
                                       + " Resource.Id: " + currentView.Id);
                    }
                    else {
                        Log.Debug(TAG, "Current Text View retrieved was null");
                        continue;
                    }
                    var childName = uscore.Split(
                        Resources.GetResourceEntryName(currentView.Id));

                    if (childName.Length > 1) {
                        signalData.Add(Integer.ParseInt(childName[1]), currentView);
                    }
                    Log.Debug(TAG, "array data: " + childName);
                    Log.Debug(TAG, "array data len: " + childName.Length);
                }
                catch (Android.Content.Res.Resources.NotFoundException) {
                    Log.Error(TAG, "Could not parse signal textviews");
                }
            }
            Log.Debug(TAG, "Returning signal data: " + signalData.Count);
            return signalData;
        }

        /// <summary>
        /// Private helper class to listen for network signal changes.
        /// </summary>
        private class MyPhoneStateListener : PhoneStateListener
        {
            /// <summary>
            /// The parent class
            /// </summary>
            private readonly SignalInfo si;

            /// <summary>
            /// Initializes a new instance of the <see cref="MyPhoneStateListener" /> class.
            /// </summary>
            /// <param name="si">The parent.</param>
            public MyPhoneStateListener(SignalInfo si)
            {
                this.si = si;
            }

            /// <summary>
            /// Get the Signal strength from the provider, each time there is an update
            /// </summary>
            /// <param name="signalStrength">has all the useful signal stuff in it.</param>
            public override void OnSignalStrengthsChanged(SignalStrength signalStrength)
            {
                base.OnSignalStrengthsChanged(signalStrength);

                if (signalStrength != null) {
                    si.setSignalInfo(signalStrength);
                    Log.Debug(TAG, "getting sig strength");
                    Log.Debug(TAG, signalStrength.ToString());
                }
            }
        }
    }
}