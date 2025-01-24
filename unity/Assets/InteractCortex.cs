using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EmotivUnityPlugin;
using UnityEngine.UI;

public class InteractCortex : MonoBehaviour
{
    
    // Please fill clientId and clientSecret of your application before starting
    private string _clientId = "tBrPSkl3CJkbFI0Bvn7Dr7KnaErkdFuNoTLQ76X6";
    private string _clientSecret = "zU1UmpIXrlftTKdkb7DpnomHjKPYC6gFCt9aS4JP0HbmF4lEHFynvNfljnJjQWsc5plek25WiCq262Zr9LEQLqBSP1YlFBuho3paESstVySAYFn4r6zLvMZjgVNRlsTf";
    private string _appName = "UnityApp";
    private string _appVersion = "3.3.0";

    EmotivUnityItf _eItf = EmotivUnityItf.Instance;
    public TimerScript tim;
    public GameObject menu_group;
    public GameObject speedText;
    
    float _timerDataUpdate = 0;
    const float TIME_UPDATE_DATA = 1f;
    bool _isDataBufferUsing = false; // default subscribed data will not saved to Data buffer
    

    [SerializeField] public InputField  HeadsetId;  
    [SerializeField] public InputField  ProfileName;   
    [SerializeField] public Toggle COMToggle;
    [SerializeField] public Toggle FEToggle;
    [SerializeField] public Text MessageLog;
    
    
    void Start()
    {
        speedText.SetActive(false);
        // init EmotivUnityItf without data buffer using
        _eItf.Init(_clientId, _clientSecret, _appName, _appVersion, _isDataBufferUsing);

        // Start
        _eItf.Start();

    }

    // Update is called once per frame
    void Update()
    {
        _timerDataUpdate += Time.deltaTime;
        if (_timerDataUpdate < TIME_UPDATE_DATA) 
            return;
        _timerDataUpdate -= TIME_UPDATE_DATA;

        if ( _eItf.MessageLog.Contains("Get Error:")) {
            // show error in red color
            MessageLog.color = Color.red;
        }
        else {
            // update message log
            MessageLog.color = Color.black;
        }
        MessageLog.text = _eItf.MessageLog;
        if (!_eItf.IsAuthorizedOK)
            return;

        // Check to call scan headset if no session is created and no scanning headset
        if (!_eItf.IsSessionCreated && !DataStreamManager.Instance.IsHeadsetScanning) {
				// Start scanning headset at headset list screen
				DataStreamManager.Instance.ScanHeadsets();
		}
        
        // Check buttons interactable
        CheckButtonsInteractable();

        // If save data to Data buffer. You can do the same EEG to get other data streams
        // Otherwise check output data at OnEEGDataReceived(), OnMotionDataReceived() ..etc..
        if (_isDataBufferUsing) {
            // get eeg data
            if (_eItf.GetNumberEEGSamples() > 0) {
                string eegHeaderStr = "EEG Header: ";
                string eegDataStr   = "EEG Data: ";
                foreach (var ele in _eItf.GetEEGChannels()) {
                    string chanStr  = ChannelStringList.ChannelToString(ele);
                    double[] data     = _eItf.GetEEGData(ele);
                    eegHeaderStr    += chanStr + ", ";
                    if (data != null && data.Length > 0)
                        eegDataStr      +=  data[0].ToString() + ", ";
                    else
                        eegDataStr      +=  "null, "; // for null value
                }
                string msgLog = eegHeaderStr + "\n" + eegDataStr;
                MessageLog.text = msgLog;
            }
        }

    }

    /// <summary>
    /// create session 
    /// </summary>
    public void onCreateSessionBtnClick() {
        Debug.Log("onCreateSessionBtnClick");
        if (!_eItf.IsSessionCreated)
        {
            _eItf.CreateSessionWithHeadset(HeadsetId.text);
            
        }
        else
        {
            UnityEngine.Debug.LogError("There is a session created.");
        }
    }


