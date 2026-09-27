using UnityEngine;

namespace ArcNet
{
    /// <summary>
    /// Developers implement this to define how their object behaves.
    /// ArcNet uses this to run the game on Client (Predict), Server (Auth), and Client (Reconcile).
    /// </summary>
    /// <typeparam name="TInput">Input structure (Buttons, Joystick)</typeparam>
    /// <typeparam name="TState">State structure (Position, Health, Ammo)</typeparam>
    public interface IArcNetSimulation<TInput, TState>
    {
        /// <summary>
        /// Run one tick of gameplay logic.
        /// e.g., Move Character, Fire Weapon, Consume Fuel.
        /// </summary>
        void ProcessInput(TInput input, float deltaTime);

        /// <summary>
        /// Capture the current state of the object for the history buffer.
        /// </summary>
        TState CaptureState();

        /// <summary>
        /// Restore the object to a previous state (used during rollback).
        /// </summary>
        void RestoreState(TState state);
    }
}