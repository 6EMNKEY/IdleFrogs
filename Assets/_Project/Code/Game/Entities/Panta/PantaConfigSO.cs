using UnityEngine;

[CreateAssetMenu(fileName = "PantaConfig", menuName = "IdleFrogs/PantaConfig")]
public class PantaConfigSO : ScriptableObject
{
    [Header("Moviment & Temps")]
    public float velocitatDeMoviment = 1.0f;
    public float tempsFinsPle = 3.0f;
    public float tempsBuidar = 3.0f;

    // Aixo es desplaçara mes tard crec
    [Header("Economia")]
    public double doblersPerPle = 10;
}