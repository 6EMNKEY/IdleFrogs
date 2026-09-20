using UnityEngine;

[CreateAssetMenu(fileName = "TrenConfig", menuName = "IdleFrogs/TrenConfig")]
public class TrenConfigSO : ScriptableObject
{
    [Header("Moviment & Temps")]
    public float velocitatDeMoviment = 3.0f;
    public float tempsDeCarrega = 0.5f;
    public float tempsDeDescarrega = 1.0f;

    [Header("Economia")]
    public double capacitat = 50;
}
