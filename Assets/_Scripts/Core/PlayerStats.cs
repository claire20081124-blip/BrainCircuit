using UnityEngine;
using System;

namespace RunLight.Core
{
    public class PlayerStats : MonoBehaviour
    {
        public static PlayerStats Instance { get; private set; }

        [Header("腦力")]
        [SerializeField] private int maxBrainPower     = 100;
        [SerializeField] private int startBrainPower   = 50;
        [SerializeField] private int noteReadThreshold = 60;

        public int MaxBrainPower      => maxBrainPower;
        public int NoteReadThreshold  => noteReadThreshold;
        public int CurrentBrainPower  { get; private set; }

        public event Action<int, int> OnBrainPowerChanged; // (current, max)
        public event Action           OnDeath;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            CurrentBrainPower = startBrainPower;
            OnBrainPowerChanged?.Invoke(CurrentBrainPower, maxBrainPower);
        }

        public void TakeDamage(int amount)
        {
            CurrentBrainPower = Mathf.Max(0, CurrentBrainPower - amount);
            OnBrainPowerChanged?.Invoke(CurrentBrainPower, maxBrainPower);
            if (CurrentBrainPower <= 0) OnDeath?.Invoke();
        }

        public void GainBrainPower(int amount)
        {
            CurrentBrainPower = Mathf.Min(maxBrainPower, CurrentBrainPower + amount);
            OnBrainPowerChanged?.Invoke(CurrentBrainPower, maxBrainPower);
        }

        public bool CanReadNote => CurrentBrainPower >= noteReadThreshold;
    }
}
