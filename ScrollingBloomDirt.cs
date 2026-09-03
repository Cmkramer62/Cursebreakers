using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ScrollingBloomDirt : MonoBehaviour {
    [Header("Dirt")]
    [SerializeField] private Material scrollingDirtMaterial;
    [SerializeField] private Volume volume;

    [Header("Scrolling")]
    [SerializeField] private float scrollSpeedX, scrollSpeedY = 0.02f;

    private RenderTexture dirtRenderTexture;
    [SerializeField] private Texture normalBloomTexture;
    private Bloom bloom;

    // Used only by the HUB.
    [SerializeField] private bool runOnStart = false;

    private void Start() {
        // Create the texture Bloom will actually use.
        dirtRenderTexture = new RenderTexture(
            512,
            512,
            0,
            RenderTextureFormat.ARGB32
        );

        dirtRenderTexture.filterMode = FilterMode.Bilinear;
        dirtRenderTexture.wrapMode = TextureWrapMode.Repeat;
        dirtRenderTexture.Create();

        // Get Bloom from the Volume.
        if(!volume.profile.TryGet(out bloom)) {
            Debug.LogError("ScrollingBloomDirt: No Bloom found on Volume.");
            return;
        }

        if(runOnStart) AfterlifeBloomTexture();
    }

    public void AfterlifeBloomTexture() {
        // Give Bloom our generated texture.
        bloom.dirtTexture.value = dirtRenderTexture;
    }

    public void NormalBloomTexture() {
        bloom.dirtTexture.value = normalBloomTexture;
    }

    private void Update() {
        if(dirtRenderTexture == null || scrollingDirtMaterial == null)
            return;

        float offsetX = Mathf.Repeat(Time.time * scrollSpeedX, 1f);
        float offsetY = Mathf.Repeat(Time.time * scrollSpeedY, 1f);

        scrollingDirtMaterial.SetFloat("_ScrollX", offsetX);
        scrollingDirtMaterial.SetFloat("_ScrollY", offsetY);

        Graphics.Blit(null, dirtRenderTexture, scrollingDirtMaterial);
    }

    private void OnDestroy() {
        if(dirtRenderTexture != null) {
            dirtRenderTexture.Release();
            Destroy(dirtRenderTexture);
        }
    }
}