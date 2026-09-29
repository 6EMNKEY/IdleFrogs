using TMPro;
using UnityEngine;
using IdleFrogs.Game;

namespace IdleFrogs.UI
{
    // Una fila del panell de millora. És un prefab petit que el panell
    // instancia tantes vegades com files li demani la unitat: el pantà
    // en té 5, el tren 3, i el panell no ha de saber-ho per endavant.
    public class FilaDEstadistica : MonoBehaviour
    {
        [SerializeField] private TMP_Text nom;
        [SerializeField] private TMP_Text valor;
        [SerializeField] private TMP_Text delta;

        public void Omplir(FilaEstadistica fila)
        {
            if (nom != null) nom.text = fila.nom;
            if (valor != null) valor.text = fila.valor;
            if (delta != null) delta.text = fila.delta;
        }
    }
}
