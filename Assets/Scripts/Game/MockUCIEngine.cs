// Assets/Scripts/Game/MockUCIEngine.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityChess;

namespace UnityChess.Engine
{
    /// <summary>
    /// MockUCIEngine: Engine giả lập để test.
    /// - KHÔNG chạy process bên ngoài, KHÔNG gửi lệnh UCI.
    /// - Chỉ "suy nghĩ" (Task.Delay) rồi trả về một nước đi hợp lệ ngẫu nhiên.
    /// </summary>
    public class MockUCIEngine : IUCIEngine
    {
        private Game _game;
        private bool _running;
        private readonly System.Random _rng = new System.Random();

        /// <summary>
        /// Khởi động mock (không spawn process).
        /// </summary>
        public void Start()
        {
            _running = true;
            Debug.Log("[MOCK UCI] Started (no external process).");
        }

        /// <summary>
        /// Tắt mock (khớp interface: return void).
        /// </summary>
        public void ShutDown()
        {
            _running = false;
            Debug.Log("[MOCK UCI] Shut down.");
        }

        /// <summary>
        /// Thiết lập ván mới.
        /// </summary>
        public Task SetupNewGame(Game game)
        {
            _game = game;
            Debug.Log("[MOCK UCI] New game set.");
            return Task.CompletedTask;
        }

        /// <summary>
        /// Trả về một nước đi hợp lệ ngẫu nhiên sau khi "suy nghĩ" thinkTimeMs.
        /// </summary>
        public async Task<Movement> GetBestMove(int thinkTimeMs)
        {
            if (!_running) throw new InvalidOperationException("[MOCK UCI] Engine not started.");
            if (_game == null) throw new InvalidOperationException("[MOCK UCI] Game is null.");

            // Giả lập thời gian suy nghĩ (giới hạn 0..5000ms cho mock)
            int delay = Mathf.Clamp(thinkTimeMs, 0, 5000);
            if (delay > 0) await Task.Delay(delay);

            var moves = CollectAllLegalMoves(_game);
            if (moves.Count == 0)
            {
                Debug.Log("[MOCK UCI] No legal moves available.");
                return null;
            }

            // Chọn ngẫu nhiên 1 nước đi hợp lệ
            int idx = _rng.Next(moves.Count);
            return moves[idx];
        }

        /// <summary>
        /// Thu thập tất cả nước đi hợp lệ cho bên đang tới lượt.
        /// Không truy cập piece.Side; chỉ cần hỏi game.
        /// </summary>
        private static List<Movement> CollectAllLegalMoves(Game game)
        {
            var result = new List<Movement>();

            game.BoardTimeline.TryGetCurrent(out Board board);

            for (int file = 1; file <= 8; file++)
            {
                for (int rank = 1; rank <= 8; rank++)
                {
                    Piece piece = board[file, rank];
                    if (piece == null) continue;

                    if (game.TryGetLegalMovesForPiece(piece, out ICollection<Movement> legal) && legal != null)
                        result.AddRange(legal);
                }
            }

            return result;
        }
    }
}
