using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;

public class ChatManager : MonoBehaviourPunCallbacks
{
    [Header("UI References")]
    public GameObject m_Content;                  // 메시지를 담을 Content
    public TMP_InputField m_inputField;           // 입력창
    public ScrollRect m_scrollRect;               // 스크롤 영역

    private PhotonView photonview;
    private GameObject m_ContentTextPrefab;       // 복제할 메시지 오브젝트
    private string m_strUserName;

    void Start()
    {
        photonview = GetComponent<PhotonView>();
        m_ContentTextPrefab = m_Content.transform.GetChild(0).gameObject;
        m_ContentTextPrefab.SetActive(false);
        m_strUserName = PhotonNetwork.NickName;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            if (!m_inputField.isFocused)
            {
                m_inputField.ActivateInputField();
            }
            else
            {
                TrySendMessage();
            }
        }
    }

    private void TrySendMessage()
    {
        string input = m_inputField.text.Trim();
        if (string.IsNullOrEmpty(input)) return;

        string message = $"{m_strUserName} : {input}";
        photonview.RPC("RPC_Chat", RpcTarget.All, message);

        m_inputField.text = "";
        m_inputField.ActivateInputField();
    }

    [PunRPC]
    void RPC_Chat(string message)
    {
        AddChatMessage(message);
    }

    void AddChatMessage(string message)
    {
        GameObject newMsg = Instantiate(m_ContentTextPrefab, m_Content.transform);
        newMsg.SetActive(true);
        newMsg.GetComponent<TextMeshProUGUI>().text = message;

        StartCoroutine(ScrollToBottom());
    }

    IEnumerator ScrollToBottom()
    {
        yield return null;
        Canvas.ForceUpdateCanvases();
        m_scrollRect.verticalNormalizedPosition = 0f;
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        AddChatMessage($"<color=green>{newPlayer.NickName}님이 입장했습니다.</color>");
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        AddChatMessage($"<color=red>{otherPlayer.NickName}님이 퇴장했습니다.</color>");
    }
}


