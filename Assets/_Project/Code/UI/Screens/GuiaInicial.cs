using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using IdleFrogs.Game;

namespace IdleFrogs.UI
{
    // La guia dels primers minuts: compra el pantà, toca una granota, toca el
    // tren, ven, estalvia i millora, desbloqueja i contracta el primer
    // encarregat. Acaba just quan el joc passa a anar sol, que és el moment
    // que la gent nova no trobaria per si mateixa.
    //
    // Cada pas té tres preguntes que es fan cada frame mirant l'estat del joc
    // (no es compta "has tocat el que et deia"):
    //   · fet      → el resultat ja s'ha aconseguit: següent pas
    //   · enCurs   → l'acció ja s'ha fet i s'espera el resultat: la fletxa
    //                desapareix i el text explica què està passant
    //   · objectiu → què assenyala la fletxa
    // Si el jugador s'avança i fa coses en un altre ordre, la guia salta sola
    // fins on és.
    //
    // Abans de poder millorar o contractar cal estalviar 100 doblers, i això
    // vol dir fer el cicle granota → tren → tenda unes quantes vegades. En
    // aquests passos la fletxa va assenyalant el següent que es pot tocar
    // del cicle, i el text diu quant falta.
    public class GuiaInicial : MonoBehaviour
    {
        [Header("Bombolla de text (al Canvas)")]
        [SerializeField] private GameObject arrel;
        [SerializeField] private TMP_Text text;
        [Tooltip("Amaga la bombolla i la fletxa fins al pas següent")]
        [SerializeField] private Button botoSaltar;

        [Header("Fletxa al món (opcional)")]
        [Tooltip("Un sprite qualsevol. Es col·loca damunt del que s'ha de tocar.")]
        [SerializeField] private Transform fletxa;
        [SerializeField] private Vector3 desplacamentDeLaFletxa = new Vector3(0f, 1.1f, 0f);
        [SerializeField] private float balanceig = 0.15f;

        [Header("Referències")]
        [SerializeField] private LlistaDePantans llistaDePantans;
        [SerializeField] private ControladorTren tren;
        [SerializeField] private Estacio estacio;
        [SerializeField] private Tenda tenda;

        [Header("Final")]
        [SerializeField] private float segonsDelMissatgeFinal = 4f;

        private struct Pas
        {
            public Func<string> text;
            public Func<bool> fet;
            public Func<bool> enCurs;        // null = el pas no té espera
            public Func<string> textEnCurs;
            public Func<Transform> objectiu;
        }

        private Pas[] _passos;
        private float _finalFins = -1f;
        private int _pasSaltat = -1;    // el pas que el jugador ha amagat amb "Saltar"
        private readonly List<ControladorGranota> _granotes = new List<ControladorGranota>();

        // ---------------------------------------------------------------
        // Dreceres a l'estat del joc
        // ---------------------------------------------------------------

        private Panta PrimerPanta =>
            llistaDePantans != null && llistaDePantans.Quantitat > 0 ? llistaDePantans.Obtenir(0) : null;

        private GestorDEncarregats Gestor => GestorDEncarregats.Instancia;

        private static double Doblers =>
            CurrencyManager.Instancia != null ? CurrencyManager.Instancia.doblersActuals : 0;

        private double CostDeMillorarElPanta =>
            PrimerPanta != null ? PrimerPanta.CostDeMillora(1) : 0;

        private double PreuDelPrimerEncarregat =>
            Gestor != null ? Gestor.PreuDeLaSeguentCompra(TipusDeLloc.Panta) : 0;

        private bool PanellDeMilloraObert =>
            PanellDeMillora.Instancia != null && PanellDeMillora.Instancia.EstaObert;

        private bool PanellDEncarregatsObert =>
            PanellDEncarregats.Instancia != null && PanellDEncarregats.Instancia.EstaObert;

        private ClientDelRestaurant ClientQueEspera =>
            tenda != null ? tenda.ClientQueEspera : null;

        private bool AlgunaGranotaTreballa
        {
            get
            {
                if (PrimerPanta == null) return false;
                PrimerPanta.GetComponentsInChildren(_granotes);   // només les actives
                foreach (ControladorGranota g in _granotes)
                    if (g.estatActual != EstatGranota.Idle) return true;
                return false;
            }
        }

