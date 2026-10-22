using System.Collections.Generic;
using UnityEngine;

namespace IdleFrogs.Game
{
    // Quina estadística és, perquè el panell sàpiga quina icona hi va. Cada
    // tram té les seves, encara que es diguin igual: la velocitat de la
    // granota i la del tren no porten el mateix dibuix.
    public enum TipusDEstadistica
    {
        Resum,
        VelocitatDeGranota, Granotes, Recollecio, CapacitatDeGranota,
        VelocitatDelTren, Carrega, Descarrega,
        // Restaurant (abans tenda: els valors no canvien, així les icones ja
        // assignades a l'inspector continuen al seu lloc).
        PerClient, Clients, VelocitatDelClient,
        TempsDeMenjar,
        // Al final perquè no es moguin els valors ja assignats a l'inspector.
        CapacitatDelTren
    }

    // Una estadística del panell de millora: "Capacitat  100k → 110k".
    // La unitat les genera; el panell només les pinta. Així el panell
    // no sap què és un pantà ni què és un tren.
    public struct FilaEstadistica
    {
        public TipusDEstadistica tipus;
        public string nom;
        public string valor;
        // El valor després de la millora que tens triada (x1, x10...).
        public string valorDesti;

        public FilaEstadistica(TipusDEstadistica tipus, string nom, string valor, string valorDesti = null)
        {
            this.tipus = tipus;
            this.nom = nom;
            this.valor = valor;
            this.valorDesti = valorDesti;
        }

        // Es compara el text i no el número: si tots dos s'escriuen igual
        // ("2.0" i "2.0") la fletxa no diria res.
        public bool TeCanvi => !string.IsNullOrEmpty(valorDesti) && valorDesti != valor;
    }

    // Els multiplicadors que hi posa l'encarregat assignat. Van tots junts
    // en un struct perquè es recalculen de cop i es guarden a la unitat:
    // llegir-los no ha de passar pel gestor cada frame.
    public struct Bonificacions
    {
        public double velocitat;
        public double treball;
        public double capacitat;
        public double reduccioDeCost;   // fracció 0..1

        public static Bonificacions Cap => new Bonificacions
        {
            velocitat = 1,
            treball = 1,
            capacitat = 1,
            reduccioDeCost = 0
        };
    }

    // Tot el que té nivell, corba de cost i panell de millora:
    // el pantà, el tren i (més endavant) l'estació.
    // El nivell 0 vol dir "encara no comprat".
    public abstract class UnitatMillorable : MonoBehaviour
    {
        [Header("Nivell (0 = no comprat)")]
        [SerializeField] protected int nivell = 1;

        [Header("Panell de millora")]
        [Tooltip("La foto de la targeta de dalt del panell")]
        [SerializeField] private Sprite retratDelPanell;

        public int Nivell => nivell;
        public bool EstaComprat => nivell >= 1;
        public Sprite RetratDelPanell => retratDelPanell;
        // El text del separador, a sobre de les caixes d'estadístiques.
        public abstract string TitolDeLesEstadistiques { get; }

        public event System.Action EnCanviDeNivell;

        // --- El que ha de definir cada unitat ---

        // Quina ranura del món sóc, per saber quin encarregat em toca.
        public abstract Ranura Ranura { get; }

        public Bonificacions Bonus { get; private set; } = Bonificacions.Cap;
        public bool EstaAutomatitzada { get; private set; }
        public event System.Action EnCanviDeBonificacions;

        // La crida el GestorDEncarregats quan canvia una assignació o quan
        // s'encén o s'apaga una habilitat.
        public void AplicarBonificacions(Bonificacions bonus, bool automatitzada)
        {
            Bonus = bonus;
            EstaAutomatitzada = automatitzada;
            EnCanviDeBonificacions?.Invoke();
            AvisarCanviDeNivell();   // les estadístiques mostrades han canviat
        }

        public abstract string NomVisible { get; }
        public abstract double CostBase { get; }
        public abstract double CreixementDeCost { get; }
        public abstract int NivellMaxim { get; }

        // L'estadística general, la que va a la targeta de dalt ("Producció
        // total"), amb el valor al nivell de destí igual que les caixes.
        public abstract FilaEstadistica Resum(int nivellDesti);

        // Les caixes de sota. S'omple una llista que ens presten, per no crear
        // escombraries cada vegada que el panell es refresca.
        public abstract void OmplirFiles(List<FilaEstadistica> files, int nivellDesti);

        // Les fites del tram: fan el salt d'estadístiques i donen les gemes.
        public abstract FitaDeMultiplicador[] FitesDeMillora { get; }

        // Ganxo per a les subclasses (repartir granotes, etc.)
        protected virtual void DespresDeCanviDeNivell() { }

        // ---------------------------------------------------------------
        // Economia — sèrie geomètrica: pujar k nivells no costa k vegades
        // el primer, costa la suma dels k costos individuals.
        // ---------------------------------------------------------------

        // Una sola línia de reducció de cost per als tres trams: pantà, tren
        // i tenda passen tots per aquí.
        private double CostDelSeguentNivell =>
            CostBase * System.Math.Pow(CreixementDeCost, nivell - 1) * (1.0 - Bonus.reduccioDeCost);

        public double CostDeMillora(int quantitat)
        {
            if (quantitat <= 0 || !EstaComprat) return 0;
            double g = CreixementDeCost;
            double primer = CostDelSeguentNivell;
            if (System.Math.Abs(g - 1.0) < 1e-9) return primer * quantitat;
            return primer * (System.Math.Pow(g, quantitat) - 1) / (g - 1);
        }

        // Forma tancada: quants nivells caben als doblers que tinc.
        public int MillorsQuePucPagar()
        {
            if (!EstaComprat || CurrencyManager.Instancia == null) return 0;

            double g = CreixementDeCost;
            double primer = CostDelSeguentNivell;
            double doblers = CurrencyManager.Instancia.doblersActuals;
            if (doblers < primer) return 0;

            int k;
            if (System.Math.Abs(g - 1.0) < 1e-9)
                k = (int)(doblers / primer);
            else
                k = (int)System.Math.Floor(
                        System.Math.Log(1 + doblers * (g - 1) / primer) / System.Math.Log(g));

            return Mathf.Clamp(k, 0, NivellMaxim - nivell);
        }

        public bool Millorar(int quantitat)
        {
            if (!EstaComprat) return false;
            quantitat = Mathf.Clamp(quantitat, 0, NivellMaxim - nivell);
            if (quantitat <= 0) return false;
            if (CurrencyManager.Instancia == null) return false;
            if (!CurrencyManager.Instancia.GastarDoblers(CostDeMillora(quantitat))) return false;

            // Només aquí i no a AplicarNivell: carregar una partida no ha de
            // tornar a pagar les fites que ja tenies.
            int gemes = Fites.GemesEntre(FitesDeMillora, nivell, nivell + quantitat);

            nivell += quantitat;
            if (gemes > 0) CurrencyManager.Instancia.AfegirGemes(gemes);
            DespresDeCanviDeNivell();
            EnCanviDeNivell?.Invoke();
            return true;
        }

        // La crida el carregador de partida, abans d'inicialitzar res.
        public void AplicarNivell(int n)
        {
            nivell = Mathf.Clamp(n, 0, NivellMaxim);
            DespresDeCanviDeNivell();
            EnCanviDeNivell?.Invoke();
        }

        protected void AvisarCanviDeNivell() => EnCanviDeNivell?.Invoke();
    }
}
