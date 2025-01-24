using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmotivUnityPlugin
{
    //

    /// <summary>
    /// EmotivUnityItf as interface for 3rd parties application work with Emotiv Unity Plugin
    /// </summary>
    public class EmotivUnityItf
    {
        //facial expression variables:
        public string UAct;
        public string LAct;
        public string EyeAct;
        public float UPow;
        public float LPow;

        private DataStreamManager _dsManager = DataStreamManager.Instance;
        private BCITraining _bciTraining = BCITraining.Instance;
        private RecordManager _recordMgr = RecordManager.Instance;
        private CortexClient   _ctxClient  = CortexClient.Instance;

        bool _isAuthorizedOK = false;
        bool _isRecording = false;

        bool _isProfileLoaded = false;
        private string _workingHeadsetId = "";
        private string _dataSubLog = ""; 
        private string _trainingLog = ""; 

        private string _messageLog = "";

        public static EmotivUnityItf Instance { get; } = new EmotivUnityItf();

        public bool IsAuthorizedOK => _isAuthorizedOK;

        public bool IsSessionCreated => _dsManager.IsSessionCreated;

        public bool IsProfileLoaded => _isProfileLoaded;

        public bool IsRecording { get => _isRecording; set => _isRecording = value; }
        public string DataSubLog { get => _dataSubLog; set => _dataSubLog = value; }
        public string TrainingLog { get => _trainingLog; set => _trainingLog = value; }
        public string MessageLog { get => _messageLog; set => _messageLog = value; }

        public class MentalComm{
            public string act = "NULL";
            public double pow = 0;
        }
        public MentalComm LatestMentalCommand { get; private set; } = new MentalComm(); //used for mental commands

        /// <summary>
        /// Set up App configuration.
        /// </summary>
        /// <param name="clientId">A clientId of Application.</param>
        /// <param name="clientSecret">A clientSecret of Application.</param>
        /// <param name="appVersion">Application version.</param>
        /// <param name="appName">Application name.</param>
        public void SetAppConfig(string clientId, string clientSecret,
                                 string appVersion, string appName,
                                 string appUrl = "", string emotivAppsPath = "")
        {
            _dsManager.SetAppConfig(clientId, clientSecret, appVersion, appName);
        }

    
        public void Init(string clientId, string clientSecret, string appName, 
                         string appVersion = "", bool isDataBufferUsing = true)
        {
            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                UnityEngine.Debug.LogError("The clientId or clientSecret is empty. Please fill them before starting.");
                return;
            }
            _dsManager.SetAppConfig(clientId, clientSecret, appVersion, appName);
            _dsManager.IsDataBufferUsing = isDataBufferUsing;

            _bciTraining.Init();

           
            _dsManager.LicenseValidTo += OnLicenseValidTo;
            _dsManager.SessionActivatedOK += OnSessionActiveOK;

            
            if (!isDataBufferUsing)
            {
                _dsManager.EEGDataReceived += OnEEGDataReceived;
                _dsManager.MotionDataReceived += OnMotionDataReceived;
                _dsManager.DevDataReceived += OnDevDataReceived;
                _dsManager.PerfDataReceived += OnPerfDataReceived;
                _dsManager.BandPowerDataReceived += OnBandPowerDataReceived;
                _dsManager.InformSuccessSubscribedData += OnInformSuccessSubscribedData;

            }

            _dsManager.FacialExpReceived += OnFacialExpReceived;
            _dsManager.MentalCommandReceived += OnMentalCommandReceived;
            _dsManager.SysEventsReceived += OnSysEventsReceived;

           
            _recordMgr.informMarkerResult += OnInformMarkerResult;
            _recordMgr.informStartRecordResult += OnInformStartRecordResult;
            _recordMgr.informStopRecordResult += OnInformStopRecordResult;


            _bciTraining.InformLoadUnLoadProfileDone += OnInformLoadUnLoadProfileDone;
            _ctxClient.ErrorMsgReceived             += MessageErrorRecieved;
        }


        public void Start()
        {
            _dsManager.StartAuthorize();
        }


        public void Stop()
        {
            _dsManager.Stop();
            _isAuthorizedOK = false;
            _isProfileLoaded = false;
            _workingHeadsetId = "";
        }


        public void CreateSessionWithHeadset(string headsetId)
        {
           
            if (_isAuthorizedOK)
                _dsManager.StartDataStream(new List<string>(), headsetId);
            else
                UnityEngine.Debug.LogWarning("Please wait authorize successfully before creating session with headset " + headsetId);
        }

        public void SubscribeData(List<string> streamNameList)
        {
            if (_dsManager.IsSessionCreated)
                _dsManager.SubscribeMoreData(streamNameList);
            else
                UnityEngine.Debug.LogWarning("Please wait session created successfully before subscribe data ");
        }

  
        public void UnSubscribeData(List<string> streamNameList)
        {
            _dsManager.UnSubscribeData(streamNameList);
        }

        

        public List<Channel_t> GetEEGChannels()
        {
            return _dsManager.GetEEGChannels();
        }

        public double[] GetEEGData(Channel_t chan)
        {
            return _dsManager.GetEEGData(chan);
        }


        public int GetNumberEEGSamples()
        {
            return _dsManager.GetNumberEEGSamples();
        }


        public double[] GetMotionData(Channel_t chan)
        {
            return _dsManager.GetMotionData(chan);
        }


        public List<Channel_t> GetMotionChannels()
        {
            return _dsManager.GetMotionChannels();
        }


        public int GetNumberMotionSamples()
        {
            return _dsManager.GetNumberMotionSamples();
        }

        public List<string> GetBandPowerLists()
        {
            return _dsManager.GetBandPowerLists();
        }


        public int GetNumberPowerBandSamples()
        {
            return _dsManager.GetNumberPowerBandSamples();
        }

        public double GetBandPower(Channel_t chan, BandPowerType _band)
        {
            switch (_band)
            {
                case BandPowerType.Thetal:
                    return _dsManager.GetThetaData(chan);
                case BandPowerType.Alpha:
                    return _dsManager.GetAlphaData(chan);
                case BandPowerType.BetalL:
                    return _dsManager.GetLowBetaData(chan);
                case BandPowerType.BetalH:
                    return _dsManager.GetHighBetaData(chan);
                case BandPowerType.Gamma:
                    return _dsManager.GetGammaData(chan);
                default:
                    return -1;
            }
        }


        public List<string> GetPMLists()
        {
            return _dsManager.GetPMLists();
        }


        public int GetNumberPMSamples()
        {
            return _dsManager.GetNumberPMSamples();
        }


        public double GetPMData(string label)
        {
            return _dsManager.GetPMData(label);
        }


        public double GetContactQuality(Channel_t channel)
        {
            return _dsManager.GetContactQuality(channel);
        }

  
        public double GetContactQuality(int channelId)
        {
            return _dsManager.GetContactQuality(channelId);
        }


        public int GetNumberCQSamples()
        {
            return _dsManager.GetNumberCQSamples();
        }

        public void StartRecord(string title, string description = null,
                                 string subjectName = null, List<string> tags = null)
        {
            _recordMgr.StartRecord(title, description, subjectName, tags);
        }


        public void StopRecord()
        {
            _recordMgr.StopRecord();
        }

        public void InjectMarker(string markerLabel, string markerValue)
        {
            _recordMgr.InjectMarker(markerLabel, markerValue);
        }


        public void UpdateMarker()
        {
            _recordMgr.UpdateMarker(); 
        }


        public void LoadProfile(string profileName)
        {
            if (!string.IsNullOrEmpty(_workingHeadsetId))
                _bciTraining.LoadProfileWithHeadset(profileName, _workingHeadsetId);
            else
                UnityEngine.Debug.LogError("LoadProfile: Please create a session with a headset first.");
        }


        public void UnLoadProfile(string profileName)
        {
            if (!string.IsNullOrEmpty(_workingHeadsetId))
                _bciTraining.UnLoadProfile(profileName, _workingHeadsetId);
            else
                UnityEngine.Debug.LogError("UnLoadProfile: Please create a session with a headset first.");
        }


        public void SaveProfile(string profileName)
        {
            if (!string.IsNullOrEmpty(_workingHeadsetId))
                _bciTraining.SaveProfile(profileName, _workingHeadsetId);
            else
                UnityEngine.Debug.LogError("SaveProfile: Please create a session with a headset first.");
        }


        public void StartMCTraining(string action)
        {
            if (_isProfileLoaded)
            {
                _bciTraining.StartTraining(action, "mentalCommand");
            }
            else
            {
                UnityEngine.Debug.LogError("Please load a profile before starting training");
            }
        }


        public void AcceptMCTraining()
        {
            _bciTraining.AcceptTraining("mentalCommand");
        }


        public void RejectMCTraining()
        {
            _bciTraining.RejectTraining("mentalCommand");
        }


        public void EraseMCTraining(string action)
        {
            _bciTraining.EraseTraining(action, "mentalCommand");
        }


        public void ResetMCTraining(string action)
        {
            _bciTraining.ResetTraining(action, "mentalCommand");
        }


        public void StartFETraining(string action)
        {
            if (_isProfileLoaded)
            {
                _bciTraining.StartTraining(action, "facialExpression");
            }
            else
            {
                UnityEngine.Debug.LogError("Please load a profile before starting training");
            }
        }


        public void AcceptFETraining()
        {
            _bciTraining.AcceptTraining("facialExpression");
        }


        public void RejectFETraining()
        {
            _bciTraining.RejectTraining("facialExpression");
        }


        public void EraseFETraining(string action)
        {
            _bciTraining.EraseTraining(action, "facialExpression");
        }

        public void ResetFETraining(string action)
        {
            _bciTraining.ResetTraining(action, "facialExpression");
        }


        private void OnLicenseValidTo(object sender, DateTime validTo)
        {
            UnityEngine.Debug.Log("OnLicenseValidTo: the license valid to " + Utils.ISODateTimeToString(validTo));
            _isAuthorizedOK = true;
            _messageLog = "Authorizing process done.";
        }

        private void OnSessionActiveOK(object sender, string headsetId)
        {
            _workingHeadsetId = headsetId;
            _messageLog = "A session working with " + headsetId + " is activated successfully.";
        }

        private void OnInformLoadUnLoadProfileDone(object sender, bool isProfileLoaded)
        {
            _isProfileLoaded = isProfileLoaded;
            if (isProfileLoaded)
            {
                _messageLog = "The profile is loaded successfully.";
            }
            else {
                _messageLog = "The profile is unloaded successfully.";
            }
        }

        private void OnInformStartRecordResult(object sender, Record record)
        {
            UnityEngine.Debug.Log("OnInformStartRecordResult recordId: " + record.Uuid + ", title: "
                + record.Title + ", startDateTime: " + record.StartDateTime);
            _isRecording = true;
            _messageLog = "The record " + record.Title + " is created at " + record.StartDateTime;

        }

        private void OnInformStopRecordResult(object sender, Record record)
        {
            UnityEngine.Debug.Log("OnInformStopRecordResult recordId: " + record.Uuid + ", title: "
                + record.Title + ", startDateTime: " + record.StartDateTime + ", endDateTime: " + record.EndDateTime);
            _isRecording = false;
            _messageLog = "The record " + record.Title + " is ended at " + record.EndDateTime;

        }

        private void OnInformMarkerResult(object sender, JObject markerObj)
        {
            UnityEngine.Debug.Log("OnInformMarkerResult");
            _messageLog = "The marker " + markerObj["uuid"].ToString() + ", label: " 
                + markerObj["label"].ToString() + ", value: " + markerObj["value"].ToString()
                + ", type: " + markerObj["type"].ToString() + ", started at: " + markerObj["startDatetime"].ToString();
        }

        private void OnInformSuccessSubscribedData(object sender, List<string> successStreams)
        {
            string tmpText = "The streams: ";
            foreach (var item in successStreams)
            {
                tmpText = tmpText + item + "; ";
            }
            tmpText = tmpText + " are subscribed successfully. The output data will be shown on the console log.";
            _messageLog = tmpText;
        }

        private void OnBandPowerDataReceived(object sender, ArrayList e)
        {
            string dataText = "pow data: ";
            foreach (var item in e) {
                dataText += item.ToString() + ",";
            }
            UnityEngine.Debug.Log(dataText);
        }

        private void OnPerfDataReceived(object sender, ArrayList e)
        {
            string dataText = "met data: ";
            foreach (var item in e) {
                dataText += item.ToString() + ",";
            }
            UnityEngine.Debug.Log(dataText);
        }

        private void OnDevDataReceived(object sender, ArrayList e)
        {
            string dataText = "dev data: ";
            foreach (var item in e) {
                dataText += item.ToString() + ",";
            }
            UnityEngine.Debug.Log(dataText);
        }

        private void OnMotionDataReceived(object sender, ArrayList e)
        {
            string dataText = "mot data: ";
            foreach (var item in e) {
                dataText += item.ToString() + ",";
            }
            UnityEngine.Debug.Log(dataText);
        }

        private void OnEEGDataReceived(object sender, ArrayList e)
        {
            string dataText = "eeg data: ";
            foreach (var item in e) {
                dataText += item.ToString() + ",";
            }
            UnityEngine.Debug.Log(dataText);
        }

        private void OnSysEventsReceived(object sender, SysEventArgs data)
        {
            string dataText = "sys data: " + data.Detection + ", event: " + data.EventMessage + ", time " + data.Time.ToString();
            UnityEngine.Debug.Log(dataText);
            _messageLog = dataText;
        }

        private void OnMentalCommandReceived(object sender, MentalCommandEventArgs data)
        {
            string dataText = "com data: " + data.Act + ", power: " + data.Pow.ToString() + ", time " + data.Time.ToString();
            UnityEngine.Debug.Log(dataText);
            //added:
            LatestMentalCommand.act = data.Act;
            LatestMentalCommand.pow = data.Pow;
            
        }

        private void OnFacialExpReceived(object sender, FacEventArgs data)
        {
            string dataText = "fac data: eye act " + data.EyeAct+ ", upper act: " +
                                data.UAct + ", upper act power " + data.UPow.ToString() + ", lower act: " +
                                data.LAct + ", lower act power " + data.LPow.ToString() + ", time: " + data.Time.ToString();
            UnityEngine.Debug.Log(dataText);
            //added:
            UAct = data.UAct;
            LAct = data.LAct;
            EyeAct = data.EyeAct;
            LPow = (float)data.LPow;
            UPow = (float)data.UPow;
        }

        private void MessageErrorRecieved(object sender, ErrorMsgEventArgs errorInfo)
        {
            string message  = errorInfo.MessageError;
            string method   = errorInfo.MethodName;
            int errorCode   = errorInfo.Code;

            _messageLog = "Get Error: errorCode " + errorCode.ToString() + ", message: " + message + ", API: " + method;  
        }

    }
}