    /// <summary>
    /// subscribe data stream
    /// </summary>
    public void onSubscribeBtnClick() {
        Debug.Log("onSubscribeBtnClick: " + _eItf.IsSessionCreated + ": " + GetStreamsList().Count);
        if (_eItf.IsSessionCreated)
        {
            if (GetStreamsList().Count == 0) {
                UnityEngine.Debug.LogError("The stream name is empty. Please set a valid stream name before subscribing.");
            }
            else {
                _eItf.DataSubLog = ""; // clear data subscribing log
                _eItf.SubscribeData(GetStreamsList());
                menu_group.SetActive(false);
                speedText.SetActive(true);
                //timer start
                
                tim.StartTimer();
            }
        }
        else
        {
            UnityEngine.Debug.LogError("Must create a session first before subscribing data.");
        }
    }

    /// <summary>
    /// un-subscribe data
    /// </summary>
    public void onUnsubscribeBtnClick() {
        Debug.Log("onUnsubscribeBtnClick");
        if (GetStreamsList().Count == 0) {
            UnityEngine.Debug.LogError("The stream name is empty. Please set a valid stream name before unsubscribing.");
        }
        else {
            _eItf.DataSubLog = ""; // clear data subscribing log
            _eItf.UnSubscribeData(GetStreamsList());
        }
    }

    /// <summary>
    /// load an exited profile or create a new profile then load the profile
    /// </summary>
    public void onLoadProfileBtnClick() {
        Debug.Log("onLoadProfileBtnClick " + ProfileName.text);
        _eItf.LoadProfile(ProfileName.text);
    }

    /// <summary>
    /// unload a profile
    /// </summary>
    public void onUnLoadProfileBtnClick() {
        Debug.Log("onUnLoadProfileBtnClick " + ProfileName.text);
        _eItf.UnLoadProfile(ProfileName.text);
    }

    /// <summary>
    /// save a profile
    /// </summary>
    public void onSaveProfileBtnClick() {
        Debug.Log("onSaveProfileBtnClick " + ProfileName.text);
        _eItf.SaveProfile(ProfileName.text);
    }


    void OnApplicationQuit()
    {
        Debug.Log("Application ending after " + Time.time + " seconds");
        _eItf.Stop();
    }

    private void CheckButtonsInteractable()
    {   
        
        if (!_eItf.IsAuthorizedOK)
            return;

        if(!menu_group.activeSelf)
            return;

        Button createSessionBtn = GameObject.Find("SessionPart").transform.Find("createSessionBtn").GetComponent<Button>();
        if (!createSessionBtn.interactable)
        {
            createSessionBtn.interactable = true;
            return;
        }

        // make startRecordBtn interactable
        Button subscribeBtn = GameObject.Find("SubscribeDataPart").transform.Find("subscribeBtn").GetComponent<Button>();
        Button unsubscribeBtn = GameObject.Find("SubscribeDataPart").transform.Find("unsubscribeBtn").GetComponent<Button>();
        Button loadProfileBtn = GameObject.Find("TrainingPart").transform.Find("loadProfileBtn").GetComponent<Button>();
        Button unloadProfileBtn = GameObject.Find("TrainingPart").transform.Find("unloadProfileBtn").GetComponent<Button>();
        Button saveProfileBtn = GameObject.Find("TrainingPart").transform.Find("saveProfileBtn").GetComponent<Button>();

        subscribeBtn.interactable = _eItf.IsSessionCreated;
        unsubscribeBtn.interactable = _eItf.IsSessionCreated;
        loadProfileBtn.interactable = _eItf.IsSessionCreated;

        saveProfileBtn.interactable = _eItf.IsProfileLoaded;
        unloadProfileBtn.interactable = _eItf.IsProfileLoaded;
    }

    private List<string> GetStreamsList() {
        List<string> _streams = new List<string> {};
        if (FEToggle.isOn) {
            _streams.Add("fac");
        }
        if (COMToggle.isOn) {
            _streams.Add("com");
        }
        return _streams;
    }
}
