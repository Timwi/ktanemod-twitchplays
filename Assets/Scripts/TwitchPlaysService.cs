﻿using Newtonsoft.Json;
using UnityEngine;

public class TwitchPlaysService : MonoBehaviour
{
    public class ModSettingsJSON
    {
        public string authToken;
        public string userName;
        public string channelName;
        public string serverName;
        public int serverPort;
    }

    public BombMessageResponder bombMessageResponder = null;
    public PostGameMessageResponder postGameMessageResponder = null;
    public MissionMessageResponder missionMessageResponder = null;
    public MiscellaneousMessageResponder miscellaneousMessageResponder = null;

    private KMGameInfo _gameInfo = null;
    private KMModSettings _modSettings = null;
    private IRCConnection _ircConnection = null;
    private CoroutineQueue _coroutineQueue = null;
    private CoroutineCanceller _coroutineCanceller = null;

    private MessageResponder _activeMessageResponder = null;

    private void Start()
    {
        _gameInfo = GetComponent<KMGameInfo>();
        _gameInfo.OnStateChange += OnStateChange;

        _modSettings = GetComponent<KMModSettings>();

        ModSettingsJSON settings = JsonConvert.DeserializeObject<ModSettingsJSON>(_modSettings.Settings);
        if (settings == null)
        {
            Debug.LogError("[TwitchPlays] Failed to read connection settings from mod settings.");
            return;
        }

        _ircConnection = new IRCConnection(settings.authToken, settings.userName, settings.channelName, settings.serverName, settings.serverPort);
        _ircConnection.Connect();

        _coroutineQueue = GetComponent<CoroutineQueue>();
        _coroutineCanceller = new CoroutineCanceller();

        SetupResponder(bombMessageResponder);
        SetupResponder(postGameMessageResponder);
        SetupResponder(missionMessageResponder);
        SetupResponder(miscellaneousMessageResponder);
    }

    private void Update()
    {
        if (_ircConnection != null)
        {
            _ircConnection.Update();
        }
    }

    private void OnDestroy()
    {
        if (_ircConnection != null)
        {
            _ircConnection.Disconnect();
        }
    }

    private void OnStateChange(KMGameInfo.State state)
    {
        if (_ircConnection == null)
        {
            return;
        }

        StopEveryCoroutine();

        if (_activeMessageResponder != null)
        {
            _activeMessageResponder.gameObject.SetActive(false);
        }

        _activeMessageResponder = GetActiveResponder(state);

        if (_activeMessageResponder != null)
        {
            _activeMessageResponder.gameObject.SetActive(true);
        }        
    }

    private void StopEveryCoroutine()
    {
        _coroutineQueue.StopQueue();
        _coroutineQueue.CancelFutureSubcoroutines();
        StopAllCoroutines();
    }

    private void SetupResponder(MessageResponder responder)
    {
        if (responder != null)
        {
            responder.SetupResponder(_ircConnection, _coroutineQueue, _coroutineCanceller);
        }
    }

    private MessageResponder GetActiveResponder(KMGameInfo.State state)
    {
        switch (state)
        {
            case KMGameInfo.State.Gameplay:
                return bombMessageResponder;

            case KMGameInfo.State.Setup:
                return missionMessageResponder;

            case KMGameInfo.State.PostGame:
                return postGameMessageResponder;

            default:
                return null;
        }
    }
}