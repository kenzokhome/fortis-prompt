using Fortis.LAN;
using Fortis.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Fortis.UI
{
    public class UIController : Singleton<UIController>
    {
        public GameObject afterConnectionPanel;
        public Text errorText;
        private IEnumerator errorCoroutine;
        public Button readyButton;


        public void EnableErrorTimeOut(string text)
        {
            if(errorCoroutine != null) StopCoroutine(errorCoroutine);
            errorCoroutine = ErrorTimeOut(text);
            StartCoroutine(errorCoroutine);
        }

        public void EnableError(string text)
        {
            if (errorCoroutine != null) StopCoroutine(errorCoroutine);
            errorText.text = text;
            errorText.gameObject.SetActive(true);
        }

        private IEnumerator ErrorTimeOut(string text)
        {
            errorText.text = text;
            errorText.gameObject.SetActive(true);
            yield return new WaitForSeconds(3);
            errorText.gameObject.SetActive(false);
        }

        public void EnableConnectionPanel()
        {
            if (errorCoroutine != null) StopCoroutine(errorCoroutine);
            errorText.gameObject.SetActive(false);
            afterConnectionPanel.SetActive(true);
            readyButton.onClick.RemoveAllListeners();
            readyButton.onClick.AddListener(() => {
                readyButton.gameObject.SetActive(false);
                ClientLogic.instance.SendPlayerReady();
                errorText.text = "Please wait for all users to be ready";
                errorText.gameObject.SetActive(true);
            });
        }

        public void EnableConnectionResetPanel()
        {
            if (errorCoroutine != null) StopCoroutine(errorCoroutine);
            afterConnectionPanel.SetActive(true);
            readyButton.onClick.RemoveAllListeners();
            readyButton.onClick.AddListener(() => {
                readyButton.gameObject.SetActive(false);
                ClientLogic.instance.SendPlayerReset();
                DisableErrorAndConnectionPanel();
            });
        }

        public void DisableErrorAndConnectionPanel()
        {
            errorText.gameObject.SetActive(false);
            afterConnectionPanel.SetActive(false);
            readyButton.gameObject.SetActive(true);
        }
    }
}