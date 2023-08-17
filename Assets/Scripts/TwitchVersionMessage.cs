using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;

public class TwitchVersionMessage : MonoBehaviour, IPointerClickHandler
{

    private TextMeshProUGUI tmp;

    public void OnPointerClick(PointerEventData eventData)
    {
        Application.OpenURL("https://itch.io/jam/numerica-twitch-jam");
    }
}
