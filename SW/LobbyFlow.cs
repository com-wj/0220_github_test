using Fusion;
using Fusion.Sockets;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyFlow : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("Fusion Prefabs")]
    [SerializeField] private NetworkRunner runnerPrefab;

    private NetworkRunner _currentRunner;

    [Header("Panels")]
    [SerializeField] private GameObject mainLobbyPanel;
    [SerializeField] private GameObject roomPanel;

    [Header("MainLobby UI")]
    [SerializeField] private TMP_InputField roomNumberInput;
    [SerializeField] private Button createRoomButton;
    [SerializeField] private Button joinButton;
    [SerializeField] private TMP_Text mainStatusText;

    [Header("Room UI")]
    [SerializeField] private TMP_Text roomNumberText;
    [SerializeField] private Button startButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private TMP_Text roomStatusText;
    [SerializeField] private TMP_Text playerListText;

    [Header("Scene")]
    [SerializeField] private int gameSceneBuildIndex = 1;

    private void Awake()
    {
        ShowMainLobby("Ready to connect.");
    }

    private void OnEnable()
    {
        createRoomButton.onClick.AddListener(() => StartGame(GameMode.Host));
        joinButton.onClick.AddListener(() => StartGame(GameMode.Client));
        startButton.onClick.AddListener(OnClickStartGameScene);
        exitButton.onClick.AddListener(OnClickExitRoom);
    }

    // ==================================================================================
    // 1. 게임 세션 시작 로직 (Host/Client 공통)
    // ==================================================================================
    private async void StartGame(GameMode mode)
    {
        // 1. UI 잠금 및 상태 표시
        SetInteractable(false);
        string sessionName = (mode == GameMode.Host) ? GenerateRoomNumber4Digits() : roomNumberInput.text.Trim();

        if (string.IsNullOrEmpty(sessionName))
        {
            mainStatusText.text = "Please enter a Room Number.";
            SetInteractable(true);
            return;
        }

        mainStatusText.text = $"{mode} starting... ({sessionName})";

        // 2. 기존 Runner가 있다면 정리 (안전장치)
        if (_currentRunner != null)
        {
            await _currentRunner.Shutdown();
            if (_currentRunner != null) Destroy(_currentRunner.gameObject);
        }

        // 3. 새 Runner 생성 (프리팹에서 인스턴스화)
        _currentRunner = Instantiate(runnerPrefab);

        // 중요: 이 LobbyFlow 스크립트가 콜백(PlayerJoined 등)을 받을 수 있도록 등록
        _currentRunner.AddCallbacks(this);
        _currentRunner.ProvideInput = true;

        // 4. StartGameArgs 설정
        var args = new StartGameArgs
        {
            GameMode = mode,
            SessionName = sessionName,
            SceneManager = _currentRunner.GetComponent<NetworkSceneManagerDefault>(),
        };

        // 5. Fusion 시작
        var result = await _currentRunner.StartGame(args);

        // 6. 결과 처리
        if (this == null) return;

        if (result.Ok)
        {
            DontDestroyOnLoad(_currentRunner.gameObject);

            ShowRoom(sessionName, "Waiting for players...");
            startButton.interactable = _currentRunner.IsServer;
        }
        else
        {
            ShowMainLobby($"Failed: {result.ShutdownReason}");
            if (_currentRunner != null) Destroy(_currentRunner.gameObject);
            _currentRunner = null;
            SetInteractable(true);
        }
    }

    // ==================================================================================
    // 2. 방 내부 로직 (게임 씬 진입 / 나가기)
    // ==================================================================================

    // [Host Only] 게임 씬으로 이동
    private void OnClickStartGameScene()
    {
        if (_currentRunner != null && _currentRunner.IsServer)
        {
            // Runner가 SceneManager를 통해 모든 클라이언트의 씬을 로드합니다.
            // 이 시점에서 Lobby 씬은 언로드되고, LobbyFlow UI도 파괴됩니다.
            _currentRunner.LoadScene(SceneRef.FromIndex(gameSceneBuildIndex));
        }
    }

    // 방 나가기 (Shutdown)
    private void OnClickExitRoom()
    {
        StartCoroutine(ExitRoomRoutine());
    }

    private IEnumerator ExitRoomRoutine()
    {
        Debug.Log("Exit sequence started...");

        // 1.중복 클릭 방지
        if (exitButton) exitButton.interactable = false;
        if (roomStatusText) roomStatusText.text = "Disconnecting...";

        // 2. Runner 정리
        if (_currentRunner != null)
        {
            // 종료 신호 보냄
            _currentRunner.Shutdown();
            yield return null;

            // Runner 객체가 여전히 있다면 파괴
            if (_currentRunner != null)
            {
                Destroy(_currentRunner.gameObject);
            }
        }

        // 참조 변수 초기화
        _currentRunner = null;

        // 3. 물리적인 시간 대기 (0.1초)
        yield return new WaitForSeconds(0.1f);

        Debug.Log("Reloading Scene...");

        // 4. 씬 리로드
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // ==================================================================================
    // 3. UI 헬퍼 메서드
    // ==================================================================================

    private void ShowMainLobby(string msg)
    {
        if (mainLobbyPanel) mainLobbyPanel.SetActive(true);
        if (roomPanel) roomPanel.SetActive(false);
        if (mainStatusText) mainStatusText.text = msg;
    }

    private void ShowRoom(string session, string msg)
    {
        if (mainLobbyPanel) mainLobbyPanel.SetActive(false);
        if (roomPanel) roomPanel.SetActive(true);
        if (roomNumberText) roomNumberText.text = session;
        if (roomStatusText) roomStatusText.text = msg;

        RefreshPlayerList();
    }

    private void SetInteractable(bool state)
    {
        createRoomButton.interactable = state;
        joinButton.interactable = state;
        roomNumberInput.interactable = state;
    }

    private void RefreshPlayerList()
    {
        if (_currentRunner == null || !_currentRunner.IsRunning)
        {
            playerListText.text = "";
            return;
        }

        var sb = new StringBuilder();
        int count = 0;
        foreach (var p in _currentRunner.ActivePlayers)
        {
            count++;
            sb.AppendLine($"Player {p.PlayerId} {(p == _currentRunner.LocalPlayer ? "(You)" : "")}");
        }
        playerListText.text = sb.ToString();
        roomStatusText.text = $"Players: {count}";
    }

    private static string GenerateRoomNumber4Digits()
    {
        return UnityEngine.Random.Range(1000, 9999).ToString();
    }

    // ==================================================================================
    // 4. Fusion Callbacks (INetworkRunnerCallbacks)
    // ==================================================================================

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        RefreshPlayerList();
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        RefreshPlayerList();

        // 만약 Host가 나갔다면 클라이언트도 정리하는 로직이 필요할 수 있음 (여기선 생략)
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        // 이미 종료 처리가 진행 중이거나, 객체가 파괴되었다면 무시
        if (this == null || gameObject == null) return;

        // 만약 의도치 않은 종료(인터넷 끊김 등)라면 로그 표시 후 씬 리로드
        Debug.Log($"OnShutdown: {shutdownReason}");

        // UI가 아직 '방 내부'를 보여주고 있다면 메인으로 튕겨내기
        if (roomPanel.activeSelf)
        {
            // 에러 메시지를 보여주기 위해 씬을 리로드하지 않고 UI만 전환할 수도 있지만,
            // 가장 안전한 건 역시 리로드입니다. (여기서는 UI 전환만 예시로 둡니다)
            ShowMainLobby($"Disconnected: {shutdownReason}");

            if (_currentRunner != null)
            {
                Destroy(_currentRunner.gameObject);
                _currentRunner = null;
            }

            SetInteractable(true);
        }
    }

    // 사용하지 않는 콜백들 (인터페이스 구현 필수)
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ArraySegment<byte> data) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
}
