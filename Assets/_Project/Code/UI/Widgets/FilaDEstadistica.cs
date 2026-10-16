using TMPro;
using UnityEngine;
using UnityEngine.UI;
using IdleFrogs.Game;

namespace IdleFrogs.UI
{
    // Una caixa d'estadística del panell de millora: icona, nom i
    // "100k → 110k". És un prefab petit que el panell instancia tantes
    // vegades com n'hi demani la unitat: el pantà en té 4, el tren i la
    // tenda 3, i el panell no ha de saber-ho per endavant.
    public class FilaDEstadistica : MonoBehaviour
    {
        [SerializeField] private Image icona;
        [SerializeField] private TMP_Text nom;
        [SerializeField] private TMP_Text valor;

        [Header("Aspecte")]
        [Tooltip("El color del valor que tindràs després de millorar")]
        [SerializeField] private Color colorDelNouValor = new Color(0.55f, 1f, 0.35f);
        [Tooltip("Si la font no té el caràcter →, posa-hi \" > \"")]
        [SerializeField] private string fletxa = " → ";

        public void Omplir(FilaEstadistica fila, Sprite sprite)
        {
            if (icona != null)
            {
                icona.sprite = sprite;
                icona.enabled = sprite != null;
            }
            if (nom != null) nom.text = fila.nom;
            if (valor != null) valor.text = Valor(fila);
        }

        // "100k → 110k" amb el segon en verd. El panell també el fa servir per
        // al resum de la targeta, llegint-lo del prefab: així la fletxa i el
        // color es canvien en un sol lloc.
        public string Valor(FilaEstadistica fila) => fila.TeCanvi
            ? $"{fila.valor}{fletxa}<color=#{ColorUtility.ToHtmlStringRGB(colorDelNouValor)}>{fila.valorDesti}</color>"
            : fila.valor;
    }
}