        private Transform GranotaQuieta
        {
            get
            {
                if (PrimerPanta == null) return null;
                PrimerPanta.GetComponentsInChildren(_granotes);
                foreach (ControladorGranota g in _granotes)
                    if (g.estatActual == EstatGranota.Idle) return g.transform;
                return null;
            }
        }

        // El següent que es pot tocar del cicle, mirant des del final: el que
        // ja està a punt de convertir-se en doblers va primer. Si tot treballa,
        // no hi ha res a tocar i la fletxa s'amaga.
        private Transform SeguentDelCicle()
        {
            ClientDelRestaurant c = ClientQueEspera;
            if (c != null && estacio != null && estacio.DoblersEmmagatzemats > 0)
                return c.transform;

            if (tren != null && tren.estatActual == EstatTren.Esperant &&
                llistaDePantans != null && llistaDePantans.IndexMesLlunyaAmbCarrega() >= 0)
                return tren.transform;

            return GranotaQuieta;
        }

        // "Estalvia 100 per millorar el pantà."
        private static string Falten(double preu, string clauDelPerQue) =>
            Textos.F("guia.estalvia", FormatadorDeNombres.Doblers(preu) + Icones.Moneda, Textos.T(clauDelPerQue));

        // ---------------------------------------------------------------
        // Els passos
        // ---------------------------------------------------------------

        private void Awake()
        {
            if (arrel != null)
            {
                arrel.SetActive(false);
                // La bombolla és només informativa: no ha de tapar mai els tocs
                // al món, que és el que impedia comprar el pantà. Menys el botó
                // de saltar, que sí que s'ha de poder tocar.
                foreach (Graphic g in arrel.GetComponentsInChildren<Graphic>(true))
                    if (botoSaltar == null || !g.transform.IsChildOf(botoSaltar.transform)) g.raycastTarget = false;
            }
            if (fletxa != null) fletxa.gameObject.SetActive(false);
            if (botoSaltar != null) botoSaltar.onClick.AddListener(Saltar);

            _passos = new[]
            {
                new Pas {
                    text = () => Textos.T("guia.comprar"),
                    fet = () => PrimerPanta != null && PrimerPanta.EstaComprat,
                    objectiu = () => Fill<BotoDeCompra>(PrimerPanta)
                },
                new Pas {
                    text = () => Textos.T("guia.granota"),
                    fet = () => PrimerPanta != null && PrimerPanta.DoblersALaCapsa > 0,
                    enCurs = () => AlgunaGranotaTreballa,
                    textEnCurs = () => Textos.T("guia.granota.espera"),
                    objectiu = () => GranotaQuieta
                },
                new Pas {
                    text = () => Textos.T("guia.tren"),
                    fet = () => (estacio != null && estacio.DoblersEmmagatzemats > 0) ||
                                (tenda != null && tenda.TotalVenut > 0),
                    enCurs = () => tren != null && (tren.estatActual != EstatTren.Esperant || tren.CarregaActual > 0),
                    textEnCurs = () => Textos.T("guia.tren.espera"),
                    objectiu = () => tren != null ? tren.transform : null
                },
                new Pas {
                    text = () => Textos.T("guia.client"),
                    fet = () => tenda != null && tenda.TotalVenut > 0,
                    enCurs = () => tenda != null && tenda.AlgunClientMenja,
                    textEnCurs = () => Textos.T("guia.client.espera"),
                    objectiu = () => ClientQueEspera != null ? ClientQueEspera.transform : null
                },
                new Pas {
                    text = () => Doblers < CostDeMillorarElPanta
                        ? Falten(CostDeMillorarElPanta, "guia.per_millorar")
                        : Textos.T("guia.millora"),
                    fet = () => PrimerPanta != null && PrimerPanta.Nivell >= 2,
                    enCurs = () => PanellDeMilloraObert && Doblers >= CostDeMillorarElPanta,
                    textEnCurs = () => Textos.T("guia.millora.espera"),
                    objectiu = () => Doblers < CostDeMillorarElPanta
                        ? SeguentDelCicle()
                        : Fill<BotoDeMillora>(PrimerPanta)
                },
                new Pas {
                    text = () => Textos.F("guia.nivell", Gestor != null ? Gestor.NivellDeDesbloqueig : 5),
                    fet = () => Gestor != null && Gestor.EstanDesbloquejats,
                    enCurs = () => PanellDeMilloraObert && Doblers >= CostDeMillorarElPanta,
                    textEnCurs = () => Textos.T("guia.nivell.espera"),
                    objectiu = () => Doblers < CostDeMillorarElPanta
                        ? SeguentDelCicle()
                        : Fill<BotoDeMillora>(PrimerPanta)
                },
                new Pas {
                    text = () => Doblers < PreuDelPrimerEncarregat
                        ? Falten(PreuDelPrimerEncarregat, "guia.per_contractar")
                        : Textos.T("guia.contractar"),
                    fet = () => Gestor != null && Gestor.Posseits.Count > 0,
                    enCurs = () => PanellDEncarregatsObert && Doblers >= PreuDelPrimerEncarregat,
                    textEnCurs = () => Textos.T("guia.contractar.espera"),
                    objectiu = () => Doblers < PreuDelPrimerEncarregat
                        ? SeguentDelCicle()
                        : Fill<SiluetaDEncarregat>(PrimerPanta)
                },
                new Pas {
                    text = () => Textos.T("guia.assignar"),
                    fet = () => Gestor != null && Gestor.HiHaAlgunAssignat,
                    enCurs = () => PanellDEncarregatsObert,
                    textEnCurs = () => Textos.T("guia.assignar.espera"),
                    objectiu = () => Fill<SiluetaDEncarregat>(PrimerPanta)
                },
            };
        }

