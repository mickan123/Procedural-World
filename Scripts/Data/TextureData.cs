using UnityEngine;

[System.Serializable, CreateAssetMenu(menuName = "Procedural Generation Settings/TextureData")]
public class TextureData : ScriptableObject
{
    public Texture2D texture;
    public Color tint;
    
    [Range(0, 1)] public float tintStrength;
    [Range(0, 1)] public float blendStrength;

    [Range(0, 1)] public float startHeight;
    [Range(0, 1)] public float endHeight;
    [Range(0, 90)] public float startSlope;
    [Range(0, 90)] public float endSlope;

    public float textureScale = 1f;

    private readonly float minWidthHeight = 0.02f;
    private readonly float minWidthSlope = 3f;

    public TextureData(float startHeight, float endHeight, float startSlope, float endSlope)
    {
        this.startHeight = startHeight;
        this.endHeight = endHeight;
        this.startSlope = startSlope;
        this.endSlope = endSlope;
    }

#if UNITY_EDITOR

    public void OnValidate()
    {
        float minWidthSlope = 3f;
        float minWidthHeight = 0.02f;

        // Ensure values are in correct range
        this.startHeight = Mathf.Clamp(this.startHeight, 0f, 1f - minWidthHeight);
        this.endHeight = Mathf.Clamp(this.endHeight, minWidthHeight, 1f);
        this.startSlope = Mathf.Clamp(this.startSlope, 0f, 90f - minWidthSlope);
        this.endSlope = Mathf.Clamp(this.endSlope, minWidthSlope, 90f);


        // Ensure min < max
        this.startHeight = Mathf.Min(this.startHeight, this.endHeight - minWidthHeight);
        this.endHeight = Mathf.Max(this.endHeight, this.startHeight + minWidthHeight);
        this.startSlope = Mathf.Min(this.startSlope, this.endSlope - minWidthSlope);
        this.endSlope = Mathf.Max(this.endSlope, this.startSlope + minWidthSlope);
    }

#endif
}