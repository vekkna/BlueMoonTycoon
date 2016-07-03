using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class PlayerGemsPanel : MonoBehaviour {

    internal static PlayerGemsPanel singleton;
    // List of gems currently in the player's panel
    internal List<GemScript> availableGems;
    // Scale of the gems in the panel
    [SerializeField]
    float gemScaleX, gemScaleY;
    // Maximum number of gems the panel can hold
    [SerializeField]
    int maxGems;
    [SerializeField]
    // How fast gems slide down the panel
    float slideRate = 1f;
    [SerializeField]
    // Positions to which gems can slide in the panel
    RectTransform offTop, top, mid1, mid2, bottom, offBottom;

    private void Awake() {
        if (singleton != null && singleton != this) {
            Destroy(gameObject);
        }
        singleton = this;
        availableGems = new List<GemScript>();
    }

    public void AddGem(GemScript _gem) {

        availableGems.Add(_gem);

        _gem.gameObject.transform.SetParent(this.transform);
        _gem.GetComponent<RectTransform>().anchoredPosition = offTop.anchoredPosition;
        _gem.gameObject.transform.localScale = new Vector3(gemScaleX, gemScaleY, 1f);
        if (availableGems.Count > maxGems) {
            StartCoroutine(SlideGemDown(availableGems[0], offBottom.anchoredPosition.y));
            availableGems.RemoveAt(0);
        }

        CorrectGemPositions();
    }

    IEnumerator SlideGemDown(GemScript _gem, float _newPos) {
        var _yPos = _gem.rt.anchoredPosition.y;
        var _xPos = offTop.anchoredPosition.x;
        while (_yPos >= _newPos) {
            _yPos -= slideRate;
            _gem.rt.anchoredPosition = new Vector2(_xPos, _yPos);
            yield return null;
        }
    }

    public void CorrectGemPositions() {

        float[] points = new float[] { offTop.anchoredPosition.y, top.anchoredPosition.y, mid1.anchoredPosition.y, mid2.anchoredPosition.y, bottom.anchoredPosition.y, offBottom.anchoredPosition.y };

        for (int i = 0; i < availableGems.Count; i++) {
            var index = points.Length - i - 2;
            StartCoroutine(SlideGemDown(availableGems[i], points[index]));
        }
    }

    public void RemoveGems(List<GemScript> _gemsToRemove) {

        for (int i = 0; i < _gemsToRemove.Count; i++) {
            int index = availableGems.FindIndex(x => x.gem == _gemsToRemove[i].gem);
            availableGems[index].Fade();
            availableGems.RemoveAt(index);
        }
        DOTween.Sequence().AppendInterval(1f).OnComplete(() => CorrectGemPositions());
    }
}