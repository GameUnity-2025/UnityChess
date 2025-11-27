using System;
using System.Threading.Tasks;
using UnityChess;
using UnityChess.Engine;
using UnityEngine;

namespace UnityChess.Engine {
    public class StockfishUCIEngine : IUCIEngine {
        private Game _game;
        private ChessEngine.Stockfish.Position currentPosition;
        private ChessEngine.Stockfish.StateStackPtr setupStates;

        public void Start() {
            // Initialize the engine components
            ChessEngine.Stockfish.Uci.init(ChessEngine.Stockfish.Engine.Options);
            ChessEngine.Stockfish.BitBoard.init();
            ChessEngine.Stockfish.Position.init();
            ChessEngine.Stockfish.Bitbases.init_kpk();
            ChessEngine.Stockfish.Search.init();
            ChessEngine.Stockfish.Pawns.init();
            ChessEngine.Stockfish.Eval.init();
            ChessEngine.Stockfish.Engine.Threads.init();
            ChessEngine.Stockfish.Engine.TT.resize((ulong)ChessEngine.Stockfish.Engine.Options["Hash"].getInt());

            // Reset the shutdown flag
            isShutDown = false;

            // Set up initial position
            currentPosition = new ChessEngine.Stockfish.Position(ChessEngine.Stockfish.Uci.StartFEN, 0, ChessEngine.Stockfish.Engine.Threads.main());
            setupStates = new ChessEngine.Stockfish.StateStackPtr();
        }

        private bool isShutDown = false;

        public void ShutDown() {
            if (!isShutDown) {
                try {
                    ChessEngine.Stockfish.Engine.Threads.exit();
                    isShutDown = true;
                } catch (System.Exception e) {
                    Debug.LogWarning($"Stockfish engine shutdown failed: {e.Message}");
                }
            }
        }

        public async Task SetupNewGame(Game game) {
            _game = game;
            // Reset to starting position
            currentPosition.set(ChessEngine.Stockfish.Uci.StartFEN, ChessEngine.Stockfish.Engine.Options["UCI_Chess960"].getInt(), ChessEngine.Stockfish.Engine.Threads.main());
            setupStates = new ChessEngine.Stockfish.StateStackPtr();
            await Task.Yield();
        }

        private string GenerateFEN(Board board, GameConditions conditions) {
            // Generate FEN from board and conditions
            string fen = "";
            for (int rank = 8; rank >= 1; rank--) {
                int emptyCount = 0;
                for (int file = 1; file <= 8; file++) {
                    Piece piece = board[file, rank];
                    if (piece == null) {
                        emptyCount++;
                    } else {
                        if (emptyCount > 0) {
                            fen += emptyCount.ToString();
                            emptyCount = 0;
                        }
                        char pieceChar = GetPieceChar(piece);
                        fen += pieceChar;
                    }
                }
                if (emptyCount > 0) {
                    fen += emptyCount.ToString();
                }
                if (rank > 1) fen += "/";
            }
            fen += " ";
            fen += conditions.SideToMove == Side.White ? "w" : "b";
            fen += " ";
            
            // Validate castling rights: MUST check both King and Rook positions
            string castling = "";
            bool whiteKingOnE1 = board[5, 1] is King whiteKing && whiteKing.Owner == Side.White;
            bool blackKingOnE8 = board[5, 8] is King blackKing && blackKing.Owner == Side.Black;
            
            if (conditions.WhiteCanCastleKingside && whiteKingOnE1 && 
                board[8, 1] is Rook whiteKingRook && whiteKingRook.Owner == Side.White) 
                castling += "K";
            if (conditions.WhiteCanCastleQueenside && whiteKingOnE1 && 
                board[1, 1] is Rook whiteQueenRook && whiteQueenRook.Owner == Side.White) 
                castling += "Q";
            if (conditions.BlackCanCastleKingside && blackKingOnE8 && 
                board[8, 8] is Rook blackKingRook && blackKingRook.Owner == Side.Black) 
                castling += "k";
            if (conditions.BlackCanCastleQueenside && blackKingOnE8 && 
                board[1, 8] is Rook blackQueenRook && blackQueenRook.Owner == Side.Black) 
                castling += "q";
            if (castling == "") castling = "-";
            fen += castling;
            fen += " ";
            var enPassant = conditions.EnPassantSquare;
            fen += enPassant != UnityChess.Square.Invalid ? enPassant.ToString().ToLower() : "-";
            fen += " ";
            fen += conditions.HalfMoveClock.ToString();
            fen += " ";
            fen += conditions.TurnNumber.ToString();
            return fen;
        }

        private char GetPieceChar(Piece piece) {
            char c = piece switch {
                Pawn _ => 'p',
                Knight _ => 'n',
                Bishop _ => 'b',
                Rook _ => 'r',
                Queen _ => 'q',
                King _ => 'k',
                _ => '?'
            };
            return piece.Owner == Side.White ? char.ToUpper(c) : c;
        }

