using System.Collections.Generic;
using UnityEngine;

namespace IdleFrogs.Game
{
    // Una fila del panell de millora: "Producció | 12.3/s | +4.5"
    // La unitat les genera; el panell només les pinta. Així el panell
    // no sap què és un pantà ni què és un tren.
    public struct FilaEstadistica
    {
        public string nom;
        public string valor;
        public string delta;

        public FilaEstadistica(string nom, string valor, string delta)
        {
            this.nom = nom;
            this.valor = valor;
            this.delta = delta;
        }
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

        public int Nivell => nivell;
        public bool EstaComprat => nivell >= 1;

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

        // S'omple una llista que ens presten, per no crear escombraries
        // cada vegada que el panell es refresca.
        public abstract void OmplirFiles(List<FilaEstadistica> files, int nivellDesti);

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

            nivell += quantitat;
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
