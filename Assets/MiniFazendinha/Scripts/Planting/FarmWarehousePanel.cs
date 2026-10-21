using System;
using UnityEngine;
using TMPro;

namespace MiniFazendinha.Planting
{
    // Presentation only: the existing interactor remains the single tool controller.
    public sealed class FarmWarehousePanel : MonoBehaviour
    {
        [SerializeField] RectTransform panel;
        [SerializeField] CanvasGroup group;
        [SerializeField] UnityEngine.UI.Button toggleButton, closeButton;
        [SerializeField] float animationSeconds=0.18f;
        Vector2 home; float openness;
        public bool IsOpen {get;private set;}
        public UnityEngine.UI.Button ToggleButton=>toggleButton;
        public UnityEngine.UI.Button CloseButton=>closeButton;
        public RectTransform Panel=>panel;
        void Awake(){home=panel.anchoredPosition;openness=0;Apply();}
        public void Bind(Action toggle,Action close){toggleButton.onClick.AddListener(()=>toggle());closeButton.onClick.AddListener(()=>close());}
        public void SetOpen(bool value){IsOpen=value;group.blocksRaycasts=value;group.interactable=value;}
        public void HideImmediately(){IsOpen=false;openness=0;Apply();}
        void Update(){openness=Mathf.MoveTowards(openness,IsOpen?1:0,Time.unscaledDeltaTime/Mathf.Max(0.01f,animationSeconds));Apply();}
        void Apply(){float eased=1-Mathf.Pow(1-openness,3);panel.anchoredPosition=home+Vector2.right*(panel.rect.width+50)*(1-eased);group.alpha=eased;group.blocksRaycasts=IsOpen;group.interactable=IsOpen;}
    }
}