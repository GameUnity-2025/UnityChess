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
            string castling = "";
            if (conditions.WhiteCanCastleKingside) castling += "K";
            if (conditions.WhiteCanCastleQueenside) castling += "Q";
            if (conditions.BlackCanCastleKingside) castling += "k";
            if (conditions.BlackCanCastleQueenside) castling += "q";
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
            // Get current board and conditions
            _game.BoardTimeline.TryGetCurrent(out Board board);
            _game.ConditionsTimeline.TryGetCurrent(out GameConditions conditions);

            // Generate FEN
            string fen = GenerateFEN(board, conditions);

            // Set position in Stockfish
            currentPosition.set(fen, 0, ChessEngine.Stockfish.Engine.Threads.main());
            setupStates = new ChessEngine.Stockfish.StateStackPtr();

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

            // Get best move
            var bestMove = ChessEngine.Stockfish.Search.RootMoves[0].pv[0];

            // Convert to UnityChess Movement
            var startSquare = ChessEngine.Stockfish.Types.from_sq(bestMove);
            var endSquare = ChessEngine.Stockfish.Types.to_sq(bestMove);

            // Convert Stockfish square (0-63) to UnityChess Square (File 1-8, Rank 1-8)
            int startFile = ChessEngine.Stockfish.Types.file_of(startSquare) + 1;
            int startRank = ChessEngine.Stockfish.Types.rank_of(startSquare) + 1;
            int endFile = ChessEngine.Stockfish.Types.file_of(endSquare) + 1;
            int endRank = ChessEngine.Stockfish.Types.rank_of(endSquare) + 1;

            Square start = new Square(startFile, startRank);
            Square end = new Square(endFile, endRank);

            await Task.Yield();
            return new Movement(start, end);
        }

    }
}