        // Amaga el pas d'ara. Quan el joc passi al següent, la guia torna.
        private void Saltar()
        {
            if (_finalFins > 0f) { _finalFins = -1f; Amagar(); return; }
            if (GameManager.Instancia != null) _pasSaltat = GameManager.Instancia.PasDeLaGuia;
            Amagar();
        }

        private static Transform Fill<T>(Component pare) where T : Component
        {
            if (pare == null) return null;
            T c = pare.GetComponentInChildren<T>();   // només els actius
            return c != null ? c.transform : null;
        }

        // ---------------------------------------------------------------
        // Bucle
        // ---------------------------------------------------------------

        private void Update()
        {
            GameManager gm = GameManager.Instancia;
            if (gm == null || !gm.PartidaCarregada) { Amagar(); return; }

            if (_finalFins > 0f)
            {
                if (Time.unscaledTime >= _finalFins) { _finalFins = -1f; Amagar(); }
                return;
            }

            int pas = gm.PasDeLaGuia;
            if (pas >= _passos.Length) { Amagar(); return; }

            // Si el jugador s'ha avançat, saltem fins al darrer pas ja fet.
            int nou = pas;
            for (int i = _passos.Length - 1; i >= pas; i--)
            {
                if (_passos[i].fet()) { nou = i + 1; break; }
            }

            if (nou != pas)
            {
                gm.PasDeLaGuia = nou;
                if (nou >= _passos.Length)
                {
                    EnsenyarFinal();
                    gm.SaveGameData();
                    return;
                }
            }

            // Amagat amb "Saltar": fins que no s'arribi al pas següent, res.
            if (nou == _pasSaltat) { Amagar(); return; }

            Ensenyar(_passos[nou]);
        }

        private void Ensenyar(Pas pas)
        {
            if (arrel != null && !arrel.activeSelf) arrel.SetActive(true);

            // Acció feta, esperant el resultat: la fletxa fora, perquè ja no
            // hi ha res a tocar, i el text diu què està passant.
            bool esperant = pas.enCurs != null && pas.enCurs();
            if (text != null)
                text.text = esperant && pas.textEnCurs != null ? pas.textEnCurs() : pas.text();

            if (fletxa == null) return;

            Transform objectiu = esperant ? null : pas.objectiu();
            if (objectiu == null)
            {
                if (fletxa.gameObject.activeSelf) fletxa.gameObject.SetActive(false);
                return;
            }

            if (!fletxa.gameObject.activeSelf) fletxa.gameObject.SetActive(true);
            float bot = Mathf.Sin(Time.unscaledTime * 4f) * balanceig;
            fletxa.position = objectiu.position + desplacamentDeLaFletxa + new Vector3(0f, bot, 0f);
        }

        private void EnsenyarFinal()
        {
            if (arrel != null) arrel.SetActive(true);
            if (text != null) text.text = Textos.T("guia.final");
            if (fletxa != null) fletxa.gameObject.SetActive(false);
            _finalFins = Time.unscaledTime + segonsDelMissatgeFinal;
        }

        private void Amagar()
        {
            if (arrel != null && arrel.activeSelf) arrel.SetActive(false);
            if (fletxa != null && fletxa.gameObject.activeSelf) fletxa.gameObject.SetActive(false);
        }
    }
}