        public async Task<Movement> GetBestMove(int timeoutMS, int depth) {
            try
            {
                // Get current board and conditions
                if (!_game.BoardTimeline.TryGetCurrent(out Board board))
                {
                    Debug.LogError("[StockfishUCIEngine] Failed to get current board.");
                    return null;
                }

                if (!_game.ConditionsTimeline.TryGetCurrent(out GameConditions conditions))
                {
                    Debug.LogError("[StockfishUCIEngine] Failed to get current game conditions.");
                    return null;
                }

                // Generate FEN
                string fen = GenerateFEN(board, conditions);
                Debug.Log($"[StockfishUCIEngine] FEN: {fen}");

                // Try to set position, retry once if it fails
                bool positionSet = false;
                for (int attempt = 0; attempt < 2; attempt++)
                {
                    try
                    {
                        // Recreate position object on retry
                        if (attempt > 0)
                        {
                            Debug.LogWarning("[StockfishUCIEngine] Retrying with fresh position object...");
                            currentPosition = new ChessEngine.Stockfish.Position(ChessEngine.Stockfish.Uci.StartFEN, 0, ChessEngine.Stockfish.Engine.Threads.main());
                        }

                        currentPosition.set(fen, 0, ChessEngine.Stockfish.Engine.Threads.main());
                        setupStates = new ChessEngine.Stockfish.StateStackPtr();
                        positionSet = true;
                        break;
                    }
                    catch (Exception posEx)
                    {
                        if (attempt == 0)
                        {
                            Debug.LogWarning($"[StockfishUCIEngine] Position.set failed (attempt {attempt + 1}): {posEx.Message}");
                        }
                        else
                        {
                            throw; // Re-throw on second attempt
                        }
                    }
                }

                if (!positionSet)
                {
                    Debug.LogError("[StockfishUCIEngine] Failed to set position after retries.");
                    return null;
                }

                // Set limits
                ChessEngine.Stockfish.LimitsType limits = new ChessEngine.Stockfish.LimitsType();
                limits.movetime = timeoutMS;
                if (depth > 0) {
                    limits.depth = depth;
                }

                // Start thinking
                ChessEngine.Stockfish.Engine.Threads.start_thinking(currentPosition, limits, setupStates);

                // Wait for completion
                ChessEngine.Stockfish.Engine.Threads.wait_for_think_finished();

                // Check if we have any moves
                if (ChessEngine.Stockfish.Search.RootMoves == null || ChessEngine.Stockfish.Search.RootMoves.Count == 0)
                {
                    Debug.LogError("[StockfishUCIEngine] No moves available from engine.");
                    return null;
                }

                // Get best move
                var bestMove = ChessEngine.Stockfish.Search.RootMoves[0].pv[0];

                // Convert to UnityChess Movement
                var startSquare = ChessEngine.Stockfish.Types.from_sq(bestMove);
                var endSquare = ChessEngine.Stockfish.Types.to_sq(bestMove);

                // Check if this is a castling move
                bool isCastling = ChessEngine.Stockfish.Types.type_of_move(bestMove) == ChessEngine.Stockfish.MoveTypeS.CASTLING;
                
                // For castling, Stockfish encodes as "King captures Rook" (e1a1 or e1h1)
                // We need to convert to standard notation (e1c1 or e1g1)
                int originalRookSquare = endSquare; // Save original rook position for CastlingMove
                if (isCastling)
                {
                    // Convert to standard chess notation
                    // If rook is to the right (kingside): King goes to g-file
                    // If rook is to the left (queenside): King goes to c-file
                    bool kingside = endSquare > startSquare;
                    endSquare = ChessEngine.Stockfish.Types.make_square(
                        kingside ? ChessEngine.Stockfish.FileS.FILE_G : ChessEngine.Stockfish.FileS.FILE_C,
                        ChessEngine.Stockfish.Types.rank_of(startSquare)
                    );
                }

                // Convert Stockfish square (0-63) to UnityChess Square (File 1-8, Rank 1-8)
                int startFile = ChessEngine.Stockfish.Types.file_of(startSquare) + 1;
                int startRank = ChessEngine.Stockfish.Types.rank_of(startSquare) + 1;
                int endFile = ChessEngine.Stockfish.Types.file_of(endSquare) + 1;
                int endRank = ChessEngine.Stockfish.Types.rank_of(endSquare) + 1;

                Square start = new Square(startFile, startRank);
                Square end = new Square(endFile, endRank);

                Debug.Log($"[StockfishUCIEngine] AI Move: {start} -> {end}{(isCastling ? " (Castling)" : "")}");

                await Task.Yield();
                
                // Return CastlingMove if it's a castling move, otherwise regular Movement
                if (isCastling)
                {
                    int rookFile = ChessEngine.Stockfish.Types.file_of(originalRookSquare) + 1;
                    int rookRank = ChessEngine.Stockfish.Types.rank_of(originalRookSquare) + 1;
                    Square rook = new Square(rookFile, rookRank);
                    Debug.Log($"[StockfishUCIEngine] Castling - Rook at {rook}");
                    return new CastlingMove(start, end, rook);
                }
                
                return new Movement(start, end);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[StockfishUCIEngine] Exception in GetBestMove: {ex.Message}\n{ex.StackTrace}");
                return null;
            }
        }

    }
}
