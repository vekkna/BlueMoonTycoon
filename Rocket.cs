using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class Rocket : MonoBehaviour, ISpawnableAI {

    public static event Action OnContractFormed;

    [SerializeField]
    Transform neededGemsPanel;
    [SerializeField]
    Text priceText;
    [SerializeField]
    GemScript gemPrefab;
    [SerializeField]
    int baseMinPerGem, baseMaxPerGem;
    int price;
    internal List<GemScript> neededGems;
    Animator anim;
    [SerializeField]
    ParticleSystem[] particles;
    List<Renderer> particleRenderers;
    [SerializeField]
    float boosterIncreaseRate = 1f;
    [SerializeField]
    float maxParticleLifetime = 1f;
    internal AISpawner spawner;
    [SerializeField]
    float gemScale = 2f;
    [SerializeField]
    float minPriceChangeDelay = 5f, maxPriceChangeDelay = 15f;
    bool contractExists;
    [SerializeField]
    GameObject contractPanel;
    AIStatePattern ai;
    bool isDelivering;

    void Awake() {

        neededGems = new List<GemScript>();
        particleRenderers = new List<Renderer>();
        particles.Each(x => particleRenderers.Add(x.GetComponent<Renderer>()));
        SetParticlesLayer("Player");
        anim = GetComponent<Animator>();
        ai = GetComponent<AIStatePattern>();
    }

    void Start() {
        anim.Play("RocketSpawn");
        ToggleParticeles(false);
    }

    public void Init(AISpawner _spawner) {
        spawner = _spawner;
        ai.speed = _spawner.aiSpeed;
    }

    void SetParticlesLayer(string _layer) {

        particleRenderers.Each(x => x.sortingLayerName = _layer);
    }

    public void CreateGems() { // Called by Animation

        for (int i = 0; i < spawner.numGemsNeeded; i++) {
            CreateGem();
        }
        priceText.text = price.ToString();
        StartCoroutine(PriceSpikes());
    }

    void CreateGem() {

        var gem = Instantiate(gemPrefab) as GemScript;
        gem.SetUpGem();
        neededGems.Add(gem);
        gem.transform.SetParent(neededGemsPanel);
        gem.transform.localScale = new Vector3(2f, 1.2f, 1f); // TODO Magic numbers
        price += UnityEngine.Random.Range(spawner.minPricePerGem, spawner.maxPricePerGem);
    }

    void Launch() {
        priceText.text = "";
        StopAllCoroutines();
        ai.SwitchState(ai.idle);
        contractPanel.SetActive(false);
        spawner.NumInhabitants--;
        neededGemsPanel.gameObject.SetActive(false);
        anim.Play("RocketLaunch");
        ToggleParticeles(true);
        particles.Each(x => x.startLifetime = maxParticleLifetime);
    }

    void ToggleParticeles(bool _state) {

        particles.Each(x => x.gameObject.SetActive(_state));
    }

    void OnTriggerEnter2D(Collider2D _other) {

        if (neededGems.Count == 0) {
            return;
        }

        if (_other.gameObject.CompareTag(STRINGS.PLAYER)) {
            if (HasCorrectGems()) {
                Deliver();
                Launch();
            }
            else {
                if (!contractExists) {
                    FormContract();
                }
            }
        }
    }

    bool HasCorrectGems() {

        if (neededGems.Count > PlayerGemsPanel.singleton.availableGems.Count) {
            return false;
        }
        var neededGemTypes = neededGems.ConvertAll(x => x.gem);
        var playerGemTypes = PlayerGemsPanel.singleton.availableGems.ConvertAll(x => x.gem);
        return neededGemTypes.TrueForAll(x => playerGemTypes.Remove(x));
    }

    void Deliver() {

        if (isDelivering) {
            return;
        }

        PlayerTransactions.singleton.Earn(price);
        PlayerGemsPanel.singleton.RemoveGems(neededGems);
        isDelivering = true;
    }

    void FormContract() {

        if (isDelivering) {
            return;
        }

        contractExists = true;

        if (OnContractFormed != null) {
            OnContractFormed();
        }

        ai.SwitchState(ai.idle);
        StopAllCoroutines();
        contractPanel.SetActive(true);
    }

    void SelfDestruct() {
        Destroy(gameObject);
    }

    IEnumerator PriceSpikes() {

        int multiplier = 3;
        int originalPrice = price;
        int raisedPrice = price * multiplier;
        float ticksPerSec = (raisedPrice - price) / 2;
        var changeRate = new WaitForSeconds(1 / ticksPerSec);
        var normalScale = priceText.rectTransform.localScale;

        while (true) {

            yield return new WaitForSeconds(UnityEngine.Random.Range(5, 10));
            int oddsIn100 = 30;
            if (UnityEngine.Random.Range(0, 100) > oddsIn100) {
                continue;
            }

            priceText.rectTransform.DOScale(normalScale * 2f, 1f);

            while (price <= raisedPrice) {
                yield return changeRate;
                AlterPrice(x => x + 1);

            }

            var timeAtPeak = UnityEngine.Random.Range(5, 10);
            priceText.rectTransform.DOShakeScale(timeAtPeak, new Vector3(0.01f, 0.01f, 0f), 2, 0);
            yield return new WaitForSeconds(timeAtPeak);

            priceText.rectTransform.DOScale(normalScale, 1f);
            while (price > originalPrice) {
                yield return changeRate;
                AlterPrice(x => x - 1);
            }
        }
    }

    void AlterPrice(Func<int, int> _func) {

        price = _func(price);
        priceText.text = price.ToString();
    }
}