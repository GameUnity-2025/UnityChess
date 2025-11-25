using System;
using System.Threading.Tasks;
using UnityChess.Engine;
using UnityEngine;

namespace UnityChess.Engine
{
    /// <summary>
    /// Simple AI manager that only uses Mock AI (basic AI)
    /// </summary>
    public class AIFallbackManager : IUCIEngine
    {
        private IUCIEngine currentEngine;
        private bool isInitialized = false;

        public enum AIEngineType
        {
            Mock
        }

        public AIEngineType CurrentEngineType => AIEngineType.Mock;

        public void Start()
        {
            if (!isInitialized)
            {
                try
                {
                    currentEngine = new StockfishUCIEngine();
                    currentEngine.Start();
                    isInitialized = true;
                    Debug.Log("[AI] Initialized Mock AI");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[AI] Failed to initialize Mock AI: {ex.Message}");
                    currentEngine = null;
                }
            }
        }

        public void ShutDown()
        {
            if (currentEngine != null)
            {
                currentEngine.ShutDown();
                currentEngine = null;
            }
            isInitialized = false;
        }

        public async Task SetupNewGame(Game game)
        {
            if (currentEngine != null)
            {
                await currentEngine.SetupNewGame(game);
            }
        }

        public async Task<Movement> GetBestMove(int timeoutMS, int depth)
        {
            if (currentEngine == null)
            {
                Debug.LogError("[AI] No AI engine available!");
                return null;
            }

            try
            {
                Movement move = await currentEngine.GetBestMove(timeoutMS, depth);
                return move;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AI] Mock AI error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Get current engine status for debugging
        /// </summary>
        public string GetStatus()
        {
            return $"Current: {CurrentEngineType}";
        }
    }
}